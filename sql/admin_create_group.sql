-- Create new group and return the resulting tuple
insert into std_group(
	uuid,
	name,
	abbreviation,
	description
)
values(
	'[GUID]',
	@name,
	@abbreviation,
	@description
)
returning uuid, name, abbreviation, description;
