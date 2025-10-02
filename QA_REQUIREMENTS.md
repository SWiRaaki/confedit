# ConfEdit Quality Assurance & Requirements Specification

Version: 1.0 (Draft)
Date: 2025-09-23
Scope: Backend (.NET 9 / WebSocket + HTTP static hosting), Frontend (HTML/CSS/JS), SQLite schema & patching, Configuration provider framework (JSON/XML/YAML/TOML).

## 1. System Overview
ConfEdit is a remote configuration editing platform providing:
- WebSocket-based API with modular request routing (auth, admin, fm (file management))
- Role/scope-based authorization model (user/group, access bitflags, hierarchical scopes)
- Multi-format configuration parsing & serialization (JSON, XML, YAML, TOML)
- Frontend SPA-like pages for login, file management, user/group administration
- SQLite persistence with patch-based migration system (".patch" + SQL scripts)

## 2. Functional Requirements (FR)
| ID | Requirement | Source | Acceptance Criteria |
|----|-------------|--------|---------------------|
| FR-01 | User authentication via password (abbreviation + security) | Code (ModuleAuth) | Login returns code=0 and JWT-like token; invalid credentials produce Authentification error |
| FR-02 | Session continuation with JWT token (HMAC SHA256) | ModuleAuth | Token with unexpired exp accepted; expired -> Authorization error |
| FR-03 | List users/groups with authorization gating | ModuleAdmin | Only tokens with appropriate access bits (Read) succeed |
| FR-04 | CRUD users (create/update/delete) | ModuleAdmin | Create returns new UUID; update respects provided fields; delete removes user |
| FR-05 | CRUD groups (create/update/delete) | ModuleAdmin | Similar to users; fields persisted |
| FR-06 | Link/unlink user-group associations | ModuleAdmin | Add/remove operations idempotent (no duplicate link) |
| FR-07 | Retrieve user/group membership | ModuleAdmin | list_user_groups & list_group_users return associated UUIDs |
| FR-08 | Enumerate accessible configuration files | ModuleFm + SQL | get_list returns only files with effective Read permission |
| FR-09 | Read configuration file content (multi-format) | Providers | get_config returns canonical ConfigTree with items & UID |
| FR-10 | Write/update configuration file | Providers | write_config persists file & returns success (code=0) |
| FR-11 | Create a new configuration entry & underlying scope | ModuleFm | create_config creates file + std_scope row |
| FR-12 | Delete configuration entry | ModuleFm | delete_config removes std_scope row (and optionally file) |
| FR-13 | Database patch application | Script/ScriptQLite | All .patch processed in dependency order, commit atomic |
| FR-14 | Authorization resolution (scopes + inherited global rules) | SQL get_effective_auth / fm_get_list | Effective permissions computed per design |

## 3. Non-Functional Requirements (NFR)
| ID | Category | Requirement | Measurement |
|----|----------|------------|------------|
| NFR-01 | Performance | Auth + simple list operations < 200 ms under 50 concurrent clients (local) | Load test (WebSocket) |
| NFR-02 | Scalability | System supports 500 concurrent WebSocket clients without memory leak | Long-run test (2h) |
| NFR-03 | Reliability | Patch application is atomic; partial failure rolls back | Transaction + post-state validation |
| NFR-04 | Security | No plaintext password exposure beyond login; stored secrets hashed (future improvement) | Code review + DB schema |
| NFR-05 | Security | All database interactions parameterized (avoid injection) | Static analysis; fix raw string queries |
| NFR-06 | Security | JWT secret configurable and not hard-coded; rotation procedure documented | Config & ops guide |
| NFR-07 | Observability | System logs auth failures, admin CRUD, FM operations at info level; errors at warning/error | Logging policy |
| NFR-08 | Maintainability | Module interface stable (Request, Response, Error codes) with semantic versioning | Version/tag policy |
| NFR-09 | Portability | Runs on Windows/Linux with .NET 9 + SQLite only | Smoke test script |
| NFR-10 | Usability | Frontend provides validation feedback for forms | UI acceptance test |

## 4. Architecture & Components Quality Notes
- Single process hosting HttpListener + WebSocket handshake; no TLS termination (risk: plaintext traffic) → Recommendation: add HTTPS reverse proxy requirement.
- Global static state (Program.*) simplifies access but reduces testability; potential concurrency risk (not thread-safe modifications to dictionaries after startup). Mitigation: freeze dictionaries post-initialization.
- Request dispatch lacks structured logging and correlation IDs. Add minimal request ID + timing.

## 5. Security Assessment & Requirements
Current Issues:
1. Passwords stored in std_user.security in plaintext (High). -> REQUIREMENT SEC-01: Introduce salted hashing (Argon2 / BCrypt / PBKDF2) + migration.
2. SQL injection risk in ModuleAuth Login (string interpolation of user abbreviation). -> SEC-02: Parameterize query.
3. JWT implementation custom and logs token + secret (Console.WriteLine). -> SEC-03: Remove secret logging; consider standard JWT library or verify algorithm field.
4. Token revocation & rotation absent. -> SEC-04: Add blacklist/exp claim lowering or short-lived tokens + refresh workflow.
5. No TLS enforced. -> SEC-05: Deploy behind HTTPS (reverse proxy or kestrel with certificate) & secure WebSocket (wss://).
6. Directory traversal risk in FM path composition (loc + user-supplied config). -> SEC-06: Normalize & restrict path within allowed root.
7. Missing CSRF considerations for any future HTTP endpoints (currently only /rest-test). -> SEC-07: Document requirement if REST expands.
8. Lack of rate limiting / brute force protection on login. -> SEC-08: Add attempt counter & exponential backoff.
9. Absence of audit logging for privileged operations. -> SEC-09: Append structured audit entries (who, what, scope, timestamp, outcome).

## 6. Data Integrity & Validation Requirements
| ID | Requirement | Gap | Action |
|----|-------------|-----|--------|
| VAL-01 | All incoming Request JSON validated for required fields | Partial (conversion errors -> generic errors) | Add schema validation layer |
| VAL-02 | Enforce filename extension whitelist (.json/.xml/.yaml/.toml) | Present but not centralized | Central validator before provider lookup |
| VAL-03 | Prevent empty or duplicate service/config names | DB unique handles duplicates; empty not screened early | Add pre-check & error code |
| VAL-04 | Ensure setlist updates ignore empty fields | Implemented via building list | Add minimum 1 field rule |
| VAL-05 | Enforce size limits per file (e.g., 1MB) | Missing | Add size check on read/write |

## 7. Error Handling & Codes
Defined error domains: Validation, Authentification (typo), Authorization, Provider, Module, UnknownRequest, Unknown.
Requirements:
- ERR-01: Standardize spelling: Authentification -> Authentication (retain backward compatibility map)
- ERR-02: Response.Errors always machine-parseable: {code, msg}
- ERR-03: No stack traces in production responses (currently none, but console prints allowed)

## 8. Logging & Observability Requirements
| ID | Requirement | Implementation Gap |
|----|------------|--------------------|
| LOG-01 | Replace Console with structured logger (Serilog) | Not implemented |
| LOG-02 | Correlate each request with client ID and GUID | Partially (client.ID) | Add requestId |
| LOG-03 | Log authentication failures with IP & abbreviation (without password) | Missing |
| LOG-04 | Track patch application summary (success/failure) | Partial (console) | Persist to std_dbver or log file |

## 9. Performance & Load Requirements
- PERF-01: Configuration listing scales O(n) with number of accessible scopes; add pagination if > 500 entries.
- PERF-02: Large config file writes buffered (currently full in-memory). Consider streaming if > 5MB.

## 10. Configuration Management
- confedit.json holds host/port/secret – REQUIREMENT CFG-01: Add environment variable overrides.
- CFG-02: Disallow empty secret (validation at startup).
- CFG-03: Provide sample hardened config with non-zero port binding, 0.0.0.0 optional.

## 11. Database Migration / Patch Quality
Requirements:
| ID | Requirement | Status | Action |
|----|------------|--------|--------|
| DB-01 | Patches applied atomically (transaction) | Implemented | Verify rollback test |
| DB-02 | Patch idempotency (skip if installed) | Implemented | Add checksum verification |
| DB-03 | Required dependency resolution | Implemented | Document algorithm |
| DB-04 | Patch audit trail (who applied) | Missing | Add operator field / metadata |

## 12. Test Strategy
Test Layers:
- Unit: Providers parsing (JSON/XML/TOML), JWT helpers, authorization logic, SQL script runner placeholder replacement.
- Integration: Login + admin CRUD flows; FM create/write/read/delete; Patch application on blank DB.
- Security: SQL injection attempt (login), path traversal attempts, expired token usage.
- Performance: Simulated 200 concurrent list_users + fm_get_list requests.
- UI/E2E: Cypress or Playwright: login flow, user CRUD, file load & edit cycle.

Required Test Cases (High Priority Subset):
1. AUTH-T01: Password login success/failure.
2. AUTH-T02: Expired token rejected.
3. AUTH-T03: Tampered token signature rejected.
4. ADMIN-T01: Create + list + update + delete user lifecycle.
5. ADMIN-T02: Add/remove user to group; verify membership lists.
6. ADMIN-T03: Unauthorized user cannot list users.
7. FM-T01: Create config then list and read.
8. FM-T02: Write invalid format -> provider error.
9. FM-T03: Delete config removes scope entry.
10. SEC-T01: SQL injection attempt in login returns validation/auth error only.
11. SEC-T02: Path traversal attempt is blocked (post implementation of SEC-06).
12. PATCH-T01: Patch already applied -> skipped.
13. PATCH-T02: Patch with failing script -> rollback state unchanged.
14. PERF-T01: 200 parallel websocket requests maintain < 5% error rate.

## 13. Risks & Mitigations
| Risk | Impact | Likelihood | Mitigation |
|------|--------|-----------|-----------|
| Plaintext passwords | Account compromise | High | Hashing (SEC-01) |
| Custom JWT flaws | Auth bypass | Medium | Use vetted JWT lib |
| Missing HTTPS | Credential interception | High | Enforce reverse proxy TLS |
| Path traversal | Unauthorized file access | Medium | Normalize + root containment |
| Unbounded file size | Memory pressure | Medium | Add size limit checks |
| Lack of logging | Incident analysis difficulty | Medium | Implement structured logging |

## 14. Open Issues / Follow-Up Actions
| ID | Description | Priority | Owner |
|----|-------------|----------|-------|
| ISSUE-01 | Replace raw SQL in ModuleAuth login with parameterized query | High | Dev |
| ISSUE-02 | Implement password hashing + migration script | High | Dev |
| ISSUE-03 | Add path normalization and root directory constraint for FM | High | Dev |
| ISSUE-04 | Remove token + secret logging in Jwt.ToString | High | Dev |
| ISSUE-05 | Introduce structured logger | Medium | Dev |
| ISSUE-06 | Standardize error enum spelling | Medium | Dev |
| ISSUE-07 | Add automated test harness | High | QA |
| ISSUE-08 | Document deployment (HTTPS, secret rotation) | Medium | DevOps |

## 15. Traceability Matrix Template
| Requirement ID | Design Artifact | Code Reference | Test Case ID | Status |
|----------------|-----------------|----------------|--------------|--------|
| FR-01 | Auth Flow | ModuleAuth.Login | AUTH-T01..T03 | Pending |
| FR-08 | FM List | ModuleFm.GetList + fm_get_list.sql | FM-T01 | Pending |
| SEC-01 | Password Hashing | (Planned) | AUTH-T04 | Open |

## 16. Acceptance Criteria for Release 1.0
Minimum set to declare production readiness:
- Implement SEC-01..SEC-04, SEC-06, SEC-08 (critical security)
- Implement LOG-01, LOG-03 (basic audit)
- All FR tests passing (100% of high priority)
- Patch rollback integration test PASS
- No critical (High) open issues

## 17. Glossary
- Scope: (namespace, name) pair representing a file or container.
- Access Bitflag: Integer representing OR'ed permissions (Read/Write/Create/Delete, etc.).
- ConfigTree: Abstract representation of configuration content independent of source format.

---
This document is a living artifact. Updates tracked via version control. Next revision to incorporate security hardening implementation details and concrete performance test metrics.
