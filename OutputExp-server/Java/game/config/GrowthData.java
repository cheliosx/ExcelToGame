package game.config;

import com.fasterxml.jackson.databind.JsonNode;
import java.util.List;

/** 成长数据 */
public class GrowthData {

    private final ConfigRow _row;

    public GrowthData(ConfigRow row) {
        this._row = row;
    }

    private JsonNode[] values() {
        return _row.values();
    }

    /** 成长类型 */
    public Enums.GrowthType getId() {
        return Enums.GrowthType.of(ConfigValue.toInt(values()[0]));
    }

    /** 等级 */
    public int getLv() {
        return ConfigValue.toInt(values()[1]);
    }

    /** 生命 */
    public int getHp() {
        return ConfigValue.toInt(values()[2]);
    }

    /** 防御 */
    public int getDef() {
        return ConfigValue.toInt(values()[3]);
    }
}
