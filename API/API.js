const express = require("express");
const cors = require("cors");
const WebSocket = require("ws");

const app = express();
const port = 3000;

app.use(express.json());
app.use(cors());

const wsServerUrl = "ws://localhost:42069";
let ws;

function connectWebSocket() {
    ws = new WebSocket(wsServerUrl);

    ws.on("open", () => {
        console.log("Connected to C# WebSocket server");
    });

    ws.on("message", (data) => {
        console.log("Received from server:", data.toString());
    });

    ws.on("close", () => {
        console.log("WebSocket closed. Reconnecting in 5s...");
        setTimeout(connectWebSocket, 5000);
    });

    ws.on("error", (err) => {
        console.error("WebSocket error:", err);
    });
}

connectWebSocket();

app.post("/send", (req, res) => {
    const message = req.body.message;
    if (ws && ws.readyState === WebSocket.OPEN) {
        ws.send(message);
        res.json({ status: "Message sent" });
    } else {
        res.status(500).json({ status: "WebSocket not connected" });
    }
});

app.listen(port, () => {
    console.log(`Server running on http://localhost:${port}`);
});