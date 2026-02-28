using Kromer.Data;
using Kromer.Models.Api.Krist.Lookup;
using Kromer.Models.Api.Krist.Name;
using Kromer.Models.Api.Krist.Transaction;
using Kromer.Models.Dto;
using Kromer.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kromer.Services;

public class LookupService(KromerContext context)
{
    public async Task<KristLookupAddresses> GetAddresses(List<string> addresses, bool fetchNames = false)
    {
        var wallets = await context.Wallets
            .Where(q => addresses.Contains(q.Address))
            .ToListAsync();

        var dtos = wallets.Select(AddressDto.FromEntity)
            .DistinctBy(q => q.Address)
            .ToList();

        if (fetchNames)
        {
            foreach (var dto in dtos)
            {
                dto.Names = await context.Names.CountAsync(q => q.Owner == dto.Address);
            }
        }

        return new KristLookupAddresses
        {
            Ok = true,
            Found = dtos.Count,
            NotFound = addresses.Count - dtos.Count,
            Addresses = dtos.ToDictionary(q => q.Address, q => q),
        };
    }

    public async Task<ActionResult<KristResultTransactions>> GetTransactions(List<string> addressList,
        TransactionOrderByParameter orderBy, OrderParameter order, bool includeMined, int limit, int offset)
    {
        var transactions = context.Transactions.AsQueryable();
        if (addressList.Count > 0)
        {
            transactions = transactions.Where(q => addressList.Contains(q.To) || addressList.Contains(q.From));
        }

        if (!includeMined)
        {
            transactions = transactions.Where(q => q.TransactionType != TransactionType.Mined);
        }

        if (order == OrderParameter.Asc)
        {
            transactions = orderBy switch
            {
                TransactionOrderByParameter.Id => transactions.OrderBy(q => q.Id),
                TransactionOrderByParameter.From => transactions.OrderBy(q => q.From),
                TransactionOrderByParameter.To => transactions.OrderBy(q => q.To),
                TransactionOrderByParameter.Value => transactions.OrderBy(q => q.Amount),
                TransactionOrderByParameter.Time => transactions.OrderBy(q => q.Date),
                TransactionOrderByParameter.SentName => transactions.OrderBy(q => q.SentName),
                TransactionOrderByParameter.SentMetaname => transactions.OrderBy(q => q.SentMetaname),
                _ => throw new ArgumentOutOfRangeException(nameof(orderBy), orderBy, null)
            };
        }
        else if (order == OrderParameter.Desc)
        {
            transactions = orderBy switch
            {
                TransactionOrderByParameter.Id => transactions.OrderByDescending(q => q.Id),
                TransactionOrderByParameter.From => transactions.OrderByDescending(q => q.From),
                TransactionOrderByParameter.To => transactions.OrderByDescending(q => q.To),
                TransactionOrderByParameter.Value => transactions.OrderByDescending(q => q.Amount),
                TransactionOrderByParameter.Time => transactions.OrderByDescending(q => q.Date),
                TransactionOrderByParameter.SentName => transactions.OrderByDescending(q => q.SentName),
                TransactionOrderByParameter.SentMetaname => transactions.OrderByDescending(q => q.SentMetaname),
                _ => throw new ArgumentOutOfRangeException(nameof(orderBy), orderBy, null)
            };
        }

        transactions = transactions
            .Skip(offset)
            .Take(limit);

        var entities = await transactions.ToListAsync();

        return new KristResultTransactions
        {
            Ok = true,
            Total = await context.Transactions.CountAsync(),
            Count = entities.Count,
            Transactions = entities.Select(TransactionDto.FromEntity).ToList()
        };
    }

    public async Task<ActionResult<KristResultNames>> GetNames(List<string> addressList,
        NameOrderByParameter orderBy, OrderParameter order, int limit, int offset)
    {
        var names = context.Names.AsQueryable();
        if (addressList.Count > 0)
        {
            names = names.Where(q => addressList.Contains(q.Name));
        }

        if (order == OrderParameter.Asc)
        {
            names = orderBy switch
            {
                NameOrderByParameter.Name => names.OrderBy(q => q.Name),
                NameOrderByParameter.Owner => names.OrderBy(q => q.Owner),
                NameOrderByParameter.OriginalOwner => names.OrderBy(q => q.OriginalOwner),
                NameOrderByParameter.Registered => names.OrderBy(q => q.TimeRegistered),
                NameOrderByParameter.Updated => names.OrderBy(q => q.LastUpdated),
                NameOrderByParameter.Transferred => names.OrderBy(q => q.LastTransfered),
                NameOrderByParameter.TransferredOrRegistered => names.OrderBy(q => q.LastTransfered)
                    .ThenBy(q => q.TimeRegistered),
                NameOrderByParameter.A => names.OrderBy(q => q.Metadata),
                NameOrderByParameter.Unpaid => names.OrderBy(q => q.Unpaid),
                _ => throw new ArgumentOutOfRangeException(nameof(orderBy), orderBy, null)
            };
        }
        else if (order == OrderParameter.Desc)
        {
            names = orderBy switch
            {
                NameOrderByParameter.Name => names.OrderByDescending(q => q.Name),
                NameOrderByParameter.Owner => names.OrderByDescending(q => q.Owner),
                NameOrderByParameter.OriginalOwner => names.OrderByDescending(q => q.OriginalOwner),
                NameOrderByParameter.Registered => names.OrderByDescending(q => q.TimeRegistered),
                NameOrderByParameter.Updated => names.OrderByDescending(q => q.LastUpdated),
                NameOrderByParameter.Transferred => names.OrderByDescending(q => q.LastTransfered),
                NameOrderByParameter.TransferredOrRegistered => names.OrderByDescending(q => q.LastTransfered)
                    .ThenByDescending(q => q.TimeRegistered),
                NameOrderByParameter.A => names.OrderByDescending(q => q.Metadata),
                NameOrderByParameter.Unpaid => names.OrderByDescending(q => q.Unpaid),
                _ => throw new ArgumentOutOfRangeException(nameof(orderBy), orderBy, null)
            };
        }

        names = names
            .Skip(offset)
            .Take(limit);

        var entities = await names.ToListAsync();

        return new KristResultNames
        {
            Ok = true,
            Total = await context.Transactions.CountAsync(),
            Count = entities.Count,
            Names = entities.Select(NameDto.FromEntity).ToList()
        };
    }

    public async Task<ActionResult<KristResultTransactions>> GetNameHistory(string name,
        TransactionOrderByParameter orderBy, OrderParameter order, int limit, int offset)
    {
        var transactions = context.Transactions
            .Where(q => q.Name == name
                        && (q.TransactionType == TransactionType.NamePurchase ||
                            q.TransactionType == TransactionType.NameARecord ||
                            q.TransactionType == TransactionType.NameTransfer));

        if (order == OrderParameter.Asc)
        {
            transactions = orderBy switch
            {
                TransactionOrderByParameter.Id => transactions.OrderBy(q => q.Id),
                TransactionOrderByParameter.From => transactions.OrderBy(q => q.From),
                TransactionOrderByParameter.To => transactions.OrderBy(q => q.To),
                TransactionOrderByParameter.Value => transactions.OrderBy(q => q.Amount),
                TransactionOrderByParameter.Time => transactions.OrderBy(q => q.Date),
                TransactionOrderByParameter.SentName => transactions.OrderBy(q => q.SentName),
                TransactionOrderByParameter.SentMetaname => transactions.OrderBy(q => q.SentMetaname),
                _ => throw new ArgumentOutOfRangeException(nameof(orderBy), orderBy, null)
            };
        }
        else if (order == OrderParameter.Desc)
        {
            transactions = orderBy switch
            {
                TransactionOrderByParameter.Id => transactions.OrderByDescending(q => q.Id),
                TransactionOrderByParameter.From => transactions.OrderByDescending(q => q.From),
                TransactionOrderByParameter.To => transactions.OrderByDescending(q => q.To),
                TransactionOrderByParameter.Value => transactions.OrderByDescending(q => q.Amount),
                TransactionOrderByParameter.Time => transactions.OrderByDescending(q => q.Date),
                TransactionOrderByParameter.SentName => transactions.OrderByDescending(q => q.SentName),
                TransactionOrderByParameter.SentMetaname => transactions.OrderByDescending(q => q.SentMetaname),
                _ => throw new ArgumentOutOfRangeException(nameof(orderBy), orderBy, null)
            };
        }

        transactions = transactions
            .Skip(offset)
            .Take(limit);

        var entities = await transactions.ToListAsync();

        return new KristResultTransactions
        {
            Ok = true,
            Total = await context.Transactions.CountAsync(),
            Count = entities.Count,
            Transactions = entities.Select(TransactionDto.FromEntity).ToList()
        };
    }

    public async Task<ActionResult<KristResultTransactions>> GetNameTransactions(string name, TransactionOrderByParameter orderBy, OrderParameter order, int limit, int offset)
    {
        var transactions = context.Transactions
            .Where(q => q.SentName == name);

        if (order == OrderParameter.Asc)
        {
            transactions = orderBy switch
            {
                TransactionOrderByParameter.Id => transactions.OrderBy(q => q.Id),
                TransactionOrderByParameter.From => transactions.OrderBy(q => q.From),
                TransactionOrderByParameter.To => transactions.OrderBy(q => q.To),
                TransactionOrderByParameter.Value => transactions.OrderBy(q => q.Amount),
                TransactionOrderByParameter.Time => transactions.OrderBy(q => q.Date),
                TransactionOrderByParameter.SentName => transactions.OrderBy(q => q.SentName),
                TransactionOrderByParameter.SentMetaname => transactions.OrderBy(q => q.SentMetaname),
                _ => throw new ArgumentOutOfRangeException(nameof(orderBy), orderBy, null)
            };
        }
        else if (order == OrderParameter.Desc)
        {
            transactions = orderBy switch
            {
                TransactionOrderByParameter.Id => transactions.OrderByDescending(q => q.Id),
                TransactionOrderByParameter.From => transactions.OrderByDescending(q => q.From),
                TransactionOrderByParameter.To => transactions.OrderByDescending(q => q.To),
                TransactionOrderByParameter.Value => transactions.OrderByDescending(q => q.Amount),
                TransactionOrderByParameter.Time => transactions.OrderByDescending(q => q.Date),
                TransactionOrderByParameter.SentName => transactions.OrderByDescending(q => q.SentName),
                TransactionOrderByParameter.SentMetaname => transactions.OrderByDescending(q => q.SentMetaname),
                _ => throw new ArgumentOutOfRangeException(nameof(orderBy), orderBy, null)
            };
        }

        transactions = transactions
            .Skip(offset)
            .Take(limit);

        var entities = await transactions.ToListAsync();

        return new KristResultTransactions
        {
            Ok = true,
            Total = await context.Transactions.CountAsync(),
            Count = entities.Count,
            Transactions = entities.Select(TransactionDto.FromEntity).ToList()
        };
    }
}