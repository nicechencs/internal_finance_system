using FluentAssertions;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.Reconciliation.Parsers;
using Microsoft.Extensions.Logging;
using Moq;
using OfficeOpenXml;

namespace FinanceApp.Application.Tests.Import;

public class HuaxiaBankParserTests
{
    private readonly HuaxiaBankParser _parser;

    public HuaxiaBankParserTests()
    {
        _parser = new HuaxiaBankParser(new Mock<ILogger>().Object);
    }

    [Fact]
    public void Parse_ValidData_ReturnsCorrectRows()
    {
        // Arrange
        var stream = CreateHuaxiaExcel(ws =>
        {
            // 元数据行 1-7
            ws.Cells[1, 1].Value = "账号：";
            ws.Cells[1, 2].Value = "123456789";
            ws.Cells[2, 1].Value = "币种：";
            ws.Cells[2, 2].Value = "人民币";
            ws.Cells[3, 1].Value = "查询日期：";
            ws.Cells[4, 1].Value = "交易方向：";
            ws.Cells[5, 1].Value = "排序方向：";
            ws.Cells[6, 1].Value = "收入总金额：";
            ws.Cells[7, 1].Value = "支出总金额：";

            // 表头行 8
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

            // 数据行 9 - 收入
            ws.Cells[9, 1].Value = "1";
            ws.Cells[9, 2].Value = "2025-07-01";
            ws.Cells[9, 3].Value = "10:30:00";
            ws.Cells[9, 4].Value = "";
            ws.Cells[9, 5].Value = "5000.00";
            ws.Cells[9, 6].Value = "15000.00";
            ws.Cells[9, 7].Value = "6222021234567890";
            ws.Cells[9, 8].Value = "张三公司";
            ws.Cells[9, 9].Value = "工商银行北京支行";
            ws.Cells[9, 10].Value = "HX20250701001";
            ws.Cells[9, 11].Value = "手机银行转账";
            ws.Cells[9, 12].Value = "项目款";

            // 数据行 10 - 支出
            ws.Cells[10, 1].Value = "2";
            ws.Cells[10, 2].Value = "2025-07-02";
            ws.Cells[10, 3].Value = "14:20:30";
            ws.Cells[10, 4].Value = "1200.50";
            ws.Cells[10, 5].Value = "";
            ws.Cells[10, 6].Value = "13799.50";
            ws.Cells[10, 7].Value = "6228481234567890";
            ws.Cells[10, 8].Value = "李四供应商";
            ws.Cells[10, 9].Value = "";
            ws.Cells[10, 10].Value = "HX20250702001";
            ws.Cells[10, 11].Value = "网银转账";
            ws.Cells[10, 12].Value = "";
        });

        // Act
        var rows = _parser.Parse(stream);

        // Assert
        rows.Should().HaveCount(2);

        // 收入行
        var income = rows[0];
        income.TransactionDate.Should().Be(new DateTime(2025, 7, 1));
        income.TransactionTime.Should().Be(new TimeSpan(10, 30, 0));
        income.Amount.Should().Be(5000.00m);
        income.Direction.Should().Be("in");
        income.Balance.Should().Be(15000.00m);
        income.CounterpartyAccount.Should().Be("6222021234567890");
        income.Counterparty.Should().Be("张三公司");
        income.CounterpartyBank.Should().Be("工商银行北京支行");
        income.TransactionNumber.Should().Be("HX20250701001");
        income.Description.Should().Be("手机银行转账");
        income.ExtendedInfo.Should().Be("项目款");

        // 支出行
        var expense = rows[1];
        expense.Amount.Should().Be(1200.50m);
        expense.Direction.Should().Be("out");
        expense.Balance.Should().Be(13799.50m);
        expense.CounterpartyBank.Should().BeNull();
        expense.Description.Should().Be("网银转账");
    }

    [Fact]
    public void Parse_NoHeaderRow_ThrowsValidationException()
    {
        // Arrange - 没有"序号"+"交易日期"的表头
        var stream = CreateHuaxiaExcel(ws =>
        {
            ws.Cells[1, 1].Value = "列A";
            ws.Cells[1, 2].Value = "列B";
            ws.Cells[2, 1].Value = "data1";
            ws.Cells[2, 2].Value = "data2";
        });

        // Act & Assert
        var act = () => _parser.Parse(stream);
        act.Should().Throw<ValidationException>().WithMessage("*无法识别华夏银行文件格式*");
    }

    [Fact]
    public void Parse_IncomeAmount_DirectionIsIn()
    {
        // Arrange
        var stream = CreateHuaxiaExcel(ws =>
        {
            WriteHuaxiaHeader(ws);
            ws.Cells[9, 1].Value = "1";
            ws.Cells[9, 2].Value = "2025-07-01";
            ws.Cells[9, 4].Value = "";
            ws.Cells[9, 5].Value = "3000.00";
            ws.Cells[9, 8].Value = "对方";
        });

        // Act
        var rows = _parser.Parse(stream);

        // Assert
        rows.Should().HaveCount(1);
        rows[0].Direction.Should().Be("in");
        rows[0].Amount.Should().Be(3000.00m);
    }

    [Fact]
    public void Parse_ExpenseAmount_DirectionIsOut()
    {
        // Arrange
        var stream = CreateHuaxiaExcel(ws =>
        {
            WriteHuaxiaHeader(ws);
            ws.Cells[9, 1].Value = "1";
            ws.Cells[9, 2].Value = "2025-07-01";
            ws.Cells[9, 4].Value = "2500.00";
            ws.Cells[9, 5].Value = "";
            ws.Cells[9, 8].Value = "供应商";
        });

        // Act
        var rows = _parser.Parse(stream);

        // Assert
        rows[0].Direction.Should().Be("out");
        rows[0].Amount.Should().Be(2500.00m);
    }

    [Fact]
    public void Parse_TransactionTime_ParsedCorrectly()
    {
        // Arrange
        var stream = CreateHuaxiaExcel(ws =>
        {
            WriteHuaxiaHeader(ws);
            ws.Cells[9, 1].Value = "1";
            ws.Cells[9, 2].Value = "2025-07-01";
            ws.Cells[9, 3].Value = "09:15:30";
            ws.Cells[9, 5].Value = "100.00";
            ws.Cells[9, 8].Value = "对方";
        });

        // Act
        var rows = _parser.Parse(stream);

        // Assert
        rows[0].TransactionTime.Should().Be(new TimeSpan(9, 15, 30));
    }

    [Fact]
    public void Parse_DescriptionAndMemo_SeparatedCorrectly()
    {
        // Arrange
        var stream = CreateHuaxiaExcel(ws =>
        {
            WriteHuaxiaHeader(ws);
            ws.Cells[9, 1].Value = "1";
            ws.Cells[9, 2].Value = "2025-07-01";
            ws.Cells[9, 5].Value = "100.00";
            ws.Cells[9, 8].Value = "对方";
            ws.Cells[9, 11].Value = "手机银行转账";
            ws.Cells[9, 12].Value = "报销款";
        });

        // Act
        var rows = _parser.Parse(stream);

        // Assert - 交易描述和摘要应分别保存
        rows[0].Description.Should().Be("手机银行转账");
        rows[0].ExtendedInfo.Should().Be("报销款");
    }

    [Fact]
    public void Parse_SkipsEmptyDataRows()
    {
        // Arrange
        var stream = CreateHuaxiaExcel(ws =>
        {
            WriteHuaxiaHeader(ws);
            // 第9行有数据
            ws.Cells[9, 1].Value = "1";
            ws.Cells[9, 2].Value = "2025-07-01";
            ws.Cells[9, 5].Value = "100.00";
            ws.Cells[9, 8].Value = "对方A";
            // 第10行空行（交易日期为空）
            ws.Cells[10, 1].Value = "";
            // 第11行有数据
            ws.Cells[11, 1].Value = "2";
            ws.Cells[11, 2].Value = "2025-07-02";
            ws.Cells[11, 5].Value = "200.00";
            ws.Cells[11, 8].Value = "对方B";
        });

        // Act
        var rows = _parser.Parse(stream);

        // Assert
        rows.Should().HaveCount(2);
    }

    [Fact]
    public void Parse_Balance_ParsedCorrectly()
    {
        // Arrange
        var stream = CreateHuaxiaExcel(ws =>
        {
            WriteHuaxiaHeader(ws);
            ws.Cells[9, 1].Value = "1";
            ws.Cells[9, 2].Value = "2025-07-01";
            ws.Cells[9, 5].Value = "5000.00";
            ws.Cells[9, 6].Value = "25000.50";
            ws.Cells[9, 8].Value = "对方";
        });

        // Act
        var rows = _parser.Parse(stream);

        // Assert
        rows[0].Balance.Should().Be(25000.50m);
    }

    private static void WriteHuaxiaHeader(ExcelWorksheet ws)
    {
        ws.Cells[1, 1].Value = "账号：";
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
    }

    private static MemoryStream CreateHuaxiaExcel(Action<ExcelWorksheet> configure)
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
