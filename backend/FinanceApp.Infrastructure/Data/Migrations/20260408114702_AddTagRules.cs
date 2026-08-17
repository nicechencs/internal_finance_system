using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FinanceApp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTagRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tag_rules",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    rule_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    target_scope = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    match_field = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    match_operator = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    match_value = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tag_rules", x => x.id);
                    table.ForeignKey(
                        name: "FK_tag_rules_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tag_rule_tags",
                columns: table => new
                {
                    tag_rule_id = table.Column<long>(type: "bigint", nullable: false),
                    tag_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tag_rule_tags", x => new { x.tag_rule_id, x.tag_id });
                    table.ForeignKey(
                        name: "FK_tag_rule_tags_tag_rules_tag_rule_id",
                        column: x => x.tag_rule_id,
                        principalTable: "tag_rules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tag_rule_tags_tags_tag_id",
                        column: x => x.tag_id,
                        principalTable: "tags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tag_rule_tags_tag_id",
                table: "tag_rule_tags",
                column: "tag_id");

            migrationBuilder.CreateIndex(
                name: "idx_tag_rules_created_by",
                table: "tag_rules",
                column: "created_by",
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "idx_tag_rules_priority",
                table: "tag_rules",
                column: "priority",
                filter: "is_active = true");

            migrationBuilder.CreateIndex(
                name: "idx_tag_rules_target_scope",
                table: "tag_rules",
                column: "target_scope",
                filter: "is_active = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tag_rule_tags");

            migrationBuilder.DropTable(
                name: "tag_rules");
        }
    }
}
