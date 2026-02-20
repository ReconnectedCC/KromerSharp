using Kromer.Models.Dto;

namespace Kromer.Models.Api.Internal;

public class WalletsResponse
{
    // no questions asked
    public IEnumerable<WalletDto> Wallet { get; set; }
}