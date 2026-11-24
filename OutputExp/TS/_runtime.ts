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

    /** 由 JSON 的 rows（逐行字符串数组）构造 */
    public static fromPayload(payload: any): _TableData {
        if (payload === null || payload === undefined || !Array.isArray(payload.rows)) {
            return new _TableData("");
        }
        return new _TableData(payload.rows.join("\n"));
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

