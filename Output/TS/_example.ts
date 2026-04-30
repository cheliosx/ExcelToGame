import { _lang } from "./_language";

/** gameArg 閰嶇疆 */
export class _gameArgConfig {
    public _data: any;

    get initHp(): number {
        return this._data['initHp'];
    }

    get initItems(): any[] {
        return JSON.parse(this._data['initItems'] || '[]');
    }

    get gamePath(): string {
        return this._data['gamePath'];
    }

}

/** gameArg */
export class _gameArg {
    public static Config: _gameArgConfig = new _gameArgConfig();

    public static load(jsonData: any): void {
        if (jsonData.setConfig) {
            this.Config._data = jsonData.setConfig;
        }
    }
}

/** roleLevel 閰嶇疆 */
export class _roleLevelConfig {
    public _data: any;

    get lv(): number {
        return this._data['lv'];
    }

    get hp(): number {
        return this._data['hp'];
    }

    get def(): number {
        return this._data['def'];
    }

}

/** roleLevel ??? */
export class _roleLevelAspect {
    public objects: any;

    get lv(): number {
        return this.objects['1'];
    }

    get hp(): number {
        return this.objects['2'];
    }

    get def(): number {
        return this.objects['3'];
    }

}

/** roleLevel 鏁版嵁鏄犲皠 */
export class _roleLevelAspectMap {
    private _map: Map<number, _roleLevelAspect> = new Map();

    set(id: number, aspect: _roleLevelAspect) {
        this._map.set(id, aspect);
    }

    get(id: number): _roleLevelAspect | undefined {
        return this._map.get(id);
    }
}

/** roleLevel */
export class _roleLevel {
    public static Config: _roleLevelConfig = new _roleLevelConfig();
    public static Aspect: _roleLevelAspect[] = [];
    public static AspectMap: _roleLevelAspectMap = new _roleLevelAspectMap();

    public static findById(id: number): _roleLevelAspect | undefined {
        return this.AspectMap.get(id);
    }

    public static load(jsonData: any): void {
        if (jsonData.setConfig) {
            this.Config._data = jsonData.setConfig;
        }
        if (jsonData.setAspect) {
            this.Aspect = [];
            this.AspectMap = new _roleLevelAspectMap();
            for (let i = 0; i < jsonData.setAspect.length; i++) {
                let aspect = new _roleLevelAspect();
                aspect.objects = JSON.parse(jsonData.setAspect[i]);
                this.Aspect.push(aspect);
                let id = aspect.objects['1'];
                this.AspectMap.set(id, aspect);
            }
        }
    }
}

/** item 閰嶇疆 */
export class _itemConfig {
    public _data: any;

    get id(): number {
        return this._data['id'];
    }

    get icon(): string {
        return this._data['icon'];
    }

    get type(): any {
        return this._data['type'];
    }

    get gType(): any {
        return this._data['gType'];
    }

}

/** item ??? */
export class _itemAspect {
    public objects: any;

    get id(): number {
        return this.objects['1'];
    }

    get name(): string {
        return _lang.t(this.objects['2']);
    }

    get icon(): string {
        return this.objects['3'];
    }

    get type(): any {
        return JSON.parse(this.objects['2'] || '{}');
    }

    get gType(): any {
        return JSON.parse(this.objects['3'] || '{}');
    }

}

/** item 鏁版嵁鏄犲皠 */
export class _itemAspectMap {
    private _map: Map<number, _itemAspect> = new Map();

    set(id: number, aspect: _itemAspect) {
        this._map.set(id, aspect);
    }

    get(id: number): _itemAspect | undefined {
        return this._map.get(id);
    }
}

/** item */
export class _item {
    public static Config: _itemConfig = new _itemConfig();
    public static Aspect: _itemAspect[] = [];
    public static AspectMap: _itemAspectMap = new _itemAspectMap();

    public static findById(id: number): _itemAspect | undefined {
        return this.AspectMap.get(id);
    }

    public static load(jsonData: any): void {
        if (jsonData.setConfig) {
            this.Config._data = jsonData.setConfig;
        }
        if (jsonData.setAspect) {
            this.Aspect = [];
            this.AspectMap = new _itemAspectMap();
            for (let i = 0; i < jsonData.setAspect.length; i++) {
                let aspect = new _itemAspect();
                aspect.objects = JSON.parse(jsonData.setAspect[i]);
                this.Aspect.push(aspect);
                let id = aspect.objects['1'];
                this.AspectMap.set(id, aspect);
            }
        }
    }
}

/** equipType 閰嶇疆 */
export class _equipTypeConfig {
    public _data: any;

    get key(): any {
        return this._data['key'];
    }

    get value(): any {
        return this._data['value'];
    }

}

/** equipType ??? */
export class _equipTypeAspect {
    public objects: any;

    get key(): any {
        return this.objects['2'];
    }

    get value(): any {
        return this.objects['3'];
    }

}

/** equipType 鏁版嵁鏄犲皠 */
export class _equipTypeAspectMap {
    private _map: Map<number, _equipTypeAspect> = new Map();

    set(id: number, aspect: _equipTypeAspect) {
        this._map.set(id, aspect);
    }

    get(id: number): _equipTypeAspect | undefined {
        return this._map.get(id);
    }
}

/** equipType */
export class _equipType {
    public static Config: _equipTypeConfig = new _equipTypeConfig();
    public static Aspect: _equipTypeAspect[] = [];
    public static AspectMap: _equipTypeAspectMap = new _equipTypeAspectMap();

    public static findById(id: number): _equipTypeAspect | undefined {
        return this.AspectMap.get(id);
    }

    public static load(jsonData: any): void {
        if (jsonData.setConfig) {
            this.Config._data = jsonData.setConfig;
        }
        if (jsonData.setAspect) {
            this.Aspect = [];
            this.AspectMap = new _equipTypeAspectMap();
            for (let i = 0; i < jsonData.setAspect.length; i++) {
                let aspect = new _equipTypeAspect();
                aspect.objects = JSON.parse(jsonData.setAspect[i]);
                this.Aspect.push(aspect);
                let id = aspect.objects['1'];
                this.AspectMap.set(id, aspect);
            }
        }
    }
}

/** growthData 閰嶇疆 */
export class _growthDataConfig {
    public _data: any;

    get id(): any {
        return this._data['id'];
    }

    get lv(): number {
        return this._data['lv'];
    }

    get hp(): number {
        return this._data['hp'];
    }

    get def(): number {
        return this._data['def'];
    }

}

/** growthData ??? */
export class _growthDataAspect {
    public objects: any;

    get id(): any {
        return JSON.parse(this.objects['1'] || '{}');
    }

    get lv(): number {
        return this.objects['2'];
    }

    get hp(): number {
        return this.objects['3'];
    }

    get def(): number {
        return this.objects['4'];
    }

}

/** growthData 鏁版嵁鏄犲皠 */
export class _growthDataAspectMap {
    private _map: Map<number, _growthDataAspect> = new Map();

    set(id: number, aspect: _growthDataAspect) {
        this._map.set(id, aspect);
    }

    get(id: number): _growthDataAspect | undefined {
        return this._map.get(id);
    }
}

/** growthData */
export class _growthData {
    public static Config: _growthDataConfig = new _growthDataConfig();
    public static Aspect: _growthDataAspect[] = [];
    public static AspectMap: _growthDataAspectMap = new _growthDataAspectMap();

    public static findById(id: number): _growthDataAspect | undefined {
        return this.AspectMap.get(id);
    }

    public static load(jsonData: any): void {
        if (jsonData.setConfig) {
            this.Config._data = jsonData.setConfig;
        }
        if (jsonData.setAspect) {
            this.Aspect = [];
            this.AspectMap = new _growthDataAspectMap();
            for (let i = 0; i < jsonData.setAspect.length; i++) {
                let aspect = new _growthDataAspect();
                aspect.objects = JSON.parse(jsonData.setAspect[i]);
                this.Aspect.push(aspect);
                let id = aspect.objects['1'];
                this.AspectMap.set(id, aspect);
            }
        }
    }
}

/** growthType 閰嶇疆 */
export class _growthTypeConfig {
    public _data: any;

    get key(): any {
        return this._data['key'];
    }

    get value(): any {
        return this._data['value'];
    }

}

/** growthType ??? */
export class _growthTypeAspect {
    public objects: any;

    get key(): any {
        return this.objects['2'];
    }

    get value(): any {
        return this.objects['3'];
    }

}

/** growthType 鏁版嵁鏄犲皠 */
export class _growthTypeAspectMap {
    private _map: Map<number, _growthTypeAspect> = new Map();

    set(id: number, aspect: _growthTypeAspect) {
        this._map.set(id, aspect);
    }

    get(id: number): _growthTypeAspect | undefined {
        return this._map.get(id);
    }
}

/** growthType */
export class _growthType {
    public static Config: _growthTypeConfig = new _growthTypeConfig();
    public static Aspect: _growthTypeAspect[] = [];
    public static AspectMap: _growthTypeAspectMap = new _growthTypeAspectMap();

    public static findById(id: number): _growthTypeAspect | undefined {
        return this.AspectMap.get(id);
    }

    public static load(jsonData: any): void {
        if (jsonData.setConfig) {
            this.Config._data = jsonData.setConfig;
        }
        if (jsonData.setAspect) {
            this.Aspect = [];
            this.AspectMap = new _growthTypeAspectMap();
            for (let i = 0; i < jsonData.setAspect.length; i++) {
                let aspect = new _growthTypeAspect();
                aspect.objects = JSON.parse(jsonData.setAspect[i]);
                this.Aspect.push(aspect);
                let id = aspect.objects['1'];
                this.AspectMap.set(id, aspect);
            }
        }
    }
}

