using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal struct StdUser {
	[JsonProperty("uid")]
	internal string UID;

	[JsonProperty("name")]
	internal string Name;

	[JsonProperty("abbreviation")]
	internal string Abbreviation;

	[JsonProperty("security")]
	internal string Security;
}
