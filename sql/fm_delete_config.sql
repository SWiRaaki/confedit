-- Delete configuration file
delete from std_scope where name = @name and namespace = @namespace;
