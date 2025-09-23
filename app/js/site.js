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
    const btnAddField = document.querySelector("#btnAddField");

    const serviceName = window.currentService || "test";

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

                delBtn.addEventListener("click", async e => {
                    e.stopPropagation();

                    if (!confirm(`Datei "${filename}" wirklich löschen?`)) return;

                    try {
                        const response = await service.sendRequest({
                            module: "fm",
                            function: "delete_config",
                            data: {
                                auth: localStorage.getItem("authToken"),
                                service: serviceName,
                                config: filename
                            }
                        });

                        if (response && response.code === 0) {
                            li.remove();
                            if (selectedFileField.value === filename) selectedFileField.value = "";
                            logMessage(`✅ Datei "${filename}" erfolgreich gelöscht.`);
                        } else if (response?.code === 1 && response.errors?.[0]?.msg?.includes("Not authorized")) {
                            logMessage(`⚠️ Sie haben keine Berechtigung zum Löschen von "${filename}".`);
                        } else {
                            logMessage(
                                `❌ Fehler beim Löschen von "${filename}": ${response?.errors?.[0]?.msg || "Unbekannter Fehler"
                                }`
                            );
                        }
                    } catch (err) {
                        logMessage(`❌ Fehler beim Löschen von "${filename}": ${err.message}`);
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
            const existingDynamicFields = formContainer.querySelectorAll('.dynamic-field');
            existingDynamicFields.forEach(field => field.remove());

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
        if (!items || !Array.isArray(items)) return;

        items.forEach(item => {
            if (!item || !item.name) return;

            if (item.type === 'category' && item.children && item.children.length > 0) {
                // Create Category Fieldset
                const categoryFieldset = document.createElement('fieldset');
                categoryFieldset.className = 'form-group dynamic-field category-fieldset';
                categoryFieldset.innerHTML = `
                <legend class="text-secondary">${item.name}</legend>
            `;

                // Add fields to the same category fieldset
                generateFormFields(item.children, categoryFieldset);
                container.appendChild(categoryFieldset);
            } else {
                // Create individual field
                const fieldDiv = document.createElement('div');
                fieldDiv.className = 'form-group dynamic-field';
                const fieldId = `field-${item.name.toLowerCase().replace(/\s+/g, '-')}`;
                let fieldHTML = '';

                switch (item.type) {
                    case 'string':
                        fieldHTML = `
                        <label for="${fieldId}">${item.name}:</label>
                        <input type="text" id="${fieldId}" name="${fieldId}" class="form-control" value="${item.value || ''}">
                    `;
                        break;

                    case 'integer':
                        fieldHTML = `
                        <label for="${fieldId}">${item.name}:</label>
                        <input type="number" id="${fieldId}" name="${fieldId}" class="form-control" value="${item.value || ''}">
                    `;
                        break;

                    case 'bool':
                        fieldHTML = `
                        <div class="form-check">
                            <input type="checkbox" id="${fieldId}" name="${fieldId}" class="form-check-input" ${item.value ? 'checked' : ''}>
                            <label for="${fieldId}" class="form-check-label">${item.name}</label>
                        </div>
                    `;
                        break;

                    case 'datetime':
                        const dateValue = item.value ? new Date(item.value).toISOString().slice(0, 16) : '';
                        fieldHTML = `
                        <label for="${fieldId}">${item.name}:</label>
                        <input type="datetime-local" id="${fieldId}" name="${fieldId}" class="form-control" value="${dateValue}">
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

    function toCamelCase(str) {
        return str.replace(/(?:^\w|[A-Z]|\b\w)/g, (word, index) =>
            index === 0 ? word.charAt(0).toLowerCase() : word.charAt(0).toUpperCase()
        ).replace(/\s+/g, '');
    }

    function collectDivField(div, parentObj) {
        const input = div.querySelector('input, select, textarea');
        if (!input || !input.id || !input.id.startsWith('field-')) return;

        const fieldName = toCamelCase(input.id.replace('field-', ''));
        let value;
        let type = 'string';

        switch (input.type) {
            case 'checkbox':
                value = input.checked ? "true" : "false";
                type = 'bool';
                break;
            case 'number':
                if (input.step && input.step.includes('.')) {
                    value = parseFloat(input.value).toString();
                    type = 'float';
                } else {
                    value = parseInt(input.value, 10).toString();
                    type = 'integer';
                }
                break;
            case 'datetime-local':
                value = input.value || "";
                type = 'datetime';
                break;
            default:
                value = input.value || "";
                type = 'string';
        }

        parentObj.children.push({
            name: fieldName,
            value: value,
            type: type,
            children: [],
            meta: {}
        });
    }

    function collectFieldCategory(fieldset, parentObj, parentName = null) {
        const legend = fieldset.querySelector('legend');
        const categoryName = toCamelCase(legend ? legend.textContent.trim() : 'category');

        const categoryObj = {
            name: categoryName,
            type: 'category',
            value: "",
            children: [],
            meta: parentName ? { ParentName: parentName } : {}
        };

        Array.from(fieldset.children).forEach(child => {
            if (child.tagName === 'FIELDSET') {
                collectFieldCategory(child, categoryObj, categoryName);
            } else if (child.classList.contains('form-group')) {
                collectDivField(child, categoryObj);
            }
        });

        parentObj.children.push(categoryObj);
    }

    // collect data before edited
    function collectFormData() {
        const formContainer = document.querySelector('#configForm');
        if (!formContainer) return { data: { config: "", uid: crypto.randomUUID(), items: [] } };
        const configName = selectedFileField.value;
        const rootObj = { children: [] };

        Array.from(formContainer.children).forEach(child => {
            if (child.tagName === 'FIELDSET') {
                collectFieldCategory(child, rootObj);
            } else if (child.classList.contains('form-group')) {
                collectDivField(child, rootObj);
            }
        });

        return { data: { config: configName, uid: crypto.randomUUID(), items: rootObj.children } };
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

    // read uploaded file
    function readFileAsText(file) {
        return new Promise((resolve, reject) => {
            const reader = new FileReader();
            reader.onload = () => resolve(reader.result);
            reader.onerror = reject;
            reader.readAsText(file);
        });
    }

    const btnUpload = Array.from(document.querySelectorAll("button.btn-secondary"))
        .find(b => b.textContent.includes("Datei hochladen"));
    if (btnUpload) {
        btnUpload.addEventListener("click", async () => {
            const fileInput = document.createElement("input");
            fileInput.type = "file";

            fileInput.onchange = async () => {
                const file = fileInput.files[0];
                if (!file) return;
                try {
                    const content = await readFileAsText(file);

                    const response = await service.sendRequest({
                        module: "fm",
                        function: "create_config",
                        data: {
                            auth: localStorage.getItem("authToken"),
                            service: serviceName,
                            config: file.name,
                            content
                        }
                    });

                    if (response && response.code === 0) {
                        logMessage(`✅ Datei "${file.name}" erfolgreich hochgeladen.`);
                        loadFileList();
                    } else {
                        logMessage(`❌ Fehler beim Hochladen: ${response?.errors?.[0]?.msg || 'Unbekannter Fehler'}`);
                    }
                } catch (err) {
                    logMessage(`❌ Fehler beim Hochladen der Datei: ${err.message}`);
                }
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
            const data = collectFormData(); // Collect all fields -> extended fields too

            //const items = {
            //    booleanOption: document.getElementById("boolean-option")?.checked,
            //    serverName: document.getElementById("string-field")?.value,
            //    port: parseInt(document.getElementById("number-picker")?.value, 10),
            //    validUntil: document.getElementById("date-picker")?.value
            //};

            try {
                const response = await service.sendRequest({
                    module: "fm",
                    function: "write_config",
                    data: {
                        auth: localStorage.getItem("authToken"),
                        service: serviceName,
                        config: configName,
                        items: data.items, 
                        validate: true
                    }
                });

                const code = response?.code;
                const errors = response?.errors;

                if (response && code === 0) {
                    logMessage(`✅ Datei "${configName}" über 'Bearbeiten' gespeichert.`);
                } else {
                    const errorDetails = errors && errors.length > 0
                        ? errors.map(e => `[${e.code}] ${e.msg}`).join("; ")
                        : "Unbekannter Fehler";
                    logMessage(`❌ Fehler beim Speichern von "${configName}": Code=${code}, Fehler=${errorDetails}`);
                }
            } catch (err) {
                logMessage(`❌ Fehler beim Speichern von "${configName}": ${err.message ?? err}`);
            }
        });
    }

    // add extended fields (supported field types are text, checkbox (bool), dateinput)
    btnAddField.addEventListener("click", () => {
        const dataTypes = ["Text", "Ja oder Nein", "Integer", "Unsigned", "Float", "Datum", "Bytes", "Kategorie", "Liste"];
        const backdrop = document.createElement('div');
        backdrop.style.position = 'fixed';
        backdrop.style.top = 0;
        backdrop.style.left = 0;
        backdrop.style.width = '100%';
        backdrop.style.height = '100%';
        backdrop.style.backgroundColor = 'rgba(0,0,0,0.5)';
        backdrop.style.display = 'flex';
        backdrop.style.justifyContent = 'center';
        backdrop.style.alignItems = 'center';
        backdrop.style.zIndex = 9999;

        const modal = document.createElement('div');
        modal.style.background = 'white';
        modal.style.padding = '1rem';
        modal.style.borderRadius = '5px';
        modal.style.minWidth = '300px';
        modal.style.maxWidth = '400px';

        const optionsHTML = dataTypes.map(t => `<option value="${t}">${t || "(leer)"}</option>`).join('');

        modal.innerHTML = `
                            <div class="modal-header">
                                <h3>Neues Feld hinzufügen</h3>
                            </div>
                            <div class="modal-body">
                                <div class="form-group">
                                    <label for="new-field-name">Feldname:</label>
                                    <input type="text" id="new-field-name" class="form-control" placeholder="">
                                </div>
                                <div class="form-group">
                                    <label for="new-field-type">Datentyp:</label>
                                    <select id="new-field-type" class="form-control">
                                        ${optionsHTML}
                                    </select>
                                </div>
                            </div>
                            <div class="modal-footer" style="margin-top:10px; text-align:right;">
                                <button type="button" id="cancel-field" class="btn btn-secondary">Abbrechen</button>
                                <button type="button" id="confirm-field" class="btn btn-primary">Hinzufügen</button>
                            </div>
                        `;

        backdrop.appendChild(modal);
        document.body.appendChild(backdrop);

        const nameInput = modal.querySelector('#new-field-name');
        const typeSelect = modal.querySelector('#new-field-type');
        const cancelBtn = modal.querySelector('#cancel-field');
        const confirmBtn = modal.querySelector('#confirm-field');

        nameInput.focus();

        cancelBtn.addEventListener('click', () => {
            document.body.removeChild(backdrop);
        });

        confirmBtn.addEventListener('click', () => {
            const fieldName = nameInput.value.trim();
            const fieldType = typeSelect.value;

            if (!fieldName) {
                alert('Bitte einen Feldnamen eingeben!');
                return;
            }

            const formContainer = document.querySelector('#configForm fieldset');
            const fieldId = `field-${fieldName.toLowerCase().replace(/\s+/g, '-')}`;

            const fieldDiv = document.createElement("div");
            fieldDiv.className = "form-group d-flex align-items-center";
            fieldDiv.style.gap = "0.5rem";

            let inputElement = '';
            switch (fieldType) {
                case 'Ja oder Nein':
                    inputElement = `
                    <div class="form-check" style="flex:1;">
                        <input type="checkbox" id="${fieldId}" name="${fieldId}" class="form-check-input">
                        <label for="${fieldId}" class="form-check-label">${fieldName}</label>
                    </div>`;
                    break;
                case 'Datum':
                    inputElement = `
                    <div style="flex:1;">
                        <label for="${fieldId}">${fieldName}:</label>
                        <input type="date" id="${fieldId}" name="${fieldId}" class="form-control">
                    </div>`;
                    break;
                case 'Integer':
                    inputElement = `
                    <div style="flex:1;">
                        <label for="${fieldId}">${fieldName}:</label>
                        <input type="number" id="${fieldId}" name="${fieldId}" class="form-control">
                    </div>`;
                    break;
                case 'Unsigned':
                case 'Float':
                    inputElement = `
                    <div style="flex:1;">
                        <label for="${fieldId}">${fieldName}:</label>
                        <input type="number" id="${fieldId}" name="${fieldId}" class="form-control">
                    </div>`;
                    break;
                default:
                    inputElement = `
                    <div style="flex:1;">
                    <label for="${fieldId}">${fieldName}:</label>
                    <input type="text" id="${fieldId}" name="${fieldId}" class="form-control">
                </div>`;
            }
            const deleteBtn = document.createElement("button");
            deleteBtn.type = "button";
            deleteBtn.className = "delete-user btn btn-outline-danger btn-sm";
            deleteBtn.innerText = "×";
            deleteBtn.title = "Feld löschen";
            deleteBtn.addEventListener("click", () => fieldDiv.remove());

            fieldDiv.innerHTML = inputElement;
            fieldDiv.appendChild(deleteBtn);

            formContainer.insertBefore(fieldDiv, btnAddField.parentElement);

            document.body.removeChild(backdrop);
        });

        backdrop.addEventListener('click', (e) => {
            if (e.target === backdrop) {
                document.body.removeChild(backdrop);
            }
        });
    });


    // Init Files list
    loadFileList();
});
