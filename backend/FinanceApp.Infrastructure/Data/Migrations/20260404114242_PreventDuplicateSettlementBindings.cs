using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceApp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class PreventDuplicateSettlementBindings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                WITH duplicate_groups AS (
                    SELECT receivable_id,
                           transaction_id,
                           MIN(id) AS keep_id,
                           SUM(amount) AS total_amount,
                           MAX(updated_at) AS max_updated_at
                    FROM receivable_details
                    WHERE is_deleted = false
                    GROUP BY receivable_id, transaction_id
                    HAVING COUNT(*) > 1
                )
                UPDATE receivable_details AS rd
                SET amount = duplicate_groups.total_amount,
                    updated_at = GREATEST(rd.updated_at, duplicate_groups.max_updated_at, timezone('UTC', now()))
                FROM duplicate_groups
                WHERE rd.id = duplicate_groups.keep_id;
                """);

            migrationBuilder.Sql(
                """
                WITH duplicate_groups AS (
                    SELECT receivable_id,
                           transaction_id,
                           MIN(id) AS keep_id
                    FROM receivable_details
                    WHERE is_deleted = false
                    GROUP BY receivable_id, transaction_id
                    HAVING COUNT(*) > 1
                )
                UPDATE receivable_details AS rd
                SET is_deleted = true,
                    deleted_at = COALESCE(rd.deleted_at, timezone('UTC', now())),
                    updated_at = GREATEST(rd.updated_at, timezone('UTC', now()))
                FROM duplicate_groups
                WHERE rd.receivable_id = duplicate_groups.receivable_id
                  AND rd.transaction_id = duplicate_groups.transaction_id
                  AND rd.id <> duplicate_groups.keep_id
                  AND rd.is_deleted = false;
                """);

            migrationBuilder.Sql(
                """
                WITH duplicate_groups AS (
                    SELECT payable_id,
                           transaction_id,
                           MIN(id) AS keep_id,
                           SUM(amount) AS total_amount,
                           MAX(updated_at) AS max_updated_at
                    FROM payable_details
                    WHERE is_deleted = false
                    GROUP BY payable_id, transaction_id
                    HAVING COUNT(*) > 1
                )
                UPDATE payable_details AS pd
                SET amount = duplicate_groups.total_amount,
                    updated_at = GREATEST(pd.updated_at, duplicate_groups.max_updated_at, timezone('UTC', now()))
                FROM duplicate_groups
                WHERE pd.id = duplicate_groups.keep_id;
                """);

            migrationBuilder.Sql(
                """
                WITH duplicate_groups AS (
                    SELECT payable_id,
                           transaction_id,
                           MIN(id) AS keep_id
                    FROM payable_details
                    WHERE is_deleted = false
                    GROUP BY payable_id, transaction_id
                    HAVING COUNT(*) > 1
                )
                UPDATE payable_details AS pd
                SET is_deleted = true,
                    deleted_at = COALESCE(pd.deleted_at, timezone('UTC', now())),
                    updated_at = GREATEST(pd.updated_at, timezone('UTC', now()))
                FROM duplicate_groups
                WHERE pd.payable_id = duplicate_groups.payable_id
                  AND pd.transaction_id = duplicate_groups.transaction_id
                  AND pd.id <> duplicate_groups.keep_id
                  AND pd.is_deleted = false;
                """);

            migrationBuilder.CreateIndex(
                name: "ux_receivable_details_receivable_transaction",
                table: "receivable_details",
                columns: new[] { "receivable_id", "transaction_id" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ux_payable_details_payable_transaction",
                table: "payable_details",
                columns: new[] { "payable_id", "transaction_id" },
                unique: true,
                filter: "is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_receivable_details_receivable_transaction",
                table: "receivable_details");

            migrationBuilder.DropIndex(
                name: "ux_payable_details_payable_transaction",
                table: "payable_details");
        }
    }
}
