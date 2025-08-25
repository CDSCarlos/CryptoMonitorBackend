using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CryptoMonitor.Core.DTOs;
using CryptoMonitor.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace CryptoMonitor.Infrastructure.Services
{
    public sealed class AlertService(ILogger<AlertService> logger) : IAlertService
    {
        private readonly ILogger<AlertService> _logger = logger;
        private readonly ConcurrentDictionary<Guid, AlertDto> _store = new();

        public Task<AlertDto> CreateAsync(AlertCreateDto dto, CancellationToken ct)
        {
            var model = new AlertDto
            {
                Id = Guid.NewGuid(),
                CoinId = dto.CoinId.Trim().ToLowerInvariant(),
                Vs = (dto.Vs ?? "usd").Trim().ToLowerInvariant(),
                TargetPrice = dto.TargetPrice,
                Direction = (dto.Direction ?? "above").Trim().ToLowerInvariant(),
                Label = dto.Label,
                CreatedAtUtc = DateTime.UtcNow
            };
            _store[model.Id] = model;
            return Task.FromResult(model);
        }

        public Task<IReadOnlyList<AlertDto>> ListAsync(CancellationToken ct)
            => Task.FromResult<IReadOnlyList<AlertDto>>([.. _store.Values.OrderBy(x => x.CreatedAtUtc)]);

        public Task<bool> DeleteAsync(Guid id, CancellationToken ct)
            => Task.FromResult(_store.TryRemove(id, out _));

        public Task ProcessAsync(
            IReadOnlyDictionary<string, decimal> pricesUsd,
            IReadOnlyDictionary<string, decimal> pricesBrl,
            CancellationToken ct)
        {
            foreach (var kv in _store)
            {
                var a = kv.Value;
                var book = a.Vs == "brl" ? pricesBrl : pricesUsd;
                if (!book.TryGetValue(a.CoinId, out var price)) continue;

                a.LastPrice = price;

                var hit = a.Direction == "above" ? price >= a.TargetPrice
                                                 : price <= a.TargetPrice;

                if (hit && !a.IsTriggered)
                {
                    a.IsTriggered = true;
                    a.TriggeredAtUtc = DateTime.UtcNow;
                    _logger.LogInformation("Alerta acionado: {Id} {Coin} {Vs} {Price} ({Dir} {Target})",
                        a.Id, a.CoinId, a.Vs, price, a.Direction, a.TargetPrice);
                    
                }
            }
            return Task.CompletedTask;
        }
    }
}
