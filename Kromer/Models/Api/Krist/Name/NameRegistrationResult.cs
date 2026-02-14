namespace Kromer.Models.Api.Krist.Name;

public class NameRegistrationResult
{
    public NameRegistrationResult()
    {
    }

    public NameRegistrationResult(string errorCode, string? parameter = null)
    {
        ErrorCode = errorCode;
        Parameter = parameter;
    }
    
    public bool Success => string.IsNullOrEmpty(ErrorCode);
    public bool HasParameter => !string.IsNullOrEmpty(Parameter);
    public string? ErrorCode  { get; set; }
    public string? Parameter { get; set; }
}