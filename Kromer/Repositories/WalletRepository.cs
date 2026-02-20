using Kromer.Data;
using Kromer.Models;
using Kromer.Models.Dto;
using Kromer.Models.Entities;
using Kromer.Utils;
using Microsoft.EntityFrameworkCore;

namespace Kromer.Repositories;

public class WalletRepository(KromerContext context, ILogger<WalletRepository> logger)
{
    public Task<bool> ExistsAsync(string address)
    {
        return context.Wallets.AnyAsync(q => EF.Functions.ILike(q.Address, address));
    }

    public async Task<IList<WalletDto>> GetPlayerWalletsAsync(Guid uuid)
    {
        var wallets = await context.Players
            .Where(p => p.Id == uuid)
            .SelectMany(p => context.Wallets
                .Where(w => p.OwnedWallets != null && p.OwnedWallets.Contains(w.Id)))
            .ToListAsync();

        return wallets.Select(WalletDto.FromEntity).ToList();
    }

    public async Task<IList<WalletDto>> GetPlayerWalletsAsync(string name)
    {
        var wallets = await context.Players
            .Where(p => EF.Functions.ILike(p.Name, name))
            .SelectMany(p => context.Wallets
                .Where(w => p.OwnedWallets != null && p.OwnedWallets.Contains(w.Id)))
            .ToListAsync();

        return wallets.Select(WalletDto.FromEntity).ToList();
    }

    public async Task<int> CountTotalWalletsAsync()
    {
        return await context.Wallets.CountAsync();
    }

    public async Task<IList<AddressDto>> GetPaginatedAddressesAsync(int offset = 0, int limit = 50)
    {
        var paginatedWallets = await context.Wallets
            .OrderBy(q => q.CreatedAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();

        return paginatedWallets.Select(AddressDto.FromEntity).ToList();
    }

    public async Task<IList<AddressDto>> GetPaginatedRichestAddressesAsync(int offset = 0, int limit = 50)
    {
        var paginatedWallets = await context.Wallets
            .OrderByDescending(q => q.Balance)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();

        return paginatedWallets.Select(AddressDto.FromEntity).ToList();
    }

    public async Task<AddressDto?> GetAddressAsync(string address, bool fetchNames = false)
    {
        var wallet = await context.Wallets.FirstOrDefaultAsync(q => EF.Functions.ILike(q.Address, address));
        if (wallet is null)
        {
            return null;
        }

        int? names = null;
        if (fetchNames)
        {
            names = await context.Names.CountAsync(q => EF.Functions.ILike(q.Owner, address));
        }

        var addressDto = AddressDto.FromEntity(wallet);
        addressDto.Names = names;

        return addressDto;
    }

    public async Task<WalletEntity> GetOrCreateWalletAsync(string address, string hash, decimal initialBalance = 0)
    {
        var wallet = await context.Wallets.FirstOrDefaultAsync(q => EF.Functions.ILike(q.Address, address));

        if (wallet is not null)
        {
            return wallet;
        }

        wallet = new WalletEntity
        {
            Address = address,
            Balance = initialBalance,
            CreatedAt = DateTime.UtcNow,
            PrivateKey = hash,
        };

        await context.Wallets.AddAsync(wallet);
        await context.SaveChangesAsync();

        return wallet;
    }

    public async Task<WalletEntity?> GetWalletFromKeyAsync(string privateKey)
    {
        var address = Crypto.MakeV2Address(privateKey, "k");
        var pairKey = $"{address}{privateKey}";
        var hash = Crypto.Sha256(pairKey);

        logger.LogDebug("Attempting authentication for address {Address}", address);

        var wallet = await GetOrCreateWalletAsync(address, hash);

        var authenticated = wallet.PrivateKey == hash;

        if (authenticated)
        {
            return wallet;
        }

        logger.LogDebug("Authentication failed for address {Address}", address);
        return null;
    }

    public async Task<WalletEntity?> GetWalletFromAddress(string address)
    {
        var wallet = await context.Wallets.FirstOrDefaultAsync(q => EF.Functions.ILike(q.Address, address));

        return wallet;
    }


    public async Task<WalletAuthenticationResult> VerifyAddressAsync(string privateKey)
    {
        var wallet = await GetWalletFromKeyAsync(privateKey);

        return new WalletAuthenticationResult
        {
            Authed = wallet is not null,
            Wallet = wallet,
        };
    }

    public async Task<decimal> GetNetworkSupply()
    {
        return await context.Wallets
            .Where(q => !EF.Functions.ILike(q.Address, "serverwelf"))
            .SumAsync(q => q.Balance);
    }
}