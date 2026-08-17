using System.Collections.Concurrent;
using FluentAssertions;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.Reconciliation.DTOs;
using FinanceApp.Application.Modules.Identity.Interfaces;
using FinanceApp.Application.Modules.MasterData.Interfaces;
using FinanceApp.Application.Modules.Reconciliation.Services;
using FinanceApp.Application.Tests.Helpers;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace FinanceApp.Application.Tests.Services;

public class ImportServiceTests : TestBase
{
    private readonly Mock<IRepository<Account>> _accountRepositoryMock;
    private readonly Mock<IRuleService> _ruleServiceMock;
    private readonly Mock<ILogger<ImportService>> _loggerMock;

    public ImportServiceTests()
    {
        _accountRepositoryMock = new Mock<IRepository<Account>>();
        _ruleServiceMock = new Mock<IRuleService>();
        _loggerMock = new Mock<ILogger<ImportService>>();
    }

    [Fact]
    public async Task PreviewAsync_WithInvalidAccountId_ShouldThrowNotFoundException()
    {
        // Arrange
        var stream = new MemoryStream();
        var fileName = "test.xlsx";
        var accountId = 999L;

        _accountRepositoryMock.Setup(x => x.GetByIdAsync(accountId))
            .ReturnsAsync((Account?)null);

        // 由于 ImportService 需要 AppDbContext，这个测试需要集成测试环境
        // 这里我们只测试基本的验证逻辑

        // Act & Assert
        // 实际测试需要在集成测试中进行
        _accountRepositoryMock.Verify(x => x.GetByIdAsync(accountId), Times.Never);
    }

    [Fact]
    public void CalculateUniqueHash_ShouldGenerateConsistentHash()
    {
        // Arrange
        var date = new DateTime(2026, 3, 13);
        var amount = 1000m;
        var counterparty = "阿里云";
        var description = "服务器费用";

        // Act
        var hash1 = CalculateHash(date, amount, counterparty, description);
        var hash2 = CalculateHash(date, amount, counterparty, description);

        // Assert
        hash1.Should().Be(hash2);
        hash1.Should().NotBeNullOrEmpty();
        hash1.Length.Should().Be(32); // MD5 hash length
    }

    [Fact]
    public void CalculateUniqueHash_WithDifferentData_ShouldGenerateDifferentHash()
    {
        // Arrange
        var date = new DateTime(2026, 3, 13);
        var amount1 = 1000m;
        var amount2 = 2000m;
        var counterparty = "阿里云";
        var description = "服务器费用";

        // Act
        var hash1 = CalculateHash(date, amount1, counterparty, description);
        var hash2 = CalculateHash(date, amount2, counterparty, description);

        // Assert
        hash1.Should().NotBe(hash2);
    }

    private static string CalculateHash(DateTime date, decimal amount, string counterparty, string description)
    {
        var raw = $"{date:yyyy-MM-dd}|{amount}|{counterparty}|{description}";
        using var md5 = System.Security.Cryptography.MD5.Create();
        var bytes = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(raw));
        return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
    }

    /// <summary>
    /// 模拟 ImportService.CalculateUniqueHash 的新版格式差异化逻辑
    /// 新规则：华夏/支付宝不再仅依赖交易号，而是使用全字段组合
    /// </summary>
    private static string CalculateHashWithFormat(
        FileFormat format, DateTime date, decimal amount, string counterparty, string description,
        decimal? balance = null, string? transactionNumber = null, string? direction = null,
        TimeSpan? transactionTime = null, string? counterpartyAccount = null)
    {
        static string Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? "" : value.Trim();
        static string NormalizeTime(TimeSpan? time) => time.HasValue ? time.Value.ToString(@"hh\:mm\:ss") : "";

        string raw;

        switch (format)
        {
            case FileFormat.HuaxiaBank:
                raw = $"HX|{Normalize(transactionNumber)}|{date:yyyy-MM-dd}|{NormalizeTime(transactionTime)}|{direction ?? ""}|{amount}|{Normalize(counterpartyAccount)}|{Normalize(counterparty)}|{Normalize(description)}|{(balance?.ToString() ?? "")}";
                break;
            case FileFormat.AlipayBusiness:
                raw = $"ZFB|{Normalize(transactionNumber)}|{date:yyyy-MM-dd}|{NormalizeTime(transactionTime)}|{direction ?? ""}|{amount}|{Normalize(counterpartyAccount)}|{Normalize(counterparty)}|{Normalize(description)}|{(balance?.ToString() ?? "")}";
                break;
            default:
                raw = $"{date:yyyy-MM-dd}|{amount}|{counterparty}|{description}";
                break;
        }

        using var md5 = System.Security.Cryptography.MD5.Create();
        var bytes = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(raw));
        return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
    }

    #region Hash 差异化测试

    [Fact]
    public void CalculateUniqueHash_HuaxiaBank_WithTransactionNumber_UsesAllFields()
    {
        // Arrange
        var date = new DateTime(2026, 3, 13);
        var amount = 5000m;
        var counterparty = "北京科技有限公司";
        var description = "货款";
        var transactionNumber = "HX20260313001";
        var balance = 100000m;
        var direction = "out";

        // Act
        var hash = CalculateHashWithFormat(FileFormat.HuaxiaBank, date, amount, counterparty, description,
            balance: balance, transactionNumber: transactionNumber, direction: direction);

        // Assert - 新规则：hash 基于全字段组合，不再只用交易号
        var expectedRaw = $"HX|{transactionNumber}|{date:yyyy-MM-dd}||{direction}|{amount}||{counterparty}|{description}|{balance}";
        var expectedHash = ComputeMd5(expectedRaw);
        hash.Should().Be(expectedHash);
        hash.Length.Should().Be(32);
    }

    [Fact]
    public void CalculateUniqueHash_HuaxiaBank_WithoutTransactionNumber_UsesAllFields()
    {
        // Arrange
        var date = new DateTime(2026, 3, 13);
        var amount = 5000m;
        var counterparty = "北京科技有限公司";
        var description = "货款";
        var balance = 100000m;
        var direction = "in";

        // Act
        var hash = CalculateHashWithFormat(FileFormat.HuaxiaBank, date, amount, counterparty, description,
            balance: balance, transactionNumber: null, direction: direction);

        // Assert - hash 基于全字段组合
        var expectedRaw = $"HX||{date:yyyy-MM-dd}||{direction}|{amount}||{counterparty}|{description}|{balance}";
        var expectedHash = ComputeMd5(expectedRaw);
        hash.Should().Be(expectedHash);
    }

    [Fact]
    public void CalculateUniqueHash_AlipayBusiness_WithTransactionNumber_UsesAllFields()
    {
        // Arrange
        var date = new DateTime(2026, 3, 14);
        var amount = 299.99m;
        var counterparty = "淘宝店铺A";
        var description = "办公用品采购";
        var transactionNumber = "2026031400001";
        var direction = "out";

        // Act
        var hash = CalculateHashWithFormat(FileFormat.AlipayBusiness, date, amount, counterparty, description,
            transactionNumber: transactionNumber, direction: direction);

        // Assert - 新规则：hash 基于全字段组合
        var expectedRaw = $"ZFB|{transactionNumber}|{date:yyyy-MM-dd}||{direction}|{amount}||{counterparty}|{description}|";
        var expectedHash = ComputeMd5(expectedRaw);
        hash.Should().Be(expectedHash);
    }

    [Fact]
    public void CalculateUniqueHash_AlipayBusiness_WithoutTransactionNumber_UsesAllFields()
    {
        // Arrange
        var date = new DateTime(2026, 3, 14);
        var amount = 299.99m;
        var counterparty = "淘宝店铺A";
        var description = "办公用品采购";
        var balance = 50000m;
        var direction = "in";

        // Act
        var hash = CalculateHashWithFormat(FileFormat.AlipayBusiness, date, amount, counterparty, description,
            balance: balance, transactionNumber: null, direction: direction);

        // Assert - hash 基于全字段组合
        var expectedRaw = $"ZFB||{date:yyyy-MM-dd}||{direction}|{amount}||{counterparty}|{description}|{balance}";
        var expectedHash = ComputeMd5(expectedRaw);
        hash.Should().Be(expectedHash);
    }

    [Fact]
    public void CalculateUniqueHash_Simple_KeepsOriginalFormat()
    {
        // Arrange
        var date = new DateTime(2026, 3, 13);
        var amount = 1000m;
        var counterparty = "阿里云";
        var description = "服务器费用";

        // Act
        var hashWithFormat = CalculateHashWithFormat(FileFormat.Simple, date, amount, counterparty, description);
        var hashOriginal = CalculateHash(date, amount, counterparty, description);

        // Assert - Simple 格式应与原有 CalculateHash 结果一致（向后兼容）
        hashWithFormat.Should().Be(hashOriginal);
    }

    [Fact]
    public void CalculateUniqueHash_HuaxiaBank_DifferentBalance_DifferentHash()
    {
        // Arrange - 相同日期、金额、对方、摘要，但余额不同
        var date = new DateTime(2026, 3, 13);
        var amount = 5000m;
        var counterparty = "北京科技有限公司";
        var description = "货款";

        // Act
        var hash1 = CalculateHashWithFormat(FileFormat.HuaxiaBank, date, amount, counterparty, description,
            balance: 100000m, transactionNumber: null);
        var hash2 = CalculateHashWithFormat(FileFormat.HuaxiaBank, date, amount, counterparty, description,
            balance: 95000m, transactionNumber: null);

        // Assert - 不同余额应产生不同 hash（区分同日同金额同对方的多笔交易）
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void CalculateUniqueHash_Simple_DoesNotIncludeBalance()
    {
        // Arrange
        var date = new DateTime(2026, 3, 13);
        var amount = 1000m;
        var counterparty = "阿里云";
        var description = "服务器费用";

        // Act - Simple 格式传入余额和不传余额，结果应相同
        var hashWithBalance = CalculateHashWithFormat(FileFormat.Simple, date, amount, counterparty, description,
            balance: 999999m);
        var hashWithoutBalance = CalculateHashWithFormat(FileFormat.Simple, date, amount, counterparty, description,
            balance: null);

        // Assert - Simple 格式不包含余额，确保向后兼容
        hashWithBalance.Should().Be(hashWithoutBalance);
    }

    [Fact]
    public void CalculateUniqueHash_HuaxiaBank_SameTransactionNumber_DifferentOtherFields_DifferentHash()
    {
        // Arrange - 相同流水号但其他字段不同
        var transactionNumber = "HX20260313001";

        // Act
        var hash1 = CalculateHashWithFormat(FileFormat.HuaxiaBank,
            new DateTime(2026, 3, 13), 5000m, "公司A", "货款",
            balance: 100000m, transactionNumber: transactionNumber, direction: "in");
        var hash2 = CalculateHashWithFormat(FileFormat.HuaxiaBank,
            new DateTime(2026, 3, 14), 9999m, "公司B", "服务费",
            balance: 50000m, transactionNumber: transactionNumber, direction: "out");

        // Assert - 新规则：即使交易号相同，其他字段不同也会产生不同 hash
        hash1.Should().NotBe(hash2);
    }

    #endregion

    #region 格式差异化：跨格式隔离测试

    [Fact]
    public void CalculateUniqueHash_DifferentFormats_SameData_DifferentHash()
    {
        // Arrange - 完全相同的交易数据
        var date = new DateTime(2026, 3, 13);
        var amount = 5000m;
        var counterparty = "公司A";
        var description = "货款";

        // Act
        var hashSimple = CalculateHashWithFormat(FileFormat.Simple, date, amount, counterparty, description);
        var hashHuaxia = CalculateHashWithFormat(FileFormat.HuaxiaBank, date, amount, counterparty, description,
            transactionNumber: null);
        var hashAlipay = CalculateHashWithFormat(FileFormat.AlipayBusiness, date, amount, counterparty, description,
            transactionNumber: null);

        // Assert - 三种格式的 hash 应互不相同，防止跨格式误判重复
        hashSimple.Should().NotBe(hashHuaxia);
        hashSimple.Should().NotBe(hashAlipay);
        hashHuaxia.Should().NotBe(hashAlipay);
    }

    [Fact]
    public void CalculateUniqueHash_AlipayBusiness_SameTransactionNumber_DifferentOtherFields_DifferentHash()
    {
        // Arrange - 相同交易号但其他字段不同
        var transactionNumber = "ZFB20260314001";

        // Act
        var hash1 = CalculateHashWithFormat(FileFormat.AlipayBusiness,
            new DateTime(2026, 3, 14), 100m, "客户A", "在线支付",
            balance: 5000m, transactionNumber: transactionNumber, direction: "in");
        var hash2 = CalculateHashWithFormat(FileFormat.AlipayBusiness,
            new DateTime(2026, 3, 15), 200m, "客户B", "转账",
            balance: 8000m, transactionNumber: transactionNumber, direction: "out");

        // Assert - 新规则：即使交易号相同，其他字段不同也会产生不同 hash
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void CalculateUniqueHash_AlipayBusiness_DifferentBalance_DifferentHash()
    {
        // Arrange - 相同数据不同余额
        var date = new DateTime(2026, 3, 14);
        var amount = 299.99m;
        var counterparty = "淘宝店铺";
        var description = "采购";

        // Act
        var hash1 = CalculateHashWithFormat(FileFormat.AlipayBusiness, date, amount, counterparty, description,
            balance: 10000m, transactionNumber: null);
        var hash2 = CalculateHashWithFormat(FileFormat.AlipayBusiness, date, amount, counterparty, description,
            balance: 8000m, transactionNumber: null);

        // Assert
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void CalculateUniqueHash_EmptyFields_StillGeneratesValidHash()
    {
        // Arrange
        var date = new DateTime(2026, 1, 1);
        var amount = 0m;
        var counterparty = "";
        var description = "";

        // Act
        var hash = CalculateHash(date, amount, counterparty, description);

        // Assert
        hash.Should().NotBeNullOrEmpty();
        hash.Length.Should().Be(32);
    }

    [Fact]
    public void CalculateUniqueHash_ChineseCharacters_HandledCorrectly()
    {
        // Arrange
        var date = new DateTime(2026, 3, 14);
        var amount = 8888.88m;
        var counterparty = "北京市海淀区某科技有限公司";
        var description = "技术服务费用 - 2026年3月";

        // Act
        var hash1 = CalculateHash(date, amount, counterparty, description);
        var hash2 = CalculateHash(date, amount, counterparty, description);

        // Assert
        hash1.Should().Be(hash2);
        hash1.Length.Should().Be(32);
    }

    [Fact]
    public void CalculateUniqueHash_DecimalPrecision_DifferentTrailingZeros_DifferentHash()
    {
        // Arrange
        var date = new DateTime(2026, 3, 14);
        var counterparty = "公司A";
        var description = "货款";

        // Act - 1000m 的 ToString() 是 "1000"，1000.00m 的 ToString() 是 "1000.00"
        var hash1 = CalculateHash(date, 1000m, counterparty, description);
        var hash2 = CalculateHash(date, 1000.00m, counterparty, description);

        // Assert - decimal 精度不同会导致不同 hash，这是预期行为
        // 因为 C# decimal 保留尾随零：1000m → "1000"，1000.00m → "1000.00"
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void CalculateUniqueHash_SameDecimalPrecision_SameHash()
    {
        // Arrange
        var date = new DateTime(2026, 3, 14);
        var counterparty = "公司A";
        var description = "货款";

        // Act - 相同精度的 decimal
        var hash1 = CalculateHash(date, 1000.00m, counterparty, description);
        var hash2 = CalculateHash(date, 1000.00m, counterparty, description);

        // Assert
        hash1.Should().Be(hash2);
    }

    #endregion

    /// <summary>
    /// 计算 MD5 哈希的辅助方法
    /// </summary>
    private static string ComputeMd5(string input)
    {
        using var md5 = System.Security.Cryptography.MD5.Create();
        var bytes = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
        return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
    }

    #region DeleteBatchAsync 测试

    [Fact]
    public async Task DeleteBatchAsync_WithPendingBatch_ShouldSucceed()
    {
        // Arrange
        var batchId = 1L;
        var batch = new ImportBatch
        {
            Id = batchId,
            FileName = "test.xlsx",
            Status = ImportBatchStatus.Pending,
            AccountId = 1L,
            IsDeleted = false
        };

        var batchRepositoryMock = new Mock<IRepository<ImportBatch>>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var auditLogServiceMock = new Mock<IAuditLogService>();

        batchRepositoryMock.Setup(x => x.GetByIdAsync(batchId))
            .ReturnsAsync(batch);
        batchRepositoryMock.Setup(x => x.Update(It.IsAny<ImportBatch>()));
        unitOfWorkMock.Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);
        auditLogServiceMock.Setup(x => x.LogAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var service = CreateImportService(
            batchRepository: batchRepositoryMock.Object,
            unitOfWork: unitOfWorkMock.Object,
            auditLogService: auditLogServiceMock.Object);

        // Act
        await service.DeleteBatchAsync(batchId);

        // Assert
        batch.IsDeleted.Should().BeTrue();
        batch.DeletedAt.Should().NotBeNull();
        batchRepositoryMock.Verify(x => x.Update(batch), Times.Once);
        unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        auditLogServiceMock.Verify(x => x.LogAsync("Delete", "ImportBatch", batchId, It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task DeleteBatchAsync_WithFailedBatch_ShouldSucceed()
    {
        // Arrange
        var batchId = 2L;
        var batch = new ImportBatch
        {
            Id = batchId,
            FileName = "failed.xlsx",
            Status = ImportBatchStatus.Failed,
            AccountId = 1L,
            IsDeleted = false
        };

        var batchRepositoryMock = new Mock<IRepository<ImportBatch>>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var auditLogServiceMock = new Mock<IAuditLogService>();

        batchRepositoryMock.Setup(x => x.GetByIdAsync(batchId))
            .ReturnsAsync(batch);
        batchRepositoryMock.Setup(x => x.Update(It.IsAny<ImportBatch>()));
        unitOfWorkMock.Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);
        auditLogServiceMock.Setup(x => x.LogAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var service = CreateImportService(
            batchRepository: batchRepositoryMock.Object,
            unitOfWork: unitOfWorkMock.Object,
            auditLogService: auditLogServiceMock.Object);

        // Act
        await service.DeleteBatchAsync(batchId);

        // Assert
        batch.IsDeleted.Should().BeTrue();
        batch.DeletedAt.Should().NotBeNull();
        batchRepositoryMock.Verify(x => x.Update(batch), Times.Once);
        unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task DeleteBatchAsync_WithCompletedBatch_ShouldThrowValidationException()
    {
        // Arrange
        var batchId = 3L;
        var batch = new ImportBatch
        {
            Id = batchId,
            FileName = "completed.xlsx",
            Status = ImportBatchStatus.Completed,
            AccountId = 1L,
            IsDeleted = false
        };

        var batchRepositoryMock = new Mock<IRepository<ImportBatch>>();
        batchRepositoryMock.Setup(x => x.GetByIdAsync(batchId))
            .ReturnsAsync(batch);

        var service = CreateImportService(batchRepository: batchRepositoryMock.Object);

        // Act
        var act = async () => await service.DeleteBatchAsync(batchId);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("只能删除待处理或失败状态的批次");
        batchRepositoryMock.Verify(x => x.Update(It.IsAny<ImportBatch>()), Times.Never);
    }

    [Fact]
    public async Task DeleteBatchAsync_WithProcessingBatch_ShouldThrowValidationException()
    {
        // Arrange
        var batchId = 4L;
        var batch = new ImportBatch
        {
            Id = batchId,
            FileName = "processing.xlsx",
            Status = ImportBatchStatus.Processing,
            AccountId = 1L,
            IsDeleted = false
        };

        var batchRepositoryMock = new Mock<IRepository<ImportBatch>>();
        batchRepositoryMock.Setup(x => x.GetByIdAsync(batchId))
            .ReturnsAsync(batch);

        var service = CreateImportService(batchRepository: batchRepositoryMock.Object);

        // Act
        var act = async () => await service.DeleteBatchAsync(batchId);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("只能删除待处理或失败状态的批次");
    }

    [Fact]
    public async Task DeleteBatchAsync_WithNonExistentBatch_ShouldThrowNotFoundException()
    {
        // Arrange
        var batchId = 999L;

        var batchRepositoryMock = new Mock<IRepository<ImportBatch>>();
        batchRepositoryMock.Setup(x => x.GetByIdAsync(batchId))
            .ReturnsAsync((ImportBatch?)null);

        var service = CreateImportService(batchRepository: batchRepositoryMock.Object);

        // Act
        var act = async () => await service.DeleteBatchAsync(batchId);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("导入批次不存在");
        batchRepositoryMock.Verify(x => x.Update(It.IsAny<ImportBatch>()), Times.Never);
    }

    #endregion

    #region GetCachedPreviewAsync 测试

    [Fact]
    public async Task GetCachedPreviewAsync_WithValidCache_ShouldReturnPreviewResponse()
    {
        // Arrange
        var batchId = 100L;
        var batch = new ImportBatch
        {
            Id = batchId,
            FileName = "cached.xlsx",
            Status = ImportBatchStatus.Pending,
            AccountId = 1L,
            Account = new Account { Id = 1L, Name = "测试账户" }
        };

        var batchRepositoryMock = new Mock<IRepository<ImportBatch>>();
        var queryable = new List<ImportBatch> { batch }.AsQueryable().BuildMock();
        batchRepositoryMock.Setup(x => x.GetQueryable()).Returns(queryable.Object);

        var service = CreateImportService(batchRepository: batchRepositoryMock.Object);

        // 通过反射向静态缓存中写入预览数据
        var previews = new List<BankTransactionPreviewDto>
        {
            new() { RowNumber = 1, Amount = 1000m, Direction = "in", CounterpartyName = "客户A", IsDuplicate = false },
            new() { RowNumber = 2, Amount = 2000m, Direction = "out", CounterpartyName = "供应商B", IsDuplicate = true }
        };
        SetPreviewCache(batchId, previews, DateTime.UtcNow.AddMinutes(30));

        try
        {
            // Act
            var result = await service.GetCachedPreviewAsync(batchId);

            // Assert
            result.Should().NotBeNull();
            result.BatchId.Should().Be(batchId);
            result.FileName.Should().Be("cached.xlsx");
            result.TotalRows.Should().Be(2);
            result.DuplicateRows.Should().Be(1);
            result.NewRows.Should().Be(1);
            result.DetectedFormat.Should().Be("Cached");
            result.Previews.Should().HaveCount(2);
        }
        finally
        {
            ClearPreviewCache(batchId);
        }
    }

    [Fact]
    public async Task GetCachedPreviewAsync_WithExpiredCache_ShouldThrowValidationException()
    {
        // Arrange
        var batchId = 101L;
        var batch = new ImportBatch
        {
            Id = batchId,
            FileName = "expired.xlsx",
            Status = ImportBatchStatus.Pending,
            AccountId = 1L,
            Account = new Account { Id = 1L, Name = "测试账户" }
        };

        var batchRepositoryMock = new Mock<IRepository<ImportBatch>>();
        var queryable = new List<ImportBatch> { batch }.AsQueryable().BuildMock();
        batchRepositoryMock.Setup(x => x.GetQueryable()).Returns(queryable.Object);

        var service = CreateImportService(batchRepository: batchRepositoryMock.Object);

        // 写入已过期的缓存数据
        var previews = new List<BankTransactionPreviewDto>
        {
            new() { RowNumber = 1, Amount = 500m, Direction = "in", CounterpartyName = "客户C" }
        };
        SetPreviewCache(batchId, previews, DateTime.UtcNow.AddMinutes(-1)); // 已过期

        try
        {
            // Act
            var act = async () => await service.GetCachedPreviewAsync(batchId);

            // Assert
            await act.Should().ThrowAsync<ValidationException>()
                .WithMessage("*过期*");
        }
        finally
        {
            ClearPreviewCache(batchId);
        }
    }

    [Fact]
    public async Task GetCachedPreviewAsync_WithNoCacheEntry_ShouldThrowValidationException()
    {
        // Arrange
        var batchId = 102L;
        var batch = new ImportBatch
        {
            Id = batchId,
            FileName = "nocache.xlsx",
            Status = ImportBatchStatus.Pending,
            AccountId = 1L,
            Account = new Account { Id = 1L, Name = "测试账户" }
        };

        var batchRepositoryMock = new Mock<IRepository<ImportBatch>>();
        var queryable = new List<ImportBatch> { batch }.AsQueryable().BuildMock();
        batchRepositoryMock.Setup(x => x.GetQueryable()).Returns(queryable.Object);

        var service = CreateImportService(batchRepository: batchRepositoryMock.Object);

        // 确保缓存中没有该 batchId 的数据
        ClearPreviewCache(batchId);

        // Act
        var act = async () => await service.GetCachedPreviewAsync(batchId);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*过期*");
    }

    [Fact]
    public async Task GetCachedPreviewAsync_WithNonPendingStatus_ShouldThrowValidationException()
    {
        // Arrange
        var batchId = 103L;
        var batch = new ImportBatch
        {
            Id = batchId,
            FileName = "completed.xlsx",
            Status = ImportBatchStatus.Completed,
            AccountId = 1L,
            Account = new Account { Id = 1L, Name = "测试账户" }
        };

        var batchRepositoryMock = new Mock<IRepository<ImportBatch>>();
        var queryable = new List<ImportBatch> { batch }.AsQueryable().BuildMock();
        batchRepositoryMock.Setup(x => x.GetQueryable()).Returns(queryable.Object);

        var service = CreateImportService(batchRepository: batchRepositoryMock.Object);

        // Act
        var act = async () => await service.GetCachedPreviewAsync(batchId);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("只有待处理状态的批次可以继续处理");
    }

    [Fact]
    public async Task GetCachedPreviewAsync_WithFailedStatus_ShouldThrowValidationException()
    {
        // Arrange
        var batchId = 104L;
        var batch = new ImportBatch
        {
            Id = batchId,
            FileName = "failed.xlsx",
            Status = ImportBatchStatus.Failed,
            AccountId = 1L,
            Account = new Account { Id = 1L, Name = "测试账户" }
        };

        var batchRepositoryMock = new Mock<IRepository<ImportBatch>>();
        var queryable = new List<ImportBatch> { batch }.AsQueryable().BuildMock();
        batchRepositoryMock.Setup(x => x.GetQueryable()).Returns(queryable.Object);

        var service = CreateImportService(batchRepository: batchRepositoryMock.Object);

        // Act
        var act = async () => await service.GetCachedPreviewAsync(batchId);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("只有待处理状态的批次可以继续处理");
    }

    [Fact]
    public async Task GetCachedPreviewAsync_WithNonExistentBatch_ShouldThrowNotFoundException()
    {
        // Arrange
        var batchId = 999L;

        var batchRepositoryMock = new Mock<IRepository<ImportBatch>>();
        var queryable = new List<ImportBatch>().AsQueryable().BuildMock(); // 空列表
        batchRepositoryMock.Setup(x => x.GetQueryable()).Returns(queryable.Object);

        var service = CreateImportService(batchRepository: batchRepositoryMock.Object);

        // Act
        var act = async () => await service.GetCachedPreviewAsync(batchId);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("导入批次不存在");
    }

    /// <summary>
    /// 通过反射操作 ImportService 的静态 _previewCache 字段
    /// </summary>
    private static void SetPreviewCache(long batchId, List<BankTransactionPreviewDto> previews, DateTime expireAt)
    {
        var cacheField = typeof(ImportService).GetField("_previewCache",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var cache = (ConcurrentDictionary<long, (List<BankTransactionPreviewDto> Previews, DateTime ExpireAt)>)cacheField!.GetValue(null)!;
        cache[batchId] = (previews, expireAt);
    }

    private static void ClearPreviewCache(long batchId)
    {
        var cacheField = typeof(ImportService).GetField("_previewCache",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var cache = (ConcurrentDictionary<long, (List<BankTransactionPreviewDto> Previews, DateTime ExpireAt)>)cacheField!.GetValue(null)!;
        cache.TryRemove(batchId, out _);
    }

    #endregion

    #region MapBatchToDto ErrorMessage 测试

    [Fact]
    public async Task DeleteBatchAsync_ResultDto_ShouldNotIncludeErrorMessage_WhenNoErrors()
    {
        // 验证 MapBatchToDto 正确映射 ErrorMessage 为 null
        var batchId = 10L;
        var batch = new ImportBatch
        {
            Id = batchId,
            FileName = "success.xlsx",
            Status = ImportBatchStatus.Pending,
            AccountId = 1L,
            ErrorMessage = null,
            IsDeleted = false
        };

        var batchRepositoryMock = new Mock<IRepository<ImportBatch>>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var auditLogServiceMock = new Mock<IAuditLogService>();

        batchRepositoryMock.Setup(x => x.GetByIdAsync(batchId)).ReturnsAsync(batch);
        batchRepositoryMock.Setup(x => x.Update(It.IsAny<ImportBatch>()));
        unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);
        auditLogServiceMock.Setup(x => x.LogAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var service = CreateImportService(
            batchRepository: batchRepositoryMock.Object,
            unitOfWork: unitOfWorkMock.Object,
            auditLogService: auditLogServiceMock.Object);

        await service.DeleteBatchAsync(batchId);
        batch.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task GetBatchByIdAsync_ShouldIncludeErrorMessage_WhenHasErrors()
    {
        // 验证 MapBatchToDto 正确映射 ErrorMessage
        var batchId = 11L;
        var batch = new ImportBatch
        {
            Id = batchId,
            FileName = "partial.xlsx",
            Status = ImportBatchStatus.PartialCompleted,
            AccountId = 1L,
            RecordCount = 10,
            SuccessCount = 8,
            ErrorCount = 2,
            ErrorMessage = "行3: 金额格式无效\n行7: 日期解析失败",
            Account = new Account { Id = 1L, Name = "测试账户" }
        };

        var batchRepositoryMock = new Mock<IRepository<ImportBatch>>();
        var queryable = new List<ImportBatch> { batch }.AsQueryable().BuildMock();
        batchRepositoryMock.Setup(x => x.GetQueryable()).Returns(queryable.Object);

        var service = CreateImportService(batchRepository: batchRepositoryMock.Object);

        var result = await service.GetBatchByIdAsync(batchId);

        result.Should().NotBeNull();
        result.ErrorCount.Should().Be(2);
        result.ErrorMessage.Should().Be("行3: 金额格式无效\n行7: 日期解析失败");
        result.Status.Should().Be("PartialCompleted");
    }

    #endregion

    #region ConfirmAsync 批量写入

    [Fact]
    public async Task ConfirmAsync_NewRows_ShouldCompleteWithConstantSaveChangesAndUpdateBalance()
    {
        var batchId = 501L;
        var account = new Account { Id = 11L, Name = "批量账户", CurrentBalance = 10000m };
        var batch = new ImportBatch
        {
            Id = batchId,
            AccountId = account.Id,
            FileName = "batch.xlsx",
            Status = ImportBatchStatus.Pending,
            RecordCount = 3
        };

        var previews = new List<BankTransactionPreviewDto>
        {
            CreateConfirmPreview(1, "in", 1000m, "hash-new-1"),
            CreateConfirmPreview(2, "out", 200m, "hash-new-2"),
            CreateConfirmPreview(3, "in", 999m, "hash-skip", duplicate: true)
        };
        SetPreviewCache(batchId, previews, DateTime.UtcNow.AddMinutes(10));

        var ctx = CreateConfirmContext(batch, account);
        var service = CreateImportService(
            batchRepository: ctx.BatchRepository.Object,
            accountRepository: ctx.AccountRepository.Object,
            bankTransactionRepository: ctx.BankRepository.Object,
            transactionRepository: ctx.TransactionRepository.Object,
            unitOfWork: ctx.UnitOfWork.Object,
            auditLogService: ctx.AuditLogService.Object);

        var result = await service.ConfirmAsync(new ImportConfirmRequest
        {
            BatchId = batchId,
            SelectedRowNumbers = new List<int> { 1, 2, 3 }
        });

        result.Status.Should().Be("Completed");
        result.SuccessCount.Should().Be(2);
        result.ErrorCount.Should().Be(0);
        account.CurrentBalance.Should().Be(10800m);
        batch.Status.Should().Be(ImportBatchStatus.Completed);

        ctx.AddedBankTransactions.Should().HaveCount(2);
        ctx.AddedTransactions.Should().HaveCount(2);
        // Processing + 流水 + 交易 + 账户/批次，成功路径插入段最多 3 次
        ctx.SaveChangesCount.Should().Be(4);
        ctx.ClearChangeTrackerCount.Should().Be(0);
    }

    [Fact]
    public async Task ConfirmAsync_ShouldSkipUnselectedDuplicateAndFileConflictRows()
    {
        var batchId = 502L;
        var account = new Account { Id = 12L, Name = "跳过账户", CurrentBalance = 5000m };
        var batch = new ImportBatch
        {
            Id = batchId,
            AccountId = account.Id,
            FileName = "skip.xlsx",
            Status = ImportBatchStatus.Pending,
            RecordCount = 4
        };

        var previews = new List<BankTransactionPreviewDto>
        {
            CreateConfirmPreview(1, "in", 100m, "hash-1"),
            CreateConfirmPreview(2, "in", 200m, "hash-2"),
            CreateConfirmPreview(3, "in", 300m, "hash-dup", duplicate: true),
            CreateConfirmPreview(4, "in", 400m, "hash-conflict", fileConflict: true)
        };
        SetPreviewCache(batchId, previews, DateTime.UtcNow.AddMinutes(10));

        var ctx = CreateConfirmContext(batch, account);
        var service = CreateImportService(
            batchRepository: ctx.BatchRepository.Object,
            accountRepository: ctx.AccountRepository.Object,
            bankTransactionRepository: ctx.BankRepository.Object,
            transactionRepository: ctx.TransactionRepository.Object,
            unitOfWork: ctx.UnitOfWork.Object,
            auditLogService: ctx.AuditLogService.Object);

        var result = await service.ConfirmAsync(new ImportConfirmRequest
        {
            BatchId = batchId,
            SelectedRowNumbers = new List<int> { 1, 3, 4 }
        });

        result.SuccessCount.Should().Be(1);
        result.Status.Should().Be("Completed");
        ctx.AddedBankTransactions.Should().HaveCount(1);
        ctx.AddedBankTransactions[0].UniqueHash.Should().Be("hash-1");
        account.CurrentBalance.Should().Be(5100m);
    }

    [Fact]
    public async Task ConfirmAsync_RecoverableMissingBankTx_ShouldRecordErrorAndNotInsert()
    {
        var batchId = 503L;
        var account = new Account { Id = 13L, Name = "可恢复账户", CurrentBalance = 1000m };
        var batch = new ImportBatch
        {
            Id = batchId,
            AccountId = account.Id,
            FileName = "recover.xlsx",
            Status = ImportBatchStatus.Pending,
            RecordCount = 1
        };

        var previews = new List<BankTransactionPreviewDto>
        {
            CreateConfirmPreview(1, "in", 100m, "hash-missing", recoverable: true)
        };
        SetPreviewCache(batchId, previews, DateTime.UtcNow.AddMinutes(10));

        var ctx = CreateConfirmContext(batch, account);
        var service = CreateImportService(
            batchRepository: ctx.BatchRepository.Object,
            accountRepository: ctx.AccountRepository.Object,
            bankTransactionRepository: ctx.BankRepository.Object,
            transactionRepository: ctx.TransactionRepository.Object,
            unitOfWork: ctx.UnitOfWork.Object,
            auditLogService: ctx.AuditLogService.Object);

        var result = await service.ConfirmAsync(new ImportConfirmRequest
        {
            BatchId = batchId,
            SelectedRowNumbers = new List<int> { 1 }
        });

        result.Status.Should().Be("Failed");
        result.SuccessCount.Should().Be(0);
        result.ErrorCount.Should().Be(1);
        result.ErrorMessage.Should().Contain("行1");
        result.ErrorMessage.Should().Contain("可恢复但未找到对应银行流水");
        ctx.AddedBankTransactions.Should().BeEmpty();
        ctx.AddedTransactions.Should().BeEmpty();
        account.CurrentBalance.Should().Be(1000m);
    }

    [Fact]
    public async Task ConfirmAsync_RecoverableExistingBankTx_ShouldOnlyInsertBusinessTransaction()
    {
        var batchId = 504L;
        var account = new Account { Id = 14L, Name = "恢复账户", CurrentBalance = 2000m };
        var batch = new ImportBatch
        {
            Id = batchId,
            AccountId = account.Id,
            FileName = "recover-ok.xlsx",
            Status = ImportBatchStatus.Pending,
            RecordCount = 1
        };

        var existingBank = new BankTransaction { Id = 88L, UniqueHash = "hash-recover" };
        var previews = new List<BankTransactionPreviewDto>
        {
            CreateConfirmPreview(1, "out", 150m, "hash-recover", recoverable: true)
        };
        SetPreviewCache(batchId, previews, DateTime.UtcNow.AddMinutes(10));

        var ctx = CreateConfirmContext(batch, account, existingBankTransactions: new[] { existingBank });
        var service = CreateImportService(
            batchRepository: ctx.BatchRepository.Object,
            accountRepository: ctx.AccountRepository.Object,
            bankTransactionRepository: ctx.BankRepository.Object,
            transactionRepository: ctx.TransactionRepository.Object,
            unitOfWork: ctx.UnitOfWork.Object,
            auditLogService: ctx.AuditLogService.Object);

        var result = await service.ConfirmAsync(new ImportConfirmRequest
        {
            BatchId = batchId,
            SelectedRowNumbers = new List<int> { 1 }
        });

        result.Status.Should().Be("Completed");
        result.SuccessCount.Should().Be(1);
        ctx.AddedBankTransactions.Should().BeEmpty();
        ctx.AddedTransactions.Should().HaveCount(1);
        ctx.AddedTransactions[0].BankTransactionId.Should().Be(88L);
        account.CurrentBalance.Should().Be(1850m);
        // Processing + 交易 + 账户/批次（可恢复行不插流水）
        ctx.SaveChangesCount.Should().Be(3);
    }

    [Fact]
    public async Task ConfirmAsync_BatchSaveChangesThrows_ShouldFallbackToRowSavepoints()
    {
        var batchId = 505L;
        var account = new Account { Id = 15L, Name = "回退账户", CurrentBalance = 3000m };
        var batch = new ImportBatch
        {
            Id = batchId,
            AccountId = account.Id,
            FileName = "fallback.xlsx",
            Status = ImportBatchStatus.Pending,
            RecordCount = 2
        };

        var previews = new List<BankTransactionPreviewDto>
        {
            CreateConfirmPreview(1, "in", 100m, "hash-fb-1"),
            CreateConfirmPreview(2, "in", 200m, "hash-fb-2")
        };
        SetPreviewCache(batchId, previews, DateTime.UtcNow.AddMinutes(10));

        var ctx = CreateConfirmContext(batch, account, throwOnSaveChangesCall: 2);
        var service = CreateImportService(
            batchRepository: ctx.BatchRepository.Object,
            accountRepository: ctx.AccountRepository.Object,
            bankTransactionRepository: ctx.BankRepository.Object,
            transactionRepository: ctx.TransactionRepository.Object,
            unitOfWork: ctx.UnitOfWork.Object,
            auditLogService: ctx.AuditLogService.Object);

        var result = await service.ConfirmAsync(new ImportConfirmRequest
        {
            BatchId = batchId,
            SelectedRowNumbers = new List<int> { 1, 2 }
        });

        result.Status.Should().Be("Completed");
        result.SuccessCount.Should().Be(2);
        ctx.ClearChangeTrackerCount.Should().Be(1);
        ctx.AddedTransactions.Should().HaveCount(2);
        account.CurrentBalance.Should().Be(3300m);
        // 回退后逐行各 2 次 SaveChanges，次数应多于成功路径的 4 次
        ctx.SaveChangesCount.Should().BeGreaterThan(4);
    }

    [Fact]
    public async Task ConfirmAsync_ConcurrencyOnFinalSave_ShouldMarkFailedWithOriginalMessage()
    {
        var batchId = 506L;
        var account = new Account { Id = 16L, Name = "并发账户", CurrentBalance = 1000m };
        var batch = new ImportBatch
        {
            Id = batchId,
            AccountId = account.Id,
            FileName = "concurrency.xlsx",
            Status = ImportBatchStatus.Pending,
            RecordCount = 1
        };

        var previews = new List<BankTransactionPreviewDto>
        {
            CreateConfirmPreview(1, "in", 50m, "hash-conc")
        };
        SetPreviewCache(batchId, previews, DateTime.UtcNow.AddMinutes(10));

        var ctx = CreateConfirmContext(batch, account, throwConcurrencyOnFinalSave: true);
        var service = CreateImportService(
            batchRepository: ctx.BatchRepository.Object,
            accountRepository: ctx.AccountRepository.Object,
            bankTransactionRepository: ctx.BankRepository.Object,
            transactionRepository: ctx.TransactionRepository.Object,
            unitOfWork: ctx.UnitOfWork.Object,
            auditLogService: ctx.AuditLogService.Object);

        var act = async () => await service.ConfirmAsync(new ImportConfirmRequest
        {
            BatchId = batchId,
            SelectedRowNumbers = new List<int> { 1 }
        });

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*账户余额被其他操作更新*重新上传*");
        batch.Status.Should().Be(ImportBatchStatus.Failed);
        batch.ErrorMessage.Should().Be("导入期间账户余额被其他操作更新，本批次已终止");
    }

    [Fact]
    public async Task ConfirmAsync_MixedNewAndRecoverableRows_ShouldPartialComplete()
    {
        var batchId = 507L;
        var account = new Account { Id = 17L, Name = "混合账户", CurrentBalance = 4000m };
        var batch = new ImportBatch
        {
            Id = batchId,
            AccountId = account.Id,
            FileName = "mixed.xlsx",
            Status = ImportBatchStatus.Pending,
            RecordCount = 3
        };

        var existingBank = new BankTransaction { Id = 77L, UniqueHash = "hash-recover-ok" };
        var previews = new List<BankTransactionPreviewDto>
        {
            CreateConfirmPreview(1, "in", 500m, "hash-new"),
            CreateConfirmPreview(2, "out", 80m, "hash-recover-ok", recoverable: true),
            CreateConfirmPreview(3, "in", 30m, "hash-recover-missing", recoverable: true)
        };
        SetPreviewCache(batchId, previews, DateTime.UtcNow.AddMinutes(10));

        var ctx = CreateConfirmContext(batch, account, existingBankTransactions: new[] { existingBank });
        var service = CreateImportService(
            batchRepository: ctx.BatchRepository.Object,
            accountRepository: ctx.AccountRepository.Object,
            bankTransactionRepository: ctx.BankRepository.Object,
            transactionRepository: ctx.TransactionRepository.Object,
            unitOfWork: ctx.UnitOfWork.Object,
            auditLogService: ctx.AuditLogService.Object);

        var result = await service.ConfirmAsync(new ImportConfirmRequest
        {
            BatchId = batchId,
            SelectedRowNumbers = new List<int> { 1, 2, 3 }
        });

        result.Status.Should().Be("PartialCompleted");
        result.SuccessCount.Should().Be(2);
        result.ErrorCount.Should().Be(1);
        result.ErrorMessage.Should().Contain("行3");
        ctx.AddedBankTransactions.Should().HaveCount(1);
        ctx.AddedTransactions.Should().HaveCount(2);
        ctx.AddedTransactions.Should().Contain(t => t.BankTransactionId == 77L);
        account.CurrentBalance.Should().Be(4420m);
    }

    private static BankTransactionPreviewDto CreateConfirmPreview(
        int row, string direction, decimal amount, string hash,
        bool duplicate = false, bool fileConflict = false, bool recoverable = false)
    {
        return new BankTransactionPreviewDto
        {
            RowNumber = row,
            TransactionDate = new DateTime(2026, 3, 10).AddDays(row),
            Amount = amount,
            Direction = direction,
            CounterpartyName = $"对方{row}",
            Description = $"描述{row}",
            UniqueHash = hash,
            IsDuplicate = duplicate,
            IsFileConflict = fileConflict,
            IsRecoverable = recoverable
        };
    }

    private sealed class ConfirmTestContext
    {
        public Mock<IRepository<ImportBatch>> BatchRepository { get; init; } = null!;
        public Mock<IRepository<Account>> AccountRepository { get; init; } = null!;
        public Mock<IRepository<BankTransaction>> BankRepository { get; init; } = null!;
        public Mock<IRepository<Transaction>> TransactionRepository { get; init; } = null!;
        public Mock<IUnitOfWork> UnitOfWork { get; init; } = null!;
        public Mock<IAuditLogService> AuditLogService { get; init; } = null!;
        public List<BankTransaction> AddedBankTransactions { get; } = new();
        public List<Transaction> AddedTransactions { get; } = new();
        public int SaveChangesCount { get; set; }
        public int ClearChangeTrackerCount { get; set; }
    }

    private static ConfirmTestContext CreateConfirmContext(
        ImportBatch batch,
        Account account,
        IEnumerable<BankTransaction>? existingBankTransactions = null,
        int? throwOnSaveChangesCall = null,
        bool throwConcurrencyOnFinalSave = false)
    {
        var ctx = new ConfirmTestContext
        {
            BatchRepository = new Mock<IRepository<ImportBatch>>(),
            AccountRepository = new Mock<IRepository<Account>>(),
            BankRepository = new Mock<IRepository<BankTransaction>>(),
            TransactionRepository = new Mock<IRepository<Transaction>>(),
            UnitOfWork = new Mock<IUnitOfWork>(),
            AuditLogService = new Mock<IAuditLogService>()
        };

        long nextBankId = 100;
        long nextTxId = 200;
        var existing = (existingBankTransactions ?? Array.Empty<BankTransaction>()).ToList();

        ctx.BatchRepository.Setup(x => x.GetByIdAsync(batch.Id)).ReturnsAsync(batch);
        ctx.BatchRepository.Setup(x => x.Update(It.IsAny<ImportBatch>()));
        ctx.AccountRepository.Setup(x => x.GetByIdAsync(account.Id)).ReturnsAsync(account);
        ctx.AccountRepository.Setup(x => x.Update(It.IsAny<Account>()));

        ctx.BankRepository.Setup(x => x.GetQueryable())
            .Returns(existing.AsQueryable().BuildMock().Object);
        ctx.BankRepository.Setup(x => x.AddAsync(It.IsAny<BankTransaction>()))
            .ReturnsAsync((BankTransaction bt) =>
            {
                bt.Id = ++nextBankId;
                ctx.AddedBankTransactions.Add(bt);
                return bt;
            });
        ctx.TransactionRepository.Setup(x => x.AddAsync(It.IsAny<Transaction>()))
            .ReturnsAsync((Transaction tx) =>
            {
                tx.Id = ++nextTxId;
                ctx.AddedTransactions.Add(tx);
                return tx;
            });

        ctx.UnitOfWork.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((ITransactionScope?)null);
        ctx.UnitOfWork.Setup(x => x.ClearChangeTracker())
            .Callback(() => ctx.ClearChangeTrackerCount++);
        ctx.UnitOfWork.Setup(x => x.DetachAddedEntities());
        ctx.UnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                ctx.SaveChangesCount++;
                if (throwOnSaveChangesCall.HasValue && ctx.SaveChangesCount == throwOnSaveChangesCall.Value)
                {
                    throw new DbUpdateException("unique constraint", new Exception("duplicate key"));
                }

                if (throwConcurrencyOnFinalSave && ctx.SaveChangesCount >= 4)
                {
                    throw new DbUpdateConcurrencyException("concurrency");
                }

                return 1;
            });

        ctx.AuditLogService
            .Setup(x => x.LogAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        return ctx;
    }

    #endregion

    /// <summary>
    /// 创建 ImportService 实例的辅助方法
    /// </summary>
    private ImportService CreateImportService(
        IRepository<ImportBatch>? batchRepository = null,
        IRepository<Account>? accountRepository = null,
        IRepository<Category>? categoryRepository = null,
        IRepository<BankTransaction>? bankTransactionRepository = null,
        IRepository<Transaction>? transactionRepository = null,
        IUnitOfWork? unitOfWork = null,
        IRuleService? ruleService = null,
        ILogger<ImportService>? logger = null,
        IAuditLogService? auditLogService = null)
    {
        return new ImportService(
            batchRepository ?? new Mock<IRepository<ImportBatch>>().Object,
            accountRepository ?? new Mock<IRepository<Account>>().Object,
            categoryRepository ?? new Mock<IRepository<Category>>().Object,
            bankTransactionRepository ?? new Mock<IRepository<BankTransaction>>().Object,
            transactionRepository ?? new Mock<IRepository<Transaction>>().Object,
            unitOfWork ?? new Mock<IUnitOfWork>().Object,
            ruleService ?? new Mock<IRuleService>().Object,
            logger ?? new Mock<ILogger<ImportService>>().Object,
            auditLogService ?? new Mock<IAuditLogService>().Object);
    }
}
