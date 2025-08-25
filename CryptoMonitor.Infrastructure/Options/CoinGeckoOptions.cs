using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace CryptoMonitor.Infrastructure.Options
{
    public sealed class CoinGeckoOptions
    {
        [Required, Url]
        public string BaseUrl { get; set; } = "https://api.coingecko.com/api/v3/";

        [Range(1, 120)]
        public int DefaultTimeoutSeconds { get; set; } = 10;

        [Range(1, 600)]
        public int CacheSeconds { get; set; } = 30;
    }
}
