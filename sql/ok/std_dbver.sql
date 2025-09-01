-- Create a std_dbver table to manage local versioning
create table if not exists std_dbver (
	dbver_serial	integer	primary key	asc	autoincrement,
	major			integer	not null	check( major >= 0),
	minor			integer	not null	check( minor >= 0),
	patch			integer	not null	check( patch >= 0),
	script_version  text	not null,
	executed_at		text	not null	default current_timestamp,

	-- Major, Minor and Patch create a unique key as well
	unique( major, minor, patch )
);
