using System.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

internal class AdminCreateGroupRequestData {
	[JsonProperty("auth")]
	internal string Token { get; set; } = "";

	[JsonProperty("name")]
	internal string Name { get; set; } = "";

	[JsonProperty("abbreviation")]
	internal string Abbreviation { get; set; } = "";

	[JsonProperty("description")]
	internal string Description { get; set; } = "";
}

internal class ModuleAdmin : Module {
	internal ModuleAdmin() : base() {
		Function.Add( "create_group", CreateGroup );
	}

	internal override string Name { get; } = "admin";

	internal bool IsAuthorized( string user_uuid, string scope, string scope_namespace, string access ) {
		string script = File.ReadAllText( "sql/admin_get_auth.sql" );
		DataTable result = Program.Database.Select( script, ( "@user_uuid", user_uuid ) );

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

		return false;
	}

	internal bool CreateGroup( object caller, Request request, out Response response ) {
		if ( request.Module != Name || request.Function != "create_group" ) {
			response = new Response() {
				Module = Name,
				Code = -2,
				Errors = {
					new Error( -3, $"{request.Module}.{request.Function} mismatched signature {Name}.create_group" )
				}
			};
			return false;
		}

		AdminCreateGroupRequestData reqdata = request.Data.ToObject<AdminCreateGroupRequestData>()!;

		if ( reqdata == null ) {
			response = new Response() {
				Module = Name,
				Code = -2,
				Errors = {
					new Error( -4, $"Failed group creation: Invalid request data provided!" )
				}
			};
			return false;
		}

		var token = Jwt.FromString( reqdata.Token );
		if ( !IsAuthorized( token.Payload.JWTID, "std_auth", "create_group", "Create" ) ) {
			response = new Response() {
				Module = Name,
				Code = -2,
				Errors = {
					new Error( -9, $"Not authorized to create group {reqdata.Name}" )
				}
			};
			return false;
		}

		try {
			Program.Database.Execute( $"insert into std_group(uuid,name,abbreviation,description) values('{Guid.NewGuid():N}',{reqdata.Name},{reqdata.Abbreviation},{reqdata.Description})" );
		}
		catch( Exception e ) {
			response = new Response() {
				Module = Name,
				Code = -3,
				Errors = {
					new Error( -8, $"Failed to create group: {e.Message}" )
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
}
