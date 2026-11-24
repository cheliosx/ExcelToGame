# 填表规范

## 一、文件命名

```
类型名[中文名].xlsx
```

示例：`Item[道具].xlsx`、`Skill[技能].xlsx`。工具会去掉方括号与中文，得到英文输出名（`Item`）。

## 二、页签结构

### 1. main 页签（必须存在）

main 用一张表定义本文件内所有数据页签：

| 字段 | file | name | client | server | type |
|---|---|---|---|---|---|
| 名称 | 编号 | 名称 | 客户端是否导出 | 服务端是否导出 | 表格类型 |
| 1 | 道具 | item | YES | YES | 主建表 |
| 1 | 装备 | equip | YES | YES | 主建表 |
| 1 | 装备类型 | equipType | YES | NO | 枚举 |
| END | | | | | |

- `file`：Excel 中实际的页签名（中文）
- `name`：英文类型名，直接用于生成代码
- `client` / `server`：该表是否进客户端 / 服务端，`0`、`NO`、`FALSE` 表示关闭，留空视为开启
- 第一列为导出开关，`TRUE`/`1` 导出，`END` 结束
- 旧表格里的 `extend` / `继承` 列会被忽略（仅打印一条提示），不再产生继承关系

### 2. 数据页签（6 行表头 + 数据行）

| 行 | 内容 | 说明 |
|---|---|---|
| 第1行 | 符号 | 字段名行，第一列为 `符号`，最后一列为 `END` |
| 第2行 | 数据类型 | 字段类型声明 |
| 第3行 | 客户端 | `YES` / `NO`，`NO` 的列不进客户端产物 |
| 第4行 | 服务端 | 同上，`NO` 的列不进服务端产物 |
| 第5行 | 名称 | 中文名，生成代码注释 |
| 第6行 | 注释 | 补充说明，可留空 |
| 第7行+ | 数据 | 第一列 `TRUE`/`1` 表示导出，`END` 结束 |

示例（道具页签）：

| 符号 | id | name | icon | END |
|---|---|---|---|---|
| 数据类型 | number | language | string | |
| 客户端 | YES | YES | YES | |
| 服务端 | YES | NO | NO | |
| 名称 | 道具id | 道具名字 | 道具图标 | |
| 注释 | | | | |
| TRUE | 1 | 道具1 | 1 | |
| END | | | | |

## 三、表格类型

| type | 说明 | 生成的容器 |
|---|---|---|
| 主建表 / 主键表 | 以 `id` 为键，一行一个实体 | `_xxx` + `_xxxMap`（`findById(id)`） |
| 数组表 | 按下标顺序访问 | `_xxx` + `_xxxList`（`findByIndex(i)`） |
| 分组表 | 按首列分组，组内多行 | `_xxx` + `_xxxGroup`（`findGroupById(key)`） |
| 参数表 | key/value 转置，只有一行 | `_xxx` 配置对象（`_data`） |
| 枚举 | key/value 枚举定义 | `enum xxx`（统一在 `_enums.ts` / `Enums.cs`） |

## 四、表之间怎么关联

**不提供继承**。每个页签独立成表、独立成类，需要关联时用 id 互相引用：

- 道具页签：`id / name / icon`
- 装备页签：`id / type / gType`（`id` 与道具同值，表示"这个道具是装备"）

运行时两边各查一次即可：

```ts
const item = cfg.item.get(3);      // 道具数据
const equip = cfg.equip.get(3);    // 装备数据，id 不存在时为 undefined
if (equip) { /* 说明 3 号道具是装备 */ }
```

好处是工具侧没有合并、没有判别列、没有类型层级：加一张新表不影响任何已有表，出问题也只需要看一张表。

如果需要"装备一定是道具"这类约束，请在数据层保证（或写个导出后校验脚本），而不是让配置表承担类型系统。

## 五、数据类型

| 声明 | 客户端 TS | C# | 说明 |
|---|---|---|---|
| `number` / `int` | `number` | `int` | |
| `long` | `number` | `long` | |
| `float` / `double` | `number` | `float` / `double` | |
| `bool` | `boolean` | `bool` | `1/0`、`true/false` |
| `string` | `string` | `string` | |
| `language` | `string` | `string` | 导出为多语言 key，运行时经 `_lang.t()` / `LanguageManager.GetText()` 取文本 |
| `array<T>` / `T[]` | `T[]` | `List<T>` | Excel 中写 `[1,2]` 或 `1,2` |
| `[]` / `array` | `any[]` | `List<object>` | 元素类型不限 |
| `example.xxx` | `example.xxx` | `Xxx` | 引用本文件内的枚举或表，表引用导出为 id |

## 六、产物与加载方式

工具读取**输入目录下的所有 `*.xlsx`**（一次运行处理全部文件），产物分两类：

全局产物（只生成一份）：

- `i10n/zh-Hans.json` + `TS/_language.ts`：所有表的多语言文本（默认语言固定为 `zh-Hans`）
- `TS/_enums.ts` + `CSharp/Enums.cs`：所有枚举，任何文件的实体都能直接引用
- `TS/_runtime.ts` + `CSharp/Runtime.cs`：`_parseKey` / `ConfigTable` / `ConfigValue` 等共用运行时
- `TS/ExcelData.ts` + `CSharp/ExcelData.cs`：统一入口，登记目录下所有表格文件

每个 Excel 文件：

- `Json/{OutputName}.json`
- `TS/_{OutputName}.ts`：`_表名` 实体 + `_表名Map/List/Group` 容器 + `_{OutputName}` 根类
- `CSharp/{OutputName}.cs`：`Game.Config` 下的同名结构（`Item` / `ItemMap` / `ExampleConfig`）

生成的 TS 平铺导出、不套 namespace：

```ts
export class _item {                      // 一行数据，getter 按列取
    public _data: string | null;          // 原始行文本，首次解析后置空
    private get objects(): any[];         // 懒解析
}

export class _itemMap {                   // 主键表
    public init(_data: any[]): void;      // 只建 主键 -> 行号 索引
    public findById(id): _item | undefined;
    public forEach(cb): void;
    public get count(): number;
}

export class _roleLevelList { public findByIndex(index): _roleLevel | undefined; }   // 数组表
export class _growthDataGroup { public findGroupById(id): _growthData[] | undefined; } // 分组表

export class _example {
    public itemMap: _itemMap = new _itemMap();
    public load(data: any) { this.itemMap.init(data.item); }
}
```

> 输出名取文件名方括号内的英文名：`J-技能表[skill].xlsx` → `skill`；没有英文名时去掉中文，`example[表格示例].xlsx` → `example`。

### 按端导出（客户端 / 服务端）

```
ExcelToGame <输入目录> <输出目录>                    # 默认 client
ExcelToGame <输入目录> <输出目录> --target server    # 也可以 --server
ExcelToGame <输入目录> <输出目录> --target=server
```

> 全部参数与组合示例见[第九节](#九命令行参数)。

筛选规则：

| 层级 | 依据 | 说明 |
|---|---|---|
| 表 | main 页签的 `client` / `server` 列 | `0` / `NO` / `FALSE` 关闭，其余（含空）开启 |
| 列 | 数据页签第 3、4 行的「客户端 / 服务端」 | `NO` 的列不进入该端产物 |

- 未标记到该端的表不会出现在 JSON / TS / C# 中，`ExcelData` 入口也不登记它
- 整个 xlsx 都没有属于该端的表时，这个文件整体跳过
- 语言 key 与 `i10n/zh-Hans.json` 始终按**全量**收集：两端 id 保持一致，也只导出某一端时不会让另一端的译文丢 key

两端通常输出到不同目录：

```
ExcelToGame 表格数据 ..\client\config
ExcelToGame 表格数据 ..\server\config --target server
```

### 只导某一种语言

```
ExcelToGame <输入目录> <输出目录> --ts           # 只导 TypeScript
ExcelToGame <输入目录> <输出目录> --cs           # 只导 C#
ExcelToGame <输入目录> <输出目录> --lang ts,cs   # 两种都导（默认）
```

> 全部参数与组合示例见[第九节](#九命令行参数)。

| 参数 | 产物目录 |
|---|---|
| `--ts` / `--lang ts` | `Json/` + `i10n/` + `TS/` |
| `--cs` / `--lang cs` | `Json/` + `i10n/` + `CSharp/` |
| 不指定 | 四个目录全出 |

- `Json/` 与 `i10n/` 与语言无关，始终生成
- 可与 `--target` 组合：`--target server --cs`
- 语言只认 `ts` / `typescript`、`cs` / `csharp` / `c#`，写错直接报错并返回退出码 1

### 输出方式是"整体替换"

1. 所有产物先写进系统临时目录（`%TEMP%\ExcelToGame\<输出名>-<guid>`）
2. 全部生成成功后，删除输出目录，把临时目录的内容整体复制过去
3. 最后删除临时目录

因此：

- 生成中途失败（读取失败、校验不通过、模板报错）时，**原输出目录保持不变**，不会出现新旧混杂
- 输出目录里**不要放自己的文件**（会被整体清空），需要保留的代码请放在输出目录之外
- 工具拒绝把输出目录设为：输入目录本身或其父目录、磁盘根目录、含 `.git` 的仓库根目录

### 语言文件的 id 是稳定的

生成语言表之前，工具会先读一次**输出目录里已有的 `i10n/zh-Hans.json`**（旧版本放在根目录的 `language.json` 也兼容），按文本内容复用旧的 id：

| 操作 | 结果 |
|---|---|
| 表格内容不变，重新导出 | 所有 id 不变（再次导出文件完全一致） |
| 新增一行文本 | 新增条目拿新 id（旧 id 最大值 + 1），已有条目 id 不变 |
| 删除一行文本 | 该条目从 `zh-Hans.json` 中消失，其它 id 不变 |
| 在不同表之间移动同一段文本 | id 跟着文本走，不变 |
| 修改某段文本 | 视为新文本，分配新 id，旧 id 释放 |
| 同一段文本出现多次 | 仍按出现次数各占一个 id，按旧文件顺序依次复用 |

因此不要手动调整 `i10n/zh-Hans.json` 的 key；改文本会换 id，这是有意为之（避免旧译文错配到新文本上）。

### 大数据表的存储方式

每张表就是一个**字符串数组**，一项一行，内容是该行的紧凑 JSON：

```json
{
  "gameArg": {
    "gameName": "1",
    "initHp": 100
  },
  "roleLevel": [
    "[400,0]",
    "[600,0]"
  ],
  "item": [
    "[1,\"2\",\"1\"]",
    "[2,\"3\",\"2\"]"
  ]
}
```

JSON 里只有数据本身：

- 列名与列下标不写（已生成成实体 getter 里的常量）
- 表类型、行数不写（由生成的容器与 `count` 推导）
- 枚举表不产出数据（直接生成 `enum` 代码）

```
ExcelToGame <输入目录> <输出目录>
```

加载过程分三层，每一层都只做必要的事：

| 阶段 | 做的事 | 代价 |
|---|---|---|
| `load(json)` | 把表读成 `string[]` | 一次 `JSON.parse` |
| `init(_data)` | 主键表建 `主键 -> 行号`；分组表建 `键 -> 行号列表`；数组表什么都不做 | `indexOf(',')` 扫一遍首列，不解析行 |
| `findById(id)` | 定位行号后 `new _item(row)` | 创建 1 个轻量实体 |
| 读字段 | `JSON.parse(row)` + 缓存 | 每行只解析一次 |

所以 10 万行的表加载后只驻留：行字符串数组 + 一张索引，**没有被访问过的行永远不会被解析**。

```ts
// 初始化 0 次解析，findById 0 次，读字段才 1 次
const item = configs.example.itemMap.findById(3);   // 定位行 + 创建实体
item.id;                                            // 这里才 JSON.parse
```

TS 用法：

```ts
const configs = new ExcelData();
configs.init("example", exampleJson);   // 只解析外层，行数据仍是文本
configs.init("attr", attrJson);

// 多语言：i10n/zh-Hans.json；切语言就再调一次
configs.initLanguage("zh-Hans", zhHansJson);

const item = configs.example.itemMap.findById(3);            // 定位到第 3 行
const id = item.id;                                          // 读字段时才解析本行
const equip = configs.example.equipMap.findById(3);
const hp = configs.example.growthDataGroup.findGroupById(1)[0].hp;
const lv = configs.example.roleLevelList.findByIndex(0).hp;

configs.example.itemMap.count;                    // 行数，无需遍历
configs.example.itemMap.forEach((row, i) => {});  // 全表遍历：逐行创建，不驻留
```

C# 用法：

```csharp
var data = new ExcelData();
data.InitLanguage("zh-Hans", File.ReadAllText("i10n/zh-Hans.json"));
data.Load("example", exampleJson);
data.Load("attr", attrJson);

Item item = data.Example.ItemMap.FindById(3);
Equip equip = data.Example.EquipMap.FindById(3);
var hp = data.Example.GrowthDataGroup.FindGroupById(1)[0].Hp;
var lv = data.Example.RoleLevelList.FindByIndex(0).Hp;
var initHp = data.Example.GameArg.InitHp;

data.Lang;   // "zh-Hans"
```

`ExcelData` 与 TS 版一一对应：

| 能力 | TS | C# |
|---|---|---|
| 加载一个表格 | `init(fileName, data)` | `Load(fileName, json)` |
| 加载多语言 | `initLanguage(lang, texts)` | `InitLanguage(lang, json)` |
| 当前语言 | `configs.lang` | `data.Lang` |

文件名与语言码都不区分大小写；未知文件名直接抛错。

### 跨表引用枚举

任意表格的字段类型写 `example.equipType`（命名空间前缀可省），生成时统一到 `_enums.ts` / `Enums.cs`：

```ts
// TS：表格导出的枚举都在 TableEnums 命名空间下，与手写枚举区分
import { TableEnums } from "./_enums";

const type: TableEnums.equipType = item.type;
```

```csharp
// C#：同 Game.Config 命名空间，直接用
EquipType type = item.Type;
```

### 注意

- 没有 `all` 属性：一次性物化几十万行正是要避免的事，需要全表就 `forEach` / 遍历 `Count`
- `findById` 的结果按 id 缓存（玩家只会访问少量行，缓存很小）；`forEach` 不缓存
- 参数表（行数极少）仍是普通对象：根类里直接 `this.gameArg._data = data.gameArg`，不走文本格式

## 七、导出前校验

以下任一不满足都会中止导出：

1. 字段类型必须是基础类型、`array<T>`、`[]`，或已定义的枚举/表
2. 枚举表的枚举名、枚举值不能重复，且至少有一项
3. 主键表/分组表必须有主键字段，且主键必须导出到**当前目标端**（`--target`）
4. 主键/分组键必须是数字类型（`number`/`int` 或枚举），因为按 id 定位走的是数值索引
5. 不同文件之间页签名不能重复（C# 共用一个命名空间）
6. `main` 页签必须存在

## 八、注意事项

1. 数据页签必须能被 main 的 `file` 列找到（中文页签名）
2. 主键约定：字段名为 `id` 的字段（大小写不敏感），没有则取第一个字段
3. 第一列的导出开关为 `TRUE`/`1` 才导出该行，`END` 结束
4. 字段名使用英文小驼峰；生成的 C# 会转成 PascalCase
5. 导出时 Excel 文件不能被独占打开（否则会报"文件被占用"并中止）

## 九、命令行参数

```
ExcelToGame <输入目录> <输出目录> [选项]
```

开发期用 `dotnet run` 时，`--` 之后才是程序自己的参数：

```
dotnet run --project ExcelToGame.csproj -- "表格数据" "OutputExp" --target server --cs
```

### 1. 位置参数

| 参数 | 说明 |
|---|---|
| `<输入目录>` | 存放 `*.xlsx` 的目录，一次读取目录下全部表格（不递归子目录）；缺省为 `bin/Debug/net8.0/Excel` |
| `<输出目录>` | 产物根目录，每次导出会**整体替换**；缺省为 `bin/Debug/net8.0/Output` |

### 2. 选项

| 选项 | 取值 | 默认 | 说明 |
|---|---|---|---|
| `--target <端>` | `client` / `server` | `client` | 按 main 页签的 `client` / `server` 列筛选表与列，详见第六节 |
| `--client` | 无 | — | `--target client` 的简写 |
| `--server` | 无 | — | `--target server` 的简写 |
| `--ts` | 无 | — | 生成 `TS/` 目录 |
| `--cs` | 无 | — | 生成 `CSharp/` 目录 |
| `--lang <列表>` | `ts` / `cs` / `ts,cs` | — | 语言选择的组合写法，等价于 `--ts` / `--cs` |

规则：

- `--ts` / `--cs` / `--lang` 都不写时，默认 `ts,cs`（两种语言都导）
- 只写其中一个就只生成那一个
- `Json/` 与 `i10n/` 与语言无关，始终生成
- 选项顺序任意，`--target server` 与 `--target=server` 都支持
- 未知选项只打印一条 warning，不中断导出；但语言名写错会直接失败返回 1

### 3. 退出码

| 退出码 | 含义 |
|---|---|
| `0` | 导出成功 |
| `1` | 读取失败 / 校验不通过 / 生成失败 / 替换输出目录失败 |

替换失败时原输出目录保持不变，已生成的文件留在临时目录（日志里会打印路径），可以直接用于排查。

### 4. 使用示例

| 场景 | 命令 |
|---|---|
| 日常全量（客户端 + TS + C#） | `ExcelToGame 表格数据 OutputExp` |
| 只导客户端 C# | `ExcelToGame 表格数据 OutputExp --cs` |
| 只导客户端 TS | `ExcelToGame 表格数据 OutputExp --ts` |
| 服务端（数据 + C#） | `ExcelToGame 表格数据 OutputExp-Server --server --cs` |
| 服务端（数据 + 两种语言） | `ExcelToGame 表格数据 OutputExp-Server --target server` |
| 显式指定两种语言 | `ExcelToGame 表格数据 OutputExp --lang ts,cs` |
| 指定输入/输出目录 | `ExcelToGame D:\excel D:\game\config --cs --server` |
| 开发期调试 | `dotnet run --project ExcelToGame.csproj -- "表格数据" "OutputExp" --cs` |

两端分别输出的典型脚本：

```bat
:: client
ExcelToGame 表格数据 ..\client\config --ts --cs
if errorlevel 1 exit /b 1

:: server
ExcelToGame 表格数据 ..\server\config --target server --cs
if errorlevel 1 exit /b 1
```
