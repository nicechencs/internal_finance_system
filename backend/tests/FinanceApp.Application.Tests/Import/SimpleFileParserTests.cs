using FluentAssertions;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.Reconciliation.Parsers;
using Microsoft.Extensions.Logging;
using Moq;
using OfficeOpenXml;

namespace FinanceApp.Application.Tests.Import;

public class SimpleFileParserTests
{
    private readonly SimpleFileParser _parser;

    public SimpleFileParserTests()
    {
        _parser = new SimpleFileParser(new Mock<ILogger>().Object);
    }

    [Fact]
    public void Parse_ValidData_ReturnsCorrectRows()
    {
        // Arrange
        var stream = CreateSimpleExcel(ws =>
        {
            ws.Cells[1, 1].Value = "日期";
            ws.Cells[1, 2].Value = "金额";
            ws.Cells[1, 3].Value = "对方";
            ws.Cells[1, 4].Value = "摘要";
            ws.Cells[1, 5].Value = "对方账号";

            ws.Cells[2, 1].Value = "2025-07-01";
            ws.Cells[2, 2].Value = 5000.00;
            ws.Cells[2, 3].Value = "客户A";
            ws.Cells[2, 4].Value = "项目收入";
            ws.Cells[2, 5].Value = "6222021234567890";

            ws.Cells[3, 1].Value = "2025-07-02";
            ws.Cells[3, 2].Value = -1200.50;
            ws.Cells[3, 3].Value = "供应商B";
            ws.Cells[3, 4].Value = "采购办公用品";
        });

        // Act
        var rows = _parser.Parse(stream);

        // Assert
        rows.Should().HaveCount(2);

        rows[0].TransactionDate.Should().Be(new DateTime(2025, 7, 1));
        rows[0].Amount.Should().Be(5000.00m);
        rows[0].Direction.Should().Be("in");
        rows[0].Counterparty.Should().Be("客户A");
        rows[0].Description.Should().Be("项目收入");
        rows[0].CounterpartyAccount.Should().Be("6222021234567890");

        rows[1].Amount.Should().Be(1200.50m);
        rows[1].Direction.Should().Be("out");
        rows[1].CounterpartyAccount.Should().BeNull();
    }

    [Fact]
    public void Parse_EmptyWorksheet_ThrowsValidationException()
    {
        // Arrange
        var stream = CreateSimpleExcel(_ => { }); // 空工作表

        // Act & Assert
        var act = () => _parser.Parse(stream);
        act.Should().Throw<ValidationException>().WithMessage("*为空*");
    }

    [Fact]
    public void Parse_OnlyHeaderRow_ThrowsValidationException()
    {
        // Arrange
        var stream = CreateSimpleExcel(ws =>
        {
            ws.Cells[1, 1].Value = "日期";
            ws.Cells[1, 2].Value = "金额";
            ws.Cells[1, 3].Value = "对方";
            ws.Cells[1, 4].Value = "摘要";
        });

        // Act & Assert
        var act = () => _parser.Parse(stream);
        act.Should().Throw<ValidationException>().WithMessage("*没有数据行*");
    }

    [Fact]
    public void Parse_SkipsEmptyRows()
    {
        // Arrange
        var stream = CreateSimpleExcel(ws =>
        {
            ws.Cells[1, 1].Value = "日期";
            ws.Cells[2, 1].Value = "2025-07-01";
            ws.Cells[2, 2].Value = 100.00;
            ws.Cells[2, 3].Value = "对方A";
            ws.Cells[2, 4].Value = "摘要A";
            // 第3行空行
            ws.Cells[4, 1].Value = "2025-07-02";
            ws.Cells[4, 2].Value = 200.00;
            ws.Cells[4, 3].Value = "对方B";
            ws.Cells[4, 4].Value = "摘要B";
        });

        // Act
        var rows = _parser.Parse(stream);

        // Assert
        rows.Should().HaveCount(2);
    }

    [Fact]
    public void Parse_InvalidDateRow_SkipsRow()
    {
        // Arrange
        var stream = CreateSimpleExcel(ws =>
        {
            ws.Cells[1, 1].Value = "日期";
            ws.Cells[2, 1].Value = "not-a-date";
            ws.Cells[2, 2].Value = 100.00;
            ws.Cells[2, 3].Value = "对方";
            ws.Cells[2, 4].Value = "摘要";

            ws.Cells[3, 1].Value = "2025-07-01";
            ws.Cells[3, 2].Value = 200.00;
            ws.Cells[3, 3].Value = "对方B";
            ws.Cells[3, 4].Value = "摘要B";
        });

        // Act
        var rows = _parser.Parse(stream);

        // Assert
        rows.Should().HaveCount(1);
        rows[0].Amount.Should().Be(200.00m);
    }

    [Fact]
    public void Parse_PositiveAmount_DirectionIsIn()
    {
        // Arrange
        var stream = CreateSimpleExcel(ws =>
        {
            ws.Cells[1, 1].Value = "日期";
            ws.Cells[2, 1].Value = "2025-07-01";
            ws.Cells[2, 2].Value = 5000.00;
            ws.Cells[2, 3].Value = "客户";
            ws.Cells[2, 4].Value = "收入";
        });

        // Act
        var rows = _parser.Parse(stream);

        // Assert
        rows[0].Direction.Should().Be("in");
        rows[0].Amount.Should().Be(5000.00m);
    }

    [Fact]
    public void Parse_NegativeAmount_DirectionIsOut()
    {
        // Arrange
        var stream = CreateSimpleExcel(ws =>
        {
            ws.Cells[1, 1].Value = "日期";
            ws.Cells[2, 1].Value = "2025-07-01";
            ws.Cells[2, 2].Value = -3000.00;
            ws.Cells[2, 3].Value = "供应商";
            ws.Cells[2, 4].Value = "支出";
        });

        // Act
        var rows = _parser.Parse(stream);

        // Assert
        rows[0].Direction.Should().Be("out");
        rows[0].Amount.Should().Be(3000.00m);
    }

    [Fact]
    public void Parse_DateTimeValue_ParsesCorrectly()
    {
        // Arrange
        var stream = CreateSimpleExcel(ws =>
        {
            ws.Cells[1, 1].Value = "日期";
            ws.Cells[2, 1].Value = new DateTime(2025, 7, 15);
            ws.Cells[2, 2].Value = 1000.00;
            ws.Cells[2, 3].Value = "对方";
            ws.Cells[2, 4].Value = "摘要";
        });

        // Act
        var rows = _parser.Parse(stream);

        // Assert
        rows[0].TransactionDate.Should().Be(new DateTime(2025, 7, 15));
    }

    private static MemoryStream CreateSimpleExcel(Action<ExcelWorksheet> configure)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        var stream = new MemoryStream();
        using (var package = new ExcelPackage(stream))
        {
            var ws = package.Workbook.Worksheets.Add("Sheet1");
            configure(ws);
            package.Save();
        }
        stream.Seek(0, SeekOrigin.Begin);
        return stream;
    }
}
