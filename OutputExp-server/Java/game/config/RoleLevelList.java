package game.config;

import com.fasterxml.jackson.databind.JsonNode;
import java.util.HashMap;
import java.util.Map;
import java.util.function.BiConsumer;

/** 角色等级 */
public class RoleLevelList {

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
