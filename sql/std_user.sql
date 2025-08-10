-- Create a std_user table to manage users
create table if not exists std_user (
	user_serial		integer	primary key	asc	autoincrement,
	uuid			text	not null	unique,
	name			text	not null,
	abbreviation	text	not null	unique,
	security		text	not null
);
