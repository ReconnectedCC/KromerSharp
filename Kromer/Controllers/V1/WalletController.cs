using Kromer.Models.Api.V1;
using Kromer.Models.Dto;
using Kromer.Models.Exceptions;
using Kromer.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Kromer.Controllers.V1;

[Route("api/v1/[controller]")]
[ApiController]
public class WalletController(WalletRepository walletRepository) : ControllerBase
{
    /// <summary>
    /// Retrieves the wallets associated with a specific player name.
    /// </summary>
    /// <param name="name">The name of the player whose wallets are to be retrieved.</param>
    /// <returns>
    /// An <see cref="ActionResult"/> containing a <see cref="ResultList{WalletDto}"/> object,
    /// which holds the list of wallets associated with the provided player name.
    /// </returns>
    /// <exception cref="KromerException">
    /// Thrown when no wallets are found for the specified player, with the error code <c>PlayerError</c>.
    /// </exception>
    [HttpGet("by-name/{name}")]
    public async Task<ActionResult<ResultList<WalletDto>>> GetWalletByName(string name)
    {
        var playerWallets = await walletRepository.GetPlayerWalletsAsync(name);

        if (playerWallets.Count == 0)
        {
            throw new KromerException(ErrorCode.PlayerError);
        }

        var response = new ResultList<WalletDto>
        {
            Data = playerWallets,
        };

        return response;
    }

    /// <summary>
    /// Retrieves the wallets associated with a specific player UUID.
    /// </summary>
    /// <param name="uuid">The unique identifier of the player whose wallets are to be retrieved.</param>
    /// <returns>
    /// An <see cref="ActionResult"/> containing a <see cref="ResultList{WalletDto}"/> object,
    /// which holds the list of wallets associated with the provided player UUID.
    /// </returns>
    /// <exception cref="KromerException">
    /// Thrown when no wallets are found for the specified player, with the error code <c>PlayerError</c>.
    /// </exception>
    [HttpGet("by-player/{uuid:guid}")]
    public async Task<ActionResult<ResultList<WalletDto>>> GetWalletByPlayer(Guid uuid)
    {
        var playerWallets = await walletRepository.GetPlayerWalletsAsync(uuid);

        if (playerWallets.Count == 0)
        {
            throw new KromerException(ErrorCode.PlayerError);
        }

        var response = new ResultList<WalletDto>
        {
            Data = playerWallets,
        };

        return response;
    }
}