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

    function logMessage(msg) {
        if (!log) return;
        log.innerText += msg + "\n";
        log.scrollTop = log.scrollHeight;
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
                fileBtn.className = "file-item btn btn-light";
                fileBtn.dataset.filename = filename;
                fileBtn.textContent = `📄 ${filename}`;
                fileBtn.style.cursor = "pointer";

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

    if (form) {
        form.addEventListener("submit", async e => {
            e.preventDefault();
            errors.clearAllErrors?.(form);
            try {
                const response = await dataSender.sendForm(form);
                if (response.ok) logMessage("Formulardaten erfolgreich gesendet.");
                else {
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
            logMessage("Aktion abbrechen...");
            form?.reset();
            errors.clearAllErrors?.(form);
            logMessage("Aktion abgebrochen.");
        });
    }

    if (form && btnSubmit) {
        btnSubmit.addEventListener("click", async e => {
            e.preventDefault();
            logMessage("Absenden...");
            try {
                const response = await dataSender.sendForm(form);
                if (response.ok) logMessage("Formulardaten erfolgreich gesendet.");
                else logMessage("Fehler beim Senden des Formulars.");
            } catch (err) {
                logMessage("Fehler beim Senden: " + err.message);
            }
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

    const btnNewFile = Array.from(document.querySelectorAll("button.btn-primary")).find(b => b.textContent.includes("Neue Datei erstellen"));
    const btnUpload = Array.from(document.querySelectorAll("button.btn-secondary")).find(b => b.textContent.includes("Datei hochladen"));
    const btnVersionen = Array.from(document.querySelectorAll("button.btn-info")).find(b => b.textContent.includes("Versionen"));
    const btnBearbeiten = Array.from(document.querySelectorAll("button.btn-primary")).find(b => b.textContent.includes("Bearbeiten"));

    if (btnNewFile) btnNewFile.addEventListener("click", () => {
        dataSender.sendAction("newFile");
        logMessage("Neue Datei Aktion gesendet.");
    });

    if (btnUpload) btnUpload.addEventListener("click", () => {
        const fileInput = document.createElement("input");
        fileInput.type = "file";
        fileInput.onchange = () => {
            const file = fileInput.files[0];
            if (!file) return;
            const reader = new FileReader();
            reader.onload = () => {
                dataSender.sendRaw({ action: "uploadFile", filename: file.name, content: reader.result });
                logMessage(`Datei hochgeladen: ${file.name}`);

                const ul = dataTree.querySelector("ul.file-list");
                if (ul) {
                    const li = document.createElement("li");
                    const fileBtn = document.createElement("button");
                    fileBtn.className = "file-item btn btn-light";
                    fileBtn.dataset.filename = file.name;
                    fileBtn.textContent = `📄 ${file.name}`;
                    fileBtn.style.cursor = "pointer";

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

    if (btnVersionen) btnVersionen.addEventListener("click", () => window.location.href = "versionen.html");
    if (btnBearbeiten) btnBearbeiten.addEventListener("click", () => logMessage("Bearbeitet."));

    loadFileList();
    dataSender.onLog = logMessage;
});
