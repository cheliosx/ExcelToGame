import { _lang } from "./_language";

/** testArg 配置 */
export class _testArgConfig {
    public _data: any;

}

/** testArg 数据项 */
export class _testArgAspect {
    public objects: any;

}

/** testArg 数据映射 */
export class _testArgAspectMap {
    private _map: Map<number, _testArgAspect> = new Map();

    set(id: number, aspect: _testArgAspect) {
        this._map.set(id, aspect);
    }

    get(id: number): _testArgAspect | undefined {
        return this._map.get(id);
    }
}

/** testArg */
export class _testArg {
    public static Config: _testArgConfig = new _testArgConfig();
    public static Aspect: _testArgAspect[] = [];
    public static AspectMap: _testArgAspectMap = new _testArgAspectMap();

    public static findById(id: number): _testArgAspect | undefined {
        return this.AspectMap.get(id);
    }

    public static load(jsonData: any): void {
        if (jsonData.setConfig) {
            this.Config._data = jsonData.setConfig;
        }
        if (jsonData.setAspect) {
            this.Aspect = [];
            this.AspectMap = new _testArgAspectMap();
            for (let i = 0; i < jsonData.setAspect.length; i++) {
                let aspect = new _testArgAspect();
                aspect.objects = JSON.parse(jsonData.setAspect[i]);
                this.Aspect.push(aspect);
                let id = aspect.objects['1'];
                this.AspectMap.set(id, aspect);
            }
        }
    }
}

/** testKey 配置 */
export class _testKeyConfig {
    public _data: any;

    get number(): any {
        return this._data['number'];
    }

    get number_1(): any {
        return this._data['number'];
    }

    get number_2(): any {
        return this._data['number'];
    }

    get __(): any {
        return this._data['[]'];
    }

    get number_3(): any {
        return this._data['number'];
    }

}

/** testKey 数据项 */
export class _testKeyAspect {
    public objects: any;

    get number(): any {
        return this.objects['1'];
    }

    get language(): string {
        return _lang.t(this.objects['2']);
    }

    get number_1(): any {
        return JSON.parse(this.objects['3'] || '{}');
    }

    get number_2(): any {
        return this.objects['4'];
    }

    get __(): any {
        return JSON.parse(this.objects['5'] || '{}');
    }

    get language_1(): string {
        return _lang.t(this.objects['6']);
    }

    get number_3(): any {
        return this.objects['7'];
    }

}

/** testKey 数据映射 */
export class _testKeyAspectMap {
    private _map: Map<number, _testKeyAspect> = new Map();

    set(id: number, aspect: _testKeyAspect) {
        this._map.set(id, aspect);
    }

    get(id: number): _testKeyAspect | undefined {
        return this._map.get(id);
    }
}

/** testKey */
export class _testKey {
    public static Config: _testKeyConfig = new _testKeyConfig();
    public static Aspect: _testKeyAspect[] = [];
    public static AspectMap: _testKeyAspectMap = new _testKeyAspectMap();

    public static findById(id: number): _testKeyAspect | undefined {
        return this.AspectMap.get(id);
    }

    public static load(jsonData: any): void {
        if (jsonData.setConfig) {
            this.Config._data = jsonData.setConfig;
        }
        if (jsonData.setAspect) {
            this.Aspect = [];
            this.AspectMap = new _testKeyAspectMap();
            for (let i = 0; i < jsonData.setAspect.length; i++) {
                let aspect = new _testKeyAspect();
                aspect.objects = JSON.parse(jsonData.setAspect[i]);
                this.Aspect.push(aspect);
                let id = aspect.objects['1'];
                this.AspectMap.set(id, aspect);
            }
        }
    }
}

/** testGroup 配置 */
export class _testGroupConfig {
    public _data: any;

    get number(): any {
        return this._data['number'];
    }

    get number_1(): any {
        return this._data['number'];
    }

    get number_2(): any {
        return this._data['number'];
    }

    get number_3(): any {
        return this._data['number'];
    }

}

/** testGroup 数据项 */
export class _testGroupAspect {
    public objects: any;

    get number(): any {
        return this.objects['1'];
    }

    get number_1(): any {
        return this.objects['2'];
    }

    get number_2(): any {
        return this.objects['3'];
    }

    get number_3(): any {
        return this.objects['4'];
    }

}

/** testGroup 数据映射 */
export class _testGroupAspectMap {
    private _map: Map<number, _testGroupAspect> = new Map();

    set(id: number, aspect: _testGroupAspect) {
        this._map.set(id, aspect);
    }

    get(id: number): _testGroupAspect | undefined {
        return this._map.get(id);
    }
}

/** testGroup */
export class _testGroup {
    public static Config: _testGroupConfig = new _testGroupConfig();
    public static Aspect: _testGroupAspect[] = [];
    public static AspectMap: _testGroupAspectMap = new _testGroupAspectMap();

    public static findById(id: number): _testGroupAspect | undefined {
        return this.AspectMap.get(id);
    }

    public static load(jsonData: any): void {
        if (jsonData.setConfig) {
            this.Config._data = jsonData.setConfig;
        }
        if (jsonData.setAspect) {
            this.Aspect = [];
            this.AspectMap = new _testGroupAspectMap();
            for (let i = 0; i < jsonData.setAspect.length; i++) {
                let aspect = new _testGroupAspect();
                aspect.objects = JSON.parse(jsonData.setAspect[i]);
                this.Aspect.push(aspect);
                let id = aspect.objects['1'];
                this.AspectMap.set(id, aspect);
            }
        }
    }
}

/** testArray 配置 */
export class _testArrayConfig {
    public _data: any;

    get number(): any {
        return this._data['number'];
    }

    get number_1(): any {
        return this._data['number'];
    }

    get number_2(): any {
        return this._data['number'];
    }

}

/** testArray 数据项 */
export class _testArrayAspect {
    public objects: any;

    get number(): any {
        return this.objects['1'];
    }

    get number_1(): any {
        return this.objects['2'];
    }

    get number_2(): any {
        return this.objects['3'];
    }

}

/** testArray 数据映射 */
export class _testArrayAspectMap {
    private _map: Map<number, _testArrayAspect> = new Map();

    set(id: number, aspect: _testArrayAspect) {
        this._map.set(id, aspect);
    }

    get(id: number): _testArrayAspect | undefined {
        return this._map.get(id);
    }
}

/** testArray */
export class _testArray {
    public static Config: _testArrayConfig = new _testArrayConfig();
    public static Aspect: _testArrayAspect[] = [];
    public static AspectMap: _testArrayAspectMap = new _testArrayAspectMap();

    public static findById(id: number): _testArrayAspect | undefined {
        return this.AspectMap.get(id);
    }

    public static load(jsonData: any): void {
        if (jsonData.setConfig) {
            this.Config._data = jsonData.setConfig;
        }
        if (jsonData.setAspect) {
            this.Aspect = [];
            this.AspectMap = new _testArrayAspectMap();
            for (let i = 0; i < jsonData.setAspect.length; i++) {
                let aspect = new _testArrayAspect();
                aspect.objects = JSON.parse(jsonData.setAspect[i]);
                this.Aspect.push(aspect);
                let id = aspect.objects['1'];
                this.AspectMap.set(id, aspect);
            }
        }
    }
}

/** testSkill 配置 */
export class _testSkillConfig {
    public _data: any;

    get number(): any {
        return this._data['number'];
    }

}

/** testSkill 数据项 */
export class _testSkillAspect {
    public objects: any;

    get number(): any {
        return this.objects['1'];
    }

    get language(): string {
        return _lang.t(this.objects['2']);
    }

}

/** testSkill 数据映射 */
export class _testSkillAspectMap {
    private _map: Map<number, _testSkillAspect> = new Map();

    set(id: number, aspect: _testSkillAspect) {
        this._map.set(id, aspect);
    }

    get(id: number): _testSkillAspect | undefined {
        return this._map.get(id);
    }
}

/** testSkill */
export class _testSkill {
    public static Config: _testSkillConfig = new _testSkillConfig();
    public static Aspect: _testSkillAspect[] = [];
    public static AspectMap: _testSkillAspectMap = new _testSkillAspectMap();

    public static findById(id: number): _testSkillAspect | undefined {
        return this.AspectMap.get(id);
    }

    public static load(jsonData: any): void {
        if (jsonData.setConfig) {
            this.Config._data = jsonData.setConfig;
        }
        if (jsonData.setAspect) {
            this.Aspect = [];
            this.AspectMap = new _testSkillAspectMap();
            for (let i = 0; i < jsonData.setAspect.length; i++) {
                let aspect = new _testSkillAspect();
                aspect.objects = JSON.parse(jsonData.setAspect[i]);
                this.Aspect.push(aspect);
                let id = aspect.objects['1'];
                this.AspectMap.set(id, aspect);
            }
        }
    }
}

/** equipType 配置 */
export class _equipTypeConfig {
    public _data: any;

}

/** equipType 数据项 */
export class _equipTypeAspect {
    public objects: any;

}

/** equipType 数据映射 */
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

/** item 配置 */
export class _itemConfig {
    public _data: any;

    get number(): any {
        return this._data['number'];
    }

    get string(): any {
        return this._data['string'];
    }

}

/** item 数据项 */
export class _itemAspect {
    public objects: any;

    get number(): any {
        return this.objects['1'];
    }

    get language(): string {
        return _lang.t(this.objects['2']);
    }

    get string(): any {
        return this.objects['3'];
    }

}

/** item 数据映射 */
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

/** equip 配置 */
export class _equipConfig {
    public _data: any;

    get number(): any {
        return this._data['number'];
    }

    get number_1(): any {
        return this._data['number'];
    }

}

/** equip 数据项 */
export class _equipAspect {
    public objects: any;

    get number(): any {
        return this.objects['1'];
    }

    get number_1(): any {
        return JSON.parse(this.objects['2'] || '{}');
    }

}

/** equip 数据映射 */
export class _equipAspectMap {
    private _map: Map<number, _equipAspect> = new Map();

    set(id: number, aspect: _equipAspect) {
        this._map.set(id, aspect);
    }

    get(id: number): _equipAspect | undefined {
        return this._map.get(id);
    }
}

/** equip */
export class _equip {
    public static Config: _equipConfig = new _equipConfig();
    public static Aspect: _equipAspect[] = [];
    public static AspectMap: _equipAspectMap = new _equipAspectMap();

    public static findById(id: number): _equipAspect | undefined {
        return this.AspectMap.get(id);
    }

    public static load(jsonData: any): void {
        if (jsonData.setConfig) {
            this.Config._data = jsonData.setConfig;
        }
        if (jsonData.setAspect) {
            this.Aspect = [];
            this.AspectMap = new _equipAspectMap();
            for (let i = 0; i < jsonData.setAspect.length; i++) {
                let aspect = new _equipAspect();
                aspect.objects = JSON.parse(jsonData.setAspect[i]);
                this.Aspect.push(aspect);
                let id = aspect.objects['1'];
                this.AspectMap.set(id, aspect);
            }
        }
    }
}

