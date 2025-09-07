WITH
target_user AS (
	SELECT user_serial, uuid
	FROM std_user
	WHERE uuid = @user_uuid
),
user_groups AS (
	SELECT g.uuid
	FROM std_group g
	JOIN std_user_gr ug ON ug.group_serial = g.group_serial
	JOIN target_user tu ON tu.user_serial = ug.user_serial
),
principals AS (
	SELECT uuid FROM target_user
	UNION ALL
	SELECT uuid FROM user_groups
),
auth_rows AS (
	SELECT a.accessee, a.access, s.scope_serial, s.namespace, s.name
	FROM std_auth a
	JOIN principals p ON p.uuid = a.accessee
	JOIN std_scope s ON s.scope_serial = a.scope_serial
),
file_scopes AS (
	SELECT scope_serial, namespace, name
	FROM std_scope
	WHERE name != 'any'
	AND name NOT LIKE '://%'
	AND (
		name LIKE '%.xml' OR
		name LIKE '%.json'
	)
),
container_scopes AS (
	SELECT scope_serial, namespace, name
	FROM std_scope
	WHERE name == 'any'
),
global_any AS (
	SELECT scope_serial
	FROM std_scope
	WHERE namespace = 'global'
	AND name = 'any'
),
read_mask AS (
	SELECT bitflag AS mask
	FROM std_acces
	WHERE description = 'Read'
	LIMIT 1
),
direct_effect AS (
	SELECT fs.scope_serial, ar.access
	FROM auth_rows ar
	JOIN file_scopes fs ON fs.scope_serial = ar.scope_serial
),
container_effect AS (
	SELECT fs.scope_serial, ar.access
	FROM auth_rows ar
	JOIN file_scopes fs ON ar.namespace = fs.namespace
),
global_effect AS (
	SELECT fs.scope_serial, ar.access
	FROM auth_rows ar
	JOIN global_any ga ON ga.scope_serial = ar.scope_serial
	JOIN file_scopes fs
),
effective_auth AS (
	SELECT * FROM direct_effect
	UNION ALL
	SELECT * FROM container_effect
	UNION ALL
	SELECT * FROM global_effect
),
readable_files AS (
	SELECT DISTINCT ea.scope_serial
	FROM effective_auth ea, read_mask rm
	WHERE (ea.access & rm.mask) != 0
),
expanded_permissions AS (
	SELECT DISTINCT
	rf.scope_serial,
	sa.description AS permission
	FROM readable_files rf
	JOIN effective_auth ea ON ea.scope_serial = rf.scope_serial
	JOIN std_acces sa ON (ea.access & sa.bitflag) != 0
)
SELECT
s.scope_serial,
s.namespace,
s.name,
group_concat(ep.permission, ', ') AS permissions
FROM readable_files rf
JOIN std_scope s ON s.scope_serial = rf.scope_serial
LEFT JOIN expanded_permissions ep ON ep.scope_serial = rf.scope_serial
GROUP BY s.scope_serial, s.namespace, s.name
ORDER BY s.namespace, s.name;
