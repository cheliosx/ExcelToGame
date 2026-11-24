package game.config;

/**
 * 表格导出的枚举统一放在这里，与手写枚举区分开，也避免命名冲突。
 */
public final class Enums {

    private Enums() {
    }

    /** 装备类型 */
    public enum EquipType {
        /** 武器 */
        Weapon(1),
        /** 衣服 */
        Clothes(2),
        /** 头盔 */
        Helmet(3);

        private final int value;

        EquipType(int value) {
            this.value = value;
        }

        public int getValue() {
            return value;
        }

        /** 由配置里的数值取枚举，找不到返回 null */
        public static EquipType of(int value) {
            for (EquipType item : values()) {
                if (item.value == value) {
                    return item;
                }
            }

            return null;
        }
    }

    /** 成长类型 */
    public enum GrowthType {
        /** 角色成长 */
        Player(1),
        /** 小怪成长 */
        Monster(2),
        /** BOSS成长 */
        Boss(3);

        private final int value;

        GrowthType(int value) {
            this.value = value;
        }

        public int getValue() {
            return value;
        }

        /** 由配置里的数值取枚举，找不到返回 null */
        public static GrowthType of(int value) {
            for (GrowthType item : values()) {
                if (item.value == value) {
                    return item;
                }
            }

            return null;
        }
    }
}
