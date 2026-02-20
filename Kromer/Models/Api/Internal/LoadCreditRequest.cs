namespace Kromer.Models.Api.Internal;

public class LoadCreditRequest
{
    public required string Address { get; set; }
    public decimal Amount { get; set; }
}