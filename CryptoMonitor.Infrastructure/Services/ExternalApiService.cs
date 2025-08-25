using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CryptoMonitor.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace CryptoMonitor.Infrastructure.Services
{
    /// <summary>
    /// Serviço HTTP genérico para integração com APIs externas.
    /// - Assume que o HttpClient já foi configurado (BaseAddress, User-Agent, Polly etc).
    /// - Lida com caminhos relativos corretamente (sem confusão de barra inicial).
    /// </summary>
    public sealed class ExternalApiService(HttpClient http, ILogger<ExternalApiService> logger) : IExternalApiService
    {
        private readonly HttpClient _http = http;
        private readonly ILogger<ExternalApiService> _logger = logger;

        private static readonly JsonSerializerOptions DefaultJson = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public async Task<T?> GetAsync<T>(string relativePath, CancellationToken ct, JsonSerializerOptions? jsonOptions = null)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                throw new ArgumentException("Path inválido.", nameof(relativePath));
            
            var path = relativePath.StartsWith('/') ? relativePath[1..] : relativePath;

            _logger.LogDebug("GET {Url}", path);
            
            return await _http.GetFromJsonAsync<T>(path, jsonOptions ?? DefaultJson, ct);
        }

        public string BuildUrl(string path, IEnumerable<(string Key, string? Value)> query)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path inválido.", nameof(path));

            var p = path.StartsWith('/') ? path[1..] : path;

            var first = true;
            var uri = new System.Text.StringBuilder(p);

            foreach (var (key, value) in query)
            {
                if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value))
                    continue;

                uri.Append(first ? '?' : '&');
                first = false;
                uri.Append(UrlEncoder.Default.Encode(key));
                uri.Append('=');
                uri.Append(UrlEncoder.Default.Encode(value));
            }

            return uri.ToString();
        }
    }
}
