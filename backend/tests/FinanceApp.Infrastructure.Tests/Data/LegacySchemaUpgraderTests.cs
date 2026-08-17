using FinanceApp.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace FinanceApp.Infrastructure.Tests.Data;

public class LegacySchemaUpgraderTests
{
    [Fact]
    public async Task ApplyAsync_ShouldAddMissingUserAuthColumns_AndBackfillLegacyRows()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new AppDbContext(options);

        await context.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                username TEXT NOT NULL,
                password_hash TEXT NOT NULL,
                full_name TEXT NOT NULL,
                role TEXT NOT NULL DEFAULT 'viewer',
                is_active INTEGER NOT NULL DEFAULT 1,
                created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                deleted_at TEXT NULL,
                is_deleted INTEGER NOT NULL DEFAULT 0
            );
            """);

        await context.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO users (username, password_hash, full_name, role, is_active, is_deleted)
            VALUES ('LegacyUser', 'hash', 'Legacy User', 'admin', 1, 0);
            """);

        await LegacySchemaUpgrader.ApplyAsync(context, NullLogger.Instance);

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT normalized_username, security_stamp, must_change_password, access_failed_count, password_changed_at
            FROM users
            WHERE username = 'LegacyUser';
            """;

        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal("LEGACYUSER", reader.GetString(0));
        Assert.False(string.IsNullOrWhiteSpace(reader.GetString(1)));
        Assert.Equal(0L, reader.GetInt64(2));
        Assert.Equal(0L, reader.GetInt64(3));
        Assert.False(reader.IsDBNull(4));
    }

    [Fact]
    public async Task BaselineInitialMigrationAsync_ShouldCreateMigrationHistoryEntry()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new AppDbContext(options);

        Assert.False(await LegacySchemaUpgrader.MigrationHistoryTableExistsAsync(context));

        await LegacySchemaUpgrader.BaselineInitialMigrationAsync(
            context,
            NullLogger.Instance,
            "20260315144117_InitialCreate");

        Assert.True(await LegacySchemaUpgrader.MigrationHistoryTableExistsAsync(context));

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT "MigrationId"
            FROM "__EFMigrationsHistory"
            WHERE "MigrationId" = '20260315144117_InitialCreate';
            """;

        var result = await command.ExecuteScalarAsync();
        Assert.Equal("20260315144117_InitialCreate", result);
    }
}
