using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace CryptoMonitor.Infrastructure.Middleware
{
    public sealed class RateLimitMiddleware(RequestDelegate next, ILogger<RateLimitMiddleware> logger, IMemoryCache cache)
    {
        private readonly RequestDelegate _next = next;
        private readonly ILogger<RateLimitMiddleware> _logger = logger;
        private readonly IMemoryCache _cache = cache;

        private readonly int _limit = 60;
        private readonly TimeSpan _window = TimeSpan.FromMinutes(1);
        
        private static readonly string[] _bypassPrefixes =
        [
            "/swagger",
            "/hubs",
            "/health",
            "/favicon.ico"
        ];

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value ?? string.Empty;
            
            foreach (var prefix in _bypassPrefixes)
            {
                if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    await _next(context);
                    return;
                }
            }
            
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var key = $"ratelimit:{ip}";

            var entry = _cache.GetOrCreate(key, e =>
            {
                e.AbsoluteExpirationRelativeToNow = _window;
                return new Counter { Count = 0 };
            });

            entry!.Count++;

            if (entry.Count > _limit)
            {
                _logger.LogWarning("Rate limit exceeded for {IP}", ip);
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                
                context.Response.Headers["Retry-After"] = ((int)_window.TotalSeconds).ToString();
                await context.Response.WriteAsync("Too many requests. Try again later.");
                return;
            }

            await _next(context);
        }

        private sealed class Counter
        {
            public int Count { get; set; }
        }
    }

    public static class RateLimitMiddlewareExtensions
    {
        public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder app)
            => app.UseMiddleware<RateLimitMiddleware>();
    }
}
