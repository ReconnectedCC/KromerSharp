namespace Kromer.Models.Api.Krist.Transaction;

public class KristRequestTransaction
{
    public required string PrivateKey { get; set; }
    public required string To { get; set; }
    public required decimal Amount { get; set; }
    public string? MetaData { get; set; }
}