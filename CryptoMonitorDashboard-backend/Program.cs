using CryptoMonitor.API.Hubs;
using CryptoMonitor.API.Services;
using CryptoMonitor.Core.Interfaces;
using CryptoMonitor.Infrastructure.BackgroundServices;
using CryptoMonitor.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Polly;
using Polly.Extensions.Http;
using System.Reflection;
using CryptoMonitor.Infrastructure.Middleware;
using CryptoMonitor.Infrastructure.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<CoinGeckoOptions>()
    .Bind(builder.Configuration.GetSection("CoinGecko"))
    .ValidateDataAnnotations();

builder.Services.AddOptions<BackgroundJobsOptions>()
    .Bind(builder.Configuration.GetSection("BackgroundJobs"))
    .ValidateDataAnnotations();

builder.Services.AddSingleton<IRealtimeNotifier, SignalRRealtimeNotifier>();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ICacheService, CacheService>();
builder.Services.AddSingleton<IAlertService, AlertService>();
builder.Services.AddSingleton<ICryptoService, CryptoService>();
builder.Services.AddSingleton<IPriceHistoryStore, InMemoryPriceHistoryStore>();
builder.Services.AddHostedService<DataUpdateService>();
builder.Services.AddHostedService<AlertMonitorService>();
builder.Services.AddHttpClient<IExternalApiService, ExternalApiService>((sp, client) =>
{
    var opts = sp.GetRequiredService<IOptions<CoinGeckoOptions>>().Value;
    client.BaseAddress = new Uri(opts.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(opts.DefaultTimeoutSeconds);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("CryptoMonitor/1.0 (+github.com/yourrepo)");
})
.AddPolicyHandler(GetPolicyWrap());

builder.Services.AddSignalR();

builder.Services.AddCors(opt =>
{
    opt.AddPolicy("frontend", p => p
        .WithOrigins(
            "http://localhost:4200"
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

builder.Services.AddControllers();

static IAsyncPolicy<HttpResponseMessage> GetPolicyWrap()
{
    var retry = HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => (int)msg.StatusCode == 429)
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: i => TimeSpan.FromMilliseconds(300 * i)
        );

    var breaker = HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => (int)msg.StatusCode == 429)
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 8,
            durationOfBreak: TimeSpan.FromSeconds(20)
        );
    
    return Policy.WrapAsync(breaker, retry);
}

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CryptoMonitor API",
        Version = "v1",
        Description = "Backend para monitoramento de criptomoedas com SignalR e serviços de background."
    });
    
    var xmlName = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlName);
    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    
    c.CustomSchemaIds(t => t.FullName);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "CryptoMonitor API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseErrorHandling();
app.UseRateLimiting();

app.UseCors("frontend");
app.MapControllers();
app.MapHub<MarketHub>("/hubs/market");

app.Run();
