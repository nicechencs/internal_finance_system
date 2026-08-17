using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FinanceApp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTagDailySummaries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tag_daily_summaries",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    summary_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    tag_id = table.Column<long>(type: "bigint", nullable: false),
                    metric_scope = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    income_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    expense_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    net_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    transaction_count = table.Column<int>(type: "integer", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    created_by = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tag_daily_summaries", x => x.id);
                    table.ForeignKey(
                        name: "FK_tag_daily_summaries_tags_tag_id",
                        column: x => x.tag_id,
                        principalTable: "tags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_tag_daily_summaries_tag_scope_date",
                table: "tag_daily_summaries",
                columns: new[] { "tag_id", "metric_scope", "summary_date" },
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ux_tag_daily_summaries_date_tag_scope",
                table: "tag_daily_summaries",
                columns: new[] { "summary_date", "tag_id", "metric_scope" },
                unique: true,
                filter: "is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "tag_daily_summaries");
        }
    }
}
