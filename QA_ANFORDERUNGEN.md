# ConfEdit Qualitätssicherung & Anforderungsspezifikation

Version: 1.0 (Entwurf)
Datum: 2025-09-23
Geltungsbereich: Backend (.NET 9 / WebSocket + HTTP Static Hosting), Frontend (HTML/CSS/JS), SQLite Schema & Patch-System, Konfigurations-Provider-Framework (JSON/XML/YAML/TOML).

## 1. Systemübersicht
ConfEdit ist eine Plattform zur Remote-Bearbeitung von Konfigurationen und bietet:
- WebSocket-basierte API mit modularem Request-Routing (auth, admin, fm = File Management)
- Rollen-/Scope-basiertes Berechtigungsmodell (User/Group, Access-Bitflags, hierarchische Scopes)
- Multi-Format Parsing & Serialisierung (JSON, XML, YAML, TOML)
- Frontend (seitenbasiert mit SPA-ähnlichem Verhalten) für Login, Dateiverwaltung, User/Group-Administration
- SQLite Persistenz mit Patch-basierter Migration (".patch" + SQL Skripte)

## 2. Funktionale Anforderungen (FR)
| ID | Anforderung | Quelle | Abnahmekriterium |
|----|-------------|--------|------------------|
| FR-01 | Benutzer-Authentifizierung per Passwort (Abkürzung + Geheimnis) | Code (ModuleAuth) | Login liefert code=0 und JWT-ähnliches Token; ungültige Credentials erzeugen Authentification Fehler |
| FR-02 | Sitzungsfortführung mit JWT Token (HMAC SHA256) | ModuleAuth | Token mit nicht abgelaufenem exp akzeptiert; abgelaufen -> Authorization Fehler |
| FR-03 | Auflisten von Benutzern/Gruppen mit Berechtigungsprüfung | ModuleAdmin | Nur Tokens mit passenden Access-Bits (Read) erfolgreich |
| FR-04 | CRUD Benutzer (anlegen/ändern/löschen) | ModuleAdmin | Create liefert neue UUID; Update berücksichtigt Felder; Delete entfernt Benutzer |
| FR-05 | CRUD Gruppen (anlegen/ändern/löschen) | ModuleAdmin | Analog Benutzer; Felder persistiert |
| FR-06 | Verknüpfen/Lösen von User-Group Zuordnungen | ModuleAdmin | Add/Remove Operationen sind idempotent (keine Duplikat-Verknüpfung) |
| FR-07 | Abruf von Benutzer-/Gruppenmitgliedschaften | ModuleAdmin | list_user_groups & list_group_users liefern zugehörige UUIDs |
| FR-08 | Auflisten zugänglicher Konfigurationsdateien | ModuleFm + SQL | get_list liefert nur Dateien mit effektiver Read Berechtigung |
| FR-09 | Lesen von Konfigurationsinhalten (Multi-Format) | Provider | get_config liefert kanonischen ConfigTree mit Items & UID |
| FR-10 | Schreiben/Aktualisieren einer Konfigurationsdatei | Provider | write_config persistiert Datei & liefert Erfolg (code=0) |
| FR-11 | Erstellen eines neuen Konfigurationseintrags & Scope | ModuleFm | create_config erzeugt Datei + std_scope Zeile |
| FR-12 | Löschen eines Konfigurationseintrags | ModuleFm | delete_config entfernt std_scope Zeile (und optional Datei) |
| FR-13 | Anwendung von Datenbank-Patches | Script/ScriptQLite | Alle .patch in Abhängigkeitsreihenfolge, Commit atomar |
| FR-14 | Berechtigungsauflösung (Scopes + geerbte globale Regeln) | SQL get_effective_auth / fm_get_list | Effektive Rechte gemäß Design berechnet |

## 3. Nicht-funktionale Anforderungen (NFR)
| ID | Kategorie | Anforderung | Messung |
|----|----------|------------|---------|
| NFR-01 | Performance | Auth + einfache List-Operationen < 200 ms bei 50 gleichzeitigen Clients (lokal) | Load Test (WebSocket) |
| NFR-02 | Skalierbarkeit | System unterstützt 500 gleichzeitige WebSocket Clients ohne Memory Leak | Langzeittest (2h) |
| NFR-03 | Zuverlässigkeit | Patch-Anwendung ist atomar; Teilfehler rollt zurück | Transaktion + Post-State Validierung |
| NFR-04 | Sicherheit | Keine Klartext-Passwortspeicherung über Login hinaus; gespeicherte Geheimnisse gehasht (zukünftig) | Code Review + DB Schema |
| NFR-05 | Sicherheit | Alle DB-Interaktionen parametrisiert (Injection vermeiden) | Statische Analyse; Raw Queries beheben |
| NFR-06 | Sicherheit | JWT Secret konfigurierbar, nicht hart codiert; Rotationsverfahren dokumentiert | Config & Betriebs-Guide |
| NFR-07 | Observability | System loggt Auth-Fehlschläge, Admin CRUD, FM Operationen auf Info-Level; Fehler auf Warning/Error | Logging Policy |
| NFR-08 | Maintainability | Modul-Interface stabil (Request, Response, Error Codes) mit SemVer | Version/Tag Policy |
| NFR-09 | Portabilität | Läuft auf Windows/Linux mit .NET 9 + SQLite | Smoke Test Skript |
| NFR-10 | Usability | Frontend bietet Formular-Validierungsfeedback | UI Abnahmetest |

## 4. Architektur & Qualitätsnotizen
- Einzelprozess hostet HttpListener + WebSocket; kein TLS (Risiko: Klartext). Empfehlung: HTTPS Reverse Proxy.
- Globale statische Zustände (Program.*) vereinfachen Zugriff, senken Testbarkeit; potenzielle Concurrency-Risiken (nicht threadsichere Modifikationen nach Startup). Maßnahme: Dictionaries nach Initialisierung einfrieren.
- Request Dispatch ohne strukturierte Logs & Korrelation. Ergänzen: Request-ID + Timing.

## 5. Sicherheitsbewertung & Anforderungen
Aktuelle Probleme:
1. Passwörter in std_user.security im Klartext (Hoch). -> ANFORDERUNG SEC-01: Salted Hashing (Argon2 / BCrypt / PBKDF2) + Migration.
2. SQL Injection Risiko in ModuleAuth Login (String Interpolation). -> SEC-02: Query parametrisieren.
3. Eigene JWT Implementierung und Loggen von Token + Secret (Console.WriteLine). -> SEC-03: Secret nicht loggen; Standard JWT Bibliothek erwägen oder Algorithmusfeld prüfen.
4. Kein Token Widerruf / Rotation. -> SEC-04: Blacklist oder kurze Laufzeit + Refresh Workflow.
5. Kein erzwungenes TLS. -> SEC-05: Hinter HTTPS (Reverse Proxy oder Kestrel mit Zertifikat) & sichere WebSocket (wss://).
6. Verzeichnis-Traversal Risiko bei FM Pfadkomposition. -> SEC-06: Normalisierung & Begrenzung auf Root.
7. Fehlende CSRF Betrachtung für zukünftige HTTP Endpoints. -> SEC-07: Dokumentieren falls REST erweitert.
8. Keine Rate Limiting / Brute Force Abwehr beim Login. -> SEC-08: Versuchszähler + Backoff.
9. Fehlendes Audit Logging für privilegierte Operationen. -> SEC-09: Strukturierte Audit-Einträge (wer, was, Scope, Zeit, Ergebnis).

## 6. Datenintegrität & Validierung
| ID | Anforderung | Lücke | Maßnahme |
|----|-------------|------|----------|
| VAL-01 | Alle eingehenden Request JSON auf Pflichtfelder prüfen | Teilweise (Konvertierungsfehler -> generisch) | Schema Validierungsschicht hinzufügen |
| VAL-02 | Whitelist für Dateiendungen (.json/.xml/.yaml/.toml) erzwingen | Vorhanden aber nicht zentral | Zentralen Validator vor Provider Lookup |
| VAL-03 | Leere oder doppelte Service-/Config-Namen verhindern | DB Unique verhindert Duplikate; leer nicht früh geprüft | Vorab-Check & Error Code |
| VAL-04 | Setlist Updates ignorieren leere Felder | Implementiert via Listenaufbau | Mindest-1-Feld Regel ergänzen |
| VAL-05 | Größenlimit pro Datei (z.B. 1MB) | Fehlend | Größenprüfung bei Read/Write |

## 7. Fehlerbehandlung & Codes
Definierte Domänen: Validation, Authentification (Tippfehler), Authorization, Provider, Module, UnknownRequest, Unknown.
Anforderungen:
- ERR-01: Schreibweise vereinheitlichen: Authentification -> Authentication (Kompatibilitäts-Mapping beibehalten)
- ERR-02: Response.Errors maschinenlesbar: {code, msg}
- ERR-03: Keine Stacktraces in Produktionsantworten (derzeit okay, Konsole darf intern).

## 8. Logging & Observability
| ID | Anforderung | Implementierungs-Lücke |
|----|-------------|------------------------|
| LOG-01 | Console durch strukturierte Logs (z.B. Serilog) ersetzen | Nicht implementiert |
| LOG-02 | Jede Anfrage mit Client ID und GUID korrelieren | Teilweise (client.ID) | requestId ergänzen |
| LOG-03 | Auth-Fehlschläge mit IP & Abkürzung (ohne Passwort) loggen | Fehlend |
| LOG-04 | Patch-Anwendung (Summary) erfassen | Teilweise (Konsole) | Persistenz in std_dbver oder Logdatei |

## 9. Performance & Last
- PERF-01: Auflistung skaliert O(n); Pagination ab > 500 Einträgen.
- PERF-02: Große Konfig-Schreibvorgänge gepuffert (derzeit vollständig im Speicher). Streaming ab > 5MB erwägen.

## 10. Konfigurationsmanagement
- confedit.json enthält host/port/secret – ANFORDERUNG CFG-01: Environment Variable Overrides erlauben.
- CFG-02: Leeres secret verbieten (Startup Validierung).
- CFG-03: Beispiel-Hardened-Config bereitstellen (Port != 0, 0.0.0.0 optional).

## 11. Datenbank Migration / Patch Qualität
| ID | Anforderung | Status | Maßnahme |
|----|-------------|--------|----------|
| DB-01 | Patches atomar (Transaktion) | Implementiert | Rollback Test verifizieren |
| DB-02 | Patch Idempotenz (skip wenn installiert) | Implementiert | Checksum Verifikation ergänzen |
| DB-03 | Abhängigkeitsauflösung notwendig | Implementiert | Algorithmus dokumentieren |
| DB-04 | Patch Audit Trail (wer angewendet) | Fehlend | Operator Feld / Metadaten ergänzen |

## 12. Teststrategie
Testebenen:
- Unit: Provider Parsing (JSON/XML/TOML), JWT Helfer, Berechtigungslogik, SQL Script Runner Platzhalter.
- Integration: Login + Admin CRUD Flows; FM create/write/read/delete; Patch auf leerer DB.
- Security: SQL Injection Versuch (Login), Path Traversal, abgelaufenes Token.
- Performance: Simulierte 200 parallele list_users + fm_get_list Requests.
- UI/E2E: Cypress oder Playwright: Login Flow, User CRUD, Datei laden & Edit Zyklus.

Wichtige Testfälle (High Priority Auszug):
1. AUTH-T01: Passwort Login Erfolg/Fehler.
2. AUTH-T02: Abgelaufenes Token abgelehnt.
3. AUTH-T03: Manipuliertes Token Signatur abgelehnt.
4. ADMIN-T01: Benutzer Lebenszyklus (Create + List + Update + Delete).
5. ADMIN-T02: Benutzer zur Gruppe hinzufügen/entfernen; Membership prüfen.
6. ADMIN-T03: Unberechtigter Benutzer kann Benutzerliste nicht abrufen.
7. FM-T01: Config anlegen, dann listen und lesen.
8. FM-T02: Ungültiges Format -> Provider Fehler.
9. FM-T03: Delete entfernt Scope Eintrag.
10. SEC-T01: SQL Injection Versuch im Login führt nur zu Validation/Auth Fehler.
11. SEC-T02: Path Traversal Versuch wird blockiert (nach SEC-06 Implementierung).
12. PATCH-T01: Patch bereits angewendet -> übersprungen.
13. PATCH-T02: Patch mit Fehler -> Rollback unverändert.
14. PERF-T01: 200 parallele WebSocket Requests < 5% Fehlerrate.

## 13. Risiken & Gegenmaßnahmen
| Risiko | Impact | Wahrscheinlichkeit | Gegenmaßnahme |
|--------|--------|-------------------|---------------|
| Klartext Passwörter | Account Kompromittierung | Hoch | Hashing (SEC-01) |
| Eigene JWT Schwächen | Auth Bypass | Mittel | Vettete JWT Lib |
| Fehlendes HTTPS | Credential Abgriff | Hoch | Reverse Proxy TLS |
| Path Traversal | Unautorisierter Dateizugriff | Mittel | Normalisieren + Root containment |
| Unbegrenzte Dateigröße | Speicher-Druck | Mittel | Größenlimit prüfen |
| Fehlendes Logging | Incident Analyse erschwert | Mittel | Strukturierte Logs |

## 14. Offene Punkte / Follow-Ups
| ID | Beschreibung | Priorität | Owner |
|----|-------------|----------|-------|
| ISSUE-01 | Raw SQL in ModuleAuth Login parametrisieren | Hoch | Dev |
| ISSUE-02 | Passwort Hashing + Migration | Hoch | Dev |
| ISSUE-03 | Path Normalisierung + Root Constraint für FM | Hoch | Dev |
| ISSUE-04 | Token + Secret Logging entfernen | Hoch | Dev |
| ISSUE-05 | Strukturierte Logger einführen | Mittel | Dev |
| ISSUE-06 | Fehler-Enum Schreibweise vereinheitlichen | Mittel | Dev |
| ISSUE-07 | Automatisiertes Test Harness hinzufügen | Hoch | QA |
| ISSUE-08 | Deployment Doku (HTTPS, Secret Rotation) | Mittel | DevOps |

## 15. Traceability Matrix (Template)
| Requirement ID | Design Artefakt | Code Referenz | Testfall ID | Status |
|----------------|-----------------|---------------|-------------|--------|
| FR-01 | Auth Flow | ModuleAuth.Login | AUTH-T01..T03 | Offen |
| FR-08 | FM List | ModuleFm.GetList + fm_get_list.sql | FM-T01 | Offen |
| SEC-01 | Passwort Hashing | (Geplant) | AUTH-T04 | Offen |

## 16. Abnahmekriterien Release 1.0
Minimum für Produktionsreife:
- Implementiert: SEC-01..SEC-04, SEC-06, SEC-08 (kritische Sicherheit)
- Implementiert: LOG-01, LOG-03 (Basis Audit)
- Alle FR Tests bestanden (100% High Priority)
- Patch Rollback Integrationstest PASS
- Keine offenen kritischen (High) Issues

## 17. Glossar
- Scope: (namespace, name) Paar repräsentiert Datei oder Container.
- Access Bitflag: Integer mit OR-verknüpften Rechten (Read/Write/Create/Delete etc.).
- ConfigTree: Abstrakte Darstellung des Konfigurationsinhalts unabhängig vom Ursprungsformat.

---
Dieses Dokument ist ein „Living Artifact“. Änderungen via Versionskontrolle. Nächste Revision ergänzt Security-Hardening Details und konkrete Performance Metriken.
