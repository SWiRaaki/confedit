-- Create a scope entry for the configuration file
insert into std_scope (
	uuid,
	name,
	namespace
)
values (
	'[GUID]',
	@name,
	@namespace
)
returning uuid, name, namespace;
