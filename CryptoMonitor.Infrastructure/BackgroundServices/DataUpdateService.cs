using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic; // <-- ADICIONADO
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using CryptoMonitor.Core.Interfaces;
using CryptoMonitor.Core.DTOs;
using CryptoMonitor.Infrastructure.Options;
using CryptoMonitor.Core.Models;

namespace CryptoMonitor.Infrastructure.BackgroundServices
{
    /// <summary>
    /// Atualiza periodicamente os principais snapshots (USD/BRL + overview)
    /// para manter o cache quente e reduzir latência nos endpoints.
    /// Publica o evento de preços em tempo real via IRealtimeNotifier.
    /// Projeta para a model de domínio (Cryptocurrency) e,
    /// se existir store de histórico, persiste pontos (USD/BRL) e embute sparkline 7d.
    /// </summary>
    public sealed class DataUpdateService(
        ILogger<DataUpdateService> logger,
        IServiceScopeFactory scopeFactory,
        IOptions<BackgroundJobsOptions> opts) : BackgroundService
    {
        private readonly ILogger<DataUpdateService> _logger = logger;
        private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
        private readonly BackgroundJobsOptions _opts = opts.Value;
        
        private const int SparkPoints = 50;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var period = TimeSpan.FromSeconds(Math.Max(5, _opts.DataUpdateSeconds));
            var timer = new PeriodicTimer(period);

            _logger.LogInformation("DataUpdateService iniciado. Intervalo: {Interval}s", period.TotalSeconds);

            while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var crypto = scope.ServiceProvider.GetRequiredService<ICryptoService>();
                    var notifier = scope.ServiceProvider.GetRequiredService<IRealtimeNotifier>();
                    
                    var historyStore = scope.ServiceProvider.GetService<IPriceHistoryStore>();
                    
                    var usd = await crypto.GetMarketsAsync(
                        new MarketsQuery { VsCurrency = "usd", Page = 1, PageSize = 250 }, stoppingToken);
                    var brl = await crypto.GetMarketsAsync(
                        new MarketsQuery { VsCurrency = "brl", Page = 1, PageSize = 250 }, stoppingToken);
                    await crypto.GetGlobalAsync(stoppingToken);
                    
                    var mapBrl = brl.Items.ToDictionary(x => x.Id, x => x);
                    var coins = usd.Items.Select(u =>
                    {
                        mapBrl.TryGetValue(u.Id, out var b);
                        return new Cryptocurrency
                        {
                            Id = u.Id,
                            Symbol = u.Symbol,
                            Name = u.Name,
                            Logo = u.Logo,
                            MarketCapRank = u.MarketCapRank,
                            PriceUsd = u.Price,
                            PriceBrl = b?.Price,
                            Change24hUsd = u.Change24hPct,
                            Change24hBrl = b?.Change24hPct,
                            MarketCapUsd = u.MarketCap,
                            MarketCapBrl = b?.MarketCap
                        };
                    }).ToList();
                    
                    if (historyStore is not null)
                    {
                        var now = DateTime.UtcNow;
                        foreach (var c in coins)
                        {
                            if (c.PriceUsd.HasValue)
                                historyStore.GetOrCreate(c.Id, "usd").Add(c.PriceUsd.Value, now);
                            if (c.PriceBrl.HasValue)
                                historyStore.GetOrCreate(c.Id, "brl").Add(c.PriceBrl.Value, now);
                        }
                    }

                    var ticks = coins.Select(c =>
                    {
                        List<decimal>? spark = null;

                        if (historyStore is not null)
                        {
                            var from = DateTime.UtcNow.AddDays(-7);
                            
                            var raw = historyStore
                                .GetOrCreate(c.Id, "usd")
                                .RangeFrom(from)
                                .Select(p => p.Price)
                                .ToList();

                            if (raw.Count > 0)
                                spark = raw.Count <= SparkPoints ? raw : DownsampleLinear(raw, SparkPoints);
                        }

                        return new PriceTickDto
                        {
                            Id = c.Id,
                            Symbol = c.Symbol,
                            Name = c.Name,
                            PriceUsd = c.PriceUsd ?? 0m,
                            PriceBrl = c.PriceBrl ?? 0m,
                            Change24hUsd = c.Change24hUsd,
                            Change24hBrl = c.Change24hBrl,
                            MarketCapRank = c.MarketCapRank,
                            Logo = c.Logo,
                            MarketCap = c.MarketCapUsd,
                            Sparkline7d = spark
                        };
                    }).ToList();

                    await notifier.BroadcastPricesAsync(ticks, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Falha ao atualizar snapshots de mercado.");
                }
            }

            _logger.LogInformation("DataUpdateService finalizado.");
        }

        /// <summary>
        /// Reduz um conjunto de N pontos para "target" pontos por interpolação linear,
        /// preservando início e fim.
        /// </summary>
        private static List<decimal> DownsampleLinear(List<decimal> src, int target)
        {
            if (src.Count == 0) return [];
            if (target <= 0 || src.Count <= target) return src;

            var res = new List<decimal>(target);
            var step = (src.Count - 1) / (decimal)(target - 1);

            for (int i = 0; i < target; i++)
            {
                var idx = i * step;
                var lo = (int)decimal.Truncate(idx);
                var hi = Math.Min(lo + 1, src.Count - 1);
                var frac = idx - lo;
                var v = src[lo] + (src[hi] - src[lo]) * frac;
                res.Add(v);
            }
            return res;
        }
    }
}
