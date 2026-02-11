using Kromer.Models.Dto;
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

    public virtual DbSet<TransactionEntity> Transactions { get; set; }

    public virtual DbSet<WalletEntity> Wallets { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasPostgresEnum("transaction_type",
                new[] { "mined", "unknown", "name_purchase", "name_a_record", "name_transfer", "transfer" })
            .HasPostgresEnum("transaction_type_enum", new[] { "unknown", "mined", "name_purchase", "transfer" });

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
                .HasPrecision(16, 2)
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
                .HasPrecision(16, 2)
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
                .HasPrecision(16, 2)
                .HasColumnName("balance");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.Locked)
                .HasDefaultValue(false)
                .HasColumnName("locked");
            entity.Property(e => e.PrivateKey)
                .HasMaxLength(64)
                .HasColumnName("private_key");
            entity.Property(e => e.TotalIn)
                .HasPrecision(16, 2)
                .HasColumnName("total_in");
            entity.Property(e => e.TotalOut)
                .HasPrecision(16, 2)
                .HasColumnName("total_out");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}