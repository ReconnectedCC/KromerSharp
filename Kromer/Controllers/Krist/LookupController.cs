using Humanizer;
using Kromer.Models.Api.Krist.Lookup;
using Kromer.Models.Api.Krist.Name;
using Kromer.Models.Api.Krist.Transaction;
using Kromer.Models.Exceptions;
using Kromer.Services;
using Microsoft.AspNetCore.Mvc;

namespace Kromer.Controllers.Krist;

[Route("api/krist/lookup")]
[ApiController]
public class LookupController(LookupService lookupService) : ControllerBase
{
    
    /// <summary>
    /// Lookup addresses.
    /// </summary>
    /// <param name="addresses"></param>
    /// <param name="fetchNames"></param>
    /// <returns></returns>
    /// <exception cref="KristParameterException"></exception>
    [HttpGet("addresses/{addresses}")]
    public async Task<ActionResult<KristLookupAddresses>> GetAddresses(string addresses,
        [FromQuery] bool fetchNames = false, [FromQuery] bool includePlayers = false)
    {
        var addressList = addresses.Split(',');
        if (addressList.Length == 0)
        {
            throw new KristParameterException("addresses");
        }

        return await lookupService.GetAddresses(addressList.ToList(), fetchNames, includePlayers);
    }

    
    /// <summary>
    /// Lookup transactions from addresses.
    /// </summary>
    /// <param name="addresses"></param>
    /// <param name="orderBy"></param>
    /// <param name="order"></param>
    /// <param name="includeMined"></param>
    /// <param name="limit"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    /// <exception cref="KristParameterException"></exception>
    [HttpGet("transactions/{addresses?}")]
    [HttpGet("transactions")]
    public async Task<ActionResult<KristResultTransactions>> GetTransactions(string? addresses,
        [FromQuery] string orderBy = "id",
        [FromQuery] string order = "ASC",
        [FromQuery] bool includeMined = false,
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0)
    {
        var addressList = addresses?.Split(',');

        limit = Math.Clamp(limit, 1, 1000);

        if (!Enum.TryParse<TransactionOrderByParameter>(orderBy.Pascalize(), out var orderByParameter))
        {
            throw new KristParameterException("orderBy");
        }

        if (!Enum.TryParse<OrderParameter>(order.ToLowerInvariant().Pascalize(), out var orderParameter))
        {
            throw new KristParameterException("order");
        }

        return await lookupService.GetTransactions(addressList?.ToList() ?? [], orderByParameter, orderParameter, includeMined, limit, offset);
    }

    /// <summary>
    /// Lookup names from addresses.
    /// </summary>
    /// <param name="addresses"></param>
    /// <param name="orderBy"></param>
    /// <param name="order"></param>
    /// <param name="limit"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    /// <exception cref="KristParameterException"></exception>
    [HttpGet("names/{addresses?}")]
    [HttpGet("names")]
    public async Task<ActionResult<KristResultNames>> GetNames(string? addresses,
        [FromQuery] string orderBy = "name",
        [FromQuery] string order = "ASC",
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0)
    {
        var addressList = addresses?.Split(',');

        limit = Math.Clamp(limit, 1, 1000);

        if (!Enum.TryParse<NameOrderByParameter>(orderBy.Pascalize(), out var orderByParameter))
        {
            throw new KristParameterException("orderBy");
        }

        if (!Enum.TryParse<OrderParameter>(order.ToLowerInvariant().Pascalize(), out var orderParameter))
        {
            throw new KristParameterException("order");
        }

        return await lookupService.GetNames(addressList?.ToList() ?? [], orderByParameter, orderParameter, limit, offset);
    }

    
    /// <summary>
    /// Lookup a name's history.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="orderBy"></param>
    /// <param name="order"></param>
    /// <param name="limit"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    /// <exception cref="KristParameterException"></exception>
    [HttpGet("names/{name}/history")]
    public async Task<ActionResult<KristResultTransactions>> GetNameHistory(string name,
        [FromQuery] string orderBy = "id",
        [FromQuery] string order = "ASC",
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0)
    {
        limit = Math.Clamp(limit, 1, 1000);

        if (!Enum.TryParse<TransactionOrderByParameter>(orderBy.Pascalize(), out var orderByParameter))
        {
            throw new KristParameterException("orderBy");
        }

        if (!Enum.TryParse<OrderParameter>(order.ToLowerInvariant().Pascalize(), out var orderParameter))
        {
            throw new KristParameterException("order");
        }

        return await lookupService.GetNameHistory(name, orderByParameter, orderParameter, limit, offset);
    }

    /// <summary>
    /// Lookup a name's transactions.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="orderBy"></param>
    /// <param name="order"></param>
    /// <param name="limit"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    /// <exception cref="KristParameterException"></exception>
    [HttpGet("names/{name}/transactions")]
    public async Task<ActionResult<KristResultTransactions>> GetNameTransactions(string name,
        [FromQuery] string orderBy = "id",
        [FromQuery] string order = "ASC",
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0)
    {
        limit = Math.Clamp(limit, 1, 1000);

        if (!Enum.TryParse<TransactionOrderByParameter>(orderBy.Pascalize(), out var orderByParameter))
        {
            throw new KristParameterException("orderBy");
        }

        if (!Enum.TryParse<OrderParameter>(order.ToLowerInvariant().Pascalize(), out var orderParameter))
        {
            throw new KristParameterException("order");
        }

        return await lookupService.GetNameTransactions(name, orderByParameter, orderParameter, limit, offset);
    }
}