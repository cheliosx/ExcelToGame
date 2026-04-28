using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using NPOI.HSSF.UserModel;
using ExcelToGame.Models;
using ExcelToGame.Config;
using ExcelToGame.Utils;

namespace ExcelToGame.Core;

/// <summary>
/// Excel读取模块 - 解析xlsx/xls文件
/// 支持多页签配置：第一个页签main为表定义，后续页签为实际数据
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
    /// 读取Excel文件（多页签模式）
    /// 返回多个TableData，每个页签对应一个
    /// </summary>
    public List<TableData> ReadExcel(string filePath)
    {
        var result = new List<TableData>();
        
        try
        {
            Logger.Info($"开始读取文件: {filePath}");
            
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
                Logger.Error($"不支持的文件格式: {Path.GetExtension(filePath)}");
                return result;
            }
            
            // 解析文件名（格式：ClassName[中文名]）
            var (baseClassName, chineseName) = ParseFileName(Path.GetFileNameWithoutExtension(filePath));
            
            // 获取第一个页签（main页签 - 表定义）
            var mainSheet = workbook.GetSheetAt(0);
            if (mainSheet == null)
            {
                Logger.Error("工作表为空");
                workbook.Close();
                return result;
            }
            
            // 解析main页签，获取所有页签定义
            var sheetDefinitions = ParseMainSheet(mainSheet);
            
            Logger.Info($"文件 {baseClassName}[{chineseName}] 包含 {sheetDefinitions.Count} 个页签定义");
            
            // 遍历所有页签定义，读取对应页签数据
            foreach (var sheetDef in sheetDefinitions)
            {
                if (!sheetDef.ShouldExport)
                {
                    Logger.Info($"跳过页签（导出开关为FALSE）: {sheetDef.SheetName}");
                    continue;
                }
                
                // 查找对应的数据页签
                var dataSheet = FindSheetByName(workbook, sheetDef.SheetName);
                if (dataSheet == null)
                {
                    Logger.Warning($"未找到页签: {sheetDef.SheetName}");
                    continue;
                }
                
                // 创建TableData
                var tableData = new TableData
                {
                    FileName = baseClassName,
                    ClassName = sheetDef.SheetName, // 使用页签名作为类名
                    ChineseName = sheetDef.Comment,
                    SheetName = sheetDef.SheetName,
                    TableType = sheetDef.TableType,
                    ExportToClient = sheetDef.ExportToClient,
                    ExportToServer = sheetDef.ExportToServer
                };
                
                // 解析数据页签
                if (ParseDataSheet(dataSheet, tableData))
                {
                    result.Add(tableData);
                    Logger.Success($"成功读取页签 [{sheetDef.SheetName}]: {tableData.Fields.Count} 个字段, {tableData.Rows.Count} 行数据");
                }
                else
                {
                    Logger.Error($"读取页签失败: {sheetDef.SheetName}");
                    foreach (var error in tableData.Errors)
                    {
                        Logger.Error($"  - {error}");
                    }
                }
            }
            
            workbook.Close();
        }
        catch (Exception ex)
        {
            Logger.Error($"读取Excel失败: {ex.Message}");
            Logger.Error($"堆栈: {ex.StackTrace}");
        }
        
        return result;
    }
    
    /// <summary>
    /// 解析文件名（格式：ClassName[中文名]）
    /// </summary>
    private (string className, string chineseName) ParseFileName(string fileName)
    {
        // 移除继承关系标记（如果有）
        var nameWithoutInheritance = fileName;
        if (fileName.Contains(':'))
        {
            nameWithoutInheritance = fileName.Split(':')[0].Trim();
        }
        
        // 解析 ClassName[中文名] 格式
        if (nameWithoutInheritance.Contains('[') && nameWithoutInheritance.Contains(']'))
        {
            var startIdx = nameWithoutInheritance.IndexOf('[');
            var endIdx = nameWithoutInheritance.IndexOf(']');
            
            if (startIdx > 0 && endIdx > startIdx)
            {
                var className = nameWithoutInheritance.Substring(0, startIdx).Trim();
                var chineseName = nameWithoutInheritance.Substring(startIdx + 1, endIdx - startIdx - 1).Trim();
                return (className, chineseName);
            }
        }
        
        // 无法解析，返回原文件名
        return (nameWithoutInheritance, string.Empty);
    }
    
    /// <summary>
    /// 解析main页签（表定义页签）
    /// 结构：字段、file、name、client、server、type、END
    /// </summary>
    private List<SheetDefinition> ParseMainSheet(ISheet sheet)
    {
        var definitions = new List<SheetDefinition>();
        
        // main页签结构：
        // 第1行：字段注释（字段、编号、名称、客户端是否导出、服务端是否导出、表格类型）
        // 第2行：英文字段名（file、name、client、server、type）
        // 第3行：导出开关（TRUE/FALSE）
        // 第4行+：数据行
        // END行：结束标记
        
        // 查找列索引
        var headerRow = sheet.GetRow(1); // 第2行是字段名
        if (headerRow == null)
        {
            Logger.Error("main页签缺少字段名行");
            return definitions;
        }
        
        int fileCol = -1, nameCol = -1, clientCol = -1, serverCol = -1, typeCol = -1, exportCol = -1;
        
        for (int col = 0; col <= headerRow.LastCellNum; col++)
        {
            var cellValue = GetCellStringValue(headerRow.GetCell(col)).ToLower();
            
            switch (cellValue)
            {
                case "file":
                    fileCol = col;
                    break;
                case "name":
                    nameCol = col;
                    break;
                case "client":
                    clientCol = col;
                    break;
                case "server":
                    serverCol = col;
                    break;
                case "type":
                    typeCol = col;
                    break;
                case "export":
                case "": // 第一列可能是空或export
                    exportCol = col;
                    break;
            }
            
            // 遇到END列停止
            if (cellValue.Equals("end", StringComparison.OrdinalIgnoreCase))
                break;
        }
        
        // 检查必要列
        if (fileCol < 0)
        {
            Logger.Error("main页签缺少必要的'file'列");
            return definitions;
        }
        
        // 从第4行开始读取数据（0-based index = 3）
        for (int rowIdx = 3; rowIdx <= sheet.LastRowNum; rowIdx++)
        {
            var row = sheet.GetRow(rowIdx);
            if (row == null) continue;
            
            // 检查END行
            var firstCell = GetCellStringValue(row.GetCell(0));
            if (firstCell.Equals("END", StringComparison.OrdinalIgnoreCase))
                break;
            
            // 检查导出开关
            var exportValue = GetCellStringValue(row.GetCell(exportCol >= 0 ? exportCol : 0));
            bool shouldExport = exportValue.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                               exportValue.Equals("1") ||
                               exportValue.Equals("yes", StringComparison.OrdinalIgnoreCase);
            
            // 读取页签定义
            var sheetName = GetCellStringValue(row.GetCell(fileCol));
            if (string.IsNullOrWhiteSpace(sheetName))
                continue;
            
            var definition = new SheetDefinition
            {
                SheetName = sheetName,
                Comment = nameCol >= 0 ? GetCellStringValue(row.GetCell(nameCol)) : string.Empty,
                ExportToClient = clientCol >= 0 ? IsYesValue(GetCellStringValue(row.GetCell(clientCol))) : true,
                ExportToServer = serverCol >= 0 ? IsYesValue(GetCellStringValue(row.GetCell(serverCol))) : true,
                TableType = typeCol >= 0 ? ParseTableType(GetCellStringValue(row.GetCell(typeCol))) : TableType.PrimaryKey,
                ShouldExport = shouldExport
            };
            
            definitions.Add(definition);
        }
        
        return definitions;
    }
    
    /// <summary>
    /// 解析数据页签
    /// </summary>
    private bool ParseDataSheet(ISheet sheet, TableData tableData)
    {
        // 数据页签结构（标准4行表头）：
        // 第1行：字段中文注释
        // 第2行：英文字段名
        // 第3行：字段数据类型
        // 第4行+：数据行
        // END行：结束标记
        
        // 解析表头
        if (!ParseHeader(sheet, tableData))
        {
            return false;
        }
        
        // 解析数据行
        ParseDataRows(sheet, tableData);
        
        return tableData.IsValid;
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
                break;
            }
            
            var fieldInfo = new FieldInfo
            {
                ColumnIndex = col,
                Name = fieldName,
                Comment = GetCellStringValue(commentRow?.GetCell(col)),
                RawType = GetCellStringValue(typeRow.GetCell(col)),
                IsExportSwitch = false, // 数据页签第一列不再是导出开关
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
        
        return true;
    }
    
    /// <summary>
    /// 解析数据行
    /// </summary>
    private void ParseDataRows(ISheet sheet, TableData tableData)
    {
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
                break;
            }
            
            var rowData = new RowData
            {
                RowNumber = rowIdx + 1,
                ShouldExport = true // 数据页签默认全部导出（导出控制在main页签）
            };
            
            // 解析各字段值
            foreach (var field in tableData.Fields)
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
    /// 根据名称查找页签
    /// </summary>
    private ISheet? FindSheetByName(IWorkbook workbook, string sheetName)
    {
        for (int i = 0; i < workbook.NumberOfSheets; i++)
        {
            var sheet = workbook.GetSheetAt(i);
            if (sheet.SheetName.Equals(sheetName, StringComparison.OrdinalIgnoreCase))
            {
                return sheet;
            }
        }
        return null;
    }
    
    /// <summary>
    /// 解析表格类型
    /// </summary>
    private TableType ParseTableType(string typeValue)
    {
        return typeValue.Trim().ToLower() switch
        {
            "参数表" or "param" => TableType.Param,
            "枚举表" or "enum" => TableType.Enum,
            "主键表" or "primarykey" => TableType.PrimaryKey,
            "数组表" or "array" => TableType.Array,
            "分组表" or "group" => TableType.Group,
            _ => TableType.PrimaryKey
        };
    }
    
    /// <summary>
    /// 判断是否为YES/TRUE
    /// </summary>
    private bool IsYesValue(string value)
    {
        return value.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("1") ||
               value.Equals("是") ||
               value.Equals("y");
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
