using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CryptoMonitor.Core.DTOs;

namespace CryptoMonitor.Core.Interfaces
{
    /// <summary>Publicação de eventos em tempo real (ex.: via SignalR).</summary>
    public interface IRealtimeNotifier
    {
        Task BroadcastPricesAsync(IEnumerable<PriceTickDto> ticks, CancellationToken ct);
        Task BroadcastAlertAsync(AlertTriggeredDto alert, CancellationToken ct);
    }
}
