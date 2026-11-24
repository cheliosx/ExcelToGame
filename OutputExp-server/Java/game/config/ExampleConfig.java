package game.config;

import com.fasterxml.jackson.databind.JsonNode;

/** example[表格示例] 配置根 */
public class ExampleConfig {
    private final GameArgConfig gameArgConfig = new GameArgConfig();
    private final RoleLevelList roleLevelList = new RoleLevelList();
    private final ItemMap itemMap = new ItemMap();
    private final EquipMap equipMap = new EquipMap();
    private final GrowthDataGroup growthDataGroup = new GrowthDataGroup();

    public GameArgConfig getGameArgConfig() {
        return gameArgConfig;
    }

    public RoleLevelList getRoleLevelList() {
        return roleLevelList;
    }

    public ItemMap getItemMap() {
        return itemMap;
    }

    public EquipMap getEquipMap() {
        return equipMap;
    }

    public GrowthDataGroup getGrowthDataGroup() {
        return growthDataGroup;
    }

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
}
