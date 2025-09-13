# ConfEdit – API‑Referenz

Dies ist eine simplifizierte API-Referenz der aktuell implementierten Module und Funktionen. KI-generiert, beinhaltet möglicherweise Fehler!

---

## Transport & Grundschema

**Protokoll:** WebSocket
**Nachrichtenformat:** JSON

### Request (allgemein)

```json
{
  "module": "<modulname>",
  "function": "<funktionsname>",
  "data": { /* funktionsspezifisch */ }
}
```

### Response (allgemein)

```json
{
  "module": "<modulname>",
  "code": 0,                 // RequestError (Enum → Zahl)
  "data": { /* optional, funktionsspezifisch */ },
  "errors": [                // nur bei Fehlern befüllt
    { "code": -1, "msg": "Fehlertext" }
  ]
}
```

### Top‑Level Fehlercodes (`code`)

`code` ist das Enum **RequestError**, das numerisch serialisiert wird:

| Name               | Wert | Bedeutung                                                   |
| ------------------ | ---: | ----------------------------------------------------------- |
| `None`             |    0 | OK                                                          |
| `Unknown`          |   -1 | Unerwarteter, interner Fehler                               |
| `UnknownRequest`   |   -2 | Unbekanntes Modul/Funktion oder binär nicht unterstützt     |
| `Validation`       |   -3 | Ungültige Anfrage (Pflichtfelder, Signatur, Provider fehlt) |
| `Authentification` |   -4 | Benutzer/Passwort fehlerhaft                                |
| `Authorization`    |   -5 | Token abgelaufen/keine Berechtigung                         |
| `Provider`         |   -6 | Datei‑Provider (Lesen/Schreiben) fehlgeschlagen             |
| `Module`           |   -7 | Domänenspezifischer Fehler (z. B. SQL)                      |

### Detail‑Fehler (`errors[].code`)

* **UnknownRequestError**: `-1 UnknownModule`, `-2 UnknownFunction`, `-3 BinaryNotSupported`
* **ValidationError**: `-1 InvalidRequestData`, `-2 FunctionMismatch`, `-3 ProviderNotFound`
* **AuthentificationError**: `-1 UserNotFound`, `-2 InvalidSecurity`
* **AuthorizationError**: `-1 Expired`, `-2 Unauthorized`
* **ProviderError**: `-1 FileLoadError`, `-2 FileSaveError`
* **ModuleError**: `-1 DataNotFound`, `-2 DataCreationFailed`, `-3 SqlError`

> **Hinweis:** Der Server erwartet zu Beginn einer WebSocket‑Session typischerweise einen `auth.login`‑Call.

---

## Modul: `auth`

### `login`

**Zweck:** Login. Zwei Modi:

* `grant_type = "jwt"` → vorhandenes Token prüfen (Refresh/Re‑Login). *`security` = JWT.*
* Andernfalls → Benutzer‑Login über Kürzel und Secret. *`user` = Abkürzung, `security` = Passwort.*

**Request – Passwort‑Login**

```json
{
  "module": "auth",
  "function": "login",
  "data": {
    "user": "jdoe",
    "security": "supersecret",
    "grant_type": "password"
  }
}
```

**Erfolg**

```json
{
  "module": "auth",
  "code": 0,
  "data": { "auth": "<jwt>" },
  "errors": []
}
```

**Fehler – Benutzer existiert nicht**

```json
{
  "module": "auth",
  "code": -4,
  "errors": [{ "code": -1, "msg": "Failed authentification: User not found!" }],
  "data": {}
}
```

**Request – JWT‑Login**

```json
{
  "module": "auth",
  "function": "login",
  "data": {
    "security": "<jwt>",
    "grant_type": "jwt"
  }
}
```

**Fehler – Token abgelaufen**

```json
{
  "module": "auth",
  "code": -5,
  "errors": [{ "code": -1, "msg": "Session token is expired!" }],
  "data": {}
}
```

---

## Modul: `admin`

> **Rechte:** Prüfen via JWT.

### `list_users`

**Zweck:** Alle Nutzer auflisten.

**Request**

```json
{ "module": "admin", "function": "list_users", "data": { "auth": "<jwt>" } }
```

**Erfolg**

```json
{
  "module": "admin",
  "code": 0,
  "data": {
    "users": [
      { "uid": "7f1c...", "name": "John Doe", "abbreviation": "jdoe" },
      { "uid": "ab42...", "name": "Jane Roe", "abbreviation": "jroe" }
    ]
  },
  "errors": []
}
```

> *Serializer‑Hinweis:* Im Code ist dies eine `List<(string UID,string Name,string Abbreviation)>`. Je nach Newtonsoft‑Version können Einträge auch als `{ "Item1": "...", "Item2": "...", "Item3": "..." }` erscheinen.

**Fehler – keine Leserechte**

```json
{
  "module": "admin",
  "code": -5,
  "errors": [{ "code": -2, "msg": "Not authorized to retrieve user list!" }],
  "data": {}
}
```

### `get_user`

**Zweck:** Einen Nutzer per `uid` holen.

**Request**

```json
{ "module": "admin", "function": "get_user", "data": { "auth": "<jwt>", "uid": "7f1c..." } }
```

**Erfolg**

```json
{ "module": "admin", "code": 0, "data": { "uid": "7f1c...", "name": "John Doe", "abbreviation": "jdoe" }, "errors": [] }
```

**Fehler – nicht gefunden**

```json
{
  "module": "admin",
  "code": -7,
  "errors": [{ "code": -1, "msg": "Failed to retrieve user: User with ID 7f1c... does not exist" }],
  "data": {}
}
```

### `create_user`

**Zweck:** Nutzer anlegen.

**Request**

```json
{
  "module": "admin",
  "function": "create_user",
  "data": { "auth": "<jwt>", "name": "John Doe", "abbreviation": "jdoe", "security": "supersecret" }
}
```

**Erfolg**

```json
{ "module": "admin", "code": 0, "data": { "uid": "7f1c...", "name": "John Doe", "abbreviation": "jdoe" }, "errors": [] }
```

**Fehler – Erstellung fehlgeschlagen**

```json
{
  "module": "admin",
  "code": -7,
  "errors": [{ "code": -2, "msg": "Failed to create user!" }],
  "data": {}
}
```

### `update_user`

**Zweck:** Nutzer aktualisieren (`name`, `abbreviation`, `security` optional).

**Request (nur Name ändern)**

```json
{ "module": "admin", "function": "update_user", "data": { "auth": "<jwt>", "uid": "7f1c...", "name": "John X. Doe" } }
```

**Erfolg**

```json
{ "module": "admin", "code": 0, "data": { "uid": "7f1c...", "name": "John X. Doe", "abbreviation": "jdoe" }, "errors": [] }
```

**Fehler – keine Schreibrechte**

```json
{ "module": "admin", "code": -5, "errors": [{ "code": -2, "msg": "Not authorized to update user data!" }], "data": {} }
```

### `delete_user`

**Zweck:** Nutzer löschen.

**Request**

```json
{ "module": "admin", "function": "delete_user", "data": { "auth": "<jwt>", "uid": "7f1c..." } }
```

**Erfolg**

```json
{ "module": "admin", "code": 0, "data": {}, "errors": [] }
```

**Fehler – SQL/Modulfehler**

```json
{ "module": "admin", "code": -7, "errors": [{ "code": -3, "msg": "Failed to delete user: [<sqlcode>]<message>" }], "data": {} }
```

### `list_groups`

**Zweck:** Alle Gruppen auflisten.

**Request**

```json
{ "module": "admin", "function": "list_groups", "data": { "auth": "<jwt>" } }
```

**Erfolg**

```json
{
  "module": "admin",
  "code": 0,
  "data": {
    "users": [
      { "uid": "g1...", "name": "Admins", "abbreviation": "admin", "description": "Administrators" }
    ]
  },
  "errors": []
}
```

> **Achtung (Bug im Modell):** Das JSON‑Feld heißt **`users`**, enthält aber **Gruppen**.

**Fehler – keine Leserechte**

```json
{ "module": "admin", "code": -5, "errors": [{ "code": -2, "msg": "Not authorized to retrieve groups!" }], "data": {} }
```

### `get_group`

**Zweck:** Gruppe per `uid` holen.

**Request**

```json
{ "module": "admin", "function": "get_group", "data": { "auth": "<jwt>", "uid": "g1..." } }
```

**Erfolg**

```json
{ "module": "admin", "code": 0, "data": { "uid": "g1...", "name": "Admins", "abbreviation": "admin", "description": "Administrators" }, "errors": [] }
```

**Fehler – nicht gefunden**

```json
{ "module": "admin", "code": -7, "errors": [{ "code": -1, "msg": "Failed to retrieve group: Group with ID g1... does not exist" }], "data": {} }
```

### `create_group`

**Zweck:** Gruppe anlegen.

**Request**

```json
{ "module": "admin", "function": "create_group", "data": { "auth": "<jwt>", "name": "Editors", "abbreviation": "edit", "description": "Can edit content" } }
```

**Erfolg**

```json
{ "module": "admin", "code": 0, "data": { "uid": "g2...", "name": "Editors", "abbreviation": "edit", "description": "Can edit content" }, "errors": [] }
```

**Fehler – Erstellung fehlgeschlagen**

```json
{ "module": "admin", "code": -7, "errors": [{ "code": -2, "msg": "Failed to create group!" }], "data": {} }
```

### `update_group`

**Zweck:** Gruppe aktualisieren (`name`, `abbreviation`, `description` optional).

**Request**

```json
{ "module": "admin", "function": "update_group", "data": { "auth": "<jwt>", "uid": "g2...", "description": "Can edit and review content" } }
```

**Erfolg**

```json
{ "module": "admin", "code": 0, "data": { "uid": "g2...", "name": "Editors", "abbreviation": "edit", "description": "Can edit and review content" }, "errors": [] }
```

**Fehler – keine Schreibrechte**

```json
{ "module": "admin", "code": -5, "errors": [{ "code": -2, "msg": "Not authorized to update group!" }], "data": {} }
```

### `delete_group`

**Zweck:** Gruppe löschen.

**Request**

```json
{ "module": "admin", "function": "delete_group", "data": { "auth": "<jwt>", "uid": "g2..." } }
```

**Erfolg**

```json
{ "module": "admin", "code": 0, "data": {}, "errors": [] }
```

**Fehler – SQL/Modulfehler**

```json
{ "module": "admin", "code": -7, "errors": [{ "code": -3, "msg": "Failed to delete group: [<sqlcode>]<message>" }], "data": {} }
```

### `list_user_groups`

**Zweck:** **Gruppen‑Mitgliedschaften eines Users** auflisten.

**Request**

```json
{ "module": "admin", "function": "list_user_groups", "data": { "auth": "<jwt>", "uid": "7f1c..." } }
```

**Erfolg**

```json
{ "module": "admin", "code": 0, "data": { "groups": [ "g1...", "g2..." ] }, "errors": [] }
```

**Fehler – Token abgelaufen**

```json
{ "module": "admin", "code": -5, "errors": [{ "code": -1, "msg": "Session token expired!" }], "data": {} }
```

### `list_group_users`

**Zweck:** **Mitglieder (User‑UIDs) einer Gruppe** auflisten.

**Request**

```json
{ "module": "admin", "function": "list_group_users", "data": { "auth": "<jwt>", "uid": "g1..." } }
```

**Erfolg**

```json
{ "module": "admin", "code": 0, "data": { "users": [ "7f1c...", "ab42..." ] }, "errors": [] }
```

**Fehler – keine Leserechte**

```json
{ "module": "admin", "code": -5, "errors": [{ "code": -2, "msg": "Not authorized to retrieve group users!" }], "data": {} }
```

### `add_user_to_group`

**Zweck:** User zu Gruppe hinzufügen.

**Request**

```json
{ "module": "admin", "function": "add_user_to_group", "data": { "auth": "<jwt>", "user_uid": "7f1c...", "group_uid": "g1..." } }
```

**Erfolg**

```json
{ "module": "admin", "code": 0, "data": {}, "errors": [] }
```

**Fehler – keine Rechte**

```json
{ "module": "admin", "code": -5, "errors": [{ "code": -2, "msg": "Not authorized to link user to group!" }], "data": {} }
```

### `remove_user_from_group`

**Zweck:** User aus Gruppe entfernen.

> **Implementierungs‑Detail:** Die Methode heißt im Code `RemoveUserToGroup`, der **Funktionsname** nach außen ist korrekt `remove_user_from_group`.

**Request**

```json
{ "module": "admin", "function": "remove_user_from_group", "data": { "auth": "<jwt>", "user_uid": "7f1c...", "group_uid": "g1..." } }
```

**Erfolg**

```json
{ "module": "admin", "code": 0, "data": {}, "errors": [] }
```

**Fehler – keine Rechte**

```json
{ "module": "admin", "code": -5, "errors": [{ "code": -2, "msg": "Not authorized to remove user from group!" }], "data": {} }
```

---

## Modul: `fm` (File/Config‑Management)

**Provider‑Auswahl:** per Dateiendung (`.json`, `.yaml`, `.toml`, `.xml`).
**Pfad‑Ermittlung:** über DB‑Tabelle `ce_resource` (Namespace = `service`, Name = `config`, optional `://...`) oder Standardpfad im Programmverzeichnis (`<service>/<config>`).

### `get_list`

**Zweck:** Liste verfügbarer Konfigurationen für den User.

**Request**

```json
{ "module": "fm", "function": "get_list", "data": { "auth": "<jwt>" } }
```

**Erfolg**

```json
{
  "module": "fm",
  "code": 0,
  "data": {
    "configurations": [
      { "service": "web", "config": "appsettings.json" },
      { "service": "web", "config": "routes.yaml" }
    ]
  },
  "errors": []
}
```

**Fehler – allgemein**

```json
{ "module": "fm", "code": -1, "errors": [{ "code": -1, "msg": "Failed to read configuration: <Detail>" }], "data": {} }
```

### `get_config`

**Zweck:** Einzelne Konfiguration laden und als **Konfigbaum** zurückgeben.

**Request**

```json
{ "module": "fm", "function": "get_config", "data": { "auth": "<jwt>", "service": "web", "config": "appsettings.json" } }
```

**Erfolg (Struktur)**

```json
{
  "module": "fm",
  "code": 0,
  "data": {
    "config": "appsettings.json",
    "uid": "5f9d...",
    "items": [
      {
        "name": "Logging",
        "value": "",
        "type": "category",
        "children": [
          { "name": "Level", "value": "Information", "type": "string", "children": [], "meta": {} }
        ],
        "meta": {}
      }
    ]
  },
  "errors": []
}
```

**Fehler – keine Leserechte**

```json
{ "module": "fm", "code": -5, "errors": [{ "code": -2, "msg": "Not authorized to read configuration web:appsettings.json" }], "data": {} }
```

**Fehler – Provider unbekannt (Endung)**

```json
{ "module": "fm", "code": -3, "errors": [{ "code": -3, "msg": "No provider known to read .json-configurations" }], "data": {} }
```

### `write_config`

**Zweck:** Konfiguration speichern. Erwartet denselben Baum wie `get_config` liefert (inkl. `uid`).

**Request**

```json
{
  "module": "fm",
  "function": "write_config",
  "data": {
    "auth": "<jwt>",
    "service": "web",
    "config": "appsettings.json",
    "uid": "5f9d...",
    "items": [ /* vollständiger Baum */ ]
  }
}
```

**Erfolg**

```json
{ "module": "fm", "code": 0, "data": {}, "errors": [] }
```

**Fehler – keine Schreibrechte**

```json
{ "module": "fm", "code": -5, "errors": [{ "code": -2, "msg": "Not authorized to write configuration web:appsettings.json" }], "data": {} }
```

**Fehler – Provider‑Speicherfehler**

```json
{ "module": "fm", "code": -6, "errors": [{ "code": -2, "msg": "Failed to write configuration: [<code>]<message>" }], "data": {} }
```

---

## Unknown‑Request Beispiele (vom Dispatcher)

**Unbekanntes Modul**

```json
{ "module": "ce", "code": -2, "errors": [{ "code": -1, "msg": "foo is not a module!" }], "data": {} }
```

**Unbekannte Funktion**

```json
{ "module": "ce", "code": -2, "errors": [{ "code": -2, "msg": "admin.nope is not a function" }], "data": {} }
```

---

### Anmerkungen & bekannte Inkonsistenzen

* `admin.list_users`/`list_groups`: Elemente werden als **Tuples** erzeugt. Je nach Newtonsoft‑Version erscheinen Feldnamen ggf. als `Item1/Item2/...`, bedarf Tests.

---
