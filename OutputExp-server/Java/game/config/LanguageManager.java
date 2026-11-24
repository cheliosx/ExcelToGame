package game.config;

import com.fasterxml.jackson.databind.JsonNode;
import java.util.HashMap;
import java.util.Iterator;
import java.util.Map;

/** 语言表：key 为数字字符串，缺失时回退为 key 本身 */
public final class LanguageManager {

    private static Map<String, String> texts = new HashMap<>();

    private LanguageManager() {
    }

    public static void load(String json) {
        Map<String, String> map = new HashMap<>();
        JsonNode root = ConfigValue.parse(json);

        if (root != null && root.isObject()) {
            Iterator<String> names = root.fieldNames();
            while (names.hasNext()) {
                String name = names.next();
                map.put(name, ConfigValue.toStr(root.get(name)));
            }
        }

        texts = map;
    }

    public static String getText(String key) {
        if (key == null || key.isEmpty()) {
            return "";
        }

        String text = texts.get(key);
        return text != null ? text : key;
    }
}
