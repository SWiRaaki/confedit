-- Group 'root' and 'sysadmin' user to the 'admin' group
insert or ignore into std_user_gr (
	uuid,
	user_serial,
	group_serial
)
values(
	'[GUID]',
	( select user_serial from std_user where abbreviation = 'root' ),
	( select group_serial from std_group where abbreviation = 'admin' )
);

insert or ignore into std_user_gr (
	uuid,
	user_serial,
	group_serial
)
values(
	'[GUID]',
	( select user_serial from std_user where abbreviation = 'sysadmin' ),
	( select group_serial from std_group where abbreviation = 'admin' )
);
