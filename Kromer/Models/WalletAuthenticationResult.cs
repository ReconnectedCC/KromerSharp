using Kromer.Models.Dto;

namespace Kromer.Models;

public class WalletAuthenticationResult
{
    public WalletDto? Wallet { get; set; }
    public bool Authed { get; set; }
}