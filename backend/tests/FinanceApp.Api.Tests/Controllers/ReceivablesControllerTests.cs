using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using FinanceApp.Api.Controllers.FinanceSettlement;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.FinanceSettlement.DTOs.Receivable;
using FinanceApp.Application.Modules.FinanceSettlement.Interfaces;

namespace FinanceApp.Api.Tests.Controllers;

public class ReceivablesControllerTests
{
    private readonly Mock<IReceivableService> _receivableServiceMock;
    private readonly Mock<ISettlementCandidateService> _settlementCandidateServiceMock;
    private readonly Mock<ILogger<ReceivablesController>> _loggerMock;
    private readonly ReceivablesController _controller;

    public ReceivablesControllerTests()
    {
        _receivableServiceMock = new Mock<IReceivableService>();
        _settlementCandidateServiceMock = new Mock<ISettlementCandidateService>();
        _loggerMock = new Mock<ILogger<ReceivablesController>>();
        _controller = new ReceivablesController(
            _receivableServiceMock.Object,
            _settlementCandidateServiceMock.Object,
            _loggerMock.Object);
    }

    #region GetPaged

    [Fact]
    public async Task GetPaged_ValidRequest_ReturnsOkWithPagedData()
    {
        // Arrange
        var request = new PageRequest { Page = 1, PageSize = 10 };
        var expectedResponse = new PageResponse<ReceivableDto>
        {
            Items = new List<ReceivableDto>
            {
                new ReceivableDto
                {
                    Id = 1,
                    ProjectName = "测试项目1",
                    CustomerName = "客户A",
                    TotalAmount = 10000.00m,
                    ReceivedAmount = 5000.00m,
                    RemainingAmount = 5000.00m,
                    Status = "partial"
                },
                new ReceivableDto
                {
                    Id = 2,
                    ProjectName = "测试项目2",
                    CustomerName = "客户B",
                    TotalAmount = 20000.00m,
                    ReceivedAmount = 0.00m,
                    RemainingAmount = 20000.00m,
                    Status = "pending"
                }
            },
            Total = 2,
            Page = 1,
            PageSize = 10
        };

        _receivableServiceMock
            .Setup(x => x.GetPagedAsync(It.IsAny<PageRequest>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetPaged(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<PageResponse<ReceivableDto>>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Items.Should().HaveCount(2);
        apiResponse.Data.Total.Should().Be(2);

        _receivableServiceMock.Verify(x => x.GetPagedAsync(It.IsAny<PageRequest>()), Times.Once);
    }

    [Fact]
    public async Task GetPaged_EmptyResult_ReturnsOkWithEmptyList()
    {
        // Arrange
        var request = new PageRequest { Page = 1, PageSize = 10 };
        var expectedResponse = new PageResponse<ReceivableDto>
        {
            Items = new List<ReceivableDto>(),
            Total = 0,
            Page = 1,
            PageSize = 10
        };

        _receivableServiceMock
            .Setup(x => x.GetPagedAsync(It.IsAny<PageRequest>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetPaged(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<PageResponse<ReceivableDto>>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Items.Should().BeEmpty();
        apiResponse.Data.Total.Should().Be(0);

        _receivableServiceMock.Verify(x => x.GetPagedAsync(It.IsAny<PageRequest>()), Times.Once);
    }

    [Fact]
    public async Task GetPaged_ServiceThrowsException_RethrowsException()
    {
        // Arrange
        var request = new PageRequest { Page = 1, PageSize = 10 };
        _receivableServiceMock
            .Setup(x => x.GetPagedAsync(It.IsAny<PageRequest>()))
            .ThrowsAsync(new InvalidOperationException("数据库连接失败"));

        // Act
        Func<Task> act = async () => await _controller.GetPaged(request);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("数据库连接失败");
    }

    #endregion

    #region GetById

    [Fact]
    public async Task GetById_ExistingId_ReturnsOkWithReceivable()
    {
        // Arrange
        var receivableId = 1L;
        var expectedReceivable = new ReceivableDto
        {
            Id = receivableId,
            ProjectId = 1,
            ProjectName = "测试项目",
            CustomerId = 1,
            CustomerName = "测试客户",
            TotalAmount = 50000.00m,
            ReceivedAmount = 20000.00m,
            RemainingAmount = 30000.00m,
            DueDate = new DateTime(2026, 6, 30),
            Status = "partial",
            Description = "项目一期款项",
            Details = new List<ReceivableDetailDto>
            {
                new ReceivableDetailDto
                {
                    Id = 1,
                    TransactionId = 100,
                    PaymentDate = new DateTime(2026, 3, 1),
                    Amount = 20000.00m,
                    PaymentMethod = "银行转账",
                    Description = "首笔收款"
                }
            }
        };

        _receivableServiceMock
            .Setup(x => x.GetByIdAsync(receivableId))
            .ReturnsAsync(expectedReceivable);

        // Act
        var result = await _controller.GetById(receivableId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<ReceivableDto>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Id.Should().Be(receivableId);
        apiResponse.Data.CustomerName.Should().Be("测试客户");
        apiResponse.Data.TotalAmount.Should().Be(50000.00m);
        apiResponse.Data.RemainingAmount.Should().Be(30000.00m);
        apiResponse.Data.Details.Should().HaveCount(1);

        _receivableServiceMock.Verify(x => x.GetByIdAsync(receivableId), Times.Once);
    }

    [Fact]
    public async Task GetById_ServiceThrowsException_RethrowsException()
    {
        // Arrange
        var receivableId = 999L;
        _receivableServiceMock
            .Setup(x => x.GetByIdAsync(receivableId))
            .ThrowsAsync(new NotFoundException("应收款不存在"));

        // Act
        Func<Task> act = async () => await _controller.GetById(receivableId);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("应收款不存在");
    }

    #endregion

    #region Create

    [Fact]
    public async Task Create_ValidRequest_ReturnsOkWithCreatedReceivable()
    {
        // Arrange
        var request = new CreateReceivableRequest
        {
            ProjectId = 1,
            CustomerId = 1,
            TotalAmount = 100000.00m,
            DueDate = new DateTime(2026, 12, 31),
            Description = "新项目合同款"
        };

        var expectedReceivable = new ReceivableDto
        {
            Id = 1,
            ProjectId = request.ProjectId,
            ProjectName = "测试项目",
            CustomerId = request.CustomerId,
            CustomerName = "测试客户",
            TotalAmount = request.TotalAmount,
            ReceivedAmount = 0.00m,
            RemainingAmount = request.TotalAmount,
            DueDate = request.DueDate!.Value,
            Status = "pending",
            Description = request.Description
        };

        _receivableServiceMock
            .Setup(x => x.CreateAsync(It.IsAny<CreateReceivableRequest>()))
            .ReturnsAsync(expectedReceivable);

        // Act
        var result = await _controller.Create(request);

        // Assert
        var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(201);

        var apiResponse = objectResult.Value.Should().BeOfType<ApiResponse<ReceivableDto>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Message.Should().Be("创建成功");
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Id.Should().Be(1);
        apiResponse.Data.CustomerName.Should().Be("测试客户");
        apiResponse.Data.TotalAmount.Should().Be(100000.00m);
        apiResponse.Data.RemainingAmount.Should().Be(100000.00m);
        apiResponse.Data.Status.Should().Be("pending");

        _receivableServiceMock.Verify(x => x.CreateAsync(It.IsAny<CreateReceivableRequest>()), Times.Once);
    }

    [Fact]
    public async Task Create_ServiceThrowsException_RethrowsException()
    {
        // Arrange
        var request = new CreateReceivableRequest
        {
            ProjectId = 999,
            CustomerId = 999,
            TotalAmount = 10000.00m
        };

        _receivableServiceMock
            .Setup(x => x.CreateAsync(It.IsAny<CreateReceivableRequest>()))
            .ThrowsAsync(new InvalidOperationException("项目不存在"));

        // Act
        Func<Task> act = async () => await _controller.Create(request);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("项目不存在");
    }

    #endregion

    #region ReceivePayment

    [Fact]
    public async Task ReceivePayment_ValidRequest_ReturnsOkWithUpdatedReceivable()
    {
        // Arrange
        var receivableId = 1L;
        var request = new ReceivePaymentRequest
        {
            PaymentDate = new DateTime(2026, 3, 14),
            Amount = 30000.00m,
            PaymentMethod = "银行转账",
            Description = "项目一期尾款",
            TransactionId = 200
        };

        var expectedReceivable = new ReceivableDto
        {
            Id = receivableId,
            ProjectId = 1,
            ProjectName = "测试项目",
            CustomerId = 1,
            CustomerName = "测试客户",
            TotalAmount = 50000.00m,
            ReceivedAmount = 50000.00m,
            RemainingAmount = 0.00m,
            Status = "settled",
            Details = new List<ReceivableDetailDto>
            {
                new ReceivableDetailDto
                {
                    Id = 1,
                    TransactionId = 100,
                    PaymentDate = new DateTime(2026, 3, 1),
                    Amount = 20000.00m,
                    PaymentMethod = "银行转账"
                },
                new ReceivableDetailDto
                {
                    Id = 2,
                    TransactionId = 200,
                    PaymentDate = new DateTime(2026, 3, 14),
                    Amount = 30000.00m,
                    PaymentMethod = "银行转账"
                }
            }
        };

        _receivableServiceMock
            .Setup(x => x.ReceivePaymentAsync(receivableId, It.IsAny<ReceivePaymentRequest>()))
            .ReturnsAsync(expectedReceivable);

        // Act
        var result = await _controller.ReceivePayment(receivableId, request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<ReceivableDto>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Message.Should().Be("收款登记成功");
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Id.Should().Be(receivableId);
        apiResponse.Data.ReceivedAmount.Should().Be(50000.00m);
        apiResponse.Data.RemainingAmount.Should().Be(0.00m);
        apiResponse.Data.Status.Should().Be("settled");
        apiResponse.Data.Details.Should().HaveCount(2);

        _receivableServiceMock.Verify(x => x.ReceivePaymentAsync(receivableId, It.IsAny<ReceivePaymentRequest>()), Times.Once);
    }

    [Fact]
    public async Task ReceivePayment_PartialPayment_ReturnsOkWithPartialStatus()
    {
        // Arrange
        var receivableId = 1L;
        var request = new ReceivePaymentRequest
        {
            PaymentDate = new DateTime(2026, 3, 14),
            Amount = 10000.00m,
            PaymentMethod = "支票",
            Description = "部分收款"
        };

        var expectedReceivable = new ReceivableDto
        {
            Id = receivableId,
            ProjectId = 1,
            ProjectName = "测试项目",
            CustomerId = 1,
            CustomerName = "测试客户",
            TotalAmount = 50000.00m,
            ReceivedAmount = 10000.00m,
            RemainingAmount = 40000.00m,
            Status = "partial",
            Details = new List<ReceivableDetailDto>
            {
                new ReceivableDetailDto
                {
                    Id = 1,
                    TransactionId = 100,
                    PaymentDate = new DateTime(2026, 3, 14),
                    Amount = 10000.00m,
                    PaymentMethod = "支票"
                }
            }
        };

        _receivableServiceMock
            .Setup(x => x.ReceivePaymentAsync(receivableId, It.IsAny<ReceivePaymentRequest>()))
            .ReturnsAsync(expectedReceivable);

        // Act
        var result = await _controller.ReceivePayment(receivableId, request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<ReceivableDto>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Message.Should().Be("收款登记成功");
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.RemainingAmount.Should().Be(40000.00m);
        apiResponse.Data.Status.Should().Be("partial");

        _receivableServiceMock.Verify(x => x.ReceivePaymentAsync(receivableId, It.IsAny<ReceivePaymentRequest>()), Times.Once);
    }

    [Fact]
    public async Task ReceivePayment_ServiceThrowsException_RethrowsException()
    {
        // Arrange
        var receivableId = 999L;
        var request = new ReceivePaymentRequest
        {
            PaymentDate = DateTime.Now,
            Amount = 10000.00m
        };

        _receivableServiceMock
            .Setup(x => x.ReceivePaymentAsync(receivableId, It.IsAny<ReceivePaymentRequest>()))
            .ThrowsAsync(new NotFoundException("应收款不存在"));

        // Act
        Func<Task> act = async () => await _controller.ReceivePayment(receivableId, request);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("应收款不存在");
    }

    #endregion

    #region GetSummary

    [Fact]
    public async Task GetSummary_ReturnsOkWithSummaryData()
    {
        // Arrange
        var expectedSummary = new ReceivableSummaryDto
        {
            TotalReceivable = 500000.00m,
            TotalReceived = 200000.00m,
            TotalRemaining = 300000.00m,
            PendingCount = 5,
            PartialCount = 3,
            SettledCount = 10,
            OverdueCount = 2
        };

        _receivableServiceMock
            .Setup(x => x.GetReceivableSummaryAsync())
            .ReturnsAsync(expectedSummary);

        // Act
        var result = await _controller.GetSummary();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<ReceivableSummaryDto>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.TotalReceivable.Should().Be(500000.00m);
        apiResponse.Data.TotalReceived.Should().Be(200000.00m);
        apiResponse.Data.TotalRemaining.Should().Be(300000.00m);
        apiResponse.Data.PendingCount.Should().Be(5);
        apiResponse.Data.PartialCount.Should().Be(3);
        apiResponse.Data.SettledCount.Should().Be(10);
        apiResponse.Data.OverdueCount.Should().Be(2);

        _receivableServiceMock.Verify(x => x.GetReceivableSummaryAsync(), Times.Once);
    }

    [Fact]
    public async Task GetSummary_NoData_ReturnsOkWithZeroValues()
    {
        // Arrange
        var expectedSummary = new ReceivableSummaryDto
        {
            TotalReceivable = 0m,
            TotalReceived = 0m,
            TotalRemaining = 0m,
            PendingCount = 0,
            PartialCount = 0,
            SettledCount = 0,
            OverdueCount = 0
        };

        _receivableServiceMock
            .Setup(x => x.GetReceivableSummaryAsync())
            .ReturnsAsync(expectedSummary);

        // Act
        var result = await _controller.GetSummary();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<ReceivableSummaryDto>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.TotalReceivable.Should().Be(0m);
        apiResponse.Data.TotalReceived.Should().Be(0m);
        apiResponse.Data.TotalRemaining.Should().Be(0m);
        apiResponse.Data.PendingCount.Should().Be(0);

        _receivableServiceMock.Verify(x => x.GetReceivableSummaryAsync(), Times.Once);
    }

    [Fact]
    public async Task GetSummary_ServiceThrowsException_RethrowsException()
    {
        // Arrange
        _receivableServiceMock
            .Setup(x => x.GetReceivableSummaryAsync())
            .ThrowsAsync(new InvalidOperationException("获取汇总信息失败"));

        // Act
        Func<Task> act = async () => await _controller.GetSummary();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("获取汇总信息失败");
    }

    #endregion

    #region GetStatistics

    [Fact]
    public async Task GetStatistics_ReturnsOkWithStatisticsData()
    {
        // Arrange
        var request = new PageRequest { Page = 1, PageSize = 10 };
        var expectedStatistics = new ReceivableStatisticsDto
        {
            TotalCount = 18,
            PendingCount = 5,
            PartialCount = 3,
            SettledCount = 10,
            TotalAmount = 500000.00m,
            ReceivedAmount = 200000.00m,
            RemainingAmount = 300000.00m,
            OverdueAmount = 50000.00m
        };

        _receivableServiceMock
            .Setup(x => x.GetStatisticsAsync(It.IsAny<PageRequest>()))
            .ReturnsAsync(expectedStatistics);

        // Act
        var result = await _controller.GetStatistics(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<ReceivableStatisticsDto>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.TotalCount.Should().Be(18);
        apiResponse.Data.PendingCount.Should().Be(5);
        apiResponse.Data.PartialCount.Should().Be(3);
        apiResponse.Data.SettledCount.Should().Be(10);
        apiResponse.Data.TotalAmount.Should().Be(500000.00m);
        apiResponse.Data.ReceivedAmount.Should().Be(200000.00m);
        apiResponse.Data.RemainingAmount.Should().Be(300000.00m);
        apiResponse.Data.OverdueAmount.Should().Be(50000.00m);

        _receivableServiceMock.Verify(x => x.GetStatisticsAsync(It.IsAny<PageRequest>()), Times.Once);
    }

    [Fact]
    public async Task GetStatistics_NoData_ReturnsOkWithZeroValues()
    {
        // Arrange
        var request = new PageRequest { Page = 1, PageSize = 10 };
        var expectedStatistics = new ReceivableStatisticsDto
        {
            TotalCount = 0,
            PendingCount = 0,
            PartialCount = 0,
            SettledCount = 0,
            TotalAmount = 0m,
            ReceivedAmount = 0m,
            RemainingAmount = 0m,
            OverdueAmount = 0m
        };

        _receivableServiceMock
            .Setup(x => x.GetStatisticsAsync(It.IsAny<PageRequest>()))
            .ReturnsAsync(expectedStatistics);

        // Act
        var result = await _controller.GetStatistics(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<ReceivableStatisticsDto>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.TotalCount.Should().Be(0);
        apiResponse.Data.PendingCount.Should().Be(0);
        apiResponse.Data.PartialCount.Should().Be(0);
        apiResponse.Data.SettledCount.Should().Be(0);
        apiResponse.Data.TotalAmount.Should().Be(0m);
        apiResponse.Data.ReceivedAmount.Should().Be(0m);
        apiResponse.Data.RemainingAmount.Should().Be(0m);
        apiResponse.Data.OverdueAmount.Should().Be(0m);

        _receivableServiceMock.Verify(x => x.GetStatisticsAsync(It.IsAny<PageRequest>()), Times.Once);
    }

    [Fact]
    public async Task GetStatistics_ServiceThrowsException_RethrowsException()
    {
        // Arrange
        var request = new PageRequest { Page = 1, PageSize = 10 };
        _receivableServiceMock
            .Setup(x => x.GetStatisticsAsync(It.IsAny<PageRequest>()))
            .ThrowsAsync(new InvalidOperationException("获取统计信息失败"));

        // Act
        Func<Task> act = async () => await _controller.GetStatistics(request);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("获取统计信息失败");
    }

    [Fact]
    public async Task GetAvailableForTransaction_ValidRequest_ReturnsOk()
    {
        var expected = new List<ReceivableDto>
        {
            new() { Id = 8, ProjectName = "项目A", RemainingAmount = 200m }
        };
        _settlementCandidateServiceMock
            .Setup(x => x.GetAvailableReceivablesForTransactionAsync(15, "项目"))
            .ReturnsAsync(expected);

        var result = await _controller.GetAvailableForTransaction(15, "项目");

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = ok.Value.Should().BeOfType<ApiResponse<List<ReceivableDto>>>().Subject;
        apiResponse.Data.Should().HaveCount(1);
        apiResponse.Data![0].Id.Should().Be(8);
    }

    #endregion
}
