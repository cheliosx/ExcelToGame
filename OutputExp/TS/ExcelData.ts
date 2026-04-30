import { _example } from "./_example";
/***
* 该文件由工具自动生成，请勿手动修改
*/
export class ExcelData {
    protected _example: _example = new _example();
    public init(fileName: string, data: any) {
        let obj = this['_' + fileName];
        obj.load(data);
    }
    /** example[表格示例] */
    public get example(): _example {
        return this._example;
    }
}