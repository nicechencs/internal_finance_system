using OfficeOpenXml;

namespace FinanceApp.Api.Helpers;

/// <summary>
/// Excel 批量导入共享辅助类，提供文件验证、工作表打开和行读取功能
/// </summary>
public static class ExcelImportHelper
{
    private static readonly string[] AllowedExtensions = { ".xlsx", ".xls", ".csv" };

    /// <summary>
    /// 验证上传的 Excel 文件并返回第一个工作表
    /// </summary>
    /// <returns>成功时返回 (worksheet, package, null)；失败时返回 (null, null, errorMessage)</returns>
    public static (ExcelWorksheet? worksheet, ExcelPackage? package, string? error) ValidateAndOpenExcel(
        IFormFile? file, int maxDataRows = 500)
    {
        if (file == null || file.Length == 0)
        {
            return (null, null, "请上传文件");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            return (null, null, "仅支持 .xlsx, .xls 和 .csv 格式");
        }

        var stream = file.OpenReadStream();
        if (stream.CanSeek)
        {
            stream.Seek(0, SeekOrigin.Begin);
        }

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        ExcelPackage? package = null;

        try
        {
            package = new ExcelPackage(stream);

            var worksheet = package.Workbook.Worksheets.FirstOrDefault();
            if (worksheet == null)
            {
                return (null, null, "Excel 文件中没有找到工作表");
            }

            var dimension = worksheet.Dimension;
            if (dimension == null || dimension.Rows < 2)
            {
                return (null, null, "Excel 文件中没有数据行");
            }

            // maxDataRows + 1 行（含表头行）
            if (dimension.Rows > maxDataRows + 1)
            {
                return (null, null, $"单次批量导入不能超过{maxDataRows}条");
            }

            // 成功路径：将 package 传出，由调用方通过 using 管理生命周期
            var result = (worksheet, package, (string?)null);
            package = null; // 防止 finally 中 dispose
            return result;
        }
        finally
        {
            // 仅在失败路径下清理资源（成功时 package 已置 null）
            if (package != null)
            {
                package.Dispose();
                stream.Dispose();
            }
        }
    }

    /// <summary>
    /// 从工作表读取数据行，使用提供的映射函数将每行转换为实体
    /// </summary>
    /// <typeparam name="T">目标实体类型</typeparam>
    /// <param name="worksheet">Excel 工作表</param>
    /// <param name="rowMapper">行映射函数，接收 (worksheet, rowIndex)，返回实体或 null（跳过该行）</param>
    /// <param name="startRow">起始行号（默认 2，跳过表头）</param>
    public static List<T> ReadRows<T>(ExcelWorksheet worksheet, Func<ExcelWorksheet, int, T?> rowMapper, int startRow = 2)
    {
        var items = new List<T>();
        int totalRows = worksheet.Dimension.End.Row;
        for (int row = startRow; row <= totalRows; row++)
        {
            var item = rowMapper(worksheet, row);
            if (item != null)
                items.Add(item);
        }
        return items;
    }
}
