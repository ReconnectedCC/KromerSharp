using System.Threading.Channels;
using Kromer.Data;
using Kromer.Models.Api.V1.Subscriptions;
using Kromer.Models.Dto;
using Kromer.Models.Entities;
using Kromer.Models.Exceptions;
using Kromer.Models.WebSocket.Events;
using Kromer.Services;
using Kromer.Utils;
using Microsoft.EntityFrameworkCore;

namespace Kromer.Repositories;

public class SubscriptionRepository(
    KromerContext context,
    WalletRepository walletRepository,
    TransactionService transactionService,
    Channel<IKristEvent> eventChannel,
    ILogger<SubscriptionRepository> logger)
{
    private const string ReasonContractCancelled = "contract_cancelled";
    private const string ReasonInsufficientFunds = "insufficient_funds";
    private const string ReasonUnsubscribed = "unsubscribed";
    private const string ReasonSubscriberMissing = "subscriber_missing";
    private const string ReasonOwnerMissing = "owner_missing";
    private const string ReasonSameWallet = "same_wallet";

    public async Task<CreateSubscriptionResponse> CreateContractAsync(CreateSubscriptionRequest? request)
    {
        AssertCreateRequest(request);

        var receiver = await NormalizeReceiverAsync(request!.Name, requireExisting: true);
        var owner = await AuthenticateCurrentOwnerAsync(request.PrivateKey, receiver.BaseName);

        var contract = new SubscriptionContractEntity
        {
            Receiver = receiver.Receiver,
            BaseName = receiver.BaseName,
            Price = decimal.Round(request.Price, 5, MidpointRounding.ToEven),
            PeriodMinutes = request.Period,
            Description = request.Description.Trim(),
            Status = SubscriptionStatus.Active,
            CreatedAt = DateTime.UtcNow,
        };

        await context.SubscriptionContracts.AddAsync(contract);
        await context.SaveChangesAsync();

        logger.LogInformation("Created subscription contract {ContractId} for {Receiver} owned by {Owner}",
            contract.Id, contract.Receiver, owner.Address);

        return new CreateSubscriptionResponse
        {
            Id = contract.Id,
        };
    }

    public async Task<SubscriptionDto> CancelContractAsync(int id, string? privateKey)
    {
        AssertPrivateKey(privateKey);

        var contract = await context.SubscriptionContracts
            .Include(q => q.WalletSubscriptions.Where(s => s.Status == SubscriptionStatus.Active))
            .FirstOrDefaultAsync(q => q.Id == id);

        if (contract is null)
        {
            throw new KromerException(ErrorCode.ResourceNotFound);
        }

        var owner = await AuthenticateCurrentOwnerAsync(privateKey, contract.BaseName);

        if (contract.Status == SubscriptionStatus.Active)
        {
            var now = DateTime.UtcNow;
            contract.Status = SubscriptionStatus.Cancelled;
            contract.CancelledAt = now;

            foreach (var subscription in contract.WalletSubscriptions)
            {
                CancelWalletSubscription(subscription, ReasonContractCancelled, now);
            }

            await context.SaveChangesAsync();

            if (contract.WalletSubscriptions.Count == 0)
            {
                await EmitSubscriptionEventAsync("contract_cancelled", contract, null, owner.Address,
                    SubscriptionStatus.Cancelled, ReasonContractCancelled);
            }
            else
            {
                foreach (var subscription in contract.WalletSubscriptions)
                {
                    await EmitSubscriptionEventAsync("contract_cancelled", contract, subscription, owner.Address,
                        SubscriptionStatus.Cancelled, ReasonContractCancelled);
                }
            }
        }

        return await BuildDtoAsync(contract, owner.Address);
    }

    public async Task<SubscriptionDto> GetContractAsync(int id, string? address)
    {
        if (!string.IsNullOrWhiteSpace(address) && !Validation.IsValidAddress(address))
        {
            throw new KromerException(ErrorCode.InvalidParameter);
        }

        var contract = await context.SubscriptionContracts.FirstOrDefaultAsync(q => q.Id == id);
        if (contract is null)
        {
            throw new KromerException(ErrorCode.ResourceNotFound);
        }

        return await BuildDtoAsync(contract, address);
    }

    public async Task<SubscriptionListResponse> ListContractsAsync(
        string? address,
        string? name,
        bool excludeOwned,
        bool onlyOwned,
        bool onlyUnsubscribable,
        int limit,
        int offset)
    {
        limit = Math.Clamp(limit, 1, 1000);
        offset = Math.Max(offset, 0);

        var query = context.SubscriptionContracts
            .Where(q => q.Status == SubscriptionStatus.Active);

        string? contextAddress = null;
        if (!string.IsNullOrWhiteSpace(name))
        {
            var receiver = await NormalizeReceiverAsync(name, requireExisting: false);
            query = receiver.IsMetaname
                ? query.Where(q => q.Receiver == receiver.Receiver)
                : query.Where(q => q.BaseName == receiver.BaseName);
        }
        else if (!string.IsNullOrWhiteSpace(address))
        {
            if (!Validation.IsValidAddress(address))
            {
                throw new KromerException(ErrorCode.InvalidParameter);
            }

            contextAddress = address;
            var ownedBaseNames = context.Names
                .Where(q => q.Owner == address)
                .Select(q => q.Name);

            var subscribedContracts = context.WalletSubscriptions
                .Where(q => q.WalletAddress == address && q.Status == SubscriptionStatus.Active);

            if (onlyUnsubscribable)
            {
                subscribedContracts = subscribedContracts.Where(q => q.CanUnsubscribe);
            }

            var subscribedIds = subscribedContracts.Select(q => q.ContractId);

            if (onlyOwned)
            {
                query = query.Where(q => ownedBaseNames.Contains(q.BaseName));
            }
            else if (excludeOwned)
            {
                query = query.Where(q => subscribedIds.Contains(q.Id) && !ownedBaseNames.Contains(q.BaseName));
            }
            else
            {
                query = query.Where(q => subscribedIds.Contains(q.Id) || ownedBaseNames.Contains(q.BaseName));
            }
        }
        else
        {
            throw new KromerException(ErrorCode.InvalidParameter);
        }

        var total = await query.CountAsync();
        var contracts = await query
            .OrderBy(q => q.Id)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();

        var dtos = new List<SubscriptionDto>();
        foreach (var contract in contracts)
        {
            dtos.Add(await BuildDtoAsync(contract, contextAddress));
        }

        return new SubscriptionListResponse
        {
            Count = dtos.Count,
            Total = total,
            Subscriptions = dtos,
        };
    }

    public async Task<SubscribeResponse> SubscribeAsync(int contractId, string? privateKey)
    {
        AssertPrivateKey(privateKey);

        var subscriber = await walletRepository.GetWalletFromKeyAsync(privateKey!);
        if (subscriber is null)
        {
            throw new KromerException(ErrorCode.AuthenticationFailed);
        }

        var contract = await context.SubscriptionContracts.FirstOrDefaultAsync(q =>
            q.Id == contractId && q.Status == SubscriptionStatus.Active);
        if (contract is null)
        {
            throw new KromerException(ErrorCode.ResourceNotFound);
        }

        var existing = await context.WalletSubscriptions.FirstOrDefaultAsync(q =>
            q.ContractId == contract.Id &&
            q.WalletAddress == subscriber.Address &&
            q.Status == SubscriptionStatus.Active);
        if (existing is not null)
        {
            return new SubscribeResponse
            {
                NextPayment = existing.NextPayment,
            };
        }

        var owner = await ResolveCurrentOwnerWalletAsync(contract.BaseName);
        if (owner.Address == subscriber.Address)
        {
            throw new KromerException(ErrorCode.SameWalletTransfer);
        }

        var now = DateTime.UtcNow;
        var subscription = new WalletSubscriptionEntity
        {
            ContractId = contract.Id,
            WalletAddress = subscriber.Address,
            NextPayment = now.AddMinutes(contract.PeriodMinutes),
            Status = SubscriptionStatus.Active,
            CanUnsubscribe = true,
            CreatedAt = now,
        };

        await context.WalletSubscriptions.AddAsync(subscription);
        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            context.Entry(subscription).State = EntityState.Detached;
            existing = await context.WalletSubscriptions.FirstOrDefaultAsync(q =>
                q.ContractId == contract.Id &&
                q.WalletAddress == subscriber.Address &&
                q.Status == SubscriptionStatus.Active);

            if (existing is not null)
            {
                return new SubscribeResponse
                {
                    NextPayment = existing.NextPayment,
                };
            }

            throw;
        }

        try
        {
            await ChargeSubscriptionAsync(contract, subscription, subscriber, owner, now);
        }
        catch (KristException ex) when (ex.Code == ErrorCode.InsufficientFunds)
        {
            CancelWalletSubscription(subscription, ReasonInsufficientFunds, DateTime.UtcNow);
            await context.SaveChangesAsync();
            await EmitSubscriptionEventAsync("payment_failed", contract, subscription, owner.Address,
                SubscriptionStatus.Cancelled, ReasonInsufficientFunds);
            throw;
        }

        await EmitSubscriptionEventAsync("subscribe", contract, subscription, owner.Address, SubscriptionStatus.Active);

        return new SubscribeResponse
        {
            NextPayment = subscription.NextPayment,
        };
    }

    public async Task UnsubscribeAsync(int contractId, string? privateKey)
    {
        AssertPrivateKey(privateKey);

        var subscriber = await walletRepository.GetWalletFromKeyAsync(privateKey!);
        if (subscriber is null)
        {
            throw new KromerException(ErrorCode.AuthenticationFailed);
        }

        var contract = await context.SubscriptionContracts.FirstOrDefaultAsync(q => q.Id == contractId);
        if (contract is null)
        {
            throw new KromerException(ErrorCode.ResourceNotFound);
        }

        var subscription = await context.WalletSubscriptions.FirstOrDefaultAsync(q =>
            q.ContractId == contract.Id &&
            q.WalletAddress == subscriber.Address &&
            q.Status == SubscriptionStatus.Active);
        if (subscription is null)
        {
            return;
        }

        if (!subscription.CanUnsubscribe)
        {
            throw new KromerException(ErrorCode.InvalidParameter);
        }

        CancelWalletSubscription(subscription, ReasonUnsubscribed, DateTime.UtcNow);
        await context.SaveChangesAsync();

        var ownerAddress = await ResolveCurrentOwnerAddressAsync(contract.BaseName, throwIfMissing: false);
        await EmitSubscriptionEventAsync("unsubscribe", contract, subscription, ownerAddress,
            SubscriptionStatus.Cancelled, ReasonUnsubscribed);
    }

    public async Task<int> BillDueSubscriptionsAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var subscriptions = await context.WalletSubscriptions
            .Include(q => q.Contract)
            .Where(q =>
                q.Status == SubscriptionStatus.Active &&
                q.Contract.Status == SubscriptionStatus.Active &&
                q.NextPayment <= now)
            .OrderBy(q => q.NextPayment)
            .Take(100)
            .ToListAsync(cancellationToken);

        foreach (var subscription in subscriptions)
        {
            try
            {
                await BillSubscriptionAsync(subscription, now, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process subscription {SubscriptionId} for contract {ContractId}",
                    subscription.Id, subscription.ContractId);
            }
        }

        return subscriptions.Count;
    }

    private async Task BillSubscriptionAsync(WalletSubscriptionEntity subscription, DateTime now,
        CancellationToken cancellationToken)
    {
        var contract = subscription.Contract;
        var ownerAddress = await ResolveCurrentOwnerAddressAsync(contract.BaseName, throwIfMissing: false);
        if (ownerAddress is null)
        {
            await CancelDueSubscriptionAsync(contract, subscription, null, ReasonOwnerMissing, cancellationToken);
            return;
        }

        var owner = await walletRepository.GetWalletFromAddress(ownerAddress);
        if (owner is null)
        {
            await CancelDueSubscriptionAsync(contract, subscription, ownerAddress, ReasonOwnerMissing,
                cancellationToken);
            return;
        }

        var subscriber = await walletRepository.GetWalletFromAddress(subscription.WalletAddress);
        if (subscriber is null)
        {
            await CancelDueSubscriptionAsync(contract, subscription, ownerAddress, ReasonSubscriberMissing,
                cancellationToken);
            return;
        }

        if (subscriber.Address == owner.Address)
        {
            await CancelDueSubscriptionAsync(contract, subscription, ownerAddress, ReasonSameWallet,
                cancellationToken);
            return;
        }

        try
        {
            await ChargeSubscriptionAsync(contract, subscription, subscriber, owner, now);
        }
        catch (KristException ex) when (ex.Code == ErrorCode.InsufficientFunds)
        {
            await CancelDueSubscriptionAsync(contract, subscription, ownerAddress, ReasonInsufficientFunds,
                cancellationToken);
        }
    }

    private async Task CancelDueSubscriptionAsync(SubscriptionContractEntity contract,
        WalletSubscriptionEntity subscription, string? ownerAddress, string reason, CancellationToken cancellationToken)
    {
        CancelWalletSubscription(subscription, reason, DateTime.UtcNow);
        await context.SaveChangesAsync(cancellationToken);
        await EmitSubscriptionEventAsync("payment_failed", contract, subscription, ownerAddress,
            SubscriptionStatus.Cancelled, reason);
    }

    private async Task ChargeSubscriptionAsync(SubscriptionContractEntity contract, WalletSubscriptionEntity subscription,
        WalletEntity subscriber, WalletEntity owner, DateTime now)
    {
        subscription.NextPayment = now.AddMinutes(contract.PeriodMinutes);
        context.WalletSubscriptions.Update(subscription);

        var transaction = transactionService.InitiateTransaction(subscriber, owner, contract.Price);
        transaction.Metadata = $"subscription={contract.Id};wallet_subscription={subscription.Id}";

        await transactionService.CommitTransactionAsync(subscriber, owner, transaction);

        await eventChannel.Writer.WriteAsync(new KristTransactionEvent
        {
            Transaction = TransactionDto.FromEntity(transaction),
        });

        await EmitSubscriptionEventAsync("payment_success", contract, subscription, owner.Address,
            SubscriptionStatus.Active);
    }

    private async Task<SubscriptionDto> BuildDtoAsync(SubscriptionContractEntity contract, string? address)
    {
        var subscribers = await context.WalletSubscriptions.CountAsync(q =>
            q.ContractId == contract.Id && q.Status == SubscriptionStatus.Active);

        var dto = new SubscriptionDto
        {
            Id = contract.Id,
            Description = contract.Description,
            Price = contract.Price,
            Period = contract.PeriodMinutes,
            Name = contract.Receiver,
            Subscribers = subscribers,
            Status = contract.Status,
        };

        if (string.IsNullOrWhiteSpace(address))
        {
            return dto;
        }

        var subscription = await context.WalletSubscriptions.FirstOrDefaultAsync(q =>
            q.ContractId == contract.Id &&
            q.WalletAddress == address &&
            q.Status == SubscriptionStatus.Active);
        var ownerAddress = await ResolveCurrentOwnerAddressAsync(contract.BaseName, throwIfMissing: false);

        dto.Subscribed = subscription is not null;
        dto.Owns = ownerAddress == address;
        dto.NextPayment = subscription?.NextPayment;
        dto.Unsubscribable = subscription?.CanUnsubscribe;

        return dto;
    }

    private async Task<WalletEntity> AuthenticateCurrentOwnerAsync(string? privateKey, string baseName)
    {
        AssertPrivateKey(privateKey);

        var wallet = await walletRepository.GetWalletFromKeyAsync(privateKey!);
        if (wallet is null)
        {
            throw new KromerException(ErrorCode.AuthenticationFailed);
        }

        var ownerAddress = await ResolveCurrentOwnerAddressAsync(baseName, throwIfMissing: true);
        if (ownerAddress != wallet.Address)
        {
            throw new KromerException(ErrorCode.NotNameOwner);
        }

        return wallet;
    }

    private async Task<WalletEntity> ResolveCurrentOwnerWalletAsync(string baseName)
    {
        var ownerAddress = await ResolveCurrentOwnerAddressAsync(baseName, throwIfMissing: true);
        var wallet = await walletRepository.GetWalletFromAddress(ownerAddress!);
        if (wallet is null)
        {
            throw new KromerException(ErrorCode.AddressNotFound);
        }

        return wallet;
    }

    private async Task<string?> ResolveCurrentOwnerAddressAsync(string baseName, bool throwIfMissing)
    {
        var name = await context.Names.FirstOrDefaultAsync(q => q.Name == baseName);
        if (name is null)
        {
            if (throwIfMissing)
            {
                throw new KromerException(ErrorCode.NameNotFound);
            }

            return null;
        }

        return name.Owner;
    }

    private async Task<NormalizedReceiver> NormalizeReceiverAsync(string? value, bool requireExisting)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new KromerException(ErrorCode.InvalidParameter);
        }

        value = value.Trim().ToLowerInvariant();
        if (Validation.IsValidAddress(value))
        {
            throw new KromerException(ErrorCode.InvalidParameter);
        }

        string receiver;
        string baseName;
        var isMetaname = false;

        if (value.EndsWith(".kro"))
        {
            var parsed = Validation.ParseMetaName(value);
            if (!parsed.Valid || !Validation.IsNameValid(parsed.Name))
            {
                throw new KromerException(ErrorCode.InvalidParameter);
            }

            baseName = Validation.SanitizeName(parsed.Name);
            var meta = parsed.Meta?.Trim().ToLowerInvariant();
            isMetaname = !string.IsNullOrWhiteSpace(meta);
            receiver = isMetaname ? $"{meta}@{baseName}.kro" : $"{baseName}.kro";
        }
        else
        {
            if (value.Contains('@') || !Validation.IsNameValid(value))
            {
                throw new KromerException(ErrorCode.InvalidParameter);
            }

            baseName = Validation.SanitizeName(value);
            receiver = $"{baseName}.kro";
        }

        if (requireExisting && !await context.Names.AnyAsync(q => q.Name == baseName))
        {
            throw new KromerException(ErrorCode.NameNotFound);
        }

        return new NormalizedReceiver(receiver, baseName, isMetaname);
    }

    private static void AssertCreateRequest(CreateSubscriptionRequest? request)
    {
        if (request is null ||
            string.IsNullOrWhiteSpace(request.PrivateKey) ||
            string.IsNullOrWhiteSpace(request.Name) ||
            string.IsNullOrWhiteSpace(request.Description) ||
            request.Description.Trim().Length > 255 ||
            request.Period < 1 ||
            decimal.Round(request.Price, 5, MidpointRounding.ToEven) <= 0)
        {
            throw new KromerException(ErrorCode.InvalidParameter);
        }
    }

    private static void AssertPrivateKey(string? privateKey)
    {
        if (string.IsNullOrWhiteSpace(privateKey))
        {
            throw new KromerException(ErrorCode.InvalidParameter);
        }
    }

    private static void CancelWalletSubscription(WalletSubscriptionEntity subscription, string reason, DateTime now)
    {
        subscription.Status = SubscriptionStatus.Cancelled;
        subscription.CancellationReason = reason;
        subscription.CancelledAt = now;
    }

    private async Task EmitSubscriptionEventAsync(string action, SubscriptionContractEntity contract,
        WalletSubscriptionEntity? subscription, string? ownerAddress, SubscriptionStatus status, string? reason = null)
    {
        await eventChannel.Writer.WriteAsync(new KromerSubscriptionEvent
        {
            Action = action,
            ContractId = contract.Id,
            SubscriptionId = subscription?.Id,
            OwnerAddress = ownerAddress,
            SubscriberAddress = subscription?.WalletAddress,
            Status = SnakeCaseNamingPolicy.Convert(status.ToString()),
            Reason = reason,
            NextPayment = subscription?.Status == SubscriptionStatus.Active ? subscription.NextPayment : null,
        });
    }

    private sealed record NormalizedReceiver(string Receiver, string BaseName, bool IsMetaname);
}
