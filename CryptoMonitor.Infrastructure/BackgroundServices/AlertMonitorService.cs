using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using CryptoMonitor.Core.Interfaces;
using CryptoMonitor.Core.DTOs;
using CryptoMonitor.Infrastructure.Options;

namespace CryptoMonitor.Infrastructure.BackgroundServices
{
    /// <summary>
    /// Verifica preços atuais e processa alertas (acende IsTriggered e registra TriggeredAtUtc).
    /// Também publica eventos em tempo real via IRealtimeNotifier (ex.: SignalR).
    /// </summary>
    public sealed class AlertMonitorService(
        ILogger<AlertMonitorService> logger,
        IServiceScopeFactory scopeFactory,
        IOptions<BackgroundJobsOptions> opts) : BackgroundService
    {
        private readonly ILogger<AlertMonitorService> _logger = logger;
        private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
        private readonly BackgroundJobsOptions _opts = opts.Value;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var period = TimeSpan.FromSeconds(Math.Max(5, _opts.AlertCheckSeconds));
            var timer = new PeriodicTimer(period);

            _logger.LogInformation("AlertMonitorService iniciado. Intervalo: {Interval}s", period.TotalSeconds);

            while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();

                    var crypto = scope.ServiceProvider.GetRequiredService<ICryptoService>();
                    var alertsSvc = scope.ServiceProvider.GetRequiredService<IAlertService>();
                    var notifier = scope.ServiceProvider.GetRequiredService<IRealtimeNotifier>();
                    
                    var usd = await crypto.GetMarketsAsync(new MarketsQuery { VsCurrency = "usd", Page = 1, PageSize = 250 }, stoppingToken);
                    var brl = await crypto.GetMarketsAsync(new MarketsQuery { VsCurrency = "brl", Page = 1, PageSize = 250 }, stoppingToken);

                    var pricesUsd = usd.Items.ToDictionary(x => x.Id, x => x.Price);
                    var pricesBrl = brl.Items.ToDictionary(x => x.Id, x => x.Price);
                    
                    var currentAlerts = await alertsSvc.ListAsync(stoppingToken);
                    foreach (var a in currentAlerts)
                    {
                        var book = a.Vs == "brl" ? pricesBrl : pricesUsd;
                        var hasNow = book.TryGetValue(a.CoinId, out var priceNow);
                        if (!hasNow) continue;
                        
                        var isHit = a.Direction == "above" ? priceNow >= a.TargetPrice
                                                           : priceNow <= a.TargetPrice;

                        if (isHit && !a.IsTriggered)
                        {
                            var payload = new AlertTriggeredDto
                            {
                                AlertId = a.Id,
                                CoinId = a.CoinId,
                                Vs = a.Vs,
                                TargetPrice = a.TargetPrice,
                                Direction = a.Direction,
                                CurrentPrice = priceNow,
                                TriggeredAtUtc = DateTime.UtcNow,
                                Label = a.Label
                            };

                            await notifier.BroadcastAlertAsync(payload, stoppingToken);
                            _logger.LogInformation("alertTriggered broadcast: {Id} {Coin} {Vs} {Price}", a.Id, a.CoinId, a.Vs, priceNow);
                        }
                    }
                    
                    await alertsSvc.ProcessAsync(pricesUsd, pricesBrl, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Falha ao processar alertas.");
                }
            }

            _logger.LogInformation("AlertMonitorService finalizado.");
        }
    }
}
