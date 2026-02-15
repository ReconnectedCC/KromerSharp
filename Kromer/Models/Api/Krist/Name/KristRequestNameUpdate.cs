using Kromer.Models.Api.Krist.Wallet;

namespace Kromer.Models.Api.Krist.Name;

public class KristRequestNameUpdate : KristRequestPrivateKey
{
    public string? A { get; set; }
}