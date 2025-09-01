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
		DataTable auths = Program.Database.Select(
			@"WITH
target_user AS (
  SELECT user_serial, uuid, name
  FROM std_user
  WHERE uuid = '" + token.Payload.JWTID + @"'
),
user_groups AS (
  SELECT g.group_serial, g.uuid, g.name
  FROM std_group g
  JOIN std_user_gr ug ON ug.group_serial = g.group_serial
  JOIN target_user tu ON tu.user_serial  = ug.user_serial
),
accessees AS (
  -- the principals that can carry auth for this user: the user and all their groups
  SELECT uuid, name, 'user'  AS kind FROM target_user
  UNION ALL
  SELECT uuid, name, 'group' AS kind FROM user_groups
),
raw_auth AS (
  SELECT
      a.auth_serial,
      a.uuid           AS auth_uuid,
      a.accessee,
      ac.kind          AS granted_via,      -- 'user' or 'group'
      ac.name          AS accessee_name,
      a.access,
      a.scope_serial,
      a.rule_serial,
      s.name           AS scope_name,
      s.namespace      AS scope_ns,
      r.name           AS rule_name,
      r.namespace      AS rule_ns
  FROM std_auth a
  JOIN accessees ac        ON ac.uuid       = a.accessee
  LEFT JOIN std_scope s    ON s.scope_serial = a.scope_serial
  LEFT JOIN std_rule  r    ON r.rule_serial  = a.rule_serial
),
expanded AS (
  -- decode the access bitfield into individual permissions
  SELECT
    ra.auth_serial,
    sa.description AS permission
  FROM raw_auth ra
  JOIN std_acces sa ON (ra.access & sa.bitflag) <> 0
)
SELECT
  ra.auth_serial,
  ra.auth_uuid,
  ra.granted_via,                   -- 'user' or 'group'
  ra.accessee_name,                 -- user/group name
  ra.accessee,                      -- user/group UUID (the principal on the auth row)
  ra.scope_serial,
  ra.scope_ns || ':' || ra.scope_name AS scope,
  ra.rule_serial,
  CASE WHEN ra.rule_serial IS NULL THEN NULL
       ELSE ra.rule_ns || ':' || ra.rule_name
  END                               AS rule,
  ra.access,                        -- original bitfield
  group_concat(e.permission, ', ') AS permissions
FROM raw_auth ra
LEFT JOIN expanded e ON e.auth_serial = ra.auth_serial
GROUP BY
  ra.auth_serial, ra.auth_uuid, ra.granted_via, ra.accessee_name, ra.accessee,
  ra.scope_serial, ra.scope_name, ra.scope_ns, ra.rule_serial, ra.rule_name, ra.rule_ns, ra.access
ORDER BY
  ra.granted_via, ra.scope_ns, ra.scope_name, ra.rule_ns, ra.rule_name, ra.auth_serial;"
		);

		auths.WriteToCsv( "select.csv" );

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
