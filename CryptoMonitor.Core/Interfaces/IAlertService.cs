using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CryptoMonitor.Core.DTOs;

namespace CryptoMonitor.Core.Interfaces
{
    /// <summary>
    /// Serviço de gerenciamento de alertas (criação, listagem, remoção e processamento).
    /// </summary>
    public interface IAlertService
    {
        Task<AlertDto> CreateAsync(AlertCreateDto dto, CancellationToken ct);
        Task<IReadOnlyList<AlertDto>> ListAsync(CancellationToken ct);
        Task<bool> DeleteAsync(Guid id, CancellationToken ct);

        /// <summary>
        /// Processa alertas com base em snapshots de mercado (preços correntes).
        /// </summary>
        Task ProcessAsync(
            IReadOnlyDictionary<string, decimal> pricesUsd,
            IReadOnlyDictionary<string, decimal> pricesBrl,
            CancellationToken ct);
    }
}
