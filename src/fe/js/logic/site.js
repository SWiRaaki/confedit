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
            try {
                const response = await dataSender.sendForm(form);
                if (response.ok) {
                    errors.clearGlobalError();
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
            errors.clearGlobalError();
            logMessage("Formular zurückgesetzt.");
        });
    }

    document.querySelectorAll("button[data-action]").forEach(btn => {
        btn.addEventListener("click", () => {
            const action = btn.dataset.action;
            dataSender.sendAction(action);
            logMessage(`Aktion ausgeführt: ${action}`);
        });
    });

    if (connectBtn && closeBtn && sendBtn && messageInput) {
        connectBtn.addEventListener("click", () => dataSender.connectWS());
        closeBtn.addEventListener("click", () => dataSender.disconnectWS());
        sendBtn.addEventListener("click", () => {
            const message = messageInput.value;
            dataSender.sendRaw(message);
        });
    }

    dataSender.onLog = logMessage;
});
