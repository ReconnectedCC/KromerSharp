using Kromer.Models.Api.Krist;
using Kromer.Models.Api.Krist.Misc;
using Kromer.Models.Api.Krist.Wallet;
using Kromer.Repositories;
using Kromer.Utils;
using Microsoft.AspNetCore.Mvc;

namespace Kromer.Controllers.Krist;

[Route("api/krist")]
[ApiController]
public class MiscellaneousController(WalletRepository walletRepository, MiscRepository miscRepository) : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<KristResultAuthentication>> Authenticate(KristRequestPrivateKey request)
    {
        var authResult = await walletRepository.VerifyAddressAsync(request.PrivateKey);

        return new KristResultAuthentication
        {
            Ok = true,
            Address = authResult.Wallet?.Address,
            Authed = authResult.Authed,
        };
    }

    [HttpGet("motd")]
    public ActionResult<KristMotdResponse> Motd()
    {
        return miscRepository.GetMotd();
    }

    [HttpGet("walletversion")]
    public ActionResult<KristWalletVersionResponse> WalletVersion()
    {
        return new KristWalletVersionResponse
        {
            Ok = true,
            WalletVersion = miscRepository.GetWalletVersion(),
        };
    }
    
    [HttpGet("whatsnew")]
    public ActionResult<object> WhatsNew()
    {
        // lgtm 👍
        return new {};
    }

    [HttpGet("supply")]
    public async Task<ActionResult<KristResultSupply>> Supply()
    {
        return new KristResultSupply
        {
            Ok = true,
            MoneySupply = await walletRepository.GetNetworkSupply(),
        };
    }
    
    [HttpPost("v2")]
    public ActionResult<KristResultV2Address> GetV2Address(KristRequestPrivateKey request)
    {
        return new KristResultV2Address
        {
            Ok = true,
            Address = Crypto.MakeV2Address(request.PrivateKey, "k"),
        };
    }
}