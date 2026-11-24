package game.config;

import com.fasterxml.jackson.databind.JsonNode;
import java.util.List;

/** 角色等级 */
public class RoleLevel {

    private final ConfigRow _row;

    public RoleLevel(ConfigRow row) {
        this._row = row;
    }

    private JsonNode[] values() {
        return _row.values();
    }

    /** 生命 */
    public int getHp() {
        return ConfigValue.toInt(values()[0]);
    }

    /** 防御 */
    public int getDef() {
        return ConfigValue.toInt(values()[1]);
    }
}
