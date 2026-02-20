using Microsoft.CodeAnalysis.CSharp;

namespace Kromer.Models.Api.V1;

public class Result<T>
{
    public T? Data { get; set; }
    
    public Error? Error { get; set; }

    public Result()
    {
    }
    
    public Result(T data) {
        Data = data;
    }
    
    public static Result<object> Throw(Error error)
    {
        var result = new Result<object>(default(T))
        {
            Error = error
        };
        return result;
    } 
}