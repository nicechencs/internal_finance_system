using FluentAssertions;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.MasterData.Services;
using FinanceApp.Application.Tests.Helpers;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace FinanceApp.Application.Tests.Services;

public class TagAnalyticsServiceTests : TestBase
{
    private readonly Mock<IRepository<Tag>> _tagRepositoryMock;
    private readonly Mock<IRepository<TagBinding>> _tagBindingRepositoryMock;
    private readonly Mock<IRepository<Transaction>> _transactionRepositoryMock;
    private readonly Mock<ILogger<TagAnalyticsService>> _loggerMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<IDataPermissionService> _permissionServiceMock;
    private readonly TagAnalyticsService _service;

    public TagAnalyticsServiceTests()
    {
        _tagRepositoryMock = new Mock<IRepository<Tag>>();
        _tagBindingRepositoryMock = new Mock<IRepository<TagBinding>>();
        _transactionRepositoryMock = new Mock<IRepository<Transaction>>();
        _loggerMock = new Mock<ILogger<TagAnalyticsService>>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _permissionServiceMock = new Mock<IDataPermissionService>();

        // Admin 用户设置
        _currentUserServiceMock.Setup(x => x.UserId).Returns(1L);
        _currentUserServiceMock.Setup(x => x.Username).Returns("admin");
        _currentUserServiceMock.Setup(x => x.Role).Returns(UserRole.Admin);
        _currentUserServiceMock.Setup(x => x.IsAdmin).Returns(true);
        _currentUserServiceMock.Setup(x => x.IsAccountant).Returns(false);
        _currentUserServiceMock.Setup(x => x.IsViewer).Returns(false);

        // Admin 权限服务：不过滤任何数据
        _permissionServiceMock
            .Setup(x => x.ApplyPermissionFilter(It.IsAny<IQueryable<Transaction>>()))
            .Returns((IQueryable<Transaction> query) => query);

        _service = new TagAnalyticsService(
            _tagRepositoryMock.Object,
            _tagBindingRepositoryMock.Object,
            _transactionRepositoryMock.Object,
            _loggerMock.Object,
            _currentUserServiceMock.Object,
            _permissionServiceMock.Object);
    }

    // ─────────────────── GetTagSummaryAsync 测试 ───────────────────

    [Fact(DisplayName = "GetTagSummaryAsync - 无效 scope 应抛出 ValidationException")]
    public async Task GetTagSummaryAsync_InvalidScope_ThrowsValidationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.GetTagSummaryAsync("invalid"));
    }

    [Fact(DisplayName = "GetTagSummaryAsync - receivable scope 应抛出 ValidationException")]
    public async Task GetTagSummaryAsync_UnsupportedScope_Receivable_ThrowsValidationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.GetTagSummaryAsync("receivable"));
    }

    [Fact(DisplayName = "GetTagSummaryAsync - payable scope 应抛出 ValidationException")]
    public async Task GetTagSummaryAsync_UnsupportedScope_Payable_ThrowsValidationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.GetTagSummaryAsync("payable"));
    }

    [Fact(DisplayName = "GetTagSummaryAsync - 没有标签时返回空 Items 和 Total=0")]
    public async Task GetTagSummaryAsync_NoTags_ReturnsEmptyItems()
    {
        // Arrange
        var emptyTags = new List<Tag>().AsQueryable().BuildMock();
        _tagRepositoryMock.Setup(r => r.GetQueryable()).Returns(emptyTags.Object);

        var emptyBindings = new List<TagBinding>().AsQueryable().BuildMock();
        _tagBindingRepositoryMock.Setup(r => r.GetQueryable()).Returns(emptyBindings.Object);

        var emptyTransactions = new List<Transaction>().AsQueryable().BuildMock();
        _transactionRepositoryMock.Setup(r => r.GetQueryable()).Returns(emptyTransactions.Object);

        // Act
        var result = await _service.GetTagSummaryAsync("transaction");

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.TotalTransactionCount.Should().Be(0);
        result.TotalIncomeAmount.Should().Be(0);
        result.TotalExpenseAmount.Should().Be(0);
        result.TotalNetAmount.Should().Be(0);
        result.Scope.Should().Be("transaction");
    }

    [Fact(DisplayName = "GetTagSummaryAsync - 有标签时正确计算收入/支出/百分比")]
    public async Task GetTagSummaryAsync_WithTags_ReturnsCorrectSummary()
    {
        // Arrange
        var tag1 = new Tag { Id = 1, Scope = TagScope.Transaction, Name = "标签A", IsActive = true, SortOrder = 1 };
        var tag2 = new Tag { Id = 2, Scope = TagScope.Transaction, Name = "标签B", IsActive = true, SortOrder = 2 };

        var tags = new List<Tag> { tag1, tag2 }.AsQueryable().BuildMock();
        _tagRepositoryMock.Setup(r => r.GetQueryable()).Returns(tags.Object);

        // 交易绑定：tx1 -> tag1, tx2 -> tag2, tx3 -> tag1（tag1 关联 tx1/tx3，tag2 关联 tx2）
        var bindings = new List<TagBinding>
        {
            new() { Id = 1, TagId = 1, OwnerType = TagScope.Transaction, OwnerId = 1 },
            new() { Id = 2, TagId = 2, OwnerType = TagScope.Transaction, OwnerId = 2 },
            new() { Id = 3, TagId = 1, OwnerType = TagScope.Transaction, OwnerId = 3 }
        }.AsQueryable().BuildMock();
        _tagBindingRepositoryMock.Setup(r => r.GetQueryable()).Returns(bindings.Object);

        // 交易：tx1 收入100，tx2 支出50，tx3 收入200
        var transactions = new List<Transaction>
        {
            new() { Id = 1, TransactionType = TransactionType.Income, Amount = 100m, TransactionDate = DateTime.Today },
            new() { Id = 2, TransactionType = TransactionType.Expense, Amount = 50m, TransactionDate = DateTime.Today },
            new() { Id = 3, TransactionType = TransactionType.Income, Amount = 200m, TransactionDate = DateTime.Today }
        }.AsQueryable().BuildMock();
        _transactionRepositoryMock.Setup(r => r.GetQueryable()).Returns(transactions.Object);

        // Act
        var result = await _service.GetTagSummaryAsync("transaction");

        // Assert
        result.Should().NotBeNull();
        result.TotalTransactionCount.Should().Be(3);
        result.TotalIncomeAmount.Should().Be(300m);   // 100 + 200
        result.TotalExpenseAmount.Should().Be(50m);
        result.TotalNetAmount.Should().Be(250m);

        // tag1 关联 tx1(收入100) + tx3(收入200)
        var tag1Item = result.Items.FirstOrDefault(i => i.TagId == 1);
        tag1Item.Should().NotBeNull();
        tag1Item!.TransactionCount.Should().Be(2);
        tag1Item.IncomeAmount.Should().Be(300m);
        tag1Item.ExpenseAmount.Should().Be(0m);
        tag1Item.NetAmount.Should().Be(300m);
        tag1Item.IncomePercentage.Should().Be(100m);   // 300/300 * 100
        tag1Item.ExpensePercentage.Should().Be(0m);    // 无支出

        // tag2 关联 tx2(支出50)
        var tag2Item = result.Items.FirstOrDefault(i => i.TagId == 2);
        tag2Item.Should().NotBeNull();
        tag2Item!.TransactionCount.Should().Be(1);
        tag2Item.IncomeAmount.Should().Be(0m);
        tag2Item.ExpenseAmount.Should().Be(50m);
        tag2Item.NetAmount.Should().Be(-50m);
        tag2Item.IncomePercentage.Should().Be(0m);
        tag2Item.ExpensePercentage.Should().Be(100m);  // 50/50 * 100
    }

    [Fact(DisplayName = "GetTagSummaryAsync - 划转类型交易被排除在外")]
    public async Task GetTagSummaryAsync_TransferTransactionsExcluded()
    {
        // Arrange
        var emptyTags = new List<Tag>().AsQueryable().BuildMock();
        _tagRepositoryMock.Setup(r => r.GetQueryable()).Returns(emptyTags.Object);

        var emptyBindings = new List<TagBinding>().AsQueryable().BuildMock();
        _tagBindingRepositoryMock.Setup(r => r.GetQueryable()).Returns(emptyBindings.Object);

        // 只有划转交易
        var transactions = new List<Transaction>
        {
            new() { Id = 1, TransactionType = TransactionType.Transfer, Amount = 500m, TransactionDate = DateTime.Today }
        }.AsQueryable().BuildMock();
        _transactionRepositoryMock.Setup(r => r.GetQueryable()).Returns(transactions.Object);

        // Act
        var result = await _service.GetTagSummaryAsync("transaction");

        // Assert
        result.TotalTransactionCount.Should().Be(0);
        result.TotalIncomeAmount.Should().Be(0m);
        result.TotalExpenseAmount.Should().Be(0m);
    }

    // ─────────────────── GetTagCrossAnalysisAsync 测试 ───────────────────

    [Fact(DisplayName = "GetTagCrossAnalysisAsync - 相同 rowScope 与 colScope 应抛出 ValidationException")]
    public async Task GetTagCrossAnalysisAsync_SameScope_ThrowsValidationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.GetTagCrossAnalysisAsync("transaction", "transaction"));
    }

    [Fact(DisplayName = "GetTagCrossAnalysisAsync - rowScope 为 receivable 应抛出 ValidationException")]
    public async Task GetTagCrossAnalysisAsync_UnsupportedScope_RowReceivable_ThrowsValidationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.GetTagCrossAnalysisAsync("receivable", "project"));
    }

    [Fact(DisplayName = "GetTagCrossAnalysisAsync - colScope 为 payable 应抛出 ValidationException")]
    public async Task GetTagCrossAnalysisAsync_UnsupportedScope_ColPayable_ThrowsValidationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.GetTagCrossAnalysisAsync("transaction", "payable"));
    }

    [Fact(DisplayName = "GetTagCrossAnalysisAsync - 无效 rowScope 应抛出 ValidationException")]
    public async Task GetTagCrossAnalysisAsync_InvalidRowScope_ThrowsValidationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.GetTagCrossAnalysisAsync("invalid", "project"));
    }

    [Fact(DisplayName = "GetTagCrossAnalysisAsync - 无效 colScope 应抛出 ValidationException")]
    public async Task GetTagCrossAnalysisAsync_InvalidColScope_ThrowsValidationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.GetTagCrossAnalysisAsync("transaction", "invalid"));
    }

    [Fact(DisplayName = "GetTagCrossAnalysisAsync - 其中一个 scope 没有标签时 Cells 为空")]
    public async Task GetTagCrossAnalysisAsync_NoTags_ReturnsEmptyResult()
    {
        {
            var projectTag = new Tag { Id = 10, Scope = TagScope.Project, Name = "项目A", IsActive = true, SortOrder = 0 };

            _tagRepositoryMock
                .SetupSequence(r => r.GetQueryable())
                .Returns(new List<Tag>().AsQueryable().BuildMock().Object)
                .Returns(new List<Tag> { projectTag }.AsQueryable().BuildMock().Object);

            _tagBindingRepositoryMock.Setup(r => r.GetQueryable())
                .Returns(new List<TagBinding>().AsQueryable().BuildMock().Object);

            _transactionRepositoryMock.Setup(r => r.GetQueryable())
                .Returns(new List<Transaction>().AsQueryable().BuildMock().Object);

            var expectedResult = await _service.GetTagCrossAnalysisAsync("transaction", "project");

            expectedResult.Should().NotBeNull();
            expectedResult.Cells.Should().BeEmpty();
            expectedResult.RowTags.Should().BeEmpty();
            expectedResult.ColTags.Should().ContainSingle();
            expectedResult.ColTags[0].TagId.Should().Be(10);
            expectedResult.ColTags[0].TransactionCount.Should().Be(0);
            expectedResult.RowScope.Should().Be("transaction");
            expectedResult.ColScope.Should().Be("project");
            return;
        }

        /*
        {
        // Arrange
        // rowScope = transaction，没有标签
        // colScope = project，有一个标签
        var projectTag = new Tag { Id = 10, Scope = TagScope.Project, Name = "项目A", IsActive = true, SortOrder = 0 };

        // GetQueryable 根据 scope 被调用两次（rowTags 和 colTags），使用序列设置
        _tagRepositoryMock
            .SetupSequence(r => r.GetQueryable())
            .Returns(new List<Tag>().AsQueryable().BuildMock().Object)        // rowTags (transaction scope) - 空
            .Returns(new List<Tag> { projectTag }.AsQueryable().BuildMock().Object); // colTags (project scope)

        var emptyBindings = new List<TagBinding>().AsQueryable().BuildMock();
        _tagBindingRepositoryMock.Setup(r => r.GetQueryable()).Returns(emptyBindings.Object);

        var emptyTransactions = new List<Transaction>().AsQueryable().BuildMock();
        _transactionRepositoryMock.Setup(r => r.GetQueryable()).Returns(emptyTransactions.Object);

        // Act
        var result = await _service.GetTagCrossAnalysisAsync("transaction", "project");

        // Assert
        result.Should().NotBeNull();
        result.Cells.Should().BeEmpty();
        result.RowTags.Should().BeEmpty();       // rowTags 本身就是空的
        result.ColTags.Should().HaveCount(1);    // colTags 有一个标签
        result.RowScope.Should().Be("transaction");
        result.ColScope.Should().Be("project");
        }
        */
    }

    [Fact(DisplayName = "GetTagCrossAnalysisAsync - 两个 scope 都有标签但无交叉时 Cells 为空")]
    public async Task GetTagCrossAnalysisAsync_TagsWithNoIntersection_ReturnsCellsEmpty()
    {
        // Arrange
        var txTag = new Tag { Id = 1, Scope = TagScope.Transaction, Name = "交易标签", IsActive = true, SortOrder = 0 };
        var projectTag = new Tag { Id = 2, Scope = TagScope.Project, Name = "项目标签", IsActive = true, SortOrder = 0 };

        _tagRepositoryMock
            .SetupSequence(r => r.GetQueryable())
            .Returns(new List<Tag> { txTag }.AsQueryable().BuildMock().Object)
            .Returns(new List<Tag> { projectTag }.AsQueryable().BuildMock().Object);

        // txTag 绑定 tx1，projectTag 绑定 project1，但没有交易同时满足两个条件（tx1 没有 project1 标签）
        var bindings = new List<TagBinding>
        {
            new() { Id = 1, TagId = 1, OwnerType = TagScope.Transaction, OwnerId = 1 },
            new() { Id = 2, TagId = 2, OwnerType = TagScope.Project, OwnerId = 1 }
            // project 标签绑定到 project=1，但 tx1 的 ProjectId 为 null
        }.AsQueryable().BuildMock();
        _tagBindingRepositoryMock.Setup(r => r.GetQueryable()).Returns(bindings.Object);

        var transactions = new List<Transaction>
        {
            new() { Id = 1, TransactionType = TransactionType.Income, Amount = 100m, TransactionDate = DateTime.Today, ProjectId = null }
        }.AsQueryable().BuildMock();
        _transactionRepositoryMock.Setup(r => r.GetQueryable()).Returns(transactions.Object);

        // Act
        var result = await _service.GetTagCrossAnalysisAsync("transaction", "project");

        // Assert
        result.Cells.Should().BeEmpty();
    }

    [Fact(DisplayName = "GetTagSummaryAsync - dateTo 应包含结束当日整天交易")]
    public async Task GetTagSummaryAsync_DateTo_IncludesWholeDay()
    {
        var tag = new Tag { Id = 1, Scope = TagScope.Transaction, Name = "当日标签", IsActive = true, SortOrder = 0 };

        _tagRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<Tag> { tag }.AsQueryable().BuildMock().Object);

        _tagBindingRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<TagBinding>
            {
                new() { Id = 1, TagId = 1, OwnerType = TagScope.Transaction, OwnerId = 1 },
                new() { Id = 2, TagId = 1, OwnerType = TagScope.Transaction, OwnerId = 2 },
                new() { Id = 3, TagId = 1, OwnerType = TagScope.Transaction, OwnerId = 3 }
            }.AsQueryable().BuildMock().Object);

        _transactionRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<Transaction>
            {
                new() { Id = 1, TransactionType = TransactionType.Income, Amount = 100m, TransactionDate = new DateTime(2026, 3, 26, 0, 0, 0) },
                new() { Id = 2, TransactionType = TransactionType.Income, Amount = 200m, TransactionDate = new DateTime(2026, 3, 26, 18, 30, 0) },
                new() { Id = 3, TransactionType = TransactionType.Income, Amount = 300m, TransactionDate = new DateTime(2026, 3, 27, 0, 0, 0) }
            }.AsQueryable().BuildMock().Object);

        var result = await _service.GetTagSummaryAsync("transaction", dateTo: new DateTime(2026, 3, 26));

        result.TotalTransactionCount.Should().Be(2);
        result.TotalIncomeAmount.Should().Be(300m);
        result.Items.Should().ContainSingle();
        result.Items[0].TransactionCount.Should().Be(2);
        result.Items[0].IncomeAmount.Should().Be(300m);
    }

    [Fact(DisplayName = "GetTagSummaryAsync - dateFrom 带时间分量应截断为当日 00:00:00 开始筛选")]
    public async Task GetTagSummaryAsync_DateFrom_ShouldTruncateToDateOnly()
    {
        // Arrange
        // dateFrom 传入 2026-03-15T14:30:00，.Date 截断后等价于 2026-03-15T00:00:00
        // 因此 2026-03-15T00:00:00 这条记录应被包含，2026-03-14T23:59:59 应被排除
        var tag = new Tag { Id = 1, Scope = TagScope.Transaction, Name = "测试标签", IsActive = true, SortOrder = 0 };

        _tagRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<Tag> { tag }.AsQueryable().BuildMock().Object);

        _tagBindingRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<TagBinding>
            {
                new() { Id = 1, TagId = 1, OwnerType = TagScope.Transaction, OwnerId = 1 },
                new() { Id = 2, TagId = 1, OwnerType = TagScope.Transaction, OwnerId = 2 },
                new() { Id = 3, TagId = 1, OwnerType = TagScope.Transaction, OwnerId = 3 }
            }.AsQueryable().BuildMock().Object);

        _transactionRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<Transaction>
            {
                // 边界前一秒：应被排除（< 2026-03-15T00:00:00）
                new() { Id = 1, TransactionType = TransactionType.Income, Amount = 999m, TransactionDate = new DateTime(2026, 3, 14, 23, 59, 59) },
                // 边界当日零点：应被包含（>= 2026-03-15T00:00:00）
                new() { Id = 2, TransactionType = TransactionType.Income, Amount = 100m, TransactionDate = new DateTime(2026, 3, 15, 0, 0, 0) },
                // 当日中间时刻：应被包含
                new() { Id = 3, TransactionType = TransactionType.Income, Amount = 200m, TransactionDate = new DateTime(2026, 3, 15, 10, 30, 0) }
            }.AsQueryable().BuildMock().Object);

        // Act：传入带时间分量的 dateFrom（14:30:00），.Date 截断后应从当日 00:00:00 开始
        var result = await _service.GetTagSummaryAsync("transaction",
            dateFrom: new DateTime(2026, 3, 15, 14, 30, 0));

        // Assert
        // id=1（前一天23:59:59）应被排除，id=2 和 id=3 应被包含
        result.TotalTransactionCount.Should().Be(2);
        result.TotalIncomeAmount.Should().Be(300m);  // 100 + 200
        result.Items.Should().ContainSingle();
        result.Items[0].TransactionCount.Should().Be(2);
        result.Items[0].IncomeAmount.Should().Be(300m);
    }

    [Fact(DisplayName = "GetTagSummaryAsync - dateTo 应转换为次日 00:00:00 作为独占上界")]
    public async Task GetTagSummaryAsync_DateTo_ShouldUseExclusiveNextDay()
    {
        // Arrange
        // dateTo = 2026-03-20，endExclusive = 2026-03-21T00:00:00
        // 2026-03-20T23:59:59 应被包含，2026-03-21T00:00:00 应被排除
        var tag = new Tag { Id = 1, Scope = TagScope.Transaction, Name = "测试标签", IsActive = true, SortOrder = 0 };

        _tagRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<Tag> { tag }.AsQueryable().BuildMock().Object);

        _tagBindingRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<TagBinding>
            {
                new() { Id = 1, TagId = 1, OwnerType = TagScope.Transaction, OwnerId = 1 },
                new() { Id = 2, TagId = 1, OwnerType = TagScope.Transaction, OwnerId = 2 },
                new() { Id = 3, TagId = 1, OwnerType = TagScope.Transaction, OwnerId = 3 }
            }.AsQueryable().BuildMock().Object);

        _transactionRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<Transaction>
            {
                // dateTo 当日最后一秒：应被包含（< 2026-03-21T00:00:00）
                new() { Id = 1, TransactionType = TransactionType.Expense, Amount = 50m,  TransactionDate = new DateTime(2026, 3, 20, 23, 59, 59) },
                // 次日零点整：应被排除（== endExclusive，不满足 <）
                new() { Id = 2, TransactionType = TransactionType.Expense, Amount = 999m, TransactionDate = new DateTime(2026, 3, 21, 0, 0, 0) },
                // 次日更晚：应被排除
                new() { Id = 3, TransactionType = TransactionType.Expense, Amount = 999m, TransactionDate = new DateTime(2026, 3, 21, 8, 0, 0) }
            }.AsQueryable().BuildMock().Object);

        // Act：传入 dateTo（不含时间分量，.Date 后仍为 2026-03-20）
        var result = await _service.GetTagSummaryAsync("transaction",
            dateTo: new DateTime(2026, 3, 20));

        // Assert：只有 id=1 被纳入统计
        result.TotalTransactionCount.Should().Be(1);
        result.TotalExpenseAmount.Should().Be(50m);
        result.Items.Should().ContainSingle();
        result.Items[0].TransactionCount.Should().Be(1);
        result.Items[0].ExpenseAmount.Should().Be(50m);
    }

    [Fact(DisplayName = "GetTagSummaryAsync - 日期范围应正确包含边界内记录并排除边界外记录")]
    public async Task GetTagSummaryAsync_DateRange_ShouldFilterCorrectly()
    {
        // Arrange
        // dateFrom=2026-03-10T09:00:00（截断后 2026-03-10T00:00:00）
        // dateTo=2026-03-20（endExclusive=2026-03-21T00:00:00）
        // 边界内：2026-03-10T00:00:00 ~ 2026-03-20T23:59:59
        // 边界外：2026-03-09T23:59:59 和 2026-03-21T00:00:00
        var tag = new Tag { Id = 1, Scope = TagScope.Transaction, Name = "范围标签", IsActive = true, SortOrder = 0 };

        _tagRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<Tag> { tag }.AsQueryable().BuildMock().Object);

        _tagBindingRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<TagBinding>
            {
                new() { Id = 1, TagId = 1, OwnerType = TagScope.Transaction, OwnerId = 1 },
                new() { Id = 2, TagId = 1, OwnerType = TagScope.Transaction, OwnerId = 2 },
                new() { Id = 3, TagId = 1, OwnerType = TagScope.Transaction, OwnerId = 3 },
                new() { Id = 4, TagId = 1, OwnerType = TagScope.Transaction, OwnerId = 4 },
                new() { Id = 5, TagId = 1, OwnerType = TagScope.Transaction, OwnerId = 5 }
            }.AsQueryable().BuildMock().Object);

        _transactionRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<Transaction>
            {
                // ❌ 下界外（前一天末）：排除
                new() { Id = 1, TransactionType = TransactionType.Income, Amount = 999m, TransactionDate = new DateTime(2026, 3, 9, 23, 59, 59) },
                // ✅ 下界（当日零点）：包含
                new() { Id = 2, TransactionType = TransactionType.Income, Amount = 100m, TransactionDate = new DateTime(2026, 3, 10, 0, 0, 0) },
                // ✅ 范围中间：包含
                new() { Id = 3, TransactionType = TransactionType.Expense, Amount = 200m, TransactionDate = new DateTime(2026, 3, 15, 12, 0, 0) },
                // ✅ 上界（dateTo 当日末秒）：包含
                new() { Id = 4, TransactionType = TransactionType.Income, Amount = 300m, TransactionDate = new DateTime(2026, 3, 20, 23, 59, 59) },
                // ❌ 上界外（次日零点整）：排除
                new() { Id = 5, TransactionType = TransactionType.Income, Amount = 999m, TransactionDate = new DateTime(2026, 3, 21, 0, 0, 0) }
            }.AsQueryable().BuildMock().Object);

        // Act：dateFrom 带时间分量，dateTo 不带时间分量
        var result = await _service.GetTagSummaryAsync("transaction",
            dateFrom: new DateTime(2026, 3, 10, 9, 0, 0),
            dateTo:   new DateTime(2026, 3, 20));

        // Assert：id=2（收入100）+ id=3（支出200）+ id=4（收入300）被包含，id=1、id=5 被排除
        result.TotalTransactionCount.Should().Be(3);
        result.TotalIncomeAmount.Should().Be(400m);    // 100 + 300
        result.TotalExpenseAmount.Should().Be(200m);
        result.TotalNetAmount.Should().Be(200m);       // 400 - 200
        result.Items.Should().ContainSingle();
        result.Items[0].TransactionCount.Should().Be(3);
        result.Items[0].IncomeAmount.Should().Be(400m);
        result.Items[0].ExpenseAmount.Should().Be(200m);
    }

    [Fact(DisplayName = "GetTagCrossAnalysisAsync - dateTo 应包含结束当日整天交易")]
    public async Task GetTagCrossAnalysisAsync_DateTo_IncludesWholeDay()
    {
        var rowTag = new Tag { Id = 1, Scope = TagScope.Transaction, Name = "交易标签", IsActive = true, SortOrder = 0 };
        var colTag = new Tag { Id = 2, Scope = TagScope.Project, Name = "项目标签", IsActive = true, SortOrder = 0 };

        _tagRepositoryMock
            .SetupSequence(r => r.GetQueryable())
            .Returns(new List<Tag> { rowTag }.AsQueryable().BuildMock().Object)
            .Returns(new List<Tag> { colTag }.AsQueryable().BuildMock().Object);

        _tagBindingRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<TagBinding>
            {
                new() { Id = 1, TagId = 1, OwnerType = TagScope.Transaction, OwnerId = 1 },
                new() { Id = 2, TagId = 1, OwnerType = TagScope.Transaction, OwnerId = 2 },
                new() { Id = 3, TagId = 2, OwnerType = TagScope.Project, OwnerId = 10 }
            }.AsQueryable().BuildMock().Object);

        _transactionRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<Transaction>
            {
                new() { Id = 1, TransactionType = TransactionType.Income, Amount = 100m, TransactionDate = new DateTime(2026, 3, 26, 10, 0, 0), ProjectId = 10 },
                new() { Id = 2, TransactionType = TransactionType.Income, Amount = 200m, TransactionDate = new DateTime(2026, 3, 26, 23, 30, 0), ProjectId = 10 },
                new() { Id = 3, TransactionType = TransactionType.Income, Amount = 300m, TransactionDate = new DateTime(2026, 3, 27, 0, 0, 0), ProjectId = 10 }
            }.AsQueryable().BuildMock().Object);

        var result = await _service.GetTagCrossAnalysisAsync("transaction", "project", dateTo: new DateTime(2026, 3, 26));

        result.Cells.Should().ContainSingle();
        result.Cells[0].TransactionCount.Should().Be(2);
        result.Cells[0].IncomeAmount.Should().Be(300m);
        result.RowTags.Should().ContainSingle();
        result.RowTags[0].TransactionCount.Should().Be(2);
        result.ColTags.Should().ContainSingle();
        result.ColTags[0].TransactionCount.Should().Be(2);
    }
}
