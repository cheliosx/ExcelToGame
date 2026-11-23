using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using ExcelToGame.Models;
using ExcelToGame.Utils;

namespace ExcelToGame.Core;

/// <summary>
/// 按 TableSchema 生成 JSON / TypeScript / C# 产物。
/// 大数据表不在加载时展开：整表只保留一份行文本 + 行偏移表，访问哪行才解析哪行。
/// </summary>
public class CodeGenerator
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

    /// <param name="splitRows">
    /// true：行数据输出成字符串数组，一行一条，方便 diff；
    /// false：行数据压成一个字符串，文件更小、解析峰值更低。
    /// </param>
    public async Task GenerateJson(ExcelFileData file, List<TableSchema> schemas, string outputDir, bool splitRows)
    {
        var result = new Dictionary<string, object?>();

        // 枚举已经生成成代码里的 enum，不再重复写进 JSON
        foreach (var schema in schemas.Where(s => s.Kind != TableKind.Enum))
        {
            result[schema.Name] = schema.Kind == TableKind.Param
                ? new Dictionary<string, object?> { ["fields"] = BuildParamFields(schema) }
                : BuildTablePayload(schema, splitRows);
        }

        var outputPath = Path.Combine(outputDir, $"{file.OutputName}.json");
        await FileUtil.WriteTextAsync(outputPath, ToReadableJson(result));
        Logger.Success($"生成JSON: {outputPath}");
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
    /// 行数据：每行是紧凑 JSON 数组。
    /// splitRows = true 时输出 <c>rows: ["[1,2]", ...]</c>（一行一条，便于 diff）；
    /// 否则拼成 <c>data: "[1,2]\n[3,4]"</c>（单块字符串，体积与解析峰值更小）。
    /// </summary>
    private static Dictionary<string, object?> BuildTablePayload(TableSchema schema, bool splitRows)
    {
        // 列名/下标、类型、行数都已固化到生成代码里，JSON 只放数据本身
        var payload = new Dictionary<string, object?>();

        if (schema.Kind == TableKind.Keyed && schema.KeyColumn != null)
        {
            payload["ids"] = schema.Rows.Select(r => r[schema.KeyColumn.Index]).ToList();
        }

        var lines = new List<string>(schema.Rows.Count);
        foreach (var row in schema.Rows)
        {
            lines.Add(JsonSerializer.Serialize(row, InlineJsonOptions));
        }

        if (splitRows)
        {
            payload["rows"] = new RowLines { Value = lines };
        }
        else
        {
            payload["data"] = string.Join("\n", lines);
        }

        return payload;
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
        var ns = Ident(file.OutputName);
        var hasLanguage = schemas.Any(s => s.Columns.Any(c => c.Type.Kind == ValueKind.Language));

        if (hasLanguage)
        {
            sb.AppendLine("import { _lang } from \"./_language\";");
            sb.AppendLine();
        }

        sb.AppendLine("/***");
        sb.AppendLine("* 该文件由工具自动生成，请勿手动修改");
        sb.AppendLine("*/");

        WriteTsRuntime(sb);
        sb.AppendLine($"export namespace {ns} {{");

        foreach (var schema in schemas.Where(s => s.Kind == TableKind.Enum))
        {
            WriteTsEnum(sb, schema);
        }

        // 每个页签一个实体类，彼此独立
        foreach (var schema in schemas.Where(s => s.Kind is not (TableKind.Enum or TableKind.Param)))
        {
            WriteTsEntity(sb, schema);
        }

        foreach (var schema in schemas.Where(s => s.Kind == TableKind.Param))
        {
            WriteTsParam(sb, schema);
        }

        sb.AppendLine("}");
        sb.AppendLine();

        foreach (var schema in schemas.Where(s => s.Kind == TableKind.Keyed))
        {
            WriteTsTable(sb, schema, ns);
        }

        foreach (var schema in schemas.Where(s => s.Kind == TableKind.Array))
        {
            WriteTsArrayList(sb, schema, ns);
        }

        foreach (var schema in schemas.Where(s => s.Kind == TableKind.Group))
        {
            WriteTsGroup(sb, schema, ns);
        }

        WriteTsRoot(sb, file, schemas, ns);

        var outputPath = Path.Combine(outputDir, $"_{file.OutputName}.ts");
        await FileUtil.WriteTextAsync(outputPath, sb.ToString());
        Logger.Success($"生成TS: {outputPath}");
    }

    /// <summary>共享运行时：行文本容器 + 主键索引（整表只在内存里存一份字符串）</summary>
    private static void WriteTsRuntime(StringBuilder sb)
    {
        sb.AppendLine("/**");
        sb.AppendLine(" * 行数据容器：整表只保留一份文本和行偏移表，");
        sb.AppendLine(" * 访问某行时才解析该行，避免一次性创建几十万个数组。");
        sb.AppendLine(" */");
        sb.AppendLine("export class _TableData {");
        sb.AppendLine("    private readonly _text: string;");
        sb.AppendLine("    private readonly _offsets: Int32Array;");
        sb.AppendLine();
        sb.AppendLine("    constructor(text: string) {");
        sb.AppendLine("        this._text = text || \"\";");
        sb.AppendLine("        this._offsets = _TableData.splitLines(this._text);");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    public get count(): number {");
        sb.AppendLine("        return this._offsets.length - 1;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    /** 兼容两种 JSON 形态：data（单块文本）或 rows（逐行字符串数组） */");
        sb.AppendLine("    public static fromPayload(payload: any): _TableData {");
        sb.AppendLine("        if (payload === null || payload === undefined) {");
        sb.AppendLine("            return new _TableData(\"\");");
        sb.AppendLine("        }");
        sb.AppendLine("        if (payload.data !== undefined && payload.data !== null) {");
        sb.AppendLine("            return new _TableData(payload.data);");
        sb.AppendLine("        }");
        sb.AppendLine("        if (Array.isArray(payload.rows)) {");
        sb.AppendLine("            return new _TableData(payload.rows.join(\"\\n\"));");
        sb.AppendLine("        }");
        sb.AppendLine("        return new _TableData(\"\");");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    /** 解析第 index 行，只创建这一行的数组 */");
        sb.AppendLine("    public parseRow(index: number): any[] {");
        sb.AppendLine("        if (index < 0 || index >= this.count) {");
        sb.AppendLine("            return [];");
        sb.AppendLine("        }");
        sb.AppendLine("        return JSON.parse(this._text.substring(this._offsets[index], this._offsets[index + 1]));");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    /** 只读取行首的数值（分组键），不解析整行 */");
        sb.AppendLine("    public readKey(index: number): number {");
        sb.AppendLine("        let i = this._offsets[index];");
        sb.AppendLine("        const end = this._offsets[index + 1];");
        sb.AppendLine("        if (this._text.charCodeAt(i) === 91) {");
        sb.AppendLine("            i++;");
        sb.AppendLine("        }");
        sb.AppendLine("        let sign = 1;");
        sb.AppendLine("        let value = 0;");
        sb.AppendLine("        let hit = false;");
        sb.AppendLine("        for (; i < end; i++) {");
        sb.AppendLine("            const code = this._text.charCodeAt(i);");
        sb.AppendLine("            if (code >= 48 && code <= 57) {");
        sb.AppendLine("                value = value * 10 + (code - 48);");
        sb.AppendLine("                hit = true;");
        sb.AppendLine("            } else if (code === 45) {");
        sb.AppendLine("                sign = -1;");
        sb.AppendLine("            } else if (hit) {");
        sb.AppendLine("                break;");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("        return sign * value;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    private static splitLines(text: string): Int32Array {");
        sb.AppendLine("        if (text.length === 0) {");
        sb.AppendLine("            return Int32Array.from([0]);");
        sb.AppendLine("        }");
        sb.AppendLine("        const offsets: number[] = [0];");
        sb.AppendLine("        let index = text.indexOf(\"\\n\");");
        sb.AppendLine("        while (index >= 0) {");
        sb.AppendLine("            offsets.push(index + 1);");
        sb.AppendLine("            index = text.indexOf(\"\\n\", index + 1);");
        sb.AppendLine("        }");
        sb.AppendLine("        offsets.push(text.length);");
        sb.AppendLine("        return Int32Array.from(offsets);");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("/** 主键索引：ids 已升序时直接二分，否则额外维护行号排序表 */");
        sb.AppendLine("export class _TableIndex {");
        sb.AppendLine("    private readonly _ids: number[] = [];");
        sb.AppendLine("    private readonly _order: Int32Array;");
        sb.AppendLine();
        sb.AppendLine("    constructor(ids: number[]) {");
        sb.AppendLine("        this._ids = ids || [];");
        sb.AppendLine("        this._order = _TableIndex.buildOrder(this._ids);");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    /** 返回行号，找不到返回 -1 */");
        sb.AppendLine("    public indexOf(id: number): number {");
        sb.AppendLine("        let low = 0;");
        sb.AppendLine("        let high = this._ids.length - 1;");
        sb.AppendLine("        while (low <= high) {");
        sb.AppendLine("            const mid = (low + high) >> 1;");
        sb.AppendLine("            const row = this._order.length === 0 ? mid : this._order[mid];");
        sb.AppendLine("            const value = this._ids[row];");
        sb.AppendLine("            if (value === id) {");
        sb.AppendLine("                return row;");
        sb.AppendLine("            }");
        sb.AppendLine("            if (value < id) {");
        sb.AppendLine("                low = mid + 1;");
        sb.AppendLine("            } else {");
        sb.AppendLine("                high = mid - 1;");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("        return -1;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    private static buildOrder(ids: number[]): Int32Array {");
        sb.AppendLine("        for (let i = 1; i < ids.length; i++) {");
        sb.AppendLine("            if (ids[i] <= ids[i - 1]) {");
        sb.AppendLine("                const order = new Int32Array(ids.length);");
        sb.AppendLine("                for (let j = 0; j < order.length; j++) {");
        sb.AppendLine("                    order[j] = j;");
        sb.AppendLine("                }");
        sb.AppendLine("                order.sort((a, b) => ids[a] - ids[b]);");
        sb.AppendLine("                return order;");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("        return new Int32Array(0);");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        sb.AppendLine();
    }

    private void WriteTsEnum(StringBuilder sb, TableSchema schema)
    {
        var name = Ident(schema.Name);

        sb.AppendLine($"    /** {schema.Sheet.ChineseName} */");
        sb.AppendLine($"    export enum {name} {{");

        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in schema.Sheet.EnumEntries)
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

    private void WriteTsEntity(StringBuilder sb, TableSchema schema)
    {
        var className = Ident(schema.Name);
        var fieldNames = BuildTsFieldNames(schema);

        sb.AppendLine($"    /** {schema.Sheet.ChineseName} */");
        sb.AppendLine($"    export class {className} {{");
        sb.AppendLine("        private readonly _table: _TableData;");
        sb.AppendLine("        private readonly _row: number;");
        sb.AppendLine("        private _values: any[] | null = null;");
        sb.AppendLine();
        sb.AppendLine("        constructor(table: _TableData, row: number) {");
        sb.AppendLine("            this._table = table;");
        sb.AppendLine("            this._row = row;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        /** 首次访问字段时才解析本行 */");
        sb.AppendLine("        private _parse(): any[] {");
        sb.AppendLine("            if (this._values === null) {");
        sb.AppendLine("                this._values = this._table.parseRow(this._row);");
        sb.AppendLine("            }");
        sb.AppendLine("            return this._values;");
        sb.AppendLine("        }");

        foreach (var column in schema.Columns)
        {
            var field = column.Field;
            var member = fieldNames[Key(column)];

            sb.AppendLine();
            sb.AppendLine($"        /** {CommentOf(field)} */");
            sb.AppendLine($"        get {member}(): {TsTypeOf(column.Type)} {{");
            sb.AppendLine($"            return {TsValueOf(column)};");
            sb.AppendLine("        }");
        }

        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private void WriteTsParam(StringBuilder sb, TableSchema schema)
    {
        var className = Ident(schema.Name);
        var fieldNames = BuildTsFieldNames(schema);

        sb.AppendLine($"    /** {schema.Sheet.ChineseName} */");
        sb.AppendLine($"    export class {className} {{");
        sb.AppendLine("        private _fields: Record<string, any> = {};");
        sb.AppendLine();
        sb.AppendLine("        public load(data: any): void {");
        sb.AppendLine("            this._fields = data.fields || {};");
        sb.AppendLine("        }");

        foreach (var column in schema.Columns)
        {
            var field = column.Field;
            var member = fieldNames[Key(column)];

            sb.AppendLine();
            sb.AppendLine($"        /** {CommentOf(field)} */");
            sb.AppendLine($"        get {member}(): {TsTypeOf(column.Type)} {{");
            sb.AppendLine(column.Type.Kind == ValueKind.Language
                ? $"            return _lang.t(this._fields[\"{column.Name}\"]);"
                : $"            return this._fields[\"{column.Name}\"];");
            sb.AppendLine("        }");
        }

        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private void WriteTsTable(StringBuilder sb, TableSchema schema, string ns)
    {
        var entity = Ident(schema.Name);
        var tableName = entity + "Table";
        var hasKey = schema.KeyColumn != null;

        sb.AppendLine($"/** {schema.Sheet.ChineseName} */");
        sb.AppendLine($"export class {tableName} {{");
        sb.AppendLine($"    private _table: _TableData = new _TableData(\"\");");

        if (hasKey)
        {
            sb.AppendLine("    private _index: _TableIndex | null = null;");
            sb.AppendLine($"    private _cache: Map<number, {ns}.{entity}> = new Map();");
        }

        sb.AppendLine();
        sb.AppendLine("    public load(data: any): void {");
        sb.AppendLine("        this._table = _TableData.fromPayload(data);");

        if (hasKey)
        {
            sb.AppendLine("        this._index = data.ids ? new _TableIndex(data.ids) : null;");
            sb.AppendLine("        this._cache = new Map();");
        }

        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    public get count(): number {");
        sb.AppendLine("        return this._table.count;");
        sb.AppendLine("    }");

        if (hasKey)
        {
            sb.AppendLine();
            sb.AppendLine($"    /** 按主键取一行；只解析被访问的行，并缓存已取过的实体 */");
            sb.AppendLine($"    public get(id: number): Readonly<{ns}.{entity}> | undefined {{");
            sb.AppendLine("        const cached = this._cache.get(id);");
            sb.AppendLine("        if (cached !== undefined) {");
            sb.AppendLine("            return cached;");
            sb.AppendLine("        }");
            sb.AppendLine("        if (this._index === null) {");
            sb.AppendLine("            return undefined;");
            sb.AppendLine("        }");
            sb.AppendLine("        const row = this._index.indexOf(id);");
            sb.AppendLine("        if (row < 0) {");
            sb.AppendLine("            return undefined;");
            sb.AppendLine("        }");
            sb.AppendLine($"        const entity = new {ns}.{entity}(this._table, row);");
            sb.AppendLine("        this._cache.set(id, entity);");
            sb.AppendLine("        return entity;");
            sb.AppendLine("    }");
        }

        sb.AppendLine();
        sb.AppendLine("    /** 全表遍历：逐行创建实体，不驻留整表对象 */");
        sb.AppendLine($"    public forEach(callback: (row: {ns}.{entity}, index: number) => void): void {{");
        sb.AppendLine("        const count = this._table.count;");
        sb.AppendLine("        for (let i = 0; i < count; i++) {");
        sb.AppendLine($"            callback(new {ns}.{entity}(this._table, i), i);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        sb.AppendLine();
    }

    private void WriteTsArrayList(StringBuilder sb, TableSchema schema, string ns)
    {
        var entity = Ident(schema.Name);
        var tableName = entity + "List";

        sb.AppendLine($"/** {schema.Sheet.ChineseName} */");
        sb.AppendLine($"export class {tableName} {{");
        sb.AppendLine("    private _table: _TableData = new _TableData(\"\");");
        sb.AppendLine();
        sb.AppendLine("    public load(data: any): void {");
        sb.AppendLine("        this._table = _TableData.fromPayload(data);");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    public get count(): number {");
        sb.AppendLine("        return this._table.count;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine($"    public get(index: number): Readonly<{ns}.{entity}> | undefined {{");
        sb.AppendLine("        if (index < 0 || index >= this._table.count) {");
        sb.AppendLine("            return undefined;");
        sb.AppendLine("        }");
        sb.AppendLine($"        return new {ns}.{entity}(this._table, index);");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine($"    public forEach(callback: (row: {ns}.{entity}, index: number) => void): void {{");
        sb.AppendLine("        const count = this._table.count;");
        sb.AppendLine("        for (let i = 0; i < count; i++) {");
        sb.AppendLine($"            callback(new {ns}.{entity}(this._table, i), i);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        sb.AppendLine();
    }

    private void WriteTsGroup(StringBuilder sb, TableSchema schema, string ns)
    {
        var entity = Ident(schema.Name);
        var tableName = entity + "Group";

        sb.AppendLine($"/** {schema.Sheet.ChineseName} */");
        sb.AppendLine($"export class {tableName} {{");
        sb.AppendLine("    private _table: _TableData = new _TableData(\"\");");
        sb.AppendLine("    private _groups: Map<number, number[]> | null = null;");
        sb.AppendLine();
        sb.AppendLine("    public load(data: any): void {");
        sb.AppendLine("        this._table = _TableData.fromPayload(data);");
        sb.AppendLine("        this._groups = null;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    public get count(): number {");
        sb.AppendLine("        return this._table.count;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine($"    public getGroup(id: number): ReadonlyArray<{ns}.{entity}> | undefined {{");
        sb.AppendLine("        const rows = this._ensureGroups().get(id);");
        sb.AppendLine("        if (rows === undefined) {");
        sb.AppendLine("            return undefined;");
        sb.AppendLine("        }");
        sb.AppendLine($"        const result: {ns}.{entity}[] = [];");
        sb.AppendLine("        for (let i = 0; i < rows.length; i++) {");
        sb.AppendLine($"            result.push(new {ns}.{entity}(this._table, rows[i]));");
        sb.AppendLine("        }");
        sb.AppendLine("        return result;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    /** 分组索引懒构建：首次按组查询时才扫描行首列 */");
        sb.AppendLine("    private _ensureGroups(): Map<number, number[]> {");
        sb.AppendLine("        if (this._groups !== null) {");
        sb.AppendLine("            return this._groups;");
        sb.AppendLine("        }");
        sb.AppendLine("        const groups = new Map<number, number[]>();");
        sb.AppendLine("        const count = this._table.count;");
        sb.AppendLine("        for (let i = 0; i < count; i++) {");
        sb.AppendLine("            const key = this._table.readKey(i);");
        sb.AppendLine("            let rows = groups.get(key);");
        sb.AppendLine("            if (rows === undefined) {");
        sb.AppendLine("                rows = [];");
        sb.AppendLine("                groups.set(key, rows);");
        sb.AppendLine("            }");
        sb.AppendLine("            rows.push(i);");
        sb.AppendLine("        }");
        sb.AppendLine("        this._groups = groups;");
        sb.AppendLine("        return groups;");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        sb.AppendLine();
    }

    private void WriteTsRoot(StringBuilder sb, ExcelFileData file, List<TableSchema> schemas, string ns)
    {
        var className = "_" + Ident(file.OutputName);
        var containers = schemas.Where(s => s.Kind != TableKind.Enum).ToList();

        sb.AppendLine($"/** {file.FileName} */");
        sb.AppendLine($"export class {className} {{");

        foreach (var schema in containers)
        {
            sb.AppendLine($"    public {Ident(schema.Name)}: {TsContainerType(schema, ns)} = new {TsContainerType(schema, ns)}();");
        }

        sb.AppendLine();
        sb.AppendLine("    public load(data: any): void {");
        foreach (var schema in containers)
        {
            var member = Ident(schema.Name);
            sb.AppendLine($"        if (data.{member}) {{");
            sb.AppendLine($"            this.{member}.load(data.{member});");
            sb.AppendLine("        }");
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");
    }

    private static string TsContainerType(TableSchema schema, string ns) => schema.Kind switch
    {
        TableKind.Keyed => Ident(schema.Name) + "Table",
        TableKind.Array => Ident(schema.Name) + "List",
        TableKind.Group => Ident(schema.Name) + "Group",
        _ => ns + "." + Ident(schema.Name)
    };

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

        foreach (var schema in schemas.Where(s => s.Kind == TableKind.Enum))
        {
            WriteCsEnum(sb, schema);
        }

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
        WriteCsRuntime(sb);

        sb.AppendLine("}");

        var outputPath = Path.Combine(outputDir, $"{file.OutputName}.cs");
        await FileUtil.WriteTextAsync(outputPath, sb.ToString());
        Logger.Success($"生成C#: {outputPath}");
    }

    private void WriteCsEnum(StringBuilder sb, TableSchema schema)
    {
        var name = Pascal(schema.Name);
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        sb.AppendLine($"    /// <summary>{schema.Sheet.ChineseName}</summary>");
        sb.AppendLine($"    public enum {name}");
        sb.AppendLine("    {");

        foreach (var entry in schema.Sheet.EnumEntries)
        {
            var member = UniqueName(SafeCsIdentifier(Pascal(entry.Name)), used);
            sb.AppendLine($"        /// <summary>{entry.Comment}</summary>");
            sb.AppendLine($"        {member} = {entry.Value},");
        }

        sb.AppendLine("    }");
        sb.AppendLine();
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
        sb.AppendLine("                if (!doc.RootElement.TryGetProperty(\"fields\", out var fields)) return;");
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
        var tableName = rootClass + "Table";

        sb.AppendLine($"    /// <summary>{schema.Sheet.ChineseName}</summary>");
        sb.AppendLine($"    public class {tableName}");
        sb.AppendLine("    {");
        sb.AppendLine("        private ConfigTable _table = new ConfigTable(string.Empty);");
        sb.AppendLine("        private int[] _ids = Array.Empty<int>();");
        sb.AppendLine("        private int[] _order;");
        sb.AppendLine($"        private readonly Dictionary<int, {rootClass}> _cache = new Dictionary<int, {rootClass}>();");
        sb.AppendLine();
        sb.AppendLine("        public int Count => _table.Count;");
        sb.AppendLine();
        sb.AppendLine("        public void Load(string json)");
        sb.AppendLine("        {");
        sb.AppendLine("            using (var doc = JsonDocument.Parse(json))");
        sb.AppendLine("            {");
        sb.AppendLine("                var root = doc.RootElement;");
        sb.AppendLine("                _table = new ConfigTable(ConfigTable.ExtractText(root));");
        sb.AppendLine("                _ids = root.TryGetProperty(\"ids\", out var ids) ? ConfigValue.ToIntArray(ids) : Array.Empty<int>();");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            _cache.Clear();");
        sb.AppendLine("            _order = ConfigValue.BuildOrder(_ids);");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine($"        /// <summary>按主键取一行；只解析被访问的行，并缓存已取过的实体</summary>");
        sb.AppendLine($"        public {rootClass} Get(int id)");
        sb.AppendLine("        {");
        sb.AppendLine($"            {rootClass} cached;");
        sb.AppendLine("            if (_cache.TryGetValue(id, out cached)) return cached;");
        sb.AppendLine();
        sb.AppendLine("            int row = ConfigValue.IndexOfKey(_ids, _order, id);");
        sb.AppendLine("            if (row < 0) return null;");
        sb.AppendLine();
        sb.AppendLine($"            var entity = new {rootClass}(_table.Row(row));");
        sb.AppendLine("            _cache[id] = entity;");
        sb.AppendLine("            return entity;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        /// <summary>全表遍历：逐行创建实体，不驻留整表对象</summary>");
        sb.AppendLine($"        public void ForEach(Action<{rootClass}, int> callback)");
        sb.AppendLine("        {");
        sb.AppendLine("            int count = _table.Count;");
        sb.AppendLine("            for (int i = 0; i < count; i++)");
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
        var tableName = entity + "Table";

        sb.AppendLine($"    /// <summary>{schema.Sheet.ChineseName}</summary>");
        sb.AppendLine($"    public class {tableName}");
        sb.AppendLine("    {");
        sb.AppendLine("        private ConfigTable _table = new ConfigTable(string.Empty);");
        sb.AppendLine();
        sb.AppendLine("        public int Count => _table.Count;");
        sb.AppendLine();
        sb.AppendLine("        public void Load(string json)");
        sb.AppendLine("        {");
        sb.AppendLine("            using (var doc = JsonDocument.Parse(json))");
        sb.AppendLine("            {");
        sb.AppendLine("                _table = new ConfigTable(ConfigTable.ExtractText(doc.RootElement));");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine($"        public {entity} Get(int index)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (index < 0 || index >= _table.Count) return null;");
        sb.AppendLine($"            return new {entity}(_table.Row(index));");
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
        sb.AppendLine("        private ConfigTable _table = new ConfigTable(string.Empty);");
        sb.AppendLine($"        private Dictionary<int, List<int>> _groups;");
        sb.AppendLine();
        sb.AppendLine("        public int Count => _table.Count;");
        sb.AppendLine();
        sb.AppendLine("        public void Load(string json)");
        sb.AppendLine("        {");
        sb.AppendLine("            using (var doc = JsonDocument.Parse(json))");
        sb.AppendLine("            {");
        sb.AppendLine("                _table = new ConfigTable(ConfigTable.ExtractText(doc.RootElement));");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            _groups = null;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine($"        public IReadOnlyList<{entity}> GetGroup(int id)");
        sb.AppendLine("        {");
        sb.AppendLine("            EnsureGroups();");
        sb.AppendLine();
        sb.AppendLine($"            List<int> rows;");
        sb.AppendLine("            if (!_groups.TryGetValue(id, out rows)) return null;");
        sb.AppendLine();
        sb.AppendLine($"            var result = new List<{entity}>(rows.Count);");
        sb.AppendLine("            for (int i = 0; i < rows.Count; i++)");
        sb.AppendLine("            {");
        sb.AppendLine($"                result.Add(new {entity}(_table.Row(rows[i])));");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            return result;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        /// <summary>分组索引懒构建：首次按组查询时才扫描行首列</summary>");
        sb.AppendLine("        private void EnsureGroups()");
        sb.AppendLine("        {");
        sb.AppendLine("            if (_groups != null) return;");
        sb.AppendLine();
        sb.AppendLine("            _groups = new Dictionary<int, List<int>>();");
        sb.AppendLine("            int count = _table.Count;");
        sb.AppendLine("            for (int i = 0; i < count; i++)");
        sb.AppendLine("            {");
        sb.AppendLine("                int key = _table.ReadKey(i);");
        sb.AppendLine("                List<int> rows;");
        sb.AppendLine("                if (!_groups.TryGetValue(key, out rows))");
        sb.AppendLine("                {");
        sb.AppendLine("                    rows = new List<int>();");
        sb.AppendLine("                    _groups.Add(key, rows);");
        sb.AppendLine("                }");
        sb.AppendLine();
        sb.AppendLine("                rows.Add(i);");
        sb.AppendLine("            }");
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
            sb.AppendLine($"        public {type} {Pascal(schema.Name)} {{ get; }} = new {type}();");
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
            sb.AppendLine($"                if (root.TryGetProperty(\"{schema.Name}\", out section)) {Pascal(schema.Name)}.Load(section.GetRawText());");
        }

        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static string CsContainerType(TableSchema schema) => schema.Kind switch
    {
        TableKind.Keyed => Pascal(schema.Name) + "Table",
        TableKind.Array => Pascal(schema.Name) + "Table",
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
        sb.AppendLine("    /// 行数据容器：整表只保留一份文本和行偏移表，");
        sb.AppendLine("    /// 访问某行时才解析该行，避免一次性创建几十万个数组。");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public sealed class ConfigTable");
        sb.AppendLine("    {");
        sb.AppendLine("        private readonly string _text;");
        sb.AppendLine("        private readonly int[] _offsets;");
        sb.AppendLine();
        sb.AppendLine("        public ConfigTable(string text)");
        sb.AppendLine("        {");
        sb.AppendLine("            _text = text ?? string.Empty;");
        sb.AppendLine("            _offsets = SplitLines(_text);");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        public int Count { get { return _offsets.Length - 1; } }");
        sb.AppendLine();
        sb.AppendLine("        /// <summary>兼容两种 JSON 形态：data（单块文本）或 rows（逐行字符串数组）</summary>");
        sb.AppendLine("        public static string ExtractText(JsonElement root)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (root.TryGetProperty(\"data\", out var data) && data.ValueKind == JsonValueKind.String)");
        sb.AppendLine("            {");
        sb.AppendLine("                return data.GetString();");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            if (root.TryGetProperty(\"rows\", out var rows) && rows.ValueKind == JsonValueKind.Array)");
        sb.AppendLine("            {");
        sb.AppendLine("                var parts = new List<string>();");
        sb.AppendLine("                foreach (var item in rows.EnumerateArray())");
        sb.AppendLine("                {");
        sb.AppendLine("                    parts.Add(item.GetString());");
        sb.AppendLine("                }");
        sb.AppendLine();
        sb.AppendLine("                return string.Join(\"\\n\", parts);");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            return string.Empty;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        public ConfigRow Row(int index)");
        sb.AppendLine("        {");
        sb.AppendLine("            return new ConfigRow(this, index);");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        /// <summary>解析第 index 行，只创建这一行的数组</summary>");
        sb.AppendLine("        public object[] ParseRow(int index)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (index < 0 || index >= Count) return Array.Empty<object>();");
        sb.AppendLine();
        sb.AppendLine("            int start = _offsets[index];");
        sb.AppendLine("            int length = _offsets[index + 1] - start;");
        sb.AppendLine("            var values = new List<object>();");
        sb.AppendLine();
        sb.AppendLine("            using (var doc = JsonDocument.Parse(_text.Substring(start, length)))");
        sb.AppendLine("            {");
        sb.AppendLine("                foreach (var cell in doc.RootElement.EnumerateArray())");
        sb.AppendLine("                {");
        sb.AppendLine("                    values.Add(ConfigValue.ToObject(cell));");
        sb.AppendLine("                }");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            return values.ToArray();");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        /// <summary>只读取行首的数值（分组键），不解析整行</summary>");
        sb.AppendLine("        public int ReadKey(int index)");
        sb.AppendLine("        {");
        sb.AppendLine("            int i = _offsets[index];");
        sb.AppendLine("            int end = _offsets[index + 1];");
        sb.AppendLine("            if (i < end && _text[i] == '[') i++;");
        sb.AppendLine();
        sb.AppendLine("            int sign = 1;");
        sb.AppendLine("            int value = 0;");
        sb.AppendLine("            bool hit = false;");
        sb.AppendLine();
        sb.AppendLine("            for (; i < end; i++)");
        sb.AppendLine("            {");
        sb.AppendLine("                char c = _text[i];");
        sb.AppendLine("                if (c >= '0' && c <= '9')");
        sb.AppendLine("                {");
        sb.AppendLine("                    value = value * 10 + (c - '0');");
        sb.AppendLine("                    hit = true;");
        sb.AppendLine("                }");
        sb.AppendLine("                else if (c == '-')");
        sb.AppendLine("                {");
        sb.AppendLine("                    sign = -1;");
        sb.AppendLine("                }");
        sb.AppendLine("                else if (hit)");
        sb.AppendLine("                {");
        sb.AppendLine("                    break;");
        sb.AppendLine("                }");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            return sign * value;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        private static int[] SplitLines(string text)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (text.Length == 0) return new int[] { 0 };");
        sb.AppendLine();
        sb.AppendLine("            var offsets = new List<int> { 0 };");
        sb.AppendLine("            for (int i = 0; i < text.Length; i++)");
        sb.AppendLine("            {");
        sb.AppendLine("                if (text[i] == '\\n') offsets.Add(i + 1);");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            offsets.Add(text.Length);");
        sb.AppendLine("            return offsets.ToArray();");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    /// <summary>一行数据：首次访问字段时才解析</summary>");
        sb.AppendLine("    public sealed class ConfigRow");
        sb.AppendLine("    {");
        sb.AppendLine("        private readonly ConfigTable _table;");
        sb.AppendLine("        private readonly int _index;");
        sb.AppendLine("        private object[] _values;");
        sb.AppendLine();
        sb.AppendLine("        public ConfigRow(ConfigTable table, int index)");
        sb.AppendLine("        {");
        sb.AppendLine("            _table = table;");
        sb.AppendLine("            _index = index;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        public object[] Values");
        sb.AppendLine("        {");
        sb.AppendLine("            get { return _values ?? (_values = _table.ParseRow(_index)); }");
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
        sb.AppendLine("        public static int[] ToIntArray(JsonElement cell)");
        sb.AppendLine("        {");
        sb.AppendLine("            var values = new List<int>();");
        sb.AppendLine("            if (cell.ValueKind != JsonValueKind.Array) return values.ToArray();");
        sb.AppendLine("            foreach (var item in cell.EnumerateArray()) values.Add(ToInt(item));");
        sb.AppendLine("            return values.ToArray();");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        /// <summary>ids 已升序时返回 null，否则返回按 id 排序的行号表</summary>");
        sb.AppendLine("        public static int[] BuildOrder(int[] ids)");
        sb.AppendLine("        {");
        sb.AppendLine("            for (int i = 1; i < ids.Length; i++)");
        sb.AppendLine("            {");
        sb.AppendLine("                if (ids[i] <= ids[i - 1])");
        sb.AppendLine("                {");
        sb.AppendLine("                    var order = new int[ids.Length];");
        sb.AppendLine("                    for (int j = 0; j < order.Length; j++) order[j] = j;");
        sb.AppendLine("                    Array.Sort(order, (a, b) => ids[a].CompareTo(ids[b]));");
        sb.AppendLine("                    return order;");
        sb.AppendLine("                }");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            return null;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        /// <summary>二分查找主键所在行，找不到返回 -1</summary>");
        sb.AppendLine("        public static int IndexOfKey(int[] ids, int[] order, int id)");
        sb.AppendLine("        {");
        sb.AppendLine("            int low = 0;");
        sb.AppendLine("            int high = ids.Length - 1;");
        sb.AppendLine();
        sb.AppendLine("            while (low <= high)");
        sb.AppendLine("            {");
        sb.AppendLine("                int mid = (low + high) >> 1;");
        sb.AppendLine("                int row = order == null ? mid : order[mid];");
        sb.AppendLine("                int value = ids[row];");
        sb.AppendLine();
        sb.AppendLine("                if (value == id) return row;");
        sb.AppendLine("                if (value < id) low = mid + 1; else high = mid - 1;");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            return -1;");
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
            ValueKind.Enum => Ident(type.Name),
            ValueKind.RowRef => "number",
            _ => "any"
        };

        return type.IsArray ? $"{core}[]" : core;
    }

    private static string TsValueOf(SchemaColumn column)
    {
        var raw = $"this._parse()[{column.Index}]";

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
