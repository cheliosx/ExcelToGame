using ExcelToGame.Models;

namespace ExcelToGame.Core;

/// <summary>
/// 把「按页签读取」的原始数据编译成导出结构：一个页签一张表，各表互不影响。
/// </summary>
public sealed class SchemaBuilder
{
    private readonly TypeRegistry _types;
    private readonly ExportTarget _target;

    public SchemaBuilder(TypeRegistry types, ExportTarget target)
    {
        _types = types;
        _target = target;
    }

    public List<TableSchema> Build(ExcelFileData file)
    {
        return file.DataSheets.Select(BuildTable).ToList();
    }

    private TableSchema BuildTable(SheetData sheet)
    {
        if (sheet.Kind == TableKind.Enum)
        {
            return new TableSchema
            {
                Sheet = sheet,
                Kind = TableKind.Enum,
                Columns = new List<SchemaColumn>(),
                Rows = new List<object?[]>()
            };
        }

        // 只导出目标端可见字段，列顺序即数据页签中的字段顺序
        var columns = sheet.FieldsFor(_target)
            .Select((field, index) => new SchemaColumn
            {
                Field = field,
                Index = index,
                Type = _types.Resolve(field)
            })
            .ToList();

        var rows = sheet.Rows.Select(row => BuildRow(columns, row)).ToList();

        var keyField = sheet.KeyField;
        var keyColumn = keyField == null
            ? null
            : columns.FirstOrDefault(c => c.Name.Equals(keyField.Name, StringComparison.OrdinalIgnoreCase));

        return new TableSchema
        {
            Sheet = sheet,
            Kind = sheet.Kind,
            Columns = columns,
            Rows = rows,
            KeyColumn = keyColumn
        };
    }

    private static object?[] BuildRow(List<SchemaColumn> columns, Dictionary<string, object?> row)
    {
        var values = new object?[columns.Count];

        foreach (var column in columns)
        {
            values[column.Index] = row.GetValueOrDefault(column.Field.Name) ?? DefaultValue(column.Type);
        }

        return values;
    }

    private static object? DefaultValue(ResolvedType type)
    {
        if (type.IsArray)
        {
            return new List<object?>();
        }

        return type.Kind switch
        {
            ValueKind.Number => 0,
            ValueKind.Bool => false,
            ValueKind.Enum => 0,
            ValueKind.RowRef => 0,
            ValueKind.String => string.Empty,
            ValueKind.Language => string.Empty,
            _ => null
        };
    }
}
