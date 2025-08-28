const dataSender = (() => {
    let ws = null;
    let onLog = null;

    function log(msg) {
        console.log(msg);
        if (onLog) onLog(msg);
    }

    function connectWS(url = "ws://localhost:8080") {
        if (ws && ws.readyState === WebSocket.OPEN) {
            log("WebSocket ist bereits verbunden.");
            return;
        }

        ws = new WebSocket(url);

        ws.onopen = () => log("WebSocket verbunden.");
        ws.onerror = (err) => log("WebSocket Fehler: " + err);
        ws.onclose = () => log("WebSocket geschlossen.");
        ws.onmessage = (msg) => {
            try {
                const data = JSON.parse(msg.data);
                log("Server-Antwort (JSON): " + JSON.stringify(data));
                if (data.errors) handleServerErrors(data.errors);
            } catch {
                log("Server-Antwort (Text): " + msg.data);
            }
        };
    }

    function disconnectWS() {
        if (!ws) return;
        ws.close();
        ws = null;
    }

    function sendRaw(message) {
        if (!ws || ws.readyState !== WebSocket.OPEN) {
            log("WebSocket nicht verbunden. Nachricht nicht gesendet.");
            alert("Keine Verbindung zum Server.");
            return;
        }

        let payload = message;
        if (typeof message !== "string") {
            payload = JSON.stringify(message);
        }

        ws.send(payload);
        log("Gesendet: " + payload);
    }

    async function sendForm(form) {
        if (!form) throw new Error("Kein Formular angegeben.");

        const formData = new FormData(form);
        const items = [];

        for (const [key, value] of formData.entries()) {
            let type = detectType(value);
            let parsedValue = value;

            if (type === "bool") parsedValue = value === "true";
            else if (type === "integer") parsedValue = parseInt(value, 10);
            else if (type === "float") parsedValue = parseFloat(value);

            items.push({
                name: key,
                value: parsedValue,
                type: type,
                children: [],
                meta: {}
            });
        }

        const request = {
            module: form.dataset.module || "defaultModule",
            function: form.dataset.function || "defaultFunction",
            data: {
                config: form.dataset.config || "myConfig",
                uid: crypto.randomUUID(),
                Items: items
            }
        };

        sendRaw(request);

        return new Promise((resolve) => {
            resolve({ ok: true });
        });
    }

    function sendAction(actionName) {
        if (!actionName) return;
        sendRaw({ action: actionName });
        log("Aktion gesendet: " + actionName);
    }

    return {
        connectWS,
        disconnectWS,
        sendRaw,
        sendForm,
        sendAction,
        set onLog(fn) { onLog = fn; },
    };
})();
