using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using FinanceApp.Api.Controllers.MasterData;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.MasterData.DTOs.Account;
using FinanceApp.Application.Modules.MasterData.Interfaces;

namespace FinanceApp.Api.Tests.Controllers;

public class AccountControllerTests
{
    private readonly Mock<IAccountService> _accountServiceMock;
    private readonly Mock<ILogger<AccountController>> _loggerMock;
    private readonly AccountController _controller;

    public AccountControllerTests()
    {
        _accountServiceMock = new Mock<IAccountService>();
        _loggerMock = new Mock<ILogger<AccountController>>();
        _controller = new AccountController(_accountServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetPaged_ValidRequest_ReturnsOkWithPagedData()
    {
        // Arrange
        var request = new PageRequest { Page = 1, PageSize = 10 };
        var expectedResponse = new PageResponse<AccountDto>
        {
            Items = new List<AccountDto>
            {
                new AccountDto { Id = 1, Name = "测试账户1", AccountNumber = "6222001234567890" },
                new AccountDto { Id = 2, Name = "测试账户2", AccountNumber = "6222009876543210" }
            },
            Total = 2,
            Page = 1,
            PageSize = 10
        };

        _accountServiceMock
            .Setup(x => x.GetPagedAsync(It.IsAny<PageRequest>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetPaged(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<PageResponse<AccountDto>>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Items.Should().HaveCount(2);
        apiResponse.Data.Total.Should().Be(2);

        _accountServiceMock.Verify(x => x.GetPagedAsync(It.IsAny<PageRequest>()), Times.Once);
    }

    [Fact]
    public async Task GetById_ExistingId_ReturnsOkWithAccount()
    {
        // Arrange
        var accountId = 1L;
        var expectedAccount = new AccountDto
        {
            Id = accountId,
            Name = "测试账户",
            AccountNumber = "6222001234567890",
            BankName = "中国工商银行",
            Balance = 10000.00m
        };

        _accountServiceMock
            .Setup(x => x.GetByIdAsync(accountId))
            .ReturnsAsync(expectedAccount);

        // Act
        var result = await _controller.GetById(accountId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<AccountDto>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Id.Should().Be(accountId);
        apiResponse.Data.Name.Should().Be("测试账户");

        _accountServiceMock.Verify(x => x.GetByIdAsync(accountId), Times.Once);
    }

    [Fact]
    public async Task Create_ValidRequest_ReturnsOkWithCreatedAccount()
    {
        // Arrange
        var request = new CreateAccountRequest
        {
            Name = "新账户",
            AccountNumber = "6222001234567890",
            BankName = "中国工商银行",
            InitialBalance = 5000.00m
        };

        var expectedAccount = new AccountDto
        {
            Id = 1,
            Name = request.Name,
            AccountNumber = request.AccountNumber,
            BankName = request.BankName,
            Balance = request.InitialBalance
        };

        _accountServiceMock
            .Setup(x => x.CreateAsync(It.IsAny<CreateAccountRequest>()))
            .ReturnsAsync(expectedAccount);

        // Act
        var result = await _controller.Create(request);

        // Assert
        var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(201);

        var apiResponse = objectResult.Value.Should().BeOfType<ApiResponse<AccountDto>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Name.Should().Be("新账户");
        apiResponse.Message.Should().Be("账户创建成功");

        _accountServiceMock.Verify(x => x.CreateAsync(It.IsAny<CreateAccountRequest>()), Times.Once);
    }

    [Fact]
    public async Task Update_ValidRequest_ReturnsOkWithUpdatedAccount()
    {
        // Arrange
        var accountId = 1L;
        var request = new UpdateAccountRequest
        {
            Name = "更新后的账户",
            BankName = "中国建设银行"
        };

        var expectedAccount = new AccountDto
        {
            Id = accountId,
            Name = request.Name,
            AccountNumber = "6222001234567890",
            BankName = request.BankName,
            Balance = 10000.00m
        };

        _accountServiceMock
            .Setup(x => x.UpdateAsync(accountId, It.IsAny<UpdateAccountRequest>()))
            .ReturnsAsync(expectedAccount);

        // Act
        var result = await _controller.Update(accountId, request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<AccountDto>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Name.Should().Be("更新后的账户");
        apiResponse.Message.Should().Be("账户更新成功");

        _accountServiceMock.Verify(x => x.UpdateAsync(accountId, It.IsAny<UpdateAccountRequest>()), Times.Once);
    }

    [Fact]
    public async Task Delete_ExistingId_ReturnsOkWithSuccessMessage()
    {
        // Arrange
        var accountId = 1L;

        _accountServiceMock
            .Setup(x => x.DeleteAsync(accountId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Delete(accountId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<object>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Message.Should().Be("账户删除成功");

        _accountServiceMock.Verify(x => x.DeleteAsync(accountId), Times.Once);
    }

    [Fact]
    public async Task GetActive_ReturnsOkWithActiveAccounts()
    {
        // Arrange
        var expectedAccounts = new List<AccountDto>
        {
            new AccountDto { Id = 1, Name = "活跃账户1", IsActive = true },
            new AccountDto { Id = 2, Name = "活跃账户2", IsActive = true }
        };

        _accountServiceMock
            .Setup(x => x.GetActiveAccountsAsync())
            .ReturnsAsync(expectedAccounts);

        // Act
        var result = await _controller.GetActive();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<List<AccountDto>>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Should().HaveCount(2);
        apiResponse.Data.All(a => a.IsActive).Should().BeTrue();

        _accountServiceMock.Verify(x => x.GetActiveAccountsAsync(), Times.Once);
    }
}
