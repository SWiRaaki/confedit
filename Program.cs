using System;
using System.IO;

class Program
{
    static void Main()
    {
        TestConfig("config.json", @"{""name"": ""Alice"", ""age"": 30}");
        TestConfig("config.xml", @"<person><name>Alice</name><age>30</age></person>");
        TestConfig("config.yaml", @"name: Alice
age: 30");
        TestConfig("config.ini", "[Person]\nname=\"Alice\"\nage=30");
        TestConfig("config.toml", @"name = 'Alice'
age = 30");
    }

    static void TestConfig(string path, string content)
    {
        File.WriteAllText(path, content);
        Console.WriteLine($"Testing {path}...");

        var parsed = ConfigFileHandler.ParseFile(path);
        if (parsed is null)
        {
            Console.WriteLine("File not found or empty!");
            return;
        }

        Console.WriteLine($"Parsed as {parsed.Format}, type: {parsed.Data.GetType()}");

        string serialized = ConfigFileHandler.SerializeFile(parsed);
        Console.WriteLine("Serialized output:");
        Console.WriteLine(serialized);
        Console.WriteLine(new string('-', 40));
    }
}
