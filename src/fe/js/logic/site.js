document.addEventListener("DOMContentLoaded", () => {
    const log = document.getElementById("log");
    const connectBtn = document.getElementById("connectBtn");
    const closeBtn = document.getElementById("closeBtn");
    const sendBtn = document.getElementById("sendBtn");
    const messageInput = document.getElementById("messageInput");

    if (!connectBtn || !closeBtn || !sendBtn || !messageInput || !log) {
        return;
    }

    let socket = null;

    function logMessage(msg) {
        log.innerText += msg + "\n";
        log.scrollTop = log.scrollHeight;
    }

    function updateUI() {
        const connected = socket && socket.readyState === WebSocket.OPEN;
        connectBtn.disabled = connected;
        closeBtn.disabled = !connected;
        sendBtn.disabled = !connected;
        messageInput.disabled = !connected;
    }

    connectBtn.addEventListener("click", () => {
        if (socket && socket.readyState === WebSocket.OPEN) {
            logMessage("Bereits verbunden.");
            return;
        }
        socket = new WebSocket("ws://localhost:42069");

        socket.addEventListener("open", () => {
            logMessage("Mit WebSocket Server verbunden.");
            updateUI();
        });

        socket.addEventListener("message", (e) => {
            logMessage("Nachricht vom Server: " + e.data);
        });

        socket.addEventListener("close", () => {
            logMessage("WebSocket-Verbindung geschlossen.");
            socket = null;
            updateUI();
        });

        socket.addEventListener("error", () => {
            logMessage("WebSocket-Fehler aufgetreten.");
        });
    });

    closeBtn.addEventListener("click", () => {
        if (socket) {
            socket.close();
            logMessage("Verbindung wird geschlossen...");
        } else {
            logMessage("Keine aktive WebSocket-Verbindung.");
        }
    });

    sendBtn.addEventListener("click", () => {
        const message = messageInput.value;
        if (socket && socket.readyState === WebSocket.OPEN) {
            socket.send(message);
            logMessage("Gesendet: " + message);
        } else {
            logMessage("WebSocket ist nicht verbunden.");
        }
    });

    updateUI();
});
