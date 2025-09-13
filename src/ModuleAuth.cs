using System.Buffers.Text;
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
		return Base64Url.EncodeToString( Encoding.UTF8.GetBytes( text ) );
	}

	internal static string FromBase64( string text ) {
		var b64 = Encoding.UTF8.GetBytes( text );
		return Encoding.UTF8.GetString( Base64Url.DecodeFromUtf8( b64 ) );
	}

	internal static string ComputeSignatureSegment( string headerb64, string payloadb64 )
    {
        var key = Encoding.UTF8.GetBytes( Program.Config.Secret );
        var data = Encoding.ASCII.GetBytes( $"{headerb64}.{payloadb64}" );
        using var hmac = new HMACSHA256( key );
        var sig = hmac.ComputeHash( data );
        return Base64Url.EncodeToString( sig );
    }

	internal bool IsExpired() {
		var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
		return now > Payload.ExpirationTime;
	}

	internal bool IsAuthorized( string scope_namespace, string scope, string access ) {
		try {
			string script = File.ReadAllText( "sql/get_effective_auth.sql" );
			DataTable result = Program.Database.Select( script, ( "@user_uuid", Payload.JWTID ) );

			foreach( DataRow row in result.Rows ) {
				string permissions = row["permissions"] as string ?? "";
				string scope_ns = row["scope_ns"] as string ?? "";
				string scope_name = row["scope_name"] as string ?? "";

				if ( !permissions.Contains( access ) ) {
					continue;
				}
				if ( scope_ns != "global" && scope_ns != scope_namespace ) {
					continue;
				}
				if ( scope_name != "any" && scope_name != scope ) {
					continue;
				}

				return true;
			}
		}
		catch( Exception e ) {
			Console.WriteLine( $"Authorization failed: {e.Message}" );
		}

		return false;
	}

	public override string ToString() {
		var headerjson = JsonConvert.SerializeObject( Header );
		var payloadjson = JsonConvert.SerializeObject( Payload );
		var headerb64 = ToBase64( headerjson );
		var payloadb64 = ToBase64( payloadjson );
		var secretb64 = ComputeSignatureSegment( headerb64, payloadb64 );
		Console.WriteLine( $"{headerb64}.{payloadb64}.{secretb64}; Secret: {Program.Config.Secret}" );
		return $"{headerb64}.{payloadb64}.{secretb64}";
	}

	internal static Jwt FromString( string token ) {
		string [] segments = token.Split( "." );
		var jwt = new Jwt();
		var headertext = FromBase64( segments[0] );
		var payloadtext = FromBase64( segments[1] );
		var secretb64 = ComputeSignatureSegment( segments[0], segments[1] );

		if ( secretb64 != segments[2] ) {
			return jwt;
		}

		jwt.Header = JsonConvert.DeserializeObject<HeaderType>( headertext )!;
		jwt.Payload = JsonConvert.DeserializeObject<PayloadType>( payloadtext )!;
		jwt.Secret = Program.Config.Secret;
		return jwt;
	}
}

internal class AuthLoginRequestData {
	[JsonProperty("user", Required = Required.Always)]
	internal string User { get; set; } = "";

	[JsonProperty("security", Required = Required.Always)]
	internal string Security { get; set; } = "";
}

internal class AuthLoginResponseData {
	[JsonProperty("auth")]
	internal string Auth { get; set; } = "";
}

internal class AuthRegisterUserRequestData {
}

internal class AuthRegisterUserResponseData {

}

internal class ModuleAuth : Module {
	internal ModuleAuth() : base() {
		Function.Add( "login", Login );
	}

	internal override string Name { get; } = "auth";

	internal bool Login( object caller, Request request, out Response response ) {
		if ( request.Module != Name || request.Function != "login" ) {
			response = new Response() {
				Module = Name,
				Code = RequestError.Validation,
				Errors = {
					new Error( ValidationError.FunctionMismatch, $"{request.Module}.{request.Function} mismatched signature {Name}.login" )
				}
			};
			return false;
		}

		AuthLoginRequestData reqdata = request.Data.ToObject<AuthLoginRequestData>()!;
		AuthLoginResponseData respdata;

		if ( reqdata == null ) {
			response = new Response() {
				Module = Name,
				Code = RequestError.Validation,
				Errors = {
					new Error( ValidationError.InvalidRequestData, $"Failed authentification: Invalid request data provided!" )
				}
			};
			return false;
		}

		string user_abr = reqdata.User;
		string stmt = $"SELECT * FROM std_user WHERE abbreviation = '{user_abr}'";
		DataTable user = Program.Database.Select( stmt );

		if ( user.Rows.Count == 0 ) {
			response = new Response() {
				Module = Name,
				Code = RequestError.Authentification,
				Errors = {
					new Error( AuthentificationError.UserNotFound, $"Failed authentification: User not found!" )
				}
			};
			return false;
		}

		var row = user.Rows[0];

		if ( row["security"].ToString() != reqdata.Security ) {
			response = new() {
				Module = Name,
				Code = RequestError.Authentification,
				Errors = {
					new Error( AuthentificationError.InvalidSecurity, $"Failed authentification: Invalid security!" )
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

		if ( caller is Client ) {
			Client client = (caller as Client)!;
			client.ID = new( row["uuid"].ToString() ?? "" );
		}

		respdata = new();
		respdata.Auth = token.ToString();

		response = new() {
			Module = Name,
			Code = RequestError.None,
			Data = JObject.FromObject( respdata )
		};

		return true;
	}
}
