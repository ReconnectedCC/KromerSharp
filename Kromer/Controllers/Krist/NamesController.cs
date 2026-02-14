using Kromer.Models.Api.Krist;
using Kromer.Models.Api.Krist.Name;
using Kromer.Models.Api.Krist.Wallet;
using Kromer.Models.Exceptions;
using Kromer.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Kromer.Controllers.Krist;

[Route("api/krist/[controller]")]
[ApiController]
public class NamesController(NameRepository nameRepository) : ControllerBase
{
    [HttpGet("{name}")]
    public async Task<ActionResult<KristResultName>> GetName(string name)
    {
        var nameDto = await nameRepository.GetNameAsync(name);

        if (nameDto is null)
        {
            throw new KristException(ErrorCode.NameNotFound);
        }

        return new KristResultName
        {
            Ok = true,
            Name = nameDto,
        };
    }

    [HttpGet("")]
    public async Task<ActionResult<KristResultNames>> GetNames([FromQuery] int limit = 50, [FromQuery] int offset = 0)
    {
        limit = Math.Clamp(limit, 1, 1000);

        var total = await nameRepository.CountTotalNamesAsync();
        var names = await nameRepository.GetNamesAsync(limit, offset);

        return new KristResultNames
        {
            Ok = true,
            Count = names.Count,
            Total = total,
            Names = names,
        };
    }

    [HttpGet("new")]
    public async Task<ActionResult<KristResultNames>> GetRecentNames([FromQuery] int limit = 50,
        [FromQuery] int offset = 0)
    {
        limit = Math.Clamp(limit, 1, 1000);

        var total = await nameRepository.CountTotalNamesAsync();
        var names = await nameRepository.GetDescendingNamesAsync(limit, offset);

        return new KristResultNames
        {
            Ok = true,
            Count = names.Count,
            Total = total,
            Names = names,
        };
    }

    [HttpGet("cost")]
    public ActionResult<KristResultCost> GetNameCost()
    {
        var cost = nameRepository.GetNameCost();

        return new KristResultCost
        {
            Ok = true,
            NameCost = cost,
        };
    }

    [HttpGet("bonus")]
    public async Task<ActionResult<KristResultBonus>> GetBonus()
    {
        var unpaid = await nameRepository.CountUnpaidAsync();

        return new KristResultBonus
        {
            Ok = true,
            NameBonus = unpaid,
        };
    }

    [HttpGet("check/{name}")]
    public async Task<ActionResult<KristResultAvailability>> CheckNameAvailability(string name)
    {
        var exists = await nameRepository.ExistsAsync(name);
        return new KristResultAvailability
        {
            Ok = true,
            Available = !exists,
        };
    }

    [HttpPost("{name}")]
    public async Task<ActionResult<KristResult>> RegisterName(string name, [FromBody] KristRequestPrivateKey request)
    {
        await nameRepository.RegisterNameAsync(request.PrivateKey, name);

        return new KristResult
        {
            Ok = true,
        };
    }

    [HttpPost("{name}/transfer")]
    public async Task<ActionResult<KristResultName>> TransferName(string name,
        [FromBody] KristRequestNameTransfer request)
    {
        throw new NotImplementedException();
        var result = await nameRepository.TransferNameAsync(request.PrivateKey, name, request.Address);
    }
}