-- Create a std_atuh table to manage scoped authorizations of users and groups
create table if not exists std_auth (
	auth_serial		integer	primary key	asc	autoincrement,
	uuid			text	not null	unique,
	accessee		text	not null,
	access			integer not null, -- Bitwise ORed std_acces flags
	scope_serial	integer	not null,
	rule_serial		integer null,

	check( length( uuid ) = 32 and uuid GLOB '[0-9a-f]*' ),

	-- Constrains ids to existing scopes and 
	constraint fk_auth_scope	foreign key ( scope_serial )	references std_scope( scope_serial )	on delete cascade,
	constraint fk_auth_rule		foreign key ( rule_serial )		references std_rule( rule_serial )		on delete cascade,

	unique( accessee, scope_serial, rule_serial )
);
