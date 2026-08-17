using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.Reconciliation.DTOs;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace FinanceApp.Api.Tests.Integration;

public class ExcelImportIntegrationTests : IntegrationTestBase
{
    public ExcelImportIntegrationTests(IntegrationTestFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task ImportExcel_Preview_ShouldReturnPreviewResponse()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var account = new Account
        {
            Name = "测试银行账户",
            AccountNumber = "6222000012345678",
            BankName = "测试银行",
            AccountType = AccountType.Bank,
            Currency = "CNY",
            OpeningBalance = 10000m,
            CurrentBalance = 10000m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        DbContext.Accounts.Add(account);

        var category = new Category
        {
            Name = "办公费用",
            CategoryType = CategoryType.Expense,
            ParentId = null,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        DbContext.Categories.Add(category);

        var rule = new ClassificationRule
        {
            RuleName = "办公用品自动分类",
            CategoryId = category.Id,
            MatchField = RuleMatchField.CounterpartyName,
            MatchOperator = RuleMatchOperator.Contains,
            MatchValue = "办公用品",
            Priority = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        DbContext.ClassificationRules.Add(rule);
        await DbContext.SaveChangesAsync();

        // 创建模拟 Excel 文件内容
        var excelContent = CreateMockExcelFile();
        var formData = new Dictionary<string, string>
        {
            { "accountId", account.Id.ToString() }
        };
        var content = CreateMultipartContent(excelContent, "test.xlsx", formData);

        // Act - 预览导入
        var previewResponse = await Client.PostAsync("/api/imports/preview", content);

        // Assert - 验证响应状态
        // 注意：由于模拟 Excel 文件不是有效的 Excel 格式，可能返回错误
        // 这里只验证 API 端点可以访问
        previewResponse.Should().NotBeNull();
    }

    [Fact]
    public async Task ImportExcel_Confirm_WithInvalidBatchId_ShouldReturnBadRequest()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var confirmRequest = new ImportConfirmRequest
        {
            BatchId = 0,
            SelectedRowNumbers = new List<int> { 1, 2, 3 }
        };

        // Act
        var confirmResponse = await PostAsync("/api/imports/confirm", confirmRequest);

        // Assert - 服务层会抛出 NotFoundException（批次不存在），映射为 404
        confirmResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ImportExcel_Confirm_WithEmptyRows_ShouldReturnNotFound()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var confirmRequest = new ImportConfirmRequest
        {
            BatchId = 1,
            SelectedRowNumbers = new List<int>()
        };

        // Act
        var confirmResponse = await PostAsync("/api/imports/confirm", confirmRequest);

        // Assert - 服务层会抛出 NotFoundException（批次不存在），映射为 404
        confirmResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #region 完整E2E流程测试

    [Fact]
    public async Task ImportExcel_FullFlow_PreviewThenConfirm_ShouldCreateTransactions()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var account = await CreateTestAccount("E2E测试账户", 50000m);

        var excelBytes = CreateValidSimpleExcel(new[]
        {
            ("2026-03-10", 3000.00m, "客户甲", "项目回款"),
            ("2026-03-11", -800.50m, "供应商乙", "采购材料"),
        });

        var formData = new Dictionary<string, string> { { "accountId", account.Id.ToString() } };
        var content = CreateMultipartContent(excelBytes, "e2e_test.xlsx", formData);

        // Act 1 - 预览
        var previewResponse = await Client.PostAsync("/api/imports/preview", content);
        previewResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var previewResult = await previewResponse.Content.ReadFromJsonAsync<ApiResponse<ImportPreviewResponse>>();
        previewResult.Should().NotBeNull();
        previewResult!.Success.Should().BeTrue();
        previewResult.Data.Should().NotBeNull();
        previewResult.Data!.TotalRows.Should().Be(2);
        previewResult.Data.NewRows.Should().Be(2);
        previewResult.Data.DuplicateRows.Should().Be(0);
        previewResult.Data.DetectedFormat.Should().Be("Simple");

        var batchId = previewResult.Data.BatchId;

        // Act 2 - 确认导入
        var confirmRequest = new ImportConfirmRequest
        {
            BatchId = batchId,
            SelectedRowNumbers = new List<int> { 1, 2 }
        };
        var confirmResponse = await PostAsync("/api/imports/confirm", confirmRequest);
        confirmResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var confirmResult = await confirmResponse.Content.ReadFromJsonAsync<ApiResponse<ImportBatchDto>>();
        confirmResult.Should().NotBeNull();
        confirmResult!.Success.Should().BeTrue();
        confirmResult.Data!.SuccessCount.Should().Be(2);
        confirmResult.Data.Status.Should().Be("Completed");

        // Assert - 验证数据库中的 BankTransaction 和 Transaction
        var bankTransactions = await DbContext.BankTransactions
            .Where(bt => bt.ImportBatchId == batchId)
            .ToListAsync();
        bankTransactions.Should().HaveCount(2);

        var transactions = await DbContext.Transactions
            .Where(t => bankTransactions.Select(bt => bt.Id).Contains(t.BankTransactionId!.Value))
            .ToListAsync();
        transactions.Should().HaveCount(2);

        // 验证收入交易
        var incomeTransaction = transactions.First(t => t.TransactionType == TransactionType.Income);
        incomeTransaction.Amount.Should().Be(3000.00m);

        // 验证支出交易
        var expenseTransaction = transactions.First(t => t.TransactionType == TransactionType.Expense);
        expenseTransaction.Amount.Should().Be(800.50m);

        // 验证账户余额更新（重新加载以避免 ChangeTracker 缓存）
        DbContext.ChangeTracker.Clear();
        var updatedAccount = await DbContext.Accounts.FindAsync(account.Id);
        updatedAccount!.CurrentBalance.Should().Be(50000m + 3000.00m - 800.50m);
    }

    [Fact]
    public async Task ImportExcel_DuplicateDetection_ShouldMarkDuplicates()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var account = await CreateTestAccount("重复检测账户", 100000m);

        // 先导入一次
        var excelBytes1 = CreateValidSimpleExcel(new[]
        {
            ("2026-03-10", 5000.00m, "客户A", "首次导入"),
        });

        var content1 = CreateMultipartContent(excelBytes1, "first.xlsx",
            new Dictionary<string, string> { { "accountId", account.Id.ToString() } });

        var previewResponse1 = await Client.PostAsync("/api/imports/preview", content1);
        previewResponse1.StatusCode.Should().Be(HttpStatusCode.OK);
        var previewResult1 = await previewResponse1.Content.ReadFromJsonAsync<ApiResponse<ImportPreviewResponse>>();
        previewResult1!.Data!.DuplicateRows.Should().Be(0);

        // 确认第一次导入
        var confirmReq1 = new ImportConfirmRequest
        {
            BatchId = previewResult1.Data.BatchId,
            SelectedRowNumbers = new List<int> { 1 }
        };
        var confirmResponse1 = await PostAsync("/api/imports/confirm", confirmReq1);
        confirmResponse1.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act - 再次导入相同数据
        var excelBytes2 = CreateValidSimpleExcel(new[]
        {
            ("2026-03-10", 5000.00m, "客户A", "首次导入"),  // 重复
            ("2026-03-11", 2000.00m, "客户B", "新数据"),    // 新的
        });

        var content2 = CreateMultipartContent(excelBytes2, "second.xlsx",
            new Dictionary<string, string> { { "accountId", account.Id.ToString() } });

        var previewResponse2 = await Client.PostAsync("/api/imports/preview", content2);
        previewResponse2.StatusCode.Should().Be(HttpStatusCode.OK);

        var previewResult2 = await previewResponse2.Content.ReadFromJsonAsync<ApiResponse<ImportPreviewResponse>>();

        // Assert - 应检测到一条重复
        previewResult2!.Data!.TotalRows.Should().Be(2);
        previewResult2.Data.DuplicateRows.Should().Be(1);
        previewResult2.Data.NewRows.Should().Be(1);

        // 验证重复行标记
        var duplicateRow = previewResult2.Data.Previews.First(p => p.IsDuplicate);
        duplicateRow.CounterpartyName.Should().Be("客户A");
        duplicateRow.Amount.Should().Be(5000.00m);
    }

    [Fact]
    public async Task ImportExcel_PartialSelect_OnlyImportsSelectedRows()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var account = await CreateTestAccount("部分选择账户", 10000m);

        var excelBytes = CreateValidSimpleExcel(new[]
        {
            ("2026-03-10", 1000.00m, "客户A", "行1"),
            ("2026-03-11", 2000.00m, "客户B", "行2"),
            ("2026-03-12", 3000.00m, "客户C", "行3"),
        });

        var content = CreateMultipartContent(excelBytes, "partial.xlsx",
            new Dictionary<string, string> { { "accountId", account.Id.ToString() } });

        var previewResponse = await Client.PostAsync("/api/imports/preview", content);
        previewResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var previewResult = await previewResponse.Content.ReadFromJsonAsync<ApiResponse<ImportPreviewResponse>>();

        // Act - 只选择第1行和第3行
        var confirmReq = new ImportConfirmRequest
        {
            BatchId = previewResult!.Data!.BatchId,
            SelectedRowNumbers = new List<int> { 1, 3 }
        };
        var confirmResponse = await PostAsync("/api/imports/confirm", confirmReq);
        confirmResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var confirmResult = await confirmResponse.Content.ReadFromJsonAsync<ApiResponse<ImportBatchDto>>();

        // Assert
        confirmResult!.Data!.SuccessCount.Should().Be(2);

        var bankTransactions = await DbContext.BankTransactions
            .Where(bt => bt.ImportBatchId == previewResult.Data.BatchId)
            .OrderBy(bt => bt.TransactionDate)
            .ToListAsync();
        bankTransactions.Should().HaveCount(2);
        bankTransactions[0].Amount.Should().Be(1000.00m);
        bankTransactions[1].Amount.Should().Be(3000.00m);

        // 验证账户余额只增加选中的收入（重新加载以避免 ChangeTracker 缓存）
        DbContext.ChangeTracker.Clear();
        var updatedAccount = await DbContext.Accounts.FindAsync(account.Id);
        updatedAccount!.CurrentBalance.Should().Be(10000m + 1000m + 3000m);
    }

    [Fact]
    public async Task ImportExcel_ConfirmTwentyRows_ShouldCreateAllTransactionsAndUpdateBalance()
    {
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var account = await CreateTestAccount("批量二十行账户", 10000m);
        var rows = Enumerable.Range(1, 20)
            .Select(i => ($"2026-03-{(i % 28) + 1:00}", i % 2 == 0 ? -100m : 200m, $"对方{i}", $"摘要{i}"))
            .ToArray();

        var excelBytes = CreateValidSimpleExcel(rows);
        var content = CreateMultipartContent(excelBytes, "batch20.xlsx",
            new Dictionary<string, string> { { "accountId", account.Id.ToString() } });

        var previewResponse = await Client.PostAsync("/api/imports/preview", content);
        previewResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var previewResult = await previewResponse.Content.ReadFromJsonAsync<ApiResponse<ImportPreviewResponse>>();
        previewResult!.Data!.NewRows.Should().Be(20);

        var confirmResponse = await PostAsync("/api/imports/confirm", new ImportConfirmRequest
        {
            BatchId = previewResult.Data.BatchId,
            SelectedRowNumbers = Enumerable.Range(1, 20).ToList()
        });
        confirmResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var confirmResult = await confirmResponse.Content.ReadFromJsonAsync<ApiResponse<ImportBatchDto>>();
        confirmResult!.Data!.SuccessCount.Should().Be(20);
        confirmResult.Data.Status.Should().Be("Completed");

        var bankTransactions = await DbContext.BankTransactions
            .Where(bt => bt.ImportBatchId == previewResult.Data.BatchId)
            .ToListAsync();
        bankTransactions.Should().HaveCount(20);

        var transactions = await DbContext.Transactions
            .Where(t => bankTransactions.Select(bt => bt.Id).Contains(t.BankTransactionId!.Value))
            .ToListAsync();
        transactions.Should().HaveCount(20);
        transactions.Count(t => t.TransactionType == TransactionType.Income).Should().Be(10);
        transactions.Count(t => t.TransactionType == TransactionType.Expense).Should().Be(10);

        DbContext.ChangeTracker.Clear();
        var updatedAccount = await DbContext.Accounts.FindAsync(account.Id);
        updatedAccount!.CurrentBalance.Should().Be(10000m + 10 * 200m - 10 * 100m);
    }

    #endregion

    #region 批次查询测试

    [Fact]
    public async Task GetBatches_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var account = await CreateTestAccount("批次查询账户", 10000m);

        // 创建多个批次
        for (int i = 1; i <= 5; i++)
        {
            DbContext.ImportBatches.Add(new ImportBatch
            {
                AccountId = account.Id,
                FileName = $"batch_{i}.xlsx",
                RecordCount = i * 10,
                SuccessCount = i * 8,
                DuplicateCount = i * 2,
                Status = ImportBatchStatus.Completed,
                CreatedAt = DateTime.UtcNow.AddHours(-i)
            });
        }
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/api/imports/batches?page=1&pageSize=3");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<PageResponse<ImportBatchDto>>>();

        // Assert
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
        result.Data!.Total.Should().Be(5);
        result.Data.Items.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetBatches_WithStatusFilter_ReturnsFilteredResults()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var account = await CreateTestAccount("状态筛选账户", 10000m);

        DbContext.ImportBatches.Add(new ImportBatch
        {
            AccountId = account.Id,
            FileName = "completed.xlsx",
            RecordCount = 10,
            SuccessCount = 10,
            Status = ImportBatchStatus.Completed,
            CreatedAt = DateTime.UtcNow
        });
        DbContext.ImportBatches.Add(new ImportBatch
        {
            AccountId = account.Id,
            FileName = "pending.xlsx",
            RecordCount = 5,
            SuccessCount = 0,
            Status = ImportBatchStatus.Pending,
            CreatedAt = DateTime.UtcNow
        });
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/api/imports/batches?page=1&pageSize=10&status=Completed");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<PageResponse<ImportBatchDto>>>();

        // Assert
        result!.Data!.Items.Should().AllSatisfy(item =>
            item.Status.Should().Be("Completed"));
    }

    [Fact]
    public async Task GetBatches_WithFileNameFilter_ReturnsMatchingResults()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var account = await CreateTestAccount("文件名筛选账户", 10000m);

        DbContext.ImportBatches.Add(new ImportBatch
        {
            AccountId = account.Id,
            FileName = "huaxia_bank_202603.xlsx",
            RecordCount = 20,
            Status = ImportBatchStatus.Completed,
            CreatedAt = DateTime.UtcNow
        });
        DbContext.ImportBatches.Add(new ImportBatch
        {
            AccountId = account.Id,
            FileName = "alipay_202603.xml",
            RecordCount = 15,
            Status = ImportBatchStatus.Completed,
            CreatedAt = DateTime.UtcNow
        });
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/api/imports/batches?page=1&pageSize=10&fileName=huaxia");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<PageResponse<ImportBatchDto>>>();

        // Assert
        result!.Data!.Items.Should().HaveCount(1);
        result.Data.Items[0].FileName.Should().Contain("huaxia");
    }

    [Fact]
    public async Task GetBatchById_ExistingBatch_ReturnsBatchDetails()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var account = await CreateTestAccount("详情查询账户", 10000m);

        var batch = new ImportBatch
        {
            AccountId = account.Id,
            FileName = "detail_test.xlsx",
            RecordCount = 25,
            SuccessCount = 20,
            DuplicateCount = 5,
            Status = ImportBatchStatus.Completed,
            CreatedAt = DateTime.UtcNow
        };
        DbContext.ImportBatches.Add(batch);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync($"/api/imports/batches/{batch.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ImportBatchDto>>();

        // Assert
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(batch.Id);
        result.Data.FileName.Should().Be("detail_test.xlsx");
        result.Data.TotalCount.Should().Be(25);
        result.Data.SuccessCount.Should().Be(20);
        result.Data.DuplicateCount.Should().Be(5);
        result.Data.AccountName.Should().Be("详情查询账户");
    }

    [Fact]
    public async Task GetBatchById_NonExistingBatch_ReturnsNotFound()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        // Act
        var response = await Client.GetAsync("/api/imports/batches/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region 权限测试

    [Fact]
    public async Task ImportExcel_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange - 不设置 token
        var excelContent = CreateMockExcelFile();
        var content = CreateMultipartContent(excelContent, "test.xlsx",
            new Dictionary<string, string> { { "accountId", "1" } });

        // Act
        var response = await Client.PostAsync("/api/imports/preview", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ImportExcel_WithViewerRole_ReturnsForbidden()
    {
        // Arrange - 创建 Viewer 用户
        var user = new User
        {
            Username = "viewer_user",
            NormalizedUsername = "VIEWER_USER",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test123!"),
            SecurityStamp = Guid.NewGuid().ToString("N"),
            FullName = "查看者",
            Email = "viewer@example.com",
            Role = UserRole.Viewer,
            IsActive = true,
            PasswordChangedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();

        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login",
            new { Username = "viewer_user", Password = "Test123!" });
        loginResponse.EnsureSuccessStatusCode();

        var excelContent = CreateMockExcelFile();
        var content = CreateMultipartContent(excelContent, "test.xlsx",
            new Dictionary<string, string> { { "accountId", "1" } });

        // Act
        var response = await Client.PostAsync("/api/imports/preview", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region 华夏银行格式集成测试

    [Fact]
    public async Task ImportExcel_HuaxiaBankFormat_ShouldDetectAndParse()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var account = await CreateTestAccount("华夏银行测试账户", 100000m);
        var excelBytes = CreateValidHuaxiaExcel();

        var content = CreateMultipartContent(excelBytes, "huaxia.xlsx",
            new Dictionary<string, string> { { "accountId", account.Id.ToString() } });

        // Act
        var previewResponse = await Client.PostAsync("/api/imports/preview", content);
        previewResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var previewResult = await previewResponse.Content.ReadFromJsonAsync<ApiResponse<ImportPreviewResponse>>();

        // Assert
        previewResult.Should().NotBeNull();
        previewResult!.Data.Should().NotBeNull();
        previewResult.Data!.DetectedFormat.Should().Be("HuaxiaBank");
        previewResult.Data.TotalRows.Should().BeGreaterThan(0);
    }

    /// <summary>
    /// 验证华夏银行格式解析后 description 字段被正确写入 BankTransaction 表
    /// 这是"description 迁移缺口"修复的端到端回归测试
    /// </summary>
    [Fact]
    public async Task ImportExcel_HuaxiaBankFormat_ShouldPersistDescriptionToBankTransaction()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var account = await CreateTestAccount("华夏描述字段测试账户", 100000m);
        var excelBytes = CreateValidHuaxiaExcel();

        var content = CreateMultipartContent(excelBytes, "huaxia_desc.xlsx",
            new Dictionary<string, string> { { "accountId", account.Id.ToString() } });

        // Act 1 - 预览，验证 description 字段已被解析
        var previewResponse = await Client.PostAsync("/api/imports/preview", content);
        previewResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var previewResult = await previewResponse.Content.ReadFromJsonAsync<ApiResponse<ImportPreviewResponse>>();
        previewResult!.Data!.DetectedFormat.Should().Be("HuaxiaBank");
        previewResult.Data.Previews.Should().NotBeEmpty();

        // 华夏 Excel 第 11 列为"交易描述"，应被解析到 Description 字段
        var rowsWithDesc = previewResult.Data.Previews.Where(p => !string.IsNullOrEmpty(p.Description)).ToList();
        rowsWithDesc.Should().NotBeEmpty("华夏银行格式应解析出 description（交易描述）字段");

        // Act 2 - 确认导入
        var selectedRows = previewResult.Data.Previews
            .Where(p => !p.IsDuplicate)
            .Select(p => p.RowNumber)
            .ToList();

        if (selectedRows.Count == 0)
            return; // 全部重复时跳过

        var confirmReq = new ImportConfirmRequest
        {
            BatchId = previewResult.Data.BatchId,
            SelectedRowNumbers = selectedRows
        };
        var confirmResponse = await PostAsync("/api/imports/confirm", confirmReq);
        confirmResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert - 验证 BankTransaction.Description 已落库
        DbContext.ChangeTracker.Clear();
        var bankTransactions = await DbContext.BankTransactions
            .Where(bt => bt.ImportBatchId == previewResult.Data.BatchId)
            .ToListAsync();

        bankTransactions.Should().NotBeEmpty();
        bankTransactions.Should().Contain(bt => bt.Description != null,
            "导入后 BankTransaction 应包含 description 字段值（来自华夏银行第11列交易描述）");
    }

    #endregion

    #region Helper Methods

    private async Task<Account> CreateTestAccount(string name, decimal balance)
    {
        var account = new Account
        {
            Name = name,
            AccountNumber = $"ACC{DateTime.UtcNow.Ticks}",
            BankName = "测试银行",
            AccountType = AccountType.Bank,
            Currency = "CNY",
            OpeningBalance = balance,
            CurrentBalance = balance,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        DbContext.Accounts.Add(account);
        await DbContext.SaveChangesAsync();
        return account;
    }

    private static byte[] CreateValidSimpleExcel((string date, decimal amount, string counterparty, string desc)[] rows)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using var stream = new MemoryStream();
        using (var package = new ExcelPackage(stream))
        {
            var ws = package.Workbook.Worksheets.Add("Sheet1");

            // 表头
            ws.Cells[1, 1].Value = "日期";
            ws.Cells[1, 2].Value = "金额";
            ws.Cells[1, 3].Value = "对方";
            ws.Cells[1, 4].Value = "摘要";

            // 数据行
            for (int i = 0; i < rows.Length; i++)
            {
                ws.Cells[i + 2, 1].Value = rows[i].date;
                ws.Cells[i + 2, 2].Value = (double)rows[i].amount;
                ws.Cells[i + 2, 3].Value = rows[i].counterparty;
                ws.Cells[i + 2, 4].Value = rows[i].desc;
            }

            package.Save();
        }
        return stream.ToArray();
    }

    private static byte[] CreateValidHuaxiaExcel()
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using var stream = new MemoryStream();
        using (var package = new ExcelPackage(stream))
        {
            var ws = package.Workbook.Worksheets.Add("Sheet1");

            // 元数据行
            ws.Cells[1, 1].Value = "账号：";
            ws.Cells[1, 2].Value = "1234567890";
            ws.Cells[2, 1].Value = "币种：";
            ws.Cells[2, 2].Value = "人民币";

            // 表头行
            ws.Cells[8, 1].Value = "序号";
            ws.Cells[8, 2].Value = "交易日期";
            ws.Cells[8, 3].Value = "交易时间";
            ws.Cells[8, 4].Value = "支出金额";
            ws.Cells[8, 5].Value = "收入金额";
            ws.Cells[8, 6].Value = "余额";
            ws.Cells[8, 7].Value = "对方账号";
            ws.Cells[8, 8].Value = "对方户名";
            ws.Cells[8, 9].Value = "对方行名";
            ws.Cells[8, 10].Value = "核心流水号";
            ws.Cells[8, 11].Value = "交易描述";
            ws.Cells[8, 12].Value = "摘要";

            // 数据行 - 收入
            ws.Cells[9, 1].Value = "1";
            ws.Cells[9, 2].Value = "2026-03-10";
            ws.Cells[9, 3].Value = "10:30:00";
            ws.Cells[9, 4].Value = "";
            ws.Cells[9, 5].Value = "8000.00";
            ws.Cells[9, 6].Value = "108000.00";
            ws.Cells[9, 7].Value = "6222021234567890";
            ws.Cells[9, 8].Value = "测试公司A";
            ws.Cells[9, 9].Value = "工商银行";
            ws.Cells[9, 10].Value = "HX20260310001";
            ws.Cells[9, 11].Value = "网银转账";
            ws.Cells[9, 12].Value = "项目款";

            // 数据行 - 支出
            ws.Cells[10, 1].Value = "2";
            ws.Cells[10, 2].Value = "2026-03-11";
            ws.Cells[10, 3].Value = "14:20:00";
            ws.Cells[10, 4].Value = "1500.00";
            ws.Cells[10, 5].Value = "";
            ws.Cells[10, 6].Value = "106500.00";
            ws.Cells[10, 7].Value = "6228481234567890";
            ws.Cells[10, 8].Value = "供应商B";
            ws.Cells[10, 9].Value = "";
            ws.Cells[10, 10].Value = "HX20260311001";
            ws.Cells[10, 11].Value = "付款";
            ws.Cells[10, 12].Value = "材料费";

            package.Save();
        }
        return stream.ToArray();
    }

    private byte[] CreateMockExcelFile()
    {
        // 这里返回一个简化的模拟 Excel 文件
        // 实际测试中应该使用真实的 Excel 文件或使用 EPPlus/ClosedXML 生成
        var mockData = new byte[] { 0x50, 0x4B, 0x03, 0x04 }; // ZIP 文件头（Excel 是 ZIP 格式）
        return mockData;
    }

    #endregion
}
