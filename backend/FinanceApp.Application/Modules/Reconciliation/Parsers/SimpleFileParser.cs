using Microsoft.Extensions.Logging;
using OfficeOpenXml;

namespace FinanceApp.Application.Modules.Reconciliation.Parsers;

public class SimpleFileParser : IFileParser
{
    private readonly ILogger _logger;

    public SimpleFileParser(ILogger logger)
    {
        _logger = logger;
    }

    public List<ParsedBankRow> Parse(Stream stream)
    {
        var rows = new List<ParsedBankRow>();

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        if (stream.CanSeek)
        {
            stream.Seek(0, SeekOrigin.Begin);
        }

        using var package = new ExcelPackage(stream);
        var worksheet = package.Workbook.Worksheets.FirstOrDefault();
        if (worksheet == null)
            throw new Common.ValidationException("Excel 文件中没有找到工作表");

        var dimension = worksheet.Dimension;
        if (dimension == null)
            throw new Common.ValidationException("工作表为空");

        var rowCount = dimension.Rows;
        if (rowCount < 2)
            throw new Common.ValidationException("Excel 文件中没有数据行（仅有标题行或为空）");

        int skippedRows = 0;
        for (int row = 2; row <= rowCount; row++)
        {
            try
            {
                if (IsEmptyRow(worksheet, row))
                {
                    skippedRows++;
                    continue;
                }

                // Parse date
                var dateCell = worksheet.Cells[row, 1];
                DateTime transactionDate;
                if (dateCell.Value is DateTime dt)
                {
                    transactionDate = dt;
                }
                else if (dateCell.Value is double oaDate)
                {
                    transactionDate = DateTime.FromOADate(oaDate);
                }
                else
                {
                    var dateStr = dateCell.GetValue<string>();
                    if (string.IsNullOrWhiteSpace(dateStr) || !DateTime.TryParse(dateStr, out transactionDate))
                    {
                        _logger.LogWarning("Excel 行 {Row}: 日期格式无效, 值={Value}", row, dateStr);
                        continue;
                    }
                }

                // Parse amount
                decimal amount;
                var amountCell = worksheet.Cells[row, 2];
                if (amountCell.Value is decimal decAmount)
                {
                    amount = decAmount;
                }
                else if (amountCell.Value is double dblAmount)
                {
                    amount = (decimal)dblAmount;
                }
                else
                {
                    var amountStr = amountCell.GetValue<string>();
                    if (string.IsNullOrWhiteSpace(amountStr) || !decimal.TryParse(amountStr, out amount))
                    {
                        _logger.LogWarning("Excel 行 {Row}: 金额格式无效, 值={Value}", row, amountStr);
                        continue;
                    }
                }

                var direction = amount >= 0 ? "in" : "out";
                amount = Math.Abs(amount);

                var counterparty = worksheet.Cells[row, 3].GetValue<string>()?.Trim() ?? string.Empty;
                var description = worksheet.Cells[row, 4].GetValue<string>()?.Trim() ?? string.Empty;

                string? counterpartyAccount = null;
                if (dimension.Columns >= 5)
                {
                    counterpartyAccount = worksheet.Cells[row, 5].GetValue<string>()?.Trim();
                }

                rows.Add(new ParsedBankRow
                {
                    TransactionDate = transactionDate,
                    Amount = amount,
                    Direction = direction,
                    Counterparty = counterparty,
                    CounterpartyAccount = counterpartyAccount,
                    Description = description
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Excel 行 {Row} 解析失败，跳过该行: {Message}", row, ex.Message);
            }
        }

        _logger.LogInformation("Simple 格式解析完成: 总行数={TotalRows}, 有效行数={ValidRows}, 跳过空行={SkippedRows}",
            rowCount - 1, rows.Count, skippedRows);

        return rows;
    }

    private static bool IsEmptyRow(ExcelWorksheet worksheet, int row)
    {
        for (int col = 1; col <= 4; col++)
        {
            var value = worksheet.Cells[row, col].Value;
            if (value != null && !string.IsNullOrWhiteSpace(value.ToString()))
                return false;
        }
        return true;
    }
}
