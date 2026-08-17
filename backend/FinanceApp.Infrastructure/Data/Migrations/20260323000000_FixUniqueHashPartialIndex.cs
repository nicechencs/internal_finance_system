using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceApp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixUniqueHashPartialIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 删除旧的全量唯一索引（不含 is_deleted = false 过滤条件）
            migrationBuilder.DropIndex(
                name: "idx_bank_transactions_hash",
                table: "bank_transactions");

            // 创建部分唯一索引，仅对未删除的记录生效
            // 这样软删除后的数据可以重新导入，不会报唯一冲突
            migrationBuilder.CreateIndex(
                name: "idx_bank_transactions_hash",
                table: "bank_transactions",
                column: "unique_hash",
                unique: true,
                filter: "is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_bank_transactions_hash",
                table: "bank_transactions");

            migrationBuilder.CreateIndex(
                name: "idx_bank_transactions_hash",
                table: "bank_transactions",
                column: "unique_hash",
                unique: true);
        }
    }
}
