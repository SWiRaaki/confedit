using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace ConfigTreeTest
{
    class Program
    {
        static void Main()
        {
            string inputFile = @"C:\Users\lmalagon10635\OneDrive - COSMO CONSULT AG\Desktop\confedit\input.json";
            if (!File.Exists(inputFile))
            {
                Console.WriteLine($"File not found: {inputFile}");
                return;
            }

            string json = File.ReadAllText(inputFile);

            var options = new ConfigTreeConverter.Options
            {
                UseLowercaseItems = true
            };

            // Convert Input → Tree
            string treeJson = ConfigTreeConverter.ConvertJsonToTree(json, options);
            string outputFile = "output.json";
            File.WriteAllText(outputFile, treeJson);
            Console.WriteLine($"Conversion done! See {outputFile}");

            // Convert Tree → Original
            string restoredJson = ConfigTreeConverter.ConvertTreeToJson(treeJson, options);
            string restoredFile = "restored.json";
            File.WriteAllText(restoredFile, restoredJson);
            Console.WriteLine($"Restoration done! See {restoredFile}");
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

        // FORWARD: Input → Tree
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

        // BACKWARD: Tree → Input
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
            // Handle metadata keys (% back)
            var dict = new Dictionary<string, object>();
            foreach (var kv in item.Meta)
            {
                dict["%" + kv.Key] = kv.Value;
            }

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
    }
}