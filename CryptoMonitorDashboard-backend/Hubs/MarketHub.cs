using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CryptoMonitor.API.Hubs
{
    /// <summary>
    /// Hub de tempo real para preços e alertas do mercado.
    /// Eventos enviados pelo servidor (server → client):
    ///  - "pricesUpdated"  : List&lt;PriceTickDto&gt;
    ///  - "alertTriggered" : AlertTriggeredDto
    ///
    /// Métodos que o client pode chamar (client → server):
    ///  - <c>Ping()</c>                 → teste de conectividade
    ///  - <c>JoinGroup(groupName)</c>   → assinar um grupo (ex.: "favorites:USER123")
    ///  - <c>LeaveGroup(groupName)</c>  → sair de um grupo
    /// </summary>
    [AllowAnonymous]
    public sealed class MarketHub : Hub
    {
        /// <summary>
        /// Retorna "pong" para testar a conexão do cliente.
        /// </summary>
        public Task<string> Ping() => Task.FromResult("pong");

        /// <summary>
        /// Adiciona a conexão atual a um grupo lógico (ex.: favoritos do usuário).
        /// </summary>
        public async Task JoinGroup(string groupName)
        {
            if (string.IsNullOrWhiteSpace(groupName)) return;
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }

        /// <summary>
        /// Remove a conexão atual de um grupo lógico.
        /// </summary>
        public async Task LeaveGroup(string groupName)
        {
            if (string.IsNullOrWhiteSpace(groupName)) return;
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        }        

        /// <summary>
        /// Nome padrão de grupo para favoritos de um usuário (se for usar auth/claims).
        /// </summary>
        public static string FavoritesGroup(string userId) => $"favorites:{userId}";
    }
}
