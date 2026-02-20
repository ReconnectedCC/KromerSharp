using Kromer.Attributes;
using Kromer.Models.Api.Internal;
using Kromer.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Kromer.Controllers.Internal;

[Route("api/_internal/wallet")]
[ApiController]
[RequireInternalKey]
public class InternalWalletController(PlayerRepository playerRepository) : ControllerBase
{
    [HttpPost("create")]
    public async Task<ActionResult<AddressCreationResponse>> CreateWallet([FromBody] PlayerRequest request)
    {
        var response = await playerRepository.CreatePlayerWalletAsync(request.Uuid, request.Name);
        return response;
    }

    [HttpPost("give-money")]
    public async Task<ActionResult<WalletResponse>> GiveMoney([FromBody] LoadCreditRequest request)
    {
        var response = await playerRepository.GiveMoneyAsync(request.Address, request.Amount);
        return response;
    }
    
    [HttpGet("by-player/{uuid:guid}")]
    public async Task<ActionResult<WalletsResponse>> GetWalletByPlayer(Guid uuid)
    {
        var response = await playerRepository.GetWalletByPlayerAsync(uuid);
        return response;
    }
}