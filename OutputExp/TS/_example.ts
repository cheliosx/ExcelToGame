import { _lang } from "./_language";

/***
* 该文件由工具自动生成，请勿手动修改
*/
/**
 * 行数据容器：整表只保留一份文本和行偏移表，
 * 访问某行时才解析该行，避免一次性创建几十万个数组。
 */
export class _TableData {
    private readonly _text: string;
    private readonly _offsets: Int32Array;

    constructor(text: string) {
        this._text = text || "";
        this._offsets = _TableData.splitLines(this._text);
    }

    public get count(): number {
        return this._offsets.length - 1;
    }

    /** 兼容两种 JSON 形态：data（单块文本）或 rows（逐行字符串数组） */
    public static fromPayload(payload: any): _TableData {
        if (payload === null || payload === undefined) {
            return new _TableData("");
        }
        if (payload.data !== undefined && payload.data !== null) {
            return new _TableData(payload.data);
        }
        if (Array.isArray(payload.rows)) {
            return new _TableData(payload.rows.join("\n"));
        }
        return new _TableData("");
    }

    /** 解析第 index 行，只创建这一行的数组 */
    public parseRow(index: number): any[] {
        if (index < 0 || index >= this.count) {
            return [];
        }
        return JSON.parse(this._text.substring(this._offsets[index], this._offsets[index + 1]));
    }

    /** 只读取行首的数值（分组键），不解析整行 */
    public readKey(index: number): number {
        let i = this._offsets[index];
        const end = this._offsets[index + 1];
        if (this._text.charCodeAt(i) === 91) {
            i++;
        }
        let sign = 1;
        let value = 0;
        let hit = false;
        for (; i < end; i++) {
            const code = this._text.charCodeAt(i);
            if (code >= 48 && code <= 57) {
                value = value * 10 + (code - 48);
                hit = true;
            } else if (code === 45) {
                sign = -1;
            } else if (hit) {
                break;
            }
        }
        return sign * value;
    }

    private static splitLines(text: string): Int32Array {
        if (text.length === 0) {
            return Int32Array.from([0]);
        }
        const offsets: number[] = [0];
        let index = text.indexOf("\n");
        while (index >= 0) {
            offsets.push(index + 1);
            index = text.indexOf("\n", index + 1);
        }
        offsets.push(text.length);
        return Int32Array.from(offsets);
    }
}

/** 主键索引：ids 已升序时直接二分，否则额外维护行号排序表 */
export class _TableIndex {
    private readonly _ids: number[] = [];
    private readonly _order: Int32Array;

    constructor(ids: number[]) {
        this._ids = ids || [];
        this._order = _TableIndex.buildOrder(this._ids);
    }

    /** 返回行号，找不到返回 -1 */
    public indexOf(id: number): number {
        let low = 0;
        let high = this._ids.length - 1;
        while (low <= high) {
            const mid = (low + high) >> 1;
            const row = this._order.length === 0 ? mid : this._order[mid];
            const value = this._ids[row];
            if (value === id) {
                return row;
            }
            if (value < id) {
                low = mid + 1;
            } else {
                high = mid - 1;
            }
        }
        return -1;
    }

    private static buildOrder(ids: number[]): Int32Array {
        for (let i = 1; i < ids.length; i++) {
            if (ids[i] <= ids[i - 1]) {
                const order = new Int32Array(ids.length);
                for (let j = 0; j < order.length; j++) {
                    order[j] = j;
                }
                order.sort((a, b) => ids[a] - ids[b]);
                return order;
            }
        }
        return new Int32Array(0);
    }
}

export namespace example {
    /** 装备类型 */
    export enum equipType {
        /** 武器 */
        weapon = 1,
        /** 衣服 */
        clothes = 2,
        /** 头盔 */
        helmet = 3,
    }

    /** 成长类型 */
    export enum growthType {
        /** 角色成长 */
        player = 1,
        /** 小怪成长 */
        monster = 2,
        /** BOSS成长 */
        boss = 3,
    }

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
        get type(): equipType {
            return this._parse()[1];
        }

        /** 成长类型 */
        get gType(): growthType {
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
        get id(): growthType {
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
