package game.config;

import java.util.Locale;

/** 所有表格文件的统一入口 */
public class ExcelData {
    private final ExampleConfig example = new ExampleConfig();
    private final AttrConfig attr = new AttrConfig();

    private String langCode = "zh-Hans";

    /** 当前语言码，如 zh-Hans / en-US */
    public String getLang() {
        return langCode;
    }

    public ExampleConfig getExample() {
        return example;
    }

    public AttrConfig getAttr() {
        return attr;
    }

    /**
     * 初始化多语言
     * @param lang 语言码，如 "zh-Hans" / "en-US"
     * @param json 该语言的 key -> 文本
     */
    public void initLanguage(String lang, String json) {
        this.langCode = lang;
        LanguageManager.load(json);
    }

    /**
     * 按文件名加载一个表格文件的 JSON（文件名不区分大小写）
     * @param fileName 文件名（输出名，如 "example"）
     * @param json 该文件对应的 JSON
     */
    public void load(String fileName, String json) {
        String key = fileName == null ? "" : fileName.toLowerCase(Locale.ROOT);

        switch (key) {
            case "example":
                example.load(json);
                return;
            case "attr":
                attr.load(json);
                return;
            default:
                throw new IllegalArgumentException("ExcelData: 未找到配置文件 " + fileName);
        }
    }
}
