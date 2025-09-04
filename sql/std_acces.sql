-- Create a std_acces table to define access rights
create table if not exists std_acces (
	acces_serial	integer	primary key	asc	autoincrement,
	uuid			text	not null	unique,
	bitflag			integer	not null	unique,
	description		text	not null,

	check( length( uuid ) = 32 and uuid GLOB '[0-9a-f]*' )
);
