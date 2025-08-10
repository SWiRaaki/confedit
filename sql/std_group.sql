-- Create a std_group table to manage groups
create table if not exists std_group (
	user_serial		integer	primary key	asc	autoincrement,
	uuid			text	not null	unique,
	name			text	not null,
	abbreviation	text	not null	unique,
	description		text	not null
);
