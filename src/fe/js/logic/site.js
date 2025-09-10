document.addEventListener("DOMContentLoaded", () => {
    const form = document.getElementById("configForm");
    const btnCancel = document.getElementById("btnCancel");
    const log = document.getElementById("log");
    const connectBtn = document.getElementById("connectBtn");
    const closeBtn = document.getElementById("closeBtn");
    const sendBtn = document.getElementById("sendBtn");
    const messageInput = document.getElementById("messageInput");

    function logMessage(msg) {
        if (!log) return;
        log.innerText += msg + "\n";
        log.scrollTop = log.scrollHeight;
    }

    if (form) {
        form.addEventListener("submit", async (e) => {
            e.preventDefault();
            errors.clearAllErrors?.(form);

            try {
                const response = await dataSender.sendForm(form);
                if (response.ok) {
                    logMessage("Formulardaten erfolgreich gesendet.");
                } else {
                    errors.setGlobalError(response.message || "Unbekannter Fehler");
                    logMessage("Fehler beim Senden des Formulars.");
                }
            } catch (err) {
                errors.setGlobalError("Fehler: " + err.message);
                logMessage("Formular-Exception: " + err.message);
            }
        });
    }

    if (btnCancel) {
        btnCancel.addEventListener("click", () => {
            form?.reset();
            errors.clearAllErrors?.(form);
            logMessage("Formular zurückgesetzt.");
        });
    }

    const btnNewFile = Array.from(document.querySelectorAll("button.btn-primary")).find(b => b.textContent.includes("Neue Datei erstellen"));
    const btnUpload = Array.from(document.querySelectorAll("button.btn-secondary")).find(b => b.textContent.includes("Datei hochladen"));
    const btnVersionen = Array.from(document.querySelectorAll("button.btn-info")).find(b => b.textContent.includes("Versionen"));
    const btnBearbeiten = Array.from(document.querySelectorAll("button.btn-primary")).find(b => b.textContent.includes("Bearbeiten"));

    if (btnNewFile) {
        btnNewFile.addEventListener("click", () => {
            dataSender.sendAction("newFile");
            logMessage("Neue Datei Aktion gesendet.");
        });
    }

    if (btnUpload) {
        btnUpload.addEventListener("click", () => {
            const fileInput = document.createElement("input");
            fileInput.type = "file";
            fileInput.onchange = () => {
                const file = fileInput.files[0];
                if (!file) return;
                const reader = new FileReader();
                reader.onload = () => {
                    dataSender.sendRaw({ action: "uploadFile", filename: file.name, content: reader.result });
                    logMessage(`Datei hochgeladen: ${file.name}`);
                };
                reader.readAsText(file);
            };
            fileInput.click();
        });
    }

    if (btnVersionen) {
        btnVersionen.addEventListener("click", () => {
            window.location.href = "versionen.html";
        });
    }

    if (btnBearbeiten) {
        btnBearbeiten.addEventListener("click", () => {
            logMessage("Bearbeiten gedrückt.");
        });
    }

    if (connectBtn && closeBtn && sendBtn && messageInput) {
        closeBtn.disabled = true;
        sendBtn.disabled = true;
        messageInput.disabled = true;

        connectBtn.addEventListener("click", () => {
            dataSender.connectWS();
            connectBtn.disabled = true;
            closeBtn.disabled = false;
            sendBtn.disabled = false;
            messageInput.disabled = false;
            logMessage("WebSocket Verbindung wird hergestellt...");
        });

        closeBtn.addEventListener("click", () => {
            dataSender.disconnectWS();
            connectBtn.disabled = false;
            closeBtn.disabled = true;
            sendBtn.disabled = true;
            messageInput.disabled = true;
            logMessage("WebSocket Verbindung getrennt.");
        });

        sendBtn.addEventListener("click", () => {
            const message = messageInput.value;
            dataSender.sendRaw(message);
        });
    }

    dataSender.onLog = logMessage;
});
