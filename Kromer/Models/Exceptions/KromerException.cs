using System.ComponentModel;
using System.Net;
using System.Reflection;
using Kromer.Models.Exceptions.Attributes;

namespace Kromer.Models.Exceptions;

public class KromerException : Exception
{
    public ErrorCode Code { get; set; }

    public string Error => GetError(Code);

    public KromerException(ErrorCode code) : base(GetDescription(code))
    {
        Code = code;
    }

    private static string GetError(ErrorCode code)
    {
        return SnakeCaseNamingPolicy.Convert(Enum.GetName(code) ?? code.ToString());
    }
    
    public HttpStatusCode GetStatusCode()
    {
        var statusCode = Code.GetType()
            .GetField(Code.ToString())?
            .GetCustomAttribute<StatusCodeAttribute>()?
            .StatusCode ?? HttpStatusCode.BadRequest;

        return statusCode;
    }
    public static string GetDescription(ErrorCode code)
    {
        var statusCode = code.GetType()
            .GetField(code.ToString())?
            .GetCustomAttribute<DescriptionAttribute>()?
            .Description ?? "No description available";

        return statusCode;
    }
}