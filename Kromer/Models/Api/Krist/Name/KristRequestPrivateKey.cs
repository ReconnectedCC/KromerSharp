using Kromer.Models.Api.Krist.Wallet;

namespace Kromer.Models.Api.Krist.Name;

public class KristRequestNameTransfer : KristRequestPrivateKey
{
    public required string Address { get; set; }
}