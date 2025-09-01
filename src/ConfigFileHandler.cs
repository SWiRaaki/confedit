using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using IniParser;
using IniParser.Model;
using System.Xml.Linq;
using Tomlyn;               
using Tomlyn.Model;
using Newtonsoft.Json.Linq;
using SQLitePCL;

public class ParsedConfig
{
    public string Format { get; set; }
    public object Data { get; set; }
}

public class Data
{
    [JsonProperty("config")]
    public string Config { get; set; }

    [JsonProperty("uid")]
    public string Uid { get; set; }

    [JsonProperty("items")]
    public List<Item> Items { get; set; }
}

public class Item
{
    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("value")]
    public string Value { get; set; }

    [JsonProperty("type")]

    public string Type { get; set; }

    [JsonProperty("children")]
    public List<Item> Children { get; set; }

    [JsonProperty("meta")]
    public Dictionary<string, object> Meta { get; set; }
}

public static class ConfigFileHandler
{
    public static string? ReadFile(string path)
    {
        if (!File.Exists(path))
            return null;
        return File.ReadAllText(path, Encoding.UTF8);
    }

    public static void WriteFile(string path, string content)
    {
        File.WriteAllText(path, content, Encoding.UTF8);
    }

    public static ParsedConfig? ParseFile(string path)
    {
        string? content = ReadFile(path);
        if (content is null)
            return null; // File not found

        string ext = Path.GetExtension(path).ToLower();

        switch (ext)
        {
            case ".json":
                return new ParsedConfig { Format = "json", Data = JsonConvert.DeserializeObject(content)! };
            case ".xml":
                return new ParsedConfig { Format = "xml", Data = XDocument.Parse(content) };
            case ".yaml":
            case ".yml":
                var yamlDeserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance).Build();
                return new ParsedConfig { Format = "yaml", Data = yamlDeserializer.Deserialize<object>(content)! };
            case ".ini":
                var iniParser = new FileIniDataParser();
                return new ParsedConfig { Format = "ini", Data = iniParser.ReadFile(path) };
            case ".toml":
                var tomlModel = Toml.ToModel(content);  // returns a TomlTable
                return new ParsedConfig { Format = "toml", Data = tomlModel };
            default:
                throw new NotSupportedException($"Unsupported format: {ext}");
        }
    }

    public static string SerializeFile(ParsedConfig parsed)
    {
        if (parsed == null)
            throw new ArgumentNullException(nameof(parsed));

        switch (parsed.Format.ToLower())
        {
            case "json":
                return JsonConvert.SerializeObject(parsed.Data, Formatting.Indented);

            case "xml":
                if (parsed.Data is XDocument doc)
                    return doc.ToString();
                if (parsed.Data is string xmlString)
                    return xmlString;
                throw new InvalidCastException("Data is not a valid XML document.");

            case "yaml":
                var yamlSerializer = new SerializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance).Build();
                return yamlSerializer.Serialize(parsed.Data);

            case "ini":
                if (parsed.Data is IniData iniData)
                    return iniData.ToString();
                throw new InvalidCastException("Data is not a valid INI structure.");

            case "toml":
                if (parsed.Data is TomlTable tomlTable)
                    return Toml.FromModel(tomlTable);
                throw new InvalidCastException("Data is not a valid TOML structure.");

            default:
                throw new NotSupportedException($"Unsupported format: {parsed.Format}");
        }
    }
}
