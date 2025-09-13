-- Create new user and return the resulting tuple without secuirty
insert into std_user(
	uuid,
	name,
	abbreviation,
	security
)
values(
	'[GUID]',
	@name,
	@abbreviation,
	@security
)
returning uuid, name, abbreviation;
