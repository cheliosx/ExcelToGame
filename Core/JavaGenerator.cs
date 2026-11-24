using System.Text;
using ExcelToGame.Models;
using ExcelToGame.Utils;

namespace ExcelToGame.Core;

/// <summary>
/// Java 服务端产物。结构与 C# 版一一对应：
/// 实体按行懒解析、容器负责主键/分组索引；
/// JSON 解析交给 Jackson（jackson-databind），不再自带解析器。
/// </summary>
public partial class CodeGenerator
{
    /// <summary>生成代码的包名</summary>
    public const string JavaPackage = "game.config";

    /// <summary>包在输出目录下的相对路径，产物可直接放进 src/main/java</summary>
    public static string JavaPackagePath => Path.Combine("game", "config");

    /// <summary>Java 12 个关键字里会出现在字段名上的部分（Pascal 后基本不会命中，兜底用）</summary>
    private static readonly HashSet<string> JavaKeywords = new(StringComparer.Ordinal)
    {
        "abstract", "assert", "boolean", "break", "byte", "case", "catch", "char", "class", "const",
        "continue", "default", "do", "double", "else", "enum", "extends", "final", "finally", "float",
        "for", "goto", "if", "implements", "import", "instanceof", "int", "interface", "long", "native",
        "new", "package", "private", "protected", "public", "return", "short", "static", "strictfp",
        "super", "switch", "synchronized", "this", "throw", "throws", "transient", "try", "void",
        "volatile", "while", "true", "false", "null", "var", "record", "sealed", "permits", "yield"
    };

    // ---------------------------------------------------------------- 全局产物

    /// <summary>Java 运行时：ConfigValue / ConfigRow / ConfigTable / LanguageManager（依赖 Jackson）</summary>
    public async Task GenerateJavaRuntime(string javaDir)
    {
        var files = new (string Name, string Body)[]
        {
            ("ConfigValue", ConfigValueBody),
            ("ConfigRow", ConfigRowBody),
            ("ConfigTable", ConfigTableBody),
            ("LanguageManager", LanguageManagerBody)
        };

        foreach (var (name, body) in files)
        {
            await FileUtil.WriteTextAsync(Path.Combine(javaDir, $"{name}.java"), JavaSource(body));
        }

        Logger.Success($"生成Java运行时: {files.Length} 个类");
    }

    /// <summary>所有枚举统一输出到 Enums.java（嵌套在 Enums 里，避免类名冲突）</summary>
    public async Task GenerateJavaEnums(List<SheetData> sheets, string javaDir)
    {
        var enums = sheets.Where(s => s.Kind == TableKind.Enum).ToList();
        if (enums.Count == 0)
        {
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"package {JavaPackage};");
        sb.AppendLine();
        sb.AppendLine("/**");
        sb.AppendLine(" * 表格导出的枚举统一放在这里，与手写枚举区分开，也避免命名冲突。");
        sb.AppendLine(" */");
        sb.AppendLine("public final class Enums {");
        sb.AppendLine();
        sb.AppendLine("    private Enums() {");
        sb.AppendLine("    }");

        foreach (var sheet in enums)
        {
            AppendJavaEnum(sb, sheet);
        }

        sb.AppendLine("}");

        var path = Path.Combine(javaDir, "Enums.java");
        await FileUtil.WriteTextAsync(path, sb.ToString());
        Logger.Success("生成Java枚举: Enums.java");
    }

    /// <summary>生成 Java 统一入口 ExcelData.java，登记目录下所有表格文件</summary>
    public async Task GenerateJavaEntry(List<ExcelFileData> files, string javaDir)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"package {JavaPackage};");
        sb.AppendLine();
        sb.AppendLine("import java.util.Locale;");
        sb.AppendLine();
        sb.AppendLine("/** 所有表格文件的统一入口 */");
        sb.AppendLine("public class ExcelData {");

        foreach (var file in files)
        {
            var type = JavaRootType(file);
            sb.AppendLine($"    private final {type} {JavaEntryField(file)} = new {type}();");
        }

        sb.AppendLine();
        sb.AppendLine("    private String langCode = \"zh-Hans\";");
        sb.AppendLine();
        sb.AppendLine("    /** 当前语言码，如 zh-Hans / en-US */");
        sb.AppendLine("    public String getLang() {");
        sb.AppendLine("        return langCode;");
        sb.AppendLine("    }");

        foreach (var file in files)
        {
            var type = JavaRootType(file);
            var name = Pascal(file.OutputName);
            sb.AppendLine();
            sb.AppendLine($"    public {type} get{name}() {{");
            sb.AppendLine($"        return {JavaEntryField(file)};");
            sb.AppendLine("    }");
        }

        sb.AppendLine();
        sb.AppendLine("    /**");
        sb.AppendLine("     * 初始化多语言");
        sb.AppendLine("     * @param lang 语言码，如 \"zh-Hans\" / \"en-US\"");
        sb.AppendLine("     * @param json 该语言的 key -> 文本");
        sb.AppendLine("     */");
        sb.AppendLine("    public void initLanguage(String lang, String json) {");
        sb.AppendLine("        this.langCode = lang;");
        sb.AppendLine("        LanguageManager.load(json);");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    /**");
        sb.AppendLine("     * 按文件名加载一个表格文件的 JSON（文件名不区分大小写）");
        sb.AppendLine("     * @param fileName 文件名（输出名，如 \"example\"）");
        sb.AppendLine("     * @param json 该文件对应的 JSON");
        sb.AppendLine("     */");
        sb.AppendLine("    public void load(String fileName, String json) {");
        sb.AppendLine("        String key = fileName == null ? \"\" : fileName.toLowerCase(Locale.ROOT);");
        sb.AppendLine();
        sb.AppendLine("        switch (key) {");

        foreach (var file in files)
        {
            var key = Ident(file.OutputName).ToLowerInvariant();
            sb.AppendLine($"            case \"{key}\":");
            sb.AppendLine($"                {JavaEntryField(file)}.load(json);");
            sb.AppendLine("                return;");
        }

        sb.AppendLine("            default:");
        sb.AppendLine("                throw new IllegalArgumentException(\"ExcelData: 未找到配置文件 \" + fileName);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        var path = Path.Combine(javaDir, "ExcelData.java");
        await FileUtil.WriteTextAsync(path, sb.ToString());
        Logger.Success("生成Java入口: ExcelData.java");
    }

    // ---------------------------------------------------------------- 单个表格文件

    public async Task GenerateJava(ExcelFileData file, List<TableSchema> schemas, string javaDir)
    {
        var classes = new List<(string Name, string Content)>();

        foreach (var schema in schemas.Where(s => s.Kind is not (TableKind.Enum or TableKind.Param)))
        {
            classes.Add((Pascal(schema.Name), BuildJavaEntity(schema)));
        }

        foreach (var schema in schemas.Where(s => s.Kind == TableKind.Param))
        {
            classes.Add((Pascal(schema.Name) + "Config", BuildJavaParam(schema)));
        }

        foreach (var schema in schemas.Where(s => s.Kind == TableKind.Keyed))
        {
            classes.Add((Pascal(schema.Name) + "Map", BuildJavaMap(schema)));
        }

        foreach (var schema in schemas.Where(s => s.Kind == TableKind.Array))
        {
            classes.Add((Pascal(schema.Name) + "List", BuildJavaList(schema)));
        }

        foreach (var schema in schemas.Where(s => s.Kind == TableKind.Group))
        {
            classes.Add((Pascal(schema.Name) + "Group", BuildJavaGroup(schema)));
        }

        var rootType = JavaRootType(file);
        classes.Add((rootType, BuildJavaRoot(file, schemas, rootType)));

        foreach (var (name, content) in classes)
        {
            await FileUtil.WriteTextAsync(Path.Combine(javaDir, $"{name}.java"), content);
        }

        Logger.Success($"生成Java: {classes.Count} 个类 ({file.OutputName})");
    }

    /// <summary>实体类：一行一个对象，首次访问字段才解析本行</summary>
    private static string BuildJavaEntity(TableSchema schema)
    {
        var className = Pascal(schema.Name);
        var fieldNames = BuildJavaFieldNames(schema);

        var sb = new StringBuilder();
        sb.AppendLine($"package {JavaPackage};");
        sb.AppendLine();
        sb.AppendLine("import com.fasterxml.jackson.databind.JsonNode;");
        sb.AppendLine("import java.util.List;");
        sb.AppendLine();
        sb.AppendLine($"/** {Doc(schema.Sheet.ChineseName)} */");
        sb.AppendLine($"public class {className} {{");
        sb.AppendLine();
        sb.AppendLine("    private final ConfigRow _row;");
        sb.AppendLine();
        sb.AppendLine($"    public {className}(ConfigRow row) {{");
        sb.AppendLine("        this._row = row;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    private JsonNode[] values() {");
        sb.AppendLine("        return _row.values();");
        sb.AppendLine("    }");

        foreach (var column in schema.Columns)
        {
            var member = fieldNames[Key(column)];
            sb.AppendLine();
            sb.AppendLine($"    /** {Doc(CommentOf(column.Field))} */");
            sb.AppendLine($"    public {JavaTypeOf(column.Type)} get{member}() {{");
            sb.AppendLine($"        return {JavaValueOf(column)};");
            sb.AppendLine("    }");
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    /// <summary>参数表：key/value 转置，整表只有一个对象</summary>
    private static string BuildJavaParam(TableSchema schema)
    {
        var className = Pascal(schema.Name) + "Config";
        var fieldNames = BuildJavaFieldNames(schema);

        var sb = new StringBuilder();
        sb.AppendLine($"package {JavaPackage};");
        sb.AppendLine();
        sb.AppendLine("import com.fasterxml.jackson.databind.JsonNode;");
        sb.AppendLine("import java.util.ArrayList;");
        sb.AppendLine("import java.util.List;");
        sb.AppendLine();
        sb.AppendLine($"/** {Doc(schema.Sheet.ChineseName)} */");
        sb.AppendLine($"public class {className} {{");

        foreach (var column in schema.Columns)
        {
            var member = fieldNames[Key(column)];
            sb.AppendLine($"    /** {Doc(CommentOf(column.Field))} */");
            sb.AppendLine($"    private {JavaTypeOf(column.Type)} {JavaFieldName(member)}{JavaDefaultOf(column.Type)};");
            sb.AppendLine();
        }

        foreach (var column in schema.Columns)
        {
            var member = fieldNames[Key(column)];
            sb.AppendLine($"    public {JavaTypeOf(column.Type)} get{member}() {{");
            sb.AppendLine($"        return {JavaFieldName(member)};");
            sb.AppendLine("    }");
            sb.AppendLine();
        }

        sb.AppendLine("    public void load(String json) {");
        sb.AppendLine("        loadSection(ConfigValue.parse(json));");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    void loadSection(JsonNode value) {");
        sb.AppendLine("        if (value == null || !value.isObject()) {");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        JsonNode fields = value;");

        foreach (var column in schema.Columns)
        {
            var member = fieldNames[Key(column)];
            sb.AppendLine();
            sb.AppendLine($"        if (fields.has(\"{column.Name}\")) {{");
            sb.AppendLine($"            {JavaFieldName(member)} = {JavaReadCellOf(column)};");
            sb.AppendLine("        }");
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    /// <summary>主键表容器：主键 -> 行号索引，取行时才创建实体</summary>
    private static string BuildJavaMap(TableSchema schema)
    {
        var entity = Pascal(schema.Name);
        var className = entity + "Map";

        var sb = new StringBuilder();
        sb.AppendLine($"package {JavaPackage};");
        sb.AppendLine();
        sb.AppendLine("import com.fasterxml.jackson.databind.JsonNode;");
        sb.AppendLine("import java.util.HashMap;");
        sb.AppendLine("import java.util.Map;");
        sb.AppendLine("import java.util.function.BiConsumer;");
        sb.AppendLine();
        sb.AppendLine($"/** {Doc(schema.Sheet.ChineseName)} */");
        sb.AppendLine($"public class {className} {{");
        sb.AppendLine();
        sb.AppendLine("    private final ConfigTable _table = new ConfigTable();");
        sb.AppendLine();
        sb.AppendLine("    /** 主键 -> 行号 */");
        sb.AppendLine("    private final Map<Integer, Integer> _index = new HashMap<>();");
        sb.AppendLine();
        sb.AppendLine("    /** 已访问过的行（只缓存用到的实体） */");
        sb.AppendLine($"    private final Map<Integer, {entity}> _data = new HashMap<>();");
        sb.AppendLine();
        sb.AppendLine("    public int getCount() {");
        sb.AppendLine("        return _table.count();");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    public void load(String json) {");
        sb.AppendLine("        loadSection(ConfigValue.parse(json));");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    void loadSection(JsonNode value) {");
        sb.AppendLine("        _table.loadSection(value);");
        sb.AppendLine("        _index.clear();");
        sb.AppendLine("        _data.clear();");
        sb.AppendLine();
        sb.AppendLine("        for (int i = 0; i < _table.count(); i++) {");
        sb.AppendLine("            _index.put(_table.readKey(i), i);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    /** 按主键取一行：定位到行号后才创建实体并解析 */");
        sb.AppendLine($"    public {entity} findById(int id) {{");
        sb.AppendLine($"        {entity} cached = _data.get(id);");
        sb.AppendLine("        if (cached != null) {");
        sb.AppendLine("            return cached;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        Integer row = _index.get(id);");
        sb.AppendLine("        if (row == null) {");
        sb.AppendLine("            return null;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine($"        {entity} item = new {entity}(_table.row(row));");
        sb.AppendLine("        _data.put(id, item);");
        sb.AppendLine("        return item;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    /** 全表遍历：逐行创建实体，不驻留整表对象 */");
        sb.AppendLine($"    public void forEach(BiConsumer<{entity}, Integer> callback) {{");
        sb.AppendLine("        for (int i = 0; i < _table.count(); i++) {");
        sb.AppendLine($"            callback.accept(new {entity}(_table.row(i)), i);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    /// <summary>数组表容器：按下标访问，访问到的行才解析</summary>
    private static string BuildJavaList(TableSchema schema)
    {
        var entity = Pascal(schema.Name);
        var className = entity + "List";

        var sb = new StringBuilder();
        sb.AppendLine($"package {JavaPackage};");
        sb.AppendLine();
        sb.AppendLine("import com.fasterxml.jackson.databind.JsonNode;");
        sb.AppendLine("import java.util.HashMap;");
        sb.AppendLine("import java.util.Map;");
        sb.AppendLine("import java.util.function.BiConsumer;");
        sb.AppendLine();
        sb.AppendLine($"/** {Doc(schema.Sheet.ChineseName)} */");
        sb.AppendLine($"public class {className} {{");
        sb.AppendLine();
        sb.AppendLine("    private final ConfigTable _table = new ConfigTable();");
        sb.AppendLine();
        sb.AppendLine("    /** 已访问过的行（只缓存用到的实体） */");
        sb.AppendLine($"    private final Map<Integer, {entity}> _data = new HashMap<>();");
        sb.AppendLine();
        sb.AppendLine("    public int getCount() {");
        sb.AppendLine("        return _table.count();");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    public void load(String json) {");
        sb.AppendLine("        loadSection(ConfigValue.parse(json));");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    void loadSection(JsonNode value) {");
        sb.AppendLine("        _table.loadSection(value);");
        sb.AppendLine("        _data.clear();");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine($"    public {entity} findByIndex(int index) {{");
        sb.AppendLine("        if (index < 0 || index >= _table.count()) {");
        sb.AppendLine("            return null;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine($"        {entity} cached = _data.get(index);");
        sb.AppendLine("        if (cached != null) {");
        sb.AppendLine("            return cached;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine($"        {entity} item = new {entity}(_table.row(index));");
        sb.AppendLine("        _data.put(index, item);");
        sb.AppendLine("        return item;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine($"    public void forEach(BiConsumer<{entity}, Integer> callback) {{");
        sb.AppendLine("        for (int i = 0; i < _table.count(); i++) {");
        sb.AppendLine($"            callback.accept(new {entity}(_table.row(i)), i);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    /// <summary>分组表容器：分组键 -> 行号列表</summary>
    private static string BuildJavaGroup(TableSchema schema)
    {
        var entity = Pascal(schema.Name);
        var className = entity + "Group";

        var sb = new StringBuilder();
        sb.AppendLine($"package {JavaPackage};");
        sb.AppendLine();
        sb.AppendLine("import com.fasterxml.jackson.databind.JsonNode;");
        sb.AppendLine("import java.util.ArrayList;");
        sb.AppendLine("import java.util.HashMap;");
        sb.AppendLine("import java.util.List;");
        sb.AppendLine("import java.util.Map;");
        sb.AppendLine();
        sb.AppendLine($"/** {Doc(schema.Sheet.ChineseName)} */");
        sb.AppendLine($"public class {className} {{");
        sb.AppendLine();
        sb.AppendLine("    private final ConfigTable _table = new ConfigTable();");
        sb.AppendLine();
        sb.AppendLine("    /** 分组键 -> 行号列表 */");
        sb.AppendLine("    private final Map<Integer, List<Integer>> _index = new HashMap<>();");
        sb.AppendLine();
        sb.AppendLine("    /** 已访问过的行（只缓存用到的实体） */");
        sb.AppendLine($"    private final Map<Integer, {entity}> _data = new HashMap<>();");
        sb.AppendLine();
        sb.AppendLine("    public int getCount() {");
        sb.AppendLine("        return _table.count();");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    public void load(String json) {");
        sb.AppendLine("        loadSection(ConfigValue.parse(json));");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    void loadSection(JsonNode value) {");
        sb.AppendLine("        _table.loadSection(value);");
        sb.AppendLine("        _index.clear();");
        sb.AppendLine("        _data.clear();");
        sb.AppendLine();
        sb.AppendLine("        for (int i = 0; i < _table.count(); i++) {");
        sb.AppendLine("            int key = _table.readKey(i);");
        sb.AppendLine("            List<Integer> rows = _index.get(key);");
        sb.AppendLine();
        sb.AppendLine("            if (rows == null) {");
        sb.AppendLine("                rows = new ArrayList<>();");
        sb.AppendLine("                _index.put(key, rows);");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            rows.add(i);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine($"    public List<{entity}> findGroupById(int id) {{");
        sb.AppendLine("        List<Integer> rows = _index.get(id);");
        sb.AppendLine("        if (rows == null) {");
        sb.AppendLine("            return null;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine($"        List<{entity}> result = new ArrayList<>(rows.size());");
        sb.AppendLine("        for (int i = 0; i < rows.size(); i++) {");
        sb.AppendLine("            result.add(getEntity(rows.get(i)));");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        return result;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    /** 同一行只创建一次实体 */");
        sb.AppendLine($"    private {entity} getEntity(int row) {{");
        sb.AppendLine($"        {entity} cached = _data.get(row);");
        sb.AppendLine("        if (cached != null) {");
        sb.AppendLine("            return cached;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine($"        {entity} item = new {entity}(_table.row(row));");
        sb.AppendLine("        _data.put(row, item);");
        sb.AppendLine("        return item;");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    /// <summary>单个 Excel 文件的配置根：解析一次，按页签名分发到各容器</summary>
    private static string BuildJavaRoot(ExcelFileData file, List<TableSchema> schemas, string rootType)
    {
        var containers = schemas.Where(s => s.Kind != TableKind.Enum).ToList();

        var sb = new StringBuilder();
        sb.AppendLine($"package {JavaPackage};");
        sb.AppendLine();
        sb.AppendLine("import com.fasterxml.jackson.databind.JsonNode;");
        sb.AppendLine();
        sb.AppendLine($"/** {Doc(file.FileName)} 配置根 */");
        sb.AppendLine($"public class {rootType} {{");

        foreach (var schema in containers)
        {
            var type = JavaContainerType(schema);
            sb.AppendLine($"    private final {type} {LowerFirst(type)} = new {type}();");
        }

        foreach (var schema in containers)
        {
            var type = JavaContainerType(schema);
            sb.AppendLine();
            sb.AppendLine($"    public {type} get{type}() {{");
            sb.AppendLine($"        return {LowerFirst(type)};");
            sb.AppendLine("    }");
        }

        sb.AppendLine();
        sb.AppendLine("    public void load(String json) {");
        sb.AppendLine("        JsonNode root = ConfigValue.parse(json);");
        sb.AppendLine("        if (root == null || !root.isObject()) {");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        JsonNode section;");

        foreach (var schema in containers)
        {
            sb.AppendLine();
            sb.AppendLine($"        section = root.get(\"{schema.Name}\");");
            sb.AppendLine("        if (section != null && !section.isNull()) {");
            sb.AppendLine($"            {LowerFirst(JavaContainerType(schema))}.loadSection(section);");
            sb.AppendLine("        }");
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    // ---------------------------------------------------------------- 枚举

    private static void AppendJavaEnum(StringBuilder sb, SheetData sheet)
    {
        var name = Pascal(sheet.SheetName);
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        sb.AppendLine();
        sb.AppendLine($"    /** {Doc(sheet.ChineseName)} */");
        sb.AppendLine($"    public enum {name} {{");

        if (sheet.EnumEntries.Count == 0)
        {
            sb.AppendLine("        ;");
        }
        else
        {
            for (int i = 0; i < sheet.EnumEntries.Count; i++)
            {
                var entry = sheet.EnumEntries[i];
                var member = UniqueName(SafeJavaIdentifier(Pascal(entry.Name)), used);
                var suffix = i == sheet.EnumEntries.Count - 1 ? ";" : ",";

                if (!string.IsNullOrWhiteSpace(entry.Comment))
                {
                    sb.AppendLine($"        /** {Doc(entry.Comment)} */");
                }

                sb.AppendLine($"        {member}({entry.Value}){suffix}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("        private final int value;");
        sb.AppendLine();
        sb.AppendLine($"        {name}(int value) {{");
        sb.AppendLine("            this.value = value;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        public int getValue() {");
        sb.AppendLine("            return value;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        /** 由配置里的数值取枚举，找不到返回 null */");
        sb.AppendLine($"        public static {name} of(int value) {{");
        sb.AppendLine($"            for ({name} item : values()) {{");
        sb.AppendLine("                if (item.value == value) {");
        sb.AppendLine("                    return item;");
        sb.AppendLine("                }");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            return null;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
    }

    // ---------------------------------------------------------------- 类型与表达式映射

    private static string JavaTypeOf(ResolvedType type)
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
            ValueKind.Bool => "boolean",
            ValueKind.String => "String",
            ValueKind.Language => "String",
            ValueKind.Enum => "Enums." + Pascal(type.Name),
            ValueKind.RowRef => "int",
            _ => "Object"
        };

        if (!type.IsArray)
        {
            return core;
        }

        return core switch
        {
            "int" => "List<Integer>",
            "long" => "List<Long>",
            "float" => "List<Float>",
            "double" => "List<Double>",
            "boolean" => "List<Boolean>",
            "String" => "List<String>",
            "Object" => "List<Object>",
            _ => $"List<{core}>"
        };
    }

    /// <summary>实体取值表达式：values()[n] 是 JSON 解析后的 Object</summary>
    private static string JavaValueOf(SchemaColumn column) =>
        JavaReadExpression(column, $"values()[{column.Index}]");

    /// <summary>参数表取值表达式：fields.get("name") 是 JSON 解析后的 Object</summary>
    private static string JavaReadCellOf(SchemaColumn column) =>
        JavaReadExpression(column, $"fields.get(\"{column.Name}\")");

    private static string JavaReadExpression(SchemaColumn column, string cell)
    {
        var type = column.Type;
        var element = type with { IsArray = false };

        if (type.IsArray)
        {
            return element.Kind switch
            {
                ValueKind.Bool => $"ConfigValue.toBoolList({cell})",
                ValueKind.String or ValueKind.Language => $"ConfigValue.toStrList({cell})",
                ValueKind.Number => element.Name switch
                {
                    "long" => $"ConfigValue.toLongList({cell})",
                    "float" => $"ConfigValue.toFloatList({cell})",
                    "double" => $"ConfigValue.toDoubleList({cell})",
                    _ => $"ConfigValue.toIntList({cell})"
                },
                ValueKind.Enum => $"ConfigValue.toEnumList({cell}, Enums.{Pascal(element.Name)}::of)",
                ValueKind.RowRef => $"ConfigValue.toIntList({cell})",
                _ => $"ConfigValue.toObjectList({cell})"
            };
        }

        return type.Kind switch
        {
            ValueKind.Number => element.Name switch
            {
                "long" => $"ConfigValue.toLong({cell})",
                "float" => $"ConfigValue.toFloat({cell})",
                "double" => $"ConfigValue.toDouble({cell})",
                _ => $"ConfigValue.toInt({cell})"
            },
            ValueKind.Bool => $"ConfigValue.toBool({cell})",
            ValueKind.Language => $"LanguageManager.getText(ConfigValue.toStr({cell}))",
            ValueKind.Enum => $"Enums.{Pascal(type.Name)}.of(ConfigValue.toInt({cell}))",
            ValueKind.RowRef => $"ConfigValue.toInt({cell})",
            ValueKind.Any or ValueKind.Unknown => $"ConfigValue.toObject({cell})",
            _ => $"ConfigValue.toStr({cell})"
        };
    }

    private static string JavaDefaultOf(ResolvedType type)
    {
        if (type.IsArray)
        {
            return " = new ArrayList<>()";
        }

        return type.Kind switch
        {
            ValueKind.String or ValueKind.Language => " = \"\"",
            _ => string.Empty
        };
    }

    // ---------------------------------------------------------------- 命名工具

    private static string JavaRootType(ExcelFileData file) => Pascal(file.OutputName) + "Config";

    /// <summary>入口里的字段名：文件名的小驼峰（example / attr）</summary>
    private static string JavaEntryField(ExcelFileData file) =>
        JavaFieldName(JavaMember(Pascal(file.OutputName)));

    private static string JavaContainerType(TableSchema schema) => schema.Kind switch
    {
        TableKind.Keyed => Pascal(schema.Name) + "Map",
        TableKind.Array => Pascal(schema.Name) + "List",
        TableKind.Group => Pascal(schema.Name) + "Group",
        _ => Pascal(schema.Name) + "Config"
    };

    private static Dictionary<string, string> BuildJavaFieldNames(TableSchema schema)
    {
        var result = new Dictionary<string, string>();
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var column in schema.Columns)
        {
            var member = UniqueName(JavaMember(Pascal(column.Field.Name)), used);
            result[Key(column)] = member;
        }

        return result;
    }

    /// <summary>getter 后缀：Object.getClass 这类冲突改名为 Class_</summary>
    private static string JavaMember(string pascal) =>
        pascal == "Class" ? pascal + "_" : pascal;

    private static string JavaFieldName(string member)
    {
        var name = LowerFirst(member);
        return JavaKeywords.Contains(name) ? name + "_" : name;
    }

    private static string LowerFirst(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        // ID / URL 这类连续大写保持原样，避免变成 iD
        if (name.Length > 1 && char.IsUpper(name[1]))
        {
            return name;
        }

        return char.ToLowerInvariant(name[0]) + name[1..];
    }

    private static string SafeJavaIdentifier(string name)
    {
        var ident = Ident(name);
        return JavaKeywords.Contains(ident) ? ident + "_" : ident;
    }

    /// <summary>javadoc 单行注释：去掉换行，避免 */ 提前闭合</summary>
    private static string Doc(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        return text.Replace("\r", " ").Replace("\n", " ").Replace("*/", "* /").Trim();
    }

    private static string JavaSource(string body) =>
        $"package {JavaPackage};\n\n" + JavaBody(body);

    /// <summary>去掉缩进块首尾空行，统一换行符</summary>
    private static string JavaBody(string body) =>
        body.Replace("\r\n", "\n").Trim('\n') + "\n";

    // ---------------------------------------------------------------- Java 运行时源码

    private const string ConfigValueBody = """
        import com.fasterxml.jackson.databind.JsonNode;
        import com.fasterxml.jackson.databind.ObjectMapper;
        import java.util.ArrayList;
        import java.util.List;
        import java.util.function.IntFunction;

        /**
         * 取值工具：把 Jackson 的 JsonNode 转成字段需要的基础类型。
         * 依赖 com.fasterxml.jackson.core:jackson-databind（2.13+ 均可用）。
         */
        final class ConfigValue {

            private static final ObjectMapper MAPPER = new ObjectMapper();

            private static final JsonNode[] EMPTY = new JsonNode[0];

            private ConfigValue() {
            }

            /** 解析 JSON 文本；空内容返回 null */
            static JsonNode parse(String json) {
                if (json == null || json.isEmpty()) {
                    return null;
                }

                try {
                    return MAPPER.readTree(json);
                } catch (Exception e) {
                    throw new IllegalStateException("JSON 解析失败: " + e.getMessage(), e);
                }
            }

            static int toInt(JsonNode node) {
                if (node == null || node.isNull()) {
                    return 0;
                }

                if (node.isNumber()) {
                    return node.intValue();
                }

                if (node.isBoolean()) {
                    return node.booleanValue() ? 1 : 0;
                }

                if (node.isTextual()) {
                    try {
                        return (int) Double.parseDouble(node.textValue().trim());
                    } catch (NumberFormatException e) {
                        return 0;
                    }
                }

                return 0;
            }

            static long toLong(JsonNode node) {
                if (node == null || node.isNull()) {
                    return 0L;
                }

                if (node.isNumber()) {
                    return node.longValue();
                }

                if (node.isBoolean()) {
                    return node.booleanValue() ? 1L : 0L;
                }

                if (node.isTextual()) {
                    try {
                        return (long) Double.parseDouble(node.textValue().trim());
                    } catch (NumberFormatException e) {
                        return 0L;
                    }
                }

                return 0L;
            }

            static float toFloat(JsonNode node) {
                if (node == null || node.isNull()) {
                    return 0f;
                }

                if (node.isNumber()) {
                    return node.floatValue();
                }

                if (node.isBoolean()) {
                    return node.booleanValue() ? 1f : 0f;
                }

                if (node.isTextual()) {
                    try {
                        return Float.parseFloat(node.textValue().trim());
                    } catch (NumberFormatException e) {
                        return 0f;
                    }
                }

                return 0f;
            }

            static double toDouble(JsonNode node) {
                if (node == null || node.isNull()) {
                    return 0d;
                }

                if (node.isNumber()) {
                    return node.doubleValue();
                }

                if (node.isBoolean()) {
                    return node.booleanValue() ? 1d : 0d;
                }

                if (node.isTextual()) {
                    try {
                        return Double.parseDouble(node.textValue().trim());
                    } catch (NumberFormatException e) {
                        return 0d;
                    }
                }

                return 0d;
            }

            static boolean toBool(JsonNode node) {
                if (node == null || node.isNull()) {
                    return false;
                }

                if (node.isBoolean()) {
                    return node.booleanValue();
                }

                if (node.isTextual()) {
                    String text = node.textValue().trim();
                    if (text.equalsIgnoreCase("true") || text.equalsIgnoreCase("yes")) {
                        return true;
                    }

                    if (text.isEmpty() || text.equalsIgnoreCase("false") || text.equalsIgnoreCase("no")) {
                        return false;
                    }
                }

                return toInt(node) != 0;
            }

            static String toStr(JsonNode node) {
                if (node == null || node.isNull()) {
                    return "";
                }

                if (node.isTextual()) {
                    return node.textValue();
                }

                if (node.isDouble() || node.isFloat()) {
                    double number = node.doubleValue();
                    if (number == Math.rint(number) && !Double.isInfinite(number)) {
                        return String.valueOf((long) number);
                    }
                }

                return node.isValueNode() ? node.asText() : node.toString();
            }

            static List<Integer> toIntList(JsonNode node) {
                List<Integer> result = new ArrayList<>();
                if (node == null || !node.isArray()) {
                    return result;
                }

                for (JsonNode item : node) {
                    result.add(toInt(item));
                }

                return result;
            }

            static List<Long> toLongList(JsonNode node) {
                List<Long> result = new ArrayList<>();
                if (node == null || !node.isArray()) {
                    return result;
                }

                for (JsonNode item : node) {
                    result.add(toLong(item));
                }

                return result;
            }

            static List<Float> toFloatList(JsonNode node) {
                List<Float> result = new ArrayList<>();
                if (node == null || !node.isArray()) {
                    return result;
                }

                for (JsonNode item : node) {
                    result.add(toFloat(item));
                }

                return result;
            }

            static List<Double> toDoubleList(JsonNode node) {
                List<Double> result = new ArrayList<>();
                if (node == null || !node.isArray()) {
                    return result;
                }

                for (JsonNode item : node) {
                    result.add(toDouble(item));
                }

                return result;
            }

            static List<Boolean> toBoolList(JsonNode node) {
                List<Boolean> result = new ArrayList<>();
                if (node == null || !node.isArray()) {
                    return result;
                }

                for (JsonNode item : node) {
                    result.add(toBool(item));
                }

                return result;
            }

            static List<String> toStrList(JsonNode node) {
                List<String> result = new ArrayList<>();
                if (node == null || !node.isArray()) {
                    return result;
                }

                for (JsonNode item : node) {
                    result.add(toStr(item));
                }

                return result;
            }

            static List<Object> toObjectList(JsonNode node) {
                List<Object> result = new ArrayList<>();
                if (node == null || !node.isArray()) {
                    return result;
                }

                for (JsonNode item : node) {
                    result.add(toObject(item));
                }

                return result;
            }

            /** 不限类型的字段：转成 Java 原生 Map / List / String / Number / Boolean */
            static Object toObject(JsonNode node) {
                if (node == null || node.isNull()) {
                    return null;
                }

                if (node.isTextual()) {
                    return node.textValue();
                }

                if (node.isBoolean()) {
                    return node.booleanValue();
                }

                if (node.isIntegralNumber()) {
                    if (node.canConvertToInt()) {
                        return node.intValue();
                    }

                    return node.longValue();
                }

                if (node.isNumber()) {
                    return node.doubleValue();
                }

                if (node.isArray()) {
                    return toObjectList(node);
                }

                if (node.isObject()) {
                    java.util.Map<String, Object> map = new java.util.LinkedHashMap<>();
                    java.util.Iterator<String> names = node.fieldNames();
                    while (names.hasNext()) {
                        String name = names.next();
                        map.put(name, toObject(node.get(name)));
                    }

                    return map;
                }

                return node.asText();
            }

            /** 枚举数组：按数值查回枚举，找不到保留 null */
            static <T> List<T> toEnumList(JsonNode node, IntFunction<T> mapper) {
                List<T> result = new ArrayList<>();
                if (node == null || !node.isArray()) {
                    return result;
                }

                for (JsonNode item : node) {
                    result.add(mapper.apply(toInt(item)));
                }

                return result;
            }

            /** JSON 段（字符串数组）-> 行文本数组；行是真数组时写回文本 */
            static String[] toRows(JsonNode node) {
                if (node == null || !node.isArray()) {
                    return new String[0];
                }

                String[] rows = new String[node.size()];
                for (int i = 0; i < rows.length; i++) {
                    JsonNode item = node.get(i);
                    rows[i] = item.isTextual() ? item.textValue() : item.toString();
                }

                return rows;
            }

            /** 从 "[1,...]" 里取首列数字（主键 / 分组键） */
            static int readKey(String row) {
                if (row == null || row.isEmpty()) {
                    return 0;
                }

                int start = row.charAt(0) == '[' ? 1 : 0;
                int end = row.indexOf(',', start);
                if (end < 0) {
                    end = row.length();
                }

                // 单列表的行形如 "[1]"，把结尾的 ] 和空白去掉再解析
                while (end > start && (row.charAt(end - 1) == ']' || Character.isWhitespace(row.charAt(end - 1)))) {
                    end--;
                }

                try {
                    return (int) Double.parseDouble(row.substring(start, end).trim());
                } catch (NumberFormatException e) {
                    return 0;
                }
            }

            /** 解析一行文本：数组内的每个元素保持 JsonNode，取值时再转 */
            static JsonNode[] parseRow(String row) {
                if (row == null || row.isEmpty()) {
                    return EMPTY;
                }

                JsonNode node = parse(row);
                if (node == null || !node.isArray()) {
                    return EMPTY;
                }

                JsonNode[] values = new JsonNode[node.size()];
                for (int i = 0; i < values.length; i++) {
                    values[i] = node.get(i);
                }

                return values;
            }
        }
        """;

    private const string ConfigRowBody = """
        import com.fasterxml.jackson.databind.JsonNode;

        /** 一行数据：首次访问字段时才解析 */
        public final class ConfigRow {

            private String data;
            private JsonNode[] values;

            public ConfigRow(String data) {
                this.data = data;
            }

            public JsonNode[] values() {
                if (values == null) {
                    values = ConfigValue.parseRow(data);
                    data = null;
                }

                return values;
            }
        }
        """;

    private const string ConfigTableBody = """
        import com.fasterxml.jackson.databind.JsonNode;

        /**
         * 行数据容器：只保存原始行文本，
         * 访问某行时才解析该行，避免一次性创建几十万个数组。
         */
        public final class ConfigTable {

            private String[] rows = new String[0];

            /** 由 JSON 的字符串数组加载 */
            public void load(String json) {
                this.rows = ConfigValue.toRows(ConfigValue.parse(json));
            }

            void loadSection(JsonNode value) {
                this.rows = ConfigValue.toRows(value);
            }

            public int count() {
                return rows.length;
            }

            public ConfigRow row(int index) {
                return new ConfigRow(index >= 0 && index < rows.length ? rows[index] : null);
            }

            /** 只读取行首的数值（分组键），不解析整行 */
            public int readKey(int index) {
                return ConfigValue.readKey(rows[index]);
            }
        }
        """;

    private const string LanguageManagerBody = """
        import com.fasterxml.jackson.databind.JsonNode;
        import java.util.HashMap;
        import java.util.Iterator;
        import java.util.Map;

        /** 语言表：key 为数字字符串，缺失时回退为 key 本身 */
        public final class LanguageManager {

            private static Map<String, String> texts = new HashMap<>();

            private LanguageManager() {
            }

            public static void load(String json) {
                Map<String, String> map = new HashMap<>();
                JsonNode root = ConfigValue.parse(json);

                if (root != null && root.isObject()) {
                    Iterator<String> names = root.fieldNames();
                    while (names.hasNext()) {
                        String name = names.next();
                        map.put(name, ConfigValue.toStr(root.get(name)));
                    }
                }

                texts = map;
            }

            public static String getText(String key) {
                if (key == null || key.isEmpty()) {
                    return "";
                }

                String text = texts.get(key);
                return text != null ? text : key;
            }
        }
        """;
}
