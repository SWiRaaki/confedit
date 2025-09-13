using System.Xml.Linq;
using Newtonsoft.Json;

internal class ConfigTree {
	[JsonProperty("config")]
	internal string Configuration { get; set; } = "";

	[JsonProperty("uid")]
	internal string UID { get; set; } = "00000000000000000000000000000000";

	[JsonProperty("items")]
	internal List<ConfigNode> Items { get; set; } = new();
}

internal class ConfigNode {
	[JsonProperty("name")]
	internal string Name { get; set; } = "";

	[JsonProperty("value")]
	internal string Value { get; set; } = "";

	[JsonProperty("type")]
	internal string Type { get; set; } = "";

	[JsonProperty("children")]
	internal List<ConfigNode> Children { get; set; } = new();

	[JsonProperty("meta")]
	internal Dictionary<string, string> Meta { get; set; } = new();
}

internal class Result {
	internal int Code { get; set; } = 0;
	internal string Message { get; set; } = "";
	public static implicit operator bool( Result result) => result.Code == 0;
}

internal class Result<T> : Result {
	internal T? Data { get; set; }
}

internal interface IConfigProvider {
	Result<ConfigTree> Load( string file );
	Result Save( string file, ConfigTree data );
}
