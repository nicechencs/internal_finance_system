using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using FinanceApp.Api.Controllers.Reconciliation;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.Reconciliation.DTOs;
using FinanceApp.Application.Modules.Reconciliation.Interfaces;

namespace FinanceApp.Api.Tests.Controllers;

public class ImportControllerTests
{
    private readonly Mock<IImportService> _importServiceMock;
    private readonly ImportController _controller;

    public ImportControllerTests()
    {
        _importServiceMock = new Mock<IImportService>();
        _controller = new ImportController(_importServiceMock.Object, new Mock<ILogger<ImportController>>().Object);

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "1") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    [Fact]
    public async Task Preview_ValidExcelFile_ReturnsOkWithPreview()
    {
        // Arrange
        var accountId = 1L;
        var fileName = "test.xlsx";
        var fileContent = new byte[] { 0x50, 0x4B, 0x03, 0x04 }; // Mock Excel file header

        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns(fileName);
        fileMock.Setup(f => f.Length).Returns(fileContent.Length);
        fileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(fileContent));

        var expectedPreview = new ImportPreviewResponse
        {
            BatchId = 1,
            FileName = fileName,
            TotalRows = 10,
            NewRows = 8,
            DuplicateRows = 2
        };

        _importServiceMock
            .Setup(x => x.PreviewAsync(It.IsAny<Stream>(), fileName, accountId))
            .ReturnsAsync(expectedPreview);

        // Act
        var result = await _controller.Preview(fileMock.Object, accountId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<ImportPreviewResponse>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.BatchId.Should().Be(1);
        apiResponse.Data.TotalRows.Should().Be(10);

        _importServiceMock.Verify(x => x.PreviewAsync(It.IsAny<Stream>(), fileName, accountId), Times.Once);
    }

    [Fact]
    public async Task Preview_NullFile_ReturnsBadRequest()
    {
        // Arrange
        var accountId = 1L;

        // Act
        var result = await _controller.Preview(null!, accountId);

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(400);

        var apiResponse = badRequestResult.Value.Should().BeOfType<ApiResponse<ImportPreviewResponse>>().Subject;
        apiResponse.Success.Should().BeFalse();
        apiResponse.Message.Should().Be("请上传文件");

        _importServiceMock.Verify(x => x.PreviewAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<long>()), Times.Never);
    }

    [Fact]
    public async Task Preview_EmptyFile_ReturnsBadRequest()
    {
        // Arrange
        var accountId = 1L;
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(0);

        // Act
        var result = await _controller.Preview(fileMock.Object, accountId);

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(400);

        var apiResponse = badRequestResult.Value.Should().BeOfType<ApiResponse<ImportPreviewResponse>>().Subject;
        apiResponse.Success.Should().BeFalse();
        apiResponse.Message.Should().Be("请上传文件");

        _importServiceMock.Verify(x => x.PreviewAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<long>()), Times.Never);
    }

    [Fact]
    public async Task Preview_InvalidFileExtension_ReturnsBadRequest()
    {
        // Arrange
        var accountId = 1L;
        var fileName = "test.txt";

        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns(fileName);
        fileMock.Setup(f => f.Length).Returns(100);

        // Act
        var result = await _controller.Preview(fileMock.Object, accountId);

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(400);

        var apiResponse = badRequestResult.Value.Should().BeOfType<ApiResponse<ImportPreviewResponse>>().Subject;
        apiResponse.Success.Should().BeFalse();
        apiResponse.Message.Should().Contain("仅支持 .xlsx 格式");

        _importServiceMock.Verify(x => x.PreviewAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<long>()), Times.Never);
    }

    [Fact]
    public async Task Preview_FileTooLarge_ShouldPassToService()
    {
        // Arrange
        var accountId = 1L;
        var fileName = "test.xlsx";
        var fileSizeInBytes = 11 * 1024 * 1024; // 11MB
        var fileContent = new byte[100];

        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns(fileName);
        fileMock.Setup(f => f.Length).Returns(fileSizeInBytes);
        fileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(fileContent));

        var expectedPreview = new ImportPreviewResponse
        {
            BatchId = 1,
            FileName = fileName,
            TotalRows = 0
        };

        _importServiceMock
            .Setup(x => x.PreviewAsync(It.IsAny<Stream>(), fileName, accountId))
            .ReturnsAsync(expectedPreview);

        // Act
        var result = await _controller.Preview(fileMock.Object, accountId);

        // Assert - controller passes large files to service (no file size check in controller)
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        _importServiceMock.Verify(x => x.PreviewAsync(It.IsAny<Stream>(), fileName, accountId), Times.Once);
    }

    [Fact]
    public async Task Confirm_ValidRequest_ReturnsOkWithBatch()
    {
        // Arrange
        var request = new ImportConfirmRequest
        {
            BatchId = 1,
            SelectedRowNumbers = new List<int> { 1, 2, 3 }
        };

        var expectedBatch = new ImportBatchDto
        {
            Id = request.BatchId,
            FileName = "test.xlsx",
            TotalCount = 10,
            SuccessCount = 3,
            Status = "completed"
        };

        _importServiceMock
            .Setup(x => x.ConfirmAsync(It.IsAny<ImportConfirmRequest>()))
            .ReturnsAsync(expectedBatch);

        // Act
        var result = await _controller.Confirm(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<ImportBatchDto>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Id.Should().Be(1);
        apiResponse.Message.Should().Be("导入成功");

        _importServiceMock.Verify(x => x.ConfirmAsync(It.IsAny<ImportConfirmRequest>()), Times.Once);
    }

    [Fact]
    public async Task Confirm_InvalidBatchId_ShouldPassToService()
    {
        // Arrange - controller does not validate BatchId, passes to service
        var request = new ImportConfirmRequest
        {
            BatchId = 0,
            SelectedRowNumbers = new List<int> { 1, 2, 3 }
        };

        var expectedBatch = new ImportBatchDto
        {
            Id = 0,
            FileName = "test.xlsx",
            Status = "completed"
        };

        _importServiceMock
            .Setup(x => x.ConfirmAsync(It.IsAny<ImportConfirmRequest>()))
            .ReturnsAsync(expectedBatch);

        // Act
        var result = await _controller.Confirm(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        _importServiceMock.Verify(x => x.ConfirmAsync(It.IsAny<ImportConfirmRequest>()), Times.Once);
    }

    [Fact]
    public async Task Confirm_EmptySelectedRows_ShouldPassToService()
    {
        // Arrange - controller does not validate empty rows, passes to service
        var request = new ImportConfirmRequest
        {
            BatchId = 1,
            SelectedRowNumbers = new List<int>()
        };

        var expectedBatch = new ImportBatchDto
        {
            Id = 1,
            FileName = "test.xlsx",
            Status = "completed"
        };

        _importServiceMock
            .Setup(x => x.ConfirmAsync(It.IsAny<ImportConfirmRequest>()))
            .ReturnsAsync(expectedBatch);

        // Act
        var result = await _controller.Confirm(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        _importServiceMock.Verify(x => x.ConfirmAsync(It.IsAny<ImportConfirmRequest>()), Times.Once);
    }

    [Fact]
    public async Task GetBatches_ValidRequest_ReturnsOkWithPagedData()
    {
        // Arrange
        var request = new ImportBatchQueryRequest { Page = 1, PageSize = 10 };
        var expectedResponse = new PageResponse<ImportBatchDto>
        {
            Items = new List<ImportBatchDto>
            {
                new ImportBatchDto { Id = 1, FileName = "batch1.xlsx", Status = "completed" },
                new ImportBatchDto { Id = 2, FileName = "batch2.xlsx", Status = "pending" }
            },
            Total = 2,
            Page = 1,
            PageSize = 10
        };

        _importServiceMock
            .Setup(x => x.GetBatchesAsync(It.IsAny<ImportBatchQueryRequest>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetBatches(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<PageResponse<ImportBatchDto>>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Items.Should().HaveCount(2);
        apiResponse.Data.Total.Should().Be(2);

        _importServiceMock.Verify(x => x.GetBatchesAsync(It.IsAny<ImportBatchQueryRequest>()), Times.Once);
    }

    [Fact]
    public async Task GetBatch_ExistingId_ReturnsOkWithBatch()
    {
        // Arrange
        var batchId = 1L;
        var expectedBatch = new ImportBatchDto
        {
            Id = batchId,
            FileName = "test.xlsx",
            TotalCount = 10,
            SuccessCount = 8,
            Status = "completed"
        };

        _importServiceMock
            .Setup(x => x.GetBatchByIdAsync(batchId))
            .ReturnsAsync(expectedBatch);

        // Act
        var result = await _controller.GetBatch(batchId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<ImportBatchDto>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Id.Should().Be(batchId);
        apiResponse.Data.FileName.Should().Be("test.xlsx");

        _importServiceMock.Verify(x => x.GetBatchByIdAsync(batchId), Times.Once);
    }

    #region 异常传播测试

    [Fact]
    public async Task Preview_ServiceThrowsNotFoundException_PropagatesException()
    {
        // Arrange
        var accountId = 999L;
        var fileName = "test.xlsx";
        var fileContent = new byte[] { 0x50, 0x4B, 0x03, 0x04 };

        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns(fileName);
        fileMock.Setup(f => f.Length).Returns(fileContent.Length);
        fileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(fileContent));

        _importServiceMock
            .Setup(x => x.PreviewAsync(It.IsAny<Stream>(), fileName, accountId))
            .ThrowsAsync(new FinanceApp.Application.Common.NotFoundException("账户不存在"));

        // Act & Assert
        await Assert.ThrowsAsync<FinanceApp.Application.Common.NotFoundException>(
            () => _controller.Preview(fileMock.Object, accountId));
    }

    [Fact]
    public async Task Preview_ServiceThrowsValidationException_PropagatesException()
    {
        // Arrange
        var accountId = 1L;
        var fileName = "test.xlsx";
        var fileContent = new byte[] { 0x50, 0x4B, 0x03, 0x04 };

        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns(fileName);
        fileMock.Setup(f => f.Length).Returns(fileContent.Length);
        fileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(fileContent));

        _importServiceMock
            .Setup(x => x.PreviewAsync(It.IsAny<Stream>(), fileName, accountId))
            .ThrowsAsync(new FinanceApp.Application.Common.ValidationException("文件中没有可解析的有效数据行"));

        // Act & Assert
        await Assert.ThrowsAsync<FinanceApp.Application.Common.ValidationException>(
            () => _controller.Preview(fileMock.Object, accountId));
    }

    [Fact]
    public async Task Confirm_ServiceThrowsNotFoundException_PropagatesException()
    {
        // Arrange
        var request = new ImportConfirmRequest
        {
            BatchId = 999,
            SelectedRowNumbers = new List<int> { 1, 2, 3 }
        };

        _importServiceMock
            .Setup(x => x.ConfirmAsync(It.IsAny<ImportConfirmRequest>()))
            .ThrowsAsync(new FinanceApp.Application.Common.NotFoundException("导入批次不存在"));

        // Act & Assert
        await Assert.ThrowsAsync<FinanceApp.Application.Common.NotFoundException>(
            () => _controller.Confirm(request));
    }

    [Fact]
    public async Task Confirm_ServiceThrowsValidationException_PropagatesException()
    {
        // Arrange
        var request = new ImportConfirmRequest
        {
            BatchId = 1,
            SelectedRowNumbers = new List<int> { 1, 2, 3 }
        };

        _importServiceMock
            .Setup(x => x.ConfirmAsync(It.IsAny<ImportConfirmRequest>()))
            .ThrowsAsync(new FinanceApp.Application.Common.ValidationException("预览数据已过期，请重新上传文件"));

        // Act & Assert
        await Assert.ThrowsAsync<FinanceApp.Application.Common.ValidationException>(
            () => _controller.Confirm(request));
    }

    [Fact]
    public async Task GetBatch_ServiceThrowsNotFoundException_PropagatesException()
    {
        // Arrange
        var batchId = 999L;

        _importServiceMock
            .Setup(x => x.GetBatchByIdAsync(batchId))
            .ThrowsAsync(new FinanceApp.Application.Common.NotFoundException("导入批次不存在"));

        // Act & Assert
        await Assert.ThrowsAsync<FinanceApp.Application.Common.NotFoundException>(
            () => _controller.GetBatch(batchId));
    }

    #endregion

    #region 文件格式验证测试

    [Theory]
    [InlineData("test.xlsx")]
    public async Task Preview_AllowedFileExtensions_CallsService(string fileName)
    {
        // Arrange
        var accountId = 1L;
        var fileContent = new byte[] { 0x50, 0x4B, 0x03, 0x04 };

        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns(fileName);
        fileMock.Setup(f => f.Length).Returns(fileContent.Length);
        fileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(fileContent));

        _importServiceMock
            .Setup(x => x.PreviewAsync(It.IsAny<Stream>(), fileName, accountId))
            .ReturnsAsync(new ImportPreviewResponse { BatchId = 1, FileName = fileName });

        // Act
        var result = await _controller.Preview(fileMock.Object, accountId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        _importServiceMock.Verify(x => x.PreviewAsync(It.IsAny<Stream>(), fileName, accountId), Times.Once);
    }

    [Theory]
    [InlineData("test.txt")]
    [InlineData("test.csv")]
    [InlineData("test.pdf")]
    [InlineData("test.doc")]
    [InlineData("test.xls")]
    [InlineData("test.xml")]
    public async Task Preview_DisallowedFileExtensions_ReturnsBadRequest(string fileName)
    {
        // Arrange
        var accountId = 1L;
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns(fileName);
        fileMock.Setup(f => f.Length).Returns(100);

        // Act
        var result = await _controller.Preview(fileMock.Object, accountId);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        _importServiceMock.Verify(x => x.PreviewAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<long>()), Times.Never);
    }

    #endregion

    #region 批次查询参数测试

    [Fact]
    public async Task GetBatches_WithFilters_PassesFiltersToService()
    {
        // Arrange
        var request = new ImportBatchQueryRequest
        {
            Page = 2,
            PageSize = 20,
            AccountId = 5,
            Status = "Completed",
            FileName = "bank"
        };

        var expectedResponse = new PageResponse<ImportBatchDto>
        {
            Items = new List<ImportBatchDto>(),
            Total = 0,
            Page = 2,
            PageSize = 20
        };

        _importServiceMock
            .Setup(x => x.GetBatchesAsync(It.Is<ImportBatchQueryRequest>(r =>
                r.Page == 2 &&
                r.PageSize == 20 &&
                r.AccountId == 5 &&
                r.Status == "Completed" &&
                r.FileName == "bank")))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetBatches(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<PageResponse<ImportBatchDto>>>().Subject;
        apiResponse.Data!.Page.Should().Be(2);
        apiResponse.Data.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task GetBatches_EmptyResult_ReturnsEmptyList()
    {
        // Arrange
        var request = new ImportBatchQueryRequest { Page = 1, PageSize = 10 };
        var expectedResponse = new PageResponse<ImportBatchDto>
        {
            Items = new List<ImportBatchDto>(),
            Total = 0,
            Page = 1,
            PageSize = 10
        };

        _importServiceMock
            .Setup(x => x.GetBatchesAsync(It.IsAny<ImportBatchQueryRequest>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetBatches(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<PageResponse<ImportBatchDto>>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data!.Items.Should().BeEmpty();
        apiResponse.Data.Total.Should().Be(0);
    }

    #endregion
}
