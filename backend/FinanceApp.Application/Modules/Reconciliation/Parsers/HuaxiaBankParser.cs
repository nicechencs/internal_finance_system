using Microsoft.Extensions.Logging;
using OfficeOpenXml;

namespace FinanceApp.Application.Modules.Reconciliation.Parsers;

public class HuaxiaBankParser : IFileParser
{
    private readonly ILogger _logger;

    public HuaxiaBankParser(ILogger logger)
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

        // 扫描前 15 行找到表头行（包含"序号"+"交易日期"）
        int headerRow = -1;
        for (int row = 1; row <= Math.Min(15, dimension.Rows); row++)
        {
            var hasSequence = false;
            var hasDate = false;
            for (int col = 1; col <= Math.Min(dimension.Columns, 15); col++)
            {
                var cellValue = worksheet.Cells[row, col].GetValue<string>()?.Trim();
                if (cellValue == "序号") hasSequence = true;
                if (cellValue == "交易日期") hasDate = true;
            }
            if (hasSequence && hasDate)
            {
                headerRow = row;
                break;
            }
        }

        if (headerRow < 0)
            throw new Common.ValidationException("无法识别华夏银行文件格式：未找到包含'序号'和'交易日期'的表头行");

        _logger.LogInformation("华夏银行文件表头行: {HeaderRow}", headerRow);

        int dataStartRow = headerRow + 1;
        int skippedRows = 0;

        for (int row = dataStartRow; row <= dimension.Rows; row++)
        {
            try
            {
                // 跳过空行：检查交易日期列是否有值
                var dateStr = worksheet.Cells[row, 2].GetValue<string>()?.Trim();
                if (string.IsNullOrWhiteSpace(dateStr))
                {
                    skippedRows++;
                    continue;
                }

                // 交易日期 (col2)
                if (!DateTime.TryParse(dateStr, out var transactionDate))
                {
                    _logger.LogWarning("华夏银行行 {Row}: 日期格式无效, 值={Value}", row, dateStr);
                    continue;
                }

                // 交易时间 (col3)
                TimeSpan? transactionTime = null;
                var timeStr = worksheet.Cells[row, 3].GetValue<string>()?.Trim();
                if (!string.IsNullOrWhiteSpace(timeStr) && TimeSpan.TryParse(timeStr, out var ts))
                {
                    transactionTime = ts;
                }

                // 支出金额 (col4) 和 收入金额 (col5)
                var expenseStr = worksheet.Cells[row, 4].GetValue<string>()?.Trim();
                var incomeStr = worksheet.Cells[row, 5].GetValue<string>()?.Trim();

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
                    _logger.LogWarning("华夏银行行 {Row}: 支出和收入金额均为空", row);
                    continue;
                }

                // 余额 (col6)
                decimal? balance = null;
                var balanceStr = worksheet.Cells[row, 6].GetValue<string>()?.Trim();
                if (!string.IsNullOrWhiteSpace(balanceStr) && decimal.TryParse(balanceStr, out var balanceVal))
                {
                    balance = balanceVal;
                }

                // 对方账号 (col7)
                var counterpartyAccount = worksheet.Cells[row, 7].GetValue<string>()?.Trim();
                // 对方户名 (col8)
                var counterparty = worksheet.Cells[row, 8].GetValue<string>()?.Trim() ?? string.Empty;
                // 对方行名 (col9)
                var counterpartyBank = worksheet.Cells[row, 9].GetValue<string>()?.Trim();
                // 核心流水号 (col10)
                var transactionNumber = worksheet.Cells[row, 10].GetValue<string>()?.Trim();
                // 交易描述 (col11) 和 摘要 (col12) 分别保存
                var txDesc = worksheet.Cells[row, 11].GetValue<string>()?.Trim() ?? string.Empty;
                var memo = worksheet.Cells[row, 12].GetValue<string>()?.Trim() ?? string.Empty;

                rows.Add(new ParsedBankRow
                {
                    TransactionDate = transactionDate,
                    TransactionTime = transactionTime,
                    Amount = amount,
                    Direction = direction,
                    Balance = balance,
                    Counterparty = counterparty,
                    CounterpartyAccount = string.IsNullOrWhiteSpace(counterpartyAccount) ? null : counterpartyAccount,
                    CounterpartyBank = string.IsNullOrWhiteSpace(counterpartyBank) ? null : counterpartyBank,
                    TransactionNumber = string.IsNullOrWhiteSpace(transactionNumber) ? null : transactionNumber,
                    Description = txDesc,
                    ExtendedInfo = string.IsNullOrWhiteSpace(memo) ? null : memo
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "华夏银行行 {Row} 解析失败，跳过该行: {Message}", row, ex.Message);
            }
        }

        _logger.LogInformation("华夏银行格式解析完成: 有效行数={ValidRows}, 跳过行数={SkippedRows}",
            rows.Count, skippedRows);

        return rows;
    }
}
