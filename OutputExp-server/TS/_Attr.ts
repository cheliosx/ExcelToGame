import { _lang } from "./_language";
import { _parseKey } from "./_runtime";

/***
* 该文件由工具自动生成，请勿手动修改
*/

/** 属性表 */
export class _attrRef {
    /** 原始行文本；首次解析后置空以释放内存 */
    public _data: string | null;
    private _object: any[] | null = null;

    constructor(data: string) {
        this._data = data;
    }

    /** 首次访问字段时才解析本行 */
    private get objects(): any[] {
        let values = this._object;
        if (values === null) {
            const data = this._data;
            if (data === null) {
                throw new Error("_attrRef: 原始数据已释放，无法再解析");
            }

            values = JSON.parse(data) as any[];
            this._object = values;
            this._data = null;
        }

        return values;
    }

    /** 属性ID */
    get id(): number {
        return this.objects[0];
    }

    /** 属性类型 */
    get type(): number {
        return this.objects[1];
    }

    /** 属性名称 */
    get name(): string {
        return _lang.t(this.objects[2]);
    }

    /** 0:数值,1:百分比 */
    get unit(): number {
        return this.objects[3];
    }

    /** 属性系数 */
    get attributeCoefficient(): number {
        return this.objects[4];
    }

    /** 属性图标 */
    get icon(): string {
        return this.objects[5];
    }
}

/** 属性表 */
export class _attrRefMap {
    /** 原始行数据，按需解析 */
    private _rows: string[] = [];

    /** 主键 -> 行号 */
    private _index: Map<number, number> = new Map();

    /** 已访问过的行（只缓存用到的实体） */
    private _data: Map<number, _attrRef> = new Map();

    public init(_data: any): void {
        this._rows = _data || [];
        this._index = new Map();
        this._data = new Map();

        for (let i = 0; i < this._rows.length; i++) {
            this._index.set(_parseKey(this._rows[i]), i);
        }
    }

    public get count(): number {
        return this._rows.length;
    }

    /** 按主键取一行：定位到行号后才创建实体并解析 */
    public findById(id: number): Readonly<_attrRef> | undefined {
        const cached = this._data.get(id);
        if (cached !== undefined) {
            return cached;
        }

        const row = this._index.get(id);
        if (row === undefined) {
            return undefined;
        }

        const entity = new _attrRef(this._rows[row]);
        this._data.set(id, entity);
        return entity;
    }

    /** 全表遍历：逐行创建实体，不驻留 */
    public forEach(callback: (row: _attrRef, index: number) => void): void {
        for (let i = 0; i < this._rows.length; i++) {
            callback(new _attrRef(this._rows[i]), i);
        }
    }
}

/** S-属性[Attr] */
export class _Attr {
    /** 属性表 */
    public attrRefMap: _attrRefMap = new _attrRefMap();

    public load(data: any): void {
        this.attrRefMap.init(data.attrRef);
    }
}
