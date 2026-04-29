using System.Text;
using System.Text.Json;
using ExcelToGame.Models;
using ExcelToGame.Utils;

namespace ExcelToGame.Core;

public class CodeGenerator
{
    // C#关键字列表
    private static readonly HashSet<string> CSharpKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
        "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
        "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
        "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is",
        "lock", "long", "namespace", "new", "null", "object", "operator", "out", "override",
        "params", "private", "protected", "public", "readonly", "ref", "return", "sbyte",
        "sealed", "short", "sizeof", "stackalloc", "static", "string", "struct", "switch",
        "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe",
        "ushort", "using", "virtual", "void", "volatile", "while"
    };

    public async Task GenerateTypeScript(ExcelFileData excelData, string outputDir)
    {
        var sb = new StringBuilder();

        sb.AppendLine("import { _lang } from \"./_language\";");
        sb.AppendLine();

        // 生成所有Sheet的类定义
        foreach (var sheet in excelData.DataSheets)
        {
            GenerateTSClassesForSheet(sb, sheet);
        }

        var outputPath = Path.Combine(outputDir, $"_{excelData.OutputName}.ts");
        await FileUtil.WriteTextAsync(outputPath, sb.ToString());
        Logger.Success($"生成TS: {outputPath}");
    }

    private void GenerateTSClassesForSheet(StringBuilder sb, SheetData sheet)
    {
        // 获取安全的字段名列表
        var safeFieldNames = GetSafeFieldNames(sheet.Fields, false);

        // Config类
        sb.AppendLine($"/** {sheet.SheetName} 配置 */");
        sb.AppendLine($"export class _{sheet.SheetName}Config {{");
        sb.AppendLine("    public _data: any;");
        sb.AppendLine();

        for (int i = 0; i < sheet.Fields.Count; i++)
        {
            var field = sheet.Fields[i];
            if (field.IsLanguage) continue;
            
            var safeName = safeFieldNames[i];
            var tsType = field.GetTSType();
            sb.AppendLine($"    get {safeName}(): {tsType} {{");

            if (field.IsArray)
            {
                sb.AppendLine($"        return JSON.parse(this._data['{field.Name}'] || '[]');");
            }
            else
            {
                sb.AppendLine($"        return this._data['{field.Name}'];");
            }

            sb.AppendLine("    }");
            sb.AppendLine();
        }

        sb.AppendLine("}");
        sb.AppendLine();

        // Aspect类
        sb.AppendLine($"/** {sheet.SheetName} 数据项 */");
        sb.AppendLine($"export class _{sheet.SheetName}Aspect {{");
        sb.AppendLine("    public objects: any;");
        sb.AppendLine();

        for (int i = 0; i < sheet.Fields.Count; i++)
        {
            var field = sheet.Fields[i];
            var safeName = safeFieldNames[i];
            var tsType = field.GetTSType();

            if (field.IsLanguage)
            {
                sb.AppendLine($"    get {safeName}(): string {{");
                sb.AppendLine($"        return _lang.t(this.objects['{field.ColumnIndex}']);");
                sb.AppendLine("    }");
            }
            else if (field.IsArray)
            {
                sb.AppendLine($"    get {safeName}(): {tsType} {{");
                sb.AppendLine($"        return JSON.parse(this.objects['{field.ColumnIndex}'] || '[]');");
                sb.AppendLine("    }");
            }
            else if (field.IsCustomType)
            {
                sb.AppendLine($"    get {safeName}(): {tsType} {{");
                sb.AppendLine($"        return JSON.parse(this.objects['{field.ColumnIndex}'] || '{{}}');");
                sb.AppendLine("    }");
            }
            else
            {
                sb.AppendLine($"    get {safeName}(): {tsType} {{");
                sb.AppendLine($"        return this.objects['{field.ColumnIndex}'];");
                sb.AppendLine("    }");
            }

            sb.AppendLine();
        }

        sb.AppendLine("}");
        sb.AppendLine();

        // AspectMap类
        sb.AppendLine($"/** {sheet.SheetName} 数据映射 */");
        sb.AppendLine($"export class _{sheet.SheetName}AspectMap {{");
        sb.AppendLine($"    private _map: Map<number, _{sheet.SheetName}Aspect> = new Map();");
        sb.AppendLine();
        sb.AppendLine($"    set(id: number, aspect: _{sheet.SheetName}Aspect) {{");
        sb.AppendLine("        this._map.set(id, aspect);");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine($"    get(id: number): _{sheet.SheetName}Aspect | undefined {{");
        sb.AppendLine("        return this._map.get(id);");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        sb.AppendLine();

        // 主类
        sb.AppendLine($"/** {sheet.SheetName} */");
        sb.AppendLine($"export class _{sheet.SheetName} {{");
        sb.AppendLine($"    public static Config: _{sheet.SheetName}Config = new _{sheet.SheetName}Config();");
        sb.AppendLine($"    public static Aspect: _{sheet.SheetName}Aspect[] = [];");
        sb.AppendLine($"    public static AspectMap: _{sheet.SheetName}AspectMap = new _{sheet.SheetName}AspectMap();");
        sb.AppendLine();

        sb.AppendLine($"    public static findById(id: number): _{sheet.SheetName}Aspect | undefined {{");
        sb.AppendLine("        return this.AspectMap.get(id);");
        sb.AppendLine("    }");
        sb.AppendLine();

        sb.AppendLine("    public static load(jsonData: any): void {");
        sb.AppendLine("        if (jsonData.setConfig) {");
        sb.AppendLine("            this.Config._data = jsonData.setConfig;");
        sb.AppendLine("        }");
        sb.AppendLine("        if (jsonData.setAspect) {");
        sb.AppendLine("            this.Aspect = [];");
        sb.AppendLine($"            this.AspectMap = new _{sheet.SheetName}AspectMap();");
        sb.AppendLine("            for (let i = 0; i < jsonData.setAspect.length; i++) {");
        sb.AppendLine($"                let aspect = new _{sheet.SheetName}Aspect();");
        sb.AppendLine("                aspect.objects = JSON.parse(jsonData.setAspect[i]);");
        sb.AppendLine("                this.Aspect.push(aspect);");
        sb.AppendLine("                let id = aspect.objects['1'];");
        sb.AppendLine("                this.AspectMap.set(id, aspect);");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("    }");

        sb.AppendLine("}");
        sb.AppendLine();
    }

    public async Task GenerateCSharp(ExcelFileData excelData, string outputDir)
    {
        var sb = new StringBuilder();

        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Text.Json;");
        sb.AppendLine("using System.Text.Json.Serialization;");
        sb.AppendLine();
        sb.AppendLine($"namespace Game.Config");
        sb.AppendLine("{");

        // 生成所有Sheet的类定义
        foreach (var sheet in excelData.DataSheets)
        {
            GenerateCSClassesForSheet(sb, sheet);
        }

        sb.AppendLine("}");

        var outputPath = Path.Combine(outputDir, $"{excelData.OutputName}.cs");
        await FileUtil.WriteTextAsync(outputPath, sb.ToString());
        Logger.Success($"生成C#: {outputPath}");
    }

    private void GenerateCSClassesForSheet(StringBuilder sb, SheetData sheet)
    {
        // 获取安全的字段名列表（处理重复和关键字）
        var safeFieldNames = GetSafeFieldNames(sheet.Fields, true);

        // Config类
        sb.AppendLine($"    /// <summary>{sheet.SheetName} 配置</summary>");
        sb.AppendLine($"    public class {sheet.SheetName}Config");
        sb.AppendLine("    {");

        for (int i = 0; i < sheet.Fields.Count; i++)
        {
            var field = sheet.Fields[i];
            if (field.IsLanguage) continue;
            
            var safeName = safeFieldNames[i];
            var csType = field.GetCSharpType();
            sb.AppendLine($"        public {csType} {safeName} {{ get; set; }}");
        }

        sb.AppendLine("    }");
        sb.AppendLine();

        // Aspect类
        sb.AppendLine($"    /// <summary>{sheet.SheetName} 数据项</summary>");
        sb.AppendLine($"    public class {sheet.SheetName}Aspect");
        sb.AppendLine("    {");

        for (int i = 0; i < sheet.Fields.Count; i++)
        {
            var field = sheet.Fields[i];
            var safeName = safeFieldNames[i];
            var csType = field.GetCSharpType();

            if (field.IsLanguage)
            {
                sb.AppendLine($"        public string {safeName} => LanguageManager.GetText(_{safeName}Key);");
                sb.AppendLine($"        [JsonPropertyName(\"{field.Name}\")] private string _{safeName}Key;");
            }
            else
            {
                sb.AppendLine($"        public {csType} {safeName} {{ get; set; }}");
            }
        }

        sb.AppendLine("    }");
        sb.AppendLine();

        // 主类
        sb.AppendLine($"    /// <summary>{sheet.SheetName}</summary>");
        sb.AppendLine($"    public static class {sheet.SheetName}");
        sb.AppendLine("    {");
        sb.AppendLine($"        public static {sheet.SheetName}Config Config {{ get; private set; }} = new {sheet.SheetName}Config();");
        sb.AppendLine($"        public static List<{sheet.SheetName}Aspect> Aspects {{ get; private set; }} = new List<{sheet.SheetName}Aspect>();");
        sb.AppendLine($"        public static Dictionary<int, {sheet.SheetName}Aspect> AspectMap {{ get; private set; }} = new Dictionary<int, {sheet.SheetName}Aspect>();");
        sb.AppendLine();
        sb.AppendLine($"        public static {sheet.SheetName}Aspect FindById(int id)");
        sb.AppendLine("        {");
        sb.AppendLine("            return AspectMap.TryGetValue(id, out var aspect) ? aspect : null;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        public static void Load(string json)");
        sb.AppendLine("        {");
        sb.AppendLine("            var data = JsonSerializer.Deserialize<Dictionary<string, object>>(json);");
        sb.AppendLine("            if (data.TryGetValue(\"setConfig\", out var config))");
        sb.AppendLine("            {");
        sb.AppendLine($"                Config = JsonSerializer.Deserialize<{sheet.SheetName}Config>(config.ToString());");
        sb.AppendLine("            }");
        sb.AppendLine("            if (data.TryGetValue(\"setAspect\", out var aspects))");
        sb.AppendLine("            {");
        sb.AppendLine($"                Aspects = new List<{sheet.SheetName}Aspect>();");
        sb.AppendLine($"                AspectMap = new Dictionary<int, {sheet.SheetName}Aspect>();");
        sb.AppendLine("                var aspectList = JsonSerializer.Deserialize<List<string>>(aspects.ToString());");
        sb.AppendLine("                foreach (var aspectJson in aspectList)");
        sb.AppendLine("                {");
        sb.AppendLine($"                    var aspect = JsonSerializer.Deserialize<{sheet.SheetName}Aspect>(aspectJson);");
        sb.AppendLine("                    Aspects.Add(aspect);");
        sb.AppendLine("                    if (!AspectMap.ContainsKey(aspect.Id))");
        sb.AppendLine("                        AspectMap.Add(aspect.Id, aspect);");
        sb.AppendLine("                }");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    /// <summary>
    /// 获取安全的字段名列表，处理重复和关键字
    /// </summary>
    private List<string> GetSafeFieldNames(List<FieldInfo> fields, bool checkKeywords = true)
    {
        var result = new List<string>();
        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var field in fields)
        {
            var safeName = GetSafeIdentifier(field.Name);
            
            // 处理C#关键字（仅C#代码生成时使用）
            if (checkKeywords && CSharpKeywords.Contains(safeName))
            {
                safeName = "@" + safeName;
            }
            
            // 处理重复名
            var uniqueName = safeName;
            int counter = 1;
            while (usedNames.Contains(uniqueName))
            {
                uniqueName = $"{safeName}_{counter}";
                counter++;
            }
            
            usedNames.Add(uniqueName);
            result.Add(uniqueName);
        }

        return result;
    }

    /// <summary>
    /// 将字段名转换为有效的标识符
    /// </summary>
    private string GetSafeIdentifier(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "field";

        // 移除或替换非法字符
        var sb = new StringBuilder();
        foreach (char c in name)
        {
            if (char.IsLetterOrDigit(c) || c == '_')
            {
                sb.Append(c);
            }
            else
            {
                sb.Append('_');
            }
        }

        var result = sb.ToString();

        // 如果以数字开头，添加下划线
        if (result.Length > 0 && char.IsDigit(result[0]))
        {
            result = "_" + result;
        }

        // 如果为空，返回默认值
        if (string.IsNullOrWhiteSpace(result))
        {
            return "field";
        }

        return result;
    }

    public async Task GenerateJson(ExcelFileData excelData, string outputDir)
    {
        var result = new Dictionary<string, object?>();

        foreach (var sheet in excelData.DataSheets)
        {
            var sheetData = new Dictionary<string, object?>();

            // Config数据（第一行）
            if (sheet.Rows.Count > 0)
            {
                var config = new Dictionary<string, object?>();
                var firstRow = sheet.Rows.FirstOrDefault();
                if (firstRow != null)
                {
                    foreach (var field in sheet.Fields.Where(f => !f.IsLanguage))
                    {
                        config[field.Name] = firstRow.GetValueOrDefault(field.Name);
                    }
                }
                sheetData["setConfig"] = config;
            }

            // Aspect数据
            var aspects = new List<object>();
            foreach (var row in sheet.Rows)
            {
                var aspectArray = new List<object?>();

                // 第一列是Id
                aspectArray.Add(row.GetValueOrDefault("Id") ?? row.GetValueOrDefault("id") ?? 0);

                foreach (var field in sheet.Fields)
                {
                    var value = row.GetValueOrDefault(field.Name);
                    aspectArray.Add(value);
                }

                aspects.Add(JsonSerializer.Serialize(aspectArray));
            }
            sheetData["setAspect"] = aspects;

            result[sheet.SheetName] = sheetData;
        }

        var json = JsonSerializer.Serialize(result, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        var outputPath = Path.Combine(outputDir, $"{excelData.OutputName}.json");
        await FileUtil.WriteTextAsync(outputPath, json);
        Logger.Success($"生成JSON: {outputPath}");
    }
}

public static class DictionaryExtensions
{
    public static object? GetValueOrDefault(this Dictionary<string, object?> dict, string key)
    {
        return dict.TryGetValue(key, out var value) ? value : null;
    }
}
