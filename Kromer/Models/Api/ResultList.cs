namespace Kromer.Models.Api;

public class ResultList<T> : Result<IEnumerable<T>>
{
    public new IEnumerable<T> Data  { get => base.Data; set => base.Data = value; }
}