package game.config;

import com.fasterxml.jackson.databind.JsonNode;

/**
 * 行数据容器：只保存原始行文本，
 * 访问某行时才解析该行，避免一次性创建几十万个数组。
 */
public final class ConfigTable {

    private String[] rows = new String[0];

    /** 由 JSON 的字符串数组加载 */
    public void load(String json) {
        this.rows = ConfigValue.toRows(ConfigValue.parse(json));
    }

    void loadSection(JsonNode value) {
        this.rows = ConfigValue.toRows(value);
    }

    public int count() {
        return rows.length;
    }

    public ConfigRow row(int index) {
        return new ConfigRow(index >= 0 && index < rows.length ? rows[index] : null);
    }

    /** 只读取行首的数值（分组键），不解析整行 */
    public int readKey(int index) {
        return ConfigValue.readKey(rows[index]);
    }
}
