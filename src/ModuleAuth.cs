using System.Collections.Generic;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

internal class Jwt {
	internal class HeaderType {
		[JsonProperty("typ")]
		internal string Type { get; set; } = "JWT";

		[JsonProperty("cty", NullValueHandling = NullValueHandling.Ignore)]
		internal string? ContentType { get; set; } = null;

		[JsonProperty("alg")]
		internal string Algorithm { get; set; } = "HS256";
	}

	internal class PayloadType {
		[JsonProperty("iss")]
		internal string Issuer { get; set; } = "";

		[JsonProperty("sub")]
		internal string Subject { get; set; } = "";

		[JsonProperty("aud")]
		internal string Audience { get; set; } = "";

		[JsonProperty("exp")]
		internal long ExpirationTime { get; set; } = 0;

		[JsonProperty("nbf")]
		internal long NotBeforeTime { get; set; } = 0;

		[JsonProperty("iat")]
		internal long IssuedAtTime { get; set; } = 0;

		[JsonProperty("jti")]
		internal string JWTID { get; set; } = "";
	}

	internal HeaderType Header { get; set; } = new();
	internal PayloadType Payload { get; set; } = new();
	internal string Secret { get; set; } = "";

	internal static string ToBase64( string text ) {
		return Convert.ToBase64String( Encoding.UTF8.GetBytes( text ) );
	}

	public override string ToString() {
		HMACSHA256 sha256 = new( Encoding.UTF8.GetBytes( Secret ) );
		string headerjson = JsonConvert.SerializeObject( Header );
		string payloadjson = JsonConvert.SerializeObject( Payload );
		string headerb64 = ToBase64( headerjson );
		string payloadb64 = ToBase64( payloadjson );
		string secret = $"{headerb64}.{payloadb64}";
		string secretb64 = Convert.ToBase64String( sha256.ComputeHash( Encoding.UTF8.GetBytes( secret ) ) );
		return $"{headerb64}.{payloadb64}.{secretb64}";
	}
}

internal class AuthLoginRequestData {
	[JsonProperty("user")]
	internal string User { get; set; } = "";

	[JsonProperty("security")]
	internal string Security { get; set; } = "";
}

internal class AuthLoginResponseData {
	[JsonProperty("auth")]
	internal string Auth { get; set; } = "";
}

internal class ModuleAuth : Module {
	internal ModuleAuth() {
		Function = new();
		Function.Add( "login", Login );
	}

	internal override string Name { get; } = "auth";

	internal bool Login( Request request, out Response response ) {
		if ( request.Module != Name || request.Function != "login" ) {
			response = new Response() {
				Module = Name,
				Code = -2,
				Errors = {
					new Error( -3, $"{request.Module}.{request.Function} mismatched signature {Name}.login" )
				}
			};
			return false;
		}

		AuthLoginRequestData? reqdata = request.Data.ToObject<AuthLoginRequestData>();
		AuthLoginResponseData respdata;

		if ( reqdata == null ) {
			response = new Response() {
				Module = Name,
				Code = -2,
				Errors = {
					new Error( -4, $"Failed authentification: User not found!" )
				}
			};
			return false;
		}

		string user_abr = reqdata.User;
		string stmt = $"SELECT * FROM std_user WHERE abbreviation = {user_abr}";
		DataTable user = Program.Database.Select( stmt );

		if ( user.Rows.Count == 0 ) {
			response = new Response() {
				Module = Name,
				Code = -3,
				Errors = {
					new Error( -5, $"Failed authentification: User not found!" )
				}
			};
			return false;
		}

		var row = user.Rows[0];

		if ( row["security"].ToString() != reqdata.Security ) {
			response = new() {
				Module = Name,
				Code = -3,
				Errors = {
					new Error( -6, $"Failed authentification: User not found!" )
				}
			};
			return false;
		}

		DateTimeOffset now = DateTimeOffset.UtcNow;
		DateTimeOffset begin = now;
		DateTimeOffset end = begin.AddHours( 1 );

		Jwt token = new() {
			Header = new(),
			Payload = new() {
				Issuer = "ce.auth",
				Subject = row["name"].ToString() ?? "",
				Audience = "ce",
				ExpirationTime = end.ToUnixTimeSeconds(),
				NotBeforeTime = begin.ToUnixTimeSeconds(),
				IssuedAtTime = now.ToUnixTimeSeconds(),
				JWTID = row["uuid"].ToString() ?? ""
			},
			Secret = Program.Config.Secret
		};

		respdata = new();
		respdata.Auth = token.ToString();

		response = new() {
			Module = Name,
			Code = 0,
			Data = new( reqdata )
		};

		return true;
	}
}
