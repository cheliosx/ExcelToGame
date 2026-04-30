export class _language {
    private _data: any;

    load(_data: any) {
        this._data = _data;
    }

    t(id: string): string {
        let data = this._data[id];
        if (!data) {
            data = id;
        }
        return data;
    }

    replaceTemplate(template: string, ...values: any[]): string {
        return template.replace(/\$\{(\d+)\}/g, (match, index) => {
            const idx = parseInt(index);
            return idx < values.length ? values[idx] : match;
        });
    }
}

export const _lang = new _language();
