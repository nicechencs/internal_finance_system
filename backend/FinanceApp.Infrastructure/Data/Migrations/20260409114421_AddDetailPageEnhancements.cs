using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FinanceApp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDetailPageEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "receivable_type_id",
                table: "receivables",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "department",
                table: "persons",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "position",
                table: "persons",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "bank_account",
                table: "customers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "bank_name",
                table: "customers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "receivable_types",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_receivable_types", x => x.id);
                    table.ForeignKey(
                        name: "FK_receivable_types_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "idx_receivables_receivable_type",
                table: "receivables",
                column: "receivable_type_id");

            migrationBuilder.CreateIndex(
                name: "idx_receivable_types_code",
                table: "receivable_types",
                column: "code",
                unique: true,
                filter: "code IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "idx_receivable_types_created_by",
                table: "receivable_types",
                column: "created_by",
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "idx_receivable_types_sort_order",
                table: "receivable_types",
                column: "sort_order");

            migrationBuilder.AddForeignKey(
                name: "FK_receivables_receivable_types_receivable_type_id",
                table: "receivables",
                column: "receivable_type_id",
                principalTable: "receivable_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_receivables_receivable_types_receivable_type_id",
                table: "receivables");

            migrationBuilder.DropTable(
                name: "receivable_types");

            migrationBuilder.DropIndex(
                name: "idx_receivables_receivable_type",
                table: "receivables");

            migrationBuilder.DropColumn(
                name: "receivable_type_id",
                table: "receivables");

            migrationBuilder.DropColumn(
                name: "department",
                table: "persons");

            migrationBuilder.DropColumn(
                name: "position",
                table: "persons");

            migrationBuilder.DropColumn(
                name: "bank_account",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "bank_name",
                table: "customers");
        }
    }
}
