document.addEventListener("DOMContentLoaded", () => {
    const form = document.getElementById("configForm");
    const btnCancel = document.getElementById("btnCancel");
    const btnSubmit = document.getElementById("btnSubmit");
    const log = document.getElementById("log");
    const connectBtn = document.getElementById("connectBtn");
    const closeBtn = document.getElementById("closeBtn");
    const sendBtn = document.getElementById("sendBtn");
    const messageInput = document.getElementById("messageInput");
    const searchInput = document.getElementById("datei-suche");
    const dataTree = document.getElementById("data-tree");
    const selectedFileField = document.getElementById("selected-file");

    const jwtToken = window.currentJwtToken || "<<<jwt_token>>>";
    const serviceName = window.currentService || "defaultService";
    let selectedFileName = "test";

    //dataSender.connectWS("ws://localhost:8080");
	service.authenticate();

    function logMessage(msg) {
        if (!log) return;
        log.innerText += msg + "\n";
        log.scrollTop = log.scrollHeight;
    }

    async function sendRequest(functionName, extraData = {}) {
        //dataSender.connectWS("ws://localhost:8080");
        if (!service.ws || service.ws.readyState !== WebSocket.OPEN) {
            console.warn("❌ WebSocket nicht verbunden, Request abgebrochen.");
            return;
        }

		let token = localStorage.getItem( "authToken" );
        try {
            //sende req
            const fmRequest = {
                module: "fm",
                function: functionName,
                data: {
                    auth: token,
                    service: extraData.service || "defaultService",
                    config: extraData.config || "test",
                }
            };

            //dataSender.sendRaw(fmRequest);
			let fmResponse = service.sendRequest( fmRequest );

        } catch (err) {
            console.error("❌ Fehler beim Senden des Requests:", err);
        }
    }

    function initFileList() {
        if (!dataTree) return;
        const items = Array.from(dataTree.querySelectorAll("li"));

        items.forEach(li => {
            const fileBtn = li.querySelector(".file-item");
            const delBtn = li.querySelector(".delete-btn");
            if (!fileBtn) return;

            fileBtn.addEventListener("click", () => {
                if (selectedFileField) selectedFileField.value = fileBtn.dataset.filename;
                logMessage(`Datei ausgewählt: ${fileBtn.dataset.filename}`);

                sendRequest("get_config", {
                    service: "defaultService",
                    config: fileBtn.dataset.filename
                });
            });

            if (delBtn) {
                delBtn.addEventListener("click", e => {
                    e.stopPropagation();
                    if (confirm(`Datei "${fileBtn.dataset.filename}" löschen?`)) {
                        li.remove();
                        if (selectedFileField && selectedFileField.value === fileBtn.dataset.filename) {
                            selectedFileField.value = "";
                        }
                        logMessage(`Datei aus Liste entfernt: ${fileBtn.dataset.filename}`);

                        sendRequest("delete_config", {
                            service: "defaultService",
                            config: fileBtn.dataset.filename
                        });
                    }
                });
            }
        });
    }

    function initSearch() {
        if (!searchInput || !dataTree) return;
        searchInput.addEventListener("input", () => {
            const query = searchInput.value.toLowerCase();
            const items = Array.from(dataTree.querySelectorAll("li"));
            items.forEach(li => {
                const fileBtn = li.querySelector(".file-item");
                if (!fileBtn) return;

                const fileName = fileBtn.dataset.filename.toLowerCase();
                if (!query || fileName.includes(query)) {
                    li.style.display = "flex";
                    const text = `📄 ${fileBtn.dataset.filename}`;
                    if (query) {
                        const regex = new RegExp(`(${query.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')})`, "gi");
                        fileBtn.innerHTML = text.replace(regex, "<mark>$1</mark>");
                    } else {
                        fileBtn.textContent = text;
                    }
                } else {
                    li.style.display = "none";
                }
            });
        });
    }

    async function loadFileList() {
        if (!dataTree) return;
        try {
            const response = await fetch("/testfiles");
            if (!response.ok) throw new Error("Server nicht erreichbar");
            const files = await response.json();
            if (!Array.isArray(files) || files.length === 0) throw new Error("Keine Dateien vom Server");

            const ul = document.createElement("ul");
            ul.className = "file-list";

            files.forEach(filename => {
                const li = document.createElement("li");

                const fileBtn = document.createElement("button");
                fileBtn.className = "file-item";
                fileBtn.dataset.filename = filename;
                fileBtn.textContent = `📄 ${filename}`;

                const delBtn = document.createElement("button");
                delBtn.className = "delete-btn btn btn-sm btn-danger";
                delBtn.textContent = "×";
                delBtn.title = "Löschen";

                li.appendChild(fileBtn);
                li.appendChild(delBtn);
                ul.appendChild(li);
            });

            dataTree.innerHTML = "<h3>Dateien</h3>";
            dataTree.appendChild(ul);
        } catch (err) {
            console.warn("Server-Dateiliste konnte nicht geladen werden, verwende statische Liste");
            logMessage("Server-Dateiliste konnte nicht geladen werden, verwende statische Liste.");
        }

        initFileList();
        initSearch();
    }

    if (form && btnSubmit) {
        btnSubmit.addEventListener("click", async (e) => {
            e.preventDefault();

            try {
                const configName = form.dataset.config || selectedFileField.value;

                const items = {
                    booleanOption: document.getElementById("boolean-option")?.checked,
                    serverName: document.getElementById("string-field")?.value,
                    port: parseInt(document.getElementById("number-picker")?.value, 10),
                    validUntil: document.getElementById("date-picker")?.value
                };

                const requestData = {
                    service: "defaultService",
                    config: configName,
                    items: items,
                    validate: true
                };

                await sendRequest("write_config", requestData);
                logMessage(`Konfigurationsdatei "${configName}" wurde gespeichert.`);
            } catch (err) {
                console.error("❌ Fehler beim Speichern:", err);
                logMessage(`❌ Fehler beim Speichern der Datei: ${err.message || err}`);
            }

        });
    }

    if (btnCancel) {
        btnCancel.addEventListener("click", () => {
            logMessage("Aktion abbrechen...");
            form?.reset();
            logMessage("Aktion abgebrochen.");
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
            sendRequest("write_config", {
                service: "defaultService",
                config: configName,
                items: items,
                validate: true
            })
        });
    }

    const btnNewFile = Array.from(document.querySelectorAll("button.btn-primary")).find(b => b.textContent.includes("Neue Datei erstellen"));
    const btnUpload = Array.from(document.querySelectorAll("button.btn-secondary")).find(b => b.textContent.includes("Datei hochladen"));
    const btnVersionen = Array.from(document.querySelectorAll("button.btn-info")).find(b => b.textContent.includes("Versionen"));
    const btnBearbeiten = Array.from(document.querySelectorAll("button.btn-primary")).find(b => b.textContent.includes("Bearbeiten"));

    if (btnNewFile) btnNewFile.addEventListener("click", () => {
        const newConfigName = "newConfig.json";

        sendRequest("new_config", {
            service: "defaultService", 
            config: "newConfig.json"
        });

        logMessage(`Neue Konfigurationsdatei angelegt: ${newConfigName}`);
    });

    if (btnUpload) btnUpload.addEventListener("click", () => {
        const fileInput = document.createElement("input");
        fileInput.type = "file";
        fileInput.onchange = () => {
            const file = fileInput.files[0];
            if (!file) return;
            const reader = new FileReader();
            reader.onload = () => {
                const content = reader.result;

                sendRequest("upload_config", {
                    service: "defaultService",
                    config: file.name,
                    content: content
                });

                logMessage(`Datei hochgeladen: ${file.name}`);

                const ul = dataTree.querySelector("ul.file-list");
                if (ul) {
                    const li = document.createElement("li");
                    const fileBtn = document.createElement("button");
                    fileBtn.className = "file-item";
                    fileBtn.dataset.filename = file.name;
                    fileBtn.textContent = `📄 ${file.name}`;

                    const delBtn = document.createElement("button");
                    delBtn.className = "delete-btn btn btn-sm btn-danger";
                    delBtn.textContent = "×";
                    delBtn.title = "Löschen";

                    li.appendChild(fileBtn);
                    li.appendChild(delBtn);
                    ul.appendChild(li);

                    initFileList(); 
                }
            };
            reader.readAsText(file);
        };
        fileInput.click();
    });

    if (btnBearbeiten) {
        btnBearbeiten.addEventListener("click", () => {
            const configName = form?.dataset.config || selectedFileField?.value;

            const items = {
                booleanOption: document.getElementById("boolean-option")?.checked,
                serverName: document.getElementById("string-field")?.value,
                port: parseInt(document.getElementById("number-picker")?.value, 10),
                validUntil: document.getElementById("date-picker")?.value
            };

            const requestData = {
                service: "defaultService",
                config: configName,
                items: items,
                validate: true
            };

            sendRequest("write_config", requestData);
            logMessage(`Konfigurationsdatei "${configName}" wurde über "Bearbeiten" gespeichert.`);
        });
    }

    if (btnVersionen) btnVersionen.addEventListener("click", () => window.location.href = "versionen.html");

    loadFileList();
    dataSender.onLog = logMessage;
});
