// Helper functions for user management
function getRolePermissions(role) {
    switch(role) {
        case "Administrator":
            return { create: true, read: true, update: true, delete: true };
        case "Editor":
            return { create: true, read: true, update: true, delete: false };
        case "Viewer":
            return { create: false, read: true, update: false, delete: false };
        default:
            return { create: false, read: false, update: false, delete: false };
    }
}

function getRoleBadgeClass(role) {
    switch(role) {
        case "Administrator":
            return "badge-danger";
        case "Editor":
            return "badge-warning";
        case "Viewer":
            return "badge-info";
        default:
            return "badge-secondary";
    }
}

function createUser(username, role) {
    const userTable = document.getElementById("user-table");
    if (!userTable) return;
    
    const newRow = userTable.insertRow();
    const permissions = getRolePermissions(role);
    const badgeClass = getRoleBadgeClass(role);
    
    newRow.innerHTML = `
        <td><strong>${username}</strong></td>
        <td><span class="badge ${badgeClass}">${role}</span></td>
        <td class="text-center"><input type="checkbox" ${permissions.create ? 'checked' : ''}></td>
        <td class="text-center"><input type="checkbox" ${permissions.read ? 'checked' : ''}></td>
        <td class="text-center"><input type="checkbox" ${permissions.update ? 'checked' : ''}></td>
        <td class="text-center"><input type="checkbox" ${permissions.delete ? 'checked' : ''}></td>
        <td class="text-center">
          <button type="button" class="edit-user">✏️</button>
          <button type="button" class="delete-user">🗑️</button>
        </td>
    `;
}

function editUser(row, username, role) {
    if (!row) return;

    const permissions = getRolePermissions(role);
    const badgeClass = getRoleBadgeClass(role);

    row.innerHTML = `
        <td><strong>${username}</strong></td>
        <td><span class="badge ${badgeClass}">${role}</span></td>
        <td class="text-center"><input type="checkbox" ${permissions.create ? 'checked' : ''}></td>
        <td class="text-center"><input type="checkbox" ${permissions.read ? 'checked' : ''}></td>
        <td class="text-center"><input type="checkbox" ${permissions.update ? 'checked' : ''}></td>
        <td class="text-center"><input type="checkbox" ${permissions.delete ? 'checked' : ''}></td>
        <td class="text-center">
          <button type="button" class="edit-user">✏️</button>
          <button type="button" class="delete-user">🗑️</button>
        </td>
    `;
}

function createUserModal() {
    // Create modal backdrop
    const backdrop = document.createElement('div');
    backdrop.className = 'modal-backdrop';
    
    // Create modal content
    const modal = document.createElement('div');
    modal.className = 'modal';
    
    modal.innerHTML = `
        <div class="modal-header">
            <h3 class="modal-title">Neuen Benutzer hinzufügen</h3>
        </div>
        <div class="modal-body">
            <div class="form-group">
                <label for="username-input">Benutzername:</label>
                <input type="text" id="username-input" class="form-control" placeholder="Benutzername eingeben" value="neuerBenutzer">
            </div>
            <div class="form-group">
                <label for="role-select">Rolle:</label>
                <select id="role-select" class="form-control">
                    <option value="Viewer">👁️ Viewer</option>
                    <option value="Editor">✏️ Editor</option>
                    <option value="Administrator">⭐ Administrator</option>
                </select>
            </div>
        </div>
        <div class="modal-footer">
            <button type="button" id="cancel-user" class="btn btn-secondary">Abbrechen</button>
            <button type="button" id="create-user" class="btn btn-success">Erstellen</button>
        </div>
    `;
    
    backdrop.appendChild(modal);
    
    // Add event listeners
    const usernameInput = modal.querySelector('#username-input');
    const roleSelect = modal.querySelector('#role-select');
    const cancelBtn = modal.querySelector('#cancel-user');
    const createBtn = modal.querySelector('#create-user');
    
    // Focus on username input
    setTimeout(() => usernameInput.focus(), 100);
    
    // Cancel button
    cancelBtn.addEventListener('click', () => {
        document.body.removeChild(backdrop);
    });
    
    // Create button
    createBtn.addEventListener('click', () => {
        const username = usernameInput.value.trim();
        const role = roleSelect.value;
        
        if (!username) {
            alert('Bitte geben Sie einen Benutzernamen ein.');
            usernameInput.focus();
            return;
        }
        
        createUser(username, role);
        document.body.removeChild(backdrop);
    });
    
    // Enter key to create
    usernameInput.addEventListener('keypress', (e) => {
        if (e.key === 'Enter') {
            createBtn.click();
        }
    });
    
    roleSelect.addEventListener('keypress', (e) => {
        if (e.key === 'Enter') {
            createBtn.click();
        }
    });
    
    // Close on backdrop click
    backdrop.addEventListener('click', (e) => {
        if (e.target === backdrop) {
            document.body.removeChild(backdrop);
        }
    });
    
    return backdrop;
}

document.addEventListener("DOMContentLoaded", () => {
    const configForm = document.getElementById("configForm");
    if (configForm) {
        configForm.addEventListener("submit", (e) => {
            e.preventDefault();
            if (typeof validateForm === "function") {
                const errors = validateForm(configForm);
                if (errors.length > 0) {
                    alert("Fehler:\n" + errors.join("\n"));
                    return;
                }
            }
            if (typeof sendDataToServer === "function") {
                const formData = new FormData(configForm);
                const dataObj = {};
                formData.forEach((value, key) => dataObj[key] = value);
                sendDataToServer("verifier", "verify_numeric", dataObj);
            }
        });
    }

    const compareBtn = document.getElementById("compare-versions");
    const restoreBtn = document.getElementById("restore-version");
    if (compareBtn) {
        compareBtn.addEventListener("click", () => {
            const selected = document.querySelector("input[name='version-select']:checked");
            if (!selected) {
                alert("Bitte zuerst eine Version auswählen!");
                return;
            }

            const label = document.querySelector(`label[for='${selected.id}']`);
            if (!label) return;

            let compareContainer = document.getElementById("version-compare-container");
            if (!compareContainer) {
                compareContainer = document.createElement("div");
                compareContainer.id = "version-compare-container";
                compareContainer.style.border = "1px solid #ccc";
                compareContainer.style.padding = "1rem";
                compareContainer.style.marginTop = "1rem";
                compareContainer.style.backgroundColor = "#f9f9f9";
                document.querySelector("main section.card .card-body").appendChild(compareContainer);
            }

            compareContainer.innerHTML = `
            <h3>Vergleich Version ${selected.value}</h3>
            <div style="display:flex; flex-direction:column; gap:0.5rem;">
                ${label.innerHTML}
            </div>
        `;
        });
    }

    if (restoreBtn) {
        restoreBtn.addEventListener("click", () => {
            const selected = document.querySelector("input[name='version-select']:checked");
            if (selected) {
                alert("Version " + selected.value + " wird wiederhergestellt!");
            } else {
                alert("Bitte zuerst eine Version auswaehlen!");
            }
        });
    }

    const addUserBtn = document.getElementById("add-user");
    const userTable = document.getElementById("user-table");
    if (addUserBtn && userTable) {
        addUserBtn.addEventListener("click", () => {
            // Create modal for user input
            const modal = createUserModal();
            document.body.appendChild(modal);
        });
        userTable.addEventListener("click", (e) => {
            if (e.target.classList.contains("edit-user")) {
                const row = e.target.closest("tr");
                const currentUsername = row.cells[0].innerText.trim();
                const currentRole = row.cells[1].innerText.trim();

                const newUsername = prompt("Neuer Benutzername:", currentUsername);
                if (!newUsername) return;

                const newRole = prompt("Neue Rolle (Administrator, Editor, Viewer):", currentRole);
                if (!newRole) return;

                editUser(row, newUsername, newRole);
            }

            if (e.target.classList.contains("delete-user")) {
                const row = e.target.closest("tr");
                if (confirm("Benutzer wirklich loeschen?")) {
                    row.remove();
                }
            }
        });
    }

    // Template button functionality
    const templateButtons = document.querySelectorAll('button[data-role]');
    templateButtons.forEach(button => {
        button.addEventListener('click', () => {
            const role = button.dataset.role;
            const username = prompt(`Benutzername für ${role} eingeben:`, `neuer${role}`);
            if (username) {
                createUser(username, role);
            }
        });
    });
});