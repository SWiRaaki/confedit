using System.Data;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

[JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
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

[JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
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

internal class FmCreateConfigRequestData {
	[JsonProperty("auth", Required = Required.Always)]
	internal string Auth { get; set; } = "";

	[JsonProperty("service", Required = Required.Always)]
	internal string Service { get; set; } = "";

	[JsonProperty("config", Required = Required.Always)]
	internal string Configuration { get; set; } = "";
}

[JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
internal class FmCreateConfigResponseData {
	[JsonProperty("service")]
	internal string Service { get; set; } = "";

	[JsonProperty("config")]
	internal string Configuration { get; set; } = "";

	[JsonProperty("uid")]
	internal string UID { get; set; } = "";
}

internal class FmWriteConfigRequestData {
	[JsonProperty("auth", Required = Required.Always)]
	internal string Auth { get; set; } = "";

	[JsonProperty("service", Required = Required.Always)]
	internal string Service { get; set; } = "";

	[JsonProperty("config", Required = Required.Always)]
	internal string Configuration { get; set; } = "";

	[JsonProperty("uid")]
	internal string UID { get; set; } = "";
}

internal class ModuleFm : Module {
	internal ModuleFm() : base() {
		Function.Add( "get_list", GetList );
		Function.Add( "get_config", GetConfig );
		Function.Add( "create_config", CreateConfig );
		Function.Add( "write_config", WriteConfig );
		Function.Add( "delete_config", DeleteConfig );
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

		var converted = ToObject<FmGetListRequestData>( request.Data );

		if ( !converted ) {
			response = new Response() {
				Module = Name,
				Code = RequestError.Validation,
				Errors = {
					new Error( ValidationError.InvalidRequestData , $"Failed retrieving configuration: Invalid request data provided! {converted.Message}" ),
				}
			};
			return false;
		}

		var reqdata = converted.Data!;
		FmGetListResponseData respdata;

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

		var converted = ToObject<FmGetConfigRequestData>( request.Data );

		if ( !converted ) {
			response = new Response() {
				Module = Name,
				Code = RequestError.Validation,
				Errors = {
					new Error( ValidationError.InvalidRequestData , $"Failed retrieving configuration: Invalid request data provided! {converted.Message}" ),
				}
			};
			return false;
		}

		var reqdata = converted.Data!;

		var extension = Path.GetExtension( reqdata.Configuration );
		Program.ConfigProvider.TryGetValue( extension, out var provider );

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
			var path = "";
			var loc  = "";
			var result = Program.Script.RunScript(
				"sql/fm_get_service_path.sql",
				null,
				("@namespace", reqdata.Service)
			);
			if ( result.Data!.Rows.Count == 0) {
				loc = $"://{reqdata.Service}";
			} else {
				loc = (result.Data!.Rows[0]["name"] as string)!.Remove( 0, 3 );
			}

			path = Path.Combine( loc, reqdata.Configuration );

			result = Program.Script.RunScript(
				"sql/fm_find_config.sql",
				null,
				("@name", reqdata.Configuration),
				("@namespace", reqdata.Service)
			);

			if ( result.Data!.Rows.Count == 0 ) {
				response = new Response() {
					Module = Name,
					Code = RequestError.Module,
					Errors = {
						new Error( ModuleError.DataNotFound, $"Failed to read configuration: {path} does not exist!" )
					}
				};
				return false;
			}

			if ( !File.Exists( path ) ) {
				response = new Response() {
					Module = Name,
					Code = RequestError.Module,
					Errors = {
						new Error( ModuleError.DataNotFound, $"Failed to read configuration: {path} does not exist!" )
					}
				};
				return false;
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

			loaded.Data!.UID = (string)result.Data!.Rows[0]["uuid"];

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

		var converted = ToObject<FmWriteConfigRequestData>( request.Data );

		if ( !converted ) {
			response = new Response() {
				Module = Name,
				Code = RequestError.Validation,
				Errors = {
					new Error( ValidationError.InvalidRequestData , $"Failed retrieving configuration: Invalid request data provided! {converted.Message}" ),
				}
			};
			return false;
		}

		var reqdata = converted.Data!;

		var extension = Path.GetExtension( reqdata.Configuration );
		Program.ConfigProvider.TryGetValue( extension, out var provider );

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
			var path = "";
			var loc  = "";
			var result = Program.Script.RunScript(
				"sql/fm_get_service_path.sql",
				null,
				("@namespace", reqdata.Service)
			);
			if ( result.Data!.Rows.Count == 0) {
				loc = $"://{reqdata.Service}";
			} else {
				loc = ((string)result.Data!.Rows[0]["name"]).Remove( 0, 3 );
			}

			path = Path.Combine( loc, reqdata.Configuration );

			result = Program.Script.RunScript(
				"sql/fm_find_config.sql",
				null,
				("@name", reqdata.Configuration),
				("@namespace", reqdata.Service)
			);

			if ( result.Data!.Rows.Count == 0 ) {
				response = new Response() {
					Module = Name,
					Code = RequestError.Module,
					Errors = {
						new Error( ModuleError.DataNotFound, $"Failed to write configuration: {path} does not exist!" )
					}
				};
				return false;
			}

			if ( !File.Exists( path ) ) {
				response = new Response() {
					Module = Name,
					Code = RequestError.Module,
					Errors = {
						new Error( ModuleError.DataNotFound, $"Failed to write configuration: {path} does not exist!" )
					}
				};
				return false;
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

	internal bool CreateConfig( object caller, Request request, out Response response ) {
		if ( request.Module != Name || request.Function != "create_config" ) {
			response = new Response() {
				Module = Name,
				Code = RequestError.Validation,
				Errors = {
					new Error( ValidationError.FunctionMismatch, $"{request.Module}.{request.Function} mismatched signature {Name}.create_config" )
				}
			};
			return false;
		}

		var converted = ToObject<FmCreateConfigRequestData>( request.Data );

		if ( !converted ) {
			response = new Response() {
				Module = Name,
				Code = RequestError.Validation,
				Errors = {
					new Error( ValidationError.InvalidRequestData , $"Failed retrieving configuration: Invalid request data provided! {converted.Message}" ),
				}
			};
			return false;
		}

		var reqdata = converted.Data!;
		FmCreateConfigResponseData respdata;

		var extension = Path.GetExtension( reqdata.Configuration );
		Program.ConfigProvider.TryGetValue( extension, out var provider );

		if ( provider == null ) {
			response = new Response() {
				Module = Name,
				Code = RequestError.Validation,
				Errors = {
					new Error( ValidationError.ProviderNotFound, $"No provider known to create {extension}-configurations" )
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
		if ( !token.IsAuthorized( "service", reqdata.Service, "Create" ) && !token.IsAuthorized( reqdata.Service, "any", "Create" ) ) {
			response = new Response() {
				Module = Name,
				Code = RequestError.Authorization,
				Errors = {
					new Error( AuthorizationError.Unauthorized, $"Not authorized to create configuration {reqdata.Service}:{reqdata.Configuration}" )
				}
			};
			return false;
		}

		try {
			var path = "";
			var loc  = "";
			var result = Program.Script.RunScript(
				"sql/fm_get_service_path.sql",
				null,
				("@namespace", reqdata.Service)
			);

			if ( result.Data!.Rows.Count == 0) {
				loc = reqdata.Service;
			} else {
				loc = (result.Data!.Rows[0]["name"] as string)!.Remove( 0, 3 );
			}

			path = Path.Combine( loc, reqdata.Configuration );

			result = provider.Create( path );
			if ( !result ) {
				response = new Response() {
					Module = Name,
					Code = RequestError.Module,
					Errors = {
						new Error( ModuleError.DataNotFound, $"Failed to create configuration: [{result.Code}] {result.Message}" )
					}
				};
				return false;
			}

			result = Program.Script.RunScript(
				"sql/fm_create_config.sql",
				null,
				("@name", reqdata.Configuration),
				("@namespace", reqdata.Service)
			);

			if ( !result ) {
				response = new Response() {
					Module = Name,
					Code = RequestError.Module,
					Errors = {
						new Error( ModuleError.DataNotFound, $"Failed to create configuration: [{result.Code}] {result.Message}" )
					}
				};
				return false;
			}

			respdata = new() {
				Service = reqdata.Service,
				Configuration = reqdata.Configuration,
				UID = (string)result.Data!.Rows[0]["uuid"]
			};

			response = new Response() {
				Module = Name,
				Code = RequestError.None,
				Data = JObject.FromObject( respdata ) ?? new JObject()
			};

			return true;
		}
		catch ( Exception e ) {
			response = new Response() {
				Module = Name,
				Code = RequestError.Unknown,
				Errors = {
					new Error( -1, $"Failed to create configuration: {e.Message}" )
				}
			};
			return false;
		}
	}

	internal bool DeleteConfig( object caller, Request request, out Response response ) {
		if ( request.Module != Name || request.Function != "delete_config" ) {
			response = new Response() {
				Module = Name,
				Code = RequestError.Validation,
				Errors = {
					new Error( ValidationError.FunctionMismatch, $"{request.Module}.{request.Function} mismatched signature {Name}.delete_config" )
				}
			};
			return false;
		}

		var converted = ToObject<FmCreateConfigRequestData>( request.Data );

		if ( !converted ) {
			response = new Response() {
				Module = Name,
				Code = RequestError.Validation,
				Errors = {
					new Error( ValidationError.InvalidRequestData , $"Failed retrieving configuration: Invalid request data provided! {converted.Message}" ),
				}
			};
			return false;
		}

		var reqdata = converted.Data!;

		var extension = Path.GetExtension( reqdata.Configuration );
		Program.ConfigProvider.TryGetValue( extension, out var provider );

		if ( provider == null ) {
			response = new Response() {
				Module = Name,
				Code = RequestError.Validation,
				Errors = {
					new Error( ValidationError.ProviderNotFound, $"No provider known to delete {extension}-configurations" )
				}
			};
			return false;
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


        if ( !token.IsAuthorized( "service", reqdata.Service, "Delete" ) &&
        	 !token.IsAuthorized( reqdata.Service, reqdata.Configuration, "Delete" ) ) {
        	response = new Response() {
        		Module = Name,
        		Code = RequestError.Authorization,
        		Errors = {
        			new Error( AuthorizationError.Unauthorized, $"Not authorized to delete configuration {reqdata.Service}:{reqdata.Configuration}" )
        		}
        	};
        	return false;
        }

        try {
			var path = "";
			var loc  = "";
			var result = Program.Script.RunScript(
				"sql/fm_get_service_path.sql",
				null,
				("@namespace", reqdata.Service)
			);

			if ( result.Data!.Rows.Count == 0) {
				loc = reqdata.Service;
			} else {
				loc = (result.Data!.Rows[0]["name"] as string)!.Remove( 0, 3 );
			}

			path = Path.Combine( loc, reqdata.Configuration );

			if ( !File.Exists( path ) ) {
				response = new Response() {
					Module = Name,
					Code = RequestError.Module,
					Errors = {
						new Error( ModuleError.DataNotFound, $"Failed to delete configuration: {path} does not exist" )
					}
				};
				return false;
			}

			result = Program.Script.RunScript(
				"sql/fm_delete_config.sql",
				null,
				("@name", reqdata.Configuration),
				("@namespace", reqdata.Service)
			);

			if ( !result ) {
				response = new Response() {
					Module = Name,
					Code = RequestError.Module,
					Errors = {
						new Error( ModuleError.DataNotFound, $"Failed to delete configuration: [{result.Code}] {result.Message}" )
					}
				};
				return false;
			}

			response = new Response() {
				Module = Name,
				Code = RequestError.None,
			};

			return true;
		}
		catch ( Exception e ) {
			response = new Response() {
				Module = Name,
				Code = RequestError.Unknown,
				Errors = {
					new Error( -1, $"Failed to delete configuration: {e.Message}" )
				}
			};
			return false;
		}
	}
}
