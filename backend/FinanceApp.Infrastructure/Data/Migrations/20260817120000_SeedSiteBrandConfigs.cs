using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceApp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedSiteBrandConfigs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                INSERT INTO system_configs (
                    config_key,
                    config_value,
                    config_type,
                    description,
                    is_active,
                    created_at,
                    updated_at,
                    is_deleted
                )
                SELECT
                    'system_name',
                    '财务管理系统',
                    'string',
                    '站点名称',
                    TRUE,
                    TIMESTAMPTZ '2026-08-17T00:00:00Z',
                    TIMESTAMPTZ '2026-08-17T00:00:00Z',
                    FALSE
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM system_configs
                    WHERE config_key = 'system_name'
                      AND is_deleted = FALSE
                );

                INSERT INTO system_configs (
                    config_key,
                    config_value,
                    config_type,
                    description,
                    is_active,
                    created_at,
                    updated_at,
                    is_deleted
                )
                SELECT
                    'system_name_en',
                    'Finance Management System',
                    'string',
                    '站点英文副标题',
                    TRUE,
                    TIMESTAMPTZ '2026-08-17T00:00:00Z',
                    TIMESTAMPTZ '2026-08-17T00:00:00Z',
                    FALSE
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM system_configs
                    WHERE config_key = 'system_name_en'
                      AND is_deleted = FALSE
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM system_configs
                WHERE config_key IN ('system_name', 'system_name_en')
                  AND created_at = TIMESTAMPTZ '2026-08-17T00:00:00Z'
                  AND config_value IN ('财务管理系统', 'Finance Management System');
                """);
        }
    }
}
