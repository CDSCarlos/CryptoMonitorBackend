using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CryptoMonitor.Infrastructure.External.CoinGecko.Models
{
    internal sealed class CoinGeckoGlobal
    {
        [JsonPropertyName("data")] public CoinGeckoGlobalData Data { get; set; } = new();
    }

    internal sealed class CoinGeckoGlobalData
    {
        [JsonPropertyName("active_cryptocurrencies")] public int ActiveCryptocurrencies { get; set; }
        [JsonPropertyName("upcoming_icos")] public int UpcomingIcos { get; set; }
        [JsonPropertyName("ongoing_icos")] public int OngoingIcos { get; set; }
        [JsonPropertyName("ended_icos")] public int EndedIcos { get; set; }
        [JsonPropertyName("markets")] public int Markets { get; set; }
        [JsonPropertyName("total_market_cap")] public Dictionary<string, decimal> TotalMarketCap { get; set; } = [];
        [JsonPropertyName("total_volume")] public Dictionary<string, decimal> TotalVolume { get; set; } = [];
        [JsonPropertyName("market_cap_percentage")] public Dictionary<string, decimal> MarketCapPercentage { get; set; } = [];
        [JsonPropertyName("market_cap_change_percentage_24h_usd")] public decimal MarketCapChangePct24hUsd { get; set; }
    }
}
