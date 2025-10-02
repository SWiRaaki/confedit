-- Search for a services root location
select uuid, name, namespace
from std_scope
where name LIKE '://%'
and   namespace = @namespace;
