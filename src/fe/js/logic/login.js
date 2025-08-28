Erweitere Frontend mit Login Funktion - name,abbreviation,security HTML MAIN_HTML button und JS in dataSender ev.
OPTION A: man loggt sich und dann kommt auf main seite!
A1 Pruefe benutzer aus Db und TODO
A2 Prüfe TOKEN validity und dann leute an mainseite

FE - 07 #29 besagt

@für die Anmeldung ist folgender Baustein verfügbar:

auth.login
Sende Authentifizierungsdaten und erhalte damit einen login token

Request
{
"module": "auth",
"function": "login",
"data": {
"user": "<<user_abbreviation>>",
"security": "<<user_security>>" // pw for example
}
}
Response
Success
{
"module": "auth",
"code": 0,
"data": {
"auth": "<<jwt_token>>"
},
"errors": null
}
Error
{
"module": "auth",
"code": -1,
"data": null,
"errors": [
{ "code": -1, "msg": "<<error_message>>" }, // code is the error code [<0]
...
]
}@
