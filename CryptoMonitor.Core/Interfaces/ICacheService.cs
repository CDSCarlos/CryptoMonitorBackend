using System;

namespace CryptoMonitor.Core.Interfaces
{
    /// <summary>
    /// Abstração de cache em memória com API genérica e segura para nullables.
    /// </summary>
    public interface ICacheService
    {
        bool TryGet<T>(string key, out T? value);
        void Set<T>(string key, T value, TimeSpan ttl);
        void Remove(string key);
    }
}