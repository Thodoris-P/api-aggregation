using System.Net;
using System.Text;
using System.Text.Json;
using ApiAggregation.Aggregation;
using ApiAggregation.Aggregation.Abstractions;
using ApiAggregation.Aggregation.Models;
using ApiAggregation.Aggregation.Services;
using ApiAggregation.Authentication;
using ApiAggregation.Authentication.Abstractions;
using ApiAggregation.Authentication.Models;
using ApiAggregation.Authentication.Services;
using ApiAggregation.Configuration;
using ApiAggregation.ExternalApis;
using ApiAggregation.ExternalApis.Abstractions;
using ApiAggregation.ExternalApis.ConcreteClients;
using ApiAggregation.ExternalApis.Decorators;
using ApiAggregation.ExternalApis.Models;
using ApiAggregation.ExternalApis.Services;
using ApiAggregation.Infrastructure.Abstractions;
using ApiAggregation.Infrastructure.Providers;
using ApiAggregation.Statistics;
using ApiAggregation.Statistics.Abstractions;
using ApiAggregation.Statistics.Handlers;
using ApiAggregation.Statistics.Middleware;
using ApiAggregation.Statistics.Models;
using ApiAggregation.Statistics.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.IdentityModel.Tokens;
using Polly;
using Polly.Fallback;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure JWT Authentication
byte[] key = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!);
builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        // In production ensure HTTPS metadata is required.
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddSingleton<IAccountService, InMemoryAccountService>();


builder.Services.AddAuthorization();

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


builder.Services.Configure<StatisticsThresholds>(builder.Configuration.GetSection("StatisticsThresholds"));
builder.Services.AddSingleton<IStatisticsService, StatisticsService>();
builder.Services.AddTransient<StatisticsHandler>();

builder.Services.Configure<SpotifyTokenSettings>(builder.Configuration.GetSection("Spotify-Token"));
builder.Services.AddScoped<ISpotifyTokenService, SpotifyTokenService>();


builder.Services.Configure<OpenWeatherMapSettings>(builder.Configuration.GetSection("OpenWeatherMap"));
builder.Services.AddHttpClient("OpenWeatherMap", client =>
    {
        var settings = builder.Configuration.GetSection("OpenWeatherMap").Get<OpenWeatherMapSettings>();
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

            var fallbackStrategyOptions = new FallbackStrategyOptions<HttpResponseMessage>()
            {
                FallbackAction = _ =>
                {
                    Log.Logger.Error("External API request failed. Resorting to fallback value");
                    return Outcome.FromResultAsValueTask(new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Headers = { { "X-Fallback-Response", "true" } },
                        Content = new StringContent(
                            JsonSerializer.Serialize(new ApiResponse
                            {
                                Content = "The service is currently unavailable. Please try again later.",
                                IsFallback = true,
                                IsSuccess = false
                            }),
                            Encoding.UTF8,
                            "application/json"
                        )
                    });
                }
            };
            builder.AddFallback(fallbackStrategyOptions);

            // See: https://www.pollydocs.org/strategies/timeout.html
            builder.AddTimeout(TimeSpan.FromSeconds(5));
        });

builder.Services.Configure<NewsApiSettings>(builder.Configuration.GetSection("NewsApi"));
builder.Services.AddHttpClient("NewsApi", client =>
    {
        var settings = builder.Configuration.GetSection("NewsApi").Get<NewsApiSettings>();
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

            var fallbackStrategyOptions = new FallbackStrategyOptions<HttpResponseMessage>()
            {
                FallbackAction = _ =>
                {
                    Log.Logger.Error("External API request failed. Resorting to fallback value");
                    return Outcome.FromResultAsValueTask(new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Headers = { { "X-Fallback-Response", "true" } },
                        Content = new StringContent(
                            JsonSerializer.Serialize(new ApiResponse
                            {
                                Content = "The service is currently unavailable. Please try again later.",
                                IsFallback = true,
                                IsSuccess = false
                            }),
                            Encoding.UTF8,
                            "application/json"
                        )
                    });
                }
            };
            builder.AddFallback(fallbackStrategyOptions);

            // See: https://www.pollydocs.org/strategies/timeout.html
            builder.AddTimeout(TimeSpan.FromSeconds(5));
        });

builder.Services.Configure<SpotifySettings>(builder.Configuration.GetSection("Spotify"));
builder.Services.AddHttpClient("Spotify", client =>
    {
        var settings = builder.Configuration.GetSection("Spotify").Get<SpotifySettings>();
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

            var fallbackStrategyOptions = new FallbackStrategyOptions<HttpResponseMessage>()
            {
                FallbackAction = _ =>
                {
                    Log.Logger.Error("External API request failed. Resorting to fallback value");
                    return Outcome.FromResultAsValueTask(new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Headers = { { "X-Fallback-Response", "true" } },
                        Content = new StringContent(
                            JsonSerializer.Serialize(new ApiResponse
                            {
                                Content = "The service is currently unavailable. Please try again later.",
                                IsFallback = true,
                                IsSuccess = false
                            }),
                            Encoding.UTF8,
                            "application/json"
                        )
                    });
                }
            };
            builder.AddFallback(fallbackStrategyOptions);

            // See: https://www.pollydocs.org/strategies/timeout.html
            builder.AddTimeout(TimeSpan.FromSeconds(5));
        });

builder.Services.AddScoped<IExternalApiClient, OpenWeatherMapClient>();
builder.Services.AddScoped<IExternalApiClient, NewsClient>();
builder.Services.AddScoped<IExternalApiClient, SpotifyClient>();

builder.Services.Decorate<IExternalApiClient>((inner, sp) =>
    new CachingExternalApiClientDecorator(
        inner,
        sp.GetRequiredService<HybridCache>()
    )
);

builder.Services.Configure<AggregatorSettings>(builder.Configuration.GetSection("AggregatorSettings"));
builder.Services.AddScoped<IAggregatorService, AggregatorService>();
builder.Services.Configure<StatisticsCleanupOptions>(builder.Configuration.GetSection("StatisticsCleanupOptions"));
builder.Services.AddHostedService<StatisticsCleanupService>();
// Bind PerformanceMonitoringOptions from your configuration (e.g., appsettings.json).
builder.Services.Configure<PerformanceMonitoringOptions>(builder.Configuration.GetSection("PerformanceMonitoringOptions"));
builder.Services.AddHostedService<PerformanceMonitoringService>();
builder.Services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseMiddleware<RequestStatisticsMiddleware>();

// Last resort error handling middleware
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Unhandled exception processing request");
        context.Response.StatusCode = 500;
        await context.Response.WriteAsJsonAsync(new { error = "An unexpected error occurred." });
    }
});


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await app.RunAsync();