using FluentAssertions;
using FinanceApp.Application.Modules.MasterData.DTOs.FixedDeposit;
using FinanceApp.Application.Modules.MasterData.Interfaces;
using FinanceApp.Api.Controllers.MasterData;
using FinanceApp.Application.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace FinanceApp.Api.Tests.Controllers;

public class FixedDepositsControllerTests
{
    private readonly Mock<IFixedDepositService> _serviceMock;
    private readonly Mock<ILogger<FixedDepositsController>> _loggerMock;
    private readonly FixedDepositsController _controller;

    public FixedDepositsControllerTests()
    {
        _serviceMock = new Mock<IFixedDepositService>();
        _loggerMock = new Mock<ILogger<FixedDepositsController>>();
        _controller = new FixedDepositsController(_serviceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Create_ValidRequest_ReturnsOk()
    {
        var request = new CreateFixedDepositRequest
        {
            AccountId = 1,
            Principal = 10000m,
            TermMonths = 3,
            InterestRate = 2.1m,
            DepositDate = DateTime.UtcNow.Date
        };

        _serviceMock.Setup(x => x.CreateAsync(It.IsAny<CreateFixedDepositRequest>()))
            .ReturnsAsync(new FixedDepositDto { Id = 1, AccountId = 1, AccountName = "定期账户", Principal = 10000m, TermMonths = 3, InterestRate = 2.1m, Status = "Active" });

        var result = await _controller.Create(request);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<FixedDepositDto>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Message.Should().Be("定期存款创建成功");
    }

    [Fact]
    public async Task GetMaturing_ReturnsOk()
    {
        _serviceMock.Setup(x => x.GetMaturingAsync(30))
            .ReturnsAsync(new List<FixedDepositDto>
            {
                new() { Id = 1, AccountId = 1, AccountName = "定期账户", Principal = 10000m, TermMonths = 3, InterestRate = 2.1m, Status = "Active", DaysToMaturity = 5 }
            });

        var result = await _controller.GetMaturing(30);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<List<FixedDepositDto>>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().HaveCount(1);
    }

    [Fact]
    public async Task Withdraw_ValidRequest_ReturnsOk()
    {
        var request = new WithdrawFixedDepositRequest
        {
            WithdrawalDate = DateTime.UtcNow.Date,
            ActualInterest = 100m
        };

        _serviceMock.Setup(x => x.WithdrawAsync(1, It.IsAny<WithdrawFixedDepositRequest>()))
            .ReturnsAsync(new FixedDepositDto { Id = 1, AccountId = 1, AccountName = "定期账户", Principal = 10000m, Status = "Withdrawn", ActualInterest = 100m });

        var result = await _controller.Withdraw(1, request);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<FixedDepositDto>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Message.Should().Be("定期存款支取成功");
    }
}
