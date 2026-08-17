using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FinanceApp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class MakeSettlementTransactionIdRequired : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM receivable_details
                        WHERE transaction_id IS NULL
                          AND deleted_at IS NULL
                    ) THEN
                        RAISE EXCEPTION 'Cannot apply MakeSettlementTransactionIdRequired: receivable_details contains active rows without transaction_id.';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM payable_details
                        WHERE transaction_id IS NULL
                          AND deleted_at IS NULL
                    ) THEN
                        RAISE EXCEPTION 'Cannot apply MakeSettlementTransactionIdRequired: payable_details contains active rows without transaction_id.';
                    END IF;
                END
                $$;
                """);

            // 回填已软删除记录中的 NULL TransactionId，避免 ALTER COLUMN SET NOT NULL 失败
            migrationBuilder.Sql(
                """
                UPDATE receivable_details SET transaction_id = 0 WHERE transaction_id IS NULL AND deleted_at IS NOT NULL;
                UPDATE payable_details SET transaction_id = 0 WHERE transaction_id IS NULL AND deleted_at IS NOT NULL;
                """);

            migrationBuilder.AlterColumn<long>(
                name: "transaction_id",
                table: "receivable_details",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "transaction_id",
                table: "payable_details",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "transaction_id",
                table: "receivable_details",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<long>(
                name: "transaction_id",
                table: "payable_details",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");
        }
    }
}
