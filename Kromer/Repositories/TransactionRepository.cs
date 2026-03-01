using System.Threading.Channels;
using Kromer.Data;
using Kromer.Models.Dto;
using Kromer.Models.Entities;
using Kromer.Models.Exceptions;
using Kromer.Models.WebSocket.Events;
using Kromer.Services;
using Kromer.Utils;
using Microsoft.EntityFrameworkCore;

namespace Kromer.Repositories;

public class TransactionRepository(
    KromerContext context,
    ILogger<TransactionRepository> logger,
    WalletRepository walletRepository,
    NameRepository nameRepository,
    TransactionService transactionService,
    Channel<IKristEvent> eventChannel)
{
    private IQueryable<TransactionEntity> PrepareAddressTransactions(string address, bool excludeMined = false)
    {
        var query = context.Transactions
            .Where(q => q.From == address || q.To == address);

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
        if (amount <= 0)
        {
            throw new KristException(ErrorCode.InvalidAmount);
        }

        if (string.IsNullOrEmpty(to) || to.Length > 64)
        {
            throw new KristParameterException("to");
        }

        var wallet = await walletRepository.GetWalletFromKeyAsync(privateKey);
        if (wallet is null)
        {
            throw new KristException(ErrorCode.AuthenticationFailed);
        }
        
        amount = decimal.Round(amount, 2, MidpointRounding.ToEven);

        if (amount > wallet.Balance)
        {
            throw new KristException(ErrorCode.InsufficientFunds);
        }

        var nameData = Validation.ParseMetaName(to);

        string recipientAddress;
        if (nameData.Valid)
        {
            var name = await nameRepository.GetNameAsync(nameData.Name);
            if (name is null)
            {
                throw new KristException(ErrorCode.NameNotFound);
            }
            recipientAddress = name.Owner;
        }
        else
        {
            recipientAddress = to;
        }

        var recipient = await walletRepository.GetWalletFromAddress(recipientAddress);
        if (recipient is null)
        {
            throw new KristException(ErrorCode.AddressNotFound);
        }

        if (recipient.Address == wallet.Address)
        {
            throw new KristException(ErrorCode.SameWalletTransfer);
        }

        var transaction = new TransactionEntity
        {
            Amount = amount,
            From = wallet.Address,
            To = recipient.Address,
            Metadata = metadata,
            SentName = nameData.Valid ? nameData.Name : null,
            SentMetaname = nameData.Valid && !string.IsNullOrWhiteSpace(nameData.Meta) ? nameData.Meta : null,
            Date = DateTime.UtcNow,
            TransactionType = TransactionType.Transfer,
        };

        await transactionService.CreateTransactionAsync(transaction);
        await context.SaveChangesAsync();
        
        // Emit transaction event
        await eventChannel.Writer.WriteAsync(new KristTransactionEvent
        {
            Transaction = TransactionDto.FromEntity(transaction),
        });

        return TransactionDto.FromEntity(transaction);
    }

    public static (string? MetaName, string? Name)? ParseName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var matches = Validation.MetaNameRegex().Matches(name);
        if (matches.Count == 0)
        {
            return null;
        }

        var metaNameMatch = matches[1];
        var nameMatch = matches[2];

        return (metaNameMatch.Value, nameMatch.Value);
    }
}