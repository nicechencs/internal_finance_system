using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceApp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCounterpartyTypeToReceivablePayable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // === Receivable: 添加 SupplierId 和 PersonId 列，CustomerId 改为可空 ===

            // 添加新列
            migrationBuilder.AddColumn<long>(
                name: "supplier_id",
                table: "receivables",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "person_id",
                table: "receivables",
                type: "bigint",
                nullable: true);

            // CustomerId 从 NOT NULL 改为 NULL
            migrationBuilder.AlterColumn<long>(
                name: "customer_id",
                table: "receivables",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            // 添加索引
            migrationBuilder.CreateIndex(
                name: "idx_receivables_supplier",
                table: "receivables",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "idx_receivables_person",
                table: "receivables",
                column: "person_id");

            // 添加外键
            migrationBuilder.AddForeignKey(
                name: "FK_receivables_suppliers_supplier_id",
                table: "receivables",
                column: "supplier_id",
                principalTable: "suppliers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_receivables_persons_person_id",
                table: "receivables",
                column: "person_id",
                principalTable: "persons",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddCheckConstraint(
                name: "CK_receivables_exactly_one_counterparty",
                table: "receivables",
                sql: "(CASE WHEN customer_id IS NOT NULL THEN 1 ELSE 0 END + CASE WHEN supplier_id IS NOT NULL THEN 1 ELSE 0 END + CASE WHEN person_id IS NOT NULL THEN 1 ELSE 0 END) = 1");

            // === Payable: 添加 CustomerId 和 PersonId 列，SupplierId 改为可空 ===

            // 添加新列
            migrationBuilder.AddColumn<long>(
                name: "customer_id",
                table: "payables",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "person_id",
                table: "payables",
                type: "bigint",
                nullable: true);

            // SupplierId 从 NOT NULL 改为 NULL
            migrationBuilder.AlterColumn<long>(
                name: "supplier_id",
                table: "payables",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            // 添加索引
            migrationBuilder.CreateIndex(
                name: "idx_payables_customer",
                table: "payables",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "idx_payables_person",
                table: "payables",
                column: "person_id");

            // 添加外键
            migrationBuilder.AddForeignKey(
                name: "FK_payables_customers_customer_id",
                table: "payables",
                column: "customer_id",
                principalTable: "customers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_payables_persons_person_id",
                table: "payables",
                column: "person_id",
                principalTable: "persons",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddCheckConstraint(
                name: "CK_payables_exactly_one_counterparty",
                table: "payables",
                sql: "(CASE WHEN supplier_id IS NOT NULL THEN 1 ELSE 0 END + CASE WHEN customer_id IS NOT NULL THEN 1 ELSE 0 END + CASE WHEN person_id IS NOT NULL THEN 1 ELSE 0 END) = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_payables_exactly_one_counterparty",
                table: "payables");

            migrationBuilder.DropCheckConstraint(
                name: "CK_receivables_exactly_one_counterparty",
                table: "receivables");

            // === Payable: 回滚 ===
            migrationBuilder.DropForeignKey(
                name: "FK_payables_customers_customer_id",
                table: "payables");

            migrationBuilder.DropForeignKey(
                name: "FK_payables_persons_person_id",
                table: "payables");

            migrationBuilder.DropIndex(
                name: "idx_payables_customer",
                table: "payables");

            migrationBuilder.DropIndex(
                name: "idx_payables_person",
                table: "payables");

            // SupplierId 恢复为 NOT NULL；若存在仅客户/人员对方的数据，则回滚会造成数据丢失，明确阻止
            migrationBuilder.Sql("DO $$ BEGIN IF EXISTS (SELECT 1 FROM payables WHERE supplier_id IS NULL) THEN RAISE EXCEPTION 'Cannot revert AddCounterpartyTypeToReceivablePayable: payables contains rows without supplier_id.'; END IF; END $$;");
            migrationBuilder.AlterColumn<long>(
                name: "supplier_id",
                table: "payables",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "customer_id",
                table: "payables");

            migrationBuilder.DropColumn(
                name: "person_id",
                table: "payables");

            // === Receivable: 回滚 ===
            migrationBuilder.DropForeignKey(
                name: "FK_receivables_suppliers_supplier_id",
                table: "receivables");

            migrationBuilder.DropForeignKey(
                name: "FK_receivables_persons_person_id",
                table: "receivables");

            migrationBuilder.DropIndex(
                name: "idx_receivables_supplier",
                table: "receivables");

            migrationBuilder.DropIndex(
                name: "idx_receivables_person",
                table: "receivables");

            // CustomerId 恢复为 NOT NULL；若存在仅供应商/人员对方的数据，则回滚会造成数据丢失，明确阻止
            migrationBuilder.Sql("DO $$ BEGIN IF EXISTS (SELECT 1 FROM receivables WHERE customer_id IS NULL) THEN RAISE EXCEPTION 'Cannot revert AddCounterpartyTypeToReceivablePayable: receivables contains rows without customer_id.'; END IF; END $$;");
            migrationBuilder.AlterColumn<long>(
                name: "customer_id",
                table: "receivables",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "supplier_id",
                table: "receivables");

            migrationBuilder.DropColumn(
                name: "person_id",
                table: "receivables");
        }
    }
}
