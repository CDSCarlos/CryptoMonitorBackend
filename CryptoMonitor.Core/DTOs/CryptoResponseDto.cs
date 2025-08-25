using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptoMonitor.Core.DTOs
{
    public sealed class CryptoResponseDto
    {
        public string Id { get; set; } = default!;
        public string Symbol { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string Logo { get; set; } = default!;
        public decimal Price { get; set; }
        public decimal MarketCap { get; set; }
        public decimal? Change24hPct { get; set; }
        public string Currency { get; set; } = "usd";
        public int? MarketCapRank { get; set; }
    }
}
