using System.Net;
using System.Text;
using System.Text.Json;
using ApiAggregation.Aggregation;
using ApiAggregation.Authentication;
using ApiAggregation.ExternalApis;
using ApiAggregation.Statistics;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Polly;
using Polly.Fallback;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure JWT Authentication
var key = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!);
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
            });

            // See: https://www.pollydocs.org/strategies/timeout.html
            builder.AddTimeout(TimeSpan.FromSeconds(5));
        });

builder.Services.Decorate<IExternalApiClient>((inner, sp) =>
    new CachingExternalApiClientDecorator(
        inner,
        sp.GetRequiredService<HybridCache>(),
        sp.GetRequiredService<IOptions<OpenWeatherMapSettings>>().Value.CacheDuration
    )
);


builder.Services.AddScoped<IAggregatorService, AggregatorService>();


var app = builder.Build();

app.UseSerilogRequestLogging();

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
