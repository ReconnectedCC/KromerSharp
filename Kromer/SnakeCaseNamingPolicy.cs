using System.Text.Json;
using Newtonsoft.Json.Serialization;

namespace Kromer;

public class SnakeCaseNamingPolicy : JsonNamingPolicy
{
    // ¯\_(ツ)_/¯
    private readonly SnakeCaseNamingStrategy _snakeCaseNamingStrategy = new();
    
    public override string ConvertName(string name)
    {
        return _snakeCaseNamingStrategy.GetPropertyName(name,  false);
    }
}