-- Drop existing tables (if exist)
DROP TABLE IF EXISTS std_authorization;
DROP TABLE IF EXISTS std_grouping;
DROP TABLE IF EXISTS std_rule;
DROP TABLE IF EXISTS std_scope;
DROP TABLE IF EXISTS std_access;
DROP TABLE IF EXISTS std_group;
DROP TABLE IF EXISTS std_user;
-- DROP TABLE IF EXISTS std_db_version;

-- Versioning table
CREATE TABLE std_db_version (
  id SERIAL PRIMARY KEY,
  version INTEGER NOT NULL,
  script_version TEXT NOT NULL,
  executed_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- User table
CREATE TABLE std_user (
  id SERIAL PRIMARY KEY,
  ext_id UUID NOT NULL UNIQUE,
  name VARCHAR(32) NOT NULL,
  abbreviation VARCHAR(6),
  pass CHAR(64) NOT NULL
);

-- Group table
CREATE TABLE std_group (
  id SERIAL PRIMARY KEY,
  ext_id UUID NOT NULL UNIQUE,
  name VARCHAR(32) NOT NULL,
  abbreviation VARCHAR(6),
  description VARCHAR(256)
);

-- Access table (bitflag reference values)
CREATE TABLE std_access (
  id SERIAL PRIMARY KEY,
  bitflag INTEGER NOT NULL,
  name VARCHAR(16) NOT NULL
);

INSERT INTO std_access (bitflag, name) VALUES
  (1, 'READ'),
  (2, 'WRITE'),
  (4, 'DELETE'),
  (8, 'CREATE');

-- Scope table
CREATE TABLE std_scope (
  id SERIAL PRIMARY KEY,
  ext_id UUID NOT NULL UNIQUE,
  name VARCHAR(32) NOT NULL,
  namespace VARCHAR(32)
);

-- Rule table
CREATE TABLE std_rule (
  id SERIAL PRIMARY KEY,
  ext_id UUID NOT NULL UNIQUE,
  name VARCHAR(32) NOT NULL,
  namespace VARCHAR(32)
);

-- Grouping table: users assigned to groups
CREATE TABLE std_grouping (
  id SERIAL PRIMARY KEY,
  ext_id_user UUID NOT NULL,
  ext_id_group UUID NOT NULL,
  CONSTRAINT fk_grouping_user FOREIGN KEY (ext_id_user) REFERENCES std_user (ext_id),
  CONSTRAINT fk_grouping_group FOREIGN KEY (ext_id_group) REFERENCES std_group (ext_id)
);

-- Authorization table: rules for user/group on a scope
CREATE TABLE std_authorization (
  id SERIAL PRIMARY KEY,
  ext_id UUID NOT NULL,
  access INTEGER NOT NULL,
  scope UUID NOT NULL,
  rule UUID NOT NULL,
  CONSTRAINT fk_auth_scope FOREIGN KEY (scope) REFERENCES std_scope (ext_id),
  CONSTRAINT fk_auth_rule FOREIGN KEY (rule) REFERENCES std_rule (ext_id)
);

-- Insert initial DB version
INSERT INTO std_db_version (version, script_version) VALUES (1, 'init-v1');
