using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using CryptoMonitor.Core.DTOs;
using CryptoMonitor.Core.Interfaces;

namespace CryptoMonitor.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces(MediaTypeNames.Application.Json)]
    public sealed class StatisticsController(ICryptoService crypto) : ControllerBase
    {
        private readonly ICryptoService _crypto = crypto;

        /// <summary>
        /// Visão global do mercado (market cap total, volume, dominância BTC/ETH, variação 24h do market cap, etc.).
        /// Fonte: CoinGecko /global
        /// </summary>
        [HttpGet("overview")]
        [ProducesResponseType(typeof(GlobalStatsDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetOverview(CancellationToken ct)
        {
            var dto = await _crypto.GetGlobalAsync(ct);
            return Ok(dto);
        }

        /// <summary>
        /// Top ganhadores e perdedores em 24h (ordenado por variação 24h).
        /// </summary>
        [HttpGet("top-movers")]
        [ProducesResponseType(typeof(TopMoversDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTopMovers(
            [FromQuery] int n = 10,
            [FromQuery] string vs = "usd",
            CancellationToken ct = default)
        {
            if (n < 1) n = 1;
            if (n > 50) n = 50;

            vs = (vs ?? "usd").Trim().ToLowerInvariant();
            if (vs != "usd" && vs != "brl")
                return BadRequest(new { message = "Parâmetro 'vs' inválido. Use 'usd' ou 'brl'." });

            var dto = await _crypto.GetTopMoversAsync(n, vs, ct);
            return Ok(dto);
        }

        /// <summary>
        /// Resumo de mercado com contagem de altas/baixas, média de variação 24h e soma do market cap.
        /// </summary>
        [HttpGet("summary")]
        [ProducesResponseType(typeof(MarketSummaryDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSummary(
            [FromQuery] string vs = "usd",
            CancellationToken ct = default)
        {
            vs = (vs ?? "usd").Trim().ToLowerInvariant();
            if (vs != "usd" && vs != "brl")
                return BadRequest(new { message = "Parâmetro 'vs' inválido. Use 'usd' ou 'brl'." });

            var dto = await _crypto.GetMarketSummaryAsync(vs, ct);
            return Ok(dto);
        }
    }
}
