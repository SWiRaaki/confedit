-- Update a group depending on a set of parameters by uuid
update std_group set [{SETLIST}] where uuid = @uuid
returning uuid, name, abbreviation, description;
