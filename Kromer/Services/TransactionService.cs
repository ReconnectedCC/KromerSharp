using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;
using Kromer.Data;
using Kromer.Models.Dto;
using Kromer.Models.Entities;
using Kromer.Models.Exceptions;
using Kromer.Models.WebSocket.Events;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

namespace Kromer.Services;

public class TransactionService(
    KromerContext context,
    ILogger<TransactionService> logger,
    Channel<IKristEvent> eventChannel)
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
    public TransactionEntity InitiateTransaction([NotNull] WalletEntity? sender, [NotNull] WalletEntity? recipient, decimal amount = 0,
        TransactionType transactionType = TransactionType.Transfer)
    {
        if (sender is null || recipient is null)
        {
            throw new KristException(ErrorCode.AddressNotFound);
        }

        if (sender.Address == recipient.Address)
        {
            throw new KristException(ErrorCode.SameWalletTransfer);
        }

        amount = decimal.Round(amount, 5, MidpointRounding.ToEven);
        if (amount == 0 && transactionType == TransactionType.Transfer)
        {
            throw new KristException(ErrorCode.InvalidAmount);
        }

        if (sender.Balance < amount)
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