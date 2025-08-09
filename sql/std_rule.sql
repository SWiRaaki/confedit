-- Create a std_rule table to manage single rules
create table if not exists std_rule (
	rule_serial		integer	primary key	asc	autoincrement,
	uuid			text	not null	unique,
	name			text	not null,
	namespace		text	not null,

	-- The combination of name and namespace creates a unique key
	unique( name, namespace )
);
