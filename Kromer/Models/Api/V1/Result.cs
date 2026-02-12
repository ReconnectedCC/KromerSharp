namespace Kromer.Models.Api.V1;

public class Result<T>
{
    public T? Data { get; set; }
    
    public string? Code  { get; set; }
    
    public string? Message { get; set; }
    
    public IEnumerable<string>? Details  { get; set; }

    public Result()
    {
    }
    
    public Result(T data) {
        Data = data;
    }

    public Result(string error)
    {
        Data = default;
        Code = error;
        Message = error;
        Details = [];
    }
}