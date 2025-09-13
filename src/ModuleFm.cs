using System.Data;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal class Config {
	[JsonProperty("service", Required = Required.Always)]
	internal string Service { get; set; } = "";

	[JsonProperty("config", Required = Required.Always)]
	internal string Configuration { get; set; } = "";
}

internal class FmGetListRequestData {
	[JsonProperty("auth", Required = Required.Always)]
	internal string Auth { get; set; } = "";
}

internal class FmGetListResponseData {
	[JsonProperty("configurations")]
	internal List<Config> Configurations { get; set; } = new();
}

internal class FmGetConfigRequestData {
	[JsonProperty("auth", Required = Required.Always)]
	internal string Auth { get; set; } = "";

	[JsonProperty("service", Required = Required.Always)]
	internal string Service { get; set; } = "";

	[JsonProperty("config", Required = Required.Always)]
	internal string Configuration { get; set; } = "";
}

internal class FmWriteConfigRequestData {
	[JsonProperty("auth", Required = Required.Always)]
	internal string Auth { get; set; } = "";

	[JsonProperty("service", Required = Required.Always)]
	internal string Service { get; set; } = "";

	[JsonProperty("config", Required = Required.Always)]
	internal string Configuration { get; set; } = "";

	[JsonProperty("uid", Required = Required.Always)]
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
				Code = RequestError.Validation,
				Errors = {
					new Error( ValidationError.FunctionMismatch, $"{request.Module}.{request.Function} mismatched signature {Name}.get_list" )
				}
			};
			return false;
		}

		FmGetListRequestData reqdata = request.Data.ToObject<FmGetListRequestData>()!;
		FmGetListResponseData respdata;

		if ( reqdata == null ) {
			response = new Response() {
				Module = Name,
				Code = RequestError.Validation,
				Errors = {
					new Error( ValidationError.InvalidRequestData , $"Failed retrieving configuration: Invalid request data provided!" )
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
				Code = RequestError.None,
				Data = JObject.FromObject( respdata )
			};

			return true;
		}
		catch( Exception e ) {
			response = new Response() {
				Module = Name,
				Code = RequestError.Unknown,
				Errors = {
					new Error( -1, $"Failed to retrieve list: {e.Message}" )
				}
			};
		}
		return false;
	}

	internal bool GetConfig( object caller, Request request, out Response response ) {
		if ( request.Module != Name || request.Function != "get_config" ) {
			response = new Response() {
				Module = Name,
				Code = RequestError.Validation,
				Errors = {
					new Error( ValidationError.FunctionMismatch, $"{request.Module}.{request.Function} mismatched signature {Name}.get_config" )
				}
			};
			return false;
		}

		FmGetConfigRequestData reqdata = request.Data.ToObject<FmGetConfigRequestData>()!;

		if ( reqdata == null ) {
			response = new Response() {
				Module = Name,
				Code = RequestError.Validation,
				Errors = {
					new Error( ValidationError.InvalidRequestData, $"Failed retrieving configuration: Invalid request data provided!" )
				}
			};
			return false;
		}

		var extension = Path.GetExtension( reqdata.Configuration );
		var provider = Program.ConfigProvider[extension];

		if ( provider == null ) {
			response = new Response() {
				Module = Name,
				Code = RequestError.Validation,
				Errors = {
					new Error( ValidationError.ProviderNotFound, $"No provider known to read {extension}-configurations" )
				}
			};
		}

		var token = Jwt.FromString( reqdata.Auth );
		if ( token.IsExpired() ) {
			response = new Response() {
				Module = Name,
				Code = RequestError.Authorization,
				Errors = {
					new Error( AuthorizationError.Expired, "Session token expired!" )
				}
			};
			return false;
		}
		if ( !token.IsAuthorized( reqdata.Service, reqdata.Configuration, "Read" ) ) {
			response = new Response() {
				Module = Name,
				Code = RequestError.Authorization,
				Errors = {
					new Error( AuthorizationError.Unauthorized, $"Not authorized to read configuration {reqdata.Service}:{reqdata.Configuration}" )
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
					Code = RequestError.Provider,
					Errors = {
						new Error( ProviderError.FileLoadError, $"Failed to read configuration: [{loaded?.Code}]{loaded?.Message}" )
					}
				};
				return false;
			}

			loaded.Data!.UID = (string)result.Rows[0]["uuid"];

			response = new Response() {
				Module = Name,
				Code = RequestError.None,
				Data = JObject.FromObject( loaded.Data ) ?? new JObject()
			};

			return true;
		}
		catch ( Exception e ) {
			response = new Response() {
				Module = Name,
				Code = RequestError.Unknown,
				Errors = {
					new Error( -1, $"Failed to read configuration: {e.Message}" )
				}
			};
			return false;
		}
	}

	internal bool WriteConfig( object caller, Request request, out Response response ) {
		if ( request.Module != Name || request.Function != "write_config" ) {
			response = new Response() {
				Module = Name,
				Code = RequestError.Validation,
				Errors = {
					new Error( ValidationError.FunctionMismatch, $"{request.Module}.{request.Function} mismatched signature {Name}.write_config" )
				}
			};
			return false;
		}

		FmWriteConfigRequestData reqdata = request.Data.ToObject<FmWriteConfigRequestData>()!;

		if ( reqdata == null ) {
			response = new Response() {
				Module = Name,
				Code = RequestError.Validation,
				Errors = {
					new Error( ValidationError.InvalidRequestData, $"Failed retrieving list: Invalid request data provided!" )
				}
			};
			return false;
		}

		var extension = Path.GetExtension( reqdata.Configuration );
		var provider = Program.ConfigProvider[extension];

		if ( provider == null ) {
			response = new Response() {
				Module = Name,
				Code = RequestError.Validation,
				Errors = {
					new Error( ValidationError.ProviderNotFound, $"No provider known to write {extension}-configurations" )
				}
			};
		}

		var token = Jwt.FromString( reqdata.Auth );
		if ( token.IsExpired() ) {
			response = new Response() {
				Module = Name,
				Code = RequestError.Authorization,
				Errors = {
					new Error( AuthorizationError.Expired, "Session token expired!" )
				}
			};
			return false;
		}
		if ( !token.IsAuthorized( reqdata.Service, reqdata.Configuration, "Write" ) ) {
			response = new Response() {
				Module = Name,
				Code = RequestError.Authorization,
				Errors = {
					new Error( AuthorizationError.Unauthorized, $"Not authorized to write configuration {reqdata.Service}:{reqdata.Configuration}" )
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
					Code = RequestError.Provider,
					Errors = {
						new Error( ProviderError.FileSaveError, $"Failed to write configuration: [{saved.Code}]{saved.Message}" )
					}
				};
				return false;
			}

			response = new Response() {
				Module = Name,
				Code = RequestError.None
			};

			return true;
		}
		catch ( Exception e ) {
			response = new Response() {
				Module = Name,
				Code = RequestError.Unknown,
				Errors = {
					new Error( -1, $"Failed to read configuration: {e.Message}" )
				}
			};
			return false;
		}

	}
}
