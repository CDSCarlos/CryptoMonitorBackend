using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using CryptoMonitor.API.Hubs;
using CryptoMonitor.Core.DTOs;
using CryptoMonitor.Core.Interfaces;

namespace CryptoMonitor.API.Services
{
    public sealed class SignalRRealtimeNotifier(IHubContext<MarketHub> hub) : IRealtimeNotifier
    {
        private readonly IHubContext<MarketHub> _hub = hub;

        public Task BroadcastPricesAsync(IEnumerable<PriceTickDto> ticks, CancellationToken ct) =>
            _hub.Clients.All.SendAsync("pricesUpdated", ticks, ct);

        public Task BroadcastAlertAsync(AlertTriggeredDto alert, CancellationToken ct) =>
            _hub.Clients.All.SendAsync("alertTriggered", alert, ct);
    }
}
