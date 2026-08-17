// 注意：此迁移为手动创建，无对应 .Designer.cs 文件。
// 应用方式：dotnet ef database update（仅执行 Up/Down，不影响运行时迁移应用）
// 如需生成完整 Designer 文件，可删除此文件后运行 dotnet ef migrations add AddBankTransactionDescription
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceApp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBankTransactionDescription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "bank_transactions",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "description",
                table: "bank_transactions");
        }
    }
}
