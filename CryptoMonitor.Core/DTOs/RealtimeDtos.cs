using System;
using System.Collections.Generic;

namespace CryptoMonitor.Core.DTOs
{
    public sealed class PriceTickDto
    {
        public string Id { get; set; } = default!;
        public string Symbol { get; set; } = default!;
        public string Name { get; set; } = default!;
        public int? MarketCapRank { get; set; }

        public decimal PriceUsd { get; set; }
        public decimal PriceBrl { get; set; }

        public decimal? Change24hUsd { get; set; }
        public decimal? Change24hBrl { get; set; }
        
        public IReadOnlyList<decimal>? Sparkline7d { get; set; }
        public string? Logo { get; set; }
        public decimal? MarketCap { get; set; }
    }

    public sealed class AlertTriggeredDto
    {
        public Guid AlertId { get; set; }
        public string CoinId { get; set; } = default!;
        public string Vs { get; set; } = "usd";
        public decimal TargetPrice { get; set; }
        public string Direction { get; set; } = "above";
        public decimal CurrentPrice { get; set; }
        public DateTime TriggeredAtUtc { get; set; }
        public string? Label { get; set; }
    }
}
