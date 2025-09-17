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
    const btnAddField = document.getElementById("btnAddField");

    const serviceName = window.currentService || "web";

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
                data: {
                    auth: localStorage.getItem("authToken"),
                    service: serviceName
                }
            });

            if (!resp || !resp.data || !resp.data.configurations) {
                logMessage("⚠️ Keine Dateien gefunden.");
                return;
            }

            const ul = document.createElement("ul");
            ul.className = "file-list";

            resp.data.configurations.forEach(config => {
                const filename = config.config;
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
                            data: { auth: localStorage.getItem("authToken"), service: serviceName, config: filename }
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
                data: {
                    auth: localStorage.getItem("authToken"),
                    service: serviceName,
                    config: filename
                }
            });

            if (!resp || !resp.data) {
                logMessage(`⚠️ Keine Daten für Datei ${filename}`);
                return;
            }

            selectedFileField.value = filename;

            // Clear existing form content
            const formContainer = document.querySelector('#configForm fieldset');
            const existingFields = formContainer.querySelectorAll('.form-group:not(:first-child)');
            existingFields.forEach(field => field.remove());

            // Generate form fields from config tree
            if (resp.data.items && Array.isArray(resp.data.items)) {
                generateFormFields(resp.data.items, formContainer);
            }

            logMessage(`✅ Datei geladen: ${filename}`);
        } catch (err) {
            logMessage(`❌ Fehler beim Laden von ${filename}: ${err.message}`);
        }
    }

    function generateFormFields(items, container) {
        items.forEach(item => {
            if (item.type === 'category' && item.children && item.children.length > 0) {
                // Create category section
                const categoryDiv = document.createElement('div');
                categoryDiv.className = 'form-group';
                categoryDiv.innerHTML = `
                    <legend class="text-secondary">${item.name}</legend>
                `;
                container.appendChild(categoryDiv);

                // Generate fields for children
                generateFormFields(item.children, container);
            } else {
                // Create form field based on type
                const fieldDiv = document.createElement('div');
                fieldDiv.className = 'form-group';

                const fieldId = `field-${item.name.toLowerCase().replace(/\s+/g, '-')}`;
                let fieldHTML = '';

                switch (item.type) {
                    case 'string':
                        fieldHTML = `
                            <label for="${fieldId}">${item.name}:</label>
                            <input type="text" id="${fieldId}" name="${fieldId}" class="form-control" value="${item.value || ''}">
                        `;
                        break;
                    case 'number':
                        fieldHTML = `
                            <label for="${fieldId}">${item.name}:</label>
                            <input type="number" id="${fieldId}" name="${fieldId}" class="form-control" value="${item.value || ''}">
                        `;
                        break;
                    case 'boolean':
                        fieldHTML = `
                            <div class="form-check">
                                <input type="checkbox" id="${fieldId}" name="${fieldId}" class="form-check-input" ${item.value ? 'checked' : ''}>
                                <label for="${fieldId}" class="form-check-label">${item.name}</label>
                            </div>
                        `;
                        break;
                    default:
                        fieldHTML = `
                            <label for="${fieldId}">${item.name}:</label>
                            <input type="text" id="${fieldId}" name="${fieldId}" class="form-control" value="${item.value || ''}">
                        `;
                }

                fieldDiv.innerHTML = fieldHTML;
                container.appendChild(fieldDiv);
            }
        });
    }

    function collectFormData() {
        const formContainer = document.querySelector('#configForm fieldset');
        const fields = formContainer.querySelectorAll('input, select, textarea');
        const data = {};

        fields.forEach(field => {
            if (field.id && field.id.startsWith('field-')) {
                const fieldName = field.id.replace('field-', '');
                let value = field.value;

                if (field.type === 'checkbox') {
                    value = field.checked;
                } else if (field.type === 'number') {
                    value = parseFloat(value) || 0;
                }

                data[fieldName] = value;
            }
        });

        return data;
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
                    data: { auth: localStorage.getItem("authToken"), service: serviceName, config: configName, items, validate: true }
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
        btnNewFile.addEventListener("click", async () => {
            const fileName = prompt("Bitte geben Sie den Namen der neuen Konfigurationsdatei ein:", "neue_konfiguration.json");
            if (!fileName) return;

            try {
                const response = await service.sendRequest({
                    module: "fm",
                    function: "create_config",
                    data: {
                        auth: localStorage.getItem("authToken"),
                        service: serviceName,
                        config: fileName
                    }
                });

                if (response && response.code === 0) {
                    logMessage(`✅ Neue Konfigurationsdatei "${fileName}" erfolgreich erstellt.`);
                    loadFileList();
                } else {
                    logMessage(`❌ Fehler beim Erstellen der Datei: ${response?.errors?.[0]?.msg || 'Unbekannter Fehler'}`);
                }
            } catch (error) {
                logMessage(`❌ Fehler beim Erstellen der Datei: ${error.message}`);
            }
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
                        data: { auth: localStorage.getItem("authToken"), service: serviceName, config: file.name, content: reader.result }
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
        btnBearbeiten.addEventListener("click", async () => {
            const configName = selectedFileField.value;
            const items = collectFormData(); // Collect all fields -> extended fields too

            //const items = {
            //    booleanOption: document.getElementById("boolean-option")?.checked,
            //    serverName: document.getElementById("string-field")?.value,
            //    port: parseInt(document.getElementById("number-picker")?.value, 10),
            //    validUntil: document.getElementById("date-picker")?.value
            //};

            service.sendRequest({
                module: "fm",
                function: "write_config",
                data: { auth: localStorage.getItem("authToken"), service: serviceName, config: configName, items, validate: true }
            });
            logMessage(`✅ Datei "${configName}" über 'Bearbeiten' gespeichert.`);
        });
    }

    // add extended fields (supported field types are text, checkbox (bool), dateinput)
    if (btnAddField) {
        btnAddField.addEventListener("click", () => {
            const formContainer = document.querySelector('#configForm fieldset');

            const fieldName = prompt("Name des neuen Feldes?");
            if (!fieldName) return;

            let fieldType = prompt("Feldtyp wählen: text | checkbox | date", "text");
            fieldType = (fieldType || "text").toLowerCase();

            const fieldId = `field-${fieldName.toLowerCase().replace(/\s+/g, '-')}`;

            const fieldDiv = document.createElement("div");
            fieldDiv.className = "form-group d-flex align-items-center";
            fieldDiv.style.gap = "0.5rem"; 

            let inputElement = "";
            switch (fieldType) {
                case "checkbox":
                    inputElement = `
                    <div class="form-check" style="flex:1;">
                        <input type="checkbox" id="${fieldId}" name="${fieldId}" class="form-check-input">
                        <label for="${fieldId}" class="form-check-label">${fieldName}</label>
                    </div>`;
                    break;

                case "date":
                    inputElement = `
                    <div style="flex:1;">
                        <label for="${fieldId}">${fieldName}:</label>
                        <input type="date" id="${fieldId}" name="${fieldId}" class="form-control">
                    </div>`;
                    break;

                case "text":
                default:
                    inputElement = `
                    <div style="flex:1;">
                        <label for="${fieldId}">${fieldName}:</label>
                        <input type="text" id="${fieldId}" name="${fieldId}" class="form-control">
                    </div>`;
                    break;
            }
            const deleteBtn = document.createElement("button");
            deleteBtn.type = "button";
            deleteBtn.className = "delete-user";
            deleteBtn.innerText = "×";
            deleteBtn.title = "Feld löschen";
            deleteBtn.addEventListener("click", () => {
                fieldDiv.remove();
            });

            fieldDiv.innerHTML = inputElement;
            fieldDiv.appendChild(deleteBtn);

            formContainer.insertBefore(fieldDiv, btnAddField.parentElement);
        });
    }

    // Init Files list
    loadFileList();
});
