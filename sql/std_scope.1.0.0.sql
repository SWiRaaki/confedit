-- Create a universal global scope for global access
insert or ignore into std_scope (
	uuid,
	name,
	namespace
)
values (
	'[GUID]',
	'any',
	'global'
);

-- Create a scope for testfiles to check up on configuration functionalities
insert or ignore into std_scope (
	uuid,
	name,
	namespace
)
values (
	'[GUID]',
	'any',
	'test'
);

insert or ignore into std_scope (
	uuid,
	name,
	namespace
)
values (
	'[GUID]',
	'://testfiles',
	'test'
);

insert or ignore into std_scope (
	uuid,
	name,
	namespace
)
values (
	'[GUID]',
	'thingdef.xml',
	'test'
);

insert or ignore into std_scope (
	uuid,
	name,
	namespace
)
values (
	'[GUID]',
	'thingdef.json',
	'test'
);

insert or ignore into std_scope (
	uuid,
	name,
	namespace
)
values (
	'[GUID]',
	'thingdef.yaml',
	'test'
);

insert or ignore into std_scope (
	uuid,
	name,
	namespace
)
values (
	'[GUID]',
	'thingdef.toml',
	'test'
);
