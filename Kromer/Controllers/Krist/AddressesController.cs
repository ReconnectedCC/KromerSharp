using Kromer.Models.Api.Krist;
using Kromer.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Kromer.Controllers.Krist;

[Route("api/krist/[controller]")]
[ApiController]
public class AddressesController(WalletRepository walletRepository, TransactionRepository transactionRepository, NameRepository nameRepository)
    : ControllerBase
{
    [HttpGet("")]
    public async Task<ActionResult<KristResultAddresses>> Addresses([FromQuery] int limit = 50,
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

    [HttpGet("{address}")]
    public async Task<ActionResult<KristResultAddress>> Address(string address, [FromQuery] bool fetchNames)
    {
        var addressDto = await walletRepository.GetAddressAsync(address, fetchNames);

        if (addressDto is null)
        {
            return new KristResultAddress
            {
                Ok = false,
                Error = "blabla", // TODO: give actual error
            };
        }

        return new KristResultAddress()
        {
            Ok = true,
            Address = addressDto,
        };
    }

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

    [HttpGet("{address}/transactions")]
    public async Task<ActionResult<KristResultTransactions>> RecentTransactions(string address,
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0, [FromQuery] bool excludeMined = false)
    {
        limit = Math.Clamp(limit, 1, 1000);

        var total = await transactionRepository.CountAddressTransactionsAsync(address, excludeMined);
        var transactions =
            await transactionRepository.GetAddressRecentTransactionsAsync(address, limit, offset, excludeMined);

        if (transactions.Count == 0)
        {
            return new KristResultTransactions
            {
                Ok = false,
                Error = "blabla", // TODO: give actual error
            };
        }

        var list = new KristResultTransactions
        {
            Ok = true,
            Total = total,
            Count = transactions.Count,
            Transactions = transactions,
        };

        return list;
    }

    [HttpGet("{address}/names")]
    public async Task<ActionResult<KristResultNames>> Names(string address, [FromQuery] int limit = 50, [FromQuery] int offset = 0)
    {
        if (!await walletRepository.Exists(address))
        {
            return new KristResultNames
            {
                Ok = false,
                Error = "blabla", // TODO: give actual error
            };
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