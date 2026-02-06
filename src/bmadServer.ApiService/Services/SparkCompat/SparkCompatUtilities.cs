using System.Text.Json;
using bmadServer.ApiService.Data.Entities;

namespace bmadServer.ApiService.Services.SparkCompat;

public static class SparkCompatUtilities
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static string CreateId(string prefix)
    {
        return $"{prefix}-{Guid.NewGuid():N}";
    }

    public static long ToUnixMilliseconds(DateTime utc)
    {
        return new DateTimeOffset(utc).ToUnixTimeMilliseconds();
    }

    public static DateTime FromUnixMilliseconds(long epochMs)
    {
        return DateTimeOffset.FromUnixTimeMilliseconds(epochMs).UtcDateTime;
    }

    public static string ToJson<T>(T value)
    {
        return JsonSerializer.Serialize(value, SerializerOptions);
    }

    public static T? FromJson<T>(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(value, SerializerOptions);
    }

    public static string ToSparkRole(PersonaType persona)
    {
        return persona == PersonaType.Technical ? "technical" : "business";
    }

    public static PersonaType ToPersonaType(string? role)
    {
        return string.Equals(role, "technical", StringComparison.OrdinalIgnoreCase)
            ? PersonaType.Technical
            : PersonaType.Business;
    }
}
