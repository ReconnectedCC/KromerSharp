using System.Threading.Channels;
using Kromer.Data;
using Kromer.Models.Dto;
using Kromer.Models.Entities;
using Kromer.Models.Exceptions;
using Kromer.Models.WebSocket.Events;
using Microsoft.EntityFrameworkCore;

namespace Kromer.Services;

public class TransactionService(KromerContext context, ILogger<TransactionService> logger, Channel<IKristEvent> eventChannel)
{
    public const string ServerWallet = "serverwelf";

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

        // Apply filter to transfer transactions only
        if (transaction is { Amount: <= 0, TransactionType: TransactionType.Transfer })
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
            await context.Wallets.FirstOrDefaultAsync(q => q.Address == transaction.From);

        var recipientWallet =
            await context.Wallets.FirstOrDefaultAsync(q => q.Address == transaction.To);

        if (senderWallet is null || recipientWallet is null)
        {
            throw new KristException(ErrorCode.AddressNotFound);
        }

        // Sanitize names?
        transaction.From = senderWallet.Address;
        transaction.To = recipientWallet.Address;

        // Apply balance updates
        if (senderWallet.Address != ServerWallet)
        {
            senderWallet.Balance -= transaction.Amount;
        }

        recipientWallet.Balance += transaction.Amount;

        await context.Transactions.AddAsync(transaction);
        context.Entry(senderWallet).State = EntityState.Modified;
        context.Entry(recipientWallet).State = EntityState.Modified;

        logger.LogInformation("New {Type} transaction {Id}: {From} -> {Amount} KRO -> {To}. Metadata: '{Metadata}'",
            transaction.TransactionType, transaction.Id, transaction.From, transaction.Amount, transaction.To,
            transaction.Metadata);

        return transaction;
    }
}