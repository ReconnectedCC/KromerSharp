using System.Text.RegularExpressions;
using Kromer.Data;
using Kromer.Models.Api;
using Kromer.Models.Api.Krist;
using Kromer.Models.Api.Krist.Name;
using Kromer.Models.Dto;
using Kromer.Models.Entities;
using Kromer.Models.Exceptions;
using Kromer.Services;
using Kromer.Utils;
using Microsoft.EntityFrameworkCore;

namespace Kromer.Repositories;

public partial class NameRepository(
    KromerContext context,
    IConfiguration configuration,
    WalletRepository walletRepository,
    ILogger<NameRepository> logger,
    TransactionService transactionService)
{
    public async Task<IList<NameDto>> GetAddressNamesAsync(string address, int limit = 50, int offset = 0)
    {
        var names = await context.Names
            .Where(q => EF.Functions.ILike(q.Owner, address))
            .Skip(offset)
            .Take(limit)
            .ToListAsync();

        return names.Select(NameDto.FromEntity).ToList();
    }

    public async Task<int> CountAddressNamesAsync(string address)
    {
        return await context.Names.CountAsync(q => EF.Functions.ILike(q.Owner, address));
    }

    public async Task<NameDto?> GetNameAsync(string name)
    {
        var nameEntity = await context.Names
            .FirstOrDefaultAsync(q => EF.Functions.ILike(q.Name, name));

        return nameEntity == null
            ? null
            : NameDto.FromEntity(nameEntity);
    }

    public async Task<IList<NameDto>> GetNamesAsync(int limit = 50, int offset = 0)
    {
        var names = await context.Names
            .OrderBy(q => q.TimeRegistered)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();

        return names.Select(NameDto.FromEntity).ToList();
    }

    public async Task<int> CountTotalNamesAsync()
    {
        return await context.Names.CountAsync();
    }

    public async Task<IList<NameDto>> GetDescendingNamesAsync(int limit = 50, int offset = 0)
    {
        var names = await context.Names
            .OrderByDescending(q => q.TimeRegistered)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();

        return names.Select(NameDto.FromEntity).ToList();
    }

    public decimal GetNameCost()
    {
        return configuration.GetValue<decimal>("NameCost", 500);
    }

    public async Task<bool> ExistsAsync(string name)
    {
        return await context.Names.AnyAsync(q => EF.Functions.ILike(q.Name, name));
    }

    public async Task<int> CountUnpaidAsync()
    {
        return await context.Names.CountAsync(q => q.Unpaid > 0);
    }

    public async Task<NameEntity> RegisterNameAsync(string privateKey, string name)
    {
        if (!Validation.IsNameValid(name))
        {
            throw new KristParameterException("name");
        }

        if (await ExistsAsync(name))
        {
            throw new KristException(ErrorCode.NameTaken);
        }

        var wallet = await walletRepository.GetWalletFromKeyAsync(privateKey);
        if (wallet is null)
        {
            throw new KristException(ErrorCode.InsufficientFunds);
        }

        var newNameCost = GetNameCost();
        if (wallet.Balance < newNameCost)
        {
            throw new KristException(ErrorCode.InsufficientFunds);
        }

        name = Validation.SanitizeName(name);


        var transaction = await transactionService.CreateSimpleTransactionAsync(wallet.Address, TransactionService.ServerWallet,
            newNameCost, TransactionType.NamePurchase);

        logger.LogInformation("Registering name '{Name}' for address {WalletAddress}", name, wallet.Address);

        var nameEntity = new NameEntity
        {
            Name = name,
            Owner = wallet.Address,
            OriginalOwner = wallet.Address,
            TimeRegistered = DateTime.UtcNow,
        };

        await context.Names.AddAsync(nameEntity);
        await context.SaveChangesAsync();

        return nameEntity;
    }

    public async Task<NameDto> TransferNameAsync(string privateKey, string name, string address)
    {
        if (!Validation.IsNameValid(name))
        {
            throw new KristParameterException("name");
        }

        var nameEntity = await context.Names.FirstOrDefaultAsync(q => EF.Functions.ILike(q.Name, name));
        if (nameEntity is null)
        {
            throw new KristException(ErrorCode.NameNotFound);
        }

        var wallet = await walletRepository.GetWalletFromKeyAsync(privateKey);
        if (wallet is null)
        {
            throw new KristException(ErrorCode.AddressNotFound); // ig ??
        }

        var recipientAddress = await walletRepository.GetAddressAsync(address);
        if (recipientAddress is null)
        {
            throw new KristException(ErrorCode.AddressNotFound);
        }

        if (!nameEntity.Owner.Equals(wallet.Address, StringComparison.OrdinalIgnoreCase))
        {
            throw new KristException(ErrorCode.NotNameOwner);
        }

        if (nameEntity.Owner.Equals(address, StringComparison.OrdinalIgnoreCase))
        {
            // self transfer
            return NameDto.FromEntity(nameEntity);
        }

        nameEntity.Owner = recipientAddress.Address;
        nameEntity.LastTransfered = DateTime.UtcNow;
        context.Entry(nameEntity).State = EntityState.Modified;
        
        var transaction = await transactionService.CreateSimpleTransactionAsync(wallet.Address, recipientAddress.Address, 0, TransactionType.NameTransfer);
        transaction.Name = name;
        
        await context.SaveChangesAsync();

        return NameDto.FromEntity(nameEntity);
    }

    public async Task<NameDto> UpdateNameAsync(string privateKey, string name, string? metadata)
    {
        if (!Validation.IsNameValid(name))
        {
            throw new KristParameterException("name");
        }

        var nameEntity = await context.Names.FirstOrDefaultAsync(q => EF.Functions.ILike(q.Name, name));
        if (nameEntity is null)
        {
            throw new KristException(ErrorCode.NameNotFound);
        }

        var wallet = await walletRepository.GetWalletFromKeyAsync(privateKey);
        if (wallet is null)
        {
            throw new KristException(ErrorCode.AddressNotFound); // ig ??
        }

        if (!nameEntity.Owner.Equals(wallet.Address, StringComparison.OrdinalIgnoreCase))
        {
            throw new KristException(ErrorCode.NotNameOwner);
        }

        nameEntity.Metadata = metadata;
        nameEntity.LastUpdated = DateTime.UtcNow;
        context.Entry(nameEntity).State = EntityState.Modified;
        
        await context.SaveChangesAsync();
        
        return NameDto.FromEntity(nameEntity);
    }
}