WITH
target_user AS (
	SELECT user_serial, uuid, name
	FROM std_user
	WHERE uuid = @user_uuid
),
user_groups AS (
	SELECT g.group_serial, g.uuid, g.name
	FROM std_group g
	JOIN std_user_gr ug ON ug.group_serial = g.group_serial
	JOIN target_user tu ON tu.user_serial  = ug.user_serial
),
accessees AS (
	SELECT uuid, name, 'user'  AS kind FROM target_user
	UNION ALL
	SELECT uuid, name, 'group' AS kind FROM user_groups
),
raw_auth AS (
	SELECT
	a.auth_serial,
	a.uuid           AS auth_uuid,
	a.accessee,
	ac.kind          AS granted_via,
	ac.name          AS accessee_name,
	a.access,
	a.scope_serial,
	a.rule_serial,
	s.name           AS scope_name,
	s.namespace      AS scope_ns,
	r.name           AS rule_name,
	r.namespace      AS rule_ns
	FROM std_auth a
	JOIN accessees ac        ON ac.uuid       = a.accessee
	LEFT JOIN std_scope s    ON s.scope_serial = a.scope_serial
	LEFT JOIN std_rule  r    ON r.rule_serial  = a.rule_serial
),
expanded AS (
	SELECT
	ra.auth_serial,
	sa.description AS permission
	FROM raw_auth ra
	JOIN std_acces sa ON (ra.access & sa.bitflag) <> 0
)
SELECT
ra.auth_serial,
ra.auth_uuid,
ra.granted_via,
ra.accessee_name,
ra.accessee,
ra.scope_serial,
-- ra.scope_ns || ':' || ra.scope_name AS scope,
ra.scope_ns,
ra.scope_name,
ra.rule_serial,
CASE WHEN ra.rule_serial IS NULL THEN 'global::allowed'
ELSE ra.rule_ns || ':' || ra.rule_name
END AS rule,
ra.access,
group_concat(e.permission, ', ') AS permissions
FROM raw_auth ra
LEFT JOIN expanded e ON e.auth_serial = ra.auth_serial
GROUP BY
ra.auth_serial, ra.auth_uuid, ra.granted_via, ra.accessee_name, ra.accessee,
ra.scope_serial, ra.scope_name, ra.scope_ns, ra.rule_serial, ra.rule_name, ra.rule_ns, ra.access
ORDER BY
ra.granted_via, ra.scope_ns, ra.scope_name, ra.rule_ns, ra.rule_name, ra.auth_serial;
