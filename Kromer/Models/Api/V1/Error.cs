namespace Kromer.Models.Api.V1;

public class Error
{
    public string Code { get; set; }
    public string Message { get; set; }
    public object? Details { get; set; }
}