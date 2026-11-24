package game.config;

import com.fasterxml.jackson.databind.JsonNode;
import java.util.HashMap;
import java.util.Map;
import java.util.function.BiConsumer;

/** 装备 */
public class EquipMap {

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
