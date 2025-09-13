using System.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

internal class AdminListUsersRequestData {
	[JsonProperty("auth", Required = Required.Always)]
	internal string Auth { get; set; } = "";
}

internal class AdminListUsersResponseData {
	[JsonProperty("users")]
	internal List<(string UID, string Name, string Abbreviation)> Users { get; set; } = new();
}

internal class AdminGetUserRequestData {
	[JsonProperty("auth", Required = Required.Always)]
	internal string Auth { get; set; } = "";

	[JsonProperty("uid", Required = Required.Always)]
	internal string UID { get; set; } = "";
}

internal class AdminGetUserResponseData {
	[JsonProperty("uid")]
	internal string UID { get; set; } = "";

	[JsonProperty("name")]
	internal string Name { get; set; } = "";

	[JsonProperty("abbreviation")]
	internal string Abbreviation { get; set; } = "";
}

internal class AdminCreateUserRequestData {
	[JsonProperty("auth", Required = Required.Always)]
	internal string Auth { get; set; } = "";

	[JsonProperty("name", Required = Required.Always)]
	internal string Name { get; set; } = "";

	[JsonProperty("abbreviation", Required = Required.Always)]
	internal string Abbreviation { get; set; } = "";

	[JsonProperty("security", Required = Required.Always)]
	internal string Security { get; set; } = "";
}

internal class AdminCreateUserResponseData {
	[JsonProperty("uid", Required = Required.Always)]
	internal string UID { get; set; } = "";

	[JsonProperty("name", Required = Required.Always)]
	internal string Name { get; set; } = "";

	[JsonProperty("abbreviation", Required = Required.Always)]
	internal string Abbreviation { get; set; } = "";
}

internal class AdminUpdateUserRequestData {
	[JsonProperty("auth", Required = Required.Always)]
	internal string Auth { get; set; } = "";

	[JsonProperty("uid", Required = Required.Always)]
	internal string UID { get; set; } = "";

	[JsonProperty("name", Required = Required.Always)]
	internal string Name { get; set; } = "";

	[JsonProperty("abbreviation", Required = Required.Always)]
	internal string Abbreviation { get; set; } = "";

	[JsonProperty("security", Required = Required.Always)]
	internal string Security { get; set; } = "";
}

internal class AdminUpdateUserResponseData {
	[JsonProperty("uid")]
	internal string UID { get; set; } = "";

	[JsonProperty("name")]
	internal string Name { get; set; } = "";

	[JsonProperty("abbreviation")]
	internal string Abbreviation { get; set; } = "";
}

internal class AdminDeleteUserRequestData {
	[JsonProperty("auth", Required = Required.Always)]
	internal string Auth { get; set; } = "";

	[JsonProperty("uid", Required = Required.Always)]
	internal string UID { get; set; } = "";
}

internal class AdminCreateGroupRequestData {
	[JsonProperty("auth", Required = Required.Always)]
	internal string Auth { get; set; } = "";

	[JsonProperty("name", Required = Required.Always)]
	internal string Name { get; set; } = "";

	[JsonProperty("abbreviation", Required = Required.Always)]
	internal string Abbreviation { get; set; } = "";

	[JsonProperty("description", Required = Required.Always)]
	internal string Description { get; set; } = "";
}

internal class ModuleAdmin : Module {
	internal ModuleAdmin() : base() {
		Function.Add( "list_users", ListUsers );
		Function.Add( "get_user", GetUser );
		Function.Add( "create_user", CreateUser );
		Function.Add( "update_user", UpdateUser );
		Function.Add( "delete_user", DeleteUser );
		Function.Add( "create_group", CreateGroup );
	}

	internal override string Name { get; } = "admin";

	internal bool ListUsers( object caller, Request request, out Response response ) {
		if ( request.Module != Name || request.Function != "list_users" ) {
			response = new Response() {
				Module = Name,
				Code = RequestError.Validation,
				Errors = {
					new Error( ValidationError.FunctionMismatch, $"{request.Module}.{request.Function} mismatched signature {Name}.list_users" )
				}
			};
			return false;
		}

		AdminListUsersRequestData reqdata = request.Data.ToObject<AdminListUsersRequestData>()!;
		AdminListUsersResponseData respdata;

		if ( reqdata == null ) {
			response = new Response() {
				Module = Name,
				Code = RequestError.Validation,
				Errors = {
					new Error( ValidationError.InvalidRequestData, "Invalid request data provided!" )
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
		if ( !token.IsAuthorized( "admin", "users", "Read" ) ) {
			response = new Response() {
				Module = Name,
				Code = RequestError.Authorization,
				Errors = {
					new Error( AuthorizationError.Unauthorized, $"Not authorized to retrieve user list!" )
				}
			};
			return false;
		}

		try {
			var selected = Program.Script.RunScript( "sql/admin_list_users.sql" );
			if ( !selected ) {
				response = new Response() {
					Module = Name,
					Code = RequestError.Module,
					Errors = {
						new Error( ModuleError.SqlError, $"Failed to retrieve user list: [{selected.Code}]{selected.Message}")
					}
				};
				return false;
			}

			respdata = new();
			foreach( DataRow row in selected.Data!.Rows ) {
				respdata.Users.Add( ((string)row["uuid"], (string)row["name"], (string)row["abbreviation"]) );
			}

			response = new Response() {
				Module = Name,
				Code = RequestError.None,
				Data = JObject.FromObject( respdata )
			};
			return true;
		}
		catch ( Exception e ) {
			response = new Response() {
				Module = Name,
				Code = RequestError.Unknown,
				Errors = {
					new Error( -1, $"Failed to retrieve users: {e.Message}" )
				}
			};
			return false;
		}
	}

	internal bool GetUser( object caller, Request request, out Response response ) {
		if ( request.Module != Name || request.Function != "get_user" ) {
			response = new Response() {
				Module = Name,
				Code = RequestError.Validation,
				Errors = {
					new Error( ValidationError.FunctionMismatch, $"{request.Module}.{request.Function} mismatched signature {Name}.get_user" )
				}
			};
			return false;
		}

		AdminGetUserRequestData reqdata = request.Data.ToObject<AdminGetUserRequestData>()!;
		AdminGetUserResponseData respdata;

		if ( reqdata == null ) {
			response = new Response() {
				Module = Name,
				Code = RequestError.Validation,
				Errors = {
					new Error( ValidationError.InvalidRequestData, "Invalid request data provided!" )
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
		if ( !token.IsAuthorized( "admin", "users", "Read" ) ) {
			response = new Response() {
				Module = Name,
				Code = RequestError.Authorization,
				Errors = {
					new Error( AuthorizationError.Unauthorized, $"Not authorized to retrieve user data!" )
				}
			};
			return false;
		}

		try {
			var selected = Program.Script.RunScript(
				"sql/admin_get_user.sql",
				("@uuid", reqdata.UID)
			);

			if ( !selected ) {
				response = new Response() {
					Module = Name,
					Code = RequestError.Module,
					Errors = {
						new Error( ModuleError.SqlError, $"Failed to retrieve user: [{selected.Code}]{selected.Message}" )
					}
				};
				return false;
			}
			respdata = new();
			if ( selected.Data!.Rows.Count == 0 ) {
				response = new Response() {
					Module = Name,
					Code = RequestError.Module,
					Errors = {
						new Error( ModuleError.DataNotFound, $"Failed to retrieve user: User with ID {reqdata.UID} does not exist" )
					}
				};
				return false;
			}
			respdata = new() {
				UID = (string)selected.Data!.Rows[0]["uuid"],
				Name = (string)selected.Data!.Rows[0]["name"],
				Abbreviation = (string)selected.Data!.Rows[0]["abbreviation"]
			};

			response = new Response() {
				Module = Name,
				Code = RequestError.None,
				Data = JObject.FromObject( respdata )
			};
			return true;
		}
		catch ( Exception e ) {
			response = new Response() {
				Module = Name,
				Code = RequestError.Unknown,
				Errors = {
					new Error( -1, $"Failed to retrieve users: {e.Message}" )
				}
			};
			return false;
		}
	}

	internal bool CreateUser( object caller, Request request, out Response response ) {
		if ( request.Module != Name || request.Function != "create_user" ) {
			response = new Response() {
				Module = Name,
				Code = RequestError.Validation,
				Errors = {
					new Error( ValidationError.FunctionMismatch, $"{request.Module}.{request.Function} mismatched signature {Name}.create_user" )
				}
			};
			return false;
		}

		AdminCreateUserRequestData reqdata = request.Data.ToObject<AdminCreateUserRequestData>()!;
		AdminCreateUserResponseData respdata;

		if ( reqdata == null ||
			 string.IsNullOrWhiteSpace( reqdata.Name ) ||
			 string.IsNullOrWhiteSpace( reqdata.Abbreviation ) ||
			 string.IsNullOrWhiteSpace( reqdata.Security ) ) {
			response = new Response() {
				Module = Name,
				Code = RequestError.Validation,
				Errors = {
					new Error( ValidationError.InvalidRequestData, "Invalid request data provided!" )
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
		if ( !token.IsAuthorized( "admin", "users", "Create" ) ) {
			response = new Response() {
				Module = Name,
				Code = RequestError.Authorization,
				Errors = {
					new Error( AuthorizationError.Unauthorized, $"Not authorized to create user data!" )
				}
			};
			return false;
		}

		try {
			var inserted = Program.Script.RunScript(
				"sql/admin_create_user.sql",
				("@name", reqdata.Name),
				("@abbreviation", reqdata.Abbreviation),
				("@security", reqdata.Security)
			);

			if ( !inserted ) {
				response = new Response() {
					Module = Name,
					Code = RequestError.Module,
					Errors = {
						new Error( ModuleError.DataCreationFailed, $"Failed to create user: [{inserted.Code}]{inserted.Message}" )
					}
				};
				return false;
			}

			respdata = new() {
				UID = (string)inserted.Data!.Rows[0]["uuid"],
				Name = (string)inserted.Data!.Rows[0]["name"],
				Abbreviation = (string)inserted.Data!.Rows[0]["abbreviation"]
			};

			response = new Response() {
				Module = Name,
				Code = RequestError.None,
				Data = JObject.FromObject( respdata )
			};
			return true;
		}
		catch ( Exception e ) {
			response = new Response() {
				Module = Name,
				Code = RequestError.Unknown,
				Errors = {
					new Error( -1, $"Failed to create users: {e.Message}" )
				}
			};
			return false;
		}
	}

	internal bool UpdateUser( object caller, Request request, out Response response ) {
		if ( request.Module != Name || request.Function != "update_user" ) {
			response = new Response() {
				Module = Name,
				Code = RequestError.Validation,
				Errors = {
					new Error( ValidationError.FunctionMismatch, $"{request.Module}.{request.Function} mismatched signature {Name}.update_user" )
				}
			};
			return false;
		}

		AdminUpdateUserRequestData reqdata = request.Data.ToObject<AdminUpdateUserRequestData>()!;
		AdminUpdateUserResponseData respdata;

		if ( reqdata == null ) {
			response = new Response() {
				Module = Name,
				Code = RequestError.Validation,
				Errors = {
					new Error( ValidationError.InvalidRequestData, "Invalid request data provided!" )
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
		if ( !token.IsAuthorized( "admin", "users", "Write" ) ) {
			response = new Response() {
				Module = Name,
				Code = RequestError.Authorization,
				Errors = {
					new Error( AuthorizationError.Unauthorized, $"Not authorized to update user data!" )
				}
			};
			return false;
		}

		var transaction = Program.Database.BeginTransaction();
		try {
			var updated = Program.Script.RunScript(
					"admin_update_user.sql",
					transaction,
					("@uuid", reqdata.UID),
					("@name", reqdata.Name),
					("@abbreviation", reqdata.Abbreviation),
					("@security", reqdata.Security)
			);
			if ( !updated ) {
				transaction.Rollback();
				response = new Response() {
					Module = Name,
					Code = RequestError.Module,
					Errors = {
						new Error( ModuleError.DataCreationFailed, $"Failed to update user!" )
					}
				};
				return false;
			}

			transaction.Commit();
			respdata = new() {
				UID = reqdata.UID,
				Name = reqdata.Name,
				Abbreviation = reqdata.Abbreviation
			};

			response = new Response() {
				Module = Name,
				Code = RequestError.None,
				Data = JObject.FromObject( respdata )
			};
			return true;
		}
		catch ( Exception e ) {
			transaction.Rollback();
			response = new Response() {
				Module = Name,
				Code = RequestError.Unknown,
				Errors = {
					new Error( -1, $"Failed to update users: {e.Message}" )
				}
			};
			return false;
		}
	}

	internal bool DeleteUser( object caller, Request request, out Response response ) {
		if ( request.Module != Name || request.Function != "delete_user" ) {
			response = new Response() {
				Module = Name,
				Code = RequestError.Validation,
				Errors = {
					new Error( ValidationError.FunctionMismatch, $"{request.Module}.{request.Function} mismatched signature {Name}.delete_user" )
				}
			};
			return false;
		}

		AdminDeleteUserRequestData reqdata = request.Data.ToObject<AdminDeleteUserRequestData>()!;

		if ( reqdata == null ) {
			response = new Response() {
				Module = Name,
				Code = RequestError.Validation,
				Errors = {
					new Error( ValidationError.InvalidRequestData, "Invalid request data provided!" )
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
		if ( !token.IsAuthorized( "admin", "users", "Delete" ) ) {
			response = new Response() {
				Module = Name,
				Code = RequestError.Authorization,
				Errors = {
					new Error( AuthorizationError.Unauthorized, $"Not authorized to delete user data!" )
				}
			};
			return false;
		}

		var transaction = Program.Database.BeginTransaction();
		try {
			var deleted = Program.Script.RunScript(
					"admin_delete_user.sql",
					transaction,
					("@uuid", reqdata.UID)
			);
			if ( !deleted ) {
				transaction.Rollback();
				response = new Response() {
					Module = Name,
					Code = RequestError.Module,
					Errors = {
						new Error( ModuleError.DataCreationFailed, $"Failed to delete user!" )
					}
				};
				return false;
			}

			transaction.Commit();
			response = new Response() {
				Module = Name,
				Code = RequestError.None
			};
			return true;
		}
		catch ( Exception e ) {
			transaction.Rollback();
			response = new Response() {
				Module = Name,
				Code = RequestError.Unknown,
				Errors = {
					new Error( -1, $"Failed to delete users: {e.Message}" )
				}
			};
			return false;
		}
	}

	internal bool CreateGroup( object caller, Request request, out Response response ) {
		if ( request.Module != Name || request.Function != "create_group" ) {
			response = new Response() {
				Module = Name,
				Code = RequestError.Validation,
				Errors = {
					new Error( ValidationError.FunctionMismatch, $"{request.Module}.{request.Function} mismatched signature {Name}.create_group" )
				}
			};
			return false;
		}

		AdminCreateGroupRequestData reqdata = request.Data.ToObject<AdminCreateGroupRequestData>()!;

		if ( reqdata == null ) {
			response = new Response() {
				Module = Name,
				Code = RequestError.Validation,
				Errors = {
					new Error( ValidationError.InvalidRequestData, "Invalid request data provided!" )
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
						new Error( AuthorizationError.Expired, $"Session token expired!" )
				}
			};
			return false;

		}
		if ( !token.IsAuthorized( "admin", "group", "Create" ) ) {
			response = new Response() {
				Module = Name,
				Code = RequestError.Authorization,
				Errors = {
					new Error( AuthorizationError.Unauthorized, $"Not authorized to create group {reqdata.Name}" )
				}
			};
			return false;
		}

		try {
			Program.Database.Execute( $"insert into std_group(uuid,name,abbreviation,description) values('{Guid.NewGuid():N}',{reqdata.Name},{reqdata.Abbreviation},{reqdata.Description})" );

			response = new Response() {
				Module = Name,
				Code = 0
			};
			return true;
		}
		catch( Exception e ) {
			response = new Response() {
				Module = Name,
				Code = RequestError.Unknown,
				Errors = {
					new Error( -1, $"Failed to create group: {e.Message}" )
				}
			};
			return false;
		}
	}
}
