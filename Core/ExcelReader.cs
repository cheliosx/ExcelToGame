using System.Text.RegularExpressions;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using NPOI.HSSF.UserModel;
using ExcelToGame.Models;
using ExcelToGame.Utils;

namespace ExcelToGame.Core;

public class ExcelReader
{
    public ExcelFileData? ReadExcelFile(string filePath)
    {
        try
        {
            Logger.Info($"读取文件: {Path.GetFileName(filePath)}");

            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            IWorkbook workbook;

            if (filePath.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                workbook = new XSSFWorkbook(fs);
            }
            else
            {
                workbook = new HSSFWorkbook(fs);
            }

            var excelData = new ExcelFileData
            {
                FilePath = filePath,
                FileName = Path.GetFileNameWithoutExtension(filePath)
            };

            // 先找main页签
            ISheet? mainSheet = null;
            for (int i = 0; i < workbook.NumberOfSheets; i++)
            {
                var sheet = workbook.GetSheetAt(i);
                if (sheet?.SheetName.Equals("main", StringComparison.OrdinalIgnoreCase) == true)
                {
                    mainSheet = sheet;
                    break;
                }
            }

            if (mainSheet == null)
            {
                Logger.Error($"未找到main页签: {filePath}");
                workbook.Close();
                return null;
            }

            // 读取main页签获取输出文件名和sheet定义
            var sheetDefs = ReadMainSheet(mainSheet);
            
            // 输出所有可用的页签名
            Logger.Info($"  可用页签: {string.Join(", ", Enumerable.Range(0, workbook.NumberOfSheets).Select(i => workbook.GetSheetAt(i).SheetName))}");
            
            if (sheetDefs.Count == 0)
            {
                Logger.Error($"main页签没有定义: {filePath}");
                workbook.Close();
                return null;
            }

            excelData.SheetDefinitions = sheetDefs;

            // 获取第一个sheet的file字段作为输出文件名
            var firstDef = sheetDefs.FirstOrDefault();
            if (firstDef != null && !string.IsNullOrWhiteSpace(firstDef.File))
            {
                var cleanedFile = CleanFileName(firstDef.File);
                Logger.Info($"  File字段: '{firstDef.File}' -> 清理后: '{cleanedFile}'");
                if (!string.IsNullOrWhiteSpace(cleanedFile))
                {
                    excelData.OutputName = cleanedFile;
                }
                else
                {
                    excelData.OutputName = CleanFileName(excelData.FileName);
                    Logger.Info($"  使用Excel文件名: '{excelData.FileName}' -> '{excelData.OutputName}'");
                }
            }
            else
            {
                excelData.OutputName = CleanFileName(excelData.FileName);
            }

            // 读取各个数据页签
            foreach (var def in sheetDefs)
            {
                // 尝试精确匹配，如果失败则尝试前缀匹配
                var dataSheet = workbook.GetSheet(def.Name);
                if (dataSheet == null)
                {
                    // 尝试前缀匹配（处理 testArg 匹配 testArg_测试参数表 的情况）
                    for (int i = 0; i < workbook.NumberOfSheets; i++)
                    {
                        var sheetName = workbook.GetSheetAt(i).SheetName;
                        if (sheetName.StartsWith(def.Name + "_", StringComparison.OrdinalIgnoreCase))
                        {
                            dataSheet = workbook.GetSheetAt(i);
                            break;
                        }
                    }
                }
                
                if (dataSheet == null)
                {
                    Logger.Warning($"未找到页签: {def.Name}");
                    continue;
                }

                var sheetData = ReadDataSheet(dataSheet, def);
                if (sheetData != null && sheetData.Rows.Count > 0)
                {
                    excelData.DataSheets.Add(sheetData);
                }
            }

            workbook.Close();

            if (excelData.DataSheets.Count == 0)
            {
                Logger.Warning($"没有读取到任何数据: {filePath}");
                return null;
            }

            return excelData;
        }
        catch (Exception ex)
        {
            Logger.Error($"读取文件失败 [{filePath}]: {ex.Message}");
            return null;
        }
    }

    private List<SheetDefinition> ReadMainSheet(ISheet sheet)
    {
        var definitions = new List<SheetDefinition>();

        // main页签结构：第1行是注释，第2行是字段名，第3行是类型，从第4行开始是数据
        var nameRow = sheet.GetRow(1);
        if (nameRow == null) return definitions;

        // 找到各列的索引
        int fileCol = -1, nameCol = -1, clientCol = -1, serverCol = -1, typeCol = -1, extendCol = -1;

        for (int col = 0; col <= nameRow.LastCellNum; col++)
        {
            var cellValue = GetCellValue(nameRow.GetCell(col)).ToLower();
            switch (cellValue)
            {
                case "file":
                case "文件名":
                case "名称":
                    fileCol = col; 
                    break;
                case "name":
                case "编号":
                case "页签名":
                    nameCol = col; 
                    break;
                case "client":
                case "客户端是否导出":
                    clientCol = col; 
                    break;
                case "server":
                case "服务端是否导出":
                    serverCol = col; 
                    break;
                case "type":
                case "表格类型":
                    typeCol = col; 
                    break;
                case "extend":
                case "继承":
                    extendCol = col; 
                    break;
            }
        }

        if (fileCol < 0 || nameCol < 0)
        {
            Logger.Error("main页签缺少必要的列(file或name)");
            return definitions;
        }

        // 读取数据行（从第4行开始，索引为3）
        for (int rowIdx = 3; rowIdx <= sheet.LastRowNum; rowIdx++)
        {
            var row = sheet.GetRow(rowIdx);
            if (row == null) continue;

            var exportFlag = GetCellValue(row.GetCell(0));
            if (exportFlag != "1" && !exportFlag.Equals("TRUE", StringComparison.OrdinalIgnoreCase))
                continue;

            if (exportFlag.Equals("END", StringComparison.OrdinalIgnoreCase))
                break;

            var file = GetCellValue(row.GetCell(fileCol));
            var name = GetCellValue(row.GetCell(nameCol));

            if (string.IsNullOrWhiteSpace(file) || string.IsNullOrWhiteSpace(name))
                continue;

            var def = new SheetDefinition
            {
                File = file,
                Name = name,
                Client = clientCol >= 0 ? GetCellValue(row.GetCell(clientCol)) != "0" : true,
                Server = serverCol >= 0 ? GetCellValue(row.GetCell(serverCol)) != "0" : true,
                Type = typeCol >= 0 ? GetCellValue(row.GetCell(typeCol)) : "",
                Extend = extendCol >= 0 ? GetCellValue(row.GetCell(extendCol)) : null
            };

            definitions.Add(def);
        }

        return definitions;
    }

    private SheetData? ReadDataSheet(ISheet sheet, SheetDefinition def)
    {
        var sheetData = new SheetData
        {
            SheetName = def.Name,
            FileName = def.File,
            ExportToClient = def.Client,
            ExportToServer = def.Server,
            Extend = def.Extend
        };

        var commentRow = sheet.GetRow(0);
        var nameRow = sheet.GetRow(1);
        var typeRow = sheet.GetRow(2);

        if (nameRow == null || typeRow == null)
        {
            return null;
        }

        var fields = new List<FieldInfo>();
        int col = 0;

        while (true)
        {
            var fieldName = GetCellValue(nameRow.GetCell(col));

            if (string.IsNullOrWhiteSpace(fieldName) || fieldName.Equals("END", StringComparison.OrdinalIgnoreCase))
                break;

            if (col == 0)
            {
                col++;
                continue;
            }

            var fieldType = GetCellValue(typeRow.GetCell(col));
            var comment = GetCellValue(commentRow?.GetCell(col));

            fields.Add(new FieldInfo
            {
                Name = fieldName,
                Type = fieldType,
                Comment = comment,
                ColumnIndex = col
            });

            col++;
        }

        sheetData.Fields = fields;

        for (int rowIdx = 3; rowIdx <= sheet.LastRowNum; rowIdx++)
        {
            var row = sheet.GetRow(rowIdx);
            if (row == null) continue;

            var exportFlag = GetCellValue(row.GetCell(0));
            if (exportFlag != "1" && !exportFlag.Equals("TRUE", StringComparison.OrdinalIgnoreCase))
                continue;

            if (exportFlag.Equals("END", StringComparison.OrdinalIgnoreCase))
                break;

            var rowData = new Dictionary<string, object?>();

            foreach (var field in fields)
            {
                var cellValue = GetCellValue(row.GetCell(field.ColumnIndex));
                rowData[field.Name] = ParseValue(cellValue, field.Type);
            }

            sheetData.Rows.Add(rowData);
        }

        return sheetData;
    }

    private string CleanFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return "config";
            
        // 先移除方括号内容
        var result = Regex.Replace(fileName, @"\[.*?\]", "");
        
        // 移除中文字符（CJK统一表意文字范围）
        result = Regex.Replace(result, "[\u4e00-\u9fa5]", "");
        
        // 清理多余字符
        result = result.Trim('_', '-', ' ');
        
        // 如果清理后为空，返回默认值
        if (string.IsNullOrWhiteSpace(result))
        {
            return "config";
        }
        
        return result;
    }

    private string GetCellValue(ICell? cell)
    {
        if (cell == null) return string.Empty;

        return cell.CellType switch
        {
            CellType.String => cell.StringCellValue?.Trim() ?? string.Empty,
            CellType.Numeric => cell.NumericCellValue.ToString(),
            CellType.Boolean => cell.BooleanCellValue ? "1" : "0",
            CellType.Formula => GetFormulaValue(cell),
            _ => string.Empty
        };
    }

    private string GetFormulaValue(ICell cell)
    {
        try
        {
            var evaluator = cell.Sheet.Workbook.GetCreationHelper().CreateFormulaEvaluator();
            var value = evaluator.Evaluate(cell);

            return value.CellType switch
            {
                CellType.String => value.StringValue ?? string.Empty,
                CellType.Numeric => value.NumberValue.ToString(),
                CellType.Boolean => value.BooleanValue ? "1" : "0",
                _ => string.Empty
            };
        }
        catch
        {
            return string.Empty;
        }
    }

    private object? ParseValue(string value, string type)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return type.ToLower() switch
            {
                "number" or "int" or "float" or "double" => 0,
                "bool" or "boolean" => false,
                _ => null
            };
        }

        type = type.ToLower();

        if (type.StartsWith("array<"))
        {
            var content = value.Trim();
            if (content.StartsWith("[") && content.EndsWith("]"))
            {
                content = content.Substring(1, content.Length - 2);
            }

            return content.Split(',').Select(s => s.Trim()).ToList();
        }

        return type switch
        {
            "number" or "int" => int.TryParse(value, out var intVal) ? intVal : 0,
            "float" => float.TryParse(value, out var floatVal) ? floatVal : 0f,
            "double" => double.TryParse(value, out var doubleVal) ? doubleVal : 0.0,
            "bool" or "boolean" => value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase),
            "string" or "language" => value,
            _ => value
        };
    }
}
