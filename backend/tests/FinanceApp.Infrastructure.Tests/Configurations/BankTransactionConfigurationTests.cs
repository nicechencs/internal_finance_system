using FluentAssertions;
using FinanceApp.Domain.Entities;
using FinanceApp.Infrastructure.Data;
using FinanceApp.Infrastructure.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace FinanceApp.Infrastructure.Tests.Configurations;

/// <summary>
/// 验证 BankTransaction 实体的 EF 列映射，
/// 重点确认 description 字段已纳入迁移（问题修复后的回归测试）
/// </summary>
public class BankTransactionConfigurationTests : IDisposable
{
    private readonly AppDbContext _context;

    public BankTransactionConfigurationTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext(Guid.NewGuid().ToString());
    }

    [Fact]
    public void BankTransactionConfiguration_ShouldMapToCorrectTable()
    {
        var entityType = _context.Model.FindEntityType(typeof(BankTransaction));

        entityType.Should().NotBeNull();
        entityType!.GetTableName().Should().Be("bank_transactions");
    }

    [Fact]
    public void BankTransactionConfiguration_ShouldHaveDescriptionProperty()
    {
        // 此测试是修复"description 迁移缺口"的回归保障：
        // 若 BankTransactionConfiguration 或实体未定义 Description，此测试将失败
        var entityType = _context.Model.FindEntityType(typeof(BankTransaction));

        var descProp = entityType!.FindProperty("Description");

        descProp.Should().NotBeNull("BankTransaction 必须包含 Description 属性以支持华夏银行等格式的交易描述字段");
        descProp!.GetColumnName().Should().Be("description", "列名应为 snake_case 格式 'description'");
        descProp.IsNullable.Should().BeTrue("description 字段为可选字段，应允许 NULL");
    }

    [Fact]
    public void BankTransactionConfiguration_ShouldHaveMemoProperty()
    {
        // 确认 memo 字段同样映射正确（与 description 互为独立字段）
        var entityType = _context.Model.FindEntityType(typeof(BankTransaction));

        var memoProp = entityType!.FindProperty("Memo");

        memoProp.Should().NotBeNull();
        memoProp!.GetColumnName().Should().Be("memo");
        memoProp.IsNullable.Should().BeTrue();
    }

    [Fact]
    public void BankTransactionConfiguration_ShouldHaveDescriptionAndMemoAsDistinctColumns()
    {
        // 确认 description 与 memo 是两个不同的列（华夏银行格式同时有这两个字段）
        var entityType = _context.Model.FindEntityType(typeof(BankTransaction));

        var descColumn = entityType!.FindProperty("Description")!.GetColumnName();
        var memoColumn = entityType.FindProperty("Memo")!.GetColumnName();

        descColumn.Should().NotBe(memoColumn, "description 和 memo 应为独立的两列");
    }

    [Fact]
    public void BankTransactionConfiguration_ShouldHaveCorrectCoreColumns()
    {
        var entityType = _context.Model.FindEntityType(typeof(BankTransaction));

        entityType!.FindProperty("Id")!.GetColumnName().Should().Be("id");
        entityType.FindProperty("AccountId")!.GetColumnName().Should().Be("account_id");
        entityType.FindProperty("Amount")!.GetColumnName().Should().Be("amount");
        entityType.FindProperty("Direction")!.GetColumnName().Should().Be("direction");
        entityType.FindProperty("UniqueHash")!.GetColumnName().Should().Be("unique_hash");
        entityType.FindProperty("IsProcessed")!.GetColumnName().Should().Be("is_processed");
    }

    [Fact]
    public void BankTransactionConfiguration_UniqueHashIndex_ShouldHavePartialFilter()
    {
        // 回归测试：修复前 InitialCreate 迁移未包含 HasFilter("is_deleted = false")，
        // 导致软删除记录仍占用唯一索引槽位，重新导入时报约束冲突。
        var entityType = _context.Model.FindEntityType(typeof(BankTransaction));
        var index = entityType!.GetIndexes()
            .FirstOrDefault(i => i.GetDatabaseName() == "idx_bank_transactions_hash");

        index.Should().NotBeNull("应存在名为 idx_bank_transactions_hash 的索引");
        index!.IsUnique.Should().BeTrue("UniqueHash 索引应为唯一索引");
        index.GetFilter().Should().Be("is_deleted = false",
            "必须配置部分索引过滤器，确保软删除记录不占用唯一槽位，允许重新导入已删除的数据");
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
