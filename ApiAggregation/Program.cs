using System.Net;
using System.Text;
using System.Text.Json;
using ApiAggregation.Infrastructure;
using ApiAggregation.Services;
using ApiAggregation.Services.Abstractions;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Fallback;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog from appsettings.json
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

// Replace default logging with Serilog
builder.Host.UseSerilog();

builder.Services.AddHybridCache();

builder.Services.AddControllers();

builder.Services.AddOpenApi();

builder.Services.AddSingleton<IStatisticsSevice, StatisticsSevice>();
builder.Services.AddTransient<StatisticsHandler>();

builder.Services.Configure<OpenWeatherMapSettings>(builder.Configuration.GetSection("OpenWeatherMap"));

builder.Services.AddHttpClient<IExternalApiClient, OpenWeatherMapClient>((serviceProvider, client) =>
    {
        var settings = serviceProvider
            .GetRequiredService<IOptions<OpenWeatherMapSettings>>().Value;

        client.BaseAddress = new Uri(settings.BaseUrl);
    })
    .AddHttpMessageHandler<StatisticsHandler>()
    .AddResilienceHandler(
        "CustomPipeline",
        static builder =>
        {
            // See: https://www.pollydocs.org/strategies/retry.html
            builder.AddRetry(new HttpRetryStrategyOptions
            {
                BackoffType = DelayBackoffType.Exponential,
                MaxRetryAttempts = 5,
                UseJitter = true
            });

            // See: https://www.pollydocs.org/strategies/circuit-breaker.html
            builder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
            {
                // Customize and configure the circuit breaker logic.
                SamplingDuration = TimeSpan.FromSeconds(10),
                FailureRatio = 0.2,
                MinimumThroughput = 3,
                ShouldHandle = static args => ValueTask.FromResult(args is
                {
                    Outcome.Result.StatusCode:
                    HttpStatusCode.RequestTimeout or
                    HttpStatusCode.TooManyRequests
                })
            });

            builder.AddConcurrencyLimiter(100);

            builder.AddFallback(new FallbackStrategyOptions<HttpResponseMessage>()
            {
                FallbackAction = _ => Outcome.FromResultAsValueTask(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(new ApiResponse
                        {
                            Content = "The service is currently unavailable. Please try again later."
                        }),
                        Encoding.UTF8,
                        "application/json"
                    )
                })
            });

            // See: https://www.pollydocs.org/strategies/timeout.html
            builder.AddTimeout(TimeSpan.FromSeconds(5));
        });

builder.Services.Decorate<IExternalApiClient>((inner, sp) =>
    new CachingExternalApiClientDecorator(
        inner,
        sp.GetRequiredService<HybridCache>(),
        TimeSpan.FromMinutes(1) //TODO: Abstract cache duration. An option is to use OpenWeatherMapSettings
    )
);


builder.Services.AddScoped<IAggregatorService, AggregatorService>();


var app = builder.Build();

app.UseSerilogRequestLogging();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

await app.RunAsync();
