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