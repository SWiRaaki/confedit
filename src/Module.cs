using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

internal record Error( [property:JsonProperty("code")] long Code, [property:JsonProperty("msg")] string Message );

internal class Request {
	[JsonProperty("module")]
	internal string Module { get; set; } = "";

	[JsonProperty("function")]
	internal string Function { get; set; } = "";

	[JsonProperty("data")]
	internal JObject Data { get; set; } = new();
}

internal class Response {
	[JsonProperty("module")]
	internal string Module { get; set; } = "";

	[JsonProperty("code")]
	internal long Code { get; set; } = 0;

	[JsonProperty("data")]
	internal JObject Data { get; set; } = new();

	[JsonProperty("errors")]
	internal List<Error> Errors { get; set; } = new();
}

internal delegate bool ModuleProcess( Request request, out Response response );

internal abstract class Module {
	internal abstract string Name { get; }
	internal protected Dictionary<string, ModuleProcess> Function { get; protected set; } = new();
}
