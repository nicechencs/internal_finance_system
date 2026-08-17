using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using FinanceApp.Api.Controllers.TransactionProcessing;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.TransactionProcessing.DTOs;
using FinanceApp.Application.Modules.TransactionProcessing.Interfaces;

namespace FinanceApp.Api.Tests.Controllers;

public class TransactionsControllerTests
{
    private readonly Mock<ITransactionService> _transactionServiceMock;
    private readonly TransactionsController _controller;

    public TransactionsControllerTests()
    {
        _transactionServiceMock = new Mock<ITransactionService>();
        _controller = new TransactionsController(_transactionServiceMock.Object, new Mock<ILogger<TransactionsController>>().Object);

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "1") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    [Fact]
    public async Task GetPaged_ValidRequest_ReturnsOkWithPagedData()
    {
        // Arrange
        var request = new PageRequest { Page = 1, PageSize = 10 };
        var expectedResponse = new PageResponse<TransactionDto>
        {
            Items = new List<TransactionDto>
            {
                new TransactionDto { Id = 1, Amount = 1000.00m, Description = "测试交易1" },
                new TransactionDto { Id = 2, Amount = 2000.00m, Description = "测试交易2" }
            },
            Total = 2,
            Page = 1,
            PageSize = 10
        };

        _transactionServiceMock
            .Setup(x => x.GetPagedAsync(It.IsAny<PageRequest>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetPaged(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<PageResponse<TransactionDto>>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Items.Should().HaveCount(2);
        apiResponse.Data.Total.Should().Be(2);

        _transactionServiceMock.Verify(x => x.GetPagedAsync(It.IsAny<PageRequest>()), Times.Once);
    }

    [Fact]
    public async Task GetById_ExistingId_ReturnsOkWithTransaction()
    {
        // Arrange
        var transactionId = 1L;
        var expectedTransaction = new TransactionDto
        {
            Id = transactionId,
            Amount = 1000.00m,
            Description = "测试交易",
            TransactionDate = DateTime.Now,
            AccountId = 1
        };

        _transactionServiceMock
            .Setup(x => x.GetByIdAsync(transactionId))
            .ReturnsAsync(expectedTransaction);

        // Act
        var result = await _controller.GetById(transactionId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<TransactionDto>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Id.Should().Be(transactionId);

        _transactionServiceMock.Verify(x => x.GetByIdAsync(transactionId), Times.Once);
    }

    [Fact]
    public async Task Create_ValidRequest_ReturnsOkWithCreatedTransaction()
    {
        // Arrange
        var request = new CreateTransactionRequest
        {
            AccountId = 1,
            Amount = 1000.00m,
            Description = "新交易",
            TransactionDate = DateTime.Now
        };

        var expectedTransaction = new TransactionDto
        {
            Id = 1,
            AccountId = request.AccountId,
            Amount = request.Amount,
            Description = request.Description,
            TransactionDate = request.TransactionDate
        };

        _transactionServiceMock
            .Setup(x => x.CreateAsync(It.IsAny<CreateTransactionRequest>()))
            .ReturnsAsync(expectedTransaction);

        // Act
        var result = await _controller.Create(request);

        // Assert
        var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(201);

        var apiResponse = objectResult.Value.Should().BeOfType<ApiResponse<TransactionDto>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Message.Should().Be("创建成功");

        _transactionServiceMock.Verify(x => x.CreateAsync(It.IsAny<CreateTransactionRequest>()), Times.Once);
    }

    [Fact]
    public async Task Update_ValidRequest_ReturnsOkWithUpdatedTransaction()
    {
        // Arrange
        var transactionId = 1L;
        var request = new UpdateTransactionRequest
        {
            TransactionDate = DateTime.Now,
            Description = "更新后的交易"
        };

        var expectedTransaction = new TransactionDto
        {
            Id = transactionId,
            TransactionDate = request.TransactionDate,
            Description = request.Description
        };

        _transactionServiceMock
            .Setup(x => x.UpdateAsync(transactionId, It.IsAny<UpdateTransactionRequest>()))
            .ReturnsAsync(expectedTransaction);

        // Act
        var result = await _controller.Update(transactionId, request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<TransactionDto>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Message.Should().Be("更新成功");

        _transactionServiceMock.Verify(x => x.UpdateAsync(transactionId, It.IsAny<UpdateTransactionRequest>()), Times.Once);
    }

    [Fact]
    public async Task Delete_ExistingId_ReturnsOkWithSuccessMessage()
    {
        // Arrange
        var transactionId = 1L;

        _transactionServiceMock
            .Setup(x => x.DeleteAsync(transactionId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Delete(transactionId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<object>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Message.Should().Be("删除成功");

        _transactionServiceMock.Verify(x => x.DeleteAsync(transactionId), Times.Once);
    }

    [Fact]
    public async Task GetByAccount_ValidAccountId_ReturnsOkWithTransactions()
    {
        // Arrange
        var accountId = 1L;
        var expectedTransactions = new List<TransactionDto>
        {
            new TransactionDto { Id = 1, AccountId = accountId, Amount = 1000.00m },
            new TransactionDto { Id = 2, AccountId = accountId, Amount = 2000.00m }
        };

        _transactionServiceMock
            .Setup(x => x.GetByAccountAsync(accountId))
            .ReturnsAsync(expectedTransactions);

        // Act
        var result = await _controller.GetByAccount(accountId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<List<TransactionDto>>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Should().HaveCount(2);
        apiResponse.Data.All(t => t.AccountId == accountId).Should().BeTrue();

        _transactionServiceMock.Verify(x => x.GetByAccountAsync(accountId), Times.Once);
    }

    [Fact]
    public async Task GetByProject_ValidProjectId_ReturnsOkWithTransactions()
    {
        // Arrange
        var projectId = 1L;
        var expectedTransactions = new List<TransactionDto>
        {
            new TransactionDto { Id = 1, ProjectId = projectId, Amount = 1000.00m },
            new TransactionDto { Id = 2, ProjectId = projectId, Amount = 2000.00m }
        };

        _transactionServiceMock
            .Setup(x => x.GetByProjectAsync(projectId))
            .ReturnsAsync(expectedTransactions);

        // Act
        var result = await _controller.GetByProject(projectId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<List<TransactionDto>>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Should().HaveCount(2);

        _transactionServiceMock.Verify(x => x.GetByProjectAsync(projectId), Times.Once);
    }

    [Fact]
    public async Task GetByCategory_ValidCategoryId_ReturnsOkWithTransactions()
    {
        // Arrange
        var categoryId = 1L;
        var expectedTransactions = new List<TransactionDto>
        {
            new TransactionDto { Id = 1, CategoryId = categoryId, Amount = 1000.00m },
            new TransactionDto { Id = 2, CategoryId = categoryId, Amount = 2000.00m }
        };

        _transactionServiceMock
            .Setup(x => x.GetByCategoryAsync(categoryId))
            .ReturnsAsync(expectedTransactions);

        // Act
        var result = await _controller.GetByCategory(categoryId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<List<TransactionDto>>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Should().HaveCount(2);

        _transactionServiceMock.Verify(x => x.GetByCategoryAsync(categoryId), Times.Once);
    }

    [Fact]
    public async Task GetByCustomer_ValidCustomerId_ReturnsOkWithTransactions()
    {
        // Arrange
        var customerId = 1L;
        var expectedTransactions = new List<TransactionDto>
        {
            new TransactionDto { Id = 1, CustomerId = customerId, Amount = 1000.00m },
            new TransactionDto { Id = 2, CustomerId = customerId, Amount = 2000.00m }
        };

        _transactionServiceMock
            .Setup(x => x.GetByCustomerAsync(customerId))
            .ReturnsAsync(expectedTransactions);

        // Act
        var result = await _controller.GetByCustomer(customerId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<List<TransactionDto>>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Should().HaveCount(2);

        _transactionServiceMock.Verify(x => x.GetByCustomerAsync(customerId), Times.Once);
    }

    [Fact]
    public async Task GetBySupplier_ValidSupplierId_ReturnsOkWithTransactions()
    {
        // Arrange
        var supplierId = 1L;
        var expectedTransactions = new List<TransactionDto>
        {
            new TransactionDto { Id = 1, SupplierId = supplierId, Amount = 1000.00m },
            new TransactionDto { Id = 2, SupplierId = supplierId, Amount = 2000.00m }
        };

        _transactionServiceMock
            .Setup(x => x.GetBySupplierAsync(supplierId))
            .ReturnsAsync(expectedTransactions);

        // Act
        var result = await _controller.GetBySupplier(supplierId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<List<TransactionDto>>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Should().HaveCount(2);

        _transactionServiceMock.Verify(x => x.GetBySupplierAsync(supplierId), Times.Once);
    }

    [Fact]
    public async Task GetByPerson_ValidPersonId_ReturnsOkWithTransactions()
    {
        // Arrange
        var personId = 1L;
        var expectedTransactions = new List<TransactionDto>
        {
            new TransactionDto { Id = 1, PersonId = personId, Amount = 1000.00m },
            new TransactionDto { Id = 2, PersonId = personId, Amount = 2000.00m }
        };

        _transactionServiceMock
            .Setup(x => x.GetByPersonAsync(personId))
            .ReturnsAsync(expectedTransactions);

        // Act
        var result = await _controller.GetByPerson(personId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<List<TransactionDto>>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Should().HaveCount(2);

        _transactionServiceMock.Verify(x => x.GetByPersonAsync(personId), Times.Once);
    }

    [Fact]
    public async Task GetAccountBalance_ValidAccountId_ReturnsOkWithBalance()
    {
        // Arrange
        var accountId = 1L;
        var expectedBalance = 10000.00m;

        _transactionServiceMock
            .Setup(x => x.GetAccountBalanceAsync(accountId))
            .ReturnsAsync(expectedBalance);

        // Act
        var result = await _controller.GetAccountBalance(accountId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<decimal>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().Be(expectedBalance);

        _transactionServiceMock.Verify(x => x.GetAccountBalanceAsync(accountId), Times.Once);
    }

    // ===== 转账端点测试 =====

    [Fact]
    public async Task CreateTransfer_ValidRequest_ReturnsOkWithTransferResult()
    {
        // Arrange
        var request = new CreateTransferRequest
        {
            FromAccountId = 1,
            ToAccountId = 2,
            Amount = 5000.00m,
            TransactionDate = DateTime.Now,
            Description = "账户间转账"
        };

        var expectedResult = new TransferResultDto
        {
            OutTransaction = new TransactionDto
            {
                Id = 1,
                AccountId = 1,
                Amount = 5000.00m,
                TransactionType = "Transfer",
                Description = "账户间转账"
            },
            InTransaction = new TransactionDto
            {
                Id = 2,
                AccountId = 2,
                Amount = 5000.00m,
                TransactionType = "Transfer",
                Description = "账户间转账"
            }
        };

        _transactionServiceMock
            .Setup(x => x.CreateTransferAsync(It.IsAny<CreateTransferRequest>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.CreateTransfer(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<TransferResultDto>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.OutTransaction.Should().NotBeNull();
        apiResponse.Data.InTransaction.Should().NotBeNull();
        apiResponse.Data.OutTransaction.AccountId.Should().Be(1);
        apiResponse.Data.InTransaction.AccountId.Should().Be(2);
        apiResponse.Message.Should().Be("转账成功");

        _transactionServiceMock.Verify(x => x.CreateTransferAsync(It.IsAny<CreateTransferRequest>()), Times.Once);
    }

    [Fact]
    public async Task CreateTransfer_ServiceThrowsValidationException_ShouldPropagate()
    {
        // Arrange
        var request = new CreateTransferRequest
        {
            FromAccountId = 1,
            ToAccountId = 1, // 同一账户，应该抛出验证异常
            Amount = 1000.00m,
            TransactionDate = DateTime.Now
        };

        _transactionServiceMock
            .Setup(x => x.CreateTransferAsync(It.IsAny<CreateTransferRequest>()))
            .ThrowsAsync(new ValidationException("转出和转入账户不能相同"));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _controller.CreateTransfer(request));

        _transactionServiceMock.Verify(x => x.CreateTransferAsync(It.IsAny<CreateTransferRequest>()), Times.Once);
    }

    [Fact]
    public async Task CreateTransfer_ServiceThrowsNotFoundException_ShouldPropagate()
    {
        // Arrange
        var request = new CreateTransferRequest
        {
            FromAccountId = 999,
            ToAccountId = 2,
            Amount = 1000.00m,
            TransactionDate = DateTime.Now
        };

        _transactionServiceMock
            .Setup(x => x.CreateTransferAsync(It.IsAny<CreateTransferRequest>()))
            .ThrowsAsync(new NotFoundException("转出账户不存在"));

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _controller.CreateTransfer(request));

        _transactionServiceMock.Verify(x => x.CreateTransferAsync(It.IsAny<CreateTransferRequest>()), Times.Once);
    }

    // ===== 统计端点测试 =====

    [Fact]
    public async Task GetAccountStatistics_ValidAccountId_ReturnsOkWithStatistics()
    {
        // Arrange
        var accountId = 1L;
        var expectedStatistics = new TransactionStatisticsDto
        {
            TotalIncome = 50000.00m,
            TotalExpense = 30000.00m,
            NetProfit = 20000.00m,
            TotalTransfer = 10000.00m,
            IncomeCount = 15,
            ExpenseCount = 20,
            TransferCount = 5,
            TotalCount = 40
        };

        _transactionServiceMock
            .Setup(x => x.GetAccountStatisticsAsync(accountId))
            .ReturnsAsync(expectedStatistics);

        // Act
        var result = await _controller.GetAccountStatistics(accountId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<TransactionStatisticsDto>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.TotalIncome.Should().Be(50000.00m);
        apiResponse.Data.TotalExpense.Should().Be(30000.00m);
        apiResponse.Data.NetProfit.Should().Be(20000.00m);
        apiResponse.Data.TotalTransfer.Should().Be(10000.00m);
        apiResponse.Data.IncomeCount.Should().Be(15);
        apiResponse.Data.ExpenseCount.Should().Be(20);
        apiResponse.Data.TransferCount.Should().Be(5);
        apiResponse.Data.TotalCount.Should().Be(40);

        _transactionServiceMock.Verify(x => x.GetAccountStatisticsAsync(accountId), Times.Once);
    }

    [Fact]
    public async Task GetAccountStatistics_ServiceThrowsNotFoundException_ShouldPropagate()
    {
        // Arrange
        var accountId = 999L;

        _transactionServiceMock
            .Setup(x => x.GetAccountStatisticsAsync(accountId))
            .ThrowsAsync(new NotFoundException("账户不存在"));

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _controller.GetAccountStatistics(accountId));

        _transactionServiceMock.Verify(x => x.GetAccountStatisticsAsync(accountId), Times.Once);
    }

    [Fact]
    public async Task GetAccountStatistics_EmptyAccount_ReturnsZeroStatistics()
    {
        // Arrange
        var accountId = 1L;
        var expectedStatistics = new TransactionStatisticsDto
        {
            TotalIncome = 0m,
            TotalExpense = 0m,
            NetProfit = 0m,
            TotalTransfer = 0m,
            IncomeCount = 0,
            ExpenseCount = 0,
            TransferCount = 0,
            TotalCount = 0
        };

        _transactionServiceMock
            .Setup(x => x.GetAccountStatisticsAsync(accountId))
            .ReturnsAsync(expectedStatistics);

        // Act
        var result = await _controller.GetAccountStatistics(accountId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<TransactionStatisticsDto>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.TotalCount.Should().Be(0);
        apiResponse.Data.TotalIncome.Should().Be(0m);
        apiResponse.Data.TotalExpense.Should().Be(0m);

        _transactionServiceMock.Verify(x => x.GetAccountStatisticsAsync(accountId), Times.Once);
    }

    [Fact]
    public async Task GetCustomerStatistics_ValidCustomerId_ReturnsOkWithStatistics()
    {
        // Arrange
        var customerId = 1L;
        var expectedStatistics = new TransactionStatisticsDto
        {
            TotalIncome = 80000.00m,
            TotalExpense = 20000.00m,
            NetProfit = 60000.00m,
            TotalTransfer = 0m,
            IncomeCount = 25,
            ExpenseCount = 8,
            TransferCount = 0,
            TotalCount = 33
        };

        _transactionServiceMock
            .Setup(x => x.GetCustomerStatisticsAsync(customerId))
            .ReturnsAsync(expectedStatistics);

        // Act
        var result = await _controller.GetCustomerStatistics(customerId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<TransactionStatisticsDto>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.TotalIncome.Should().Be(80000.00m);
        apiResponse.Data.TotalExpense.Should().Be(20000.00m);
        apiResponse.Data.NetProfit.Should().Be(60000.00m);
        apiResponse.Data.TotalTransfer.Should().Be(0m);
        apiResponse.Data.IncomeCount.Should().Be(25);
        apiResponse.Data.ExpenseCount.Should().Be(8);
        apiResponse.Data.TransferCount.Should().Be(0);
        apiResponse.Data.TotalCount.Should().Be(33);

        _transactionServiceMock.Verify(x => x.GetCustomerStatisticsAsync(customerId), Times.Once);
    }

    [Fact]
    public async Task GetCustomerStatistics_ServiceThrowsNotFoundException_ShouldPropagate()
    {
        // Arrange
        var customerId = 999L;

        _transactionServiceMock
            .Setup(x => x.GetCustomerStatisticsAsync(customerId))
            .ThrowsAsync(new NotFoundException("客户不存在"));

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _controller.GetCustomerStatistics(customerId));

        _transactionServiceMock.Verify(x => x.GetCustomerStatisticsAsync(customerId), Times.Once);
    }

    [Fact]
    public async Task GetCustomerStatistics_EmptyCustomer_ReturnsZeroStatistics()
    {
        // Arrange
        var customerId = 1L;
        var expectedStatistics = new TransactionStatisticsDto
        {
            TotalIncome = 0m,
            TotalExpense = 0m,
            NetProfit = 0m,
            TotalTransfer = 0m,
            IncomeCount = 0,
            ExpenseCount = 0,
            TransferCount = 0,
            TotalCount = 0
        };

        _transactionServiceMock
            .Setup(x => x.GetCustomerStatisticsAsync(customerId))
            .ReturnsAsync(expectedStatistics);

        // Act
        var result = await _controller.GetCustomerStatistics(customerId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<TransactionStatisticsDto>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.TotalCount.Should().Be(0);
        apiResponse.Data.TotalIncome.Should().Be(0m);
        apiResponse.Data.TotalExpense.Should().Be(0m);

        _transactionServiceMock.Verify(x => x.GetCustomerStatisticsAsync(customerId), Times.Once);
    }

    [Fact]
    public async Task GetSupplierStatistics_ValidSupplierId_ReturnsOkWithStatistics()
    {
        var supplierId = 1L;
        var expectedStatistics = new TransactionStatisticsDto
        {
            TotalIncome = 1000m,
            TotalExpense = 5000m,
            NetProfit = -4000m,
            TotalTransfer = 200m,
            IncomeCount = 1,
            ExpenseCount = 2,
            TransferCount = 1,
            TotalCount = 4
        };

        _transactionServiceMock
            .Setup(x => x.GetSupplierStatisticsAsync(supplierId))
            .ReturnsAsync(expectedStatistics);

        var result = await _controller.GetSupplierStatistics(supplierId);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<TransactionStatisticsDto>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data!.TotalExpense.Should().Be(5000m);
        apiResponse.Data.TotalTransfer.Should().Be(200m);
        _transactionServiceMock.Verify(x => x.GetSupplierStatisticsAsync(supplierId), Times.Once);
    }

    [Fact]
    public async Task GetSupplierStatistics_ServiceThrowsNotFoundException_ShouldPropagate()
    {
        var supplierId = 999L;
        _transactionServiceMock
            .Setup(x => x.GetSupplierStatisticsAsync(supplierId))
            .ThrowsAsync(new NotFoundException("供应商不存在"));

        await Assert.ThrowsAsync<NotFoundException>(() => _controller.GetSupplierStatistics(supplierId));

        _transactionServiceMock.Verify(x => x.GetSupplierStatisticsAsync(supplierId), Times.Once);
    }

    [Fact]
    public async Task GetSupplierStatistics_EmptySupplier_ReturnsZeroStatistics()
    {
        var supplierId = 1L;
        var expectedStatistics = new TransactionStatisticsDto();

        _transactionServiceMock
            .Setup(x => x.GetSupplierStatisticsAsync(supplierId))
            .ReturnsAsync(expectedStatistics);

        var result = await _controller.GetSupplierStatistics(supplierId);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<TransactionStatisticsDto>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data!.TotalCount.Should().Be(0);
        _transactionServiceMock.Verify(x => x.GetSupplierStatisticsAsync(supplierId), Times.Once);
    }

    [Fact]
    public async Task GetPersonStatistics_ValidPersonId_ReturnsOkWithStatistics()
    {
        var personId = 2L;
        var expectedStatistics = new TransactionStatisticsDto
        {
            TotalIncome = 6000m,
            TotalExpense = 1500m,
            NetProfit = 4500m,
            TotalTransfer = 300m,
            IncomeCount = 2,
            ExpenseCount = 1,
            TransferCount = 1,
            TotalCount = 4
        };

        _transactionServiceMock
            .Setup(x => x.GetPersonStatisticsAsync(personId))
            .ReturnsAsync(expectedStatistics);

        var result = await _controller.GetPersonStatistics(personId);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<TransactionStatisticsDto>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data!.NetProfit.Should().Be(4500m);
        apiResponse.Data.TotalTransfer.Should().Be(300m);
        _transactionServiceMock.Verify(x => x.GetPersonStatisticsAsync(personId), Times.Once);
    }

    [Fact]
    public async Task GetPersonStatistics_ServiceThrowsNotFoundException_ShouldPropagate()
    {
        var personId = 999L;
        _transactionServiceMock
            .Setup(x => x.GetPersonStatisticsAsync(personId))
            .ThrowsAsync(new NotFoundException("人员不存在"));

        await Assert.ThrowsAsync<NotFoundException>(() => _controller.GetPersonStatistics(personId));

        _transactionServiceMock.Verify(x => x.GetPersonStatisticsAsync(personId), Times.Once);
    }

    [Fact]
    public async Task GetPersonStatistics_EmptyPerson_ReturnsZeroStatistics()
    {
        var personId = 2L;
        var expectedStatistics = new TransactionStatisticsDto();

        _transactionServiceMock
            .Setup(x => x.GetPersonStatisticsAsync(personId))
            .ReturnsAsync(expectedStatistics);

        var result = await _controller.GetPersonStatistics(personId);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<TransactionStatisticsDto>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data!.TotalCount.Should().Be(0);
        _transactionServiceMock.Verify(x => x.GetPersonStatisticsAsync(personId), Times.Once);
    }
}
