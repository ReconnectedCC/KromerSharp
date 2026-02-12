using Kromer.Data;
using Kromer.Models.Dto;
using Microsoft.EntityFrameworkCore;

namespace Kromer.Repositories;

public class WalletRepository(KromerContext context)
{
    public Task<bool> Exists(string address)
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
}