const ws = new WebSocket("ws://localhost:8080");

ws.onopen = () => {
    console.log("WebSocket (Sender) verbunden @8080");
};
ws.onerror = (e) => console.error("Sender-WS Fehler:", e);
ws.onclose = () => console.warn("Sender-WS geschlossen.");

function serializeForm(form) {
    const data = {};
    const elements = Array.from(form.elements).filter(el => el.name && !el.disabled);

    // hier keine einzelne felder auslesen, wir lesen und packen in json komplette datei! 
    for (const el of elements) {
        const name = el.name;
        const type = (el.type || "").toLowerCase();

        //if (type === "radio") {
        //    if (!el.checked) continue;
        //    data[name] = el.value;
        //    continue;
        //}

        //if (type === "checkbox") {
        //    data[name] = !!el.checked;
        //    continue;
        //}

        //if (type === "number") {
        //    if (el.value === "") { data[name] = null; }
        //    else {
        //        const num = Number(el.value);
        //        data[name] = Number.isFinite(num) ? num : null;
        //    }
        //    continue;
        //}

        //if (type === "date") {
        //    data[name] = el.value || null;
        //    continue;
        //}

        data[name] = el.value;
    }

    return data;
}

async function handleSubmit(event) {
    event.preventDefault();
    const form = event.target;

    const ruleEls = form.querySelectorAll("[data-rules]");
    for (const el of ruleEls) {
        const ok = await window.validateField(el);
        if (!ok) {
            console.warn("Validierung fehlgeschlagen bei:", el.name || el.id);
            return;
        }
    }

    const dataObj = serializeForm(form);
    const mod = form.dataset.module || "verifier";
    const fn = form.dataset.function || "verify_numeric";

    const request = {
        module: mod,
        function: fn,
        data: dataObj
    };
    if (ws.readyState === WebSocket.OPEN) {
        ws.send(JSON.stringify(request));
        console.log("Request gesendet:", request);
    } else {
        console.error("Sender-WS ist nicht verbunden (8080). Request nicht gesendet.", request);
        alert("Senden fehlgeschlagen: Keine Verbindung zum Backend-WebSocket (8080).");
    }
}

document.addEventListener("DOMContentLoaded", () => {
    const form = document.getElementById("configForm");
    if (form) {
        form.addEventListener("submit", handleSubmit);
        const cancel = document.getElementById("btnCancel");
        if (cancel) cancel.addEventListener("click", () => form.reset());
    }
});
