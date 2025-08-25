using System.ComponentModel.DataAnnotations;

namespace CryptoMonitor.Infrastructure.Options
{
    public sealed class BackgroundJobsOptions
    {
        /// <summary>Intervalo (segundos) para atualizar o cache de mercado.</summary>
        [Range(5, 600)]
        public int DataUpdateSeconds { get; set; } = 30;

        /// <summary>Intervalo (segundos) para checar/processar alertas.</summary>
        [Range(5, 600)]
        public int AlertCheckSeconds { get; set; } = 15;
    }
}