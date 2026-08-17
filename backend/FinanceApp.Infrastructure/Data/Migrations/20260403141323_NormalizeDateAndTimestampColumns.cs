using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceApp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeDateAndTimestampColumns : Migration
    {
        private static readonly string[] BaseEntityTables =
        [
            "accounts",
            "audit_logs",
            "bank_transactions",
            "categories",
            "classification_rules",
            "customers",
            "fixed_deposit_records",
            "import_batches",
            "payable_details",
            "payable_types",
            "payables",
            "persons",
            "projects",
            "receivable_details",
            "receivables",
            "suppliers",
            "system_configs",
            "tag_bindings",
            "tag_daily_summaries",
            "tags",
            "transaction_allocations",
            "transactions",
            "users"
        ];

        private static readonly (string Table, string Column)[] UtcTimestampColumns =
        [
            ("import_batches", "import_date"),
            ("payables", "settled_at"),
            ("receivables", "settled_at"),
            ("users", "last_login_at"),
            ("users", "lockout_end_at"),
            ("users", "password_changed_at")
        ];

        private static readonly (string Table, string Column)[] DateOnlyColumns =
        [
            ("accounts", "interest_start_date"),
            ("accounts", "maturity_date"),
            ("bank_transactions", "transaction_date"),
            ("fixed_deposit_records", "deposit_date"),
            ("fixed_deposit_records", "maturity_date"),
            ("fixed_deposit_records", "withdrawal_date"),
            ("payables", "due_date"),
            ("payable_details", "payment_date"),
            ("persons", "join_date"),
            ("persons", "leave_date"),
            ("projects", "end_date"),
            ("projects", "start_date"),
            ("receivables", "due_date"),
            ("receivable_details", "payment_date"),
            ("tag_daily_summaries", "summary_date"),
            ("transactions", "transaction_date")
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (!IsNpgsqlProvider())
            {
                return;
            }

            DropProjectProfitView(migrationBuilder);

            foreach (var table in BaseEntityTables)
            {
                AlterTimestampWithoutTimeZoneToUtcTimestamp(migrationBuilder, table, "created_at");
                AlterTimestampWithoutTimeZoneToUtcTimestamp(migrationBuilder, table, "updated_at");
                AlterTimestampWithoutTimeZoneToUtcTimestamp(migrationBuilder, table, "deleted_at");
            }

            foreach (var (table, column) in UtcTimestampColumns)
            {
                AlterTimestampWithoutTimeZoneToUtcTimestamp(migrationBuilder, table, column);
            }

            foreach (var (table, column) in DateOnlyColumns)
            {
                AlterTimestampWithoutTimeZoneToDate(migrationBuilder, table, column);
            }

            CreateOrReplaceProjectProfitView(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (!IsNpgsqlProvider())
            {
                return;
            }

            DropProjectProfitView(migrationBuilder);

            foreach (var (table, column) in DateOnlyColumns)
            {
                AlterDateToTimestampWithoutTimeZone(migrationBuilder, table, column);
            }

            foreach (var (table, column) in UtcTimestampColumns)
            {
                AlterUtcTimestampToTimestampWithoutTimeZone(migrationBuilder, table, column);
            }

            foreach (var table in BaseEntityTables)
            {
                AlterUtcTimestampToTimestampWithoutTimeZone(migrationBuilder, table, "deleted_at");
                AlterUtcTimestampToTimestampWithoutTimeZone(migrationBuilder, table, "updated_at");
                AlterUtcTimestampToTimestampWithoutTimeZone(migrationBuilder, table, "created_at");
            }

            CreateOrReplaceProjectProfitView(migrationBuilder);
        }

        private bool IsNpgsqlProvider()
        {
            return ActiveProvider.Contains("Npgsql", System.StringComparison.Ordinal);
        }

        private static void AlterTimestampWithoutTimeZoneToUtcTimestamp(
            MigrationBuilder migrationBuilder,
            string table,
            string column)
        {
            migrationBuilder.Sql($"""
                ALTER TABLE "{table}"
                ALTER COLUMN "{column}" TYPE timestamp with time zone
                USING CASE
                    WHEN "{column}" IS NULL THEN NULL
                    ELSE "{column}" AT TIME ZONE 'UTC'
                END;
                """);
        }

        private static void AlterTimestampWithoutTimeZoneToDate(
            MigrationBuilder migrationBuilder,
            string table,
            string column)
        {
            migrationBuilder.Sql($"""
                ALTER TABLE "{table}"
                ALTER COLUMN "{column}" TYPE date
                USING "{column}"::date;
                """);
        }

        private static void AlterUtcTimestampToTimestampWithoutTimeZone(
            MigrationBuilder migrationBuilder,
            string table,
            string column)
        {
            migrationBuilder.Sql($"""
                ALTER TABLE "{table}"
                ALTER COLUMN "{column}" TYPE timestamp without time zone
                USING CASE
                    WHEN "{column}" IS NULL THEN NULL
                    ELSE "{column}" AT TIME ZONE 'UTC'
                END;
                """);
        }

        private static void AlterDateToTimestampWithoutTimeZone(
            MigrationBuilder migrationBuilder,
            string table,
            string column)
        {
            migrationBuilder.Sql($"""
                ALTER TABLE "{table}"
                ALTER COLUMN "{column}" TYPE timestamp without time zone
                USING "{column}"::timestamp without time zone;
                """);
        }

        private static void DropProjectProfitView(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP VIEW IF EXISTS v_project_profit;""");
        }

        private static void CreateOrReplaceProjectProfitView(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE OR REPLACE VIEW v_project_profit AS
                WITH project_direct_cost AS (
                    SELECT
                        t.project_id,
                        COALESCE(SUM(t.amount), 0) AS cost
                    FROM transactions t
                    WHERE t.transaction_type = 'expense'
                      AND t.is_allocated = false
                      AND t.is_deleted = false
                      AND t.project_id IS NOT NULL
                    GROUP BY t.project_id
                ),
                project_allocated_cost AS (
                    SELECT
                        ta.project_id,
                        COALESCE(SUM(ta.amount), 0) AS cost
                    FROM transaction_allocations ta
                    JOIN transactions t ON t.id = ta.transaction_id
                    WHERE t.transaction_type = 'expense'
                      AND t.is_deleted = false
                      AND ta.project_id IS NOT NULL
                    GROUP BY ta.project_id
                )
                SELECT
                    p.id,
                    p.name AS project_name,
                    p.project_code,
                    c.name AS customer_name,
                    p.contract_amount,
                    p.received_amount,
                    p.receivable_amount,
                    COALESCE(dc.cost, 0) + COALESCE(ac.cost, 0) AS total_cost,
                    p.received_amount - (COALESCE(dc.cost, 0) + COALESCE(ac.cost, 0)) AS profit_amount,
                    CASE
                        WHEN p.received_amount > 0 THEN
                            ROUND((p.received_amount - (COALESCE(dc.cost, 0) + COALESCE(ac.cost, 0))) / p.received_amount * 100, 2)
                        ELSE 0
                    END AS profit_rate,
                    p.status,
                    p.start_date,
                    p.end_date
                FROM projects p
                LEFT JOIN customers c ON p.customer_id = c.id
                LEFT JOIN project_direct_cost dc ON dc.project_id = p.id
                LEFT JOIN project_allocated_cost ac ON ac.project_id = p.id
                WHERE p.is_deleted = false;

                COMMENT ON VIEW v_project_profit IS '项目利润视图';
                """);
        }
    }
}
