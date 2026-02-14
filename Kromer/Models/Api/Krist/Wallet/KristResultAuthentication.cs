namespace Kromer.Models.Api.Krist.Wallet;

public class KristResultAuthentication
{
    public bool Ok { get; set; }
    public bool Authed { get; set; }
    public string? Address { get; set; }
}