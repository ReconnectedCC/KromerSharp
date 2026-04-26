using System.Net;
using Kromer.Attributes;
using Kromer.Models.Api.Internal;
using Kromer.Repositories;
using Kromer.Services;
using Kromer.Utils;
using Microsoft.AspNetCore.Mvc;

namespace Kromer.Controllers.Internal;

[Route("api/_internal/wallet")]
[ApiController]
[RequireInternalKey]
public class InternalWalletController(PlayerRepository playerRepository, DiscordService discordService, ILogger<InternalWalletController> logger) : ControllerBase
{
    /// <summary>
    /// Creates a new wallet for the player.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("create")]
    public async Task<ActionResult<AddressCreationResponse>> CreateWallet([FromBody] PlayerRequest request)
    {
        var response = await playerRepository.CreatePlayerWalletAsync(request.Uuid, request.Name);
        return response;
    }

    /// <summary>
    /// Give money to a player's wallet.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("give-money")]
    public async Task<ActionResult<WalletResponse>> GiveMoney([FromBody] LoadCreditRequest request)
    {
        var response = await playerRepository.GiveMoneyAsync(request.Address, request.Amount);

        var remoteAddress = HttpContext.Connection.RemoteIpAddress;
        if (remoteAddress is not null)
        {
            if (!LocalAddress.IsLanAddress(remoteAddress))
            {
                await discordService.SendGiveMoneyAlertAsync(request.Address, request.Amount, remoteAddress);
            }
        }
        else
        {
            logger.LogWarning("Unable to determine remote address");
        }


        return response;
    }

    /// <summary>
    /// Get a player's wallet.
    /// </summary>
    /// <param name="uuid"></param>
    /// <returns></returns>
    [HttpGet("by-player/{uuid:guid}")]
    public async Task<ActionResult<WalletsResponse>> GetWalletByPlayer(Guid uuid)
    {
        var response = await playerRepository.GetWalletByPlayerAsync(uuid);
        return response;
    }

    [HttpPost("update-player")]
    public async Task<ActionResult> UpdatePlayer([FromBody] PlayerUpdateRequest request)
    {
        await playerRepository.UpdatePlayerAsync(request.Uuid, request.Username);
        return Ok();
    }
}