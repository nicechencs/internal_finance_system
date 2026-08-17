using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using FinanceApp.Api.Controllers.FinanceSettlement;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.FinanceSettlement.DTOs.Payable;
using FinanceApp.Application.Modules.FinanceSettlement.Interfaces;


namespace FinanceApp.Api.Tests.Controllers;

public class PayablesControllerTests
{
    private readonly Mock<IPayableService> _payableServiceMock;
    private readonly Mock<ISettlementCandidateService> _settlementCandidateServiceMock;
    private readonly Mock<ILogger<PayablesController>> _loggerMock;
    private readonly PayablesController _controller;

    public PayablesControllerTests()
    {
        _payableServiceMock = new Mock<IPayableService>();
        _settlementCandidateServiceMock = new Mock<ISettlementCandidateService>();
        _loggerMock = new Mock<ILogger<PayablesController>>();
        _controller = new PayablesController(
            _payableServiceMock.Object,
            _settlementCandidateServiceMock.Object,
            _loggerMock.Object);
    }

    #region GetPaged

    [Fact]
    public async Task GetPaged_ValidRequest_ReturnsOkWithPagedData()
    {
        // Arrange
        var request = new PageRequest { Page = 1, PageSize = 10 };
        var expectedResponse = new PageResponse<PayableDto>
        {
            Items = new List<PayableDto>
            {
                new PayableDto
                {
                    Id = 1,
                    SupplierId = 1,
                    SupplierName = "供应商A",
                    TotalAmount = 10000.00m,
                    PaidAmount = 5000.00m,
                    RemainingAmount = 5000.00m,
                    Status = "partial"
                },
                new PayableDto
                {
                    Id = 2,
                    SupplierId = 2,
                    SupplierName = "供应商B",
                    TotalAmount = 20000.00m,
                    PaidAmount = 0.00m,
                    RemainingAmount = 20000.00m,
                    Status = "pending"
                }
            },
            Total = 2,
            Page = 1,
            PageSize = 10
        };

        _payableServiceMock
            .Setup(x => x.GetPagedAsync(It.IsAny<PageRequest>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetPaged(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<PageResponse<PayableDto>>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Items.Should().HaveCount(2);
        apiResponse.Data.Total.Should().Be(2);

        _payableServiceMock.Verify(x => x.GetPagedAsync(It.IsAny<PageRequest>()), Times.Once);
    }

    [Fact]
    public async Task GetPaged_EmptyResult_ReturnsOkWithEmptyList()
    {
        // Arrange
        var request = new PageRequest { Page = 1, PageSize = 10 };
        var expectedResponse = new PageResponse<PayableDto>
        {
            Items = new List<PayableDto>(),
            Total = 0,
            Page = 1,
            PageSize = 10
        };

        _payableServiceMock
            .Setup(x => x.GetPagedAsync(It.IsAny<PageRequest>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetPaged(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<PageResponse<PayableDto>>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Items.Should().BeEmpty();
        apiResponse.Data.Total.Should().Be(0);

        _payableServiceMock.Verify(x => x.GetPagedAsync(It.IsAny<PageRequest>()), Times.Once);
    }

    [Fact]
    public async Task GetPaged_ServiceThrowsException_RethrowsException()
    {
        // Arrange
        var request = new PageRequest { Page = 1, PageSize = 10 };
        _payableServiceMock
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
    public async Task GetById_ExistingId_ReturnsOkWithPayable()
    {
        // Arrange
        var payableId = 1L;
        var expectedPayable = new PayableDto
        {
            Id = payableId,
            SupplierId = 1,
            SupplierName = "测试供应商",
            ProjectId = 1,
            ProjectName = "测试项目",
            TotalAmount = 50000.00m,
            PaidAmount = 20000.00m,
            RemainingAmount = 30000.00m,
            DueDate = new DateTime(2026, 6, 30),
            Status = "partial",
            Description = "项目一期款项",
            Details = new List<PayableDetailDto>
            {
                new PayableDetailDto
                {
                    Id = 1,
                    PayableId = payableId,
                    TransactionId = 100,
                    PaymentDate = new DateTime(2026, 3, 1),
                    Amount = 20000.00m,
                    PaymentMethod = "银行转账",
                    Description = "首笔付款"
                }
            }
        };

        _payableServiceMock
            .Setup(x => x.GetByIdAsync(payableId))
            .ReturnsAsync(expectedPayable);

        // Act
        var result = await _controller.GetById(payableId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<PayableDto>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Id.Should().Be(payableId);
        apiResponse.Data.SupplierName.Should().Be("测试供应商");
        apiResponse.Data.TotalAmount.Should().Be(50000.00m);
        apiResponse.Data.RemainingAmount.Should().Be(30000.00m);
        apiResponse.Data.Details.Should().HaveCount(1);

        _payableServiceMock.Verify(x => x.GetByIdAsync(payableId), Times.Once);
    }

    [Fact]
    public async Task GetById_ServiceThrowsException_RethrowsException()
    {
        // Arrange
        var payableId = 999L;
        _payableServiceMock
            .Setup(x => x.GetByIdAsync(payableId))
            .ThrowsAsync(new NotFoundException("应付款不存在"));

        // Act
        Func<Task> act = async () => await _controller.GetById(payableId);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("应付款不存在");
    }

    #endregion

    #region Create

    [Fact]
    public async Task Create_ValidRequest_ReturnsOkWithCreatedPayable()
    {
        // Arrange
        var request = new CreatePayableRequest
        {
            SupplierId = 1,
            ProjectId = 1,
            TotalAmount = 100000.00m,
            DueDate = new DateTime(2026, 12, 31),
            Description = "新项目合同款"
        };

        var expectedPayable = new PayableDto
        {
            Id = 1,
            SupplierId = request.SupplierId,
            SupplierName = "测试供应商",
            ProjectId = request.ProjectId,
            ProjectName = "测试项目",
            TotalAmount = request.TotalAmount,
            PaidAmount = 0.00m,
            RemainingAmount = request.TotalAmount,
            DueDate = request.DueDate,
            Status = "pending",
            Description = request.Description
        };

        _payableServiceMock
            .Setup(x => x.CreateAsync(It.IsAny<CreatePayableRequest>()))
            .ReturnsAsync(expectedPayable);

        // Act
        var result = await _controller.Create(request);

        // Assert
        var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(201);

        var apiResponse = objectResult.Value.Should().BeOfType<ApiResponse<PayableDto>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Message.Should().Be("创建成功");
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Id.Should().Be(1);
        apiResponse.Data.SupplierName.Should().Be("测试供应商");
        apiResponse.Data.TotalAmount.Should().Be(100000.00m);
        apiResponse.Data.RemainingAmount.Should().Be(100000.00m);
        apiResponse.Data.Status.Should().Be("pending");

        _payableServiceMock.Verify(x => x.CreateAsync(It.IsAny<CreatePayableRequest>()), Times.Once);
    }

    [Fact]
    public async Task Create_ServiceThrowsException_RethrowsException()
    {
        // Arrange
        var request = new CreatePayableRequest
        {
            SupplierId = 999,
            TotalAmount = 10000.00m
        };

        _payableServiceMock
            .Setup(x => x.CreateAsync(It.IsAny<CreatePayableRequest>()))
            .ThrowsAsync(new InvalidOperationException("供应商不存在"));

        // Act
        Func<Task> act = async () => await _controller.Create(request);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("供应商不存在");
    }

    #endregion

    #region PayPayment

    [Fact]
    public async Task PayPayment_ValidRequest_ReturnsOkWithUpdatedPayable()
    {
        // Arrange
        var payableId = 1L;
        var request = new PayPaymentRequest
        {
            PaymentDate = new DateTime(2026, 3, 14),
            Amount = 30000.00m,
            PaymentMethod = "银行转账",
            Description = "项目一期尾款",
            TransactionId = 200
        };

        var expectedPayable = new PayableDto
        {
            Id = payableId,
            SupplierId = 1,
            SupplierName = "测试供应商",
            ProjectId = 1,
            ProjectName = "测试项目",
            TotalAmount = 50000.00m,
            PaidAmount = 50000.00m,
            RemainingAmount = 0.00m,
            Status = "settled",
            Details = new List<PayableDetailDto>
            {
                new PayableDetailDto
                {
                    Id = 1,
                    PayableId = payableId,
                    TransactionId = 100,
                    PaymentDate = new DateTime(2026, 3, 1),
                    Amount = 20000.00m,
                    PaymentMethod = "银行转账"
                },
                new PayableDetailDto
                {
                    Id = 2,
                    PayableId = payableId,
                    TransactionId = 200,
                    PaymentDate = new DateTime(2026, 3, 14),
                    Amount = 30000.00m,
                    PaymentMethod = "银行转账"
                }
            }
        };

        _payableServiceMock
            .Setup(x => x.PayPaymentAsync(payableId, It.IsAny<PayPaymentRequest>()))
            .ReturnsAsync(expectedPayable);

        // Act
        var result = await _controller.PayPayment(payableId, request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<PayableDto>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Message.Should().Be("付款登记成功");
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Id.Should().Be(payableId);
        apiResponse.Data.PaidAmount.Should().Be(50000.00m);
        apiResponse.Data.RemainingAmount.Should().Be(0.00m);
        apiResponse.Data.Status.Should().Be("settled");
        apiResponse.Data.Details.Should().HaveCount(2);

        _payableServiceMock.Verify(x => x.PayPaymentAsync(payableId, It.IsAny<PayPaymentRequest>()), Times.Once);
    }

    [Fact]
    public async Task PayPayment_PartialPayment_ReturnsOkWithPartialStatus()
    {
        // Arrange
        var payableId = 1L;
        var request = new PayPaymentRequest
        {
            PaymentDate = new DateTime(2026, 3, 14),
            Amount = 10000.00m,
            PaymentMethod = "支票",
            Description = "部分付款"
        };

        var expectedPayable = new PayableDto
        {
            Id = payableId,
            SupplierId = 1,
            SupplierName = "测试供应商",
            ProjectId = 1,
            ProjectName = "测试项目",
            TotalAmount = 50000.00m,
            PaidAmount = 10000.00m,
            RemainingAmount = 40000.00m,
            Status = "partial",
            Details = new List<PayableDetailDto>
            {
                new PayableDetailDto
                {
                    Id = 1,
                    PayableId = payableId,
                    TransactionId = 100,
                    PaymentDate = new DateTime(2026, 3, 14),
                    Amount = 10000.00m,
                    PaymentMethod = "支票"
                }
            }
        };

        _payableServiceMock
            .Setup(x => x.PayPaymentAsync(payableId, It.IsAny<PayPaymentRequest>()))
            .ReturnsAsync(expectedPayable);

        // Act
        var result = await _controller.PayPayment(payableId, request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<PayableDto>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Message.Should().Be("付款登记成功");
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.RemainingAmount.Should().Be(40000.00m);
        apiResponse.Data.Status.Should().Be("partial");

        _payableServiceMock.Verify(x => x.PayPaymentAsync(payableId, It.IsAny<PayPaymentRequest>()), Times.Once);
    }

    [Fact]
    public async Task PayPayment_ServiceThrowsException_RethrowsException()
    {
        // Arrange
        var payableId = 999L;
        var request = new PayPaymentRequest
        {
            PaymentDate = DateTime.Now,
            Amount = 10000.00m
        };

        _payableServiceMock
            .Setup(x => x.PayPaymentAsync(payableId, It.IsAny<PayPaymentRequest>()))
            .ThrowsAsync(new NotFoundException("应付款不存在"));

        // Act
        Func<Task> act = async () => await _controller.PayPayment(payableId, request);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("应付款不存在");
    }

    #endregion

    #region GetSummary

    [Fact]
    public async Task GetSummary_ReturnsOkWithSummaryData()
    {
        // Arrange
        var expectedSummary = new PayableSummaryDto
        {
            TotalPayable = 500000.00m,
            TotalPaid = 200000.00m,
            TotalRemaining = 300000.00m,
            PendingCount = 5,
            PartialCount = 3,
            SettledCount = 10,
            OverdueCount = 2
        };

        _payableServiceMock
            .Setup(x => x.GetPayableSummaryAsync())
            .ReturnsAsync(expectedSummary);

        // Act
        var result = await _controller.GetSummary();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<PayableSummaryDto>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.TotalPayable.Should().Be(500000.00m);
        apiResponse.Data.TotalPaid.Should().Be(200000.00m);
        apiResponse.Data.TotalRemaining.Should().Be(300000.00m);
        apiResponse.Data.PendingCount.Should().Be(5);
        apiResponse.Data.PartialCount.Should().Be(3);
        apiResponse.Data.SettledCount.Should().Be(10);
        apiResponse.Data.OverdueCount.Should().Be(2);

        _payableServiceMock.Verify(x => x.GetPayableSummaryAsync(), Times.Once);
    }

    [Fact]
    public async Task GetSummary_NoData_ReturnsOkWithZeroValues()
    {
        // Arrange
        var expectedSummary = new PayableSummaryDto
        {
            TotalPayable = 0m,
            TotalPaid = 0m,
            TotalRemaining = 0m,
            PendingCount = 0,
            PartialCount = 0,
            SettledCount = 0,
            OverdueCount = 0
        };

        _payableServiceMock
            .Setup(x => x.GetPayableSummaryAsync())
            .ReturnsAsync(expectedSummary);

        // Act
        var result = await _controller.GetSummary();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<PayableSummaryDto>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.TotalPayable.Should().Be(0m);
        apiResponse.Data.TotalPaid.Should().Be(0m);
        apiResponse.Data.TotalRemaining.Should().Be(0m);
        apiResponse.Data.PendingCount.Should().Be(0);

        _payableServiceMock.Verify(x => x.GetPayableSummaryAsync(), Times.Once);
    }

    [Fact]
    public async Task GetSummary_ServiceThrowsException_RethrowsException()
    {
        // Arrange
        _payableServiceMock
            .Setup(x => x.GetPayableSummaryAsync())
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
        var expectedStatistics = new PayableStatisticsDto
        {
            TotalCount = 18,
            PendingCount = 5,
            PartialCount = 3,
            SettledCount = 10,
            TotalAmount = 500000.00m,
            PaidAmount = 200000.00m,
            RemainingAmount = 300000.00m,
            OverdueAmount = 50000.00m
        };

        _payableServiceMock
            .Setup(x => x.GetStatisticsAsync(It.IsAny<PageRequest>()))
            .ReturnsAsync(expectedStatistics);

        // Act
        var result = await _controller.GetStatistics(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<PayableStatisticsDto>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.TotalCount.Should().Be(18);
        apiResponse.Data.PendingCount.Should().Be(5);
        apiResponse.Data.PartialCount.Should().Be(3);
        apiResponse.Data.SettledCount.Should().Be(10);
        apiResponse.Data.TotalAmount.Should().Be(500000.00m);
        apiResponse.Data.PaidAmount.Should().Be(200000.00m);
        apiResponse.Data.RemainingAmount.Should().Be(300000.00m);
        apiResponse.Data.OverdueAmount.Should().Be(50000.00m);

        _payableServiceMock.Verify(x => x.GetStatisticsAsync(It.IsAny<PageRequest>()), Times.Once);
    }

    [Fact]
    public async Task GetStatistics_NoData_ReturnsOkWithZeroValues()
    {
        // Arrange
        var request = new PageRequest { Page = 1, PageSize = 10 };
        var expectedStatistics = new PayableStatisticsDto
        {
            TotalCount = 0,
            PendingCount = 0,
            PartialCount = 0,
            SettledCount = 0,
            TotalAmount = 0m,
            PaidAmount = 0m,
            RemainingAmount = 0m,
            OverdueAmount = 0m
        };

        _payableServiceMock
            .Setup(x => x.GetStatisticsAsync(It.IsAny<PageRequest>()))
            .ReturnsAsync(expectedStatistics);

        // Act
        var result = await _controller.GetStatistics(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<PayableStatisticsDto>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.TotalCount.Should().Be(0);
        apiResponse.Data.PendingCount.Should().Be(0);
        apiResponse.Data.PartialCount.Should().Be(0);
        apiResponse.Data.SettledCount.Should().Be(0);
        apiResponse.Data.TotalAmount.Should().Be(0m);
        apiResponse.Data.PaidAmount.Should().Be(0m);
        apiResponse.Data.RemainingAmount.Should().Be(0m);
        apiResponse.Data.OverdueAmount.Should().Be(0m);

        _payableServiceMock.Verify(x => x.GetStatisticsAsync(It.IsAny<PageRequest>()), Times.Once);
    }

    [Fact]
    public async Task GetStatistics_ServiceThrowsException_RethrowsException()
    {
        // Arrange
        var request = new PageRequest { Page = 1, PageSize = 10 };
        _payableServiceMock
            .Setup(x => x.GetStatisticsAsync(It.IsAny<PageRequest>()))
            .ThrowsAsync(new InvalidOperationException("获取统计信息失败"));

        // Act
        Func<Task> act = async () => await _controller.GetStatistics(request);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("获取统计信息失败");
    }

    #endregion
}
