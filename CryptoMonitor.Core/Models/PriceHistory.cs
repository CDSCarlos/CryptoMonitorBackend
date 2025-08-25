namespace CryptoMonitor.Core.Models
{
    /// <summary>
    /// Histórico de preços (time series) para uma moeda e um par (vs).
    /// Mantém retenção e fornece ranges (ex.: 7d, 30d) para sparkline e gráficos.
    /// </summary>
    public sealed class PriceHistory(string coinId, string vs, TimeSpan? retention = null)
    {
        public string CoinId { get; } = coinId;
        public string Vs { get; } = vs.ToLowerInvariant();
        public TimeSpan Retention { get; } = retention ?? TimeSpan.FromDays(30);
        
        private readonly List<PricePoint> _points = new(1024);

        public void Add(decimal price, DateTime utcNow)
        {
            _points.Add(new PricePoint(utcNow, price));
            Prune(utcNow);
        }

        /// <summary>Retorna pontos com timestamp >= fromUtc.</summary>
        public IReadOnlyList<PricePoint> RangeFrom(DateTime fromUtc)
            => [.. _points.Where(p => p.TimestampUtc >= fromUtc)];

        /// <summary>Retorna preços dos últimos 7 dias (útil para sparkline).</summary>
        public IReadOnlyList<decimal> Sparkline7d()
            => [.. RangeFrom(DateTime.UtcNow - TimeSpan.FromDays(7)).Select(p => p.Price)];

        private void Prune(DateTime utcNow)
        {
            var cutoff = utcNow - Retention;
            if (_points.Count == 0 || _points[0].TimestampUtc >= cutoff) return;
            
            var idx = _points.FindIndex(p => p.TimestampUtc >= cutoff);
            if (idx > 0) _points.RemoveRange(0, idx);
        }
    }

    public readonly record struct PricePoint(DateTime TimestampUtc, decimal Price);
}
