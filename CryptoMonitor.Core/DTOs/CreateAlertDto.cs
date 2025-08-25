using System;

namespace CryptoMonitor.Core.DTOs
{
    public class AlertCreateDto
    {
        public string CoinId { get; set; } = default!;
        public string Vs { get; set; } = "usd";   
        public decimal TargetPrice { get; set; }
        public string Direction { get; set; } = "above";
        public string? Label { get; set; }
    }

    public sealed class AlertDto : AlertCreateDto
    {
        public Guid Id { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public bool IsTriggered { get; set; }
        public DateTime? TriggeredAtUtc { get; set; }
        public decimal? LastPrice { get; set; }
    }
}
