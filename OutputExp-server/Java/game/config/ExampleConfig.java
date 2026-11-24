package game.config;

import com.fasterxml.jackson.databind.JsonNode;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.function.BiConsumer;

/** example[表格示例] 配置根：页签是内部类，用法见各 getter */
public class ExampleConfig {
    private final GameArgConfig gameArgConfig = new GameArgConfig();
    private final RoleLevelList roleLevelList = new RoleLevelList();
    private final ItemMap itemMap = new ItemMap();
    private final EquipMap equipMap = new EquipMap();
    private final GrowthDataGroup growthDataGroup = new GrowthDataGroup();

    /** 游戏参数表 */
    public GameArgConfig getGameArgConfig() {
        return gameArgConfig;
    }

    /** 角色等级 */
    public RoleLevelList getRoleLevelList() {
        return roleLevelList;
    }

    /** 道具 */
    public ItemMap getItemMap() {
        return itemMap;
    }

    /** 装备 */
    public EquipMap getEquipMap() {
        return equipMap;
    }

    /** 成长数据 */
    public GrowthDataGroup getGrowthDataGroup() {
        return growthDataGroup;
    }

    /** 解析整个表格文件的 JSON，再分发到各张表 */
    public void load(String json) {
        JsonNode root = ConfigValue.parse(json);
        if (root == null || !root.isObject()) {
            return;
        }

        JsonNode section;

        section = root.get("gameArg");
        if (section != null && !section.isNull()) {
            gameArgConfig.loadSection(section);
        }

        section = root.get("roleLevel");
        if (section != null && !section.isNull()) {
            roleLevelList.loadSection(section);
        }

        section = root.get("item");
        if (section != null && !section.isNull()) {
            itemMap.loadSection(section);
        }

        section = root.get("equip");
        if (section != null && !section.isNull()) {
            equipMap.loadSection(section);
        }

        section = root.get("growthData");
        if (section != null && !section.isNull()) {
            growthDataGroup.loadSection(section);
        }
    }

    /** 角色等级 */
    public static final class RoleLevel {

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

    /** 道具 */
    public static final class Item {

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

    /** 装备 */
    public static final class Equip {

        private final ConfigRow _row;

        public Equip(ConfigRow row) {
            this._row = row;
        }

        private JsonNode[] values() {
            return _row.values();
        }

        /** 道具id */
        public int getId() {
            return ConfigValue.toInt(values()[0]);
        }

        /** 装备类型 */
        public Enums.EquipType getType() {
            return Enums.EquipType.of(ConfigValue.toInt(values()[1]));
        }

        /** 成长类型 */
        public Enums.GrowthType getGType() {
            return Enums.GrowthType.of(ConfigValue.toInt(values()[2]));
        }
    }

    /** 成长数据 */
    public static final class GrowthData {

        private final ConfigRow _row;

        public GrowthData(ConfigRow row) {
            this._row = row;
        }

        private JsonNode[] values() {
            return _row.values();
        }

        /** 成长类型 */
        public Enums.GrowthType getId() {
            return Enums.GrowthType.of(ConfigValue.toInt(values()[0]));
        }

        /** 等级 */
        public int getLv() {
            return ConfigValue.toInt(values()[1]);
        }

        /** 生命 */
        public int getHp() {
            return ConfigValue.toInt(values()[2]);
        }

        /** 防御 */
        public int getDef() {
            return ConfigValue.toInt(values()[3]);
        }
    }

    /** 游戏参数表 */
    public static final class GameArgConfig {
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

    /** 道具 */
    public static final class ItemMap {

        private final ConfigTable _table = new ConfigTable();

        /** 主键 -> 行号 */
        private final Map<Integer, Integer> _index = new HashMap<>();

        /** 已访问过的行（只缓存用到的实体） */
        private final Map<Integer, Item> _data = new HashMap<>();

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
        public Item findById(int id) {
            Item cached = _data.get(id);
            if (cached != null) {
                return cached;
            }

            Integer row = _index.get(id);
            if (row == null) {
                return null;
            }

            Item item = new Item(_table.row(row));
            _data.put(id, item);
            return item;
        }

        /** 全表遍历：逐行创建实体，不驻留整表对象 */
        public void forEach(BiConsumer<Item, Integer> callback) {
            for (int i = 0; i < _table.count(); i++) {
                callback.accept(new Item(_table.row(i)), i);
            }
        }
    }

    /** 装备 */
    public static final class EquipMap {

        private final ConfigTable _table = new ConfigTable();

        /** 主键 -> 行号 */
        private final Map<Integer, Integer> _index = new HashMap<>();

        /** 已访问过的行（只缓存用到的实体） */
        private final Map<Integer, Equip> _data = new HashMap<>();

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
        public Equip findById(int id) {
            Equip cached = _data.get(id);
            if (cached != null) {
                return cached;
            }

            Integer row = _index.get(id);
            if (row == null) {
                return null;
            }

            Equip item = new Equip(_table.row(row));
            _data.put(id, item);
            return item;
        }

        /** 全表遍历：逐行创建实体，不驻留整表对象 */
        public void forEach(BiConsumer<Equip, Integer> callback) {
            for (int i = 0; i < _table.count(); i++) {
                callback.accept(new Equip(_table.row(i)), i);
            }
        }
    }

    /** 角色等级 */
    public static final class RoleLevelList {

        private final ConfigTable _table = new ConfigTable();

        /** 已访问过的行（只缓存用到的实体） */
        private final Map<Integer, RoleLevel> _data = new HashMap<>();

        public int getCount() {
            return _table.count();
        }

        public void load(String json) {
            loadSection(ConfigValue.parse(json));
        }

        void loadSection(JsonNode value) {
            _table.loadSection(value);
            _data.clear();
        }

        public RoleLevel findByIndex(int index) {
            if (index < 0 || index >= _table.count()) {
                return null;
            }

            RoleLevel cached = _data.get(index);
            if (cached != null) {
                return cached;
            }

            RoleLevel item = new RoleLevel(_table.row(index));
            _data.put(index, item);
            return item;
        }

        public void forEach(BiConsumer<RoleLevel, Integer> callback) {
            for (int i = 0; i < _table.count(); i++) {
                callback.accept(new RoleLevel(_table.row(i)), i);
            }
        }
    }

    /** 成长数据 */
    public static final class GrowthDataGroup {

        private final ConfigTable _table = new ConfigTable();

        /** 分组键 -> 行号列表 */
        private final Map<Integer, List<Integer>> _index = new HashMap<>();

        /** 已访问过的行（只缓存用到的实体） */
        private final Map<Integer, GrowthData> _data = new HashMap<>();

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
                int key = _table.readKey(i);
                List<Integer> rows = _index.get(key);

                if (rows == null) {
                    rows = new ArrayList<>();
                    _index.put(key, rows);
                }

                rows.add(i);
            }
        }

        public List<GrowthData> findGroupById(int id) {
            List<Integer> rows = _index.get(id);
            if (rows == null) {
                return null;
            }

            List<GrowthData> result = new ArrayList<>(rows.size());
            for (int i = 0; i < rows.size(); i++) {
                result.add(getEntity(rows.get(i)));
            }

            return result;
        }

        /** 同一行只创建一次实体 */
        private GrowthData getEntity(int row) {
            GrowthData cached = _data.get(row);
            if (cached != null) {
                return cached;
            }

            GrowthData item = new GrowthData(_table.row(row));
            _data.put(row, item);
            return item;
        }
    }
}
