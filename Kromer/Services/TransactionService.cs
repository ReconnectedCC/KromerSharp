using System.Diagnostics.CodeAnalysis;
using Kromer.Data;
using Kromer.Models.Entities;
using Kromer.Models.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Kromer.Services;

public class TransactionService(
    KromerContext context,
    ILogger<TransactionService> logger)
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="recipient"></param>
    /// <param name="amount"></param>
    /// <param name="transactionType"></param>
    /// <returns></returns>
    /// <exception cref="KristException"></exception>
    public async Task<TransactionEntity> RunTransactionAsync([NotNull] WalletEntity? sender,
        [NotNull] WalletEntity? recipient,
        decimal amount = 0,
        TransactionType transactionType = TransactionType.Transfer, TransactionEntity? transaction = null)
    {
        // If mined, sender is null (or serverwelf actually, because too late to fix it)
        // If amount is negative, swap sender and recipient, abs amount
        if (transactionType == TransactionType.Mined && amount < 0)
        {
            (sender, recipient) = (recipient, sender);
            amount = Math.Abs(amount);
        }

        if (sender is null || recipient is null)
        {
            throw new KristException(ErrorCode.AddressNotFound);
        }

        if (sender.Address == recipient.Address && transactionType == TransactionType.Transfer)
        {
            throw new KristException(ErrorCode.SameWalletTransfer);
        }

        amount = decimal.Round(amount, 5, MidpointRounding.ToEven);
        if (amount < 0 || (amount == 0 && transactionType == TransactionType.Transfer))
        {
            throw new KristException(ErrorCode.InvalidAmount);
        }

        var ownsTx = context.Database.CurrentTransaction is null;
        await using var tx = ownsTx
            ? await context.Database.BeginTransactionAsync()
            : null;

        if (sender.Address == Constants.ServerWallet)
        {
            var updated = await context.Wallets
                .Where(q => q.Id == sender.Id)
                .ExecuteUpdateAsync(setter => setter
                    .SetProperty(q => q.TotalOut, q => q.TotalOut + amount)
                );

            if (updated != 1)
            {
                throw new KristException(ErrorCode.AddressNotFound);
            }
        }
        else
        {
            var debited = await context.Wallets
                .Where(q => q.Id == sender.Id && q.Balance >= amount)
                .ExecuteUpdateAsync(setter => setter
                    .SetProperty(q => q.Balance, q => q.Balance - amount)
                    .SetProperty(q => q.TotalOut, q => q.TotalOut + amount)
                );

            if (debited != 1)
            {
                throw new KristException(ErrorCode.InsufficientFunds);
            }
        }

        var credited = await context.Wallets
            .Where(q => q.Id == recipient.Id)
            .ExecuteUpdateAsync(setter => setter
                .SetProperty(q => q.Balance, q => q.Balance + amount)
                .SetProperty(q => q.TotalIn, q => q.TotalIn + amount)
            );

        if (credited != 1)
        {
            throw new KristException(ErrorCode.AddressNotFound);
        }

        transaction ??= new TransactionEntity();
        transaction.From = sender.Address;
        transaction.To = recipient.Address;
        transaction.Amount = amount;
        transaction.TransactionType = transactionType;
        transaction.Date = DateTime.UtcNow;

        await context.Transactions.AddAsync(transaction);

        await context.SaveChangesAsync();

        logger.LogInformation("New {Type} transaction {Id}: {From} -> {Amount} KRO -> {To}. Metadata: '{Metadata}'",
            transaction.TransactionType, transaction.Id, transaction.From, transaction.Amount, transaction.To,
            transaction.Metadata);

        if (tx is not null)
        {
            await tx.CommitAsync();
        }

        return transaction;
    }
}