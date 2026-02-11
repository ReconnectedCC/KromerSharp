using Kromer.Data;
using Kromer.Models.Api;
using Kromer.Models.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kromer.Controllers;

[Route("api/v1/[controller]")]
[ApiController]
public class WalletController(KromerContext context) : ControllerBase
{
    [HttpGet("by-name/{name}")]
    public async Task<ActionResult<ResultList<WalletDto>>> GetWallet(string name)
    {
        var player = await context.Players
            .FirstOrDefaultAsync(p => EF.Functions.Like(p.Name, name));
        
        if (player is null || player.OwnedWallets?.Count is 0 or null)
        {
            return NotFound(new Result<WalletDto>("player_error")); // TODO: add proper errors
        }
        
        var playerWallets = await context.Wallets
            .Where(q => player.OwnedWallets.Contains(q.Id))
            .ToListAsync();

        var response = new ResultList<WalletDto>
        {
            Data = playerWallets.Select(WalletDto.FromEntity),
        };

        return response;

    }
}