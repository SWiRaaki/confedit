-- Create the 4 basic access rights: READ, WRITE, DELETE, CREATE
insert or ignore into std_acces (
	uuid,
	bitflag,
	description
)
values(
	'[GUID]',
	0x00000001,
	'Read'
);

insert or ignore into std_acces (
	uuid,
	bitflag,
	description
)
values(
	'[GUID]',
	0x00000002,
	'Write'
);

insert or ignore into std_acces (
	uuid,
	bitflag,
	description
)
values(
	'[GUID]',
	0x00000004,
	'Delete'
);

insert or ignore into std_acces (
	uuid,
	bitflag,
	description
)
values(
	'[GUID]',
	0x00000008,
	'Create'
);
