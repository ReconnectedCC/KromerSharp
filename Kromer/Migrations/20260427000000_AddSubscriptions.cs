using Kromer.Data;
using Kromer.Models.Entities;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Kromer.Migrations;

[DbContext(typeof(KromerContext))]
[Migration("20260427000000_AddSubscriptions")]
public partial class AddSubscriptions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DO $$
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'subscription_status') THEN
                    CREATE TYPE public.subscription_status AS ENUM ('active', 'closed', 'cancelled');
                ELSIF NOT EXISTS (
                    SELECT 1
                    FROM pg_enum e
                    JOIN pg_type t ON t.oid = e.enumtypid
                    WHERE t.typname = 'subscription_status' AND e.enumlabel = 'closed'
                ) THEN
                    ALTER TYPE public.subscription_status ADD VALUE 'closed' BEFORE 'cancelled';
                END IF;
            END
            $$;
            """);

        migrationBuilder.CreateTable(
            name: "subscription_contracts",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                receiver = table.Column<string>(type: "character varying(98)", maxLength: 98, nullable: false),
                base_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                price = table.Column<decimal>(type: "numeric(16,5)", precision: 16, scale: 5, nullable: false),
                period_minutes = table.Column<int>(type: "integer", nullable: false),
                description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                max_subscribers = table.Column<int>(type: "integer", nullable: true),
                allowed_subscriber_addresses = table.Column<string[]>(type: "text[]", nullable: true),
                status = table.Column<SubscriptionStatus>(type: "public.subscription_status", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                cancelled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("subscription_contracts_pkey", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "wallet_subscriptions",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                contract_id = table.Column<int>(type: "integer", nullable: false),
                wallet_address = table.Column<string>(type: "character(10)", fixedLength: true, maxLength: 10, nullable: false),
                next_payment = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                status = table.Column<SubscriptionStatus>(type: "public.subscription_status", nullable: false),
                cancellation_reason = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                can_unsubscribe = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                cancelled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("wallet_subscriptions_pkey", x => x.id);
                table.ForeignKey(
                    name: "wallet_subscriptions_contract_id_fkey",
                    column: x => x.contract_id,
                    principalTable: "subscription_contracts",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "idx_subscription_contract_base_name",
            table: "subscription_contracts",
            column: "base_name");

        migrationBuilder.CreateIndex(
            name: "idx_subscription_contract_receiver",
            table: "subscription_contracts",
            column: "receiver");

        migrationBuilder.CreateIndex(
            name: "idx_subscription_contract_status",
            table: "subscription_contracts",
            column: "status");

        migrationBuilder.CreateIndex(
            name: "idx_wallet_subscription_contract_id",
            table: "wallet_subscriptions",
            column: "contract_id");

        migrationBuilder.CreateIndex(
            name: "idx_wallet_subscription_next_payment",
            table: "wallet_subscriptions",
            column: "next_payment");

        migrationBuilder.CreateIndex(
            name: "idx_wallet_subscription_wallet_address",
            table: "wallet_subscriptions",
            column: "wallet_address");

        migrationBuilder.CreateIndex(
            name: "unique_active_wallet_subscription",
            table: "wallet_subscriptions",
            columns: new[] { "contract_id", "wallet_address" },
            unique: true,
            filter: "status = 'active'");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "wallet_subscriptions");

        migrationBuilder.DropTable(
            name: "subscription_contracts");

        migrationBuilder.Sql("DROP TYPE IF EXISTS public.subscription_status;");
    }
}
