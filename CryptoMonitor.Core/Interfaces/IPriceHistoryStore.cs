using CryptoMonitor.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptoMonitor.Core.Interfaces
{
    public interface IPriceHistoryStore
    {
        PriceHistory GetOrCreate(string coinId, string vs);
    }
}
