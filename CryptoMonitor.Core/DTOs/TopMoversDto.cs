using System.Collections.Generic;

namespace CryptoMonitor.Core.DTOs
{
    public sealed class TopMoversDto
    {
        public IReadOnlyList<CryptoResponseDto> TopGainers { get; set; } = [];
        public IReadOnlyList<CryptoResponseDto> TopLosers { get; set; } = [];
    }
}