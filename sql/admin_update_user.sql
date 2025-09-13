-- Update a user depending on a set of parameters by uuid
update std_user set [{SETLIST}] where uuid = @uuid
returning uuid, name, abbreviation;
