using Microsoft.EntityFrameworkCore;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Interfaces;

namespace FinanceApp.Infrastructure.Data;

public class AppDbContext : DbContext
{
    private const string DateColumnType = "date";
    private const string TimestampWithTimeZoneColumnType = "timestamp with time zone";

    private readonly ICurrentUserService? _currentUserService;

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUserService currentUserService) : base(options)
    {
        _currentUserService = currentUserService;
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Account> Accounts { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Supplier> Suppliers { get; set; }
    public DbSet<Person> Persons { get; set; }
    public DbSet<Project> Projects { get; set; }
    public DbSet<ImportBatch> ImportBatches { get; set; }
    public DbSet<BankTransaction> BankTransactions { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<TransactionAllocation> TransactionAllocations { get; set; }
    public DbSet<Receivable> Receivables { get; set; }
    public DbSet<ReceivableDetail> ReceivableDetails { get; set; }
    public DbSet<Payable> Payables { get; set; }
    public DbSet<PayableDetail> PayableDetails { get; set; }
    public DbSet<ClassificationRule> ClassificationRules { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<SystemConfig> SystemConfigs { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<TagBinding> TagBindings { get; set; }
    public DbSet<TagDailySummary> TagDailySummaries { get; set; }
    public DbSet<TagRule> TagRules { get; set; }
    public DbSet<TagRuleTag> TagRuleTags { get; set; }
    public DbSet<FixedDepositRecord> FixedDepositRecords { get; set; }
    public DbSet<PayableType> PayableTypes { get; set; }
    public DbSet<ReceivableType> ReceivableTypes { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        ApplyDateTimeColumnTypes(modelBuilder);

        // For SQLite: remove filtered indexes, jsonb column types, and descending sorts (unsupported)
        if (Database.IsSqlite())
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var index in entityType.GetIndexes().ToList())
                {
                    if (index.GetFilter() != null)
                    {
                        index.SetFilter(null);
                    }
                }

                foreach (var property in entityType.GetProperties())
                {
                    if (property.GetColumnType() == "jsonb")
                    {
                        property.SetColumnType("TEXT");
                    }
                }
            }
        }

        // Global query filter for soft delete
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
                var property = System.Linq.Expressions.Expression.Property(parameter, nameof(BaseEntity.IsDeleted));
                var filter = System.Linq.Expressions.Expression.Lambda(
                    System.Linq.Expressions.Expression.Equal(property, System.Linq.Expressions.Expression.Constant(false)),
                    parameter);
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
            }
        }
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is BaseEntity && (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
        {
            NormalizeDateOnlyValues(entry);

            var entity = (BaseEntity)entry.Entity;
            entity.UpdatedAt = DateTime.UtcNow;

            if (entry.State == EntityState.Added)
            {
                entity.CreatedAt = DateTime.UtcNow;

                // 自动设置 CreatedBy（如果当前用户服务可用且实体的 CreatedBy 为 null）
                if (_currentUserService != null && entity.CreatedBy == null && _currentUserService.UserId > 0)
                {
                    entity.CreatedBy = _currentUserService.UserId;
                }
            }
            else if (entry.State == EntityState.Modified && entry.Entity is IConcurrencyVersioned)
            {
                // 乐观并发版本自增：WHERE 条件使用更新前的原值（EF 依据并发令牌的 OriginalValue 生成），
                // 这里仅把 CurrentValue 置为原值 +1，保证提供商无关（PostgreSQL/SQLite 一致）。
                var versionProperty = entry.Property(nameof(IConcurrencyVersioned.Version));
                versionProperty.CurrentValue = (long)versionProperty.OriginalValue! + 1;
            }
        }
    }

    private static void ApplyDateTimeColumnTypes(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                var propertyType = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
                if (propertyType != typeof(DateTime))
                {
                    continue;
                }

                if (IsDateOnlyProperty(property.Name))
                {
                    property.SetColumnType(DateColumnType);
                }
                else if (IsUtcTimestampProperty(property.Name))
                {
                    property.SetColumnType(TimestampWithTimeZoneColumnType);
                }
            }
        }
    }

    private static void NormalizeDateOnlyValues(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        foreach (var property in entry.Properties)
        {
            var propertyType = Nullable.GetUnderlyingType(property.Metadata.ClrType) ?? property.Metadata.ClrType;
            if (propertyType != typeof(DateTime) || !IsDateOnlyProperty(property.Metadata.Name))
            {
                continue;
            }

            if (property.CurrentValue is DateTime value)
            {
                property.CurrentValue = value.Date;
            }
        }
    }

    private static bool IsDateOnlyProperty(string propertyName)
    {
        return propertyName.EndsWith("Date", StringComparison.Ordinal)
            && !string.Equals(propertyName, nameof(ImportBatch.ImportDate), StringComparison.Ordinal);
    }

    private static bool IsUtcTimestampProperty(string propertyName)
    {
        return propertyName.EndsWith("At", StringComparison.Ordinal)
            || string.Equals(propertyName, nameof(ImportBatch.ImportDate), StringComparison.Ordinal);
    }
}
