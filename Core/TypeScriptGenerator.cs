using System.Text;
using ExcelToGame.Models;
using ExcelToGame.Config;
using ExcelToGame.Utils;

namespace ExcelToGame.Core;

/// <summary>
/// TypeScript代码生成模块 - 生成Cocos Creator使用的Interface和枚举
/// </summary>
public class TypeScriptGenerator
{
    private readonly AppConfig _config;
    
    public TypeScriptGenerator(AppConfig config)
    {
        _config = config;
    }
    
    /// <summary>
    /// 生成TypeScript接口
    /// </summary>
    public async Task<bool> GenerateInterfaceAsync(TableData tableData)
    {
        try
        {
            var outputPath = Path.Combine(_config.GetFullTypeScriptPath(), $"{tableData.ClassName}.ts");
            var code = GenerateInterfaceCode(tableData);
            
            await FileUtils.WriteTextAsync(outputPath, code);
            
            Logger.Success($"生成TypeScript接口: {outputPath}");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error($"生成TypeScript接口失败 [{tableData.ClassName}]: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// 生成接口代码
    /// </summary>
    private string GenerateInterfaceCode(TableData tableData)
    {
        var sb = new StringBuilder();
        
        // 文件头
        sb.AppendLine("/**");
        sb.AppendLine(" * 此文件由Excel转配置工具自动生成，请勿手动修改");
        sb.AppendLine(" */");
        sb.AppendLine();
        
        // 导入枚举
        sb.AppendLine("import { " + string.Join(", ", tableData.GetEnumFields().Select(f => f.EnumName).Distinct()) + " } from './Enums';");
        sb.AppendLine();
        
        // 接口注释
        sb.AppendLine("/**");
        sb.AppendLine($" * {tableData.FileName} 配置数据接口");
        sb.AppendLine(" */");
        
        // 接口声明（支持继承）
        var extends = tableData.ParentTableName != null 
            ? $" extends {tableData.ParentTableName}" 
            : "";
        sb.AppendLine($"export interface {tableData.ClassName}{extends}");
        sb.AppendLine("{");
        
        // 生成字段
        var dataFields = tableData.GetDataFields();
        foreach (var field in dataFields)
        {
            // 字段注释
            if (!string.IsNullOrEmpty(field.Comment))
            {
                sb.AppendLine("    /**");
                sb.AppendLine($"     * {field.Comment}");
                sb.AppendLine("     */");
            }
            
            // 字段声明
            var typeName = field.GetTypeScriptType();
            var optional = field.BaseType == "string" ? "?" : ""; // 字符串类型可选
            sb.AppendLine($"    {field.Name}{optional}: {typeName};");
        }
        
        sb.AppendLine("}");
        sb.AppendLine();
        
        // 生成配置加载器类
        sb.AppendLine("/**");
        sb.AppendLine($" * {tableData.ClassName} 配置加载器");
        sb.AppendLine(" */");
        sb.AppendLine($"export class {tableData.ClassName}Loader");
        sb.AppendLine("{");
        sb.AppendLine("    private static _data: Map<number, " + tableData.ClassName + "> = new Map();");
        sb.AppendLine();
        sb.AppendLine("    /**");
        sb.AppendLine("     * 加载配置数据");
        sb.AppendLine("     */");
        sb.AppendLine("    public static load(jsonData: any[]): void");
        sb.AppendLine("    {");
        sb.AppendLine("        this._data.clear();");
        sb.AppendLine("        for (const item of jsonData)");
        sb.AppendLine("        {");
        sb.AppendLine("            this._data.set(item.id, item as " + tableData.ClassName + ");");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    /**");
        sb.AppendLine("     * 获取配置项");
        sb.AppendLine("     */");
        sb.AppendLine($"    public static get(id: number): {tableData.ClassName} | undefined");
        sb.AppendLine("    {");
        sb.AppendLine("        return this._data.get(id);");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    /**");
        sb.AppendLine("     * 获取所有配置");
        sb.AppendLine("     */");
        sb.AppendLine($"    public static getAll(): {tableData.ClassName}[]");
        sb.AppendLine("    {");
        sb.AppendLine("        return Array.from(this._data.values());");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    /**");
        sb.AppendLine("     * 获取配置数量");
        sb.AppendLine("     */");
        sb.AppendLine("    public static getCount(): number");
        sb.AppendLine("    {");
        sb.AppendLine("        return this._data.size;");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        
        return sb.ToString();
    }
    
    /// <summary>
    /// 生成配置管理器
    /// </summary>
    public async Task<bool> GenerateConfigManagerAsync(List<TableData> tables)
    {
        try
        {
            var outputPath = Path.Combine(_config.GetFullTypeScriptPath(), "ConfigManager.ts");
            var code = GenerateConfigManagerCode(tables);
            
            await FileUtils.WriteTextAsync(outputPath, code);
            
            Logger.Success($"生成TypeScript配置管理器: {outputPath}");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error($"生成TypeScript配置管理器失败: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// 生成配置管理器代码
    /// </summary>
    private string GenerateConfigManagerCode(List<TableData> tables)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("/**");
        sb.AppendLine(" * 此文件由Excel转配置工具自动生成，请勿手动修改");
        sb.AppendLine(" */");
        sb.AppendLine();
        
        // 导入所有接口
        foreach (var table in tables)
        {
            sb.AppendLine($"import {{ {table.ClassName}, {table.ClassName}Loader }} from './{table.ClassName}';");
        }
        sb.AppendLine();
        
        sb.AppendLine("/**");
        sb.AppendLine(" * 配置管理器");
        sb.AppendLine(" */");
        sb.AppendLine("export class ConfigManager");
        sb.AppendLine("{");
        sb.AppendLine("    private static _isLoaded: boolean = false;");
        sb.AppendLine();
        sb.AppendLine("    /**");
        sb.AppendLine("     * 加载所有配置");
        sb.AppendLine("     */");
        sb.AppendLine("    public static async loadAll(): Promise<void>");
        sb.AppendLine("    {");
        sb.AppendLine("        if (this._isLoaded) return;");
        sb.AppendLine();
        
        foreach (var table in tables)
        {
            sb.AppendLine($"        const {ToCamelCase(table.ClassName)}Json = await this.loadJson('config/{table.ClassName}');");
            sb.AppendLine($"        {table.ClassName}Loader.load({ToCamelCase(table.ClassName)}Json);");
            sb.AppendLine();
        }
        
        sb.AppendLine("        this._isLoaded = true;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    /**");
        sb.AppendLine("     * 加载JSON文件");
        sb.AppendLine("     */");
        sb.AppendLine("    private static async loadJson(path: string): Promise<any>");
        sb.AppendLine("    {");
        sb.AppendLine("        return new Promise((resolve, reject) =>");
        sb.AppendLine("        {");
        sb.AppendLine("            cc.resources.load(path, cc.JsonAsset, (err, asset) =>");
        sb.AppendLine("            {");
        sb.AppendLine("                if (err)");
        sb.AppendLine("                {");
        sb.AppendLine("                    reject(err);");
        sb.AppendLine("                    return;");
        sb.AppendLine("                }");
        sb.AppendLine("                resolve(asset.json);");
        sb.AppendLine("            });");
        sb.AppendLine("        });");
        sb.AppendLine("    }");
        sb.AppendLine();
        
        // 生成各配置表的获取方法
        foreach (var table in tables)
        {
            sb.AppendLine("    /**");
            sb.AppendLine($"     * 获取 {table.ClassName} 配置");
            sb.AppendLine("     */");
            sb.AppendLine($"    public static get{table.ClassName}(id: number): {table.ClassName} | undefined");
            sb.AppendLine("    {");
            sb.AppendLine($"        return {table.ClassName}Loader.get(id);");
            sb.AppendLine("    }");
            sb.AppendLine();
        }
        
        sb.AppendLine("}");
        
        return sb.ToString();
    }
    
    /// <summary>
    /// 转换为驼峰命名
    /// </summary>
    private string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;
        
        return char.ToLowerInvariant(name[0]) + name.Substring(1);
    }
}
