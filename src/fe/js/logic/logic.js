const express = require("express");
const cors = require("cors");
const WebSocket = require("ws");

const app = express();
const port = 3000;

app.use(express.json());
app.use(cors());

const wsServerUrl = "ws://localhost:42069";
let ws;

// todo
function test() { }