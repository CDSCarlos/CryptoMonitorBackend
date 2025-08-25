namespace CryptoMonitor.Core.Models
{
    /// <summary>
    /// Entidade de domínio para uma criptomoeda (snapshot agregado para o app).
    /// Não depende do formato do provedor externo.
    /// </summary>
    public sealed class Cryptocurrency
    {
        public string Id { get; init; } = default!;
        public string Symbol { get; init; } = default!;
        public string Name { get; init; } = default!;
        public string? Logo { get; init; }
        public int? MarketCapRank { get; init; }
        
        public decimal? PriceUsd { get; init; }
        public decimal? PriceBrl { get; init; }
        public decimal? Change24hUsd { get; init; }
        public decimal? Change24hBrl { get; init; }

        public decimal? MarketCapUsd { get; init; }
        public decimal? MarketCapBrl { get; init; }
    }
}
