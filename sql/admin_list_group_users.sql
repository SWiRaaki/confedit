-- Select all users a group is associated with
select usr.uuid
from std_user usr, std_group grp, std_user_gr link
where grp.uuid = @uuid
and   link.user_serial = usr.user_serial
and   link.group_serial = grp.group_serial;
