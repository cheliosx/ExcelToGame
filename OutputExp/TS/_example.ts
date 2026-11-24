import { _lang } from "./_language";
import { TableEnums } from "./_enums";
import { _TableData, _TableIndex } from "./_runtime";

/***
* 该文件由工具自动生成，请勿手动修改
*/
export namespace example {
    /** 角色等级 */
    export class roleLevel {
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

        /** 生命 */
        get hp(): number {
            return this._parse()[0];
        }

        /** 防御 */
        get def(): number {
            return this._parse()[1];
        }
    }

    /** 道具 */
    export class item {
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

        /** 道具id */
        get id(): number {
            return this._parse()[0];
        }

        /** 道具名字 */
        get name(): string {
            return _lang.t(this._parse()[1]);
        }

        /** 道具图标 */
        get icon(): string {
            return this._parse()[2];
        }
    }

    /** 装备 */
    export class equip {
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

        /** 道具id */
        get id(): number {
            return this._parse()[0];
        }

        /** 装备类型 */
        get type(): TableEnums.equipType {
            return this._parse()[1];
        }

        /** 成长类型 */
        get gType(): TableEnums.growthType {
            return this._parse()[2];
        }
    }

    /** 成长数据 */
    export class growthData {
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

        /** 成长类型 */
        get id(): TableEnums.growthType {
            return this._parse()[0];
        }

        /** 等级 */
        get lv(): number {
            return this._parse()[1];
        }

        /** 生命 */
        get hp(): number {
            return this._parse()[2];
        }

        /** 防御 */
        get def(): number {
            return this._parse()[3];
        }
    }

    /** 游戏参数表 */
    export class gameArg {
        private _fields: Record<string, any> = {};

        public load(data: any): void {
            this._fields = data.fields || {};
        }

        /** 游戏名字 */
        get gameName(): string {
            return _lang.t(this._fields["gameName"]);
        }

        /** 初始血量 */
        get initHp(): number {
            return this._fields["initHp"];
        }

        /** 初始道具 */
        get initItems(): any[] {
            return this._fields["initItems"];
        }

        /** 游戏路径 */
        get gamePath(): string {
            return this._fields["gamePath"];
        }
    }

}

/** 道具 */
export class itemTable {
    private _table: _TableData = new _TableData("");
    private _index: _TableIndex | null = null;
    private _cache: Map<number, example.item> = new Map();

    public load(data: any): void {
        this._table = _TableData.fromPayload(data);
        this._index = data.ids ? new _TableIndex(data.ids) : null;
        this._cache = new Map();
    }

    public get count(): number {
        return this._table.count;
    }

    /** 按主键取一行；只解析被访问的行，并缓存已取过的实体 */
    public get(id: number): Readonly<example.item> | undefined {
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
        const entity = new example.item(this._table, row);
        this._cache.set(id, entity);
        return entity;
    }

    /** 全表遍历：逐行创建实体，不驻留整表对象 */
    public forEach(callback: (row: example.item, index: number) => void): void {
        const count = this._table.count;
        for (let i = 0; i < count; i++) {
            callback(new example.item(this._table, i), i);
        }
    }
}

/** 装备 */
export class equipTable {
    private _table: _TableData = new _TableData("");
    private _index: _TableIndex | null = null;
    private _cache: Map<number, example.equip> = new Map();

    public load(data: any): void {
        this._table = _TableData.fromPayload(data);
        this._index = data.ids ? new _TableIndex(data.ids) : null;
        this._cache = new Map();
    }

    public get count(): number {
        return this._table.count;
    }

    /** 按主键取一行；只解析被访问的行，并缓存已取过的实体 */
    public get(id: number): Readonly<example.equip> | undefined {
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
        const entity = new example.equip(this._table, row);
        this._cache.set(id, entity);
        return entity;
    }

    /** 全表遍历：逐行创建实体，不驻留整表对象 */
    public forEach(callback: (row: example.equip, index: number) => void): void {
        const count = this._table.count;
        for (let i = 0; i < count; i++) {
            callback(new example.equip(this._table, i), i);
        }
    }
}

/** 角色等级 */
export class roleLevelList {
    private _table: _TableData = new _TableData("");

    public load(data: any): void {
        this._table = _TableData.fromPayload(data);
    }

    public get count(): number {
        return this._table.count;
    }

    public get(index: number): Readonly<example.roleLevel> | undefined {
        if (index < 0 || index >= this._table.count) {
            return undefined;
        }
        return new example.roleLevel(this._table, index);
    }

    public forEach(callback: (row: example.roleLevel, index: number) => void): void {
        const count = this._table.count;
        for (let i = 0; i < count; i++) {
            callback(new example.roleLevel(this._table, i), i);
        }
    }
}

/** 成长数据 */
export class growthDataGroup {
    private _table: _TableData = new _TableData("");
    private _groups: Map<number, number[]> | null = null;

    public load(data: any): void {
        this._table = _TableData.fromPayload(data);
        this._groups = null;
    }

    public get count(): number {
        return this._table.count;
    }

    public getGroup(id: number): ReadonlyArray<example.growthData> | undefined {
        const rows = this._ensureGroups().get(id);
        if (rows === undefined) {
            return undefined;
        }
        const result: example.growthData[] = [];
        for (let i = 0; i < rows.length; i++) {
            result.push(new example.growthData(this._table, rows[i]));
        }
        return result;
    }

    /** 分组索引懒构建：首次按组查询时才扫描行首列 */
    private _ensureGroups(): Map<number, number[]> {
        if (this._groups !== null) {
            return this._groups;
        }
        const groups = new Map<number, number[]>();
        const count = this._table.count;
        for (let i = 0; i < count; i++) {
            const key = this._table.readKey(i);
            let rows = groups.get(key);
            if (rows === undefined) {
                rows = [];
                groups.set(key, rows);
            }
            rows.push(i);
        }
        this._groups = groups;
        return groups;
    }
}

/** example[表格示例] */
export class _example {
    public gameArg: example.gameArg = new example.gameArg();
    public roleLevel: roleLevelList = new roleLevelList();
    public item: itemTable = new itemTable();
    public equip: equipTable = new equipTable();
    public growthData: growthDataGroup = new growthDataGroup();

    public load(data: any): void {
        if (data.gameArg) {
            this.gameArg.load(data.gameArg);
        }
        if (data.roleLevel) {
            this.roleLevel.load(data.roleLevel);
        }
        if (data.item) {
            this.item.load(data.item);
        }
        if (data.equip) {
            this.equip.load(data.equip);
        }
        if (data.growthData) {
            this.growthData.load(data.growthData);
        }
    }
}
