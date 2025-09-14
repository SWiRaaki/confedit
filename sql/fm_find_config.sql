-- Search for configuration file by name and namespace
select uuid, name, namespace
from std_scope
where name = @name
and   namespace = @namespace;
