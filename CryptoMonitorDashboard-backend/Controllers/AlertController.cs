using System.Net.Mime;
using CryptoMonitor.Core.DTOs;
using CryptoMonitor.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CryptoMonitor.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces(MediaTypeNames.Application.Json)]
    public sealed class AlertsController(IAlertService alerts) : ControllerBase
    {
        private readonly IAlertService _alerts = alerts;

        /// <summary>
        /// Cria um novo alerta de preço para uma criptomoeda.
        /// </summary>
        /// <remarks>
        /// Exemplo:
        /// {
        ///   "coinId": "bitcoin",
        ///   "vs": "usd",
        ///   "targetPrice": 65000,
        ///   "direction": "above",
        ///   "label": "BTC acima de 65k"
        /// }
        /// </remarks>
        [HttpPost]
        [ProducesResponseType(typeof(AlertDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] AlertCreateDto body, CancellationToken ct)
        {            
            if (body is null)
                return BadRequest(new { message = "Body inválido." });

            if (string.IsNullOrWhiteSpace(body.CoinId))
                return BadRequest(new { message = "CoinId é obrigatório." });

            var vs = (body.Vs ?? "usd").Trim().ToLowerInvariant();
            if (vs is not "usd" and not "brl")
                return BadRequest(new { message = "Parâmetro 'vs' inválido. Use 'usd' ou 'brl'." });

            var dir = (body.Direction ?? "above").Trim().ToLowerInvariant();
            if (dir is not "above" and not "below")
                return BadRequest(new { message = "Parâmetro 'direction' inválido. Use 'above' ou 'below'." });

            if (body.TargetPrice <= 0)
                return BadRequest(new { message = "TargetPrice deve ser maior que zero." });
            
            body.CoinId = body.CoinId.Trim().ToLowerInvariant();
            body.Vs = vs;
            body.Direction = dir;

            var created = await _alerts.CreateAsync(body, ct);
            
            return CreatedAtAction(nameof(List), new { id = created.Id }, created);
        }

        /// <summary>
        /// Lista todos os alertas atuais (in-memory).
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<AlertDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> List(CancellationToken ct)
        {
            var list = await _alerts.ListAsync(ct);
            return Ok(list);
        }

        /// <summary>
        /// Remove um alerta pelo identificador.
        /// </summary>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var ok = await _alerts.DeleteAsync(id, ct);
            return ok ? NoContent() : NotFound(new { message = "Alerta não encontrado." });
        }
    }
}
