-- Create a universal global scope for global access
insert or ignore into std_scope (
	uuid,
	name,
	namespace
)
values (
	[GUID],
	'any',
	'global'
);
