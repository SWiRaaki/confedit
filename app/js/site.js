document.addEventListener("DOMContentLoaded", () => {
    const form = document.getElementById("configForm");
    const btnCancel = document.getElementById("btnCancel");
    const btnSubmit = document.getElementById("btnSubmit");
    const logBox = document.getElementById("log");
    const connectBtn = document.getElementById("connectBtn");
    const closeBtn = document.getElementById("closeBtn");
    const sendBtn = document.getElementById("sendBtn");
    const messageInput = document.getElementById("messageInput");
    const searchInput = document.getElementById("datei-suche");
    const dataTree = document.getElementById("data-tree");
    const selectedFileField = document.getElementById("selected-file");

    const serviceName = window.currentService || "defaultService";

    service.authenticate();

    function logMessage(msg) {
        if (!logBox) return;
        logBox.innerText += msg + "\n";
        logBox.scrollTop = logBox.scrollHeight;
    }
    dataSender.onLog = logMessage;

    // fm.get_list
    async function loadFileList() {
        if (!dataTree) return;
        try {
            const resp = await service.sendRequest({
                module: "fm",
                function: "get_list",
                data: {}
            });

            if (!resp || !resp.data) {
                logMessage("⚠️ Keine Dateien gefunden.");
                return;
            }

            const ul = document.createElement("ul");
            ul.className = "file-list";

            resp.data.forEach(filename => {
                const li = document.createElement("li");

                const fileBtn = document.createElement("button");
                fileBtn.className = "file-item";
                fileBtn.dataset.filename = filename;
                fileBtn.textContent = `📄 ${filename}`;

                const delBtn = document.createElement("button");
                delBtn.className = "delete-btn btn btn-sm btn-danger";
                delBtn.textContent = "×";
                delBtn.title = "Löschen";

                fileBtn.addEventListener("click", () => loadFileConfig(filename));

                delBtn.addEventListener("click", e => {
                    e.stopPropagation();
                    if (confirm(`Datei "${filename}" löschen?`)) {
                        li.remove();
                        if (selectedFileField.value === filename) {
                            selectedFileField.value = "";
                        }
                        logMessage(`Datei gelöscht: ${filename}`);

                        service.sendRequest({
                            module: "fm",
                            function: "delete_config",
                            data: { service: serviceName, config: filename }
                        });
                    }
                });

                li.appendChild(fileBtn);
                li.appendChild(delBtn);
                ul.appendChild(li);
            });

            dataTree.innerHTML = "<h3>Dateien</h3>";
            dataTree.appendChild(ul);
        } catch (err) {
            logMessage("❌ Fehler beim Laden der Liste: " + err.message);
        }
		initSearch()
    }

    // fm.get_config
    async function loadFileConfig(filename) {
        try {
            const resp = await service.sendRequest({
                module: "fm",
                function: "get_config",
                data: { filename }
            });

            if (!resp || !resp.data) {
                logMessage(`⚠️ Keine Daten für Datei ${filename}`);
                return;
            }

            selectedFileField.value = filename;
            if (resp.data.serverName) {
                document.getElementById("string-field").value = resp.data.serverName;
            }
            if (resp.data.port) {
                document.getElementById("number-picker").value = resp.data.port;
            }
            if (resp.data.enabled !== undefined) {
                document.getElementById("boolean-option").checked = resp.data.enabled;
            }

            logMessage(`✅ Datei geladen: ${filename}`);
        } catch (err) {
            logMessage(`❌ Fehler beim Laden von ${filename}: ${err.message}`);
        }
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

    // fm.write_config
    if (form && btnSubmit) {
        btnSubmit.addEventListener("click", async (e) => {
            e.preventDefault();
            try {
                const configName = selectedFileField.value;
                const items = {
                    booleanOption: document.getElementById("boolean-option")?.checked,
                    serverName: document.getElementById("string-field")?.value,
                    port: parseInt(document.getElementById("number-picker")?.value, 10),
                    validUntil: document.getElementById("date-picker")?.value
                };

                await service.sendRequest({
                    module: "fm",
                    function: "write_config",
                    data: { service: serviceName, config: configName, items, validate: true }
                });

                logMessage(`✅ Konfigurationsdatei "${configName}" gespeichert.`);
            } catch (err) {
                logMessage(`❌ Fehler beim Speichern: ${err.message}`);
            }
        });
    }

    if (btnCancel) {
        btnCancel.addEventListener("click", () => {
            form?.reset();
            selectedFileField.value = "";
            logMessage("ℹ️ Formular zurückgesetzt.");
        });
    }

    // Connection test
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
            logMessage("WebSocket Verbindung hergestellt.");
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
            const msg = messageInput.value || '{"ping":"pong"}';
            dataSender.sendRaw(msg);
        });
    }

    // Action Handling
    const btnNewFile = Array.from(document.querySelectorAll("button.btn-primary"))
        .find(b => b.textContent.includes("Neue Datei erstellen"));
    if (btnNewFile) {
        btnNewFile.addEventListener("click", () => {
            const newConfigName = "newConfig.json";
            service.sendRequest({
                module: "fm",
                function: "new_config",
                data: { service: serviceName, config: newConfigName }
            });
            logMessage(`Neue Datei erstellt: ${newConfigName}`);
            loadFileList();
        });
    }

    const btnUpload = Array.from(document.querySelectorAll("button.btn-secondary"))
        .find(b => b.textContent.includes("Datei hochladen"));
    if (btnUpload) {
        btnUpload.addEventListener("click", () => {
            const fileInput = document.createElement("input");
            fileInput.type = "file";
            fileInput.onchange = () => {
                const file = fileInput.files[0];
                if (!file) return;
                const reader = new FileReader();
                reader.onload = () => {
                    service.sendRequest({
                        module: "fm",
                        function: "upload_config",
                        data: { service: serviceName, config: file.name, content: reader.result }
                    });
                    logMessage(`Datei hochgeladen: ${file.name}`);
                    loadFileList();
                };
                reader.readAsText(file);
            };
            fileInput.click();
        });
    }

    const btnVersionen = Array.from(document.querySelectorAll("button.btn-info"))
        .find(b => b.textContent.includes("Versionen"));
    if (btnVersionen) btnVersionen.addEventListener("click", () => window.location.href = "versionen.html");

    const btnBearbeiten = Array.from(document.querySelectorAll("button.btn-primary"))
        .find(b => b.textContent.includes("Bearbeiten"));
    if (btnBearbeiten) {
        btnBearbeiten.addEventListener("click", () => {
            const configName = selectedFileField.value;
            const items = {
                booleanOption: document.getElementById("boolean-option")?.checked,
                serverName: document.getElementById("string-field")?.value,
                port: parseInt(document.getElementById("number-picker")?.value, 10),
                validUntil: document.getElementById("date-picker")?.value
            };
            service.sendRequest({
                module: "fm",
                function: "write_config",
                data: { service: serviceName, config: configName, items, validate: true }
            });
            logMessage(`✅ Datei "${configName}" über 'Bearbeiten' gespeichert.`);
        });
    }

    // Init Files list
    loadFileList();
});
