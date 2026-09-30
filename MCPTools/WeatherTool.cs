using System.Globalization;
using System.Text.Json;
using Anthropic.Helpers.Beta;
using Anthropic.Models.Beta.Messages;

namespace ClaudeDemo.MCPTools;

public static class WeatherTool
{
    public static BetaRunnableTool Create() =>
        new()
        {
            Name = "get_current_weather",
            Definition = new BetaTool
            {
                Name = "get_current_weather",
                Description = "Returns the current weather (temperature, wind, conditions) for a city.",
                InputSchema = new InputSchema
                {
                    Properties = new Dictionary<string, JsonElement>
                    {
                        ["city"] = JsonSerializer.SerializeToElement(
                            new { type = "string", description = "City name, e.g. Paris" }
                        ),
                    },
                    Required = ["city"],
                },
            },
            Run = async (call, cancellationToken) =>
            {
                var city = call.Input.TryGetValue("city", out var c) ? c.GetString() ?? "" : "";
                Console.WriteLine($"[tool] get_current_weather({city})");
                return await GetWeatherAsync(city, cancellationToken);
            },
        };

    // Two Open-Meteo calls: geocoding (city name -> coordinates), then the
    // forecast endpoint (coordinates -> current conditions).
    private static async Task<string> GetWeatherAsync(string city, CancellationToken ct)
    {
        try
        {
            var Http = new HttpClient();
            var geoUrl =
                "https://geocoding-api.open-meteo.com/v1/search"
                + $"?name={Uri.EscapeDataString(city)}&count=1&language=en&format=json";
            using var geo = JsonDocument.Parse(await Http.GetStringAsync(geoUrl, ct));
            if (!geo.RootElement.TryGetProperty("results", out var results) || results.GetArrayLength() == 0)
            {
                return $"City not found: {city}";
            }

            var place = results[0];
            var latitude = place.GetProperty("latitude").GetDouble();
            var longitude = place.GetProperty("longitude").GetDouble();
            var placeName = place.GetProperty("name").GetString();
            var country = place.TryGetProperty("country", out var co) ? co.GetString() : "";

            var forecastUrl =
                "https://api.open-meteo.com/v1/forecast"
                + $"?latitude={latitude.ToString(CultureInfo.InvariantCulture)}"
                + $"&longitude={longitude.ToString(CultureInfo.InvariantCulture)}"
                + "&current=temperature_2m,apparent_temperature,wind_speed_10m,weather_code"
                + "&timezone=auto";
            using var forecast = JsonDocument.Parse(await Http.GetStringAsync(forecastUrl, ct));
            var current = forecast.RootElement.GetProperty("current");

            return $"Weather in {placeName}, {country}: "
                + $"{DescribeWeatherCode(current.GetProperty("weather_code").GetInt32())}, "
                + $"{current.GetProperty("temperature_2m").GetDouble():F1} C "
                + $"(feels like {current.GetProperty("apparent_temperature").GetDouble():F1} C), "
                + $"wind {current.GetProperty("wind_speed_10m").GetDouble():F1} km/h.";
        }
        catch (HttpRequestException ex)
        {
            return $"Weather service unavailable: {ex.Message}";
        }
    }

    // WMO weather interpretation codes used by Open-Meteo.
    private static string DescribeWeatherCode(int code) =>
        code switch
        {
            0 => "clear sky",
            1 or 2 or 3 => "partly cloudy to overcast",
            45 or 48 => "fog",
            >= 51 and <= 57 => "drizzle",
            >= 61 and <= 67 => "rain",
            >= 71 and <= 77 => "snow",
            >= 80 and <= 82 => "rain showers",
            85 or 86 => "snow showers",
            >= 95 => "thunderstorm",
            _ => "unknown conditions",
        };
}
