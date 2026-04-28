using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using NPOI.HSSF.UserModel;
using ExcelToGame.Models;
using ExcelToGame.Config;
using ExcelToGame.Utils;

namespace ExcelToGame.Core;

/// <summary>
/// Excel读取模块 - 解析xlsx/xls文件
/// </summary>
public class ExcelReader
{
    private readonly AppConfig _config;
    private readonly TypeParser _typeParser;
    
    public ExcelReader(AppConfig config)
    {
        _config = config;
        _typeParser = new TypeParser();
    }
    
    /// <summary>
    /// 读取Excel文件
    /// </summary>
    public TableData ReadExcel(string filePath)
    {
        var tableData = new TableData
        {
            FileName = Path.GetFileNameWithoutExtension(filePath)
        };
        
        // 解析类名和继承关系（格式：子表:父表）
        ParseClassName(tableData);
        
        try
        {
            Logger.Table(tableData.FileName, $"开始读取文件: {filePath}");
            
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            IWorkbook workbook;
            
            if (filePath.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                workbook = new XSSFWorkbook(fs);
            }
            else if (filePath.EndsWith(".xls", StringComparison.OrdinalIgnoreCase))
            {
                workbook = new HSSFWorkbook(fs);
            }
            else
            {
                tableData.AddError($"不支持的文件格式: {Path.GetExtension(filePath)}");
                return tableData;
            }
            
            // 读取第一个工作表
            var sheet = workbook.GetSheetAt(0);
            if (sheet == null)
            {
                tableData.AddError("工作表为空");
                return tableData;
            }
            
            // 解析表头
            if (!ParseHeader(sheet, tableData))
            {
                return tableData;
            }
            
            // 解析数据行
            ParseDataRows(sheet, tableData);
            
            Logger.Table(tableData.FileName, $"读取完成: {tableData.Fields.Count} 个字段, {tableData.Rows.Count} 行数据");
            
            workbook.Close();
        }
        catch (Exception ex)
        {
            tableData.AddError($"读取Excel失败: {ex.Message}");
            Logger.Error($"读取文件异常: {ex}");
        }
        
        return tableData;
    }
    
    /// <summary>
    /// 解析类名和继承关系
    /// </summary>
    private void ParseClassName(TableData tableData)
    {
        var fileName = tableData.FileName;
        
        // 检查是否包含继承关系（格式：子表:父表）
        if (fileName.Contains(':'))
        {
            var parts = fileName.Split(':');
            if (parts.Length == 2)
            {
                tableData.ClassName = parts[0].Trim();
                tableData.ParentTableName = parts[1].Trim();
                Logger.Table(tableData.FileName, $"检测到继承关系: {tableData.ClassName} : {tableData.ParentTableName}");
            }
            else
            {
                tableData.ClassName = fileName;
            }
        }
        else
        {
            tableData.ClassName = fileName;
        }
    }
    
    /// <summary>
    /// 解析表头（前3行）
    /// </summary>
    private bool ParseHeader(ISheet sheet, TableData tableData)
    {
        // 表头行：第1行注释、第2行字段名、第3行类型
        var commentRow = sheet.GetRow(0);
        var nameRow = sheet.GetRow(1);
        var typeRow = sheet.GetRow(2);
        
        if (nameRow == null || typeRow == null)
        {
            tableData.AddError("表头不完整，缺少字段名行或类型行");
            return false;
        }
        
        // 遍历列，直到遇到END列或空列
        for (int col = 0; col <= nameRow.LastCellNum; col++)
        {
            var cell = nameRow.GetCell(col);
            var fieldName = GetCellStringValue(cell);
            
            // 遇到END列或空列，停止解析
            if (string.IsNullOrWhiteSpace(fieldName) || 
                fieldName.Equals(_config.ColumnEndMarker, StringComparison.OrdinalIgnoreCase))
            {
                Logger.Debug($"遇到列结束标记，停止解析列。当前列数: {col}");
                break;
            }
            
            // 跳过导出开关列（第一列）
            if (col == 0)
            {
                if (!fieldName.Equals(_config.ExportSwitchColumnName, StringComparison.OrdinalIgnoreCase))
                {
                    tableData.AddWarning($"第一列应为导出开关列，但名称为 '{fieldName}'");
                }
            }
            
            var fieldInfo = new FieldInfo
            {
                ColumnIndex = col,
                Name = fieldName,
                Comment = GetCellStringValue(commentRow?.GetCell(col)),
                RawType = GetCellStringValue(typeRow.GetCell(col)),
                IsExportSwitch = col == 0,
                IsPrimaryKey = fieldName.Equals("Id", StringComparison.OrdinalIgnoreCase) || 
                               fieldName.Equals("ID", StringComparison.OrdinalIgnoreCase)
            };
            
            // 解析类型
            _typeParser.ParseFieldType(fieldInfo);
            
            // 验证类型
            var typeErrors = new List<string>();
            if (!fieldInfo.ValidateType(typeErrors))
            {
                foreach (var error in typeErrors)
                {
                    tableData.AddError(error);
                }
            }
            
            // 检查字段名重复
            if (tableData.Fields.Any(f => f.Name.Equals(fieldInfo.Name, StringComparison.OrdinalIgnoreCase)))
            {
                tableData.AddError($"字段名重复: {fieldInfo.Name}");
            }
            else
            {
                tableData.Fields.Add(fieldInfo);
            }
        }
        
        if (tableData.Fields.Count == 0)
        {
            tableData.AddError("未找到有效字段");
            return false;
        }
        
        // 检查是否有导出开关列
        if (tableData.GetExportSwitchField() == null)
        {
            tableData.AddError("缺少导出开关列（第一列）");
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// 解析数据行
    /// </summary>
    private void ParseDataRows(ISheet sheet, TableData tableData)
    {
        var exportSwitchField = tableData.GetExportSwitchField();
        if (exportSwitchField == null) return;
        
        // 从第4行开始读取数据（0-based index = 3）
        for (int rowIdx = _config.DataStartRow; rowIdx <= sheet.LastRowNum; rowIdx++)
        {
            var row = sheet.GetRow(rowIdx);
            if (row == null) continue;
            
            // 检查是否为END行
            var firstCell = row.GetCell(0);
            var firstValue = GetCellStringValue(firstCell);
            
            if (firstValue.Equals(_config.RowEndMarker, StringComparison.OrdinalIgnoreCase))
            {
                Logger.Debug($"第 {rowIdx + 1} 行为结束标记行，停止读取数据");
                break;
            }
            
            var rowData = new RowData
            {
                RowNumber = rowIdx + 1
            };
            
            // 读取导出开关
            var switchCell = row.GetCell(exportSwitchField.ColumnIndex);
            var switchValue = GetCellStringValue(switchCell);
            rowData.ShouldExport = switchValue.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                                   switchValue.Equals("1") ||
                                   switchValue.Equals("yes", StringComparison.OrdinalIgnoreCase);
            
            // 如果不需要导出，跳过此行的详细解析
            if (!rowData.ShouldExport)
            {
                continue;
            }
            
            // 解析各字段值
            foreach (var field in tableData.GetDataFields())
            {
                var cell = row.GetCell(field.ColumnIndex);
                var cellValue = GetCellStringValue(cell);
                
                try
                {
                    var parsedValue = ParseCellValue(cellValue, field);
                    rowData.SetValue(field.Name, parsedValue);
                    
                    // 记录主键值
                    if (field.IsPrimaryKey)
                    {
                        rowData.PrimaryKeyValue = parsedValue;
                    }
                }
                catch (Exception ex)
                {
                    tableData.AddError($"第 {rowIdx + 1} 行，字段 '{field.Name}' 解析失败: {ex.Message}");
                }
            }
            
            tableData.Rows.Add(rowData);
        }
    }
    
    /// <summary>
    /// 解析单元格值
    /// </summary>
    private object? ParseCellValue(string value, FieldInfo field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            // 返回类型默认值
            if (field.IsArray) return new List<object?>();
            return field.BaseType.ToLower() switch
            {
                "int" or "long" or "float" or "double" => 0,
                "bool" or "boolean" => false,
                _ => null
            };
        }
        
        // 数组类型解析
        if (field.IsArray)
        {
            return ParseArrayValue(value, field);
        }
        
        // 枚举类型
        if (field.IsEnum)
        {
            return value.Trim();
        }
        
        // 自定义对象类型
        if (field.IsCustomType)
        {
            return ParseCustomObject(value, field);
        }
        
        // 基础类型解析
        return field.BaseType.ToLower() switch
        {
            "int" => int.TryParse(value, out var intVal) ? intVal : 0,
            "long" => long.TryParse(value, out var longVal) ? longVal : 0L,
            "float" => float.TryParse(value, out var floatVal) ? floatVal : 0f,
            "double" => double.TryParse(value, out var doubleVal) ? doubleVal : 0.0,
            "bool" or "boolean" => value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                                   value.Equals("1") ||
                                   value.Equals("yes", StringComparison.OrdinalIgnoreCase),
            "string" => value,
            _ => value
        };
    }
    
    /// <summary>
    /// 解析数组值
    /// </summary>
    private List<object?> ParseArrayValue(string value, FieldInfo field)
    {
        var result = new List<object?>();
        
        if (string.IsNullOrWhiteSpace(value))
        {
            return result;
        }
        
        // 使用英文逗号分割
        var items = value.Split(',');
        
        foreach (var item in items)
        {
            var trimmedItem = item.Trim();
            if (string.IsNullOrEmpty(trimmedItem))
            {
                continue;
            }
            
            object? parsedItem = field.BaseType.ToLower() switch
            {
                "int" => int.TryParse(trimmedItem, out var intVal) ? intVal : 0,
                "long" => long.TryParse(trimmedItem, out var longVal) ? longVal : 0L,
                "float" => float.TryParse(trimmedItem, out var floatVal) ? floatVal : 0f,
                "double" => double.TryParse(trimmedItem, out var doubleVal) ? doubleVal : 0.0,
                "bool" or "boolean" => trimmedItem.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                                       trimmedItem.Equals("1") ||
                                       trimmedItem.Equals("yes", StringComparison.OrdinalIgnoreCase),
                "string" => trimmedItem,
                _ => trimmedItem // 枚举或自定义类型
            };
            
            result.Add(parsedItem);
        }
        
        return result;
    }
    
    /// <summary>
    /// 解析自定义对象
    /// </summary>
    private Dictionary<string, object?> ParseCustomObject(string value, FieldInfo field)
    {
        // 自定义对象格式: key1=value1;key2=value2
        var result = new Dictionary<string, object?>();
        
        if (string.IsNullOrWhiteSpace(value))
        {
            return result;
        }
        
        var pairs = value.Split(';');
        foreach (var pair in pairs)
        {
            var kv = pair.Split('=');
            if (kv.Length == 2)
            {
                result[kv[0].Trim()] = kv[1].Trim();
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// 获取单元格字符串值
    /// </summary>
    private string GetCellStringValue(ICell? cell)
    {
        if (cell == null)
        {
            return string.Empty;
        }
        
        return cell.CellType switch
        {
            CellType.String => cell.StringCellValue?.Trim() ?? string.Empty,
            CellType.Numeric => cell.NumericCellValue.ToString(),
            CellType.Boolean => cell.BooleanCellValue.ToString().ToLower(),
            CellType.Formula => GetFormulaCellValue(cell),
            _ => string.Empty
        };
    }
    
    /// <summary>
    /// 获取公式单元格的值
    /// </summary>
    private string GetFormulaCellValue(ICell cell)
    {
        try
        {
            var evaluator = cell.Sheet.Workbook.GetCreationHelper().CreateFormulaEvaluator();
            var evaluatedCell = evaluator.Evaluate(cell);
            
            return evaluatedCell.CellType switch
            {
                CellType.String => evaluatedCell.StringValue?.Trim() ?? string.Empty,
                CellType.Numeric => evaluatedCell.NumberValue.ToString(),
                CellType.Boolean => evaluatedCell.BooleanValue.ToString().ToLower(),
                _ => string.Empty
            };
        }
        catch
        {
            return string.Empty;
        }
    }
}
