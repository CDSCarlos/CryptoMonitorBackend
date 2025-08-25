using System.Collections.Concurrent;
using CryptoMonitor.Core.Interfaces;
using CryptoMonitor.Core.Models;

namespace CryptoMonitor.Infrastructure.Services
{
    public sealed class InMemoryPriceHistoryStore : IPriceHistoryStore
    {
        private readonly ConcurrentDictionary<(string coinId, string vs), PriceHistory> _store = new();

        public PriceHistory GetOrCreate(string coinId, string vs)
        {
            var key = (coinId.ToLowerInvariant(), vs.ToLowerInvariant());
            return _store.GetOrAdd(key, static k => new PriceHistory(k.coinId, k.vs));
        }
    }
}
