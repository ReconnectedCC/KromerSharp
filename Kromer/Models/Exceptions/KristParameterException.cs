namespace Kromer.Models.Exceptions;

public class KristParameterException(string parameterName) : KristException(ErrorCode.InvalidParameter)
{
    public string Parameter { get; set; } = parameterName;
}