-- Remove user from group by user uuid and group uuid
delete from std_user_gr
where user_serial = (select user_serial from std_user where uuid = @user_uuid)
and   group_serial = (select group_serial from std_group where uuid = @group_uuid)
