document.addEventListener("DOMContentLoaded", () => {
    const form = document.getElementById("configForm");
    const btnCancel = document.getElementById("btnCancel");
    const logBox = document.getElementById("log");
    const connectBtn = document.getElementById("connectBtn");
    const closeBtn = document.getElementById("closeBtn");
    const sendBtn = document.getElementById("sendBtn");
    const messageInput = document.getElementById("messageInput");
    const searchInput = document.getElementById("datei-suche");
    const dataTree = document.getElementById("data-tree");
    const selectedFileField = document.getElementById("selected-file");
    const btnAddField = document.querySelector("#btnAddField");
    let showEditBtn = false;
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
                if (btnSubmit) btnSubmit.style.display = showEditBtn ? "inline-block" : "none";
                if (btnAddField) btnAddField.style.display = showEditBtn ? "inline-block" : "none";
                if (btnCancel) btnCancel.style.display = showEditBtn ? "inline-block" : "none";
                if (btnVersionen) btnVersionen.style.display = showEditBtn ? "inline-block" : "none";
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
                        const response = await retryWithBackoff(async () => {
                            return await service.sendRequest({
                                module: "fm",
                                function: "delete_config",
                                data: {
                                    auth: localStorage.getItem("authToken"),
                                    service: serviceName,
                                    config: filename
                                }
                            });
                        });

                        if (response && response.code === 0) {
                            li.remove();
                            if (selectedFileField.value === filename) selectedFileField.value = "";
                            logMessage(`✅ Datei "${filename}" erfolgreich gelöscht.`);
                        } else if (response?.code === 1 && response.errors?.[0]?.msg?.includes("Not authorized")) {
                            logMessage(`⚠️ Sie haben keine Berechtigung zum Löschen von "${filename}".`);
                        } else {
                            const errorMsg = response?.errors?.[0]?.msg || "Unbekannter Fehler";
                            if (errorMsg.includes('being used by another process')) {
                                logMessage(`❌ Fehler: Die Datei "${filename}" ist gesperrt. Bitte schließen Sie andere Programme, die diese Datei verwenden könnten.`);
                            } else {
                                logMessage(`❌ Fehler beim Löschen von "${filename}": ${errorMsg}`);
                            }
                        }
                    } catch (err) {
                        if (err.message?.includes('being used by another process')) {
                            logMessage(`❌ Fehler: Die Datei "${filename}" ist gesperrt. Bitte schließen Sie andere Programme, die diese Datei verwenden könnten.`);
                        } else {
                            logMessage(`❌ Fehler beim Löschen von "${filename}": ${err.message}`);
                        }
                    }
                });

                li.appendChild(fileBtn);
                li.appendChild(delBtn);
                ul.appendChild(li);
            });

            dataTree.innerHTML = "<h3>Dateien</h3>";
            dataTree.appendChild(ul);
            showEditBtn = false
        } catch (err) {
            logMessage("❌ Fehler beim Laden der Liste: " + err.message);
            showEditBtn = false
            if (btnSubmit) btnSubmit.style.display = showEditBtn ? "inline-block" : "none";
            if (btnAddField) btnAddField.style.display = showEditBtn ? "inline-block" : "none";
            if (btnCancel) btnCancel.style.display = showEditBtn ? "inline-block" : "none";
            if (btnVersionen) btnVersionen.style.display = showEditBtn ? "inline-block" : "none";
            selectedFileField.value = "";
        }
        if (btnSubmit) btnSubmit.style.display = showEditBtn ? "inline-block" : "none";
        if (btnAddField) btnAddField.style.display = showEditBtn ? "inline-block" : "none";
        if (btnCancel) btnCancel.style.display = showEditBtn ? "inline-block" : "none";
        if (btnVersionen) btnVersionen.style.display = showEditBtn ? "inline-block" : "none";
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

            const errorMessage = handleApiError(resp, `Laden von "${filename}"`);
            if (errorMessage) {
                logMessage(errorMessage);
                if (btnSubmit) btnSubmit.style.display = showEditBtn ? "inline-block" : "none";
                if (btnAddField) btnAddField.style.display = showEditBtn ? "inline-block" : "none";
                if (btnCancel) btnCancel.style.display = showEditBtn ? "inline-block" : "none";
                if (btnVersionen) btnVersionen.style.display = showEditBtn ? "inline-block" : "none";
                return;
            }

            if (!resp || !resp.data) {
                logMessage(`⚠️ Keine Daten für Datei ${filename}`);
                if (btnSubmit) btnSubmit.style.display = showEditBtn ? "inline-block" : "none";
                if (btnAddField) btnAddField.style.display = showEditBtn ? "inline-block" : "none";
                if (btnCancel) btnCancel.style.display = showEditBtn ? "inline-block" : "none";
                if (btnVersionen) btnVersionen.style.display = showEditBtn ? "inline-block" : "none";
                return;
            }

            selectedFileField.value = filename;

            // Clear existing form content
            const formContainer = document.querySelector('#configForm fieldset');
            const existingDynamicFields = formContainer.querySelectorAll('.dynamic-field');
            existingDynamicFields.forEach(field => field.remove());

            // Generate form fields from config tree
            // Handle both 'items' and 'Items' for backward compatibility
            const items = resp.data.items || resp.data.Items;
            if (items && Array.isArray(items)) {
                generateFormFields(items, formContainer);
                logMessage(`✅ Datei geladen: ${filename}`);
                showEditBtn = true
                if (btnSubmit) btnSubmit.style.display = showEditBtn ? "inline-block" : "none";
                if (btnAddField) btnAddField.style.display = showEditBtn ? "inline-block" : "none";
                if (btnCancel) btnCancel.style.display = showEditBtn ? "inline-block" : "none";
                if (btnVersionen) btnVersionen.style.display = showEditBtn ? "inline-block" : "none";
            } else {
                logMessage(`⚠️ Keine gültigen Konfigurationsdaten in ${filename}`);
                showEditBtn = false
                if (btnSubmit) btnSubmit.style.display = showEditBtn ? "inline-block" : "none";
                if (btnAddField) btnAddField.style.display = showEditBtn ? "inline-block" : "none";
                if (btnCancel) btnCancel.style.display = showEditBtn ? "inline-block" : "none"
                if (btnVersionen) btnVersionen.style.display = showEditBtn ? "inline-block" : "none";
            }
        } catch (err) {
            if (err.message?.includes('Error reading JToken from JsonReader')) {
                logMessage(`❌ Fehler: Die Datei "${filename}" enthält ungültiges JSON. Bitte überprüfen Sie die Datei.`);
            } else {
                logMessage(`❌ Fehler beim Laden von ${filename}: ${err.message}`);
            }
            showEditBtn = false
            if (btnSubmit) btnSubmit.style.display = showEditBtn ? "inline-block" : "none";
            if (btnAddField) btnAddField.style.display = showEditBtn ? "inline-block" : "none";
            if (btnCancel) btnCancel.style.display = showEditBtn ? "inline-block" : "none";
            if (btnVersionen) btnVersionen.style.display = showEditBtn ? "inline-block" : "none";
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

                // Add meta fields if present
                if (item.meta && Object.keys(item.meta).length > 0) {
                    Object.entries(item.meta).forEach(([key, value]) => {
                        const metaDiv = document.createElement('div');
                        metaDiv.className = 'form-group meta-field';
                        metaDiv.innerHTML = `
                            <label for="meta-${key}">${key} (Meta):</label>
                            <input type="text" id="meta-${key}" name="meta-${key}" class="form-control" value="${value}" data-meta-for="${item.name}">
                        `;
                        categoryFieldset.appendChild(metaDiv);
                    });
                }

                // Add fields to the same category fieldset
                generateFormFields(item.children, categoryFieldset);
                container.appendChild(categoryFieldset);
            } else {
                // Create individual field
                const fieldDiv = document.createElement('div');
                fieldDiv.className = 'form-group dynamic-field';
                const fieldId = `field-${item.name.replace(/\s+/g, '-')}`;
                let fieldHTML = '';

                switch (item.type) {
                    case 'bool':
                        fieldHTML = `
                        <div class="form-check">
                            <input type="checkbox" id="${fieldId}" name="${fieldId}" class="form-check-input" ntype="${item.type}" ${item.value === 'true' ? 'checked' : ''}>
                            <label for="${fieldId}" class="form-check-label">${item.name}</label>
                        </div>
                    `;
                        break;

                    case 'integer':
                        fieldHTML = `
                        <label for="${fieldId}">${item.name}:</label>
                        <input type="number" id="${fieldId}" name="${fieldId}" class="form-control" step="1" value="${item.value || ''}" ntype="${item.type}">
                    `;
                        break;

                    case 'unsigned':
                        fieldHTML = `
                        <label for="${fieldId}">${item.name}:</label>
                        <input type="number" id="${fieldId}" name="${fieldId}" class="form-control" step="1" min="0" value="${item.value || ''}" ntype="${item.type}">
                    `;
                        break;

                    case 'float':
                        fieldHTML = `
                        <label for="${fieldId}">${item.name}:</label>
                        <input type="number" id="${fieldId}" name="${fieldId}" class="form-control" step="any" value="${item.value || ''}" ntype="${item.type}">
                    `;
                        break;

                    case 'datetime':
                        const dateValue = item.value ? new Date(item.value).toISOString().slice(0, 16) : '';
                        fieldHTML = `
                        <label for="${fieldId}">${item.name}:</label>
                        <input type="datetime-local" id="${fieldId}" name="${fieldId}" class="form-control" value="${dateValue}" ntype="${item.type}">
                    `;
                        break;

                    case 'bytes':
                        fieldHTML = `
                        <label for="${fieldId}">${item.name}:</label>
                        <input type="file" id="${fieldId}" name="${fieldId}" class="form-control" accept="*/*" ntype="${item.type}">
                        ${item.value ? `<small class="form-text text-muted">Current: ${item.value}</small>` : ''}
                    `;
                        break;

                    case 'list':
                        // Convert children array to textarea value
                        const listValue = item.children && item.children.length > 0
                            ? item.children.map(child => child.value).join('\n')
                            : '';
                        fieldHTML = `
                        <label for="${fieldId}">${item.name} (one per line):</label>
                        <textarea id="${fieldId}" name="${fieldId}" class="form-control" rows="4" placeholder="Enter items, one per line" ntype="${item.type}">${listValue}</textarea>
                    `;
                        break;

                    default:
                        fieldHTML = `
                        <label for="${fieldId}">${item.name}:</label>
                        <input type="text" id="${fieldId}" name="${fieldId}" class="form-control" value="${item.value || ''}" ntype="${item.type}">
                    `;
                }

                fieldDiv.innerHTML = fieldHTML;
                container.appendChild(fieldDiv);
            }
        });
    }

    function collectDivField(div, parentObj) {
        const input = div.querySelector('input, select, textarea');
        if (!input || !input.id || !input.id.startsWith('field-')) return;

        const fieldName = input.id.replace('field-', '').replace(/-/g, ' ');
        const ntype = input.getAttribute('ntype') || 'string';
        let value;

        switch (ntype) {
            case 'bool':
                value = input.checked ? "true" : "false";
                break;
            case 'integer':
            case 'unsigned':
                value = input.value ? parseInt(input.value, 10).toString() : "0";
                break;
            case 'float':
                value = input.value ? parseFloat(input.value).toString() : "0.0";
                break;
            case 'datetime':
                value = input.value || "";
                break;
            case 'bytes':
                // Handle file input - would need additional file reading logic
                value = input.files && input.files[0] ? input.files[0].name : "";
                break;
            case 'list':
                // Convert textarea to children array structure
                const lines = input.value.split('\n').filter(line => line.trim());
                const listItem = {
                    name: fieldName,
                    value: "",
                    type: 'list',
                    children: lines.map((line, index) => ({
                        name: index.toString(),
                        value: line.trim(),
                        type: 'string',
                        children: [],
                        meta: {}
                    })),
                    meta: {}
                };
                parentObj.children.push(listItem);
                return; // Early return to avoid duplicate push
            default:
                value = input.value || "";
        }

        parentObj.children.push({
            name: fieldName,
            value: value,
            type: ntype,
            children: [],
            meta: {}
        });
    }

    function collectFieldCategory(fieldset, parentObj, parentName = null) {
        const legend = fieldset.querySelector('legend');
        const categoryNameRaw = legend ? legend.textContent.trim() : 'category';
        const skipHeader = categoryNameRaw === 'Konfiguration bearbeiten';
        const categoryName = skipHeader ? (parentName || 'category') : categoryNameRaw;

        const categoryObj = {
            name: categoryName,
            type: 'category',
            value: "",
            children: [],
            meta: parentName ? { ParentName: parentName } : {}
        };

        // Collect meta fields
        const metaFields = fieldset.querySelectorAll('.meta-field input');
        metaFields.forEach(metaInput => {
            if (metaInput.dataset.metaFor === categoryName) {
                const metaKey = metaInput.id.replace('meta-', '');
                categoryObj.meta[metaKey] = metaInput.value;
            }
        });

        Array.from(fieldset.children).forEach(child => {
            if (child.tagName === 'FIELDSET') {
                collectFieldCategory(child, categoryObj, categoryName);
            } else if (child.classList.contains('form-group') && !child.classList.contains('meta-field')) {
                collectDivField(child, categoryObj);
            }
        });

        if (!skipHeader) {
            parentObj.children.push(categoryObj);
        } else {
            parentObj.children.push(...categoryObj.children);
        }
    }


    // collect data before edited
    function collectFormData() {
        const formContainer = document.querySelector('#configForm');
        if (!formContainer) return {
            data: {
                config: "",
                uid: crypto.randomUUID(),
                Items: []
            }
        };

        const configName = selectedFileField.value;
        const rootObj = { children: [] };

        Array.from(formContainer.children).forEach(child => {
            if (child.tagName === 'FIELDSET') {
                collectFieldCategory(child, rootObj);
            } else if (child.classList.contains('form-group') && !child.classList.contains('meta-field')) {
                collectDivField(child, rootObj);
            }
        });

        return {
            data: {
                config: configName,
                uid: crypto.randomUUID(),
                Items: rootObj.children
            }
        };
    }

    function initSearch() {
        if (!searchInput || !dataTree) return;
        searchInput.addEventListener("input", () => {
            const query = searchInput.value;
            const items = Array.from(dataTree.querySelectorAll("li"));
            items.forEach(li => {
                const fileBtn = li.querySelector(".file-item");
                if (!fileBtn) return;

                const fileName = fileBtn.dataset.filename;
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


    // Helper function for retry logic with exponential backoff
    async function retryWithBackoff(operation, maxRetries = 3, baseDelay = 1000) {
        for (let attempt = 1; attempt <= maxRetries; attempt++) {
            try {
                return await operation();
            } catch (error) {
                const isFileLockError = error.message?.includes('being used by another process') ||
                    error.message?.includes('file is locked') ||
                    error.message?.includes('access denied');

                if (isFileLockError && attempt < maxRetries) {
                    const delay = baseDelay * Math.pow(2, attempt - 1);
                    logMessage(`⚠️ Datei gesperrt, versuche erneut in ${delay / 1000}s... (Versuch ${attempt}/${maxRetries})`);
                    await new Promise(resolve => setTimeout(resolve, delay));
                    continue;
                }
                throw error;
            }
        }
    }

    // Helper function to validate JSON data structure
    function validateJsonStructure(data) {
        try {
            if (!data || typeof data !== 'object') {
                return { valid: false, error: 'Daten sind leer oder ungültig' };
            }

            if (data.data && data.data.Items) {
                // Validate Items array
                if (!Array.isArray(data.data.Items)) {
                    return { valid: false, error: 'Items muss ein Array sein' };
                }

                // Validate each item has required properties
                for (let i = 0; i < data.data.Items.length; i++) {
                    const item = data.data.Items[i];
                    if (!item.name || typeof item.name !== 'string') {
                        return { valid: false, error: `Item ${i} hat keinen gültigen Namen` };
                    }
                    if (!item.type || typeof item.type !== 'string') {
                        return { valid: false, error: `Item ${i} hat keinen gültigen Typ` };
                    }
                    if (!Array.isArray(item.children)) {
                        return { valid: false, error: `Item ${i} hat keine gültigen children` };
                    }
                }
            }

            return { valid: true };
        } catch (error) {
            return { valid: false, error: `Validierungsfehler: ${error.message}` };
        }
    }

    // Helper function to handle API response errors
    function handleApiError(response, operation) {
        if (!response) {
            return `❌ ${operation}: Keine Antwort vom Server`;
        }

        const code = response.code;
        const errors = response.errors;

        if (code === 0) {
            return null; // Success
        }

        if (errors && errors.length > 0) {
            const errorMsg = errors[0].msg || 'Unbekannter Fehler';

            if (errorMsg.includes('Error reading JToken from JsonReader')) {
                return `❌ ${operation}: Die Konfigurationsdatei enthält ungültiges JSON. Bitte überprüfen Sie die Datei.`;
            } else if (errorMsg.includes('being used by another process')) {
                return `❌ ${operation}: Die Datei ist gesperrt. Bitte schließen Sie andere Programme, die diese Datei verwenden könnten.`;
            } else if (errorMsg.includes('Not authorized')) {
                return `⚠️ ${operation}: Sie haben keine Berechtigung für diese Aktion.`;
            } else if (errorMsg.includes('Failed to read configuration')) {
                return `❌ ${operation}: Fehler beim Lesen der Konfiguration. Die Datei ist möglicherweise beschädigt.`;
            } else {
                return `❌ ${operation}: ${errorMsg}`;
            }
        }

        return `❌ ${operation}: Unbekannter Fehler (Code: ${code})`;
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
            fileInput.accept = ".json,.xml,.yaml,.yml,.toml,.ini";

            fileInput.onchange = async () => {
                const file = fileInput.files[0];
                if (!file) return;

                try {
                    const content = await readFileAsText(file);

                    // Parse content based on file extension
                    let parsedData;
                    const extension = file.name.split('.').pop();

                    switch (extension) {
                        case 'json':
                            try {
                                parsedData = JSON.parse(content);
                                // Validate JSON structure if it's a configuration file
                                const validation = validateJsonStructure({ data: { Items: parsedData } });
                                if (!validation.valid) {
                                    logMessage(`❌ JSON-Validierungsfehler: ${validation.error}`);
                                    return;
                                }
                            } catch (jsonError) {
                                logMessage(`❌ Fehler: Die JSON-Datei "${file.name}" ist ungültig. ${jsonError.message}`);
                                return;
                            }
                            break;
                        default:
                            // For other formats, send as raw content for server-side parsing
                            parsedData = content;
                    }

                    const response = await retryWithBackoff(async () => {
                        return await service.sendRequest({
                            module: "fm",
                            function: "create_config",
                            data: {
                                auth: localStorage.getItem("authToken"),
                                service: serviceName,
                                config: file.name,
                                content: parsedData,
                                format: extension
                            }
                        });
                    });

                    const errorMessage = handleApiError(response, `Hochladen von "${file.name}"`);
                    if (errorMessage) {
                        logMessage(errorMessage);
                    } else {
                        logMessage(`✅ Datei "${file.name}" erfolgreich hochgeladen.`);
                        loadFileList();
                    }
                } catch (err) {
                    if (err.message?.includes('Error reading JToken from JsonReader')) {
                        logMessage(`❌ Fehler: Die Datei "${file.name}" enthält ungültiges JSON. Bitte überprüfen Sie die Datei.`);
                    } else {
                        logMessage(`❌ Fehler beim Hochladen der Datei: ${err.message}`);
                    }
                }
            };

            fileInput.click();
        });
    }

    const btnVersionen = Array.from(document.querySelectorAll("button.btn-info"))
        .find(b => b.textContent.includes("Versionen"));
    if (btnVersionen) btnVersionen.addEventListener("click", () => window.location.href = "versionen.html");
    const btnSubmit = Array.from(document.querySelectorAll("button.btn-success"))
        .find(b => b.textContent.includes("Absenden"));
    if (btnSubmit) {
        btnSubmit.addEventListener("click", async () => {
            event.preventDefault();

            const configName = selectedFileField.value;
            if (!configName) {
                logMessage("❌ Keine Datei ausgewählt.");
                return;
            }

            const collectedData = collectFormData();

            // Validate data structure before sending
            const validation = validateJsonStructure(collectedData);
            if (!validation.valid) {
                logMessage(`❌ Validierungsfehler: ${validation.error}`);
                return;
            }

            try {
                const response = await retryWithBackoff(async () => {
                    return await service.sendRequest({
                        module: "fm",
                        function: "write_config",
                        data: {
                            auth: localStorage.getItem("authToken"),
                            service: serviceName,
                            config: configName,
                            Items: collectedData.data.Items,  // Korrigiert: Items statt items
                            validate: true
                        }
                    });
                });

                const errorMessage = handleApiError(response, `Speichern von "${configName}"`);
                if (errorMessage) {
                    logMessage(errorMessage);
                } else {
                    logMessage(`✅ Datei "${configName}" erfolgreich gespeichert.`);
                }
            } catch (err) {
                if (err.message?.includes('being used by another process')) {
                    logMessage(`❌ Fehler: Die Datei "${configName}" ist gesperrt. Bitte schließen Sie andere Programme, die diese Datei verwenden könnten.`);
                } else {
                    logMessage(`❌ Fehler beim Speichern von "${configName}": ${err.message ?? err}`);
                }
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
            const fieldId = `field-${fieldName.normalize("NFD").replace(/[\u0300-\u036f]/g, "").replace(/\s+/g, "-").replace(/[^a-zA-Z0-9-]/g, "")}`;
            const fieldDiv = document.createElement("div");
            fieldDiv.className = "form-group d-flex align-items-center";
            fieldDiv.style.gap = "0.5rem";

            let inputElement = '';
            switch (fieldType) {
                case 'Ja oder Nein':
                    inputElement = `
                    <div class="form-check" style="flex:1;">
                        <input type="checkbox" id="${fieldId}" name="${fieldId}" class="form-check-input" ntype="bool">
                        <label for="${fieldId}" class="form-check-label">${fieldName}</label>
                    </div>`;
                    break;
                case 'Datum':
                    inputElement = `
                    <div style="flex:1;">
                        <label for="${fieldId}">${fieldName}:</label>
                        <input type="datetime-local" id="${fieldId}" name="${fieldId}" class="form-control" ntype="datetime">
                    </div>`;
                    break;
                case 'Integer':
                    inputElement = `
                    <div style="flex:1;">
                        <label for="${fieldId}">${fieldName}:</label>
                        <input type="number" id="${fieldId}" name="${fieldId}" class="form-control" step="1" ntype="integer">
                    </div>`;
                    break;
                case 'Unsigned':
                    inputElement = `
                    <div style="flex:1;">
                        <label for="${fieldId}">${fieldName}:</label>
                        <input type="number" id="${fieldId}" name="${fieldId}" class="form-control" step="1" min="0" ntype="unsigned">
                    </div>`;
                    break;
                case 'Float':
                    inputElement = `
                    <div style="flex:1;">
                        <label for="${fieldId}">${fieldName}:</label>
                        <input type="number" id="${fieldId}" name="${fieldId}" class="form-control" step="any" ntype="float">
                    </div>`;
                    break;
                case 'Bytes':
                    inputElement = `
                    <div style="flex:1;">
                        <label for="${fieldId}">${fieldName}:</label>
                        <input type="file" id="${fieldId}" name="${fieldId}" class="form-control" accept="*/*" ntype="bytes">
                    </div>`;
                    break;
                case 'Liste':
                    inputElement = `
                    <div style="flex:1;">
                        <label for="${fieldId}">${fieldName} (one per line):</label>
                        <textarea id="${fieldId}" name="${fieldId}" class="form-control" rows="4" placeholder="Enter items, one per line" ntype="list"></textarea>
                    </div>`;
                    break;
                case 'Kategorie':
                    inputElement = `
                    <fieldset class="form-group dynamic-field category-fieldset" style="flex:1;">
                        <legend class="text-secondary">${fieldName}</legend>
                    </fieldset>`;
                    break;
                default:
                    inputElement = `
                    <div style="flex:1;">
                        <label for="${fieldId}">${fieldName}:</label>
                        <input type="text" id="${fieldId}" name="${fieldId}" class="form-control" ntype="string">
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
