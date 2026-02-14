using System.Text.Json;
using Newtonsoft.Json.Serialization;

namespace Kromer;

public class SnakeCaseNamingPolicy : JsonNamingPolicy
{
    // ¯\_(ツ)_/¯
    private static readonly SnakeCaseNamingStrategy SnakeCaseNamingStrategy = new();

    public static string Convert(string name)
    {
        return SnakeCaseNamingStrategy.GetPropertyName(name, false);
    }
    
    public override string ConvertName(string name)
    {
        return Convert(name);
    }

}