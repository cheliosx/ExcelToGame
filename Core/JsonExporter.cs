using System.Text.Json;
using ExcelToGame.Models;
using ExcelToGame.Config;
using ExcelToGame.Utils;

namespace ExcelToGame.Core;

/// <summary>
/// JSON导出模块
/// </summary>
public class JsonExporter
{
    private readonly AppConfig _config;
    private readonly JsonSerializerOptions _jsonOptions;
    
    public JsonExporter(AppConfig config)
    {
        _config = config;
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = config.FormatJson,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }
    
    /// <summary>
    /// 导出表格为JSON
    /// </summary>
    public async Task<bool> ExportAsync(TableData tableData)
    {
        try
        {
            var outputPath = Path.Combine(_config.GetFullJsonPath(), $"{tableData.ClassName}.json");
            
            // 准备导出数据
            var exportData = PrepareExportData(tableData);
            
            // 序列化为JSON
            var json = JsonSerializer.Serialize(exportData, _jsonOptions);
            
            // 写入文件
            await FileUtils.WriteTextAsync(outputPath, json);
            
            Logger.Success($"导出JSON: {outputPath}");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error($"导出JSON失败 [{tableData.ClassName}]: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// 准备导出数据
    /// </summary>
    private List<Dictionary<string, object?>> PrepareExportData(TableData tableData)
    {
        var result = new List<Dictionary<string, object?>>();
        var dataFields = tableData.GetDataFields();
        
        foreach (var row in tableData.GetExportableRows())
        {
            var rowData = new Dictionary<string, object?>();
            
            foreach (var field in dataFields)
            {
                var value = row.GetValue(field.Name);
                
                // 转换值为JSON兼容格式
                rowData[field.Name] = ConvertToJsonValue(value, field);
            }
            
            result.Add(rowData);
        }
        
        return result;
    }
    
    /// <summary>
    /// 转换为JSON兼容值
    /// </summary>
    private object? ConvertToJsonValue(object? value, FieldInfo field)
    {
        if (value == null)
        {
            return null;
        }
        
        // 数组类型处理
        if (field.IsArray && value is List<object?> arrayValue)
        {
            return arrayValue.Select(item => ConvertSingleValue(item, field)).ToList();
        }
        
        return ConvertSingleValue(value, field);
    }
    
    /// <summary>
    /// 转换单个值
    /// </summary>
    private object? ConvertSingleValue(object? value, FieldInfo field)
    {
        if (value == null)
        {
            return null;
        }
        
        // 自定义对象类型
        if (field.IsCustomType && value is Dictionary<string, object?> dictValue)
        {
            return dictValue;
        }
        
        // 枚举类型作为字符串输出
        if (field.IsEnum)
        {
            return value.ToString();
        }
        
        // 基础类型直接返回
        return value;
    }
}
