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
	internal RequestError Code { get; set; } = RequestError.None;

	[JsonProperty("data")]
	internal JObject Data { get; set; } = new();

	[JsonProperty("errors")]
	internal List<Error> Errors { get; set; } = new();
}

internal enum RequestError {
	None             = 0,
	Unknown          = -1,
	UnknownRequest   = -2,
	Validation       = -3,
	Authentification = -4,
	Authorization    = -5,
	Provider         = -6,
	Module           = -7
}

internal static class UnknownRequestError {
	internal const long UnknownModule      = -1;
	internal const long UnknownFunction    = -2;
	internal const long BinaryNotSupported = -3;
}

internal static class ValidationError {
	internal const long InvalidRequestData = -1;
	internal const long FunctionMismatch   = -2;
	internal const long ProviderNotFound   = -3;
}

internal static class AuthentificationError {
	internal const long UserNotFound       = -1;
	internal const long InvalidSecurity    = -2;
}

internal static class AuthorizationError {
	internal const long Expired      = -1;
	internal const long Unauthorized = -2;
}

internal static class ProviderError {
	internal const long FileLoadError = -1;
	internal const long FileSaveError = -2;
}

internal static class ModuleError {
	internal const long DataNotFound       = -1;
	internal const long DataCreationFailed = -2;
	internal const long SqlError           = -3;
}

internal delegate bool ModuleProcess( object caller, Request request, out Response response );

internal abstract class Module {
	internal abstract string Name { get; }
	internal protected Dictionary<string, ModuleProcess> Function { get; protected set; } = new();
}
