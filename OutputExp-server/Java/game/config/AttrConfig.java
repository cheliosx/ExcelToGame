package game.config;

import com.fasterxml.jackson.databind.JsonNode;
import java.util.HashMap;
import java.util.Map;
import java.util.function.BiConsumer;

/** S-属性[Attr] 配置根：页签是内部类，用法见各 getter */
public class AttrConfig {
    private final AttrRefMap attrRefMap = new AttrRefMap();

    /** 属性表 */
    public AttrRefMap getAttrRefMap() {
        return attrRefMap;
    }

    /** 解析整个表格文件的 JSON，再分发到各张表 */
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

    /** 属性表 */
    public static final class AttrRef {

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

    /** 属性表 */
    public static final class AttrRefMap {

        private final ConfigTable _table = new ConfigTable();

        /** 主键 -> 行号 */
        private final Map<Integer, Integer> _index = new HashMap<>();

        /** 已访问过的行（只缓存用到的实体） */
        private final Map<Integer, AttrRef> _data = new HashMap<>();

        public int getCount() {
            return _table.count();
        }

        public void load(String json) {
            loadSection(ConfigValue.parse(json));
        }

        void loadSection(JsonNode value) {
            _table.loadSection(value);
            _index.clear();
            _data.clear();

            for (int i = 0; i < _table.count(); i++) {
                _index.put(_table.readKey(i), i);
            }
        }

        /** 按主键取一行：定位到行号后才创建实体并解析 */
        public AttrRef findById(int id) {
            AttrRef cached = _data.get(id);
            if (cached != null) {
                return cached;
            }

            Integer row = _index.get(id);
            if (row == null) {
                return null;
            }

            AttrRef item = new AttrRef(_table.row(row));
            _data.put(id, item);
            return item;
        }

        /** 全表遍历：逐行创建实体，不驻留整表对象 */
        public void forEach(BiConsumer<AttrRef, Integer> callback) {
            for (int i = 0; i < _table.count(); i++) {
                callback.accept(new AttrRef(_table.row(i)), i);
            }
        }
    }
}
