using System.Text.Json;
using RideTogether.Models;

namespace RideTogether.Services;

public sealed class RideSearchParser(HttpClient http)
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private static readonly JsonElement FilterSchema = JsonSerializer
        .Deserialize<JsonElement>("""
        {
          "type": "object",
          "properties": {
            "departure": { "type": ["string", "null"] },
            "destination": { "type": ["string", "null"] },
            "date": { "type": ["string", "null"] },
            "after": { "type": ["string", "null"] }
          },
          "required": ["departure", "destination", "date", "after"],
          "additionalProperties": false
        }
        """);

    public async Task<RideSearchFilters> ParseAsync(
        string query, CancellationToken cancellationToken)
    {
        var warsaw = TimeZoneInfo.FindSystemTimeZoneById("Europe/Warsaw");
        TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, warsaw);

        using var response = await http.PostAsJsonAsync("api/chat", new
        {
            model = "qwen2.5:3b",
            stream = false,
            format = FilterSchema,
            options = new { temperature = 0 },
            messages = new[]
            {
                new
                {
                    role = "system",
                    content = $$$"""
                        Extract filters for a ride search from the user's request.
                        Today's date in Warsaw is {{today:yyyy-MM-dd}}.
                        Use today's date only to resolve an explicit relative date such as "today" or "tomorrow".
                        If the user gives no date, return null for date.
                        "from X" means departure. "to Y" means destination.
                        A city name by itself means departure.
                        Set after only when the user specifies an earliest departure time.
                        Format dates as YYYY-MM-DD and times as HH:mm.
                        Preserve Polish place names and diacritics.
                        
                        Examples:
                        "Show rides from Gdańsk to Sopot" -> {"departure":"Gdańsk","destination":"Sopot","date":null,"after":null}
                        "to Sopot" -> {"departure":null,"destination":"Sopot","date":null,"after":null}
                        "Gdańsk" -> {"departure":"Gdańsk","destination":null,"date":null,"after":null}
                        """
                },
                new { role = "user", content = query }
            }
        }, cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OllamaResponse>(
            JsonOptions, cancellationToken);

        var content = result?.Message?.Content;
        if (string.IsNullOrWhiteSpace(content))
            throw new JsonException("Ollama returned an empty response.");

        return JsonSerializer.Deserialize<RideSearchFilters>(content, JsonOptions)
            ?? throw new JsonException("Ollama returned no search filters.");
    }

    private sealed record OllamaResponse(OllamaMessage? Message);
    private sealed record OllamaMessage(string? Content);
}