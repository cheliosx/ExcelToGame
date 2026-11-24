package game.config;

import com.fasterxml.jackson.databind.JsonNode;

/** 一行数据：首次访问字段时才解析 */
public final class ConfigRow {

    private String data;
    private JsonNode[] values;

    public ConfigRow(String data) {
        this.data = data;
    }

    public JsonNode[] values() {
        if (values == null) {
            values = ConfigValue.parseRow(data);
            data = null;
        }

        return values;
    }
}
