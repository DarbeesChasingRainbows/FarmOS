using FarmOS.Apiary.Domain;
using FarmOS.SharedKernel;

namespace FarmOS.Apiary.Infrastructure;

/// <summary>
/// Interface for retrieving current weather conditions at a given position.
/// Used to enrich inspection events with weather context.
/// </summary>
public interface IWeatherService
{
    Task<WeatherSnapshot?> GetCurrentWeatherAsync(GeoPosition position, CancellationToken ct);
}

/// <summary>
/// Placeholder weather service implementation.
/// Replace with real API integration (OpenWeatherMap, WeatherAPI, etc.)
/// when API key is configured.
/// </summary>
public sealed class NoOpWeatherService : IWeatherService
{
    public Task<WeatherSnapshot?> GetCurrentWeatherAsync(GeoPosition position, CancellationToken ct) =>
        Task.FromResult<WeatherSnapshot?>(null);
}
