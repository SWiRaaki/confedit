// Err Handler example 10.08 - 11.08.25

(() => {
    // Validation rules
    const rules = {
        required: (val) => val.trim() !== "" || "Feld darf nicht leer sein.",
        minLength: (len) => (val) => val.length >= len || `Mindestens ${len} Zeichen.`,
        maxLength: (len) => (val) => val.length <= len || `Maximal ${len} Zeichen.`,
        ipv4: (val) => {
            const ipv4Regex = /^(25[0-5]|2[0-4]\d|[01]?\d\d?)(\.(25[0-5]|2[0-4]\d|[01]?\d\d?)){3}$/;
            return ipv4Regex.test(val) || "Ungültige IPv4-Adresse.";
        },
        ipv6: (val) => {
            const ipv6Regex = /^(([0-9a-f]{1,4}:){7}[0-9a-f]{1,4}|::1)$/i;
            return ipv6Regex.test(val) || "Ungültige IPv6-Adresse.";
        },
        checkPortServer: async (val) => {
            const res = await sendServerCheck({ type: "checkPort", port: parseInt(val, 10) });
            return res.valid || res.message || "Serverfehler.";
        }
    };

    // UI 
    function setError(el, message) {
        let hint = el.parentNode.querySelector(".hint");
        if (!hint) {
            hint = document.createElement("div");
            hint.className = "hint";
            el.parentNode.appendChild(hint);
        }
        hint.textContent = message || "";
        hint.className = "hint " + (message ? "error" : "ok");
    }

    function clearError(el) {
        const hint = el.parentNode.querySelector(".hint");
        if (hint) {
            hint.textContent = "";
            hint.className = "hint";
        }
    }

    function setGlobalError(msg) {
        const container = document.getElementById("globalErrors");
        if (!container) return;
        container.textContent = msg || "";
        container.className = msg ? "errorBox" : "";
    }

    // WebSocket communication
    let socket;
    function initWebSocket() {
        socket = new WebSocket("ws://localhost:42069/");
        socket.onopen = () => console.log("WebSocket verbunden");
        socket.onerror = (e) => {
            console.error("WebSocket-Fehler:", e);
            setGlobalError("Serververbindung fehlgeschlagen.");
        };
        socket.onclose = () => {
            setGlobalError("Verbindung zum Server geschlossen.");
        };
    }

    function sendServerCheck(payload) {
        return new Promise((resolve) => {
            if (!socket || socket.readyState !== WebSocket.OPEN) {
                setGlobalError("Keine Verbindung zum Server für Validierung.");
                return resolve({ valid: false, message: "Keine Serververbindung" });
            }
            const id = Date.now().toString();
            payload._id = id;

            function listener(msg) {
                try {
                    const data = JSON.parse(msg.data);
                    if (data._id === id) {
                        socket.removeEventListener("message", listener);
                        resolve(data);
                    }
                } catch {
                    setGlobalError("Ungültige Antwort vom Server.");
                }
            }
            socket.addEventListener("message", listener);

            try {
                socket.send(JSON.stringify(payload));
            } catch {
                setGlobalError("Senden an den Server fehlgeschlagen.");
                resolve({ valid: false, message: "Sendefehler" });
            }
        });
    }

    // Validation 
    async function validate(el) {
        const val = el.value;
        const fieldRules = el.dataset.rules ? el.dataset.rules.split("|") : [];

        for (let rule of fieldRules) {
            let fn;
            if (rule.includes(":")) {
                const [name, arg] = rule.split(":");
                fn = rules[name] ? rules[name](isNaN(arg) ? arg : parseInt(arg, 10)) : null;
            } else {
                fn = rules[rule];
            }
            if (!fn) continue;

            let result = fn(val);
            if (result instanceof Promise) result = await result;
            if (result !== true) {
                setError(el, result);
                return false;
            }
        }
        clearError(el);
        return true;
    }

    // Init 
    function init() {
        initWebSocket();
        document.querySelectorAll("[data-rules]").forEach((el) => {
            el.addEventListener("input", () => validate(el));
            el.addEventListener("blur", () => validate(el));
        });
    }

    if (document.readyState !== "loading") init();
    else document.addEventListener("DOMContentLoaded", init);
    window.validateField = validate;
})();
