using Kromer.Data;
using Kromer.Models.Dto;
using Kromer.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kromer.Repositories;

public class TransactionRepository(KromerContext context, ILogger<TransactionRepository> logger)
{
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
    /// Create and track a simple transaction, changes are not committed to the database.
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
    /// Create and track a new transaction, changes are not committed to the database.
    /// This method does not validate balance.
    /// </summary>
    /// <param name="transaction">Transaction entity.</param>
    /// <returns>Tracked transaction entity.</returns>
    public async Task<TransactionEntity> CreateTransactionAsync(TransactionEntity transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        await context.Transactions.AddAsync(transaction);

        logger.LogInformation("New {Type} transaction {Id}: {From} -> {Amount} KRO -> {To}. Metadata: '{Metadata}'",
            transaction.TransactionType, transaction.Id, transaction.From, transaction.Amount, transaction.To,
            transaction.Metadata);

        return transaction;
    }
}