using System.Data;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal class Config {
	[JsonProperty("service")]
	internal string Service { get; set; } = "";

	[JsonProperty("config")]
	internal string Configuration { get; set; } = "";
}

internal class FmGetListRequestData {
	[JsonProperty("auth")]
	internal string Auth { get; set; } = "";
}

internal class FmGetListResponseData {
	[JsonProperty("configurations")]
	internal List<Config> Configurations { get; set; } = new();
}

internal class FmGetConfigRequestData {
	[JsonProperty("auth")]
	internal string Auth { get; set; } = "";

	[JsonProperty("service")]
	internal string Service { get; set; } = "";

	[JsonProperty("config")]
	internal string Configuration { get; set; } = "";
}

internal class FmWriteConfigRequestData {
	[JsonProperty("auth")]
	internal string Auth { get; set; } = "";

	[JsonProperty("service")]
	internal string Service { get; set; } = "";

	[JsonProperty("config")]
	internal string Configuration { get; set; } = "";

	[JsonProperty("uid")]
	internal string UID { get; set; } = "";
}

internal class ModuleFm : Module {
	internal ModuleFm() : base() {
		Function.Add( "get_list", GetList );
		Function.Add( "get_config", GetConfig );
		Function.Add( "write_config", WriteConfig );
	}

	internal override string Name { get; } = "fm";

	internal bool GetList( object caller, Request request, out Response response ) {
		if ( request.Module != Name || request.Function != "get_list" ) {
			response = new Response() {
				Module = Name,
				Code = -2,
				Errors = {
					new Error( -3, $"{request.Module}.{request.Function} mismatched signature {Name}.get_list" )
				}
			};
			return false;
		}

		FmGetListRequestData reqdata = request.Data.ToObject<FmGetListRequestData>()!;
		FmGetListResponseData respdata;

		if ( reqdata == null ) {
			response = new Response() {
				Module = Name,
				Code = -2,
				Errors = {
					new Error( -4, $"Failed retrieving configuration: Invalid request data provided!" )
				}
			};
			return false;
		}

		try {
			var token = Jwt.FromString( reqdata.Auth );
			string script = File.ReadAllText( "sql/fm_get_list.sql" );
			DataTable result = Program.Database.Select( script, ( "@user_uuid", token.Payload.JWTID ) );

			respdata = new();
			foreach( DataRow row in result.Rows ) {
				respdata.Configurations.Add( new Config() { Service = (string)row["namespace"], Configuration = (string)row["name"] } );
			}

			response = new() {
				Module = Name,
				Code = 0,
				Data = JObject.FromObject( respdata )
			};

			return true;
		}
		catch( Exception e ) {
			response = new Response() {
				Module = Name,
				Code = -3,
				Errors = {
					new Error( -10, $"Failed to retrieve list: {e.Message}" )
				}
			};
		}
		return false;
	}

	internal bool GetConfig( object caller, Request request, out Response response ) {
		if ( request.Module != Name || request.Function != "get_config" ) {
			response = new Response() {
				Module = Name,
				Code = -2,
				Errors = {
					new Error( -3, $"{request.Module}.{request.Function} mismatched signature {Name}.get_config" )
				}
			};
			return false;
		}

		FmGetConfigRequestData reqdata = request.Data.ToObject<FmGetConfigRequestData>()!;

		if ( reqdata == null ) {
			response = new Response() {
				Module = Name,
				Code = -2,
				Errors = {
					new Error( -4, $"Failed retrieving configuration: Invalid request data provided!" )
				}
			};
			return false;
		}

		var extension = Path.GetExtension( reqdata.Configuration );
		var provider = Program.ConfigProvider[extension];

		if ( provider == null ) {
			response = new Response() {
				Module = Name,
				Code = -2,
				Errors = {
					new Error( -11, $"No provider known to read {extension}-configurations" )
				}
			};
		}

		var token = Jwt.FromString( reqdata.Auth );
		if ( !ModuleAdmin.IsAuthorized( token.Payload.JWTID, reqdata.Service, reqdata.Configuration, "Read" ) ) {
			response = new Response() {
				Module = Name,
				Code = -2,
				Errors = {
					new Error( -9, $"Not authorized to read configuration {reqdata.Service}:{reqdata.Configuration}" )
				}
			};
			return false;
		}

		try {
			var result = Program.Database.Select( $"select name, uuid from std_scope where namespace = '{reqdata.Service}' and name LIKE '://%'" );
			string path = "";
			if ( result.Rows.Count == 0) {
				path = Path.Combine( Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location )!, reqdata.Service, reqdata.Configuration );
			} else {
				var loc = (result.Rows[0]["name"] as string)!.Remove( 0, 3 );
				path = Path.Combine( loc, reqdata.Configuration );
			}

			var loaded = provider!.Load( path );
			if ( loaded.Code != 0 ) {
				response = new Response() {
					Module = Name,
					Code = -3,
					Errors = {
						new Error( -13, $"Failed to read configuration: {loaded?.Message}" )
					}
				};
				return false;
			}

			loaded.Data!.UID = (string)result.Rows[0]["uuid"];

			response = new Response() {
				Module = Name,
				Code = 0,
				Data = JObject.FromObject( loaded.Data ) ?? new JObject()
			};

			return true;
		}
		catch ( Exception e ) {
			response = new Response() {
				Module = Name,
				Code = -3,
				Errors = {
					new Error( -12, $"Failed to read configuration: {e.Message}" )
				}
			};
			return false;
		}
	}

	internal bool WriteConfig( object caller, Request request, out Response response ) {
		if ( request.Module != Name || request.Function != "write_config" ) {
			response = new Response() {
				Module = Name,
				Code = -2,
				Errors = {
					new Error( -3, $"{request.Module}.{request.Function} mismatched signature {Name}.write_config" )
				}
			};
			return false;
		}

		FmWriteConfigRequestData reqdata = request.Data.ToObject<FmWriteConfigRequestData>()!;

		if ( reqdata == null ) {
			response = new Response() {
				Module = Name,
				Code = -2,
				Errors = {
					new Error( -4, $"Failed retrieving list: Invalid request data provided!" )
				}
			};
			return false;
		}

		var extension = Path.GetExtension( reqdata.Configuration );
		var provider = Program.ConfigProvider[extension];

		if ( provider == null ) {
			response = new Response() {
				Module = Name,
				Code = -2,
				Errors = {
					new Error( -11, $"No provider known to write {extension}-configurations" )
				}
			};
		}

		var token = Jwt.FromString( reqdata.Auth );
		if ( !ModuleAdmin.IsAuthorized( token.Payload.JWTID, reqdata.Service, reqdata.Configuration, "Write" ) ) {
			response = new Response() {
				Module = Name,
				Code = -2,
				Errors = {
					new Error( -9, $"Not authorized to read configuration {reqdata.Service}:{reqdata.Configuration}" )
				}
			};
			return false;
		}

		try {
			var result = Program.Database.Select( $"select name, namespace, uuid from std_scope where namespace = '{reqdata.Service}' and name LIKE '://%'" );
			string path = "";
			if ( result.Rows.Count == 0) {
				path = Path.Combine( Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location )!, reqdata.Service, reqdata.Configuration );
			} else {
				var loc = (result.Rows[0]["name"] as string)!.Remove( 0, 3 );
				path = Path.Combine( loc, reqdata.Configuration );
			}

			var tree = request.Data.ToObject<ConfigTree>()!;
			var saved = provider!.Save( path, tree )!;
			if ( saved.Code != 0 ) {
				response = new Response() {
					Module = Name,
					Code = -3,
					Errors = {
						new Error( -13, $"Failed to write configuration: {saved.Message}" )
					}
				};
				return false;
			}

			response = new Response() {
				Module = Name,
				Code = 0
			};

			return true;
		}
		catch ( Exception e ) {
			response = new Response() {
				Module = Name,
				Code = -3,
				Errors = {
					new Error( -12, $"Failed to read configuration: {e.Message}" )
				}
			};
			return false;
		}

	}
}
