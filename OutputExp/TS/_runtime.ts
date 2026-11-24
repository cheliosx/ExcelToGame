/***
* 该文件由工具自动生成，请勿手动修改
*/
/** 从 "[1,...]" 里取首列数字（主键 / 分组键） */
export function _parseKey(row: string): number {
    const end = row.indexOf(",");
    return parseInt(row.substring(1, end < 0 ? row.length : end));
}

