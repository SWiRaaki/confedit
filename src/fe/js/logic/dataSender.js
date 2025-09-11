class DataSender {
    constructor() {
        this.ws = null;
        this.onLog = null;
    }

    log(msg) {
        console.log(msg);
        if (this.onLog) this.onLog(msg);
    }

    connectWS(url = "ws://localhost:8080") {
        if (this.ws && this.ws.readyState === WebSocket.OPEN) {
            this.log("WebSocket ist bereits verbunden.");
            return;
        }

        this.ws = new WebSocket(url);

        this.ws.onopen = () => this.log("WebSocket verbunden.");
        this.ws.onerror = (err) => this.log("WebSocket Fehler: " + err);
        this.ws.onclose = () => this.log("WebSocket geschlossen.");
        this.ws.onmessage = (msg) => {
            try {
                const data = JSON.parse(msg.data);
                this.log("Server-Antwort (JSON): " + JSON.stringify(data));
                if (data.errors) this.handleServerErrors(data.errors);
            } catch {
                this.log("Server-Antwort (Text): " + msg.data);
            }
        };
    }

    disconnectWS() {
        if (!this.ws) return;
        this.ws.close();
        this.ws = null;
    }

    sendRaw(message) {
        if (!this.ws || this.ws.readyState !== WebSocket.OPEN) {
            this.log("WebSocket nicht verbunden. Nachricht nicht gesendet.");
            alert("Keine Verbindung zum Server.");
            return;
        }

        let payload = message;
        if (typeof message !== "string") {
            payload = JSON.stringify(message);
        }

        this.ws.send(payload);
        this.log("Gesendet: " + payload);
    }

    async sendForm(form) {
        if (!form) throw new Error("Kein Formular angegeben.");

        const formData = new FormData(form);
        const items = [];

        for (const [key, value] of formData.entries()) {
            let type = this.detectType(value);
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

        this.sendRaw(request);

        return { ok: true };
    }

    sendAction(actionName) {
        if (!actionName) return;
        this.sendRaw({ action: actionName });
        this.log("Aktion gesendet: " + actionName);
    }

    set onLogCallback(fn) {
        this.onLog = fn;
    }

    detectType(value) {
        if (value === "true" || value === "false") return "bool";
        if (!isNaN(parseInt(value, 10)) && Number.isInteger(+value)) return "integer";
        if (!isNaN(parseFloat(value))) return "float";
        return "string";
    }

    handleServerErrors(errors) {
        errors.forEach(err => this.log("Server-Fehler: " + err.msg));
    }
}

const dataSender = new DataSender();
