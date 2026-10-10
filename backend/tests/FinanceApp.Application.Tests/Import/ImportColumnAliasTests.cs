using FluentAssertions;
using FinanceApp.Application.Modules.Reconciliation.Parsers;

namespace FinanceApp.Application.Tests.Import;

public class ImportColumnAliasTests
{
    [Theory]
    [InlineData("凭证号")]
    [InlineData("凭证号码")]
    [InlineData("voucher_no")]
    [InlineData("voucherNo")]
    [InlineData("VOUCHERNO")]
    public void FindColumn_AcceptsKnownAliases(string header)
    {
        var lookup = ExcelHeaderLookup.FromRow(col => col switch
        {
            1 => "日期",
            2 => header,
            _ => null
        }, 3);

        ImportColumnAliases.FindColumn(lookup, ImportColumnAliases.VoucherNo).Should().Be(2);
    }

    [Fact]
    public void FindColumn_MissingHeader_ReturnsNull()
    {
        var lookup = ExcelHeaderLookup.FromRow(col => col == 1 ? "日期" : "金额", 2);

        ImportColumnAliases.FindColumn(lookup, ImportColumnAliases.VoucherNo).Should().BeNull();
    }
}
