using System.Text.RegularExpressions;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using NPOI.HSSF.UserModel;
using ExcelToGame.Models;
using ExcelToGame.Utils;

namespace ExcelToGame.Core;

public class ExcelReader
{
    private int _languageId = 1; // 全局语言ID计数器，从1开始
    
    // 获取下一个可用的语言ID（已被使用的最大ID + 1）
    public int GetNextLanguageId() => _languageId;
    
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

            // 使用Excel文件名（去掉方括号及其中内容）作为输出文件名
            excelData.OutputName = CleanFileName(excelData.FileName);
            Logger.Info($"  使用Excel文件名: '{excelData.FileName}' -> '{excelData.OutputName}'");

            // 读取各个数据页签
            foreach (var def in sheetDefs)
            {
                // 使用中文名查找sheet（因为sheet的实际名称是中文）
                var dataSheet = workbook.GetSheet(def.ChineseName);
                if (dataSheet == null)
                {
                    // 尝试前缀匹配（处理 游戏参数表 匹配 游戏参数表_xxx 的情况）
                    for (int i = 0; i < workbook.NumberOfSheets; i++)
                    {
                        var sheetName = workbook.GetSheetAt(i).SheetName;
                        if (sheetName.StartsWith(def.ChineseName + "_", StringComparison.OrdinalIgnoreCase))
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

            var chineseName = GetCellValue(row.GetCell(fileCol));  // 文件名列是中文名
            var englishName = GetCellValue(row.GetCell(nameCol));      // 页签名列是英文名

            if (string.IsNullOrWhiteSpace(englishName))
                continue;

            var typeValue = typeCol >= 0 ? GetCellValue(row.GetCell(typeCol)) : "";
            Logger.Info($"[DEBUG] ReadMainSheet: {englishName}, typeCol={typeCol}, typeValue='{typeValue}'");
            
            var def = new SheetDefinition
            {
                File = englishName,
                Name = englishName,  // 使用英文名作为sheet名称
                ChineseName = chineseName,
                Client = clientCol >= 0 ? GetCellValue(row.GetCell(clientCol)) != "0" : true,
                Server = serverCol >= 0 ? GetCellValue(row.GetCell(serverCol)) != "0" : true,
                Type = typeValue,
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
            ChineseName = def.ChineseName,
            FileName = def.File,
            TableType = ParseTableType(def.Type),
            ExportToClient = def.Client,
            ExportToServer = def.Server,
            Extend = def.Extend
        };

        // 查找关键行
        int nameRowIdx = -1;      // 字段名行（包含"符号"或第一列是"符号"）
        int typeRowIdx = -1;      // 数据类型行（包含"数据类型"）
        int commentRowIdx = -1;   // 注释行（包含"注释"）
        int chineseNameRowIdx = -1; // 中文名称行（包含"名称"）
        int clientRowIdx = -1;    // 客户端导出行
        int serverRowIdx = -1;    // 服务端导出行

        for (int i = 0; i <= Math.Min(10, sheet.LastRowNum); i++)
        {
            var row = sheet.GetRow(i);
            if (row == null) continue;

            var firstCell = GetCellValue(row.GetCell(0));
            if (string.IsNullOrWhiteSpace(firstCell)) continue;

            if (firstCell == "符号")
                nameRowIdx = i;
            else if (firstCell == "数据类型")
                typeRowIdx = i;
            else if (firstCell == "注释")
                commentRowIdx = i;
            else if (firstCell == "名称")
                chineseNameRowIdx = i;
            else if (firstCell == "客户端")
                clientRowIdx = i;
            else if (firstCell == "服务端")
                serverRowIdx = i;
        }

        // 如果没找到字段名行，使用第0行
        if (nameRowIdx < 0) nameRowIdx = 0;
        // 如果没找到类型行，使用字段名行的下一行
        if (typeRowIdx < 0) typeRowIdx = nameRowIdx + 1;

        var nameRow = sheet.GetRow(nameRowIdx);
        var typeRow = sheet.GetRow(typeRowIdx);
        var commentRow = commentRowIdx >= 0 ? sheet.GetRow(commentRowIdx) : null;
        var chineseNameRow = chineseNameRowIdx >= 0 ? sheet.GetRow(chineseNameRowIdx) : null;
        var clientRow = clientRowIdx >= 0 ? sheet.GetRow(clientRowIdx) : null;
        var serverRow = serverRowIdx >= 0 ? sheet.GetRow(serverRowIdx) : null;

        if (nameRow == null || typeRow == null)
        {
            return null;
        }

        var fields = new List<FieldInfo>();
        int col = 1; // 从第1列开始（跳过"符号"列）

        while (true)
        {
            var fieldName = GetCellValue(nameRow.GetCell(col));

            if (string.IsNullOrWhiteSpace(fieldName) || fieldName.Equals("END", StringComparison.OrdinalIgnoreCase))
                break;

            // 跳过特殊列
            if (fieldName == "注释" || fieldName == "客户端" || fieldName == "服务端" ||
                fieldName == "数据类型" || fieldName == "符号" || fieldName == "名称")
            {
                col++;
                continue;
            }

            var fieldType = GetCellValue(typeRow.GetCell(col));
            var comment = commentRow != null ? GetCellValue(commentRow.GetCell(col)) : "";
            var chineseName = chineseNameRow != null ? GetCellValue(chineseNameRow.GetCell(col)) : "";
            var clientExport = clientRow != null ? GetCellValue(clientRow.GetCell(col)) : "YES";
            var serverExport = serverRow != null ? GetCellValue(serverRow.GetCell(col)) : "YES";

            fields.Add(new FieldInfo
            {
                Name = fieldName,
                Type = fieldType,
                Comment = chineseName, // 使用中文名作为注释
                ColumnIndex = col,
                ExportToClient = clientExport != "NO" && clientExport != "0",
                ExportToServer = serverExport != "NO" && serverExport != "0"
            });

            col++;
        }

        // Config表特殊处理：key列的值是字段名，value列的值是字段值
        bool isConfigTable = sheetData.TableType == TableType.Config ||
                             def.Name.EndsWith("Arg", StringComparison.OrdinalIgnoreCase) ||
                             def.Name.EndsWith("Config", StringComparison.OrdinalIgnoreCase);

        int keyColIdx = -1;
        int valueColIdx = -1;
        int typeColIdx = -1;

        if (isConfigTable)
        {
            // 查找key和value列
            for (int c = 1; c <= nameRow.LastCellNum; c++)
            {
                var colName = GetCellValue(nameRow.GetCell(c));
                if (colName.Equals("key", StringComparison.OrdinalIgnoreCase))
                    keyColIdx = c;
                else if (colName.Equals("value", StringComparison.OrdinalIgnoreCase))
                    valueColIdx = c;
            }

            // 查找数据类型列（在nameRow中查找"数据类型"列标题）
            for (int c = 1; c <= nameRow.LastCellNum; c++)
            {
                var colName = GetCellValue(nameRow.GetCell(c));
                if (colName.Equals("数据类型", StringComparison.OrdinalIgnoreCase) ||
                    colName.Equals("type", StringComparison.OrdinalIgnoreCase))
                {
                    typeColIdx = c;
                    break;
                }
            }
        }

        if (isConfigTable && keyColIdx > 0 && valueColIdx > 0)
        {
            // Config表：每行是一个配置项，key列的值作为字段名，value列的值作为字段值
            var configFields = new List<FieldInfo>();
            var configRowData = new Dictionary<string, object?>();

            // 数据从类型行的下一行开始
            int dataStartRow = typeRowIdx + 1;
            while (dataStartRow <= sheet.LastRowNum)
            {
                var row = sheet.GetRow(dataStartRow);
                if (row == null) break;

                var firstCell = GetCellValue(row.GetCell(0));
                if (firstCell == "TRUE" || firstCell == "1")
                {
                    var key = GetCellValue(row.GetCell(keyColIdx));
                    var value = GetCellValue(row.GetCell(valueColIdx));
                    var fieldType = typeColIdx > 0 ? GetCellValue(row.GetCell(typeColIdx)) : "string";
                    var comment = commentRow != null ? GetCellValue(commentRow.GetCell(keyColIdx)) : "";

                    if (!string.IsNullOrWhiteSpace(key))
                    {
                        configFields.Add(new FieldInfo
                        {
                            Name = key,
                            Type = fieldType,
                            Comment = comment,
                            ColumnIndex = valueColIdx
                        });

                        // 处理language类型字段
                        if (fieldType.Equals("language", StringComparison.OrdinalIgnoreCase))
                        {
                            var langKey = $"{_languageId++}";
                            sheetData.LanguageEntries.Add(new LanguageEntry
                            {
                                Key = langKey,
                                Value = value,
                                SheetName = sheetData.SheetName,
                                RowId = dataStartRow,
                                FieldName = key
                            });
                            configRowData[key] = langKey;
                        }
                        else
                        {
                            configRowData[key] = ParseValue(value, fieldType);
                        }
                    }
                }
                else if (firstCell == "END")
                {
                    break;
                }

                dataStartRow++;
            }

            sheetData.Fields = configFields;
            if (configRowData.Count > 0)
            {
                sheetData.Rows.Add(configRowData);
            }
        }
        else
        {
            // 普通表：使用解析的fields
            sheetData.Fields = fields;

            // 数据从类型行的下一行开始
            int dataStartRow = typeRowIdx + 1;
            // 跳过客户端、服务端、名称、注释等元数据行
            while (dataStartRow <= sheet.LastRowNum)
            {
                var row = sheet.GetRow(dataStartRow);
                if (row == null) break;

                var firstCell = GetCellValue(row.GetCell(0));
                // 如果遇到TRUE/1/FALSE/0/END，说明数据行开始
                if (firstCell == "TRUE" || firstCell == "1" || firstCell == "FALSE" || firstCell == "0" || firstCell == "END")
                    break;

                dataStartRow++;
            }

            for (int rowIdx = dataStartRow; rowIdx <= sheet.LastRowNum; rowIdx++)
            {
                var row = sheet.GetRow(rowIdx);
                if (row == null) continue;

                var exportFlag = GetCellValue(row.GetCell(0));
                if (exportFlag.Equals("END", StringComparison.OrdinalIgnoreCase))
                    break;

                if (exportFlag != "1" && !exportFlag.Equals("TRUE", StringComparison.OrdinalIgnoreCase))
                    continue;

                var rowData = new Dictionary<string, object?>();

                foreach (var field in fields)
                {
                    var cellValue = GetCellValue(row.GetCell(field.ColumnIndex));
                    rowData[field.Name] = ParseValue(cellValue, field.Type);
                }

                sheetData.Rows.Add(rowData);
            }
        }

        return sheetData;
    }

    private TableType ParseTableType(string typeStr)
    {
        // Support both Chinese and English type names
        return typeStr.ToLower() switch
        {
            "参数表" or "config" => TableType.Config,
            "数组表" or "aspect" or "array" => TableType.Aspect,
            "主建表" or "normal" or "main" => TableType.Normal,
            "枚举" or "枚举表" or "enum" => TableType.Enum,
            "分组表" or "group" => TableType.Normal,
            _ => TableType.Normal
        };
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

            // 提取数组元素类型
            var elementType = type.Substring(6, type.Length - 7).ToLower(); // array array<xxx> -> xxx
            
            var elements = content.Split(',').Select(s => s.Trim()).ToList();
            var result = new List<object?>();
            
            foreach (var elem in elements)
            {
                if (string.IsNullOrWhiteSpace(elem))
                {
                    result.Add(null);
                    continue;
                }
                
                // 根据元素类型解析
                result.Add(elementType switch
                {
                    "number" or "int" or "float" or "double" => TryParseNumber(elem),
                    "bool" or "boolean" => elem == "1" || elem.Equals("true", StringComparison.OrdinalIgnoreCase),
                    "string" or "language" => elem,
                    _ => TryParseNumber(elem) // 对于自定义类型(如example.item)，尝试解析为数字
                });
            }
            
            return result;
        }

        return type switch
        {
            "number" or "int" => int.TryParse(value, out var intVal) ? intVal : 0,
            "float" => float.TryParse(value, out var floatVal) ? floatVal : 0f,
            "double" => double.TryParse(value, out var doubleVal) ? doubleVal : 0.0,
            "bool" or "boolean" => value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase),
            "string" or "language" => value,
            _ => TryParseNumber(value)  // 对于自定义类型，尝试解析为数字
        };
    }

    private object? TryParseNumber(string value)
    {
        // 尝试解析为整数
        if (int.TryParse(value, out var intVal))
            return intVal;
        // 尝试解析为浮点数
        if (float.TryParse(value, out var floatVal))
            return floatVal;
        // 如果都失败，返回原始字符串
        return value;
    }
}
