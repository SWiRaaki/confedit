-- Create root and sysadmin user, that get global rights
insert or ignore into std_user (
	uuid,
	name,
	abbreviation,
	security
)
values (
	'[GUID]',
	'root',
	'root',
	''
);

insert or ignore into std_user (
	uuid,
	name,
	abbreviation,
	security
)
values (
	'[GUID]',
	'sysadmin',
	'sysadmin',
	''
);
