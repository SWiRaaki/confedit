-- Create global rules "Allowed" and "Denied"
insert or ignore into std_rule (
	uuid,
	name,
	namespace
)
values (
	[GUID],
	'allowed',
	'global'
);

insert or ignore into std_rule (
	uuid,
	name,
	namespace
)
values (
	[GUID],
	'denied',
	'global'
);
