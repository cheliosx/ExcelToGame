using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Game.Config
{
    /// <summary>gameArg 閰嶇疆</summary>
    public class gameArgConfig
    {
        public int initHp { get; set; }
        public List<object> initItems { get; set; }
        public string gamePath { get; set; }
    }

    /// <summary>gameArg</summary>
    public static class gameArg
    {
        public static gameArgConfig Config { get; private set; } = new gameArgConfig();

        public static void Load(string json)
        {
            Config = JsonSerializer.Deserialize<gameArgConfig>(json);
        }
    }

    /// <summary>roleLevel 閰嶇疆</summary>
    public class roleLevelConfig
    {
        public int lv { get; set; }
        public int hp { get; set; }
        public int def { get; set; }
    }

    /// <summary>roleLevel 鏁版嵁椤?/summary>
    public class roleLevelAspect
    {
        public int lv { get; set; }
        public int hp { get; set; }
        public int def { get; set; }
    }

    /// <summary>roleLevel</summary>
    public static class roleLevel
    {
        public static roleLevelConfig Config { get; private set; } = new roleLevelConfig();
        public static List<roleLevelAspect> Aspects { get; private set; } = new List<roleLevelAspect>();
        public static Dictionary<int, roleLevelAspect> AspectMap { get; private set; } = new Dictionary<int, roleLevelAspect>();

        public static roleLevelAspect FindById(int id)
        {
            return AspectMap.TryGetValue(id, out var aspect) ? aspect : null;
        }

        public static void Load(string json)
        {
            var data = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            if (data.TryGetValue("setConfig", out var config))
            {
                Config = JsonSerializer.Deserialize<roleLevelConfig>(config.ToString());
            }
            if (data.TryGetValue("setAspect", out var aspects))
            {
                Aspects = new List<roleLevelAspect>();
                AspectMap = new Dictionary<int, roleLevelAspect>();
                var aspectList = JsonSerializer.Deserialize<List<string>>(aspects.ToString());
                foreach (var aspectJson in aspectList)
                {
                    var aspect = JsonSerializer.Deserialize<roleLevelAspect>(aspectJson);
                    Aspects.Add(aspect);
                    if (!AspectMap.ContainsKey(aspect.id))
                        AspectMap.Add(aspect.id, aspect);
                }
            }
        }
    }

    /// <summary>item 閰嶇疆</summary>
    public class itemConfig
    {
        public int id { get; set; }
        public string icon { get; set; }
        public object type { get; set; }
        public object gType { get; set; }
    }

    /// <summary>item 鏁版嵁椤?/summary>
    public class itemAspect
    {
        public int id { get; set; }
        public string name => LanguageManager.GetText(_nameKey);
        [JsonPropertyName("name")] private string _nameKey;
        public string icon { get; set; }
        public object type { get; set; }
        public object gType { get; set; }
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
                    if (!AspectMap.ContainsKey(aspect.id))
                        AspectMap.Add(aspect.id, aspect);
                }
            }
        }
    }

    /// <summary>equipType 閰嶇疆</summary>
    public class equipTypeConfig
    {
        public object key { get; set; }
        public object value { get; set; }
    }

    /// <summary>equipType 鏁版嵁椤?/summary>
    public class equipTypeAspect
    {
        public object key { get; set; }
        public object value { get; set; }
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
                    if (!AspectMap.ContainsKey(aspect.id))
                        AspectMap.Add(aspect.id, aspect);
                }
            }
        }
    }

    /// <summary>growthData 閰嶇疆</summary>
    public class growthDataConfig
    {
        public object id { get; set; }
        public int lv { get; set; }
        public int hp { get; set; }
        public int def { get; set; }
    }

    /// <summary>growthData 鏁版嵁椤?/summary>
    public class growthDataAspect
    {
        public object id { get; set; }
        public int lv { get; set; }
        public int hp { get; set; }
        public int def { get; set; }
    }

    /// <summary>growthData</summary>
    public static class growthData
    {
        public static growthDataConfig Config { get; private set; } = new growthDataConfig();
        public static List<growthDataAspect> Aspects { get; private set; } = new List<growthDataAspect>();
        public static Dictionary<int, growthDataAspect> AspectMap { get; private set; } = new Dictionary<int, growthDataAspect>();

        public static growthDataAspect FindById(int id)
        {
            return AspectMap.TryGetValue(id, out var aspect) ? aspect : null;
        }

        public static void Load(string json)
        {
            var data = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            if (data.TryGetValue("setConfig", out var config))
            {
                Config = JsonSerializer.Deserialize<growthDataConfig>(config.ToString());
            }
            if (data.TryGetValue("setAspect", out var aspects))
            {
                Aspects = new List<growthDataAspect>();
                AspectMap = new Dictionary<int, growthDataAspect>();
                var aspectList = JsonSerializer.Deserialize<List<string>>(aspects.ToString());
                foreach (var aspectJson in aspectList)
                {
                    var aspect = JsonSerializer.Deserialize<growthDataAspect>(aspectJson);
                    Aspects.Add(aspect);
                    if (!AspectMap.ContainsKey(aspect.id))
                        AspectMap.Add(aspect.id, aspect);
                }
            }
        }
    }

    /// <summary>growthType 閰嶇疆</summary>
    public class growthTypeConfig
    {
        public object key { get; set; }
        public object value { get; set; }
    }

    /// <summary>growthType 鏁版嵁椤?/summary>
    public class growthTypeAspect
    {
        public object key { get; set; }
        public object value { get; set; }
    }

    /// <summary>growthType</summary>
    public static class growthType
    {
        public static growthTypeConfig Config { get; private set; } = new growthTypeConfig();
        public static List<growthTypeAspect> Aspects { get; private set; } = new List<growthTypeAspect>();
        public static Dictionary<int, growthTypeAspect> AspectMap { get; private set; } = new Dictionary<int, growthTypeAspect>();

        public static growthTypeAspect FindById(int id)
        {
            return AspectMap.TryGetValue(id, out var aspect) ? aspect : null;
        }

        public static void Load(string json)
        {
            var data = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            if (data.TryGetValue("setConfig", out var config))
            {
                Config = JsonSerializer.Deserialize<growthTypeConfig>(config.ToString());
            }
            if (data.TryGetValue("setAspect", out var aspects))
            {
                Aspects = new List<growthTypeAspect>();
                AspectMap = new Dictionary<int, growthTypeAspect>();
                var aspectList = JsonSerializer.Deserialize<List<string>>(aspects.ToString());
                foreach (var aspectJson in aspectList)
                {
                    var aspect = JsonSerializer.Deserialize<growthTypeAspect>(aspectJson);
                    Aspects.Add(aspect);
                    if (!AspectMap.ContainsKey(aspect.id))
                        AspectMap.Add(aspect.id, aspect);
                }
            }
        }
    }

}
