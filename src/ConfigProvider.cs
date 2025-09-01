using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Xml.Serialization;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Tomlyn;
using Tomlyn.Model;
using SharpConfig;

namespace ConfigSystem
{
    public class ConfigTree
    {
        public string Configuration { get; set; }
        public Guid UID { get; set; }
        public List<ConfigNode> Items { get; set; } = new();
    }

    public class ConfigNode
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public string Type { get; set; }
        public List<ConfigNode> Children { get; set; } = new();
        public Dictionary<string, string> Meta { get; set; } = new();
    }

    public class Result
    {
        public int Code { get; set; }
        public string Message { get; set; }
    }

    public class TResult<T> : Result
    {
        public T? Data { get; set; }
    }

    public interface IConfigProvider
    {
        TResult<ConfigTree> Load(string file);
        Result Save(string file, ConfigTree data);
    }

    public class JsonConfigProvider : IConfigProvider
    {
        public TResult<ConfigTree> Load(string file)
        {
            try
            {
                var json = File.ReadAllText(file);
                var tree = JsonSerializer.Deserialize<ConfigTree>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return new TResult<ConfigTree> { Code = 0, Message = "OK", Data = tree };
            }
            catch (Exception ex)
            {
                return new TResult<ConfigTree> { Code = -1, Message = ex.Message };
            }
        }

        public Result Save(string file, ConfigTree data)
        {
            try
            {
                var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(file, json);
                return new Result { Code = 0, Message = "Saved successfully" };
            }
            catch (Exception ex)
            {
                return new Result { Code = -1, Message = ex.Message };
            }
        }
    }

    public class XmlConfigProvider : IConfigProvider
    {
        public TResult<ConfigTree> Load(string file)
        {
            try
            {
                using var stream = new FileStream(file, FileMode.Open);
                var serializer = new XmlSerializer(typeof(ConfigTree));
                var tree = (ConfigTree)serializer.Deserialize(stream);
                return new TResult<ConfigTree> { Code = 0, Message = "OK", Data = tree };
            }
            catch (Exception ex)
            {
                return new TResult<ConfigTree> { Code = -1, Message = ex.Message };
            }
        }

        public Result Save(string file, ConfigTree data)
        {
            try
            {
                using var stream = new FileStream(file, FileMode.Create);
                var serializer = new XmlSerializer(typeof(ConfigTree));
                serializer.Serialize(stream, data);
                return new Result { Code = 0, Message = "Saved successfully" };
            }
            catch (Exception ex)
            {
                return new Result { Code = -1, Message = ex.Message };
            }
        }
    }

    public class YamlConfigProvider : IConfigProvider
    {
        private readonly IDeserializer deserializer;
        private readonly ISerializer serializer;

        public YamlConfigProvider()
        {
            deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
        }

        public TResult<ConfigTree> Load(string file)
        {
            try
            {
                var yaml = File.ReadAllText(file);
                var tree = deserializer.Deserialize<ConfigTree>(yaml);
                return new TResult<ConfigTree> { Code = 0, Message = "OK", Data = tree };
            }
            catch (Exception ex)
            {
                return new TResult<ConfigTree> { Code = -1, Message = ex.Message };
            }
        }

        public Result Save(string file, ConfigTree data)
        {
            try
            {
                var yaml = serializer.Serialize(data);
                File.WriteAllText(file, yaml);
                return new Result { Code = 0, Message = "Saved successfully" };
            }
            catch (Exception ex)
            {
                return new Result { Code = -1, Message = ex.Message };
            }
        }
    }
    public class TomlConfigProvider : IConfigProvider
    {
        public TResult<ConfigTree> Load(string file)
        {
            try
            {
                var text = File.ReadAllText(file);
                var model = Toml.ToModel(text);

                // Toml <-> ConfigTree Mapping musst du evtl. noch anpassen
                var tree = TomlToConfigTree(model);

                return new TResult<ConfigTree> { Code = 0, Message = "OK", Data = tree };
            }
            catch (Exception ex)
            {
                return new TResult<ConfigTree> { Code = -1, Message = ex.Message };
            }
        }

        public Result Save(string file, ConfigTree data)
        {
            try
            {
                var model = ConfigTreeToToml(data);
                var toml = Toml.FromModel(model);
                File.WriteAllText(file, toml);
                return new Result { Code = 0, Message = "Saved successfully" };
            }
            catch (Exception ex)
            {
                return new Result { Code = -1, Message = ex.Message };
            }
        }

        private ConfigTree TomlToConfigTree(TomlTable table)
        {
            var tree = new ConfigTree
            {
                Configuration = "toml",
                UID = Guid.NewGuid()
            };

            foreach (var kv in table)
            {
                tree.Items.Add(new ConfigNode
                {
                    Name = kv.Key,
                    Value = kv.Value?.ToString(),
                    Type = kv.Value?.GetType().Name ?? "string"
                });
            }

            return tree;
        }

        private TomlTable ConfigTreeToToml(ConfigTree tree)
        {
            var table = new TomlTable();

            foreach (var node in tree.Items)
            {
                table[node.Name] = node.Value;
            }

            return table;
        }
    }

    public class IniConfigProvider : IConfigProvider
    {
        public TResult<ConfigTree> Load(string file)
        {
            try
            {
                var cfg = Configuration.LoadFromFile(file);
                var tree = new ConfigTree
                {
                    Configuration = "ini",
                    UID = Guid.NewGuid()
                };

                foreach (var section in cfg)
                {
                    var node = new ConfigNode { Name = section.Name, Type = "section" };

                    foreach (var setting in section)
                    {
                        node.Children.Add(new ConfigNode
                        {
                            Name = setting.Name,
                            Value = setting.StringValue,
                            Type = "string"
                        });
                    }

                    tree.Items.Add(node);
                }

                return new TResult<ConfigTree> { Code = 0, Message = "OK", Data = tree };
            }
            catch (Exception ex)
            {
                return new TResult<ConfigTree> { Code = -1, Message = ex.Message };
            }
        }

        public Result Save(string file, ConfigTree data)
        {
            try
            {
                var cfg = new Configuration();

                foreach (var sectionNode in data.Items)
                {
                    var section = new Section(sectionNode.Name);

                    foreach (var child in sectionNode.Children)
                    {
                        section.Add(new Setting(child.Name, child.Value));
                    }

                    cfg.Add(section);
                }

                cfg.SaveToFile(file);
                return new Result { Code = 0, Message = "Saved successfully" };
            }
            catch (Exception ex)
            {
                return new Result { Code = -1, Message = ex.Message };
            }
        }
    }
}