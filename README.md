# Excel转配置工具 - Unity/Cocos Creator双引擎支持

基于 .NET 8 开发的Excel配置导出工具，支持导出JSON、C#实体类、TypeScript接口，适用于Unity和Cocos Creator游戏引擎。

## 一、功能特性

- **多引擎支持**：同时生成Unity(C#)和Cocos Creator(TypeScript)代码
- **完整类型系统**：支持基础类型、枚举、数组、嵌套对象
- **继承关系**：支持表格间的类继承关系
- **数据校验**：字段名校验、类型校验、主键重复检测、枚举值校验
- **智能过滤**：导出开关控制、END标记、自动排除临时文件
- **中文支持**：完整的中文注释和日志输出

## 二、项目结构

```
ExcelToGame/
├── Program.cs                    # 程序入口
├── ExcelToGame.csproj           # 项目文件
├── Config/
│   └── AppConfig.cs             # 配置管理
├── Core/
│   ├── ExcelReader.cs           # Excel读取模块
│   ├── TypeParser.cs            # 类型解析模块
│   ├── DataValidator.cs         # 数据校验模块
│   ├── EnumManager.cs           # 枚举管理模块
│   ├── JsonExporter.cs          # JSON导出模块
│   ├── CSharpCodeGenerator.cs   # C#代码生成
│   ├── TypeScriptGenerator.cs   # TypeScript代码生成
│   └── ExcelExporter.cs         # 主导出控制器
├── Models/
│   ├── FieldInfo.cs             # 字段信息模型
│   ├── RowData.cs               # 行数据模型
│   └── TableData.cs             # 表格数据模型
├── Utils/
│   ├── Logger.cs                # 日志工具
│   └── FileUtils.cs              # 文件工具
├── ExcelTemplates/              # Excel模板示例
│   └── README.md                # 填表规范说明
└── config.json                  # 配置文件（运行后生成）
```

## 三、快速开始

### 1. 安装依赖

```bash
# 安装NPOI库（Excel解析）
dotnet add package NPOI --version 2.6.2

# 安装编码支持
dotnet add package System.Text.Encoding.CodePages --version 8.0.0
```

或直接还原：

```bash
dotnet restore
```

### 2. 编译项目

```bash
dotnet build -c Release
```

### 3. 运行工具

```bash
dotnet run
# 或运行编译后的可执行文件
./bin/Release/net8.0/ExcelToGame.exe
```

## 四、配置说明

首次运行会自动生成 `config.json` 配置文件：

```json
{
  "inputDirectory": "Excel",           # Excel输入目录
  "outputDirectory": "Output",         # 输出根目录
  "jsonOutputPath": "Json",            # JSON输出子目录
  "typeScriptOutputPath": "TypeScript", # TS输出子目录
  "csharpOutputPath": "CSharp",        # C#输出子目录
  "enumOutputPath": "Enums",           # 枚举输出子目录
  "csharpNamespace": "Game.Config",    # C#命名空间
  "exportSwitchColumnName": "Export",  # 导出开关列名
  "rowEndMarker": "END",               # 行结束标记
  "columnEndMarker": "END",            # 列结束标记
  "formatJson": true                   # 是否格式化JSON
}
```

## 五、Excel表格规范

### 表头结构（固定4行）

| 行号 | 内容 | 说明 |
|------|------|------|
| 第1行 | 中文注释 | 生成代码注释 |
| 第2行 | 英文字段名 | 变量名 |
| 第3行 | 数据类型 | 类型声明 |
| 第4行+ | 数据 | 实际配置数据 |

### 数据类型

| 类型格式 | 说明 | 示例 |
|----------|------|------|
| `int` | 整数 | 100 |
| `long` | 长整数 | 1000000 |
| `float` | 浮点数 | 3.14 |
| `double` | 双精度浮点 | 3.14159 |
| `bool` | 布尔值 | true/false |
| `string` | 字符串 | text |
| `int[]` | 整数数组 | 1,2,3 |
| `enum_Xxx` | 枚举类型 | enum_ItemType |
| `Xxx` | 自定义类型 | RewardInfo |

### 特殊标记

- **导出开关**：第一列控制是否导出（true/false）
- **END行**：数据结束后填写 `END`
- **END列**：字段结束后填写 `END`

### 继承语法

文件名格式：`子表:父表.xlsx`

例如：`Equipment:Item.xlsx` 表示 Equipment 继承自 Item

## 六、输出文件

运行后会在 `Output` 目录生成：

```
Output/
├── Json/                    # JSON配置文件
│   ├── Item.json
│   └── PlayerLevel.json
├── CSharp/                  # Unity C#实体类
│   ├── Enums/
│   │   └── Enums.cs
│   ├── Item.cs
│   ├── PlayerLevel.cs
│   └── ConfigManager.cs
└── TypeScript/              # Cocos TS接口
    ├── Enums/
    │   └── Enums.ts
    ├── Item.ts
    ├── PlayerLevel.ts
    └── ConfigManager.ts
```

## 七、Unity集成

### 1. 复制生成的C#文件到Unity项目

将 `Output/CSharp/` 下的文件复制到Unity的 `Scripts/Config/` 目录

### 2. 加载配置

```csharp
// 在启动时加载所有配置
ConfigManager.Instance.LoadAllConfigs();

// 获取配置项
var item = ConfigManager.Instance.ItemDict[1001];
```

### 3. JSON文件放置

将 `Output/Json/` 下的文件放入Unity的 `Resources/Config/` 目录

## 八、Cocos Creator集成

### 1. 复制生成的TS文件到Cocos项目

将 `Output/TypeScript/` 下的文件复制到Cocos项目的 `assets/scripts/config/` 目录

### 2. 加载配置

```typescript
// 在启动时加载所有配置
await ConfigManager.loadAll();

// 获取配置项
const item = ConfigManager.getItem(1001);
```

### 3. JSON文件放置

将 `Output/Json/` 下的文件放入Cocos项目的 `assets/resources/config/` 目录

## 九、日志输出

工具运行时会输出彩色日志：

- **白色**：普通信息
- **绿色**：成功信息
- **黄色**：警告信息
- **红色**：错误信息
- **青色**：表格处理信息

## 十、常见问题

### Q: 中文乱码怎么办？
A: 工具已自动设置UTF-8编码，确保控制台支持UTF-8显示。

### Q: 如何排除某些Excel文件？
A: 将不需要导出的文件重命名为以 `~$` 开头，或设置为隐藏文件。

### Q: 枚举值如何定义？
A: 枚举值从表格数据中自动收集，所有使用该枚举的表格中的值会被合并。

### Q: 如何添加新的数据类型？
A: 修改 `TypeParser.cs` 中的类型解析逻辑，并在 `FieldInfo.cs` 中添加对应的代码生成。

## 十一、NuGet包安装命令

```bash
# 安装NPOI（Excel解析）
dotnet add package NPOI --version 2.6.2

# 安装编码支持
dotnet add package System.Text.Encoding.CodePages --version 8.0.0
```

## 十二、许可证

MIT License
