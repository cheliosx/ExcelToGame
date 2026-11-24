package game.config;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import java.util.ArrayList;
import java.util.List;
import java.util.function.IntFunction;

/**
 * 取值工具：把 Jackson 的 JsonNode 转成字段需要的基础类型。
 * 依赖 com.fasterxml.jackson.core:jackson-databind（2.13+ 均可用）。
 */
final class ConfigValue {

    private static final ObjectMapper MAPPER = new ObjectMapper();

    private static final JsonNode[] EMPTY = new JsonNode[0];

    private ConfigValue() {
    }

    /** 解析 JSON 文本；空内容返回 null */
    static JsonNode parse(String json) {
        if (json == null || json.isEmpty()) {
            return null;
        }

        try {
            return MAPPER.readTree(json);
        } catch (Exception e) {
            throw new IllegalStateException("JSON 解析失败: " + e.getMessage(), e);
        }
    }

    static int toInt(JsonNode node) {
        if (node == null || node.isNull()) {
            return 0;
        }

        if (node.isNumber()) {
            return node.intValue();
        }

        if (node.isBoolean()) {
            return node.booleanValue() ? 1 : 0;
        }

        if (node.isTextual()) {
            try {
                return (int) Double.parseDouble(node.textValue().trim());
            } catch (NumberFormatException e) {
                return 0;
            }
        }

        return 0;
    }

    static long toLong(JsonNode node) {
        if (node == null || node.isNull()) {
            return 0L;
        }

        if (node.isNumber()) {
            return node.longValue();
        }

        if (node.isBoolean()) {
            return node.booleanValue() ? 1L : 0L;
        }

        if (node.isTextual()) {
            try {
                return (long) Double.parseDouble(node.textValue().trim());
            } catch (NumberFormatException e) {
                return 0L;
            }
        }

        return 0L;
    }

    static float toFloat(JsonNode node) {
        if (node == null || node.isNull()) {
            return 0f;
        }

        if (node.isNumber()) {
            return node.floatValue();
        }

        if (node.isBoolean()) {
            return node.booleanValue() ? 1f : 0f;
        }

        if (node.isTextual()) {
            try {
                return Float.parseFloat(node.textValue().trim());
            } catch (NumberFormatException e) {
                return 0f;
            }
        }

        return 0f;
    }

    static double toDouble(JsonNode node) {
        if (node == null || node.isNull()) {
            return 0d;
        }

        if (node.isNumber()) {
            return node.doubleValue();
        }

        if (node.isBoolean()) {
            return node.booleanValue() ? 1d : 0d;
        }

        if (node.isTextual()) {
            try {
                return Double.parseDouble(node.textValue().trim());
            } catch (NumberFormatException e) {
                return 0d;
            }
        }

        return 0d;
    }

    static boolean toBool(JsonNode node) {
        if (node == null || node.isNull()) {
            return false;
        }

        if (node.isBoolean()) {
            return node.booleanValue();
        }

        if (node.isTextual()) {
            String text = node.textValue().trim();
            if (text.equalsIgnoreCase("true") || text.equalsIgnoreCase("yes")) {
                return true;
            }

            if (text.isEmpty() || text.equalsIgnoreCase("false") || text.equalsIgnoreCase("no")) {
                return false;
            }
        }

        return toInt(node) != 0;
    }

    static String toStr(JsonNode node) {
        if (node == null || node.isNull()) {
            return "";
        }

        if (node.isTextual()) {
            return node.textValue();
        }

        if (node.isDouble() || node.isFloat()) {
            double number = node.doubleValue();
            if (number == Math.rint(number) && !Double.isInfinite(number)) {
                return String.valueOf((long) number);
            }
        }

        return node.isValueNode() ? node.asText() : node.toString();
    }

    static List<Integer> toIntList(JsonNode node) {
        List<Integer> result = new ArrayList<>();
        if (node == null || !node.isArray()) {
            return result;
        }

        for (JsonNode item : node) {
            result.add(toInt(item));
        }

        return result;
    }

    static List<Long> toLongList(JsonNode node) {
        List<Long> result = new ArrayList<>();
        if (node == null || !node.isArray()) {
            return result;
        }

        for (JsonNode item : node) {
            result.add(toLong(item));
        }

        return result;
    }

    static List<Float> toFloatList(JsonNode node) {
        List<Float> result = new ArrayList<>();
        if (node == null || !node.isArray()) {
            return result;
        }

        for (JsonNode item : node) {
            result.add(toFloat(item));
        }

        return result;
    }

    static List<Double> toDoubleList(JsonNode node) {
        List<Double> result = new ArrayList<>();
        if (node == null || !node.isArray()) {
            return result;
        }

        for (JsonNode item : node) {
            result.add(toDouble(item));
        }

        return result;
    }

    static List<Boolean> toBoolList(JsonNode node) {
        List<Boolean> result = new ArrayList<>();
        if (node == null || !node.isArray()) {
            return result;
        }

        for (JsonNode item : node) {
            result.add(toBool(item));
        }

        return result;
    }

    static List<String> toStrList(JsonNode node) {
        List<String> result = new ArrayList<>();
        if (node == null || !node.isArray()) {
            return result;
        }

        for (JsonNode item : node) {
            result.add(toStr(item));
        }

        return result;
    }

    static List<Object> toObjectList(JsonNode node) {
        List<Object> result = new ArrayList<>();
        if (node == null || !node.isArray()) {
            return result;
        }

        for (JsonNode item : node) {
            result.add(toObject(item));
        }

        return result;
    }

    /** 不限类型的字段：转成 Java 原生 Map / List / String / Number / Boolean */
    static Object toObject(JsonNode node) {
        if (node == null || node.isNull()) {
            return null;
        }

        if (node.isTextual()) {
            return node.textValue();
        }

        if (node.isBoolean()) {
            return node.booleanValue();
        }

        if (node.isIntegralNumber()) {
            if (node.canConvertToInt()) {
                return node.intValue();
            }

            return node.longValue();
        }

        if (node.isNumber()) {
            return node.doubleValue();
        }

        if (node.isArray()) {
            return toObjectList(node);
        }

        if (node.isObject()) {
            java.util.Map<String, Object> map = new java.util.LinkedHashMap<>();
            java.util.Iterator<String> names = node.fieldNames();
            while (names.hasNext()) {
                String name = names.next();
                map.put(name, toObject(node.get(name)));
            }

            return map;
        }

        return node.asText();
    }

    /** 枚举数组：按数值查回枚举，找不到保留 null */
    static <T> List<T> toEnumList(JsonNode node, IntFunction<T> mapper) {
        List<T> result = new ArrayList<>();
        if (node == null || !node.isArray()) {
            return result;
        }

        for (JsonNode item : node) {
            result.add(mapper.apply(toInt(item)));
        }

        return result;
    }

    /** JSON 段（字符串数组）-> 行文本数组；行是真数组时写回文本 */
    static String[] toRows(JsonNode node) {
        if (node == null || !node.isArray()) {
            return new String[0];
        }

        String[] rows = new String[node.size()];
        for (int i = 0; i < rows.length; i++) {
            JsonNode item = node.get(i);
            rows[i] = item.isTextual() ? item.textValue() : item.toString();
        }

        return rows;
    }

    /** 从 "[1,...]" 里取首列数字（主键 / 分组键） */
    static int readKey(String row) {
        if (row == null || row.isEmpty()) {
            return 0;
        }

        int start = row.charAt(0) == '[' ? 1 : 0;
        int end = row.indexOf(',', start);
        if (end < 0) {
            end = row.length();
        }

        // 单列表的行形如 "[1]"，把结尾的 ] 和空白去掉再解析
        while (end > start && (row.charAt(end - 1) == ']' || Character.isWhitespace(row.charAt(end - 1)))) {
            end--;
        }

        try {
            return (int) Double.parseDouble(row.substring(start, end).trim());
        } catch (NumberFormatException e) {
            return 0;
        }
    }

    /** 解析一行文本：数组内的每个元素保持 JsonNode，取值时再转 */
    static JsonNode[] parseRow(String row) {
        if (row == null || row.isEmpty()) {
            return EMPTY;
        }

        JsonNode node = parse(row);
        if (node == null || !node.isArray()) {
            return EMPTY;
        }

        JsonNode[] values = new JsonNode[node.size()];
        for (int i = 0; i < values.length; i++) {
            values[i] = node.get(i);
        }

        return values;
    }
}
