/***
* 该文件由工具自动生成，请勿手动修改
*/
/**
 * 表格导出的枚举统一放在 TableEnums 命名空间下，
 * 与手写枚举区分开，也避免命名冲突。
 */
export namespace TableEnums {
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

}
