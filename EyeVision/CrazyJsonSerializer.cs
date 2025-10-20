using System.Text.Json;
using System.Text.Json.Serialization;

namespace EyeVision;

public static class CrazyJsonSerializer
{
    private static JsonSerializerOptions Options => new JsonSerializerOptions()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };
    
    public static string Serialize(object obj)
    {
        return JsonSerializer.Serialize(obj, obj.GetType(), Options);
    }
    
    public static string SerializeCompact(object obj)
    {
        JsonSerializerOptions options = Options;
        options.WriteIndented = false;
        
        return JsonSerializer.Serialize(obj, obj.GetType(), options);
    }

    public static T? Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, Options);
    }
}