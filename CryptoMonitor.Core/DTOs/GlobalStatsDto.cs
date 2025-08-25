namespace CryptoMonitor.Core.DTOs
{
    public sealed class GlobalStatsDto
    {
        public decimal TotalMarketCapUsd { get; set; }
        public decimal TotalMarketCapBrl { get; set; }
        public decimal TotalVolumeUsd { get; set; }
        public decimal TotalVolumeBrl { get; set; }

        public decimal MarketCapChangePct24hUsd { get; set; }
        public decimal BtcDominancePct { get; set; }
        public decimal EthDominancePct { get; set; }

        public int ActiveCryptocurrencies { get; set; }
        public int OngoingIcos { get; set; }
        public int EndedIcos { get; set; }
        public int Markets { get; set; }
    }
}