using Kromer.Models.Api.Krist;
using Kromer.Models.Api.Krist.Name;
using Kromer.Models.Api.Krist.Wallet;
using Kromer.Models.Exceptions;
using Kromer.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Kromer.Controllers.Krist;

[Route("api/krist/names")]
[ApiController]
public class NamesController(NameRepository nameRepository) : ControllerBase
{
    /// <summary>
    /// Retrieves the information of a specific name.
    /// </summary>
    /// <param name="name">The name to retrieve information for.</param>
    /// <returns>An <see cref="ActionResult{T}"/> containing a <see cref="KristResultName"/> object if the name exists.</returns>
    /// <exception cref="KristException">Thrown if the specified name is not found.</exception>
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

    /// <summary>
    /// Retrieves a list of names with optional pagination parameters.
    /// </summary>
    /// <param name="limit">The maximum number of names to retrieve. Valid values range from 1 to 1000. Defaults to 50 if not specified.</param>
    /// <param name="offset">The number of names to skip before starting to retrieve records. Defaults to 0 if not specified.</param>
    /// <returns>An <see cref="ActionResult{T}"/> containing a <see cref="KristResultNames"/> object, which includes the list of names, total count, and result metadata.</returns>
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

    /// <summary>
    /// Retrieves a list of the most recently registered names.
    /// </summary>
    /// <param name="limit">The maximum number of names to retrieve. Defaults to 50. Must be a value between 1 and 1000.</param>
    /// <param name="offset">The number of names to skip before starting to retrieve the list. Defaults to 0.</param>
    /// <returns>An <see cref="ActionResult{T}"/> containing a <see cref="KristResultNames"/> object with the retrieved names, their count, and the total number of available names.</returns>
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

    /// <summary>
    /// Retrieves the current cost required to register a name.
    /// </summary>
    /// <returns>An <see cref="ActionResult{T}"/> containing a <see cref="KristResultCost"/> object with the current name registration cost.</returns>
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

    /// <summary>
    /// Retrieves the bonus information associated with unpaid names.
    /// </summary>
    /// <returns>An <see cref="ActionResult{T}"/> containing a <see cref="KristResultBonus"/> object with the bonus details.</returns>
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


    /// <summary>
    /// Checks the availability of a specific name.
    /// </summary>
    /// <param name="name">The name to check for availability.</param>
    /// <returns>An <see cref="ActionResult{T}"/> containing a <see cref="KristResultAvailability"/> object with the availability status of the name.</returns>
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

    /// <summary>
    /// Registers a new name in the system.
    /// </summary>
    /// <param name="name">The name to be registered.</param>
    /// <param name="request">An object containing the private key required for registration.</param>
    /// <returns>A <see cref="KristResult"/> indicating whether the name registration was successful.</returns>
    [HttpPost("{name}")]
    public async Task<ActionResult<KristResult>> RegisterName(string name, [FromBody] KristRequestPrivateKey request)
    {
        await nameRepository.RegisterNameAsync(request.PrivateKey, name);

        return new KristResult
        {
            Ok = true,
        };
    }

    /// <summary>
    /// Transfers ownership of a name to a new address.
    /// Transferring to 'serverwelf' will delete the name.
    /// </summary>
    /// <param name="name">The name to be transferred.</param>
    /// <param name="request">The transfer request containing the private key and the destination address.</param>
    /// <returns>An <see cref="ActionResult{T}"/> containing a <see cref="KristResultName"/> object representing the transferred name.</returns>
    /// <exception cref="KristException">Thrown if the transfer fails due to invalid credentials or other errors.</exception>
    [HttpPost("{name}/transfer")]
    public async Task<ActionResult<KristResultName>> TransferName(string name,
        [FromBody] KristRequestNameTransfer request)
    {
        var result = await nameRepository.TransferNameAsync(request.PrivateKey, name, request.Address);

        return new KristResultName
        {
            Ok = true,
            Name = result
        };
    }

    /// <summary>
    /// Updates the metadata of an existing name.
    /// </summary>
    /// <param name="name">The name to update.</param>
    /// <param name="request">The update request containing the private key and the new metadata.</param>
    /// <returns>An <see cref="ActionResult{T}"/> containing the updated <see cref="KristResultName"/> object if the update is successful.</returns>
    /// <exception cref="KristException">Thrown if the specified name does not exist, the private key is invalid, or the user is not authorized to update the name.</exception>
    [HttpPost("{name}/update")]
    [HttpPut("{name}/update")]
    public async Task<ActionResult<KristResultName>> UpdateName(string name, [FromBody] KristRequestNameUpdate request)
    {
        var result = await nameRepository.UpdateNameAsync(request.PrivateKey, name, request.A);

        return new KristResultName
        {
            Ok = true,
            Name = result
        };
    }
}