using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using CryptoMonitor.Core.DTOs;
using CryptoMonitor.Core.Interfaces;

namespace CryptoMonitor.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces(MediaTypeNames.Application.Json)]
    public class CryptocurrenciesController(ICryptoService cryptoService, IPriceHistoryStore? priceHistoryStore) : ControllerBase
    {
        private readonly ICryptoService _cryptoService = cryptoService;
        private readonly IPriceHistoryStore? _priceHistoryStore = priceHistoryStore;

        /// <summary>
        /// Lista criptomoedas com paginação e filtros.
        /// </summary>
        /// <param name="vs">Moeda de cotação (usd ou brl). Padrão: usd.</param>
        /// <param name="page">Página (>=1). Padrão: 1.</param>
        /// <param name="pageSize">Tamanho da página (1..250). Padrão: 50.</param>
        /// <param name="search">Texto para busca por nome/símbolo (opcional).</param>
        /// <param name="ids">Lista de ids (CoinGecko) separada por vírgula (opcional).</param>
        /// <param name="orderBy">Ordenação (ex: market_cap_desc, volume_desc). Padrão: market_cap_desc.</param>
        /// <param name="ct"></param>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<CryptoResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAsync(
            [FromQuery] string vs = "usd",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string? search = null,
            [FromQuery] string? ids = null,
            [FromQuery] string orderBy = "market_cap_desc",
            CancellationToken ct = default)
        {
            vs = NormalizeVs(vs);
            if (!IsSupportedVs(vs))
                return BadRequest(new { message = "Parâmetro 'vs' inválido. Use 'usd' ou 'brl'." });

            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 1;
            if (pageSize > 250) pageSize = 250;

            var result = await _cryptoService.GetMarketsAsync(new MarketsQuery
            {
                VsCurrency = vs,
                Page = page,
                PageSize = pageSize,
                Search = search,
                Ids = ids,
                OrderBy = orderBy
            }, ct);

            return Ok(result);
        }

        /// <summary>
        /// Obtém detalhes de uma criptomoeda por id (CoinGecko).
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(CryptoResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByIdAsync(
            [FromRoute] string id,
            [FromQuery] string vs = "usd",
            CancellationToken ct = default)
        {
            vs = NormalizeVs(vs);
            if (!IsSupportedVs(vs))
                return BadRequest(new { message = "Parâmetro 'vs' inválido. Use 'usd' ou 'brl'." });

            var coin = await _cryptoService.GetByIdAsync(id, vs, ct);
            if (coin is null)
                return NotFound(new { message = $"Criptomoeda '{id}' não encontrada." });

            return Ok(coin);
        }

        /// <summary>
        /// Retorna o Top N por market cap.
        /// </summary>
        [HttpGet("top/{n:int}")]
        [ProducesResponseType(typeof(IEnumerable<CryptoResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTopAsync(
            [FromRoute] int n,
            [FromQuery] string vs = "usd",
            CancellationToken ct = default)
        {
            if (n < 1) n = 1;
            if (n > 250) n = 250;

            vs = NormalizeVs(vs);
            if (!IsSupportedVs(vs))
                return BadRequest(new { message = "Parâmetro 'vs' inválido. Use 'usd' ou 'brl'." });

            var list = await _cryptoService.GetTopAsync(n, vs, ct);
            return Ok(list);
        }

        /// <summary>Histórico de preços (até N dias) com downsampling opcional.</summary>
        [HttpGet("{id}/history")]
        [ProducesResponseType(typeof(PriceHistoryDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetHistory(
            [FromRoute] string id,
            [FromQuery] string vs = "usd",
            [FromQuery] int days = 30,
            [FromQuery] int points = 200,
            CancellationToken ct = default)
        {
            vs = NormalizeVs(vs);
            if (!IsSupportedVs(vs))
                return BadRequest(new { message = "Parâmetro 'vs' inválido. Use 'usd' ou 'brl'." });

            days = Math.Clamp(days, 1, 365);
            points = Math.Clamp(points, 1, 1000);
            
            if (days >= 3 || _priceHistoryStore is null)
            {
                var dto = await _cryptoService.GetHistoryDailyAsync(id, vs, days, points, ct);
                return Ok(dto);
            }
            
            var toUtc = DateTime.UtcNow;
            var fromUtc = toUtc - TimeSpan.FromDays(days);
            var series = _priceHistoryStore!.GetOrCreate(id, vs).RangeFrom(fromUtc);

            var samples = series.Select(p => new PriceSampleDto
            {
                T = new DateTimeOffset(p.TimestampUtc).ToUnixTimeMilliseconds(),
                P = p.Price
            }).ToList();

            if (samples.Count > points)
                samples = DownsampleLinear(samples, points);

            var dtoLocal = new PriceHistoryDto
            {
                CoinId = id,
                Vs = vs,
                FromUtc = fromUtc,
                ToUtc = toUtc,
                Samples = samples
            };
            return Ok(dtoLocal);
        }

        /// <summary>
        /// Snapshot inicial para o dashboard: retorna linhas no formato PriceTickDto
        /// já com USD/BRL + sparkline 7d (se houver IPriceHistoryStore).
        /// </summary>
        [HttpGet("snapshot")]
        [ProducesResponseType(typeof(IEnumerable<PriceTickDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSnapshot(
            [FromQuery] int n = 50,
            [FromQuery] int points = 50,
            CancellationToken ct = default)
        {
            if (n < 1) n = 1;
            if (n > 250) n = 250;
            points = Math.Clamp(points, 1, 500);

            var usd = await _cryptoService.GetMarketsAsync(
                new MarketsQuery { VsCurrency = "usd", Page = 1, PageSize = n, OrderBy = "market_cap_desc" }, ct);

            var brl = await _cryptoService.GetMarketsAsync(
                new MarketsQuery { VsCurrency = "brl", Page = 1, PageSize = n, OrderBy = "market_cap_desc" }, ct);

            var mapBrl = brl.Items.ToDictionary(x => x.Id, x => x);
            var list = new List<PriceTickDto>(usd.Items.Count());

            foreach (var u in usd.Items)
            {
                mapBrl.TryGetValue(u.Id, out var b);

                List<decimal>? spark = null;
                if (_priceHistoryStore is not null)
                {
                    var raw = _priceHistoryStore.GetOrCreate(u.Id, "usd").Sparkline7d().ToList();
                    if (raw.Count > 0)
                        spark = raw.Count <= points ? raw : DownsampleLinear(raw, points);
                }

                list.Add(new PriceTickDto
                {
                    Id = u.Id,
                    Symbol = u.Symbol,
                    Name = u.Name,
                    Logo = u.Logo,
                    PriceUsd = u.Price,
                    PriceBrl = b?.Price ?? 0m,
                    Change24hUsd = u.Change24hPct,
                    Change24hBrl = b?.Change24hPct,
                    MarketCapRank = u.MarketCapRank,
                    MarketCap = u.MarketCap,
                    Sparkline7d = spark
                });
            }

            return Ok(list);
        }

        private static List<decimal> DownsampleLinear(List<decimal> src, int target)
        {
            if (src.Count == 0) return [];
            if (target <= 0 || src.Count <= target) return [.. src];

            var res = new List<decimal>(target);
            var step = (src.Count - 1) / (decimal)(target - 1);

            for (int i = 0; i < target; i++)
            {
                var idx = i * step;
                var lo = (int)decimal.Truncate(idx);
                var hi = Math.Min(lo + 1, src.Count - 1);
                var frac = idx - lo;
                var v = src[lo] + (src[hi] - src[lo]) * frac;
                res.Add(v);
            }
            return res;
        }

        private static List<PriceSampleDto> DownsampleLinear(List<PriceSampleDto> src, int target)
        {
            if (target <= 0 || src.Count <= target) return src;

            var res = new List<PriceSampleDto>(target);
            var step = (src.Count - 1) / (decimal)(target - 1);

            for (int i = 0; i < target; i++)
            {
                var idx = i * step;
                var lo = (int)decimal.Truncate(idx);
                var hi = Math.Min(lo + 1, src.Count - 1);
                var frac = (double)(idx - lo);

                var t = (long)Math.Round(src[lo].T + (src[hi].T - src[lo].T) * frac);
                var p = src[lo].P + (src[hi].P - src[lo].P) * (decimal)frac;

                res.Add(new PriceSampleDto { T = t, P = p });
            }
            return res;
        }

        private static string NormalizeVs(string vs) => (vs ?? "usd").Trim().ToLowerInvariant();
        private static bool IsSupportedVs(string vs) => vs == "usd" || vs == "brl";
    }
}
