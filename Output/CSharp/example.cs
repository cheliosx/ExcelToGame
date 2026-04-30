using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Game.Config
{
    /// <summary>testArg 配置</summary>
    public class testArgConfig
    {
    }

    /// <summary>testArg 数据项</summary>
    public class testArgAspect
    {
    }

    /// <summary>testArg</summary>
    public static class testArg
    {
        public static testArgConfig Config { get; private set; } = new testArgConfig();
        public static List<testArgAspect> Aspects { get; private set; } = new List<testArgAspect>();
        public static Dictionary<int, testArgAspect> AspectMap { get; private set; } = new Dictionary<int, testArgAspect>();

        public static testArgAspect FindById(int id)
        {
            return AspectMap.TryGetValue(id, out var aspect) ? aspect : null;
        }

        public static void Load(string json)
        {
            var data = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            if (data.TryGetValue("setConfig", out var config))
            {
                Config = JsonSerializer.Deserialize<testArgConfig>(config.ToString());
            }
            if (data.TryGetValue("setAspect", out var aspects))
            {
                Aspects = new List<testArgAspect>();
                AspectMap = new Dictionary<int, testArgAspect>();
                var aspectList = JsonSerializer.Deserialize<List<string>>(aspects.ToString());
                foreach (var aspectJson in aspectList)
                {
                    var aspect = JsonSerializer.Deserialize<testArgAspect>(aspectJson);
                    Aspects.Add(aspect);
                    if (!AspectMap.ContainsKey(aspect.Id))
                        AspectMap.Add(aspect.Id, aspect);
                }
            }
        }
    }

    /// <summary>testKey 配置</summary>
    public class testKeyConfig
    {
        public object number { get; set; }
        public object number_1 { get; set; }
        public object number_2 { get; set; }
        public object __ { get; set; }
        public object number_3 { get; set; }
    }

    /// <summary>testKey 数据项</summary>
    public class testKeyAspect
    {
        public object number { get; set; }
        public string language => LanguageManager.GetText(_languageKey);
        [JsonPropertyName("language")] private string _languageKey;
        public object number_1 { get; set; }
        public object number_2 { get; set; }
        public object __ { get; set; }
        public string language_1 => LanguageManager.GetText(_language_1Key);
        [JsonPropertyName("language")] private string _language_1Key;
        public object number_3 { get; set; }
    }

    /// <summary>testKey</summary>
    public static class testKey
    {
        public static testKeyConfig Config { get; private set; } = new testKeyConfig();
        public static List<testKeyAspect> Aspects { get; private set; } = new List<testKeyAspect>();
        public static Dictionary<int, testKeyAspect> AspectMap { get; private set; } = new Dictionary<int, testKeyAspect>();

        public static testKeyAspect FindById(int id)
        {
            return AspectMap.TryGetValue(id, out var aspect) ? aspect : null;
        }

        public static void Load(string json)
        {
            var data = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            if (data.TryGetValue("setConfig", out var config))
            {
                Config = JsonSerializer.Deserialize<testKeyConfig>(config.ToString());
            }
            if (data.TryGetValue("setAspect", out var aspects))
            {
                Aspects = new List<testKeyAspect>();
                AspectMap = new Dictionary<int, testKeyAspect>();
                var aspectList = JsonSerializer.Deserialize<List<string>>(aspects.ToString());
                foreach (var aspectJson in aspectList)
                {
                    var aspect = JsonSerializer.Deserialize<testKeyAspect>(aspectJson);
                    Aspects.Add(aspect);
                    if (!AspectMap.ContainsKey(aspect.Id))
                        AspectMap.Add(aspect.Id, aspect);
                }
            }
        }
    }

    /// <summary>testGroup 配置</summary>
    public class testGroupConfig
    {
        public object number { get; set; }
        public object number_1 { get; set; }
        public object number_2 { get; set; }
        public object number_3 { get; set; }
    }

    /// <summary>testGroup 数据项</summary>
    public class testGroupAspect
    {
        public object number { get; set; }
        public object number_1 { get; set; }
        public object number_2 { get; set; }
        public object number_3 { get; set; }
    }

    /// <summary>testGroup</summary>
    public static class testGroup
    {
        public static testGroupConfig Config { get; private set; } = new testGroupConfig();
        public static List<testGroupAspect> Aspects { get; private set; } = new List<testGroupAspect>();
        public static Dictionary<int, testGroupAspect> AspectMap { get; private set; } = new Dictionary<int, testGroupAspect>();

        public static testGroupAspect FindById(int id)
        {
            return AspectMap.TryGetValue(id, out var aspect) ? aspect : null;
        }

        public static void Load(string json)
        {
            var data = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            if (data.TryGetValue("setConfig", out var config))
            {
                Config = JsonSerializer.Deserialize<testGroupConfig>(config.ToString());
            }
            if (data.TryGetValue("setAspect", out var aspects))
            {
                Aspects = new List<testGroupAspect>();
                AspectMap = new Dictionary<int, testGroupAspect>();
                var aspectList = JsonSerializer.Deserialize<List<string>>(aspects.ToString());
                foreach (var aspectJson in aspectList)
                {
                    var aspect = JsonSerializer.Deserialize<testGroupAspect>(aspectJson);
                    Aspects.Add(aspect);
                    if (!AspectMap.ContainsKey(aspect.Id))
                        AspectMap.Add(aspect.Id, aspect);
                }
            }
        }
    }

    /// <summary>testArray 配置</summary>
    public class testArrayConfig
    {
        public object number { get; set; }
        public object number_1 { get; set; }
        public object number_2 { get; set; }
    }

    /// <summary>testArray 数据项</summary>
    public class testArrayAspect
    {
        public object number { get; set; }
        public object number_1 { get; set; }
        public object number_2 { get; set; }
    }

    /// <summary>testArray</summary>
    public static class testArray
    {
        public static testArrayConfig Config { get; private set; } = new testArrayConfig();
        public static List<testArrayAspect> Aspects { get; private set; } = new List<testArrayAspect>();
        public static Dictionary<int, testArrayAspect> AspectMap { get; private set; } = new Dictionary<int, testArrayAspect>();

        public static testArrayAspect FindById(int id)
        {
            return AspectMap.TryGetValue(id, out var aspect) ? aspect : null;
        }

        public static void Load(string json)
        {
            var data = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            if (data.TryGetValue("setConfig", out var config))
            {
                Config = JsonSerializer.Deserialize<testArrayConfig>(config.ToString());
            }
            if (data.TryGetValue("setAspect", out var aspects))
            {
                Aspects = new List<testArrayAspect>();
                AspectMap = new Dictionary<int, testArrayAspect>();
                var aspectList = JsonSerializer.Deserialize<List<string>>(aspects.ToString());
                foreach (var aspectJson in aspectList)
                {
                    var aspect = JsonSerializer.Deserialize<testArrayAspect>(aspectJson);
                    Aspects.Add(aspect);
                    if (!AspectMap.ContainsKey(aspect.Id))
                        AspectMap.Add(aspect.Id, aspect);
                }
            }
        }
    }

    /// <summary>testSkill 配置</summary>
    public class testSkillConfig
    {
        public object number { get; set; }
    }

    /// <summary>testSkill 数据项</summary>
    public class testSkillAspect
    {
        public object number { get; set; }
        public string language => LanguageManager.GetText(_languageKey);
        [JsonPropertyName("language")] private string _languageKey;
    }

    /// <summary>testSkill</summary>
    public static class testSkill
    {
        public static testSkillConfig Config { get; private set; } = new testSkillConfig();
        public static List<testSkillAspect> Aspects { get; private set; } = new List<testSkillAspect>();
        public static Dictionary<int, testSkillAspect> AspectMap { get; private set; } = new Dictionary<int, testSkillAspect>();

        public static testSkillAspect FindById(int id)
        {
            return AspectMap.TryGetValue(id, out var aspect) ? aspect : null;
        }

        public static void Load(string json)
        {
            var data = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            if (data.TryGetValue("setConfig", out var config))
            {
                Config = JsonSerializer.Deserialize<testSkillConfig>(config.ToString());
            }
            if (data.TryGetValue("setAspect", out var aspects))
            {
                Aspects = new List<testSkillAspect>();
                AspectMap = new Dictionary<int, testSkillAspect>();
                var aspectList = JsonSerializer.Deserialize<List<string>>(aspects.ToString());
                foreach (var aspectJson in aspectList)
                {
                    var aspect = JsonSerializer.Deserialize<testSkillAspect>(aspectJson);
                    Aspects.Add(aspect);
                    if (!AspectMap.ContainsKey(aspect.Id))
                        AspectMap.Add(aspect.Id, aspect);
                }
            }
        }
    }

    /// <summary>equipType 配置</summary>
    public class equipTypeConfig
    {
    }

    /// <summary>equipType 数据项</summary>
    public class equipTypeAspect
    {
    }

    /// <summary>equipType</summary>
    public static class equipType
    {
        public static equipTypeConfig Config { get; private set; } = new equipTypeConfig();
        public static List<equipTypeAspect> Aspects { get; private set; } = new List<equipTypeAspect>();
        public static Dictionary<int, equipTypeAspect> AspectMap { get; private set; } = new Dictionary<int, equipTypeAspect>();

        public static equipTypeAspect FindById(int id)
        {
            return AspectMap.TryGetValue(id, out var aspect) ? aspect : null;
        }

        public static void Load(string json)
        {
            var data = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            if (data.TryGetValue("setConfig", out var config))
            {
                Config = JsonSerializer.Deserialize<equipTypeConfig>(config.ToString());
            }
            if (data.TryGetValue("setAspect", out var aspects))
            {
                Aspects = new List<equipTypeAspect>();
                AspectMap = new Dictionary<int, equipTypeAspect>();
                var aspectList = JsonSerializer.Deserialize<List<string>>(aspects.ToString());
                foreach (var aspectJson in aspectList)
                {
                    var aspect = JsonSerializer.Deserialize<equipTypeAspect>(aspectJson);
                    Aspects.Add(aspect);
                    if (!AspectMap.ContainsKey(aspect.Id))
                        AspectMap.Add(aspect.Id, aspect);
                }
            }
        }
    }

    /// <summary>item 配置</summary>
    public class itemConfig
    {
        public object number { get; set; }
        public object @string { get; set; }
    }

    /// <summary>item 数据项</summary>
    public class itemAspect
    {
        public object number { get; set; }
        public string language => LanguageManager.GetText(_languageKey);
        [JsonPropertyName("language")] private string _languageKey;
        public object @string { get; set; }
    }

    /// <summary>item</summary>
    public static class item
    {
        public static itemConfig Config { get; private set; } = new itemConfig();
        public static List<itemAspect> Aspects { get; private set; } = new List<itemAspect>();
        public static Dictionary<int, itemAspect> AspectMap { get; private set; } = new Dictionary<int, itemAspect>();

        public static itemAspect FindById(int id)
        {
            return AspectMap.TryGetValue(id, out var aspect) ? aspect : null;
        }

        public static void Load(string json)
        {
            var data = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            if (data.TryGetValue("setConfig", out var config))
            {
                Config = JsonSerializer.Deserialize<itemConfig>(config.ToString());
            }
            if (data.TryGetValue("setAspect", out var aspects))
            {
                Aspects = new List<itemAspect>();
                AspectMap = new Dictionary<int, itemAspect>();
                var aspectList = JsonSerializer.Deserialize<List<string>>(aspects.ToString());
                foreach (var aspectJson in aspectList)
                {
                    var aspect = JsonSerializer.Deserialize<itemAspect>(aspectJson);
                    Aspects.Add(aspect);
                    if (!AspectMap.ContainsKey(aspect.Id))
                        AspectMap.Add(aspect.Id, aspect);
                }
            }
        }
    }

    /// <summary>equip 配置</summary>
    public class equipConfig
    {
        public object number { get; set; }
        public object number_1 { get; set; }
    }

    /// <summary>equip 数据项</summary>
    public class equipAspect
    {
        public object number { get; set; }
        public object number_1 { get; set; }
    }

    /// <summary>equip</summary>
    public static class equip
    {
        public static equipConfig Config { get; private set; } = new equipConfig();
        public static List<equipAspect> Aspects { get; private set; } = new List<equipAspect>();
        public static Dictionary<int, equipAspect> AspectMap { get; private set; } = new Dictionary<int, equipAspect>();

        public static equipAspect FindById(int id)
        {
            return AspectMap.TryGetValue(id, out var aspect) ? aspect : null;
        }

        public static void Load(string json)
        {
            var data = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            if (data.TryGetValue("setConfig", out var config))
            {
                Config = JsonSerializer.Deserialize<equipConfig>(config.ToString());
            }
            if (data.TryGetValue("setAspect", out var aspects))
            {
                Aspects = new List<equipAspect>();
                AspectMap = new Dictionary<int, equipAspect>();
                var aspectList = JsonSerializer.Deserialize<List<string>>(aspects.ToString());
                foreach (var aspectJson in aspectList)
                {
                    var aspect = JsonSerializer.Deserialize<equipAspect>(aspectJson);
                    Aspects.Add(aspect);
                    if (!AspectMap.ContainsKey(aspect.Id))
                        AspectMap.Add(aspect.Id, aspect);
                }
            }
        }
    }

}
