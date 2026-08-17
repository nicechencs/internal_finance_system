using System.Data;
using System.Data.Common;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FinanceApp.Infrastructure.Data;

public static class LegacySchemaUpgrader
{
    public static async Task<bool> MigrationHistoryTableExistsAsync(
        AppDbContext context,
        CancellationToken cancellationToken = default)
    {
        if (!context.Database.IsRelational())
        {
            return false;
        }

        var providerName = context.Database.ProviderName ?? string.Empty;

        if (providerName.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
        {
            return await ScalarBooleanAsync(
                context,
                """
                SELECT EXISTS (
                    SELECT 1
                    FROM information_schema.tables
                    WHERE table_schema = 'public' AND table_name = '__EFMigrationsHistory')
                """,
                cancellationToken);
        }

        if (providerName.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            return await ScalarBooleanAsync(
                context,
                "SELECT EXISTS(SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = '__EFMigrationsHistory')",
                cancellationToken);
        }

        return false;
    }

    public static async Task BaselineInitialMigrationAsync(
        AppDbContext context,
        ILogger logger,
        string migrationId,
        CancellationToken cancellationToken = default)
    {
        if (!context.Database.IsRelational() || string.IsNullOrWhiteSpace(migrationId))
        {
            return;
        }

        var providerName = context.Database.ProviderName ?? string.Empty;
        var productVersion = GetEfCoreProductVersion();

        if (providerName.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
        {
            await context.Database.ExecuteSqlRawAsync(
                """
                CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
                    "MigrationId" character varying(150) NOT NULL,
                    "ProductVersion" character varying(32) NOT NULL,
                    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
                );
                """,
                cancellationToken);

            await context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                  INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
                  VALUES ({migrationId}, {productVersion})
                  ON CONFLICT ("MigrationId") DO NOTHING;
                  """,
                cancellationToken);
        }
        else if (providerName.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            await context.Database.ExecuteSqlRawAsync(
                """
                CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
                    "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
                    "ProductVersion" TEXT NOT NULL
                );
                """,
                cancellationToken);

            await context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                  INSERT OR IGNORE INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
                  VALUES ({migrationId}, {productVersion});
                  """,
                cancellationToken);
        }
        else
        {
            return;
        }

        logger.LogWarning(
            "Recorded baseline EF migration history for legacy database: {MigrationId}",
            migrationId);
    }

    public static async Task ApplyAsync(
        AppDbContext context,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (!context.Database.IsRelational())
        {
            return;
        }

        var providerName = context.Database.ProviderName ?? string.Empty;

        if (providerName.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
        {
            await EnsurePostgresUserAuthSchemaAsync(context, logger, cancellationToken);
            await EnsurePostgresTransactionSchemaAsync(context, logger, cancellationToken);
            await EnsurePostgresBankTransactionSchemaAsync(context, logger, cancellationToken);
            return;
        }

        if (providerName.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            await EnsureSqliteUserAuthSchemaAsync(context, logger, cancellationToken);
            await EnsureSqliteTransactionSchemaAsync(context, logger, cancellationToken);
            await EnsureSqliteBankTransactionSchemaAsync(context, logger, cancellationToken);
        }
    }

    private static async Task EnsurePostgresUserAuthSchemaAsync(
        AppDbContext context,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (!await ScalarBooleanAsync(
                context,
                """
                SELECT EXISTS (
                    SELECT 1
                    FROM information_schema.tables
                    WHERE table_schema = 'public' AND table_name = 'users')
                """,
                cancellationToken))
        {
            return;
        }

        var columns = await GetColumnNamesAsync(
            context,
            """
            SELECT column_name
            FROM information_schema.columns
            WHERE table_schema = 'public' AND table_name = 'users'
            """,
            0,
            cancellationToken);

        var addedColumns = new List<string>();

        await EnsurePostgresColumnAsync(
            context,
            columns,
            addedColumns,
            "normalized_username",
            "ALTER TABLE users ADD COLUMN normalized_username VARCHAR(50) NOT NULL DEFAULT '';",
            cancellationToken);
        await EnsurePostgresColumnAsync(
            context,
            columns,
            addedColumns,
            "security_stamp",
            "ALTER TABLE users ADD COLUMN security_stamp VARCHAR(64) NOT NULL DEFAULT '';",
            cancellationToken);
        await EnsurePostgresColumnAsync(
            context,
            columns,
            addedColumns,
            "must_change_password",
            "ALTER TABLE users ADD COLUMN must_change_password BOOLEAN NOT NULL DEFAULT FALSE;",
            cancellationToken);
        await EnsurePostgresColumnAsync(
            context,
            columns,
            addedColumns,
            "access_failed_count",
            "ALTER TABLE users ADD COLUMN access_failed_count INT NOT NULL DEFAULT 0;",
            cancellationToken);
        await EnsurePostgresColumnAsync(
            context,
            columns,
            addedColumns,
            "lockout_end_at",
            "ALTER TABLE users ADD COLUMN lockout_end_at TIMESTAMP;",
            cancellationToken);
        await EnsurePostgresColumnAsync(
            context,
            columns,
            addedColumns,
            "last_login_at",
            "ALTER TABLE users ADD COLUMN last_login_at TIMESTAMP;",
            cancellationToken);
        await EnsurePostgresColumnAsync(
            context,
            columns,
            addedColumns,
            "password_changed_at",
            "ALTER TABLE users ADD COLUMN password_changed_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP;",
            cancellationToken);

        await context.Database.ExecuteSqlRawAsync(
            """
            UPDATE users
            SET normalized_username = UPPER(TRIM(username))
            WHERE COALESCE(BTRIM(normalized_username), '') = ''
            """,
            cancellationToken);
        await context.Database.ExecuteSqlRawAsync(
            """
            UPDATE users
            SET security_stamp = md5(random()::text || id::text || clock_timestamp()::text)
            WHERE COALESCE(BTRIM(security_stamp), '') = ''
            """,
            cancellationToken);
        await context.Database.ExecuteSqlRawAsync(
            """
            UPDATE users
            SET password_changed_at = COALESCE(password_changed_at, created_at, CURRENT_TIMESTAMP)
            WHERE password_changed_at IS NULL
            """,
            cancellationToken);

        if (addedColumns.Count > 0)
        {
            logger.LogWarning(
                "Applied legacy schema compatibility updates to users table: {Columns}",
                string.Join(", ", addedColumns));
        }

        await EnsurePostgresIndexesAsync(context, logger, cancellationToken);
    }

    private static async Task EnsureSqliteUserAuthSchemaAsync(
        AppDbContext context,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (!await ScalarBooleanAsync(
                context,
                "SELECT EXISTS(SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = 'users')",
                cancellationToken))
        {
            return;
        }

        var columns = await GetColumnNamesAsync(
            context,
            "PRAGMA table_info('users')",
            1,
            cancellationToken);

        var addedColumns = new List<string>();

        await EnsureSqliteColumnAsync(
            context,
            columns,
            addedColumns,
            "normalized_username",
            "ALTER TABLE users ADD COLUMN normalized_username TEXT NOT NULL DEFAULT '';",
            cancellationToken);
        await EnsureSqliteColumnAsync(
            context,
            columns,
            addedColumns,
            "security_stamp",
            "ALTER TABLE users ADD COLUMN security_stamp TEXT NOT NULL DEFAULT '';",
            cancellationToken);
        await EnsureSqliteColumnAsync(
            context,
            columns,
            addedColumns,
            "must_change_password",
            "ALTER TABLE users ADD COLUMN must_change_password INTEGER NOT NULL DEFAULT 0;",
            cancellationToken);
        await EnsureSqliteColumnAsync(
            context,
            columns,
            addedColumns,
            "access_failed_count",
            "ALTER TABLE users ADD COLUMN access_failed_count INTEGER NOT NULL DEFAULT 0;",
            cancellationToken);
        await EnsureSqliteColumnAsync(
            context,
            columns,
            addedColumns,
            "lockout_end_at",
            "ALTER TABLE users ADD COLUMN lockout_end_at TEXT;",
            cancellationToken);
        await EnsureSqliteColumnAsync(
            context,
            columns,
            addedColumns,
            "last_login_at",
            "ALTER TABLE users ADD COLUMN last_login_at TEXT;",
            cancellationToken);
        await EnsureSqliteColumnAsync(
            context,
            columns,
            addedColumns,
            "password_changed_at",
            "ALTER TABLE users ADD COLUMN password_changed_at TEXT;",
            cancellationToken);

        await context.Database.ExecuteSqlRawAsync(
            """
            UPDATE users
            SET normalized_username = UPPER(TRIM(username))
            WHERE COALESCE(TRIM(normalized_username), '') = ''
            """,
            cancellationToken);
        await context.Database.ExecuteSqlRawAsync(
            """
            UPDATE users
            SET security_stamp = lower(hex(randomblob(16)))
            WHERE COALESCE(TRIM(security_stamp), '') = ''
            """,
            cancellationToken);
        await context.Database.ExecuteSqlRawAsync(
            """
            UPDATE users
            SET password_changed_at = COALESCE(password_changed_at, created_at, CURRENT_TIMESTAMP)
            WHERE password_changed_at IS NULL
            """,
            cancellationToken);

        if (addedColumns.Count > 0)
        {
            logger.LogWarning(
                "Applied legacy schema compatibility updates to users table: {Columns}",
                string.Join(", ", addedColumns));
        }

        await EnsureSqliteIndexesAsync(context, logger, cancellationToken);
    }

    private static async Task EnsurePostgresIndexesAsync(
        AppDbContext context,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (!await ScalarBooleanAsync(
                context,
                """
                SELECT EXISTS (
                    SELECT 1
                    FROM pg_indexes
                    WHERE schemaname = 'public' AND indexname = 'idx_users_normalized_username')
                """,
                cancellationToken))
        {
            if (await HasDuplicateNormalizedUsernamesAsync(context, cancellationToken))
            {
                await context.Database.ExecuteSqlRawAsync(
                    "CREATE INDEX IF NOT EXISTS idx_users_normalized_username ON users(normalized_username) WHERE is_deleted = false;",
                    cancellationToken);
                logger.LogWarning(
                    "Detected duplicate normalized usernames in legacy data. Created a non-unique index for normalized_username.");
            }
            else
            {
                await context.Database.ExecuteSqlRawAsync(
                    "CREATE UNIQUE INDEX IF NOT EXISTS idx_users_normalized_username ON users(normalized_username) WHERE is_deleted = false;",
                    cancellationToken);
            }
        }

        if (!await ScalarBooleanAsync(
                context,
                """
                SELECT EXISTS (
                    SELECT 1
                    FROM pg_indexes
                    WHERE schemaname = 'public' AND indexname = 'idx_users_active')
                """,
                cancellationToken))
        {
            await context.Database.ExecuteSqlRawAsync(
                "CREATE INDEX IF NOT EXISTS idx_users_active ON users(is_active) WHERE is_deleted = false;",
                cancellationToken);
        }
    }

    private static async Task EnsureSqliteIndexesAsync(
        AppDbContext context,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (!await ScalarBooleanAsync(
                context,
                """
                SELECT EXISTS(
                    SELECT 1
                    FROM sqlite_master
                    WHERE type = 'index' AND name = 'idx_users_normalized_username')
                """,
                cancellationToken))
        {
            if (await HasDuplicateNormalizedUsernamesAsync(context, cancellationToken))
            {
                await context.Database.ExecuteSqlRawAsync(
                    "CREATE INDEX IF NOT EXISTS idx_users_normalized_username ON users(normalized_username) WHERE is_deleted = 0;",
                    cancellationToken);
                logger.LogWarning(
                    "Detected duplicate normalized usernames in legacy data. Created a non-unique index for normalized_username.");
            }
            else
            {
                await context.Database.ExecuteSqlRawAsync(
                    "CREATE UNIQUE INDEX IF NOT EXISTS idx_users_normalized_username ON users(normalized_username) WHERE is_deleted = 0;",
                    cancellationToken);
            }
        }

        if (!await ScalarBooleanAsync(
                context,
                """
                SELECT EXISTS(
                    SELECT 1
                    FROM sqlite_master
                    WHERE type = 'index' AND name = 'idx_users_active')
                """,
                cancellationToken))
        {
            await context.Database.ExecuteSqlRawAsync(
                "CREATE INDEX IF NOT EXISTS idx_users_active ON users(is_active) WHERE is_deleted = 0;",
                cancellationToken);
        }
    }

    private static async Task EnsurePostgresTransactionSchemaAsync(
        AppDbContext context,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (!await ScalarBooleanAsync(
                context,
                """
                SELECT EXISTS (
                    SELECT 1
                    FROM information_schema.tables
                    WHERE table_schema = 'public' AND table_name = 'transactions')
                """,
                cancellationToken))
        {
            return;
        }

        var columns = await GetColumnNamesAsync(
            context,
            """
            SELECT column_name
            FROM information_schema.columns
            WHERE table_schema = 'public' AND table_name = 'transactions'
            """,
            0,
            cancellationToken);

        var addedColumns = new List<string>();

        await EnsurePostgresColumnAsync(
            context,
            columns,
            addedColumns,
            "transfer_direction",
            "ALTER TABLE transactions ADD COLUMN transfer_direction VARCHAR(20) NOT NULL DEFAULT 'none';",
            cancellationToken);

        if (addedColumns.Count > 0)
        {
            logger.LogWarning(
                "Applied legacy schema compatibility updates to transactions table: {Columns}",
                string.Join(", ", addedColumns));
        }
    }

    private static async Task EnsurePostgresBankTransactionSchemaAsync(
        AppDbContext context,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (!await ScalarBooleanAsync(
                context,
                """
                SELECT EXISTS (
                    SELECT 1
                    FROM information_schema.tables
                    WHERE table_schema = 'public' AND table_name = 'bank_transactions')
                """,
                cancellationToken))
        {
            return;
        }

        var columns = await GetColumnNamesAsync(
            context,
            """
            SELECT column_name
            FROM information_schema.columns
            WHERE table_schema = 'public' AND table_name = 'bank_transactions'
            """,
            0,
            cancellationToken);

        var addedColumns = new List<string>();

        await EnsurePostgresColumnAsync(
            context,
            columns,
            addedColumns,
            "description",
            "ALTER TABLE bank_transactions ADD COLUMN description TEXT;",
            cancellationToken);

        if (addedColumns.Count > 0)
        {
            logger.LogWarning(
                "Applied legacy schema compatibility updates to bank_transactions table: {Columns}",
                string.Join(", ", addedColumns));
        }
    }

    private static async Task EnsureSqliteTransactionSchemaAsync(
        AppDbContext context,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (!await ScalarBooleanAsync(
                context,
                "SELECT EXISTS(SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = 'transactions')",
                cancellationToken))
        {
            return;
        }

        var columns = await GetColumnNamesAsync(
            context,
            "PRAGMA table_info('transactions')",
            1,
            cancellationToken);

        var addedColumns = new List<string>();

        await EnsureSqliteColumnAsync(
            context,
            columns,
            addedColumns,
            "transfer_direction",
            "ALTER TABLE transactions ADD COLUMN transfer_direction TEXT NOT NULL DEFAULT 'none';",
            cancellationToken);

        if (addedColumns.Count > 0)
        {
            logger.LogWarning(
                "Applied legacy schema compatibility updates to transactions table: {Columns}",
                string.Join(", ", addedColumns));
        }
    }

    private static async Task EnsureSqliteBankTransactionSchemaAsync(
        AppDbContext context,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (!await ScalarBooleanAsync(
                context,
                "SELECT EXISTS(SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = 'bank_transactions')",
                cancellationToken))
        {
            return;
        }

        var columns = await GetColumnNamesAsync(
            context,
            "PRAGMA table_info('bank_transactions')",
            1,
            cancellationToken);

        var addedColumns = new List<string>();

        await EnsureSqliteColumnAsync(
            context,
            columns,
            addedColumns,
            "description",
            "ALTER TABLE bank_transactions ADD COLUMN description TEXT;",
            cancellationToken);

        if (addedColumns.Count > 0)
        {
            logger.LogWarning(
                "Applied legacy schema compatibility updates to bank_transactions table: {Columns}",
                string.Join(", ", addedColumns));
        }
    }

    private static async Task EnsurePostgresColumnAsync(
        AppDbContext context,
        ISet<string> columns,
        ICollection<string> addedColumns,
        string columnName,
        string sql,
        CancellationToken cancellationToken)
    {
        if (columns.Contains(columnName))
        {
            return;
        }

        await context.Database.ExecuteSqlRawAsync(sql, cancellationToken);
        columns.Add(columnName);
        addedColumns.Add(columnName);
    }

    private static async Task EnsureSqliteColumnAsync(
        AppDbContext context,
        ISet<string> columns,
        ICollection<string> addedColumns,
        string columnName,
        string sql,
        CancellationToken cancellationToken)
    {
        if (columns.Contains(columnName))
        {
            return;
        }

        await context.Database.ExecuteSqlRawAsync(sql, cancellationToken);
        columns.Add(columnName);
        addedColumns.Add(columnName);
    }

    private static async Task<bool> HasDuplicateNormalizedUsernamesAsync(
        AppDbContext context,
        CancellationToken cancellationToken)
    {
        return await ScalarBooleanAsync(
            context,
            """
            SELECT EXISTS (
                SELECT 1
                FROM users
                WHERE COALESCE(TRIM(normalized_username), '') <> '' AND NOT is_deleted
                GROUP BY normalized_username
                HAVING COUNT(*) > 1)
            """,
            cancellationToken);
    }

    private static async Task<HashSet<string>> GetColumnNamesAsync(
        AppDbContext context,
        string sql,
        int columnOrdinal,
        CancellationToken cancellationToken)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        await WithOpenConnectionAsync(
            context,
            async connection =>
            {
                await using var command = connection.CreateCommand();
                command.CommandText = sql;

                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    if (!reader.IsDBNull(columnOrdinal))
                    {
                        names.Add(reader.GetString(columnOrdinal));
                    }
                }
            },
            cancellationToken);

        return names;
    }

    private static async Task<bool> ScalarBooleanAsync(
        AppDbContext context,
        string sql,
        CancellationToken cancellationToken)
    {
        object? result = null;

        await WithOpenConnectionAsync(
            context,
            async connection =>
            {
                await using var command = connection.CreateCommand();
                command.CommandText = sql;
                result = await command.ExecuteScalarAsync(cancellationToken);
            },
            cancellationToken);

        return result switch
        {
            bool boolValue => boolValue,
            long longValue => longValue != 0,
            int intValue => intValue != 0,
            string stringValue when bool.TryParse(stringValue, out var parsed) => parsed,
            string stringValue when long.TryParse(stringValue, out var parsed) => parsed != 0,
            _ => false
        };
    }

    private static async Task WithOpenConnectionAsync(
        AppDbContext context,
        Func<DbConnection, Task> action,
        CancellationToken cancellationToken)
    {
        var connection = context.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;

        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await action(connection);
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static string GetEfCoreProductVersion()
    {
        var informationalVersion = typeof(DbContext).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        return informationalVersion?.Split('+')[0]
            ?? typeof(DbContext).Assembly.GetName().Version?.ToString(3)
            ?? "8.0.0";
    }

}
