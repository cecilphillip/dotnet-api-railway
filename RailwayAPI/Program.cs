using System.Threading.RateLimiting;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Serialization.SystemTextJson;

var builder = WebApplication.CreateBuilder(args);

var redisConnection = builder.Configuration.GetValue<string>("REDIS_CONNECTION") ?? string.Empty;

builder.Services.AddFusionCache()
    .WithDefaultEntryOptions(new FusionCacheEntryOptions
    {
        Duration = TimeSpan.FromSeconds(10)
    })
    .WithSerializer(new FusionCacheSystemTextJsonSerializer())
    .WithDistributedCache(new RedisCache(new RedisCacheOptions { Configuration = redisConnection }));

builder.Services.AddHealthChecks()
    .AddRedis(redisConnection);;

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.Logger.LogInformation("This is the REDIS_CONNECTION {RedisConnection}", redisConnection);

app.UseSwagger();
app.UseSwaggerUI();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

WeatherForecast[] GetWeatherForecast() => Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
    .ToArray();


app.MapHealthChecks("/health");

app.MapGet("/weatherforecast", (IFusionCache cache) =>
    {
        var forecast = cache.GetOrSet("products", _ => GetWeatherForecast());

        return Results.Ok(forecast);
    })
    .WithName("GetWeatherForecast")
    .WithOpenApi();

app.Run();


record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}