using System.Text.RegularExpressions;
using Kromer.Data;
using Kromer.Models.Api;
using Kromer.Models.Api.Krist;
using Kromer.Models.Api.Krist.Name;
using Kromer.Models.Dto;
using Kromer.Models.Entities;
using Kromer.Models.Exceptions;
using Kromer.Utils;
using Microsoft.EntityFrameworkCore;

namespace Kromer.Repositories;

public partial class NameRepository(
    KromerContext context,
    IConfiguration configuration,
    WalletRepository walletRepository,
    TransactionRepository transactionRepository,
    ILogger<NameRepository> logger)
{
    [GeneratedRegex("^[a-z0-9]{1,64}$")]
    private static partial Regex NameRegex();

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
        return configuration.GetValue<decimal>("Kromer:NameCost", 500);
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
        if (!IsNameValid(name))
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

        name = SanitizeName(name);


        var transaction = await transactionRepository.CreateSimpleTransactionAsync(wallet.Address, "serverwelf",
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

    public bool IsNameValid(string name)
    {
        return NameRegex().IsMatch(name.ToLowerInvariant());
    }

    public string SanitizeName(string name)
    {
        return name.Trim().ToLowerInvariant();
    }

    public async Task<NameDto> TransferNameAsync(string privateKey, string name, string address)
    {
        if (!IsNameValid(name))
        {
            throw new KristParameterException("name");
        }

        if (!await ExistsAsync(name))
        {
            throw new KristException(ErrorCode.NameNotFound);
        }

        var nameEntity = await context.Names.FirstOrDefaultAsync(q => EF.Functions.ILike(q.Name, name));

        throw new NotImplementedException();
    }
}