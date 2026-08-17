using System.Text;
using FluentAssertions;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.Reconciliation.Parsers;
using Microsoft.Extensions.Logging;
using Moq;

namespace FinanceApp.Application.Tests.Import;

public class AlipayBusinessParserTests
{
    private readonly AlipayBusinessParser _parser;

    public AlipayBusinessParserTests()
    {
        _parser = new AlipayBusinessParser(new Mock<ILogger>().Object);
    }

    [Fact]
    public void Parse_ValidData_ReturnsCorrectRows()
    {
        // Arrange
        var xml = BuildAlipayXml(new[]
        {
            new AlipayTestRow
            {
                InvoiceStatus = "未开票",
                Seq = "1",
                DateTime = "2025-04-14 11:01:46",
                TradeNo = "DEMO2025041400000001",
                AccountType = "在线支付",
                Income = "200.00",
                Expense = " ",
                Balance = "200.00",
                ServiceFee = "0.00",
                PayChannel = "快捷支付-借记卡",
                CounterpartyAccount = "buyer***@example.com",
                CounterpartyName = "示例买家",
                ProductName = "测试商品",
                BizDesc = "交易收款",
                PaymentMemo = " "
            }
        });
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

        // Act
        var rows = _parser.Parse(stream);

        // Assert
        rows.Should().HaveCount(1);
        var row = rows[0];
        row.TransactionDate.Should().Be(new DateTime(2025, 4, 14));
        row.TransactionTime.Should().Be(new TimeSpan(11, 1, 46));
        row.Amount.Should().Be(200.00m);
        row.Direction.Should().Be("in");
        row.Balance.Should().Be(200.00m);
        row.TransactionNumber.Should().Be("DEMO2025041400000001");
        row.CounterpartyAccount.Should().Be("buyer***@example.com");
        row.Counterparty.Should().Be("示例买家");
        row.Description.Should().Be("在线支付");
    }

    [Fact]
    public void Parse_ExpenseRow_DirectionIsOut()
    {
        // Arrange
        var xml = BuildAlipayXml(new[]
        {
            new AlipayTestRow
            {
                Seq = "1",
                DateTime = "2025-04-15 09:00:00",
                TradeNo = "ZFB20250415001",
                AccountType = "转账",
                Income = " ",
                Expense = "500.00",
                Balance = "1500.00",
                CounterpartyName = "供应商X"
            }
        });
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

        // Act
        var rows = _parser.Parse(stream);

        // Assert
        rows[0].Direction.Should().Be("out");
        rows[0].Amount.Should().Be(500.00m);
    }

    [Fact]
    public void Parse_ExtendedInfo_ContainsExpectedFields()
    {
        // Arrange
        var xml = BuildAlipayXml(new[]
        {
            new AlipayTestRow
            {
                InvoiceStatus = "未开票",
                Seq = "1",
                DateTime = "2025-04-14 11:00:00",
                TradeNo = "T001",
                AccountType = "在线支付",
                Income = "100.00",
                Expense = " ",
                Balance = "100.00",
                ServiceFee = "1.50",
                PayChannel = "快捷支付",
                CounterpartyName = "客户",
                ProductName = "商品名称ABC",
                BizDesc = "业务描述XYZ",
                PaymentMemo = "备注信息"
            }
        });
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

        // Act
        var rows = _parser.Parse(stream);

        // Assert
        rows[0].ExtendedInfo.Should().NotBeNullOrEmpty();
        rows[0].ExtendedInfo.Should().Contain("\"serviceFee\":\"1.50\"");
        rows[0].ExtendedInfo.Should().Contain("\"payChannel\":");
        rows[0].ExtendedInfo.Should().Contain("\"productName\":\"商品名称ABC\"");
        rows[0].ExtendedInfo.Should().Contain("\"bizDesc\":\"业务描述XYZ\"");
        rows[0].ExtendedInfo.Should().Contain("\"invoiceStatus\":\"未开票\"");
        rows[0].ExtendedInfo.Should().Contain("\"paymentMemo\":\"备注信息\"");
    }

    [Fact]
    public void Parse_EmptyWorksheet_ThrowsValidationException()
    {
        // Arrange - XML with only title and header, no data rows
        var xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<?mso-application progid=""Excel.Sheet""?>
<Workbook xmlns=""urn:schemas-microsoft-com:office:spreadsheet""
 xmlns:ss=""urn:schemas-microsoft-com:office:spreadsheet"">
 <Worksheet ss:Name=""Sheet1"">
  <Table>
   <Row><Cell ss:MergeAcross=""22""><Data ss:Type=""String"">标题</Data></Cell></Row>
   <Row><Cell><Data ss:Type=""String"">表头</Data></Cell></Row>
  </Table>
 </Worksheet>
</Workbook>";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

        // Act & Assert
        var act = () => _parser.Parse(stream);
        act.Should().Throw<ValidationException>().WithMessage("*数据不足*");
    }

    [Fact]
    public void Parse_InvalidDateTime_SkipsRow()
    {
        // Arrange
        var xml = BuildAlipayXml(new[]
        {
            new AlipayTestRow
            {
                Seq = "1",
                DateTime = "invalid-date",
                TradeNo = "T001",
                Income = "100.00",
                CounterpartyName = "客户"
            },
            new AlipayTestRow
            {
                Seq = "2",
                DateTime = "2025-04-15 10:00:00",
                TradeNo = "T002",
                Income = "200.00",
                CounterpartyName = "客户B"
            }
        });
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

        // Act
        var rows = _parser.Parse(stream);

        // Assert
        rows.Should().HaveCount(1);
        rows[0].Amount.Should().Be(200.00m);
    }

    [Fact]
    public void Parse_MultipleRows_ParsesAll()
    {
        // Arrange
        var xml = BuildAlipayXml(new[]
        {
            new AlipayTestRow { Seq = "1", DateTime = "2025-04-14 11:00:00", TradeNo = "T001", Income = "100.00", CounterpartyName = "A" },
            new AlipayTestRow { Seq = "2", DateTime = "2025-04-15 12:00:00", TradeNo = "T002", Expense = "50.00", CounterpartyName = "B" },
            new AlipayTestRow { Seq = "3", DateTime = "2025-04-16 13:00:00", TradeNo = "T003", Income = "300.00", CounterpartyName = "C" },
        });
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

        // Act
        var rows = _parser.Parse(stream);

        // Assert
        rows.Should().HaveCount(3);
        rows[0].Direction.Should().Be("in");
        rows[1].Direction.Should().Be("out");
        rows[2].Direction.Should().Be("in");
    }

    #region XML Builder Helpers

    private class AlipayTestRow
    {
        public string InvoiceStatus { get; set; } = " ";
        public string Seq { get; set; } = "1";
        public string DateTime { get; set; } = "2025-01-01 00:00:00";
        public string TradeNo { get; set; } = " ";
        public string FlowNo { get; set; } = " ";
        public string OrderNo { get; set; } = " ";
        public string AccountType { get; set; } = " ";
        public string Income { get; set; } = " ";
        public string Expense { get; set; } = " ";
        public string Balance { get; set; } = " ";
        public string ServiceFee { get; set; } = " ";
        public string PayChannel { get; set; } = " ";
        public string SignProduct { get; set; } = " ";
        public string CounterpartyAccount { get; set; } = " ";
        public string CounterpartyName { get; set; } = " ";
        public string BankOrderNo { get; set; } = " ";
        public string ProductName { get; set; } = " ";
        public string Memo { get; set; } = " ";
        public string BizBaseOrderNo { get; set; } = " ";
        public string BizOrderNo { get; set; } = " ";
        public string BizBillSource { get; set; } = " ";
        public string BizDesc { get; set; } = " ";
        public string PaymentMemo { get; set; } = " ";
    }

    private static string BuildAlipayXml(AlipayTestRow[] dataRows)
    {
        var sb = new StringBuilder();
        sb.AppendLine(@"<?xml version=""1.0"" encoding=""UTF-8""?>");
        sb.AppendLine(@"<?mso-application progid=""Excel.Sheet""?>");
        sb.AppendLine(@"<Workbook xmlns=""urn:schemas-microsoft-com:office:spreadsheet""");
        sb.AppendLine(@" xmlns:ss=""urn:schemas-microsoft-com:office:spreadsheet"">");
        sb.AppendLine(@" <Worksheet ss:Name=""账务组合查询1"">");
        sb.AppendLine(@"  <Table>");

        // Row 1: Title row (merged cell)
        sb.AppendLine(@"   <Row><Cell ss:MergeAcross=""22""><Data ss:Type=""String"">企业支付宝 #账号：test@test.com</Data></Cell></Row>");

        // Row 2: Header row (23 columns)
        sb.AppendLine("   <Row>");
        var headers = new[] { "开票情况", "序号", "入账时间", "支付宝交易号", "支付宝流水号", "商户订单号", "账务类型",
            "收入（+元）", "支出（-元）", "账户余额（元）", "服务费（元）", "支付渠道", "签约产品",
            "对方账户", "对方名称", "银行订单号", "商品名称", "备注", "业务基础订单号", "业务订单号",
            "业务账单来源", "业务描述", "付款备注" };
        foreach (var h in headers)
        {
            sb.AppendLine($@"    <Cell><Data ss:Type=""String"">{h}</Data></Cell>");
        }
        sb.AppendLine("   </Row>");

        // Data rows
        foreach (var row in dataRows)
        {
            sb.AppendLine("   <Row>");
            var values = new[] { row.InvoiceStatus, row.Seq, row.DateTime, row.TradeNo, row.FlowNo,
                row.OrderNo, row.AccountType, row.Income, row.Expense, row.Balance,
                row.ServiceFee, row.PayChannel, row.SignProduct, row.CounterpartyAccount,
                row.CounterpartyName, row.BankOrderNo, row.ProductName, row.Memo,
                row.BizBaseOrderNo, row.BizOrderNo, row.BizBillSource, row.BizDesc, row.PaymentMemo };
            foreach (var v in values)
            {
                sb.AppendLine($@"    <Cell><Data ss:Type=""String"">{v}</Data></Cell>");
            }
            sb.AppendLine("   </Row>");
        }

        sb.AppendLine(@"  </Table>");
        sb.AppendLine(@" </Worksheet>");
        sb.AppendLine(@"</Workbook>");
        return sb.ToString();
    }

    #endregion
}
