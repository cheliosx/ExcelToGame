package game.config;

import com.fasterxml.jackson.databind.JsonNode;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

/** 成长数据 */
public class GrowthDataGroup {

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
