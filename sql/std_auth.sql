-- Create a std_atuh table to manage scoped authorizations of users and groups
create table if not exists std_auth (
	auth_serial		integer	primary key	asc	autoincrement,
	uuid			text	not null	unique,
	accessee		text	not null,
	access			integer not null, -- Bitwise ORed std_acces flags
	scope_serial	integer	not null,
	rule_serial		integer null,

	-- Constrains ids to existing scopes and 
	constraint fk_auth_scope	foreign key ( scope_serial )	references std_scope( scope_serial ),
	constraint fk_auth_rule		foreign key ( rule_serial )		references std_rule( rule_serial )
);
