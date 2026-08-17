using FluentAssertions;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.Identity.Interfaces;
using FinanceApp.Application.Modules.MasterData.DTOs.TagRule;
using FinanceApp.Application.Modules.MasterData.Services;
using FinanceApp.Application.Tests.Helpers;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Interfaces;
using MockQueryable.Moq;
using Moq;

namespace FinanceApp.Application.Tests.Services;

public class TagRuleServiceTests : TestBase
{
    private readonly Mock<IRepository<TagRule>> _tagRuleRepositoryMock;
    private readonly Mock<IRepository<Tag>> _tagRepositoryMock;
    private readonly Mock<IRepository<TagBinding>> _tagBindingRepositoryMock;
    private readonly Mock<IRepository<Transaction>> _transactionRepositoryMock;
    private readonly TagRuleService _service;

    public TagRuleServiceTests()
    {
        _tagRuleRepositoryMock = new Mock<IRepository<TagRule>>();
        _tagRepositoryMock = new Mock<IRepository<Tag>>();
        _tagBindingRepositoryMock = new Mock<IRepository<TagBinding>>();
        _transactionRepositoryMock = new Mock<IRepository<Transaction>>();

        _service = new TagRuleService(
            _tagRuleRepositoryMock.Object,
            _tagRepositoryMock.Object,
            _tagBindingRepositoryMock.Object,
            _transactionRepositoryMock.Object,
            Mock.Of<Microsoft.Extensions.Logging.ILogger<TagRuleService>>(),
            AuditLogServiceMock.Object,
            CreateAdminCurrentUserService(),
            CreateAdminDataPermissionService(),
            UnitOfWorkMock.Object
        );
    }

    // ─────────────────────── CreateAsync ───────────────────────

    [Fact]
    public async Task CreateAsync_WithInvalidScope_ShouldThrowValidation()
    {
        // Arrange
        var request = new CreateTagRuleRequest
        {
            RuleName = "测试规则",
            TargetScope = "InvalidScope",
            MatchField = "CounterpartyName",
            MatchOperator = "Contains",
            MatchValue = "阿里云",
            Priority = 100
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_WithInvalidRegex_ShouldThrowValidation()
    {
        // Arrange
        var request = new CreateTagRuleRequest
        {
            RuleName = "正则规则",
            TargetScope = "Transaction",
            MatchField = "CounterpartyName",
            MatchOperator = "Regex",
            MatchValue = "[invalid(regex", // 无效正则表达式
            Priority = 100
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(request));
    }

    // ─────────────────────── DeleteAsync ───────────────────────

    [Fact]
    public async Task DeleteAsync_NonExistentRule_ShouldThrowNotFound()
    {
        // Arrange: GetQueryable() 返回空列表
        MockHelpers.SetupRepo(_tagRuleRepositoryMock, Array.Empty<TagRule>());

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteAsync(999L));
    }

    [Fact]
    public async Task DeleteAsync_ExistingRule_ShouldCallDelete()
    {
        // Arrange
        var rule = new TagRule
        {
            Id = 1L,
            RuleName = "待删除规则",
            TargetScope = TagScope.Transaction,
            MatchField = RuleMatchField.CounterpartyName,
            MatchOperator = RuleMatchOperator.Contains,
            MatchValue = "测试",
            Priority = 50,
            IsActive = true
        };

        MockHelpers.SetupRepo(_tagRuleRepositoryMock, rule);

        // Act
        await _service.DeleteAsync(rule.Id);

        // Assert: Delete 被调用一次，然后保存
        _tagRuleRepositoryMock.Verify(x => x.Delete(It.Is<TagRule>(r => r.Id == rule.Id)), Times.Once);
        UnitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─────────────────────── RunRulesAsync: 字符串 operator ───────────────────────

    [Theory]
    [InlineData(RuleMatchOperator.Contains, "ali", "Alibaba Cloud", true)]
    [InlineData(RuleMatchOperator.Contains, "ALI", "Alibaba Cloud", true)]          // 大小写不敏感
    [InlineData(RuleMatchOperator.Contains, "xxx", "Alibaba Cloud", false)]
    [InlineData(RuleMatchOperator.Equals, "Alibaba", "alibaba", true)]              // 大小写不敏感
    [InlineData(RuleMatchOperator.Equals, "Alibaba", "Alibaba Cloud", false)]       // 不是前缀 == 全等
    [InlineData(RuleMatchOperator.StartsWith, "Ali", "Alibaba Cloud", true)]
    [InlineData(RuleMatchOperator.StartsWith, "Cloud", "Alibaba Cloud", false)]
    [InlineData(RuleMatchOperator.EndsWith, "Cloud", "Alibaba Cloud", true)]
    [InlineData(RuleMatchOperator.EndsWith, "Ali", "Alibaba Cloud", false)]
    [InlineData(RuleMatchOperator.Regex, @"^Ali.*Cloud$", "Alibaba Cloud", true)]
    [InlineData(RuleMatchOperator.Regex, @"^Xx.*$", "Alibaba Cloud", false)]
    public async Task RunRulesAsync_CounterpartyOperators_MatchAsExpected(
        RuleMatchOperator op, string ruleValue, string actualCounterparty, bool expectedMatch)
    {
        var rule = BuildRule(RuleMatchField.CounterpartyName, op, ruleValue);
        var tx = BuildTxWithCounterparty(actualCounterparty);
        SetupRunContext(rule, tx);

        var result = await _service.RunRulesAsync(new RunTagRulesRequest { TargetScope = "Transaction" });

        result.AddedCount.Should().Be(expectedMatch ? 1 : 0);
    }

    // ─────────────────────── RunRulesAsync: Amount 数值比较 ───────────────────────

    [Theory]
    [InlineData("100.5", 100.5, true)]       // 相同精度 — 现有实现也能过
    [InlineData("100.50", 100.5, true)]      // 财务等价但字符串形态不同 — 现有实现会挂
    [InlineData("100.00", 100.0, true)]      // 同上
    [InlineData("100", 100.5, false)]        // 不相等
    public async Task RunRulesAsync_AmountEquals_UsesNumericEquality(
        string ruleValue, double amount, bool expectedMatch)
    {
        var rule = BuildRule(RuleMatchField.Amount, RuleMatchOperator.Equals, ruleValue);
        var tx = BuildTxWithAmount((decimal)amount);
        SetupRunContext(rule, tx);

        var result = await _service.RunRulesAsync(new RunTagRulesRequest { TargetScope = "Transaction" });

        result.AddedCount.Should().Be(expectedMatch ? 1 : 0);
    }

    // ─────────────────────── RunRulesAsync: Range operator（Amount 区间）───────────────────────

    [Theory]
    [InlineData("1000", "10000", 5000, true)]    // 区间内
    [InlineData("1000", "10000", 1000, true)]    // 下界闭区间
    [InlineData("1000", "10000", 10000, true)]   // 上界闭区间
    [InlineData("1000", "10000", 999.99, false)] // 下界外
    [InlineData("1000", "10000", 10000.01, false)] // 上界外
    [InlineData("1000", null, 5000, true)]       // 开放上限，>= 下界命中
    [InlineData("1000", null, 500, false)]       // 开放上限，低于下界不命中
    [InlineData("1000", "", 5000, true)]         // 空串等同 null，开放上限
    public async Task RunRulesAsync_AmountRange_MatchesByNumericBounds(
        string min, string? max, double amount, bool expectedMatch)
    {
        var rule = BuildRule(RuleMatchField.Amount, RuleMatchOperator.Range, min, max);
        var tx = BuildTxWithAmount((decimal)amount);
        SetupRunContext(rule, tx);

        var result = await _service.RunRulesAsync(new RunTagRulesRequest { TargetScope = "Transaction" });

        result.AddedCount.Should().Be(expectedMatch ? 1 : 0);
    }

    // ─────────────────────── 字段/操作符组合校验 ───────────────────────

    [Theory]
    [InlineData("CounterpartyName", "Range", "100")]   // 字符串字段不允许 Range
    [InlineData("Description", "Range", "100")]
    [InlineData("Memo", "Range", "100")]
    [InlineData("Amount", "Contains", "100")]          // Amount 不允许 Contains
    [InlineData("Amount", "StartsWith", "100")]
    [InlineData("Amount", "EndsWith", "100")]
    [InlineData("Amount", "Regex", "100")]
    [InlineData("Amount", "Equals", "abc")]            // Amount 值必须是数字
    public async Task CreateAsync_InvalidFieldOperatorCombination_ShouldThrowValidation(
        string matchField, string matchOperator, string matchValue)
    {
        var request = new CreateTagRuleRequest
        {
            RuleName = "非法组合",
            TargetScope = "Transaction",
            MatchField = matchField,
            MatchOperator = matchOperator,
            MatchValue = matchValue,
            Priority = 10
        };

        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(request));
    }

    [Theory]
    [InlineData("100", "abc")]   // max 非数字
    [InlineData("100", "50")]    // max < min
    public async Task CreateAsync_InvalidAmountRangeBounds_ShouldThrowValidation(
        string min, string max)
    {
        var request = new CreateTagRuleRequest
        {
            RuleName = "非法 Range 区间",
            TargetScope = "Transaction",
            MatchField = "Amount",
            MatchOperator = "Range",
            MatchValue = min,
            MatchValueMax = max,
            Priority = 10
        };

        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(request));
    }

    // ─────────────────────── PreviewRerunAsync ───────────────────────

    [Fact]
    public async Task PreviewRerunAsync_WithMatchingRule_ReturnsCandidate()
    {
        var rule = BuildRuleWithTag(RuleMatchField.CounterpartyName, RuleMatchOperator.Contains, "Ali", tagId: 100, tagName: "VIP");
        var tx = BuildTxWithCounterparty("Alibaba Cloud");
        SetupRunContext(rule, tx);

        var result = await _service.PreviewRerunAsync(new RerunPreviewRequest { TargetScope = "Transaction" });

        result.TotalScanned.Should().Be(1);
        result.TotalAffected.Should().Be(1);
        result.TotalTagsToAdd.Should().Be(1);
        result.Candidates.Should().HaveCount(1);
        result.Candidates[0].TransactionId.Should().Be(1);
        result.Candidates[0].MatchedRules.Should().ContainSingle().Which.RuleName.Should().Be("test rule");
        result.Candidates[0].TagsToAdd.Should().ContainSingle().Which.TagName.Should().Be("VIP");
    }

    [Fact]
    public async Task PreviewRerunAsync_WithExistingBinding_OmitsCandidate()
    {
        var rule = BuildRuleWithTag(RuleMatchField.CounterpartyName, RuleMatchOperator.Contains, "Ali", tagId: 100, tagName: "VIP");
        var tx = BuildTxWithCounterparty("Alibaba Cloud");
        MockHelpers.SetupRepo(_tagRuleRepositoryMock, rule);
        MockHelpers.SetupRepo(_transactionRepositoryMock, tx);
        MockHelpers.SetupRepo(_tagBindingRepositoryMock, new TagBinding
        {
            Id = 1, OwnerType = TagScope.Transaction, OwnerId = 1, TagId = 100
        });

        var result = await _service.PreviewRerunAsync(new RerunPreviewRequest { TargetScope = "Transaction" });

        result.TotalScanned.Should().Be(1);
        result.TotalAffected.Should().Be(0);
        result.Candidates.Should().BeEmpty();
    }

    [Fact]
    public async Task PreviewRerunAsync_NoMatchingRule_ReturnsEmptyCandidates()
    {
        var rule = BuildRuleWithTag(RuleMatchField.CounterpartyName, RuleMatchOperator.Contains, "xxx", tagId: 100, tagName: "VIP");
        var tx = BuildTxWithCounterparty("Alibaba Cloud");
        SetupRunContext(rule, tx);

        var result = await _service.PreviewRerunAsync(new RerunPreviewRequest { TargetScope = "Transaction" });

        result.TotalScanned.Should().Be(1);
        result.TotalAffected.Should().Be(0);
        result.Candidates.Should().BeEmpty();
    }

    // ─────────────────────── ConfirmRerunAsync ───────────────────────

    [Fact]
    public async Task ConfirmRerunAsync_WithEmptyTransactionIds_ShouldThrowValidation()
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            _service.ConfirmRerunAsync(new RerunConfirmRequest
            {
                TargetScope = "Transaction",
                TransactionIds = new List<long>()
            }));
    }

    [Fact]
    public async Task ConfirmRerunAsync_WithMatchingRule_WritesBindingAndReturnsCount()
    {
        var rule = BuildRuleWithTag(RuleMatchField.CounterpartyName, RuleMatchOperator.Contains, "Ali", tagId: 100, tagName: "VIP");
        var tx = BuildTxWithCounterparty("Alibaba Cloud");
        SetupRunContext(rule, tx);

        var result = await _service.ConfirmRerunAsync(new RerunConfirmRequest
        {
            TargetScope = "Transaction",
            TransactionIds = new List<long> { 1 }
        });

        result.ScannedCount.Should().Be(1);
        result.AddedCount.Should().Be(1);
        result.SkippedCount.Should().Be(0);
        result.Message.Should().Contain("新增");
    }

    // ─────────────────────── 辅助 ───────────────────────

    private void SetupRunContext(TagRule rule, Transaction tx)
    {
        MockHelpers.SetupRepo(_tagRuleRepositoryMock, rule);
        MockHelpers.SetupRepo(_transactionRepositoryMock, tx);
        MockHelpers.SetupRepo(_tagBindingRepositoryMock, Array.Empty<TagBinding>());
    }

    private static TagRule BuildRule(
        RuleMatchField field,
        RuleMatchOperator op,
        string matchValue,
        string? matchValueMax = null,
        long tagId = 100)
    {
        return new TagRule
        {
            Id = 1,
            RuleName = "test rule",
            TargetScope = TagScope.Transaction,
            MatchField = field,
            MatchOperator = op,
            MatchValue = matchValue,
            MatchValueMax = matchValueMax,
            Priority = 1,
            IsActive = true,
            TagRuleTags = new List<TagRuleTag>
            {
                new() { TagRuleId = 1, TagId = tagId }
            }
        };
    }

    // BuildRule 变体：带 Tag 导航对象（供 Preview 读取 Name/Color）
    private static TagRule BuildRuleWithTag(
        RuleMatchField field,
        RuleMatchOperator op,
        string matchValue,
        long tagId,
        string tagName,
        string? tagColor = null)
    {
        var tag = new Tag { Id = tagId, Name = tagName, Color = tagColor, Scope = TagScope.Transaction };
        return new TagRule
        {
            Id = 1,
            RuleName = "test rule",
            TargetScope = TagScope.Transaction,
            MatchField = field,
            MatchOperator = op,
            MatchValue = matchValue,
            Priority = 1,
            IsActive = true,
            TagRuleTags = new List<TagRuleTag>
            {
                new() { TagRuleId = 1, TagId = tagId, Tag = tag }
            }
        };
    }

    private static Transaction BuildTxWithCounterparty(string counterparty)
    {
        return new Transaction
        {
            Id = 1,
            AccountId = 1,
            Amount = 0m,
            TransactionDate = DateTime.UtcNow,
            BankTransaction = new BankTransaction
            {
                Id = 1,
                AccountId = 1,
                Counterparty = counterparty,
                Amount = 0m,
                TransactionDate = DateTime.UtcNow
            }
        };
    }

    private static Transaction BuildTxWithAmount(decimal amount)
    {
        return new Transaction
        {
            Id = 1,
            AccountId = 1,
            Amount = amount,
            TransactionDate = DateTime.UtcNow,
            BankTransaction = new BankTransaction
            {
                Id = 1,
                AccountId = 1,
                Amount = amount,
                TransactionDate = DateTime.UtcNow
            }
        };
    }
}
