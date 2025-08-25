namespace CryptoMonitor.Core.DTOs
{
    public class MarketsQuery
    {
        public string VsCurrency { get; set; } = "usd";
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public string? Search { get; set; }
        public string? Ids { get; set; }
        public string OrderBy { get; set; } = "market_cap_desc";
    }
}