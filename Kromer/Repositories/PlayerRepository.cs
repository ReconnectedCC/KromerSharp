using System.Threading.Channels;
using Kromer.Data;
using Kromer.Models;
using Kromer.Models.Api.Internal;
using Kromer.Models.Dto;
using Kromer.Models.Entities;
using Kromer.Models.Exceptions;
using Kromer.Models.WebSocket.Events;
using Kromer.Services;
using Kromer.Utils;
using Microsoft.EntityFrameworkCore;

namespace Kromer.Repositories;

public class PlayerRepository(
    KromerContext context,
    ILogger<PlayerRepository> logger,
    WalletRepository walletRepository,
    IConfiguration configuration,
    TransactionService transactionService,
    Channel<IKristEvent> eventChannel)
{
    public decimal GetInitialBalance()
    {
        return configuration.GetValue("InitialBalance", 100m);
    }

    public async Task<PlayerEntity> GetOrCreatePlayer(Guid uuid, string? name = null)
    {
        var player = await context.Players.FirstOrDefaultAsync(q => q.Id == uuid);
        if (player is not null)
        {
            return player;
        }

        ArgumentException.ThrowIfNullOrEmpty(name);
        player = new PlayerEntity
        {
            Id = uuid,
            Name = name,
        };

        await context.Players.AddAsync(player);
        await context.SaveChangesAsync();

        return player;
    }

    public async Task<AddressCreationResponse> CreatePlayerWalletAsync(Guid uuid, string name)
    {
        var player = await GetOrCreatePlayer(uuid, name);
        player.OwnedWallets ??= [];
        string privateKey;
        WalletAuthenticationResult verification;

        // Ensure the key is unique to an address. This may be ridiculously rare, but never zero.
        do
        {
            privateKey = Crypto.GenerateSecurePassword();
            verification = await walletRepository.VerifyAddressAsync(privateKey);
        } while (!verification.Authed);

        var wallet = verification.Wallet;
        if (wallet is null)
        {
            throw new InvalidOperationException("Wallet was not created.");
        }

        wallet.Balance = 100m;
        context.Entry(wallet).State = EntityState.Modified;

        player.OwnedWallets.Add(wallet.Id);
        context.Entry(player).State = EntityState.Modified;

        await context.SaveChangesAsync();

        return new AddressCreationResponse
        {
            Address = wallet.Address,
            PrivateKey = privateKey,
        };
    }

    public async Task<WalletResponse> GiveMoneyAsync(string address, decimal amount)
    {
        var wallet = await walletRepository.GetWalletFromAddress(address);
        if (wallet is null)
        {
            throw new KristException(ErrorCode.AddressNotFound);
        }

        var transaction = await transactionService.CreateTransactionAsync(new TransactionEntity()
        {
            Amount = amount,
            From = TransactionService.ServerWallet,
            To = address,
            TransactionType = TransactionType.Mined,
            Date = DateTime.UtcNow,
        });

        await context.SaveChangesAsync();
        
        // Emit transaction event
        await eventChannel.Writer.WriteAsync(new KristTransactionEvent
        {
            Transaction = TransactionDto.FromEntity(transaction),
        });

        return new WalletResponse
        {
            Wallet = WalletDto.FromEntity(wallet),
        };
    }

    public async Task<WalletsResponse> GetWalletByPlayerAsync(Guid uuid)
    {
        var player = await context.Players.FirstOrDefaultAsync(q => q.Id == uuid);
        if (player is null)
        {
            throw new KromerException(ErrorCode.PlayerError);
        }

        var wallets = await walletRepository.GetPlayerWalletsAsync(uuid);

        return new WalletsResponse
        {
            Wallet = wallets,
        };
    }
}