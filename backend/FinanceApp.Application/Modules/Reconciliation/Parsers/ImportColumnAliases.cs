namespace FinanceApp.Application.Modules.Reconciliation.Parsers;

/// <summary>
/// 银行流水导入可选列的表头别名。列不存在时调用方应保持原字段为空，不影响既有导入。
/// </summary>
public static class ImportColumnAliases
{
    public static readonly string[] VoucherNo =
    [
        "凭证号",
        "凭证号码",
        "voucher_no",
        "voucherNo"
    ];

    public static int? FindColumn(ExcelHeaderLookup headers, params string[] aliases)
    {
        foreach (var alias in aliases)
        {
            if (headers.TryGetColumn(alias, out var column))
            {
                return column;
            }
        }

        return null;
    }
}

/// <summary>
/// 表头文本到列号（1-based）的大小写不敏感查找。
/// </summary>
public sealed class ExcelHeaderLookup
{
    private readonly Dictionary<string, int> _columns;

    private ExcelHeaderLookup(Dictionary<string, int> columns)
    {
        _columns = columns;
    }

    public static ExcelHeaderLookup FromRow(Func<int, string?> readCell, int columnCount)
    {
        var columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var col = 1; col <= columnCount; col++)
        {
            var header = readCell(col)?.Trim();
            if (string.IsNullOrEmpty(header) || columns.ContainsKey(header))
            {
                continue;
            }

            columns[header] = col;
        }

        return new ExcelHeaderLookup(columns);
    }

    public bool TryGetColumn(string header, out int column) => _columns.TryGetValue(header, out column);
}
