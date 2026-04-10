using Kromer.Models.Api.Krist.Address;
using Kromer.Models.Api.Krist.Name;
using Kromer.Models.Api.Krist.Transaction;
using Kromer.Models.Exceptions;
using Kromer.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Kromer.Controllers.Krist;

[Route("api/krist/addresses")]
[ApiController]
public class AddressesController(WalletRepository walletRepository, TransactionRepository transactionRepository, NameRepository nameRepository)
    : ControllerBase
{
    /// <summary>
    /// Retrieves a paginated list of Krist wallet addresses.
    /// </summary>
    /// <param name="limit">The maximum number of addresses to retrieve. The value is clamped between 1 and 1000. Default is 50.</param>
    /// <param name="offset">The number of addresses to skip before starting to retrieve the list. Default is 0.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the paginated list of addresses in a <see cref="KristResultAddresses"/> object.</returns>
    [HttpGet("")]
    public async Task<ActionResult<KristResultAddresses>> GetAddresses([FromQuery] int limit = 50,
        [FromQuery] int offset = 0)
    {
        limit = Math.Clamp(limit, 1, 1000);

        var total = await walletRepository.CountTotalWalletsAsync();
        var wallets = await walletRepository.GetPaginatedAddressesAsync(offset, limit);

        var list = new KristResultAddresses
        {
            Ok = true,
            Total = total,
            Count = wallets.Count,
            Addresses = wallets,
        };

        return list;
    }

    /// <summary>
    /// Retrieves detailed information about a specific Krist wallet address.
    /// </summary>
    /// <param name="address">The Krist wallet address to retrieve details for.</param>
    /// <param name="fetchNames">A boolean indicating whether to include associated names in the address information.</param>
    /// <param name="includePlayer">A boolean indicating whether to include the UUID of the player that owns the address if available.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the details of the address in a <see cref="KristResultAddress"/> object.</returns>
    /// <exception cref="KristException">Thrown when the specified address is not found.</exception>
    [HttpGet("{address}")]
    public async Task<ActionResult<KristResultAddress>> Address(string address, [FromQuery] bool fetchNames, [FromQuery] bool includePlayer = false)
    {
        var addressDto = await walletRepository.GetAddressAsync(address, fetchNames, includePlayer);

        if (addressDto is null)
        {
            throw new KristException(ErrorCode.AddressNotFound);
        }
        
        return new KristResultAddress()
        {
            Ok = true,
            Address = addressDto,
        };
    }

    /// <summary>
    /// Retrieves a paginated list of the richest Krist wallet addresses, ordered by balance in descending order.
    /// </summary>
    /// <param name="limit">The maximum number of addresses to retrieve. The value is clamped between 1 and 1000. Default is 50.</param>
    /// <param name="offset">The number of addresses to skip before starting to retrieve the list. Default is 0.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the paginated list of the richest addresses in a <see cref="KristResultAddresses"/> object.</returns>
    [HttpGet("rich")]
    public async Task<ActionResult<KristResultAddresses>> Richest([FromQuery] int limit = 50,
        [FromQuery] int offset = 0)
    {
        limit = Math.Clamp(limit, 1, 1000);

        var total = await walletRepository.CountTotalWalletsAsync();
        var wallets = await walletRepository.GetPaginatedRichestAddressesAsync(offset, limit);

        var list = new KristResultAddresses
        {
            Ok = true,
            Total = total,
            Count = wallets.Count,
            Addresses = wallets,
        };

        return list;
    }

    /// <summary>
    /// Retrieves a list of recent transactions associated with a specified Krist wallet address.
    /// </summary>
    /// <param name="address">The Krist wallet address for which to fetch recent transactions.</param>
    /// <param name="limit">The maximum number of transactions to retrieve. The value is clamped between 1 and 1000. Default is 50.</param>
    /// <param name="offset">The number of transactions to skip before starting to retrieve the list. Default is 0.</param>
    /// <param name="excludeMined">A flag indicating whether to exclude mined transactions from the result. Default is false.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains a list of recent transactions and associated metadata in a <see cref="KristResultTransactions"/> object.</returns>
    /// <exception cref="KristException">Thrown when the specified wallet address does not exist.</exception>
    [HttpGet("{address}/transactions")]
    public async Task<ActionResult<KristResultTransactions>> RecentTransactions(string address,
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0, [FromQuery] bool excludeMined = false)
    {
        limit = Math.Clamp(limit, 1, 1000);

        if (!await walletRepository.ExistsAsync(address))
        {
            throw new KristException(ErrorCode.AddressNotFound);
        }

        var total = await transactionRepository.CountAddressTransactionsAsync(address, excludeMined);
        var transactions =
            await transactionRepository.GetAddressRecentTransactionsAsync(address, limit, offset, excludeMined);

        var list = new KristResultTransactions
        {
            Ok = true,
            Total = total,
            Count = transactions.Count,
            Transactions = transactions,
        };

        return list;
    }


    /// <summary>
    /// Retrieves a paginated list of Krist names associated with the specified wallet address.
    /// </summary>
    /// <param name="address">The wallet address whose associated names are to be retrieved.</param>
    /// <param name="limit">The maximum number of names to retrieve. The value is clamped between 1 and 1000. Default is 50.</param>
    /// <param name="offset">The number of names to skip before starting to retrieve the list. Default is 0.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the paginated list of names in a <see cref="KristResultNames"/> object.</returns>
    /// <exception cref="KristException">Thrown when the specified wallet address does not exist.</exception>
    [HttpGet("{address}/names")]
    public async Task<ActionResult<KristResultNames>> Names(string address, [FromQuery] int limit = 50,
        [FromQuery] int offset = 0)
    {
        if (!await walletRepository.ExistsAsync(address))
        {
            throw new KristException(ErrorCode.AddressNotFound);
        }

        limit = Math.Clamp(limit, 1, 1000);

        var total = await nameRepository.CountAddressNamesAsync(address);
        var names = await nameRepository.GetAddressNamesAsync(address, limit, offset);

        var list = new KristResultNames
        {
            Ok = true,
            Total = total,
            Count = names.Count,
            Names = names,
        };

        return list;
    }
}