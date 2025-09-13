-- Link user to group by user uuid and group uuid
insert into std_user_gr (
	uuid,
	user_serial,
	group_serial
)
values (
	'[GUID]',
	(select user_serial from std_user where uuid = @user_uuid),
	(select group_serial from std_group where uuid = @group_uuid)
);
