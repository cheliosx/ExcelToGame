import { _lang } from "./_language";
/***
* 该文件由工具自动生成，请勿手动修改
*/
export namespace example {
    /**
     * 装备类型
     */
    export enum equipType {
        /** 武器 */
        weapon = 1,
        /** 衣服 */
        clothes = 2,
        /** 头盔 */
        helmet = 3
    }
    /**
     * 成长类型
     */
    export enum growthType {
        /** 角色成长 */
        player = 1,
        /** 小怪成长 */
        monster = 2,
        /** BOSS成长 */
        boss = 3
    }
    /** 个性参数    */
    export class gameArg {
        public _data: any;
        /** 游戏名字    */
        get gameName(): string {
            return _lang.t(this._data.gameName);
        }
        /** 初始血量    */
        get initHp(): number {
            return this._data.initHp;
        }
        /** 初始道具    */
        get initItems(): number[] {
            return this._data.initItems;
        }
    }
    /** 角色等级    */
    export class roleLevel {
        public _data: string;
        protected _object: any[];
        private get objects() {
            if (!this._object) {
                this._object = JSON.parse(this._data);
                this._data = null;
            }
            return this._object;
        }
        /** 血量    */
        get hp(): number {
            return this.objects[0];
        }
        /** 防御    */
        get def(): number {
            return this.objects[1];
        }
    }
    /** 道具    */
    export class item {
        public _data: string;
        protected _object: any[];
        protected get objects() {
            if (!this._object) {
                this._object = JSON.parse(this._data);
                this._data = null;
            }
            return this._object;
        }
        /** 道具id    */
        get id(): number {
            return this.objects[0];
        }
        /** 道具名字    */
        get name(): string {
            return _lang.t(this.objects[1]);
        }
        /** 道具图标    */
        get icon(): string {
            return this.objects[2];
        }
    }
    /**
     * 装备
     */
    export class equip extends item {
        /** 装备类型    */
        get type(): equipType {
            return this.objects[3];
        }
        /** 成长类型    */
        get gType(): growthType {
            return this.objects[4];
        }
    }

    /**
     * 成长类型
     */
    export class growthData {
        public _data: string;
        protected _object: any[];
        private get objects() {
            if (!this._object) {
                this._object = JSON.parse(this._data);
                this._data = null;
            }
            return this._object;
        }
        /** 成长类型    */
        get id(): growthType {
            return this.objects[0];
        }
        /** 等级    */
        get lv(): number {
            return this.objects[1];
        }
        /** 血量    */
        get hp(): number {
            return this.objects[2];
        }
        /** 防御    */
        get def(): number {
            return this.objects[3];
        }
    }
}
/** 角色等级    */
export class roleLevelList {
    public _data: example.roleLevel[] = [];
    public init(_data: any[]) {
        for (let i = 0; i < _data.length; i++) {
            const list = _data[i];
            let data = new example.roleLevel();
            data._data = list;
            this._data.push(data);
        }
    }
    public findByIndex(index: number): Readonly<example.roleLevel> {
        return this._data[index];
    }
}
/** 道具    */
export class itemMap {
    public _data: Map<number, example.item> = new Map;
    public init(_data: any) {
        for (let i = 0; i < _data.length; i++) {
            const element = _data[i];
            let data = new example.item();
            data._data = element;
            let idEnd = element.indexOf(',');
            let id = parseInt(element.substring(1, idEnd));
            this._data.set(id, data);
        }
    }
    /** 获取道具 */
    public getItem(id: number): Readonly<example.item> {
        return this._data.get(id);
    }
    /** 获取装备 */
    public getEquip(id: number): Readonly<example.equip> {
        let temp = this._data.get(id);
        if (temp instanceof example.equip) {
            return temp;
        } else {
            let data = new example.equip();
            data._data = temp._data;
            this._data.set(id, data);
            return data;
        }
    }
}
/** 成长数据    */
export class growthDataGroup {
    public _dataMap: Map<number, example.growthData[]> = new Map();
    public init(_data: any[]) {
        for (let i = 0; i < _data.length; i++) {
            const element = _data[i];
            let data = new example.growthData();
            data._data = element;
            let idEnd = element.indexOf(',');
            let id = parseInt(element.substring(1, idEnd));
            let temp = this._dataMap.get(id);
            if (!temp) {
                temp = [];
                this._dataMap.set(id, temp);
            }
            temp.push(data);
        }
    }
    public findGroupById(id: number): Readonly<example.growthData[]> {
        return this._dataMap.get(id);
    }
}
/** example[表格示例].xlsx    */
export class _example {
    /** 游戏参数 */
    public gameArg: example.gameArg = new example.gameArg();
    /** 道具 */
    public item: itemMap = new itemMap();
    /** 成长数据 */
    public growthData: growthDataGroup = new growthDataGroup();
    public load(data: any) {
        this.gameArg._data = data.gameArg;
        this.item.init(data.item);
        this.growthData.init(data.growthData);
    }
}
