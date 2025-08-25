using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptoMonitor.Core.DTOs
{
    public sealed class PriceSampleDto
    {
        public long T { get; set; }
        public decimal P { get; set; }
    }

    public sealed class PriceHistoryDto
    {
        public string CoinId { get; set; } = default!;
        public string Vs { get; set; } = "usd";
        public DateTime FromUtc { get; set; }
        public DateTime ToUtc { get; set; }
        public List<PriceSampleDto> Samples { get; set; } = [];
    }
}
