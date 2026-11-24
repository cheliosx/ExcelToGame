import { _example } from "./_example";
import { _Attr } from "./_Attr";

/***
* 该文件由工具自动生成，请勿手动修改
*/
/** 单个表格文件的数据入口需要实现的契约 */
interface ITableContainer {
    load(data: any): void;
}

export class ExcelData {
    protected readonly _example: _example = new _example();
    protected readonly _Attr: _Attr = new _Attr();

    /** 文件名 -> 数据入口 */
    protected readonly _files: Record<string, ITableContainer> = {
        example: this._example,
        Attr: this._Attr,
    };

    /**
     * 加载一个表格文件的 JSON
     * @param fileName 文件名（输出名，如 "example"）
     * @param data 该文件对应的 JSON
     */
    public init(fileName: string, data: any): void {
        const file = this._files[fileName];
        if (file === undefined) {
            throw new Error(`ExcelData: 未找到配置文件 "${fileName}"`);
        }
        file.load(data);
    }

    public get example(): _example {
        return this._example;
    }

    public get Attr(): _Attr {
        return this._Attr;
    }
}

