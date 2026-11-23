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
- 第一列为导出开关，`TRUE`/`1` 导出，`END` 结束
- 旧表格里的 `extend` / `继承` 列会被忽略（仅打印一条提示），不再产生继承关系

### 2. 数据页签（6 行表头 + 数据行）

| 行 | 内容 | 说明 |
|---|---|---|
| 第1行 | 符号 | 字段名行，第一列为 `符号`，最后一列为 `END` |
| 第2行 | 数据类型 | 字段类型声明 |
| 第3行 | 客户端 | `YES` / `NO`，`NO` 的字段不导出到客户端 |
| 第4行 | 服务端 | 同上（服务端产物暂未生成，仅保留配置） |
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
| 主建表 / 主键表 | 以 `id` 为键，一行一个实体 | `xxxTable`（`get(id)`） |
| 数组表 | 按下标顺序访问 | `xxxList`（`get(index)`） |
| 分组表 | 按首列分组，组内多行 | `xxxGroup`（`getGroup(key)`） |
| 参数表 | key/value 转置，只有一行 | `xxx` 配置对象 |
| 枚举 | key/value 枚举定义 | `enum xxx` |

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

每个 Excel 文件生成：

- `Json/{OutputName}.json`
- `TS/_{OutputName}.ts`：运行时 + `namespace` 实体 + 表容器 + `_<OutputName>` 入口
- `CSharp/{OutputName}.cs`：`Game.Config` 下的同名结构
- 全局 `language.json` 与 `TS/_language.ts`

### 大数据表的存储方式

行数据不会展开成对象数组，每行只是一条字符串。默认**一行一条**，便于 diff：

```json
{
  "gameArg": {
    "fields": {
      "gameName": "1",
      "initHp": 100
    }
  },
  "item": {
    "ids": [1,2,3,4],
    "rows": [
      "[1,\"2\",\"1\"]",
      "[2,\"3\",\"2\"]",
      "[3,\"4\",\"1\"]",
      "[4,\"5\",\"2\"]"
    ]
  }
}
```

JSON 里只有数据本身：

- 列名与列下标不写（已生成成实体 getter 里的常量）
- 表类型、行数不写（由生成的容器与 `count` 推导）
- 枚举表不产出数据（直接生成 `enum` 代码）

打包发布时用 `--compact`，行数据会压成一个字符串 `data: "[1,\"2\",\"1\"]\n[2,\"3\",\"2\"]"`（此时表对象只有 `ids` 和 `data`），文件更小、解析峰值更低：

```
ExcelToGame <输入目录> <输出目录>            # 默认：行数据逐行（rows）
ExcelToGame <输入目录> <输出目录> --compact  # 紧凑：行数据单块（data）
```

两种格式生成代码都能读，加载时统一 `join("\n")` 成一份文本，再扫一遍换行生成行偏移表（`Int32Array`，4 字节/行）。**不会创建任何行对象**。

访问时：

- `get(id)`：用 `ids` 二分定位行号（ids 已升序则零额外索引，否则建一张 4 字节/行的排序表），只解析这一行
- `getGroup(id)`：首次调用时扫描行首列建分组索引，之后直接取行号
- `get(index)`（数组表）：按偏移直接解析

TS 用法：

```ts
const cfg = new _example();
cfg.load(exampleJson);            // 只解析外层，行数据仍是文本
_lang.load(languageJson);

const item = cfg.item.get(3);      // item | undefined，此时才解析第 3 行
const equip = cfg.equip.get(3);    // equip | undefined
const hp = cfg.growthData.getGroup(1)[0].hp;
const initHp = cfg.gameArg.initHp;

cfg.item.count;                    // 行数，无需遍历
cfg.item.forEach((row, i) => {});  // 全表遍历：逐行创建，不驻留
```

C# 用法：

```csharp
LanguageManager.Load(languageJson);
var cfg = new ExampleConfig();
cfg.Load(exampleJson);

Item item = cfg.Item.Get(3);
Equip equip = cfg.Equip.Get(3);
var hp = cfg.GrowthData.GetGroup(1)[0].Hp;

cfg.Item.Count;
cfg.Item.ForEach((row, i) => { });
```

### 注意

- 没有 `all` 属性：一次性物化几十万行正是要避免的事，需要全表就 `forEach`
- `get` 的结果按 id 缓存（玩家只会访问少量行，缓存很小）；`forEach` 不缓存
- 参数表（行数极少）仍是普通对象，不走文本格式

## 七、导出前校验

以下任一不满足都会中止导出：

1. 字段类型必须是基础类型、`array<T>`、`[]`，或已定义的枚举/表
2. 枚举表的枚举名、枚举值不能重复，且至少有一项
3. 主键表/分组表必须有主键字段，且主键必须导出到客户端
4. 主键/分组键必须是数字类型（`number`/`int` 或枚举），因为按 id 定位走的是数值索引
5. 不同文件之间页签名不能重复（C# 共用一个命名空间）
6. `main` 页签必须存在

## 八、注意事项

1. 数据页签必须能被 main 的 `file` 列找到（中文页签名）
2. 主键约定：字段名为 `id` 的字段（大小写不敏感），没有则取第一个字段
3. 第一列的导出开关为 `TRUE`/`1` 才导出该行，`END` 结束
4. 字段名使用英文小驼峰；生成的 C# 会转成 PascalCase
5. 导出时 Excel 文件不能被独占打开（否则会报"文件被占用"并中止）
