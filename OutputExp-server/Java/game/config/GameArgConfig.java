package game.config;

import com.fasterxml.jackson.databind.JsonNode;
import java.util.ArrayList;
import java.util.List;

/** 游戏参数表 */
public class GameArgConfig {
    /** 游戏名字 */
    private String gameName = "";

    /** 初始血量 */
    private int initHp;

    /** 初始道具 */
    private List<Object> initItems = new ArrayList<>();

    /** 游戏路径 */
    private String gamePath = "";

    public String getGameName() {
        return gameName;
    }

    public int getInitHp() {
        return initHp;
    }

    public List<Object> getInitItems() {
        return initItems;
    }

    public String getGamePath() {
        return gamePath;
    }

    public void load(String json) {
        loadSection(ConfigValue.parse(json));
    }

    void loadSection(JsonNode value) {
        if (value == null || !value.isObject()) {
            return;
        }

        JsonNode fields = value;

        if (fields.has("gameName")) {
            gameName = LanguageManager.getText(ConfigValue.toStr(fields.get("gameName")));
        }

        if (fields.has("initHp")) {
            initHp = ConfigValue.toInt(fields.get("initHp"));
        }

        if (fields.has("initItems")) {
            initItems = ConfigValue.toObjectList(fields.get("initItems"));
        }

        if (fields.has("gamePath")) {
            gamePath = ConfigValue.toStr(fields.get("gamePath"));
        }
    }
}
