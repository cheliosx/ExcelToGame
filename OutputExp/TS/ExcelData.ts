import { _example } from "./_example";
/***
* 该文件由工具自动生成，请勿手动修改
*/
/** 数据表容器需要实现的加载契约 */
interface ITableContainer {
    load(data: any): void;
}
export class ExcelData {
    protected _example: _example = new _example();
    /** 表名 -> 数据容器；集中登记分发关系，避免用字符串索引 this */
    protected readonly _tableMap: Record<string, ITableContainer> = {
        example: this._example,
    };
    /**
     * 按表名加载数据
     * @param fileName 表名（不含下划线前缀，如 "example"）
     * @param data 表格 JSON 数据
     */
    public init(fileName: string, data: any) {
        const table = this._tableMap[fileName];
        // 表名不存在时立即报错，避免 obj.load 抛出难以定位的 undefined 错误
        if (!table) {
            throw new Error(`ExcelData: 未找到数据表 "${fileName}"`);
        }
        table.load(data);
    }
    /** example[表格示例] */
    public get example(): _example {
        return this._example;
    }
}
