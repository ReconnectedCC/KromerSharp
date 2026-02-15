using System.Text.RegularExpressions;
using Kromer.Data;
using Kromer.Models.Dto;
using Kromer.Models.Entities;
using Kromer.Models.Exceptions;
using Kromer.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Kromer.Repositories;

public partial class TransactionRepository(
    KromerContext context,
    ILogger<TransactionRepository> logger,
    WalletRepository walletRepository,
    NameRepository nameRepository)
{
    public const string ServerWallet = "serverwelf";

    [GeneratedRegex(@"^(?:([a-z0-9-_]{1,32})@)?([a-z0-9]{1,64})\.kro", RegexOptions.Compiled)]
    public static partial Regex KroNameRegex();

    private IQueryable<TransactionEntity> PrepareAddressTransactions(string address, bool excludeMined = false)
    {
        var query = context.Transactions
            .Where(q => (q.From != null && EF.Functions.ILike(q.From, address))
                        || EF.Functions.ILike(q.To, address));

        if (excludeMined)
        {
            query = query.Where(q => q.TransactionType != TransactionType.Mined);
        }

        return query;
    }

    public async Task<IList<TransactionDto>> GetAddressRecentTransactionsAsync(string address, int limit = 50,
        int offset = 0,
        bool excludeMined = false)
    {
        var transactions = await PrepareAddressTransactions(address, excludeMined)
            .OrderByDescending(q => q.Date)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();

        return transactions.Select(TransactionDto.FromEntity).ToList();
    }

    public async Task<int> CountAddressTransactionsAsync(string address, bool excludeMined = false)
    {
        var total = await PrepareAddressTransactions(address, excludeMined).CountAsync();

        return total;
    }

    /// <summary>
    /// Create and track a new simple transaction and update the relevant wallets. Changes are not committed to the database.
    /// This method does not validate balance.
    /// </summary>
    /// <param name="from">Sender address.</param>
    /// <param name="to">Recipient address.</param>
    /// <param name="amount">Transaction amount.</param>
    /// <param name="transactionType">Type of transaction.</param>
    /// <returns>Tracked transaction entity.</returns>
    public async Task<TransactionEntity> CreateSimpleTransactionAsync(string from, string to, decimal amount,
        TransactionType transactionType)
    {
        var transaction = new TransactionEntity
        {
            From = from,
            To = to,
            Amount = amount,
            TransactionType = transactionType,

            Date = DateTime.UtcNow,
        };

        return await CreateTransactionAsync(transaction);
    }

    /// <summary>
    /// Create and track a new transaction and update the relevant wallets. Changes are not committed to the database.
    /// This method does not validate balance.
    /// </summary>
    /// <param name="transaction">Transaction entity.</param>
    /// <returns>Tracked transaction entity.</returns>
    public async Task<TransactionEntity> CreateTransactionAsync(TransactionEntity transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        if (transaction.Amount < 0)
        {
            throw new KristException(ErrorCode.InvalidAmount);
        }

        transaction.Amount = Math.Round(transaction.Amount, 2, MidpointRounding.ToZero);

        if (string.IsNullOrWhiteSpace(transaction.From))
        {
            transaction.From = ServerWallet;
        }

        if (string.IsNullOrWhiteSpace(transaction.To))
        {
            transaction.To = ServerWallet;
        }

        var senderWallet =
            await context.Wallets.FirstOrDefaultAsync(q => EF.Functions.ILike(q.Address, transaction.From));

        var recipientWallet =
            await context.Wallets.FirstOrDefaultAsync(q => EF.Functions.ILike(q.Address, transaction.To));

        if (senderWallet is null || recipientWallet is null)
        {
            throw new KristException(ErrorCode.AddressNotFound);
        }

        // Sanitize names?
        transaction.From = senderWallet.Address;
        transaction.To = recipientWallet.Address;

        // Apply balance updates
        senderWallet.Balance -= transaction.Amount;
        recipientWallet.Balance += transaction.Amount;

        await context.Transactions.AddAsync(transaction);
        context.Entry(senderWallet).State = EntityState.Modified;
        context.Entry(recipientWallet).State = EntityState.Modified;

        logger.LogInformation("New {Type} transaction {Id}: {From} -> {Amount} KRO -> {To}. Metadata: '{Metadata}'",
            transaction.TransactionType, transaction.Id, transaction.From, transaction.Amount, transaction.To,
            transaction.Metadata);

        return transaction;
    }

    private IQueryable<TransactionEntity> PrepareTransactionList(bool excludeMined)
    {
        var query = context.Transactions.AsQueryable();
        if (excludeMined)
        {
            query = query.Where(q => q.TransactionType != TransactionType.Mined);
        }

        return query;
    }

    public async Task<int> CountTransactionsAsync(bool excludeMined = false)
    {
        return await PrepareTransactionList(excludeMined).CountAsync();
    }

    public async Task<IList<TransactionDto>> GetPaginatedTransactionsAsync(int offset = 0, int limit = 50,
        bool excludeMined = false)
    {
        var query = PrepareTransactionList(excludeMined);

        query = query
            .OrderBy(q => q.Id)
            .Skip(offset)
            .Take(limit);

        var transactions = await query.ToListAsync();

        return transactions.Select(TransactionDto.FromEntity).ToList();
    }

    public async Task<IList<TransactionDto>> GetPaginatedLatestTransactionsAsync(int offset = 0, int limit = 50,
        bool excludeMined = false)
    {
        var query = PrepareTransactionList(excludeMined);

        query = query
            .OrderByDescending(q => q.Id)
            .Skip(offset)
            .Take(limit);

        var transactions = await query.ToListAsync();

        return transactions.Select(TransactionDto.FromEntity).ToList();
    }

    public async Task<TransactionDto> GetTransaction(int id)
    {
        var transaction = await context.Transactions.FirstOrDefaultAsync(q => q.Id == id);

        if (transaction is null)
        {
            throw new KristException(ErrorCode.TransactionNotFound);
        }

        return TransactionDto.FromEntity(transaction);
    }

    public async Task<TransactionDto> RequestCreateTransaction(string privateKey, string to, decimal amount,
        string? metadata = null)
    {
        throw new NotImplementedException();
        if (string.IsNullOrEmpty(to) || to.Length > 64)
        {
            throw new KristParameterException("to");
        }

        var wallet = await walletRepository.GetWalletFromKeyAsync(privateKey);
        if (wallet is null)
        {
            throw new KristException(ErrorCode.AuthenticationFailed);
        }

        if (amount > wallet.Balance)
        {
            throw new KristException(ErrorCode.InsufficientFunds);
        }

        var isName = Validation.IsMetaNameValid(to);

        var recipientAddress = await walletRepository.GetAddressAsync(to);
        if (recipientAddress is null)
        {
            throw new KristException(ErrorCode.AddressNotFound);
        }

        if (isName)
        {
            var nameData = ParseName(to) ?? (null, null);
            var name = await nameRepository.GetNameAsync(nameData.Name);
        }

        var transaction = new TransactionEntity
        {
            Amount = amount,
            From = wallet.Address,
            To = recipientAddress.Address,
            Metadata = metadata,
        };

        return null;
    }

    public static (string? MetaName, string? Name)? ParseName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var matches = KroNameRegex().Matches(name);
        if (matches.Count == 0)
        {
            return null;
        }

        var metaNameMatch = matches[1];
        var nameMatch = matches[2];

        return (metaNameMatch.Value, nameMatch.Value);
    }
}