using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceApp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAllocationStatusAndPayableType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. 创建 payable_types 表
            migrationBuilder.CreateTable(
                name: "payable_types",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_by = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payable_types", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_payable_types_code",
                table: "payable_types",
                column: "code",
                unique: true,
                filter: "code IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_payable_types_sort_order",
                table: "payable_types",
                column: "sort_order");

            // 2. 初始化预置业务类型
            migrationBuilder.InsertData(
                table: "payable_types",
                columns: new[] { "name", "code", "description", "is_active", "sort_order", "created_at", "updated_at", "is_deleted" },
                values: new object[,]
                {
                    { "项目成本支出", "PROJECT_COST", "项目相关的成本支出", true, 1, DateTime.UtcNow, DateTime.UtcNow, false },
                    { "人员费用", "PERSONNEL_EXPENSE", "人员工资、福利等费用", true, 2, DateTime.UtcNow, DateTime.UtcNow, false },
                    { "人员开发费用成本", "DEV_PERSONNEL_COST", "开发人员相关成本", true, 3, DateTime.UtcNow, DateTime.UtcNow, false },
                    { "外包费用", "OUTSOURCING_FEE", "外包服务费用", true, 4, DateTime.UtcNow, DateTime.UtcNow, false },
                    { "其他", "OTHER", "其他类型费用", true, 99, DateTime.UtcNow, DateTime.UtcNow, false }
                });

            // 3. 为 transactions 表添加 allocation_status 字段
            migrationBuilder.AddColumn<string>(
                name: "allocation_status",
                table: "transactions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "unallocated");

            migrationBuilder.CreateIndex(
                name: "idx_transactions_allocation_status",
                table: "transactions",
                column: "allocation_status",
                filter: "is_deleted = false");

            // 4. 为 payables 表添加 payable_type_id 字段
            migrationBuilder.AddColumn<long>(
                name: "payable_type_id",
                table: "payables",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "idx_payables_payable_type",
                table: "payables",
                column: "payable_type_id");

            migrationBuilder.AddForeignKey(
                name: "FK_payables_payable_types_payable_type_id",
                table: "payables",
                column: "payable_type_id",
                principalTable: "payable_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            // 5. 计算现有交易的分配状态
            migrationBuilder.Sql(@"
                UPDATE transactions t
                SET allocation_status = CASE
                    WHEN (
                        SELECT COALESCE(SUM(rd.amount), 0)
                        FROM receivable_details rd
                        WHERE rd.transaction_id = t.id AND rd.deleted_at IS NULL
                    ) + (
                        SELECT COALESCE(SUM(pd.amount), 0)
                        FROM payable_details pd
                        WHERE pd.transaction_id = t.id AND pd.deleted_at IS NULL
                    ) >= t.amount THEN 'fullyallocated'
                    WHEN (
                        SELECT COALESCE(SUM(rd.amount), 0)
                        FROM receivable_details rd
                        WHERE rd.transaction_id = t.id AND rd.deleted_at IS NULL
                    ) + (
                        SELECT COALESCE(SUM(pd.amount), 0)
                        FROM payable_details pd
                        WHERE pd.transaction_id = t.id AND pd.deleted_at IS NULL
                    ) > 0 THEN 'partiallyallocated'
                    ELSE 'unallocated'
                END
                WHERE t.deleted_at IS NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 移除外键
            migrationBuilder.DropForeignKey(
                name: "FK_payables_payable_types_payable_type_id",
                table: "payables");

            // 移除索引
            migrationBuilder.DropIndex(
                name: "idx_payables_payable_type",
                table: "payables");

            migrationBuilder.DropIndex(
                name: "idx_transactions_allocation_status",
                table: "transactions");

            // 移除列
            migrationBuilder.DropColumn(
                name: "payable_type_id",
                table: "payables");

            migrationBuilder.DropColumn(
                name: "allocation_status",
                table: "transactions");

            // 删除表
            migrationBuilder.DropTable(
                name: "payable_types");
        }
    }
}

