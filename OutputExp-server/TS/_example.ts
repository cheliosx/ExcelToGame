import { _lang } from "./_language";
import { TableEnums } from "./_enums";
import { _parseKey } from "./_runtime";

/***
* 该文件由工具自动生成，请勿手动修改
*/

/** 角色等级 */
export class _roleLevel {
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
                throw new Error("_roleLevel: 原始数据已释放，无法再解析");
            }

            values = JSON.parse(data) as any[];
            this._object = values;
            this._data = null;
        }

        return values;
    }

    /** 生命 */
    get hp(): number {
        return this.objects[0];
    }

    /** 防御 */
    get def(): number {
        return this.objects[1];
    }
}

/** 道具 */
export class _item {
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
                throw new Error("_item: 原始数据已释放，无法再解析");
            }

            values = JSON.parse(data) as any[];
            this._object = values;
            this._data = null;
        }

        return values;
    }

    /** 道具id */
    get id(): number {
        return this.objects[0];
    }
}

/** 装备 */
export class _equip {
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
                throw new Error("_equip: 原始数据已释放，无法再解析");
            }

            values = JSON.parse(data) as any[];
            this._object = values;
            this._data = null;
        }

        return values;
    }

    /** 道具id */
    get id(): number {
        return this.objects[0];
    }

    /** 装备类型 */
    get type(): TableEnums.equipType {
        return this.objects[1];
    }

    /** 成长类型 */
    get gType(): TableEnums.growthType {
        return this.objects[2];
    }
}

/** 成长数据 */
export class _growthData {
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
                throw new Error("_growthData: 原始数据已释放，无法再解析");
            }

            values = JSON.parse(data) as any[];
            this._object = values;
            this._data = null;
        }

        return values;
    }

    /** 成长类型 */
    get id(): TableEnums.growthType {
        return this.objects[0];
    }

    /** 等级 */
    get lv(): number {
        return this.objects[1];
    }

    /** 生命 */
    get hp(): number {
        return this.objects[2];
    }

    /** 防御 */
    get def(): number {
        return this.objects[3];
    }
}

/** 游戏参数表 */
export class _gameArg {
    public _data: any;

    /** 游戏名字 */
    get gameName(): string {
        return _lang.t(this._data.gameName);
    }

    /** 初始血量 */
    get initHp(): number {
        return this._data.initHp;
    }

    /** 初始道具 */
    get initItems(): any[] {
        return this._data.initItems;
    }

    /** 游戏路径 */
    get gamePath(): string {
        return this._data.gamePath;
    }
}

/** 道具 */
export class _itemMap {
    /** 原始行数据，按需解析 */
    private _rows: string[] = [];

    /** 主键 -> 行号 */
    private _index: Map<number, number> = new Map();

    /** 已访问过的行（只缓存用到的实体） */
    private _data: Map<number, _item> = new Map();

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
    public findById(id: number): Readonly<_item> | undefined {
        const cached = this._data.get(id);
        if (cached !== undefined) {
            return cached;
        }

        const row = this._index.get(id);
        if (row === undefined) {
            return undefined;
        }

        const entity = new _item(this._rows[row]);
        this._data.set(id, entity);
        return entity;
    }

    /** 全表遍历：逐行创建实体，不驻留 */
    public forEach(callback: (row: _item, index: number) => void): void {
        for (let i = 0; i < this._rows.length; i++) {
            callback(new _item(this._rows[i]), i);
        }
    }
}

/** 装备 */
export class _equipMap {
    /** 原始行数据，按需解析 */
    private _rows: string[] = [];

    /** 主键 -> 行号 */
    private _index: Map<number, number> = new Map();

    /** 已访问过的行（只缓存用到的实体） */
    private _data: Map<number, _equip> = new Map();

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
    public findById(id: number): Readonly<_equip> | undefined {
        const cached = this._data.get(id);
        if (cached !== undefined) {
            return cached;
        }

        const row = this._index.get(id);
        if (row === undefined) {
            return undefined;
        }

        const entity = new _equip(this._rows[row]);
        this._data.set(id, entity);
        return entity;
    }

    /** 全表遍历：逐行创建实体，不驻留 */
    public forEach(callback: (row: _equip, index: number) => void): void {
        for (let i = 0; i < this._rows.length; i++) {
            callback(new _equip(this._rows[i]), i);
        }
    }
}

/** 角色等级 */
export class _roleLevelList {
    /** 原始行数据，按需解析 */
    private _rows: string[] = [];

    /** 已访问过的行（只缓存用到的实体） */
    private _data: Map<number, _roleLevel> = new Map();

    public init(_data: any[]): void {
        this._rows = _data || [];
        this._data = new Map();
    }

    public get count(): number {
        return this._rows.length;
    }

    public findByIndex(index: number): Readonly<_roleLevel> | undefined {
        if (index < 0 || index >= this._rows.length) {
            return undefined;
        }

        let entity = this._data.get(index);
        if (entity === undefined) {
            entity = new _roleLevel(this._rows[index]);
            this._data.set(index, entity);
        }

        return entity;
    }

    public forEach(callback: (row: _roleLevel, index: number) => void): void {
        for (let i = 0; i < this._rows.length; i++) {
            callback(new _roleLevel(this._rows[i]), i);
        }
    }
}

/** 成长数据 */
export class _growthDataGroup {
    /** 原始行数据，按需解析 */
    private _rows: string[] = [];

    /** 分组键 -> 行号列表 */
    private _index: Map<number, number[]> = new Map();

    /** 已访问过的行（只缓存用到的实体） */
    private _data: Map<number, _growthData> = new Map();

    public init(_data: any[]): void {
        this._rows = _data || [];
        this._index = new Map();
        this._data = new Map();

        for (let i = 0; i < this._rows.length; i++) {
            const id = _parseKey(this._rows[i]);
            let temp = this._index.get(id);
            if (temp === undefined) {
                temp = [];
                this._index.set(id, temp);
            }

            temp.push(i);
        }
    }

    public get count(): number {
        return this._rows.length;
    }

    public findGroupById(id: number): ReadonlyArray<_growthData> | undefined {
        const rows = this._index.get(id);
        if (rows === undefined) {
            return undefined;
        }

        const result: _growthData[] = [];
        for (let i = 0; i < rows.length; i++) {
            result.push(this._entity(rows[i]));
        }

        return result;
    }

    /** 同一行只创建一次实体 */
    private _entity(row: number): _growthData {
        let entity = this._data.get(row);
        if (entity === undefined) {
            entity = new _growthData(this._rows[row]);
            this._data.set(row, entity);
        }

        return entity;
    }
}

/** example[表格示例] */
export class _example {
    /** 游戏参数表 */
    public gameArg: _gameArg = new _gameArg();
    /** 角色等级 */
    public roleLevelList: _roleLevelList = new _roleLevelList();
    /** 道具 */
    public itemMap: _itemMap = new _itemMap();
    /** 装备 */
    public equipMap: _equipMap = new _equipMap();
    /** 成长数据 */
    public growthDataGroup: _growthDataGroup = new _growthDataGroup();

    public load(data: any): void {
        this.gameArg._data = data.gameArg;
        this.roleLevelList.init(data.roleLevel);
        this.itemMap.init(data.item);
        this.equipMap.init(data.equip);
        this.growthDataGroup.init(data.growthData);
    }
}
