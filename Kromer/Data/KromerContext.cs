using Kromer.Models.Dto;
using Kromer.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kromer.Data;

public partial class KromerContext : DbContext
{
    public KromerContext()
    {
    }

    public KromerContext(DbContextOptions<KromerContext> options)
        : base(options)
    {
    }

    public virtual DbSet<NameEntity> Names { get; set; }

    public virtual DbSet<PlayerEntity> Players { get; set; }

    public virtual DbSet<SubscriptionContractEntity> SubscriptionContracts { get; set; }

    public virtual DbSet<TransactionEntity> Transactions { get; set; }

    public virtual DbSet<WalletEntity> Wallets { get; set; }

    public virtual DbSet<WalletSubscriptionEntity> WalletSubscriptions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasPostgresEnum("transaction_type",
                new[] { "mined", "unknown", "name_purchase", "name_a_record", "name_transfer", "transfer" })
            .HasPostgresEnum("transaction_type_enum", new[] { "unknown", "mined", "name_purchase", "transfer" })
            .HasPostgresEnum("subscription_status", new[] { "active", "cancelled" });

        modelBuilder.Entity<NameEntity>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("names_pkey");

            entity.ToTable("names");

            entity.HasIndex(e => e.Name, "unique_name_index").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.LastTransfered).HasColumnName("last_transfered");
            entity.Property(e => e.LastUpdated).HasColumnName("last_updated");
            entity.Property(e => e.Metadata)
                .HasMaxLength(255)
                .HasColumnName("metadata");
            entity.Property(e => e.Name)
                .HasMaxLength(64)
                .HasColumnName("name");
            entity.Property(e => e.OriginalOwner)
                .HasMaxLength(10)
                .IsFixedLength()
                .HasColumnName("original_owner");
            entity.Property(e => e.Owner)
                .HasMaxLength(10)
                .IsFixedLength()
                .HasColumnName("owner");
            entity.Property(e => e.TimeRegistered).HasColumnName("time_registered");
            entity.Property(e => e.Unpaid)
                .HasPrecision(16, 5)
                .HasColumnName("unpaid");
        });

        modelBuilder.Entity<PlayerEntity>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("players_pkey");

            entity.ToTable("players");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(16)
                .HasColumnName("name");
            entity.Property(e => e.OwnedWallets)
                .HasDefaultValueSql("ARRAY[]::integer[]")
                .HasColumnName("owned_wallets");
        });

        modelBuilder.Entity<TransactionEntity>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("transactions_pkey");

            entity.ToTable("transactions");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Amount)
                .HasPrecision(16, 5)
                .HasColumnName("amount");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.From)
                .HasMaxLength(10)
                .IsFixedLength()
                .HasColumnName("from");
            entity.Property(e => e.Metadata)
                .HasMaxLength(512)
                .HasColumnName("metadata");
            entity.Property(e => e.Name)
                .HasMaxLength(128)
                .HasColumnName("name");
            entity.Property(e => e.SentMetaname)
                .HasMaxLength(32)
                .HasColumnName("sent_metaname");
            entity.Property(e => e.SentName)
                .HasMaxLength(32)
                .HasColumnName("sent_name");
            entity.Property(e => e.To)
                .HasMaxLength(10)
                .IsFixedLength()
                .HasColumnName("to");
            entity.Property(e => e.TransactionType)
                .HasColumnName("transaction_type")
                .HasColumnType("public.transaction_type");
        });

        modelBuilder.Entity<SubscriptionContractEntity>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("subscription_contracts_pkey");

            entity.ToTable("subscription_contracts");

            entity.HasIndex(e => e.BaseName, "idx_subscription_contract_base_name");
            entity.HasIndex(e => e.Receiver, "idx_subscription_contract_receiver");
            entity.HasIndex(e => e.Status, "idx_subscription_contract_status");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Receiver)
                .HasMaxLength(98)
                .HasColumnName("receiver");
            entity.Property(e => e.BaseName)
                .HasMaxLength(64)
                .HasColumnName("base_name");
            entity.Property(e => e.Price)
                .HasPrecision(16, 5)
                .HasColumnName("price");
            entity.Property(e => e.PeriodMinutes).HasColumnName("period_minutes");
            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .HasColumnName("description");
            entity.Property(e => e.Status)
                .HasColumnName("status")
                .HasColumnType("public.subscription_status");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.CancelledAt).HasColumnName("cancelled_at");
        });

        modelBuilder.Entity<WalletEntity>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("wallets_pkey");

            entity.ToTable("wallets");

            entity.HasIndex(e => e.Address, "idx_wallet_address").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Address)
                .HasMaxLength(10)
                .IsFixedLength()
                .HasColumnName("address");
            entity.Property(e => e.Balance)
                .HasPrecision(16, 5)
                .HasColumnName("balance");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.Locked)
                .HasDefaultValue(false)
                .HasColumnName("locked");
            entity.Property(e => e.PrivateKey)
                .HasMaxLength(64)
                .HasColumnName("private_key");
            entity.Property(e => e.TotalIn)
                .HasPrecision(16, 5)
                .HasColumnName("total_in");
            entity.Property(e => e.TotalOut)
                .HasPrecision(16, 5)
                .HasColumnName("total_out");
        });

        modelBuilder.Entity<WalletSubscriptionEntity>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("wallet_subscriptions_pkey");

            entity.ToTable("wallet_subscriptions");

            entity.HasIndex(e => e.ContractId, "idx_wallet_subscription_contract_id");
            entity.HasIndex(e => e.WalletAddress, "idx_wallet_subscription_wallet_address");
            entity.HasIndex(e => e.NextPayment, "idx_wallet_subscription_next_payment");
            entity.HasIndex(e => new { e.ContractId, e.WalletAddress }, "unique_active_wallet_subscription")
                .IsUnique()
                .HasFilter("status = 'active'");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ContractId).HasColumnName("contract_id");
            entity.Property(e => e.WalletAddress)
                .HasMaxLength(10)
                .IsFixedLength()
                .HasColumnName("wallet_address");
            entity.Property(e => e.NextPayment).HasColumnName("next_payment");
            entity.Property(e => e.Status)
                .HasColumnName("status")
                .HasColumnType("public.subscription_status");
            entity.Property(e => e.CancellationReason)
                .HasMaxLength(64)
                .HasColumnName("cancellation_reason");
            entity.Property(e => e.CanUnsubscribe)
                .HasDefaultValue(true)
                .HasColumnName("can_unsubscribe");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.CancelledAt).HasColumnName("cancelled_at");

            entity.HasOne(e => e.Contract)
                .WithMany(e => e.WalletSubscriptions)
                .HasForeignKey(e => e.ContractId)
                .HasConstraintName("wallet_subscriptions_contract_id_fkey")
                .OnDelete(DeleteBehavior.Cascade);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
