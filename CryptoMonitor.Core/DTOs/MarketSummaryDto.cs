namespace CryptoMonitor.Core.DTOs
{
    public sealed class MarketSummaryDto
    {
        public int Count { get; set; }
        public int Advancers { get; set; }
        public int Decliners { get; set; }
        public decimal AvgChange24hPct { get; set; }
        public decimal SumMarketCap { get; set; }
    }
}