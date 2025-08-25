namespace CryptoMonitor.Core.Models
{
    /// <summary>
    /// Alerta de preço do usuário (domínio). Pode ser mapeado de/para DTOs.
    /// </summary>
    public sealed class Alert
    {
        public Guid Id { get; init; } = Guid.NewGuid();

        public string CoinId { get; init; } = default!;
        public string Vs { get; init; } = "usd";
        public decimal TargetPrice { get; init; }
        public string Direction { get; init; } = "above";
        public string? Label { get; init; }

        public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
        public bool IsTriggered { get; private set; }
        public DateTime? TriggeredAtUtc { get; private set; }

        /// <summary>Retorna true se o preço atual "atinge" a condição do alerta.</summary>
        public bool IsHit(decimal currentPrice) =>
            Direction.Equals("above", StringComparison.OrdinalIgnoreCase)
                ? currentPrice >= TargetPrice
                : currentPrice <= TargetPrice;

        /// <summary>Marca como disparado.</summary>
        public void MarkTriggered(DateTime utcNow)
        {
            IsTriggered = true;
            TriggeredAtUtc = utcNow;
        }
    }
}
