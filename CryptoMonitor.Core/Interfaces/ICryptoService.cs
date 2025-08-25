using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CryptoMonitor.Core.DTOs;

namespace CryptoMonitor.Core.Interfaces
{
    /// <summary>
    /// Serviço responsável por consultar dados de criptomoedas
    /// (CoinGecko API) e fornecer estatísticas globais e de mercado.
    /// </summary>
    public interface ICryptoService
    {
        /// <summary>
        /// Lista mercados com paginação/filtros.
        /// </summary>
        Task<PagedResult<CryptoResponseDto>> GetMarketsAsync(
            MarketsQuery query,
            CancellationToken ct = default);

        /// <summary>
        /// Detalhe por id (id do CoinGecko) na moeda informada (usd|brl).
        /// </summary>
        Task<CryptoResponseDto?> GetByIdAsync(
            string id,
            string vs = "usd",
            CancellationToken ct = default);

        /// <summary>
        /// Top N por market cap na moeda informada (usd|brl).
        /// </summary>
        Task<IReadOnlyList<CryptoResponseDto>> GetTopAsync(
            int n,
            string vs = "usd",
            CancellationToken ct = default);

        /// <summary>
        /// Retorna estatísticas globais do mercado de criptomoedas,
        /// incluindo market cap total, volume, dominância BTC/ETH e
        /// variação de market cap em 24h.
        /// </summary>
        Task<GlobalStatsDto> GetGlobalAsync(CancellationToken ct);

        /// <summary>
        /// Retorna as principais moedas em alta (gainers) e em queda (losers)
        /// no período de 24h, ordenadas pela variação percentual.
        /// </summary>
        /// <param name="n">Quantidade de moedas a retornar em cada grupo (gainers/losers).</param>
        /// <param name="vs">Moeda de referência (usd ou brl).</param>
        Task<TopMoversDto> GetTopMoversAsync(int n, string vs, CancellationToken ct);

        /// <summary>
        /// Retorna um resumo geral do mercado, incluindo quantidade de ativos,
        /// quantos subiram/caíram em 24h, média de variação e soma do market cap.
        /// </summary>
        /// <param name="vs">Moeda de referência (usd ou brl).</param>
        Task<MarketSummaryDto> GetMarketSummaryAsync(string vs, CancellationToken ct);

        /// <summary>
        /// Obtém o histórico **diário** de preços de uma criptomoeda por até <paramref name="days"/> dias,
        /// consultando a fonte externa (CoinGecko /market_chart com interval=daily) e retornando uma série
        /// reduzida para no máximo <paramref name="points"/> amostras (downsampling linear, se necessário).
        /// </summary>
        /// <param name="id">Identificador do ativo no CoinGecko (ex.: <c>"bitcoin"</c>).</param>
        /// <param name="vs">Moeda de cotação. Valores aceitos: <c>"usd"</c> ou <c>"brl"</c>.</param>
        /// <param name="days">Janela de tempo em dias (faixa recomendada: 1..365).</param>
        /// <param name="points">
        /// Número máximo de pontos a retornar (faixa recomendada: 1..1000).
        /// Se a série possuir mais pontos, aplica-se downsampling linear para limitar o tamanho.
        /// </param>
        /// <param name="ct">Token de cancelamento da operação.</param>
        /// <returns>
        /// Um <see cref="PriceHistoryDto"/> contendo metadados do intervalo (de/até) e a lista de amostras
        /// (timestamps em milissegundos desde a epoch e preço).
        /// </returns>
        /// <remarks>
        /// A implementação pode aplicar cache e políticas de repetição/backoff para mitigar limites de taxa (429).
        /// </remarks>
        Task<PriceHistoryDto> GetHistoryDailyAsync(
        string id, string vs, int days, int points, CancellationToken ct = default);

    }
}
