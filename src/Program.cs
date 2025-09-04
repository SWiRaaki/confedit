using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Xml.Linq;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

namespace ConfigTreeTest
{
    class Program
    {
        static void Main()
        {
            string inputFile = @"C:\Users\lmalagon10635\OneDrive - COSMO CONSULT AG\Desktop\confedit\input.json";
            string xmlFile = @"C:\Users\lmalagon10635\OneDrive - COSMO CONSULT AG\Desktop\confedit\input.xml";
            string yamlFile = @"C:\Users\lmalagon10635\OneDrive - COSMO CONSULT AG\Desktop\confedit\input.yaml";

            var options = new ConfigTreeConverter.Options
            {
                UseLowercaseItems = true,
                LiftPercentKeysToMeta = true
            };

            // JSON
            if (File.Exists(inputJsonFile))
            {
                var json = File.ReadAllText(inputJsonFile);
                var treeJson = ConfigTreeConverter.ConvertJsonToTree(json, options);
                File.WriteAllText("output.json", treeJson);
                Console.WriteLine("JSON → Tree done (output.json)");

                var restoredJson = ConfigTreeConverter.ConvertTreeToJson(treeJson, options);
                File.WriteAllText("restored.json", restoredJson);
                Console.WriteLine("Tree → JSON restored (restored.json)");
            }

            // XML
            if (File.Exists(inputXmlFile))
            {
                var xml = File.ReadAllText(inputXmlFile);
                var treeJson = ConfigTreeConverter.ConvertXmlToTree(xml, options);
                File.WriteAllText("output_from_xml.json", treeJson);
                Console.WriteLine("XML → Tree done (output_from_xml.json)");

                var restoredXml = ConfigTreeConverter.ConvertTreeToXml(treeJson, options);
                File.WriteAllText("restored.xml", restoredXml);
                Console.WriteLine("Tree → XML restored (restored.xml)");
            }

            // YAML
            if (File.Exists(inputYamlFile))
            {
                var yaml = File.ReadAllText(inputYamlFile);
                var treeJson = ConfigTreeConverter.ConvertYamlToTree(yaml, options);
                File.WriteAllText("output_from_yaml.json", treeJson);
                Console.WriteLine("YAML → Tree done (output_from_yaml.json)");

                var restoredYaml = ConfigTreeConverter.ConvertTreeToYaml(treeJson, options);
                File.WriteAllText("restored.yaml", restoredYaml);
                Console.WriteLine("Tree → YAML restored (restored.yaml)");
            }

            Console.WriteLine("All done.");
        }
    }

    public static class ConfigTreeConverter
    {
        public sealed class Options
        {
            public bool UseLowercaseItems { get; set; } = false;
            public bool LiftPercentKeysToMeta { get; set; } = true;
        }

        public sealed class Root
        {
            public string Config { get; set; } = "";
            public string Uid { get; set; } = Guid.NewGuid().ToString();
            public List<Item>? Items { get; set; }
            public List<Item>? items { get; set; }
        }

        public sealed class Item
        {
            public string Name { get; set; } = "";
            public string Value { get; set; } = "";
            public string Type { get; set; } = "category";
            public List<Item> Children { get; set; } = new();
            public Dictionary<string, object> Meta { get; set; } = new();
        }

        public static string ConvertJsonToTree(string json, Options? options = null)
        {
            options ??= new Options();

            using var doc = JsonDocument.Parse(json);
            var root = new Root { Uid = Guid.NewGuid().ToString() };

            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                throw new ArgumentException("Top-level JSON must be an object.");

            string configName;
            JsonElement configElement;

            if (doc.RootElement.EnumerateObject().Count() == 1)
            {
                var prop = doc.RootElement.EnumerateObject().First();
                configName = prop.Name;
                configElement = prop.Value;
            }
            else
            {
                configName = "root";
                configElement = doc.RootElement;
            }

            root.Config = configName;
            var configItem = new Item { Name = configName, Type = "category", Value = "" };
            BuildChildren(configElement, configItem, options);

            var rootItems = new List<Item> { configItem };
            if (options.UseLowercaseItems)
                root.items = rootItems;
            else
                root.Items = rootItems;

            return JsonSerializer.Serialize(root, new JsonSerializerOptions { WriteIndented = true });
        }

        private static void BuildChildren(JsonElement element, Item parent, Options options)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    var meta = new Dictionary<string, object>();
                    var normalProps = new List<JsonProperty>();
                    foreach (var p in element.EnumerateObject())
                    {
                        if (options.LiftPercentKeysToMeta && p.Name.StartsWith("%"))
                            meta[p.Name.Substring(1)] = p.Value.GetString() ?? "";
                        else
                            normalProps.Add(p);
                    }
                    foreach (var kv in meta) parent.Meta[kv.Key] = kv.Value;
                    foreach (var p in normalProps)
                        parent.Children.Add(CreateNodeFromElement(p.Name, p.Value, options));
                    break;

                case JsonValueKind.Array:
                    parent.Type = "list";
                    foreach (var (item, idx) in element.EnumerateArray().Select((e, i) => (e, i)))
                        parent.Children.Add(CreateNodeFromElement(idx.ToString(), item, options));
                    break;
            }
        }

        private static Item CreateNodeFromElement(string name, JsonElement el, Options options)
        {
            var node = new Item { Name = name };
            switch (el.ValueKind)
            {
                case JsonValueKind.Object:
                    node.Type = "category"; node.Value = ""; BuildChildren(el, node, options); break;
                case JsonValueKind.Array:
                    node.Type = "list"; node.Value = ""; BuildChildren(el, node, options); break;
                case JsonValueKind.String:
                    node.Type = "string"; node.Value = el.GetString() ?? ""; break;
                case JsonValueKind.Number:
                    if (el.TryGetInt64(out var l)) { node.Type = "integer"; node.Value = l.ToString(); }
                    else { node.Type = "float"; node.Value = el.GetDouble().ToString(CultureInfo.InvariantCulture); }
                    break;
                case JsonValueKind.True:
                case JsonValueKind.False:
                    node.Type = "bool"; node.Value = (el.ValueKind == JsonValueKind.True).ToString().ToLower(); break;
                case JsonValueKind.Null:
                    node.Type = "null"; node.Value = ""; break;
            }
            return node;
        }

        public static string ConvertTreeToJson(string treeJson, Options? options = null)
        {
            options ??= new Options();

            var root = JsonSerializer.Deserialize<Root>(treeJson);
            if (root == null)
                throw new ArgumentException("Invalid tree JSON");

            var rootItems = root.Items ?? root.items;
            if (rootItems == null || rootItems.Count == 0)
                throw new ArgumentException("Tree JSON does not contain items");

            var configItem = rootItems[0];
            var reconstructed = new Dictionary<string, object>
            {
                [root.Config] = BuildElementFromItem(configItem)
            };

            return JsonSerializer.Serialize(reconstructed, new JsonSerializerOptions { WriteIndented = true });
        }

        private static object BuildElementFromItem(Item item)
        {
            var dict = new Dictionary<string, object>();
            foreach (var kv in item.Meta)
                dict["%" + kv.Key] = kv.Value;

            switch (item.Type)
            {
                case "category":
                    foreach (var child in item.Children)
                        dict[child.Name] = BuildElementFromItem(child);
                    return dict;

                case "list":
                    var list = new List<object>();
                    foreach (var child in item.Children.OrderBy(c => int.Parse(c.Name)))
                        list.Add(BuildElementFromItem(child));
                    return list;

                case "string":
                    return item.Value;

                case "integer":
                    return Int64.TryParse(item.Value, out var l) ? l : 0;

                case "float":
                    return Double.TryParse(item.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0.0;

                case "bool":
                    return item.Value == "true";

                case "null":
                    return null!;

                default:
                    return item.Value;
            }
        }

        public static string ConvertXmlToTree(string xml, Options? options = null)
        {
            options ??= new Options();
            var doc = XDocument.Parse(xml);

            var rootElement = doc.Root ?? throw new ArgumentException("XML has no root");
            var root = new Root { Config = rootElement.Name.LocalName, Uid = Guid.NewGuid().ToString() };

            var rootItem = CreateNodeFromXElement(rootElement, options);

            var rootItems = new List<Item> { rootItem };
            if (options.UseLowercaseItems)
                root.items = rootItems;
            else
                root.Items = rootItems;

            return JsonSerializer.Serialize(root, new JsonSerializerOptions { WriteIndented = true });
        }

        private static Item CreateNodeFromXElement(XElement el, Options options)
        {
            var node = new Item { Name = el.Name.LocalName, Type = "category", Value = "" };

            foreach (var attr in el.Attributes())
                node.Meta[attr.Name.LocalName] = attr.Value;

            foreach (var child in el.Elements())
                node.Children.Add(CreateNodeFromXElement(child, options));

            if (!el.HasElements && !string.IsNullOrWhiteSpace(el.Value))
            {
                node.Type = "string";
                node.Value = el.Value;
            }

            return node;
        }

        public static string ConvertTreeToXml(string treeJson, Options? options = null)
        {
            options ??= new Options();

            var root = JsonSerializer.Deserialize<Root>(treeJson);
            if (root == null)
                throw new ArgumentException("Invalid tree JSON");

            var rootItems = root.Items ?? root.items;
            if (rootItems == null || rootItems.Count == 0)
                throw new ArgumentException("Tree JSON does not contain items");

            var configItem = rootItems[0];
            var xRoot = BuildXElementFromItem(configItem);

            var doc = new XDocument(xRoot);
            return doc.ToString();
        }

        private static XElement BuildXElementFromItem(Item item)
        {
            var el = new XElement(item.Name);

            foreach (var kv in item.Meta)
                el.SetAttributeValue(kv.Key, kv.Value);

            foreach (var child in item.Children)
                el.Add(BuildXElementFromItem(child));

            if (item.Type != "category" && item.Type != "list" && item.Children.Count == 0)
                el.Value = item.Value;

            return el;
        }

        public static string ConvertYamlToTree(string yaml, Options? options = null)
        {
            options ??= new Options();

            var yamlStream = new YamlStream();
            yamlStream.Load(new StringReader(yaml));

            if (yamlStream.Documents.Count == 0)
                throw new ArgumentException("YAML has no documents");

            var rootNode = yamlStream.Documents[0].RootNode;

            string configName;
            YamlNode configNode;
            if (rootNode is YamlMappingNode topMap && topMap.Children.Count == 1)
            {
                var first = topMap.Children.First();
                var keyNode = first.Key as YamlScalarNode;
                if (keyNode != null)
                {
                    configName = keyNode.Value ?? "root";
                    configNode = first.Value;
                }
                else
                {
                    configName = "root";
                    configNode = rootNode;
                }
            }
            else
            {
                configName = "root";
                configNode = rootNode;
            }

            var root = new Root { Config = configName, Uid = Guid.NewGuid().ToString() };
            var rootItem = CreateNodeFromYamlNode(configName, configNode, options);

            var rootItems = new List<Item> { rootItem };
            if (options.UseLowercaseItems)
                root.items = rootItems;
            else
                root.Items = rootItems;

            return JsonSerializer.Serialize(root, new JsonSerializerOptions { WriteIndented = true });
        }

        private static Item CreateNodeFromYamlNode(string name, YamlNode node, Options options)
        {
            var item = new Item { Name = name };

            switch (node)
            {
                case YamlMappingNode map:
                    item.Type = "category";
                    foreach (var kv in map.Children)
                    {
                        var key = kv.Key.ToString();
                        var val = kv.Value;

                        if (options.LiftPercentKeysToMeta && key.StartsWith("%"))
                        {
                            var metaKey = key.Substring(1);
                            if (val is YamlScalarNode s)
                                item.Meta[metaKey] = s.Value ?? "";
                            else
                                item.Meta[metaKey] = ConvertYamlNodeToObject(val);
                        }
                        else
                        {
                            item.Children.Add(CreateNodeFromYamlNode(key, val, options));
                        }
                    }
                    break;

                case YamlSequenceNode seq:
                    item.Type = "list";
                    int idx = 0;
                    foreach (var child in seq.Children)
                    {
                        item.Children.Add(CreateNodeFromYamlNode(idx.ToString(), child, options));
                        idx++;
                    }
                    break;

                case YamlScalarNode scalar:
                    var scalarVal = scalar.Value;
                    var detected = DetectScalarType(scalarVal);
                    item.Type = detected;
                    if (detected == "float")
                        item.Value = double.TryParse(scalarVal, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d.ToString(CultureInfo.InvariantCulture) : scalarVal ?? "";
                    else if (detected == "integer")
                        item.Value = long.TryParse(scalarVal, out var l) ? l.ToString() : scalarVal ?? "";
                    else if (detected == "bool")
                        item.Value = (scalarVal?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false).ToString().ToLower();
                    else if (detected == "null")
                        item.Value = "";
                    else
                        item.Value = scalarVal ?? "";
                    break;
            }

            return item;
        }

        private static object? ConvertYamlNodeToObject(YamlNode node)
        {
            switch (node)
            {
                case YamlScalarNode s:
                    if (s.Value == null) return null;
                    if (long.TryParse(s.Value, out var l)) return l;
                    if (double.TryParse(s.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var d)) return d;
                    if (s.Value.Equals("true", StringComparison.OrdinalIgnoreCase)) return true;
                    if (s.Value.Equals("false", StringComparison.OrdinalIgnoreCase)) return false;
                    if (s.Value.Equals("null", StringComparison.OrdinalIgnoreCase) || s.Value == "~") return null;
                    return s.Value;

                case YamlMappingNode m:
                    var dict = new Dictionary<string, object?>();
                    foreach (var kv in m.Children)
                        dict[kv.Key.ToString()] = ConvertYamlNodeToObject(kv.Value);
                    return dict;

                case YamlSequenceNode seq:
                    var list = new List<object?>();
                    foreach (var child in seq.Children)
                        list.Add(ConvertYamlNodeToObject(child));
                    return list;

                default:
                    return null;
            }
        }

        private static string DetectScalarType(string? value)
        {
            if (value == null) return "null";
            if (value.Equals("null", StringComparison.OrdinalIgnoreCase) || value == "~") return "null";
            if (long.TryParse(value, out _)) return "integer";
            if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out _)) return "float";
            if (value.Equals("true", StringComparison.OrdinalIgnoreCase) || value.Equals("false", StringComparison.OrdinalIgnoreCase))
                return "bool";
            return "string";
        }

        public static string ConvertTreeToYaml(string treeJson, Options? options = null)
        {
            options ??= new Options();

            var root = JsonSerializer.Deserialize<Root>(treeJson);
            if (root == null)
                throw new ArgumentException("Invalid tree JSON");

            var rootItems = root.Items ?? root.items;
            if (rootItems == null || rootItems.Count == 0)
                throw new ArgumentException("Tree JSON does not contain items");

            var configItem = rootItems[0];

            var dict = new Dictionary<string, object?>
            {
                [root.Config] = BuildYamlElementFromItem(configItem)
            };

            var serializer = new SerializerBuilder().Build();
            return serializer.Serialize(dict);
        }

        private static object? BuildYamlElementFromItem(Item item)
        {
            switch (item.Type)
            {
                case "category":
                    var dict = new Dictionary<string, object?>();
                    foreach (var kv in item.Meta)
                        dict["%" + kv.Key] = kv.Value;
                    foreach (var child in item.Children)
                        dict[child.Name] = BuildYamlElementFromItem(child);
                    return dict;

                case "list":
                    var list = new List<object?>();
                    foreach (var child in item.Children.OrderBy(c => int.Parse(c.Name)))
                        list.Add(BuildYamlElementFromItem(child));
                    return list;

                case "string":
                    return item.Value;

                case "integer":
                    return long.TryParse(item.Value, out var l) ? l : 0;

                case "float":
                    return double.TryParse(item.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0.0;

                case "bool":
                    return item.Value == "true";

                case "null":
                    return null;

                default:
                    return item.Value;
            }
        }
    }
}
