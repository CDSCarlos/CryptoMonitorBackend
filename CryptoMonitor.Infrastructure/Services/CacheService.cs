using System;
using CryptoMonitor.Core.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace CryptoMonitor.Infrastructure.Services
{
    public sealed class CacheService(IMemoryCache cache) : ICacheService
    {
        private readonly IMemoryCache _cache = cache;

        public bool TryGet<T>(string key, out T? value)
        {
            if (_cache.TryGetValue(key, out var obj) && obj is T typed)
            {
                value = typed;
                return true;
            }
            value = default;
            return false;
        }

        public void Set<T>(string key, T value, TimeSpan ttl)
        {
            _cache.Set(key, value, ttl);
        }

        public void Remove(string key) => _cache.Remove(key);
    }
}
