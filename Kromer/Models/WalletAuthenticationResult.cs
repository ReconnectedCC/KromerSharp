using Kromer.Models.Entities;

namespace Kromer.Models;

public class WalletAuthenticationResult
{
    public WalletEntity? Wallet { get; set; }
    public bool Authed { get; set; }
}