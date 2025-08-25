using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CryptoMonitor.Core.Interfaces
{
    /// <summary>
    /// Abstração para chamadas HTTP a serviços externos (ex.: CoinGecko).
    /// Mantém a política de deserialização e resolve URLs relativas ao BaseAddress.
    /// </summary>
    public interface IExternalApiService
    {
        /// <summary>
        /// Executa GET e desserializa o JSON para o tipo informado.
        /// O caminho deve ser relativo ao BaseAddress configurado no HttpClient.
        /// </summary>
        Task<T?> GetAsync<T>(string relativePath, CancellationToken ct, JsonSerializerOptions? jsonOptions = null);

        /// <summary>
        /// Helper para montar URL relativa com query string (com encoding).
        /// </summary>
        string BuildUrl(string path, IEnumerable<(string Key, string? Value)> query);
    }
}
