package game.config;

import com.fasterxml.jackson.databind.JsonNode;

/** S-属性[Attr] 配置根 */
public class AttrConfig {
    private final AttrRefMap attrRefMap = new AttrRefMap();

    public AttrRefMap getAttrRefMap() {
        return attrRefMap;
    }

    public void load(String json) {
        JsonNode root = ConfigValue.parse(json);
        if (root == null || !root.isObject()) {
            return;
        }

        JsonNode section;

        section = root.get("attrRef");
        if (section != null && !section.isNull()) {
            attrRefMap.loadSection(section);
        }
    }
}
