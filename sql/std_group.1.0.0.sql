-- Create admin group for admin user rights
insert or ignore into std_group (
	uuid,
	name,
	abbreviation,
	description
)
values(
	'[GUID]',
	'admin',
	'admin',
	'Admin user group with global access rights'
);
