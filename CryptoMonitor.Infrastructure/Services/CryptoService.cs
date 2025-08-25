using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using Polly.CircuitBreaker;

using CryptoMonitor.Core.DTOs;
using CryptoMonitor.Core.Interfaces;
using CryptoMonitor.Infrastructure.External.CoinGecko.Models;
using CryptoMonitor.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace CryptoMonitor.Infrastructure.Services
{
    public sealed class CryptoService(
        IExternalApiService external,
        ICacheService cache,
        IOptions<CoinGeckoOptions> options) : ICryptoService
    {
        private readonly IExternalApiService _external = external;
        private readonly ICacheService _cache = cache;
        private readonly CoinGeckoOptions _options = options.Value;

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public async Task<PagedResult<CryptoResponseDto>> GetMarketsAsync(MarketsQuery query, CancellationToken ct)
        {
            var url = BuildMarketsUrl(query);

            var cacheKey = $"cg:markets:{url}";
            if (_cache.TryGet<PagedResult<CryptoResponseDto>>(cacheKey, out var cached) && cached is not null)
                return cached;

            try
            {
                var data = await _external.GetAsync<List<CoinGeckoMarket>>(url, ct, JsonOpts) ?? [];

                if (!string.IsNullOrWhiteSpace(query.Search))
                {
                    var s = query.Search.Trim();
                    data = [.. data.Where(x =>
                        x.Name.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                        x.Symbol.Contains(s, StringComparison.OrdinalIgnoreCase))];
                }

                var items = data.Select(MapToDto(query.VsCurrency)).ToList();

                var result = new PagedResult<CryptoResponseDto>
                {
                    Page = query.Page,
                    PageSize = query.PageSize,
                    Total = 0,
                    Items = items
                };

                _cache.Set(cacheKey, result, TimeSpan.FromSeconds(_options.CacheSeconds));
                return result;
            }
            catch (BrokenCircuitException<HttpResponseMessage>)
            {
                if (_cache.TryGet<PagedResult<CryptoResponseDto>>(cacheKey, out var stale) && stale is not null)
                    return stale;
                throw;
            }
            catch (HttpRequestException)
            {
                if (_cache.TryGet<PagedResult<CryptoResponseDto>>(cacheKey, out var stale) && stale is not null)
                    return stale;
                throw;
            }
            catch (TaskCanceledException)
            {
                if (_cache.TryGet<PagedResult<CryptoResponseDto>>(cacheKey, out var stale) && stale is not null)
                    return stale;
                throw;
            }
        }

        public async Task<CryptoResponseDto?> GetByIdAsync(string id, string vs, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;

            var url = $"coins/markets?vs_currency={Encode(vs)}&ids={Encode(id)}&price_change_percentage=24h&per_page=1&page=1";
            var cacheKey = $"cg:byid:{url}";
            if (_cache.TryGet<CryptoResponseDto>(cacheKey, out var cached) && cached is not null)
                return cached;

            try
            {
                var list = await _external.GetAsync<List<CoinGeckoMarket>>(url, ct, JsonOpts);
                var coin = list?.FirstOrDefault();
                if (coin is null) return null;

                var dto = MapToDto(vs)(coin);
                _cache.Set(cacheKey, dto, TimeSpan.FromSeconds(_options.CacheSeconds));
                return dto;
            }
            catch (BrokenCircuitException<HttpResponseMessage>)
            {
                if (_cache.TryGet<CryptoResponseDto>(cacheKey, out var stale) && stale is not null)
                    return stale;
                throw;
            }
            catch (HttpRequestException)
            {
                if (_cache.TryGet<CryptoResponseDto>(cacheKey, out var stale) && stale is not null)
                    return stale;
                throw;
            }
            catch (TaskCanceledException)
            {
                if (_cache.TryGet<CryptoResponseDto>(cacheKey, out var stale) && stale is not null)
                    return stale;
                throw;
            }
        }

        public async Task<IReadOnlyList<CryptoResponseDto>> GetTopAsync(int n, string vs, CancellationToken ct)
        {
            var pageSize = Math.Min(Math.Max(n, 1), 250);
            var query = new MarketsQuery
            {
                VsCurrency = vs,
                Page = 1,
                PageSize = pageSize,
                OrderBy = "market_cap_desc"
            };

            var result = await GetMarketsAsync(query, ct);
            return [.. result.Items.Take(n)];
        }

        private static string Encode(string value) => UrlEncoder.Default.Encode(value);

        private static string BuildMarketsUrl(MarketsQuery q)
        {
            var vs = string.IsNullOrWhiteSpace(q.VsCurrency) ? "usd" : q.VsCurrency.Trim().ToLowerInvariant();
            var order = string.IsNullOrWhiteSpace(q.OrderBy) ? "market_cap_desc" : q.OrderBy;
            var ids = string.IsNullOrWhiteSpace(q.Ids) ? null : q.Ids.Trim().ToLowerInvariant();

            var parts = new List<string>
            {
                $"vs_currency={Encode(vs)}",
                $"order={Encode(order)}",
                $"per_page={q.PageSize}",
                $"page={q.Page}",
                "price_change_percentage=24h"
            };
            if (!string.IsNullOrEmpty(ids))
                parts.Add($"ids={Encode(ids)}");

            return "coins/markets?" + string.Join("&", parts);
        }

        private static Func<CoinGeckoMarket, CryptoResponseDto> MapToDto(string vs) =>
            x => new CryptoResponseDto
            {
                Id = x.Id,
                Symbol = x.Symbol,
                Name = x.Name,
                Logo = x.Image,
                Price = x.CurrentPrice,
                MarketCap = x.MarketCap,
                Change24hPct = x.PriceChangePct24h,
                Currency = vs.ToLowerInvariant(),
                MarketCapRank = x.MarketCapRank
            };

        public async Task<GlobalStatsDto> GetGlobalAsync(CancellationToken ct)
        {
            const string url = "global";
            const string cacheKey = "cg:global";

            if (_cache.TryGet<GlobalStatsDto>(cacheKey, out var cached) && cached is not null)
                return cached;

            try
            {
                var payload = await _external.GetAsync<CoinGeckoGlobal>(url, ct, JsonOpts);
                var d = payload?.Data ?? new CoinGeckoGlobalData();

                d.TotalMarketCap.TryGetValue("usd", out var mcUsd);
                d.TotalMarketCap.TryGetValue("brl", out var mcBrl);
                d.TotalVolume.TryGetValue("usd", out var volUsd);
                d.TotalVolume.TryGetValue("brl", out var volBrl);
                d.MarketCapPercentage.TryGetValue("btc", out var btcDom);
                d.MarketCapPercentage.TryGetValue("eth", out var ethDom);

                var dto = new GlobalStatsDto
                {
                    TotalMarketCapUsd = mcUsd,
                    TotalMarketCapBrl = mcBrl,
                    TotalVolumeUsd = volUsd,
                    TotalVolumeBrl = volBrl,
                    MarketCapChangePct24hUsd = d.MarketCapChangePct24hUsd,
                    BtcDominancePct = btcDom,
                    EthDominancePct = ethDom,
                    ActiveCryptocurrencies = d.ActiveCryptocurrencies,
                    OngoingIcos = d.OngoingIcos,
                    EndedIcos = d.EndedIcos,
                    Markets = d.Markets
                };

                _cache.Set(cacheKey, dto, TimeSpan.FromSeconds(_options.CacheSeconds));
                return dto;
            }
            catch (BrokenCircuitException<HttpResponseMessage>)
            {
                if (_cache.TryGet<GlobalStatsDto>(cacheKey, out var stale) && stale is not null)
                    return stale;
                throw;
            }
            catch (HttpRequestException)
            {
                if (_cache.TryGet<GlobalStatsDto>(cacheKey, out var stale) && stale is not null)
                    return stale;
                throw;
            }
            catch (TaskCanceledException)
            {
                if (_cache.TryGet<GlobalStatsDto>(cacheKey, out var stale) && stale is not null)
                    return stale;
                throw;
            }
        }

        public async Task<TopMoversDto> GetTopMoversAsync(int n, string vs, CancellationToken ct)
        {
            var pageSize = Math.Min(Math.Max(n * 4, 50), 250);
            var query = new MarketsQuery
            {
                VsCurrency = vs,
                Page = 1,
                PageSize = pageSize,
                OrderBy = "market_cap_desc"
            };

            var result = await GetMarketsAsync(query, ct);
            var list = result.Items.ToList();

            var gainers = list.Where(x => x.Change24hPct.HasValue)
                              .OrderByDescending(x => x.Change24hPct!.Value)
                              .Take(n)
                              .ToList();

            var losers = list.Where(x => x.Change24hPct.HasValue)
                              .OrderBy(x => x.Change24hPct!.Value)
                              .Take(n)
                              .ToList();

            return new TopMoversDto
            {
                TopGainers = gainers,
                TopLosers = losers
            };
        }

        public async Task<MarketSummaryDto> GetMarketSummaryAsync(string vs, CancellationToken ct)
        {
            var query = new MarketsQuery
            {
                VsCurrency = vs,
                Page = 1,
                PageSize = 250,
                OrderBy = "market_cap_desc"
            };

            var result = await GetMarketsAsync(query, ct);
            var list = result.Items.ToList();

            var advancers = list.Count(x => (x.Change24hPct ?? 0) > 0);
            var decliners = list.Count(x => (x.Change24hPct ?? 0) < 0);
            var avgChange = list.Count > 0 ? list.Average(x => x.Change24hPct ?? 0) : 0;
            var sumMcap = list.Sum(x => x.MarketCap);

            return new MarketSummaryDto
            {
                Count = list.Count,
                Advancers = advancers,
                Decliners = decliners,
                AvgChange24hPct = avgChange,
                SumMarketCap = sumMcap
            };
        }

        public async Task<PriceHistoryDto> GetHistoryDailyAsync(
        string id, string vs, int days, int points, CancellationToken ct = default)
        {
            vs = string.IsNullOrWhiteSpace(vs) ? "usd" : vs.Trim().ToLowerInvariant();
            days = Math.Clamp(days, 1, 365);
            points = Math.Clamp(points, 1, 1000);

            var url = $"coins/{Encode(id)}/market_chart?vs_currency={Encode(vs)}&days={days}&interval=daily";
            var cacheKey = $"cg:hist:{id}:{vs}:{days}:d";

            if (_cache.TryGet<PriceHistoryDto>(cacheKey, out var cached) && cached is not null)
                return cached;

            var chart = await _external.GetAsync<CoinGeckoMarketChart>(url, ct, JsonOpts);

            var samples = chart?.Prices?.Select(p => new PriceSampleDto
            {
                T = (long)p[0],
                P = p[1]
            }).ToList() ?? new List<PriceSampleDto>();

            if (samples.Count > points)
                samples = DownsampleLinear(samples, points);

            var fromUtc = samples.Count > 0
                ? DateTimeOffset.FromUnixTimeMilliseconds(samples[0].T).UtcDateTime
                : DateTime.UtcNow.AddDays(-days);

            var toUtc = samples.Count > 0
                ? DateTimeOffset.FromUnixTimeMilliseconds(samples[^1].T).UtcDateTime
                : DateTime.UtcNow;

            var dto = new PriceHistoryDto
            {
                CoinId = id,
                Vs = vs,
                FromUtc = fromUtc,
                ToUtc = toUtc,
                Samples = samples
            };
            
            _cache.Set(cacheKey, dto, TimeSpan.FromSeconds(_options.CacheSeconds));
            return dto;
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
    }
}
