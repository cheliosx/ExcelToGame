package game.config;

import com.fasterxml.jackson.databind.JsonNode;
import java.util.List;

/** 属性表 */
public class AttrRef {

    private final ConfigRow _row;

    public AttrRef(ConfigRow row) {
        this._row = row;
    }

    private JsonNode[] values() {
        return _row.values();
    }

    /** 属性ID */
    public int getId() {
        return ConfigValue.toInt(values()[0]);
    }

    /** 属性类型 */
    public int getType() {
        return ConfigValue.toInt(values()[1]);
    }

    /** 属性名称 */
    public String getName() {
        return LanguageManager.getText(ConfigValue.toStr(values()[2]));
    }

    /** 0:数值,1:百分比 */
    public int getUnit() {
        return ConfigValue.toInt(values()[3]);
    }

    /** 属性系数 */
    public int getAttributeCoefficient() {
        return ConfigValue.toInt(values()[4]);
    }

    /** 属性图标 */
    public String getIcon() {
        return ConfigValue.toStr(values()[5]);
    }
}
