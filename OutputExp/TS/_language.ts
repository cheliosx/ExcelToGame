/***
* 该文件由工具自动生成，请勿手动修改
*/
export class _language {
    private _texts: Record<string, string> = {};

    public load(texts: Record<string, string>): void {
        this._texts = texts || {};
    }

    /** 取多语言文本，缺失时回退为 key 本身 */
    public t(key: string): string {
        if (key === null || key === undefined) {
            return "";
        }
        const text = this._texts[key];
        return text === undefined ? key : text;
    }

    /** 把文本中的 {0} {1} 替换为参数 */
    public replaceTemplate(template: string, ...values: any[]): string {
        return template.replace(/\{(\d+)\}/g, (match, index) => {
            const idx = parseInt(index);
            return idx < values.length ? String(values[idx]) : match;
        });
    }
}

export const _lang = new _language();
