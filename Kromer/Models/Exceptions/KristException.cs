using System.Net;
using System.Reflection;
using Kromer.Models.Exceptions.Attributes;

namespace Kromer.Models.Exceptions;

public class KristException : Exception
{
    public ErrorCode Code { get; set; }

    public string Error => GetError(Code);

    public KristException(ErrorCode code)
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
            .GetField(nameof(Code))?
            .GetCustomAttribute<StatusCodeAttribute>()?
            .StatusCode ?? HttpStatusCode.BadRequest;

        return statusCode;
    }
}