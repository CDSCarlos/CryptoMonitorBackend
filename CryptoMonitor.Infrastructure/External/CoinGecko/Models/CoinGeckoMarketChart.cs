using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptoMonitor.Infrastructure.External.CoinGecko.Models
{
    public sealed class CoinGeckoMarketChart
    {
        public List<List<decimal>> Prices { get; set; } = new();
    }
}
