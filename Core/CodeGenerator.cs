using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using ExcelToGame.Models;
using ExcelToGame.Utils;

namespace ExcelToGame.Core;

/// <summary>
/// 按 TableSchema 生成 JSON / TypeScript / C# / Java 产物。
/// 大数据表不在加载时展开：整表只保留一份行文本 + 行偏移表，访问哪行才解析哪行。
/// </summary>
public partial class CodeGenerator
{
    private static readonly HashSet<string> CSharpKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
        "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
        "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
        "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
        "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
        "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
        "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true",
        "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual",
        "void", "volatile", "while"
    };

    // ---------------------------------------------------------------- JSON

    public async Task GenerateJson(ExcelFileData file, List<TableSchema> schemas, string outputDir)
    {
        var result = new Dictionary<string, object?>();

        // 枚举已经生成成代码里的 enum，不再重复写进 JSON
        foreach (var schema in schemas.Where(s => s.Kind != TableKind.Enum))
        {
            // 参数表输出字段对象；其余表输出字符串数组，一行一条
            result[schema.Name] = schema.Kind == TableKind.Param
                ? BuildParamFields(schema)
                : BuildRowLines(schema);
        }

        var outputPath = Path.Combine(outputDir, $"{file.OutputName}.json");
        await FileUtil.WriteTextAsync(outputPath, ToReadableJson(result));
        Logger.Success($"生成JSON: {Path.GetFileName(outputPath)}");
    }

    /// <summary>单值序列化选项：保留中文，不转义成 \uXXXX</summary>
    private static readonly JsonSerializerOptions InlineJsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>
    /// 表结构（kind/columns/count/ids/fields）逐行缩进，行数据 data 保持单行：
    /// 既能一眼看出每张表的字段，也不会因为缩进让几十万行数据膨胀数倍。
    /// </summary>
    private static string ToReadableJson(Dictionary<string, object?> root)
    {
        var sb = new StringBuilder();
        sb.Append("{\n");
        AppendMapBody(sb, root, 1);
        sb.Append("}\n");
        return sb.ToString();
    }

    private static void AppendMapBody(StringBuilder sb, Dictionary<string, object?> map, int indent)
    {
        var pad = new string(' ', indent * 2);
        var index = 0;

        foreach (var (key, value) in map)
        {
            sb.Append(pad)
              .Append(JsonSerializer.Serialize(key, InlineJsonOptions))
              .Append(": ");

            AppendValue(sb, value, indent);
            sb.Append(index++ < map.Count - 1 ? ",\n" : "\n");
        }
    }

    private static void AppendValue(StringBuilder sb, object? value, int indent)
    {
        if (value is Dictionary<string, object?> map)
        {
            if (map.Count == 0)
            {
                sb.Append("{}");
                return;
            }

            sb.Append("{\n");
            AppendMapBody(sb, map, indent + 1);
            sb.Append(new string(' ', indent * 2)).Append('}');
            return;
        }

        // 行数据逐条一行，便于 diff
        if (value is RowLines lines)
        {
            if (lines.Value.Count == 0)
            {
                sb.Append("[]");
                return;
            }

            var pad = new string(' ', (indent + 1) * 2);
            sb.Append("[\n");

            for (int i = 0; i < lines.Value.Count; i++)
            {
                sb.Append(pad).Append(JsonSerializer.Serialize(lines.Value[i], InlineJsonOptions));
                sb.Append(i < lines.Value.Count - 1 ? ",\n" : "\n");
            }

            sb.Append(new string(' ', indent * 2)).Append(']');
            return;
        }

        sb.Append(JsonSerializer.Serialize(value, InlineJsonOptions));
    }

    /// <summary>
    /// 行数据：整表就是字符串数组，每项是一条紧凑 JSON 数组文本。
    /// 客户端只在这一行被访问时才 JSON.parse，不会一次性展开成对象。
    /// </summary>
    private static RowLines BuildRowLines(TableSchema schema)
    {
        var lines = new RowLines();

        foreach (var row in schema.Rows)
        {
            lines.Value.Add(JsonSerializer.Serialize(row, InlineJsonOptions));
        }

        return lines;
    }

    /// <summary>逐行输出的行数据：序列化成多行字符串数组，其余数组仍保持单行</summary>
    private sealed class RowLines
    {
        public List<string> Value { get; init; } = new();
    }

    private static Dictionary<string, object?> BuildParamFields(TableSchema schema)
    {
        var fields = new Dictionary<string, object?>();
        var row = schema.Rows.FirstOrDefault();
        if (row == null)
        {
            return fields;
        }

        foreach (var column in schema.Columns)
        {
            fields[column.Name] = row[column.Index];
        }

        return fields;
    }

    // ---------------------------------------------------------------- TypeScript

    public async Task GenerateTypeScript(ExcelFileData file, List<TableSchema> schemas, string outputDir)
    {
        var sb = new StringBuilder();
        var hasLanguage = schemas.Any(s => s.Columns.Any(c => c.Type.Kind == ValueKind.Language));
        var usesEnums = UsesEnums(schemas);

        if (hasLanguage)
        {
            sb.AppendLine("import { _lang } from \"./_language\";");
        }

        // 枚举已统一生成到 _enums.ts 的 TableEnums 命名空间下
        if (usesEnums)
        {
            sb.AppendLine("import { TableEnums } from \"./_enums\";");
        }

        // 主键表 / 分组表要从行文本里取首列数字
        var needsParseKey = schemas.Any(s => s.Kind is TableKind.Keyed or TableKind.Group);
        if (needsParseKey)
        {
            sb.AppendLine("import { _parseKey } from \"./_runtime\";");
        }

        if (hasLanguage || usesEnums || needsParseKey)
        {
            sb.AppendLine();
        }

        sb.AppendLine("/***");
        sb.AppendLine("* 该文件由工具自动生成，请勿手动修改");
        sb.AppendLine("*/");
        sb.AppendLine();

        // 每个页签一个实体类（_表名），平铺导出，不再套 namespace
        foreach (var schema in schemas.Where(s => s.Kind is not (TableKind.Enum or TableKind.Param)))
        {
            WriteTsEntity(sb, schema);
        }

        foreach (var schema in schemas.Where(s => s.Kind == TableKind.Param))
        {
            WriteTsParam(sb, schema);
        }

        foreach (var schema in schemas.Where(s => s.Kind == TableKind.Keyed))
        {
            WriteTsMap(sb, schema);
        }

        foreach (var schema in schemas.Where(s => s.Kind == TableKind.Array))
        {
            WriteTsList(sb, schema);
        }

        foreach (var schema in schemas.Where(s => s.Kind == TableKind.Group))
        {
            WriteTsGroup(sb, schema);
        }

        WriteTsRoot(sb, file, schemas);

        var outputPath = Path.Combine(outputDir, $"_{file.OutputName}.ts");
        await FileUtil.WriteTextAsync(outputPath, sb.ToString());
        Logger.Success($"生成TS: {Path.GetFileName(outputPath)}");
    }

    /// <summary>共享运行时：从行文本里取首列数字，主键表与分组表都用它</summary>
    private static void WriteTsRuntime(StringBuilder sb)
    {
        sb.AppendLine("/** 从 \"[1,...]\" 里取首列数字（主键 / 分组键） */");
        sb.AppendLine("export function _parseKey(row: string): number {");
        sb.AppendLine("    const end = row.indexOf(\",\");");
        sb.AppendLine("    return parseInt(row.substring(1, end < 0 ? row.length : end));");
        sb.AppendLine("}");
        sb.AppendLine();
    }

    /// <summary>本文件的实体是否引用了枚举（决定要不要 import ./_enums）</summary>
    private static bool UsesEnums(List<TableSchema> schemas) =>
        schemas.Any(s => s.Columns.Any(c => c.Type.Kind == ValueKind.Enum));

    /// <summary>生成 TS 端统一入口 ExcelData.ts，登记目录下所有表格文件</summary>
    public async Task GenerateEntry(List<ExcelFileData> files, string tsDir)
    {
        var sb = new StringBuilder();

        sb.AppendLine("import { _lang } from \"./_language\";");

        foreach (var file in files)
        {
            var name = Ident(file.OutputName);
            sb.AppendLine($"import {{ _{name} }} from \"./_{name}\";");
        }

        sb.AppendLine();
        sb.AppendLine("/***");
        sb.AppendLine("* 该文件由工具自动生成，请勿手动修改");
        sb.AppendLine("*/");
        sb.AppendLine("/** 单个表格文件的数据入口需要实现的契约 */");
        sb.AppendLine("interface ITableContainer {");
        sb.AppendLine("    load(data: any): void;");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("export class ExcelData {");
        sb.AppendLine("    /** 当前语言码，如 zh-Hans / en-US */");
        sb.AppendLine("    protected _langCode: string = \"zh-Hans\";");
        sb.AppendLine();

        foreach (var file in files)
        {
            var name = Ident(file.OutputName);
            sb.AppendLine($"    protected readonly _{name}: _{name} = new _{name}();");
        }

        sb.AppendLine();
        sb.AppendLine("    /** 文件名 -> 数据入口 */");
        sb.AppendLine("    protected readonly _files: Record<string, ITableContainer> = {");

        foreach (var file in files)
        {
            var name = Ident(file.OutputName);
            sb.AppendLine($"        {name.ToLowerInvariant()}: this._{name},");
        }

        sb.AppendLine("    };");
        sb.AppendLine();
        sb.AppendLine("    /**");
        sb.AppendLine("     * 加载一个表格文件的 JSON（文件名不区分大小写）");
        sb.AppendLine("     * @param fileName 文件名（输出名，如 \"example\"）");
        sb.AppendLine("     * @param data 该文件对应的 JSON");
        sb.AppendLine("     */");
        sb.AppendLine("    public init(fileName: string, data: any): void {");
        sb.AppendLine("        const file = this._files[fileName.toLowerCase()];");
        sb.AppendLine("        if (file === undefined) {");
        sb.AppendLine("            throw new Error(`ExcelData: 未找到配置文件 \"${fileName}\"`);");
        sb.AppendLine("        }");
        sb.AppendLine("        file.load(data);");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    /**");
        sb.AppendLine("     * 初始化多语言");
        sb.AppendLine("     * @param lang 语言码，如 \"zh-Hans\" / \"en-US\"");
        sb.AppendLine("     * @param texts 该语言的 key -> 文本");
        sb.AppendLine("     */");
        sb.AppendLine("    public initLanguage(lang: string, texts: Record<string, string>): void {");
        sb.AppendLine("        this._langCode = lang;");
        sb.AppendLine("        _lang.load(texts);");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    /** 当前语言码 */");
        sb.AppendLine("    public get lang(): string {");
        sb.AppendLine("        return this._langCode;");
        sb.AppendLine("    }");

        foreach (var file in files)
        {
            var name = Ident(file.OutputName);
            sb.AppendLine();
            sb.AppendLine($"    public get {name}(): _{name} {{");
            sb.AppendLine($"        return this._{name};");
            sb.AppendLine("    }");
        }

        sb.AppendLine("}");
        sb.AppendLine();

        var outputPath = Path.Combine(tsDir, "ExcelData.ts");
        await FileUtil.WriteTextAsync(outputPath, sb.ToString());
        Logger.Success($"生成TS入口: {Path.GetFileName(outputPath)}");
    }

    /// <summary>生成 C# 端统一入口 ExcelData.cs，登记目录下所有表格文件</summary>
    public async Task GenerateCsEntry(List<ExcelFileData> files, string csDir)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated>");
        sb.AppendLine("//     表格统一入口，由 ExcelToGame 生成，请勿手动修改");
        sb.AppendLine("// </auto-generated>");
        sb.AppendLine("#nullable disable");
        sb.AppendLine("using System;");
        sb.AppendLine();
        sb.AppendLine("namespace Game.Config");
        sb.AppendLine("{");
        sb.AppendLine("    /// <summary>所有表格文件的统一入口</summary>");
        sb.AppendLine("    public sealed class ExcelData");
        sb.AppendLine("    {");

        foreach (var file in files)
        {
            var type = Pascal(file.OutputName) + "Config";
            sb.AppendLine($"        public {type} {CsFileMember(file)} {{ get; }} = new {type}();");
        }

        sb.AppendLine();
        sb.AppendLine("        /// <summary>当前语言码，如 zh-Hans / en-US</summary>");
        sb.AppendLine("        public string Lang { get; private set; } = \"zh-Hans\";");
        sb.AppendLine();
        sb.AppendLine("        /// <summary>初始化多语言</summary>");
        sb.AppendLine("        /// <param name=\"lang\">语言码，如 \"zh-Hans\" / \"en-US\"</param>");
        sb.AppendLine("        /// <param name=\"json\">该语言的 key -> 文本</param>");
        sb.AppendLine("        public void InitLanguage(string lang, string json)");
        sb.AppendLine("        {");
        sb.AppendLine("            Lang = lang;");
        sb.AppendLine("            LanguageManager.Load(json);");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        /// <summary>按文件名加载一个表格的 JSON（文件名不区分大小写）</summary>");
        sb.AppendLine("        /// <param name=\"fileName\">文件名（输出名，如 \"example\"）</param>");
        sb.AppendLine("        /// <param name=\"json\">该文件对应的 JSON</param>");
        sb.AppendLine("        public void Load(string fileName, string json)");
        sb.AppendLine("        {");
        sb.AppendLine("            switch (fileName.ToLowerInvariant())");
        sb.AppendLine("            {");

        foreach (var file in files)
        {
            var key = Ident(file.OutputName).ToLowerInvariant();
            sb.AppendLine($"                case \"{key}\": {CsFileMember(file)}.Load(json); break;");
        }

        sb.AppendLine("                default:");
        sb.AppendLine("                    throw new ArgumentException(\"ExcelData: 未找到配置文件 \" + fileName);");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        var outputPath = Path.Combine(csDir, "ExcelData.cs");
        await FileUtil.WriteTextAsync(outputPath, sb.ToString());
        Logger.Success($"生成C#入口: {Path.GetFileName(outputPath)}");
    }

    /// <summary>统一入口里的属性名，与 TS 的 getter 同名（example / attr）</summary>
    private static string CsFileMember(ExcelFileData file) =>
        SafeCsIdentifier(Pascal(file.OutputName));

    /// <summary>TS 共用运行时：输出 TS/_runtime.ts</summary>
    public async Task GenerateTsRuntime(string tsDir)
    {
        var ts = new StringBuilder();
        ts.AppendLine("/***");
        ts.AppendLine("* 该文件由工具自动生成，请勿手动修改");
        ts.AppendLine("*/");
        WriteTsRuntime(ts);

        var tsPath = Path.Combine(tsDir, "_runtime.ts");
        await FileUtil.WriteTextAsync(tsPath, ts.ToString());
        Logger.Success($"生成TS运行时: {Path.GetFileName(tsPath)}");
    }

    /// <summary>C# 共用运行时：输出 CSharp/Runtime.cs</summary>
    public async Task GenerateCsRuntime(string csDir)
    {
        var cs = new StringBuilder();
        cs.AppendLine("// <auto-generated>");
        cs.AppendLine("//     运行时，由 ExcelToGame 生成，请勿手动修改");
        cs.AppendLine("// </auto-generated>");
        cs.AppendLine("#nullable disable");
        cs.AppendLine("using System;");
        cs.AppendLine("using System.Collections.Generic;");
        cs.AppendLine("using System.Text.Json;");
        cs.AppendLine();
        cs.AppendLine("namespace Game.Config");
        cs.AppendLine("{");
        WriteCsRuntime(cs);
        cs.AppendLine("}");

        var csPath = Path.Combine(csDir, "Runtime.cs");
        await FileUtil.WriteTextAsync(csPath, cs.ToString());
        Logger.Success($"生成C#运行时: {Path.GetFileName(csPath)}");
    }

    /// <summary>所有枚举统一输出到 TS/_enums.ts</summary>
    public async Task GenerateTsEnums(List<SheetData> sheets, string tsDir)
    {
        var enums = sheets.Where(s => s.Kind == TableKind.Enum).ToList();
        if (enums.Count == 0)
        {
            Logger.Warning("没有枚举表，跳过枚举文件生成");
            return;
        }

        var ts = new StringBuilder();
        ts.AppendLine("/***");
        ts.AppendLine("* 该文件由工具自动生成，请勿手动修改");
        ts.AppendLine("*/");
        ts.AppendLine("/**");
        ts.AppendLine(" * 表格导出的枚举统一放在 TableEnums 命名空间下，");
        ts.AppendLine(" * 与手写枚举区分开，也避免命名冲突。");
        ts.AppendLine(" */");
        ts.AppendLine("export namespace TableEnums {");
        foreach (var sheet in enums)
        {
            AppendTsEnum(ts, sheet);
        }

        ts.AppendLine("}");

        var tsPath = Path.Combine(tsDir, "_enums.ts");
        await FileUtil.WriteTextAsync(tsPath, ts.ToString());
        Logger.Success($"生成TS枚举: {Path.GetFileName(tsPath)}");
    }

    /// <summary>所有枚举统一输出到 CSharp/Enums.cs</summary>
    public async Task GenerateCsEnums(List<SheetData> sheets, string csDir)
    {
        var enums = sheets.Where(s => s.Kind == TableKind.Enum).ToList();
        if (enums.Count == 0)
        {
            return;
        }

        var cs = new StringBuilder();
        cs.AppendLine("// <auto-generated>");
        cs.AppendLine("//     枚举定义，由 ExcelToGame 生成，请勿手动修改");
        cs.AppendLine("// </auto-generated>");
        cs.AppendLine("#nullable disable");
        cs.AppendLine();
        cs.AppendLine("namespace Game.Config");
        cs.AppendLine("{");
        foreach (var sheet in enums)
        {
            AppendCsEnum(cs, sheet);
        }

        cs.AppendLine("}");

        var csPath = Path.Combine(csDir, "Enums.cs");
        await FileUtil.WriteTextAsync(csPath, cs.ToString());
        Logger.Success($"生成C#枚举: {Path.GetFileName(csPath)}");
    }

    private static void AppendTsEnum(StringBuilder sb, SheetData sheet)
    {
        var name = Ident(sheet.SheetName);
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        sb.AppendLine($"    /** {sheet.ChineseName} */");
        sb.AppendLine($"    export enum {name} {{");

        foreach (var entry in sheet.EnumEntries)
        {
            var member = UniqueName(Ident(entry.Name), used);
            if (!string.IsNullOrWhiteSpace(entry.Comment))
            {
                sb.AppendLine($"        /** {entry.Comment} */");
            }

            sb.AppendLine($"        {member} = {entry.Value},");
        }

        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void AppendCsEnum(StringBuilder sb, SheetData sheet)
    {
        var name = Pascal(sheet.SheetName);
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        sb.AppendLine($"    /// <summary>{sheet.ChineseName}</summary>");
        sb.AppendLine($"    public enum {name}");
        sb.AppendLine("    {");

        foreach (var entry in sheet.EnumEntries)
        {
            var member = UniqueName(SafeCsIdentifier(Pascal(entry.Name)), used);
            sb.AppendLine($"        /// <summary>{entry.Comment}</summary>");
            sb.AppendLine($"        {member} = {entry.Value},");
        }

        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private void WriteTsEntity(StringBuilder sb, TableSchema schema)
    {
        var className = "_" + Ident(schema.Name);
        var fieldNames = BuildTsFieldNames(schema);

        sb.AppendLine($"/** {schema.Sheet.ChineseName} */");
        sb.AppendLine($"export class {className} {{");
        sb.AppendLine("    /** 原始行文本；首次解析后置空以释放内存 */");
        sb.AppendLine("    public _data: string | null;");
        sb.AppendLine("    private _object: any[] | null = null;");
        sb.AppendLine();
        sb.AppendLine("    constructor(data: string) {");
        sb.AppendLine("        this._data = data;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    /** 首次访问字段时才解析本行 */");
        sb.AppendLine("    private get objects(): any[] {");
        sb.AppendLine("        let values = this._object;");
        sb.AppendLine("        if (values === null) {");
        sb.AppendLine("            const data = this._data;");
        sb.AppendLine("            if (data === null) {");
        sb.AppendLine($"                throw new Error(\"{className}: 原始数据已释放，无法再解析\");");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            values = JSON.parse(data) as any[];");
        sb.AppendLine("            this._object = values;");
        sb.AppendLine("            this._data = null;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        return values;");
        sb.AppendLine("    }");

        foreach (var column in schema.Columns)
        {
            var field = column.Field;
            var member = fieldNames[Key(column)];

            sb.AppendLine();
            sb.AppendLine($"    /** {CommentOf(field)} */");
            sb.AppendLine($"    get {member}(): {TsTypeOf(column.Type)} {{");
            sb.AppendLine($"        return {TsValueOf(column)};");
            sb.AppendLine("    }");
        }

        sb.AppendLine("}");
        sb.AppendLine();
    }

    private void WriteTsParam(StringBuilder sb, TableSchema schema)
    {
        var className = "_" + Ident(schema.Name);
        var fieldNames = BuildTsFieldNames(schema);

        sb.AppendLine($"/** {schema.Sheet.ChineseName} */");
        sb.AppendLine($"export class {className} {{");
        sb.AppendLine("    public _data: any;");

        foreach (var column in schema.Columns)
        {
            var field = column.Field;
            var member = fieldNames[Key(column)];
            var access = "this._data" + FieldAccess(field.Name);

            sb.AppendLine();
            sb.AppendLine($"    /** {CommentOf(field)} */");
            sb.AppendLine($"    get {member}(): {TsTypeOf(column.Type)} {{");
            sb.AppendLine(column.Type.Kind == ValueKind.Language
                ? $"        return _lang.t({access});"
                : $"        return {access};");
            sb.AppendLine("    }");
        }

        sb.AppendLine("}");
        sb.AppendLine();
    }

    /// <summary>字段访问：合法标识符用点号，否则用方括号</summary>
    private static string FieldAccess(string name) =>
        IsIdentifier(name) ? $".{name}" : $"[\"{name}\"]";

    private static bool IsIdentifier(string name) =>
        name.Length > 0 &&
        (char.IsLetter(name[0]) || name[0] == '_') &&
        name.All(c => char.IsLetterOrDigit(c) || c == '_');

    private void WriteTsMap(StringBuilder sb, TableSchema schema)
    {
        var entity = "_" + Ident(schema.Name);
        var className = entity + "Map";

        sb.AppendLine($"/** {schema.Sheet.ChineseName} */");
        sb.AppendLine($"export class {className} {{");
        sb.AppendLine("    /** 原始行数据，按需解析 */");
        sb.AppendLine("    private _rows: string[] = [];");
        sb.AppendLine();
        sb.AppendLine("    /** 主键 -> 行号 */");
        sb.AppendLine("    private _index: Map<number, number> = new Map();");
        sb.AppendLine();
        sb.AppendLine("    /** 已访问过的行（只缓存用到的实体） */");
        sb.AppendLine($"    private _data: Map<number, {entity}> = new Map();");
        sb.AppendLine();
        sb.AppendLine("    public init(_data: any): void {");
        sb.AppendLine("        this._rows = _data || [];");
        sb.AppendLine("        this._index = new Map();");
        sb.AppendLine("        this._data = new Map();");
        sb.AppendLine();
        sb.AppendLine("        for (let i = 0; i < this._rows.length; i++) {");
        sb.AppendLine("            this._index.set(_parseKey(this._rows[i]), i);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    public get count(): number {");
        sb.AppendLine("        return this._rows.length;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    /** 按主键取一行：定位到行号后才创建实体并解析 */");
        sb.AppendLine($"    public findById(id: number): Readonly<{entity}> | undefined {{");
        sb.AppendLine("        const cached = this._data.get(id);");
        sb.AppendLine("        if (cached !== undefined) {");
        sb.AppendLine("            return cached;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        const row = this._index.get(id);");
        sb.AppendLine("        if (row === undefined) {");
        sb.AppendLine("            return undefined;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine($"        const entity = new {entity}(this._rows[row]);");
        sb.AppendLine("        this._data.set(id, entity);");
        sb.AppendLine("        return entity;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    /** 全表遍历：逐行创建实体，不驻留 */");
        sb.AppendLine($"    public forEach(callback: (row: {entity}, index: number) => void): void {{");
        sb.AppendLine("        for (let i = 0; i < this._rows.length; i++) {");
        sb.AppendLine($"            callback(new {entity}(this._rows[i]), i);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        sb.AppendLine();
    }

    private void WriteTsList(StringBuilder sb, TableSchema schema)
    {
        var entity = "_" + Ident(schema.Name);
        var className = entity + "List";

        sb.AppendLine($"/** {schema.Sheet.ChineseName} */");
        sb.AppendLine($"export class {className} {{");
        sb.AppendLine("    /** 原始行数据，按需解析 */");
        sb.AppendLine("    private _rows: string[] = [];");
        sb.AppendLine();
        sb.AppendLine("    /** 已访问过的行（只缓存用到的实体） */");
        sb.AppendLine($"    private _data: Map<number, {entity}> = new Map();");
        sb.AppendLine();
        sb.AppendLine("    public init(_data: any[]): void {");
        sb.AppendLine("        this._rows = _data || [];");
        sb.AppendLine("        this._data = new Map();");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    public get count(): number {");
        sb.AppendLine("        return this._rows.length;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine($"    public findByIndex(index: number): Readonly<{entity}> | undefined {{");
        sb.AppendLine("        if (index < 0 || index >= this._rows.length) {");
        sb.AppendLine("            return undefined;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        let entity = this._data.get(index);");
        sb.AppendLine("        if (entity === undefined) {");
        sb.AppendLine($"            entity = new {entity}(this._rows[index]);");
        sb.AppendLine("            this._data.set(index, entity);");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        return entity;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine($"    public forEach(callback: (row: {entity}, index: number) => void): void {{");
        sb.AppendLine("        for (let i = 0; i < this._rows.length; i++) {");
        sb.AppendLine($"            callback(new {entity}(this._rows[i]), i);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        sb.AppendLine();
    }

    private void WriteTsGroup(StringBuilder sb, TableSchema schema)
    {
        var entity = "_" + Ident(schema.Name);
        var className = entity + "Group";

        sb.AppendLine($"/** {schema.Sheet.ChineseName} */");
        sb.AppendLine($"export class {className} {{");
        sb.AppendLine("    /** 原始行数据，按需解析 */");
        sb.AppendLine("    private _rows: string[] = [];");
        sb.AppendLine();
        sb.AppendLine("    /** 分组键 -> 行号列表 */");
        sb.AppendLine("    private _index: Map<number, number[]> = new Map();");
        sb.AppendLine();
        sb.AppendLine("    /** 已访问过的行（只缓存用到的实体） */");
        sb.AppendLine($"    private _data: Map<number, {entity}> = new Map();");
        sb.AppendLine();
        sb.AppendLine("    public init(_data: any[]): void {");
        sb.AppendLine("        this._rows = _data || [];");
        sb.AppendLine("        this._index = new Map();");
        sb.AppendLine("        this._data = new Map();");
        sb.AppendLine();
        sb.AppendLine("        for (let i = 0; i < this._rows.length; i++) {");
        sb.AppendLine("            const id = _parseKey(this._rows[i]);");
        sb.AppendLine("            let temp = this._index.get(id);");
        sb.AppendLine("            if (temp === undefined) {");
        sb.AppendLine("                temp = [];");
        sb.AppendLine("                this._index.set(id, temp);");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            temp.push(i);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    public get count(): number {");
        sb.AppendLine("        return this._rows.length;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine($"    public findGroupById(id: number): ReadonlyArray<{entity}> | undefined {{");
        sb.AppendLine("        const rows = this._index.get(id);");
        sb.AppendLine("        if (rows === undefined) {");
        sb.AppendLine("            return undefined;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine($"        const result: {entity}[] = [];");
        sb.AppendLine("        for (let i = 0; i < rows.length; i++) {");
        sb.AppendLine("            result.push(this._entity(rows[i]));");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        return result;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    /** 同一行只创建一次实体 */");
        sb.AppendLine($"    private _entity(row: number): {entity} {{");
        sb.AppendLine($"        let entity = this._data.get(row);");
        sb.AppendLine("        if (entity === undefined) {");
        sb.AppendLine($"            entity = new {entity}(this._rows[row]);");
        sb.AppendLine("            this._data.set(row, entity);");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        return entity;");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        sb.AppendLine();
    }

    private void WriteTsRoot(StringBuilder sb, ExcelFileData file, List<TableSchema> schemas)
    {
        var className = "_" + Ident(file.OutputName);
        var containers = schemas.Where(s => s.Kind != TableKind.Enum).ToList();

        sb.AppendLine($"/** {file.FileName} */");
        sb.AppendLine($"export class {className} {{");

        foreach (var schema in containers)
        {
            var type = TsContainerType(schema);
            sb.AppendLine($"    /** {schema.Sheet.ChineseName} */");
            sb.AppendLine($"    public {TsMemberName(schema)}: {type} = new {type}();");
        }

        sb.AppendLine();
        sb.AppendLine("    public load(data: any): void {");
        foreach (var schema in containers)
        {
            var member = TsMemberName(schema);
            var key = Ident(schema.Name);

            sb.AppendLine(schema.Kind == TableKind.Param
                ? $"        this.{member}._data = data.{key};"
                : $"        this.{member}.init(data.{key});");
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");
    }

    /// <summary>根类里的字段名：表名 + 容器后缀（参数表无后缀）</summary>
    private static string TsMemberName(TableSchema schema) => schema.Kind switch
    {
        TableKind.Keyed => Ident(schema.Name) + "Map",
        TableKind.Array => Ident(schema.Name) + "List",
        TableKind.Group => Ident(schema.Name) + "Group",
        _ => Ident(schema.Name)
    };

    private static string TsContainerType(TableSchema schema) => "_" + TsMemberName(schema);

    // ---------------------------------------------------------------- C#

    public async Task GenerateCSharp(ExcelFileData file, List<TableSchema> schemas, string outputDir)
    {
        var sb = new StringBuilder();
        var rootClass = Pascal(file.OutputName) + "Config";

        sb.AppendLine("// <auto-generated>");
        sb.AppendLine($"//     {file.FileName}，由 ExcelToGame 生成，请勿手动修改");
        sb.AppendLine("// </auto-generated>");
        sb.AppendLine("#nullable disable");
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Text.Json;");
        sb.AppendLine();
        sb.AppendLine("namespace Game.Config");
        sb.AppendLine("{");

        // 枚举统一在 Enums.cs 中定义（同命名空间，直接可用）
        foreach (var schema in schemas.Where(s => s.Kind is not (TableKind.Enum or TableKind.Param)))
        {
            WriteCsEntity(sb, schema);
        }

        foreach (var schema in schemas.Where(s => s.Kind == TableKind.Param))
        {
            WriteCsParam(sb, schema);
        }

        foreach (var schema in schemas.Where(s => s.Kind == TableKind.Keyed))
        {
            WriteCsTable(sb, schema);
        }

        foreach (var schema in schemas.Where(s => s.Kind == TableKind.Array))
        {
            WriteCsList(sb, schema);
        }

        foreach (var schema in schemas.Where(s => s.Kind == TableKind.Group))
        {
            WriteCsGroup(sb, schema);
        }

        WriteCsRoot(sb, file, schemas, rootClass);

        sb.AppendLine("}");

        var outputPath = Path.Combine(outputDir, $"{file.OutputName}.cs");
        await FileUtil.WriteTextAsync(outputPath, sb.ToString());
        Logger.Success($"生成C#: {Path.GetFileName(outputPath)}");
    }

    private void WriteCsEntity(StringBuilder sb, TableSchema schema)
    {
        var className = Pascal(schema.Name);
        var fieldNames = BuildCsFieldNames(schema);

        sb.AppendLine($"    /// <summary>{schema.Sheet.ChineseName}</summary>");
        sb.AppendLine($"    public class {className}");
        sb.AppendLine("    {");
        sb.AppendLine("        private readonly ConfigRow _row;");
        sb.AppendLine();
        sb.AppendLine($"        public {className}(ConfigRow row)");
        sb.AppendLine("        {");
        sb.AppendLine("            _row = row;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        private object[] Values => _row.Values;");

        foreach (var column in schema.Columns)
        {
            var member = fieldNames[Key(column)];
            sb.AppendLine();
            sb.AppendLine($"        /// <summary>{CommentOf(column.Field)}</summary>");
            sb.AppendLine($"        public {CsTypeOf(column.Type)} {member} => {CsValueOf(column)};");
        }

        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private void WriteCsParam(StringBuilder sb, TableSchema schema)
    {
        var className = Pascal(schema.Name) + "Config";
        var fieldNames = BuildCsFieldNames(schema);

        sb.AppendLine($"    /// <summary>{schema.Sheet.ChineseName}</summary>");
        sb.AppendLine($"    public class {className}");
        sb.AppendLine("    {");

        foreach (var column in schema.Columns)
        {
            var member = fieldNames[Key(column)];
            var type = CsTypeOf(column.Type);
            var initializer = CsDefaultOf(column.Type);

            sb.AppendLine($"        /// <summary>{CommentOf(column.Field)}</summary>");
            sb.AppendLine($"        public {type} {member} {{ get; private set; }}{initializer}");
            sb.AppendLine();
        }

        sb.AppendLine("        public void Load(string json)");
        sb.AppendLine("        {");
        sb.AppendLine("            using (var doc = JsonDocument.Parse(json))");
        sb.AppendLine("            {");
        sb.AppendLine("                var fields = doc.RootElement;");
        sb.AppendLine("                JsonElement cell;");

        foreach (var column in schema.Columns)
        {
            var member = fieldNames[Key(column)];
            sb.AppendLine($"                if (fields.TryGetProperty(\"{column.Name}\", out cell)) {member} = {CsReadCellOf(column)};");
        }

        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private void WriteCsTable(StringBuilder sb, TableSchema schema)
    {
        var rootClass = Pascal(schema.Name);
        var tableName = rootClass + "Map";

        sb.AppendLine($"    /// <summary>{schema.Sheet.ChineseName}</summary>");
        sb.AppendLine($"    public class {tableName}");
        sb.AppendLine("    {");
        sb.AppendLine("        private readonly ConfigTable _table = new ConfigTable();");
        sb.AppendLine();
        sb.AppendLine("        /// <summary>主键 -> 行号</summary>");
        sb.AppendLine("        private readonly Dictionary<int, int> _index = new Dictionary<int, int>();");
        sb.AppendLine();
        sb.AppendLine($"        /// <summary>已访问过的行（只缓存用到的实体）</summary>");
        sb.AppendLine($"        private readonly Dictionary<int, {rootClass}> _data = new Dictionary<int, {rootClass}>();");
        sb.AppendLine();
        sb.AppendLine("        public int Count => _table.Count;");
        sb.AppendLine();
        sb.AppendLine("        public void Load(string json)");
        sb.AppendLine("        {");
        sb.AppendLine("            _table.Load(json);");
        sb.AppendLine("            _index.Clear();");
        sb.AppendLine("            _data.Clear();");
        sb.AppendLine();
        sb.AppendLine("            for (int i = 0; i < _table.Count; i++)");
        sb.AppendLine("            {");
        sb.AppendLine("                _index[_table.ReadKey(i)] = i;");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine($"        /// <summary>按主键取一行：定位到行号后才创建实体并解析</summary>");
        sb.AppendLine($"        public {rootClass} FindById(int id)");
        sb.AppendLine("        {");
        sb.AppendLine($"            {rootClass} cached;");
        sb.AppendLine("            if (_data.TryGetValue(id, out cached)) return cached;");
        sb.AppendLine();
        sb.AppendLine("            int row;");
        sb.AppendLine("            if (!_index.TryGetValue(id, out row)) return null;");
        sb.AppendLine();
        sb.AppendLine($"            var entity = new {rootClass}(_table.Row(row));");
        sb.AppendLine("            _data[id] = entity;");
        sb.AppendLine("            return entity;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        /// <summary>全表遍历：逐行创建实体，不驻留整表对象</summary>");
        sb.AppendLine($"        public void ForEach(Action<{rootClass}, int> callback)");
        sb.AppendLine("        {");
        sb.AppendLine("            for (int i = 0; i < _table.Count; i++)");
        sb.AppendLine("            {");
        sb.AppendLine($"                callback(new {rootClass}(_table.Row(i)), i);");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private void WriteCsList(StringBuilder sb, TableSchema schema)
    {
        var entity = Pascal(schema.Name);
        var tableName = entity + "List";

        sb.AppendLine($"    /// <summary>{schema.Sheet.ChineseName}</summary>");
        sb.AppendLine($"    public class {tableName}");
        sb.AppendLine("    {");
        sb.AppendLine("        private readonly ConfigTable _table = new ConfigTable();");
        sb.AppendLine();
        sb.AppendLine("        /// <summary>已访问过的行（只缓存用到的实体）</summary>");
        sb.AppendLine($"        private readonly Dictionary<int, {entity}> _data = new Dictionary<int, {entity}>();");
        sb.AppendLine();
        sb.AppendLine("        public int Count => _table.Count;");
        sb.AppendLine();
        sb.AppendLine("        public void Load(string json)");
        sb.AppendLine("        {");
        sb.AppendLine("            _table.Load(json);");
        sb.AppendLine("            _data.Clear();");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine($"        public {entity} FindByIndex(int index)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (index < 0 || index >= _table.Count) return null;");
        sb.AppendLine();
        sb.AppendLine($"            {entity} cached;");
        sb.AppendLine("            if (_data.TryGetValue(index, out cached)) return cached;");
        sb.AppendLine();
        sb.AppendLine($"            var entity = new {entity}(_table.Row(index));");
        sb.AppendLine("            _data[index] = entity;");
        sb.AppendLine("            return entity;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine($"        public void ForEach(Action<{entity}, int> callback)");
        sb.AppendLine("        {");
        sb.AppendLine("            int count = _table.Count;");
        sb.AppendLine("            for (int i = 0; i < count; i++)");
        sb.AppendLine("            {");
        sb.AppendLine($"                callback(new {entity}(_table.Row(i)), i);");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private void WriteCsGroup(StringBuilder sb, TableSchema schema)
    {
        var entity = Pascal(schema.Name);
        var tableName = entity + "Group";

        sb.AppendLine($"    /// <summary>{schema.Sheet.ChineseName}</summary>");
        sb.AppendLine($"    public class {tableName}");
        sb.AppendLine("    {");
        sb.AppendLine("        private readonly ConfigTable _table = new ConfigTable();");
        sb.AppendLine();
        sb.AppendLine("        /// <summary>分组键 -> 行号列表</summary>");
        sb.AppendLine("        private readonly Dictionary<int, List<int>> _index = new Dictionary<int, List<int>>();");
        sb.AppendLine();
        sb.AppendLine("        /// <summary>已访问过的行（只缓存用到的实体）</summary>");
        sb.AppendLine($"        private readonly Dictionary<int, {entity}> _data = new Dictionary<int, {entity}>();");
        sb.AppendLine();
        sb.AppendLine("        public int Count => _table.Count;");
        sb.AppendLine();
        sb.AppendLine("        public void Load(string json)");
        sb.AppendLine("        {");
        sb.AppendLine("            _table.Load(json);");
        sb.AppendLine("            _index.Clear();");
        sb.AppendLine("            _data.Clear();");
        sb.AppendLine();
        sb.AppendLine("            for (int i = 0; i < _table.Count; i++)");
        sb.AppendLine("            {");
        sb.AppendLine("                int key = _table.ReadKey(i);");
        sb.AppendLine("                List<int> rows;");
        sb.AppendLine("                if (!_index.TryGetValue(key, out rows))");
        sb.AppendLine("                {");
        sb.AppendLine("                    rows = new List<int>();");
        sb.AppendLine("                    _index.Add(key, rows);");
        sb.AppendLine("                }");
        sb.AppendLine();
        sb.AppendLine("                rows.Add(i);");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine($"        public IReadOnlyList<{entity}> FindGroupById(int id)");
        sb.AppendLine("        {");
        sb.AppendLine("            List<int> rows;");
        sb.AppendLine("            if (!_index.TryGetValue(id, out rows)) return null;");
        sb.AppendLine();
        sb.AppendLine($"            var result = new List<{entity}>(rows.Count);");
        sb.AppendLine("            for (int i = 0; i < rows.Count; i++)");
        sb.AppendLine("            {");
        sb.AppendLine("                result.Add(GetEntity(rows[i]));");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            return result;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        /// <summary>同一行只创建一次实体</summary>");
        sb.AppendLine($"        private {entity} GetEntity(int row)");
        sb.AppendLine("        {");
        sb.AppendLine($"            {entity} cached;");
        sb.AppendLine("            if (_data.TryGetValue(row, out cached)) return cached;");
        sb.AppendLine();
        sb.AppendLine($"            var entity = new {entity}(_table.Row(row));");
        sb.AppendLine("            _data[row] = entity;");
        sb.AppendLine("            return entity;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private void WriteCsRoot(StringBuilder sb, ExcelFileData file, List<TableSchema> schemas, string rootClass)
    {
        var containers = schemas.Where(s => s.Kind != TableKind.Enum).ToList();

        sb.AppendLine($"    /// <summary>{file.FileName} 配置根</summary>");
        sb.AppendLine($"    public class {rootClass}");
        sb.AppendLine("    {");

        foreach (var schema in containers)
        {
            var type = CsContainerType(schema);
            sb.AppendLine($"        public {type} {CsMemberName(schema)} {{ get; }} = new {type}();");
        }

        sb.AppendLine();
        sb.AppendLine("        public void Load(string json)");
        sb.AppendLine("        {");
        sb.AppendLine("            using (var doc = JsonDocument.Parse(json))");
        sb.AppendLine("            {");
        sb.AppendLine("                var root = doc.RootElement;");
        sb.AppendLine("                JsonElement section;");

        foreach (var schema in containers)
        {
            sb.AppendLine($"                if (root.TryGetProperty(\"{schema.Name}\", out section)) {CsMemberName(schema)}.Load(section.GetRawText());");
        }

        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    /// <summary>根类里的字段名：表名 + 容器后缀（参数表无后缀）</summary>
    private static string CsMemberName(TableSchema schema) => schema.Kind switch
    {
        TableKind.Keyed => Pascal(schema.Name) + "Map",
        TableKind.Array => Pascal(schema.Name) + "List",
        TableKind.Group => Pascal(schema.Name) + "Group",
        _ => Pascal(schema.Name)
    };

    private static string CsContainerType(TableSchema schema) => schema.Kind switch
    {
        TableKind.Keyed => Pascal(schema.Name) + "Map",
        TableKind.Array => Pascal(schema.Name) + "List",
        TableKind.Group => Pascal(schema.Name) + "Group",
        _ => Pascal(schema.Name) + "Config"
    };

    private static void WriteCsRuntime(StringBuilder sb)
    {
        sb.AppendLine("    /// <summary>语言表：key 为数字字符串，缺失时回退为 key 本身</summary>");
        sb.AppendLine("    public static class LanguageManager");
        sb.AppendLine("    {");
        sb.AppendLine("        private static Dictionary<string, string> _texts = new Dictionary<string, string>();");
        sb.AppendLine();
        sb.AppendLine("        public static void Load(string json)");
        sb.AppendLine("        {");
        sb.AppendLine("            _texts = JsonSerializer.Deserialize<Dictionary<string, string>>(json)");
        sb.AppendLine("                     ?? new Dictionary<string, string>();");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        public static string GetText(string key)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (string.IsNullOrEmpty(key)) return string.Empty;");
        sb.AppendLine("            string text;");
        sb.AppendLine("            return _texts.TryGetValue(key, out text) ? text : key;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// 行数据容器：只保存原始行文本数组，");
        sb.AppendLine("    /// 访问某行时才解析该行，避免一次性创建几十万个数组。");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public sealed class ConfigTable");
        sb.AppendLine("    {");
        sb.AppendLine("        private string[] _rows = Array.Empty<string>();");
        sb.AppendLine();
        sb.AppendLine("        public int Count { get { return _rows.Length; } }");
        sb.AppendLine();
        sb.AppendLine("        /// <summary>由 JSON 的字符串数组加载</summary>");
        sb.AppendLine("        public void Load(string json)");
        sb.AppendLine("        {");
        sb.AppendLine("            _rows = ConfigValue.ToRows(json);");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        public ConfigRow Row(int index)");
        sb.AppendLine("        {");
        sb.AppendLine("            return new ConfigRow(index < _rows.Length ? _rows[index] : null);");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        /// <summary>只读取行首的数值（分组键），不解析整行</summary>");
        sb.AppendLine("        public int ReadKey(int index)");
        sb.AppendLine("        {");
        sb.AppendLine("            return ConfigValue.ReadKey(_rows[index]);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    /// <summary>一行数据：首次访问字段时才解析</summary>");
        sb.AppendLine("    public sealed class ConfigRow");
        sb.AppendLine("    {");
        sb.AppendLine("        private string _data;");
        sb.AppendLine("        private object[] _values;");
        sb.AppendLine();
        sb.AppendLine("        public ConfigRow(string data)");
        sb.AppendLine("        {");
        sb.AppendLine("            _data = data;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        public object[] Values");
        sb.AppendLine("        {");
        sb.AppendLine("            get");
        sb.AppendLine("            {");
        sb.AppendLine("                if (_values == null)");
        sb.AppendLine("                {");
        sb.AppendLine("                    _values = ConfigValue.ParseRow(_data);");
        sb.AppendLine("                    _data = null;");
        sb.AppendLine("                }");
        sb.AppendLine();
        sb.AppendLine("                return _values;");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    /// <summary>取值工具</summary>");
        sb.AppendLine("    internal static class ConfigValue");
        sb.AppendLine("    {");
        sb.AppendLine("        public static object ToObject(JsonElement cell)");
        sb.AppendLine("        {");
        sb.AppendLine("            switch (cell.ValueKind)");
        sb.AppendLine("            {");
        sb.AppendLine("                case JsonValueKind.Number:");
        sb.AppendLine("                    int number;");
        sb.AppendLine("                    return cell.TryGetInt32(out number) ? (object)number : cell.GetDouble();");
        sb.AppendLine("                case JsonValueKind.String:");
        sb.AppendLine("                    return cell.GetString();");
        sb.AppendLine("                case JsonValueKind.True:");
        sb.AppendLine("                    return true;");
        sb.AppendLine("                case JsonValueKind.False:");
        sb.AppendLine("                    return false;");
        sb.AppendLine("                case JsonValueKind.Array:");
        sb.AppendLine("                    var list = new List<object>();");
        sb.AppendLine("                    foreach (var item in cell.EnumerateArray()) list.Add(ToObject(item));");
        sb.AppendLine("                    return list;");
        sb.AppendLine("                default:");
        sb.AppendLine("                    return null;");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        public static int ToInt(object value)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (value == null) return 0;");
        sb.AppendLine("            if (value is int) return (int)value;");
        sb.AppendLine("            if (value is long) return (int)(long)value;");
        sb.AppendLine("            if (value is bool) return (bool)value ? 1 : 0;");
        sb.AppendLine("            if (value is double) return (int)(double)value;");
        sb.AppendLine("            if (value is float) return (int)(float)value;");
        sb.AppendLine("            double number;");
        sb.AppendLine("            return double.TryParse(value.ToString(), out number) ? (int)number : 0;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        public static long ToLong(object value)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (value == null) return 0;");
        sb.AppendLine("            if (value is long) return (long)value;");
        sb.AppendLine("            if (value is int) return (int)value;");
        sb.AppendLine("            return (long)ToDouble(value);");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        public static double ToDouble(object value)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (value == null) return 0;");
        sb.AppendLine("            if (value is double) return (double)value;");
        sb.AppendLine("            if (value is float) return (float)value;");
        sb.AppendLine("            if (value is int) return (int)value;");
        sb.AppendLine("            if (value is long) return (long)value;");
        sb.AppendLine("            double number;");
        sb.AppendLine("            return double.TryParse(value.ToString(), out number) ? number : 0;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        public static bool ToBool(object value)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (value is bool) return (bool)value;");
        sb.AppendLine("            return ToInt(value) != 0;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        public static string ToStr(object value)");
        sb.AppendLine("        {");
        sb.AppendLine("            return value == null ? string.Empty : value.ToString();");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        public static int ToInt(JsonElement cell) { return ToInt(ToObject(cell)); }");
        sb.AppendLine();
        sb.AppendLine("        public static long ToLong(JsonElement cell) { return ToLong(ToObject(cell)); }");
        sb.AppendLine();
        sb.AppendLine("        public static double ToDouble(JsonElement cell) { return ToDouble(ToObject(cell)); }");
        sb.AppendLine();
        sb.AppendLine("        public static bool ToBool(JsonElement cell) { return ToBool(ToObject(cell)); }");
        sb.AppendLine();
        sb.AppendLine("        public static string ToStr(JsonElement cell) { return ToStr(ToObject(cell)); }");
        sb.AppendLine();
        sb.AppendLine("        public static List<int> ToIntList(object value)");
        sb.AppendLine("        {");
        sb.AppendLine("            var result = new List<int>();");
        sb.AppendLine("            var items = value as List<object>;");
        sb.AppendLine("            if (items == null) return result;");
        sb.AppendLine("            foreach (var item in items) result.Add(ToInt(item));");
        sb.AppendLine("            return result;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        public static List<string> ToStrList(object value)");
        sb.AppendLine("        {");
        sb.AppendLine("            var result = new List<string>();");
        sb.AppendLine("            var items = value as List<object>;");
        sb.AppendLine("            if (items == null) return result;");
        sb.AppendLine("            foreach (var item in items) result.Add(ToStr(item));");
        sb.AppendLine("            return result;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        public static List<bool> ToBoolList(object value)");
        sb.AppendLine("        {");
        sb.AppendLine("            var result = new List<bool>();");
        sb.AppendLine("            var items = value as List<object>;");
        sb.AppendLine("            if (items == null) return result;");
        sb.AppendLine("            foreach (var item in items) result.Add(ToBool(item));");
        sb.AppendLine("            return result;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        public static List<object> ToObjectList(JsonElement cell)");
        sb.AppendLine("        {");
        sb.AppendLine("            var list = new List<object>();");
        sb.AppendLine("            if (cell.ValueKind != JsonValueKind.Array) return list;");
        sb.AppendLine("            foreach (var item in cell.EnumerateArray()) list.Add(ToObject(item));");
        sb.AppendLine("            return list;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        /// <summary>把 JSON 根节点（字符串数组）读成行文本数组</summary>");
        sb.AppendLine("        public static string[] ToRows(string json)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (string.IsNullOrEmpty(json)) return Array.Empty<string>();");
        sb.AppendLine();
        sb.AppendLine("            using (var doc = JsonDocument.Parse(json))");
        sb.AppendLine("            {");
        sb.AppendLine("                if (doc.RootElement.ValueKind != JsonValueKind.Array) return Array.Empty<string>();");
        sb.AppendLine();
        sb.AppendLine("                var rows = new List<string>();");
        sb.AppendLine("                foreach (var item in doc.RootElement.EnumerateArray())");
        sb.AppendLine("                {");
        sb.AppendLine("                    rows.Add(item.ValueKind == JsonValueKind.String ? item.GetString() : item.GetRawText());");
        sb.AppendLine("                }");
        sb.AppendLine();
        sb.AppendLine("                return rows.ToArray();");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        /// <summary>从 \"[1,...]\" 里取首列数字（主键 / 分组键）</summary>");
        sb.AppendLine("        public static int ReadKey(string row)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (string.IsNullOrEmpty(row)) return 0;");
        sb.AppendLine();
        sb.AppendLine("            int start = row[0] == '[' ? 1 : 0;");
        sb.AppendLine("            int end = row.IndexOf(',', start);");
        sb.AppendLine("            if (end < 0) end = row.Length;");
        sb.AppendLine();
        sb.AppendLine("            // 单列表的行形如 \"[1]\"，把结尾的 ] 和空白去掉再解析");
        sb.AppendLine("            while (end > start && (row[end - 1] == ']' || char.IsWhiteSpace(row[end - 1]))) end--;");
        sb.AppendLine();
        sb.AppendLine("            int value;");
        sb.AppendLine("            return int.TryParse(row.Substring(start, end - start), out value) ? value : 0;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        /// <summary>解析一行文本</summary>");
        sb.AppendLine("        public static object[] ParseRow(string row)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (string.IsNullOrEmpty(row)) return Array.Empty<object>();");
        sb.AppendLine();
        sb.AppendLine("            using (var doc = JsonDocument.Parse(row))");
        sb.AppendLine("            {");
        sb.AppendLine("                var values = new List<object>();");
        sb.AppendLine("                foreach (var cell in doc.RootElement.EnumerateArray())");
        sb.AppendLine("                {");
        sb.AppendLine("                    values.Add(ToObject(cell));");
        sb.AppendLine("                }");
        sb.AppendLine();
        sb.AppendLine("                return values.ToArray();");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
    }

    // ---------------------------------------------------------------- 类型与表达式映射

    private static string TsTypeOf(ResolvedType type)
    {
        var core = type.Kind switch
        {
            ValueKind.Number => "number",
            ValueKind.Bool => "boolean",
            ValueKind.String => "string",
            ValueKind.Language => "string",
            ValueKind.Enum => "TableEnums." + Ident(type.Name),
            ValueKind.RowRef => "number",
            _ => "any"
        };

        return type.IsArray ? $"{core}[]" : core;
    }

    private static string TsValueOf(SchemaColumn column)
    {
        var raw = $"this.objects[{column.Index}]";

        if (column.Type.Kind == ValueKind.Language && !column.Type.IsArray)
        {
            return $"_lang.t({raw})";
        }

        return column.Type.IsArray ? $"{raw} as {TsTypeOf(column.Type)}" : raw;
    }

    private static string CsTypeOf(ResolvedType type)
    {
        var core = type.Kind switch
        {
            ValueKind.Number => type.Name switch
            {
                "long" => "long",
                "float" => "float",
                "double" => "double",
                _ => "int"
            },
            ValueKind.Bool => "bool",
            ValueKind.String => "string",
            ValueKind.Language => "string",
            ValueKind.Enum => Pascal(type.Name),
            ValueKind.RowRef => "int",
            _ => "object"
        };

        if (!type.IsArray)
        {
            return core;
        }

        return core switch
        {
            "int" => "List<int>",
            "long" => "List<long>",
            "float" => "List<float>",
            "double" => "List<double>",
            "bool" => "List<bool>",
            "string" => "List<string>",
            _ => "List<object>"
        };
    }

    private static string CsValueOf(SchemaColumn column)
    {
        var cell = $"Values[{column.Index}]";
        var type = column.Type;
        var element = type with { IsArray = false };

        if (type.Kind is ValueKind.Any or ValueKind.Unknown)
        {
            return type.IsArray ? $"{cell} as List<object>" : cell;
        }

        if (type.IsArray)
        {
            return element.Kind switch
            {
                ValueKind.Bool => $"ConfigValue.ToBoolList({cell})",
                ValueKind.String or ValueKind.Language => $"ConfigValue.ToStrList({cell})",
                _ => $"ConfigValue.ToIntList({cell})"
            };
        }

        return type.Kind switch
        {
            ValueKind.Number => element.Name switch
            {
                "long" => $"ConfigValue.ToLong({cell})",
                "float" or "double" => $"(float)ConfigValue.ToDouble({cell})",
                _ => $"ConfigValue.ToInt({cell})"
            },
            ValueKind.Bool => $"ConfigValue.ToBool({cell})",
            ValueKind.Language => $"LanguageManager.GetText(ConfigValue.ToStr({cell}))",
            ValueKind.Enum => $"({Pascal(type.Name)})ConfigValue.ToInt({cell})",
            ValueKind.RowRef => $"ConfigValue.ToInt({cell})",
            _ => $"ConfigValue.ToStr({cell})"
        };
    }

    private static string CsReadCellOf(SchemaColumn column)
    {
        var type = column.Type;
        var cell = "cell";

        if (type.Kind is ValueKind.Any or ValueKind.Unknown)
        {
            return type.IsArray ? "ConfigValue.ToObjectList(cell)" : "ConfigValue.ToObject(cell)";
        }

        if (type.IsArray)
        {
            return type.Kind switch
            {
                ValueKind.Bool => $"ConfigValue.ToBoolList({cell})",
                ValueKind.String or ValueKind.Language => $"ConfigValue.ToStrList({cell})",
                _ => $"ConfigValue.ToIntList({cell})"
            };
        }

        return type.Kind switch
        {
            ValueKind.Number => type.Name switch
            {
                "long" => $"ConfigValue.ToLong({cell})",
                "float" or "double" => $"(float)ConfigValue.ToDouble({cell})",
                _ => $"ConfigValue.ToInt({cell})"
            },
            ValueKind.Bool => $"ConfigValue.ToBool({cell})",
            ValueKind.Language => $"LanguageManager.GetText(ConfigValue.ToStr({cell}))",
            ValueKind.Enum => $"({Pascal(type.Name)})ConfigValue.ToInt({cell})",
            ValueKind.RowRef => $"ConfigValue.ToInt({cell})",
            _ => $"ConfigValue.ToStr({cell})"
        };
    }

    private static string CsDefaultOf(ResolvedType type)
    {
        if (type.IsArray)
        {
            return " = new " + CsTypeOf(type) + "();";
        }

        return type.Kind switch
        {
            ValueKind.String or ValueKind.Language => " = string.Empty;",
            _ => string.Empty
        };
    }

    // ---------------------------------------------------------------- 命名工具

    private static Dictionary<string, string> BuildTsFieldNames(TableSchema schema)
    {
        var result = new Dictionary<string, string>();
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var column in schema.Columns)
        {
            result[Key(column)] = UniqueName(Ident(column.Field.Name), used);
        }

        return result;
    }

    private static Dictionary<string, string> BuildCsFieldNames(TableSchema schema)
    {
        var result = new Dictionary<string, string>();
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var column in schema.Columns)
        {
            var member = UniqueName(SafeCsIdentifier(Pascal(column.Field.Name)), used);
            result[Key(column)] = member;
        }

        return result;
    }

    private static string Key(SchemaColumn column) => column.Field.Name;

    private static string CommentOf(FieldInfo field) =>
        string.IsNullOrWhiteSpace(field.Comment) ? field.Name : field.Comment;

    private static string SafeCsIdentifier(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Field";
        }

        var sb = new StringBuilder();
        foreach (var c in name)
        {
            sb.Append(char.IsLetterOrDigit(c) || c == '_' ? c : '_');
        }

        var result = sb.ToString();
        if (char.IsDigit(result[0]))
        {
            result = "_" + result;
        }

        return CSharpKeywords.Contains(result) ? "@" + result : result;
    }

    private static string UniqueName(string name, HashSet<string> used)
    {
        var unique = name;
        var counter = 1;

        while (!used.Add(unique))
        {
            unique = $"{name}_{counter}";
            counter++;
        }

        return unique;
    }

    private static string Ident(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "field";
        }

        var sb = new StringBuilder();
        foreach (var c in name)
        {
            sb.Append(char.IsLetterOrDigit(c) || c == '_' ? c : '_');
        }

        var result = sb.ToString();
        if (result.Length == 0)
        {
            return "field";
        }

        if (char.IsDigit(result[0]))
        {
            result = "_" + result;
        }

        return result;
    }

    private static string Pascal(string name)
    {
        var ident = Ident(name);
        var parts = ident.Split('_', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return "Value";
        }

        var sb = new StringBuilder();
        foreach (var part in parts)
        {
            sb.Append(char.ToUpperInvariant(part[0]));
            if (part.Length > 1)
            {
                sb.Append(part.Substring(1));
            }
        }

        return sb.ToString();
    }
}
