-- Authorize admin group to have global unrestricted access
insert or ignore into std_auth (
	uuid,
	accessee,
	access,
	scope_serial,
	rule_serial
)
values(
	[GUID],
	( select uuid from std_group where abbreviation = 'admin' ),
	0xFFFFFFFF,
	( select scope_serial from std_scope where name = 'any' and namespace = 'global' ),
	( select rule_serial from std_rule where name = 'allowed' and namespace = 'global' )
);
