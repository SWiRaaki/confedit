-- Select Name and abbreviation from a specific user identified by UUID
select uuid, name, abbreviation from std_user where uuid = @uuid
