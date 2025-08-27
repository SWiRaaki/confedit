function detectType(value) {
    if (value === "true" || value === "false") return "bool";
    if (!isNaN(value) && value.toString().indexOf('.') === -1) return "integer";
    if (!isNaN(value) && value.toString().indexOf('.') !== -1) return "float";
    if (/\d{4}-\d{2}-\d{2}/.test(value)) return "datetime";
    if (Array.isArray(value)) return "list";
    return "string";
}

function createItem(name, value, type = "string", children = [], meta = {}) {
    return {
        name: String(name),
        value: String(value),
        type: type,
        children: children,
        meta: meta
    };
}

function buildConfigRequest(configName, uid, items, moduleName, functionName) {
    return {
        module: moduleName,
        function: functionName,
        data: {
            config: configName,
            uid: uid,
            Items: items
        }
    };
}

function sendConfig(ws, request) {
    if (ws.readyState === WebSocket.OPEN) {
        ws.send(JSON.stringify(request));
        console.log("Request gesendet:", request);
    } else {
        console.error("Sender-WS nicht verbunden. Request nicht gesendet:", request);
        alert("Senden fehlgeschlagen: Keine Verbindung zum Backend-WebSocket.");
    }
}

function buildTreeFromForm(form, meta = {}) {
    const items = [];
    const formData = new FormData(form);

    for (const [key, value] of formData.entries()) {
        const type = detectType(value);
        items.push(createItem(key, value, type, [], meta));
    }

    return items;
}

document.addEventListener("DOMContentLoaded", () => {
    const form = document.querySelector("#configForm");
    if (!form) return;

    const ws = new WebSocket("ws://localhost:8080");

    ws.onopen = () => console.log("WebSocket verbunden");
    ws.onerror = (err) => console.error("WebSocket Fehler:", err);
    ws.onmessage = (msg) => {
        try {
            const data = JSON.parse(msg.data);
            console.log("Antwort vom Server (JSON):", data);
        } catch {
            console.log("Antwort vom Server (Text):", msg.data);
        }
    };

    form.addEventListener("submit", (e) => {
        e.preventDefault();

        const items = buildTreeFromForm(form);
        const moduleName = form.dataset.module || "defaultModule";
        const functionName = form.dataset.function || "defaultFunction";

        const request = buildConfigRequest(
            form.dataset.config || "myConfig",
            crypto.randomUUID(),
            items,
            moduleName,
            functionName
        );

        sendConfig(ws, request);
    });
});
