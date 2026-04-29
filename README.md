# Excel转游戏配置工具

基于 .NET 8 的 Excel 配置导出工具，支持导出 JSON、TypeScript（Cocos）、C#（Unity）。

## 一、功能特性

- ✅ 多 Sheet 支持，Sheet 名即表名
- ✅ 严格遵循策划表格式（符号列、END 标记）
- ✅ 支持数据类型：number、string、language、array<T>、自定义类型
- ✅ 多语言自动生成唯一 key，统一收集到 language.json
- ✅ 自定义类型检测，未定义类型报错
- ✅ 继承支持（extend 列）
- ✅ 枚举表特殊处理
- ✅ 客户端/服务端导出控制（client/server 列）

## 二、Excel 格式规范

### Sheet 结构

```
第1行：字段注释（中文说明）
第2行：字段名（英文）
第3行：数据类型
第4行+：数据
END行：结束标记
```

### 列结构

| 符号 | 字段1 | 字段2 | 字段3 | ... | END |
|------|-------|-------|-------|-----|-----|
| 1/0  | ...   | ...   | ...   | ... |     |

- **第一列（符号）**：1=导出，0=不导出
- **最后一列**：固定为 END，工具自动忽略

### 数据类型

| 类型 | 说明 | 示例 |
|------|------|------|
| `number` | 数字 | 100, 3.14 |
| `string` | 字符串 | "hello" |
| `language` | 多语言文本 | 自动收集到 language.json |
| `array<number>` | 数字数组 | [1,2,3] |
| `array<string>` | 字符串数组 | ["a","b"] |
| `example.skill` | 自定义类型 | 引用其他表 |

### 特殊列

- **client**：YES/NO，控制是否导出到客户端
- **server**：YES/NO，控制是否导出到服务端
- **extend**：继承其他表名

## 三、输出格式

### 1. JSON 格式

```json
{
    "setConfig": {
        "amendName": [10001, 100],
        "Recharge1": 5000
    },
    "setAspect": [
        "[101,\"7047\",4,...]",
        "[102,\"6907\",4,...]"
    ]
}
```

### 2. TypeScript 格式

```typescript
import { _lang } from "./_language";

export class _ItemConfig {
    public _data: any;
    get amendName(): number[] {
        return this._data['amendName'];
    }
}

export class _ItemAspect {
    public objects: any;
    get name(): string {
        return _lang.t(this.objects['2']);
    }
}

export class _Item {
    public static Config: _ItemConfig = new _ItemConfig();
    public static Aspect: _ItemAspect[] = [];
    public static AspectMap: _ItemAspectMap = new _ItemAspectMap();
    
    public static findById(id: number): _ItemAspect | undefined {
        return this.AspectMap.get(id);
    }
}
```

### 3. C# 格式

```csharp
namespace Game.Config
{
    public class ItemConfig
    {
        public int[] amendName { get; set; }
    }
    
    public class ItemAspect
    {
        public string name => LanguageManager.GetText(_nameKey);
        [JsonProperty] private string _nameKey;
    }
    
    public static class Item
    {
        public static ItemConfig Config { get; private set; }
        public static Dictionary<int, ItemAspect> AspectMap { get; private set; }
        
        public static ItemAspect FindById(int id) => AspectMap.TryGetValue(id, out var a) ? a : null;
    }
}
```

### 4. 多语言文件

**language.json**：
```json
{
    "Text_1001": "初始名字",
    "Item_1002_Name": "道具2",
    "Skill_1001_Desc": "技能描述"
}
```

**_language.ts**：
```typescript
export class _language {
    private _data: any;
    load(_data: any) { this._data = _data; }
    t(id: string): string { return this._data[id] || id; }
    replaceTemplate(template: string, ...values: any[]): string { ... }
}
export const _lang = new _language();
```

## 四、目录结构

```
ExcelToGame/
├── Program.cs              # 程序入口
├── ExcelToGame.csproj      # 项目文件
├── Core/
│   ├── ExcelReader.cs      # Excel读取（NPOI）
│   ├── TypeChecker.cs      # 类型检查
│   ├── LanguageGenerator.cs # 多语言生成
│   └── CodeGenerator.cs    # 代码生成
├── Models/
│   ├── FieldInfo.cs        # 字段信息
│   └── SheetData.cs        # Sheet数据
├── Utils/
│   ├── FileUtil.cs         # 文件工具
│   └── Logger.cs           # 日志工具
└── README.md               # 使用说明
```

## 五、使用方法

### 1. 安装依赖

```bash
dotnet restore
```

### 2. 编译

```bash
dotnet build -c Release
```

### 3. 运行

```bash
# 默认路径
./bin/Release/net8.0/ExcelToGame.exe

# 指定输入输出路径
./bin/Release/net8.0/ExcelToGame.exe [Excel目录] [输出目录]
```

### 4. 输出目录

```
Output/
├── Json/           # JSON配置文件
├── TS/             # TypeScript代码
│   └── _language.ts
├── CSharp/         # C#代码
└── language.json   # 多语言文件
```

## 六、Excel 示例

### 道具表 (item)

| 符号 | Id | name | type | value | client | server | END |
|------|----|------|------|-------|--------|--------|-----|
| 注释 | ID | 名称 | 类型 | 数值 | 客户端 | 服务端 |     |
| 类型 | number | language | number | number | string | string |     |
| 1 | 1001 | 铁剑 | 1 | 100 | YES | YES |     |
| 1 | 1002 | 木盾 | 2 | 50 | YES | NO |     |
| END | | | | | | |     |

### 参数表 (setConfig)

| 符号 | key | value | END |
|------|-----|-------|-----|
| 注释 | 键 | 值 |     |
| 类型 | string | number |     |
| 1 | Recharge1 | 5000 |     |
| 1 | amendName | 100 |     |
| END | | |     |

## 七、注意事项

1. **第一列必须是符号列**：1=导出，0=不导出
2. **最后一列必须是 END**：工具自动识别并忽略
3. **language 类型字段**：自动生成唯一 key，格式为 `表名_ID_字段名`
4. **自定义类型**：如 `example.skill`，必须确保 skill 表已定义
5. **数组格式**：支持 `[1,2,3]` 或 `1,2,3` 两种写法

## 八、错误处理

- **红色错误**：类型未定义、Sheet 解析失败
- **黄色警告**：空值、格式不规范
- **绿色成功**：文件生成成功

## 九、依赖

- .NET 8
- NPOI 2.6.2（Excel解析）
- System.Text.Encoding.CodePages 8.0.0
