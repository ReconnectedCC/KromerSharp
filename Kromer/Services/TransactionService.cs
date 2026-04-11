using System.Diagnostics.CodeAnalysis;
using Kromer.Data;
using Kromer.Models.Dto;
using Kromer.Models.Entities;
using Kromer.Models.Exceptions;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

namespace Kromer.Services;

public class TransactionService(
    KromerContext context,
    ILogger<TransactionService> logger)
{
    public const string ServerWallet = "serverwelf";

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="recipient"></param>
    /// <param name="amount"></param>
    /// <param name="transactionType"></param>
    /// <returns></returns>
    /// <exception cref="KristException"></exception>
    public TransactionEntity InitiateTransaction([NotNull] WalletEntity? sender, [NotNull] WalletEntity? recipient,
        decimal amount = 0,
        TransactionType transactionType = TransactionType.Transfer)
    {
        // If mined, sender is null (or serverwelf actually, because too late to fix it)
        // If amount is negative, swap sender and recipient, abs amount and use Transfer type.
        if (transactionType == TransactionType.Mined && amount < 0)
        {
            (sender, recipient) = (recipient, sender);
            amount = Math.Abs(amount);
            transactionType = TransactionType.Transfer;

            // Guard against sender not being serverwelf
            if (sender?.Address != ServerWallet)
            {
                throw new KristException(ErrorCode.InvalidRequestType);
            }
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
        if (amount <= 0 && transactionType == TransactionType.Transfer)
        {
            throw new KristException(ErrorCode.InvalidAmount);
        }

        if (sender.Balance < amount && sender.Address != ServerWallet)
        {
            throw new KristException(ErrorCode.InsufficientFunds);
        }

        if (sender.Address != ServerWallet)
        {
            sender.Balance -= amount;
        }

        recipient.Balance += amount;

        sender.TotalOut += amount;
        recipient.TotalIn += amount;

        return new TransactionEntity
        {
            From = sender.Address,
            To = recipient.Address,
            Amount = amount,
            TransactionType = transactionType,
            Date = DateTime.UtcNow,
        };
    }

    public async Task<TransactionEntity> CommitTransactionAsync(WalletEntity sender, WalletEntity recipient,
        TransactionEntity transaction)
    {
        ArgumentNullException.ThrowIfNull(sender);
        ArgumentNullException.ThrowIfNull(recipient);
        ArgumentNullException.ThrowIfNull(transaction);

        context.Wallets.Update(sender);
        context.Wallets.Update(recipient);
        await context.Transactions.AddAsync(transaction);

        await context.SaveChangesAsync();

        logger.LogInformation("New {Type} transaction {Id}: {From} -> {Amount} KRO -> {To}. Metadata: '{Metadata}'",
            transaction.TransactionType, transaction.Id, transaction.From, transaction.Amount, transaction.To,
            transaction.Metadata);

        return transaction;
    }
}