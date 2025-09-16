using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

#pragma warning disable 0649

internal struct StdDbver {
	[JsonProperty("serial")]
	internal long Serial;

	[JsonProperty("major")]
	internal long Major;

	[JsonProperty("minor")]
	internal long Minor;

	[JsonProperty("patch")]
	internal long Patch;

	[JsonProperty("script_version")]
	internal string ScriptVersion;

	[JsonProperty("run_at")]
	internal DateTimeOffset ExecutedAt;
}

internal struct StdUser {
	[JsonProperty("serial")]
	internal long Serial;

	[JsonProperty("uid")]
	internal string UID;

	[JsonProperty("name")]
	internal string Name;

	[JsonProperty("abbreviation")]
	internal string Abbreviation;

	[JsonProperty("security")]
	internal string Security;
}

internal struct StdGroup {
	[JsonProperty("serial")]
	internal long Serial;

	[JsonProperty("uid")]
	internal string UID;

	[JsonProperty("name")]
	internal string Name;

	[JsonProperty("abbreviation")]
	internal string Abbreviation;

	[JsonProperty("description")]
	internal string Description;
}

internal struct StdUserGroup {
	[JsonProperty("serial")]
	internal long Serial;

	[JsonProperty("uid")]
	internal string UID;

	[JsonProperty("user_serial")]
	internal long UserSerial;

	[JsonProperty("group_serial")]
	internal long GroupSerial;
}

internal struct StdAccess {
	[JsonProperty("serial")]
	internal long Serial;

	[JsonProperty("uid")]
	internal string UID;

	[JsonProperty("bit")]
	internal long Bitflag;

	[JsonProperty("description")]
	internal string Description;
}

internal struct StdRule {
	[JsonProperty("serial")]
	internal long Serial;

	[JsonProperty("uid")]
	internal string UID;

	[JsonProperty("name")]
	internal string Name;

	[JsonProperty("namespace")]
	internal string Namespace;
}

internal struct StdScope {
	[JsonProperty("serial")]
	internal long Serial;

	[JsonProperty("uid")]
	internal string UID;

	[JsonProperty("name")]
	internal string Name;

	[JsonProperty("namespace")]
	internal string Namespace;
}

internal struct StdAuth {
	[JsonProperty("serial")]
	internal long Serial;

	[JsonProperty("uid")]
	internal string UID;

	[JsonProperty("accessee")]
	internal string Accessee;

	[JsonProperty("scope_serial")]
	internal long ScopeSerial;

	[JsonProperty("rule_serial")]
	internal long? RuleSerial;
}

#pragma warning restore 0649
