-- Select all groups a user is associated with
select grp.uuid
from std_user usr, std_group grp, std_user_gr link
where usr.uuid = @uuid
and   link.user_serial = usr.user_serial
and   link.group_serial = grp.group_serial;
