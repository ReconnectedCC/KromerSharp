namespace Kromer.Models.Api.V1;

public class ResultList<T> : Result<IEnumerable<T>>
{
    public new IEnumerable<T> Data { get => base.Data; set => base.Data = value; }
}