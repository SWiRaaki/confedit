-- Create a std_user_gr table to manage user-group associations
create table if not exists std_user_gr (
	user_gr_serial	integer	primary key	asc	autoincrement,
	uuid			text	not null unique,
	user_serial		integer	not null,
	group_serial	integer	not null,

	check( length( uuid ) = 32 and uuid GLOB '[0-9a-f]*' ),

	-- Constrains ids to existing users and groups
	constraint fk_user_gr_user	foreign key ( user_serial )		references std_user( user_serial )   on delete cascade,
	constraint fk_user_gr_group	foreign key ( group_serial )	references std_group( group_serial ) on delete cascade,

	-- Only allow any user<->group serial combination once
	unique( user_serial, group_serial )
);
