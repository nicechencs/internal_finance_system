using System.Text.Encodings.Web;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;

namespace FinanceApp.Application.Modules.Reconciliation.Parsers;

public class AlipayBusinessParser : IFileParser
{
    private readonly ILogger _logger;

    // SpreadsheetML 命名空间
    private static readonly XNamespace SsNs = "urn:schemas-microsoft-com:office:spreadsheet";

    public AlipayBusinessParser(ILogger logger)
    {
        _logger = logger;
    }

    public List<ParsedBankRow> Parse(Stream stream)
    {
        var rows = new List<ParsedBankRow>();

        if (stream.CanSeek)
        {
            stream.Seek(0, SeekOrigin.Begin);
        }

        var doc = XDocument.Load(stream);
        var worksheet = doc.Descendants(SsNs + "Worksheet").FirstOrDefault();
        if (worksheet == null)
            throw new Common.ValidationException("XML 文件中没有找到工作表");

        var table = worksheet.Element(SsNs + "Table");
        if (table == null)
            throw new Common.ValidationException("工作表中没有找到数据表");

        var xmlRows = table.Elements(SsNs + "Row").ToList();
        if (xmlRows.Count < 3)
            throw new Common.ValidationException("支付宝文件数据不足：至少需要标题行、表头行和一行数据");

        // 第1行是标题行（合并单元格，账号信息），第2行是表头行，第3行起是数据
        int skippedRows = 0;
        for (int i = 2; i < xmlRows.Count; i++)
        {
            try
            {
                var cells = GetCellValues(xmlRows[i]);

                // 至少需要有入账时间列（col3，index 2）
                if (cells.Count < 3 || string.IsNullOrWhiteSpace(GetCellValue(cells, 2)))
                {
                    skippedRows++;
                    continue;
                }

                // 入账时间 (col3, index 2)
                var dateTimeStr = GetCellValue(cells, 2)?.Trim();
                if (string.IsNullOrWhiteSpace(dateTimeStr) || !DateTime.TryParse(dateTimeStr, out var dateTime))
                {
                    _logger.LogWarning("支付宝行 {Row}: 入账时间格式无效, 值={Value}", i + 1, dateTimeStr);
                    continue;
                }

                var transactionDate = dateTime.Date;
                var transactionTime = dateTime.TimeOfDay;

                // 支付宝交易号 (col4, index 3)
                var transactionNumber = GetCellValue(cells, 3)?.Trim();

                // 收入 (col8, index 7) 和 支出 (col9, index 8)
                var incomeStr = GetCellValue(cells, 7)?.Trim();
                var expenseStr = GetCellValue(cells, 8)?.Trim();

                decimal amount;
                string direction;

                if (!string.IsNullOrWhiteSpace(incomeStr) && decimal.TryParse(incomeStr, out var incomeAmount) && incomeAmount != 0)
                {
                    amount = Math.Abs(incomeAmount);
                    direction = "in";
                }
                else if (!string.IsNullOrWhiteSpace(expenseStr) && decimal.TryParse(expenseStr, out var expenseAmount) && expenseAmount != 0)
                {
                    amount = Math.Abs(expenseAmount);
                    direction = "out";
                }
                else
                {
                    _logger.LogWarning("支付宝行 {Row}: 收入和支出金额均为空", i + 1);
                    continue;
                }

                // 账户余额 (col10, index 9)
                decimal? balance = null;
                var balanceStr = GetCellValue(cells, 9)?.Trim();
                if (!string.IsNullOrWhiteSpace(balanceStr) && decimal.TryParse(balanceStr, out var balanceVal))
                {
                    balance = balanceVal;
                }

                // 对方账户 (col14, index 13)
                var counterpartyAccount = GetCellValue(cells, 13)?.Trim();
                // 对方名称 (col15, index 14)
                var counterparty = GetCellValue(cells, 14)?.Trim() ?? string.Empty;

                // 账务类型 (col7, index 6) 作为 Description
                var accountType = GetCellValue(cells, 6)?.Trim() ?? string.Empty;

                // 构建支付宝特有字段 JSON → ExtendedInfo
                var extendedInfo = BuildExtendedInfo(cells);

                rows.Add(new ParsedBankRow
                {
                    TransactionDate = transactionDate,
                    TransactionTime = transactionTime == TimeSpan.Zero ? null : transactionTime,
                    Amount = amount,
                    Direction = direction,
                    Balance = balance,
                    Counterparty = counterparty,
                    CounterpartyAccount = string.IsNullOrWhiteSpace(counterpartyAccount) ? null : counterpartyAccount,
                    TransactionNumber = string.IsNullOrWhiteSpace(transactionNumber) ? null : transactionNumber,
                    Description = accountType,
                    ExtendedInfo = extendedInfo
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "支付宝行 {Row} 解析失败，跳过该行: {Message}", i + 1, ex.Message);
            }
        }

        _logger.LogInformation("支付宝格式解析完成: 有效行数={ValidRows}, 跳过行数={SkippedRows}",
            rows.Count, skippedRows);

        return rows;
    }

    private static List<string> GetCellValues(XElement row)
    {
        var values = new List<string>();
        foreach (var cell in row.Elements(SsNs + "Cell"))
        {
            // 处理 ss:Index 属性（跳过的列用空字符串填充）
            var indexAttr = cell.Attribute(SsNs + "Index");
            if (indexAttr != null && int.TryParse(indexAttr.Value, out var index))
            {
                while (values.Count < index - 1)
                {
                    values.Add(string.Empty);
                }
            }

            var data = cell.Element(SsNs + "Data");
            values.Add(data?.Value ?? string.Empty);
        }
        return values;
    }

    private static string? GetCellValue(List<string> cells, int index)
    {
        if (index >= cells.Count) return null;
        var val = cells[index];
        // 支付宝空值用空格表示
        return string.IsNullOrWhiteSpace(val) ? null : val;
    }

    private static string? BuildExtendedInfo(List<string> cells)
    {
        var dict = new Dictionary<string, string>();

        // 备注 (col18, index 17)
        AddIfNotEmpty(dict, "memo", GetCellValue(cells, 17));
        // 服务费 (col11, index 10)
        AddIfNotEmpty(dict, "serviceFee", GetCellValue(cells, 10));
        // 支付渠道 (col12, index 11)
        AddIfNotEmpty(dict, "payChannel", GetCellValue(cells, 11));
        // 商品名称 (col17, index 16)
        AddIfNotEmpty(dict, "productName", GetCellValue(cells, 16));
        // 业务描述 (col22, index 21)
        AddIfNotEmpty(dict, "bizDesc", GetCellValue(cells, 21));
        // 开票情况 (col1, index 0)
        AddIfNotEmpty(dict, "invoiceStatus", GetCellValue(cells, 0));
        // 付款备注 (col23, index 22)
        AddIfNotEmpty(dict, "paymentMemo", GetCellValue(cells, 22));

        if (dict.Count == 0) return null;

        return JsonSerializer.Serialize(dict, new JsonSerializerOptions
        {
            WriteIndented = false,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
    }

    private static void AddIfNotEmpty(Dictionary<string, string> dict, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            dict[key] = value;
        }
    }
}
