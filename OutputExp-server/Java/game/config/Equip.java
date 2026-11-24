package game.config;

import com.fasterxml.jackson.databind.JsonNode;
import java.util.List;

/** 装备 */
public class Equip {

    private final ConfigRow _row;

    public Equip(ConfigRow row) {
        this._row = row;
    }

    private JsonNode[] values() {
        return _row.values();
    }

    /** 道具id */
    public int getId() {
        return ConfigValue.toInt(values()[0]);
    }

    /** 装备类型 */
    public Enums.EquipType getType() {
        return Enums.EquipType.of(ConfigValue.toInt(values()[1]));
    }

    /** 成长类型 */
    public Enums.GrowthType getGType() {
        return Enums.GrowthType.of(ConfigValue.toInt(values()[2]));
    }
}
