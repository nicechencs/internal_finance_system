using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FinanceApp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFixedDepositRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "fixed_deposit_records",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    account_id = table.Column<long>(type: "bigint", nullable: false),
                    principal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    deposit_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    maturity_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    term_months = table.Column<int>(type: "integer", nullable: false),
                    interest_rate = table.Column<decimal>(type: "numeric(8,4)", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    withdrawal_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    actual_interest = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    is_early_withdrawal = table.Column<bool>(type: "boolean", nullable: false),
                    deposit_transaction_id = table.Column<long>(type: "bigint", nullable: false),
                    withdrawal_transaction_id = table.Column<long>(type: "bigint", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    created_by = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fixed_deposit_records", x => x.id);
                    table.ForeignKey(
                        name: "FK_fixed_deposit_records_accounts_account_id",
                        column: x => x.account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_fixed_deposit_records_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "idx_fixed_deposit_records_account_id",
                table: "fixed_deposit_records",
                column: "account_id",
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "idx_fixed_deposit_records_created_by",
                table: "fixed_deposit_records",
                column: "created_by",
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "idx_fixed_deposit_records_maturity_date",
                table: "fixed_deposit_records",
                column: "maturity_date",
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "idx_fixed_deposit_records_status",
                table: "fixed_deposit_records",
                column: "status",
                filter: "is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fixed_deposit_records");
        }
    }
}
