package game.config;

import com.fasterxml.jackson.databind.JsonNode;
import java.util.List;

/** 道具 */
public class Item {

    private final ConfigRow _row;

    public Item(ConfigRow row) {
        this._row = row;
    }

    private JsonNode[] values() {
        return _row.values();
    }

    /** 道具id */
    public int getId() {
        return ConfigValue.toInt(values()[0]);
    }
}
