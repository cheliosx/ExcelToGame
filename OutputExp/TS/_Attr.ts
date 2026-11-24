import { _lang } from "./_language";
import { _TableData, _TableIndex } from "./_runtime";

/***
* 该文件由工具自动生成，请勿手动修改
*/
export namespace Attr {
    /** 属性表 */
    export class attrRef {
        private readonly _table: _TableData;
        private readonly _row: number;
        private _values: any[] | null = null;

        constructor(table: _TableData, row: number) {
            this._table = table;
            this._row = row;
        }

        /** 首次访问字段时才解析本行 */
        private _parse(): any[] {
            if (this._values === null) {
                this._values = this._table.parseRow(this._row);
            }
            return this._values;
        }

        /** 属性ID */
        get id(): number {
            return this._parse()[0];
        }

        /** 属性类型 */
        get type(): number {
            return this._parse()[1];
        }

        /** 属性名称 */
        get name(): string {
            return _lang.t(this._parse()[2]);
        }

        /** 描述 */
        get effect(): string {
            return _lang.t(this._parse()[3]);
        }

        /** 0:数值,1:百分比 */
        get unit(): number {
            return this._parse()[4];
        }

        /** 属性是否展示 */
        get show(): number {
            return this._parse()[5];
        }

        /** 属性系数 */
        get attributeCoefficient(): number {
            return this._parse()[6];
        }

        /** 属性图标 */
        get icon(): string {
            return this._parse()[7];
        }
    }

}

/** 属性表 */
export class attrRefTable {
    private _table: _TableData = new _TableData("");
    private _index: _TableIndex | null = null;
    private _cache: Map<number, Attr.attrRef> = new Map();

    public load(data: any): void {
        this._table = _TableData.fromPayload(data);
        this._index = data.ids ? new _TableIndex(data.ids) : null;
        this._cache = new Map();
    }

    public get count(): number {
        return this._table.count;
    }

    /** 按主键取一行；只解析被访问的行，并缓存已取过的实体 */
    public get(id: number): Readonly<Attr.attrRef> | undefined {
        const cached = this._cache.get(id);
        if (cached !== undefined) {
            return cached;
        }
        if (this._index === null) {
            return undefined;
        }
        const row = this._index.indexOf(id);
        if (row < 0) {
            return undefined;
        }
        const entity = new Attr.attrRef(this._table, row);
        this._cache.set(id, entity);
        return entity;
    }

    /** 全表遍历：逐行创建实体，不驻留整表对象 */
    public forEach(callback: (row: Attr.attrRef, index: number) => void): void {
        const count = this._table.count;
        for (let i = 0; i < count; i++) {
            callback(new Attr.attrRef(this._table, i), i);
        }
    }
}

/** S-属性[Attr] */
export class _Attr {
    public attrRef: attrRefTable = new attrRefTable();

    public load(data: any): void {
        if (data.attrRef) {
            this.attrRef.load(data.attrRef);
        }
    }
}
