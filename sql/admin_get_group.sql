-- Select uuid, name, abbreviation and description from a specific group identified by uuid
select uuid, name, abbreviation, description from std_group where uuid = @uuid
