using ApiAggregation.Infrastructure;
using ApiAggregation.Services;
using ApiAggregation.Services.Abstractions;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Load configuration
// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
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
    .AddHttpMessageHandler<StatisticsHandler>();
builder.Services.AddScoped<IAggregatorService, AggregatorService>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

await app.RunAsync();