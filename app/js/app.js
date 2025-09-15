// User Management Functions following API Reference

async function loadUsers() {
    try {
        const response = await service.sendRequest({
            module: "admin",
            function: "list_users",
            data: {
				auth: localStorage.getItem( "authToken" )
			}
        });

        if (response && response.code === 0 && response.data && response.data.users) {
            populateUserTable(response.data.users);
        } else {
            console.error("Failed to load users:", response);
        }
    } catch (error) {
        console.error("Error loading users:", error);
    }
}

async function loadGroups() {
    try {
        const response = await service.sendRequest({
            module: "admin",
            function: "list_groups",
            data: {}
        });

        if (response && response.code === 0 && response.data && response.data.users) {
            populateGroupTable(response.data.users);
        } else {
            console.error("Failed to load groups:", response);
        }
    } catch (error) {
        console.error("Error loading groups:", error);
    }
}

function populateUserTable(users) {
    const tbody = document.querySelector("#user-table tbody");
    if (!tbody) return;

    tbody.innerHTML = "";

    users.forEach(user => {
        const row = document.createElement("tr");
        row.dataset.uid = user.uid;

        row.innerHTML = `
            <td><strong>${user.name}</strong></td>
            <td>${user.abbreviation}</td>
            <td><span id="groups-${user.uid}">Lade Gruppen...</span></td>
        <td class="text-center">
                <button type="button" class="btn btn-sm btn-primary edit-user" data-uid="${user.uid}">✏️</button>
                <button type="button" class="btn btn-sm btn-danger delete-user" data-uid="${user.uid}">🗑️</button>
        </td>
    `;

        tbody.appendChild(row);

        // Load user groups
        loadUserGroups(user.uid);
    });
}

function populateGroupTable(groups) {
    const tbody = document.querySelector("#group-table tbody");
    if (!tbody) return;

    tbody.innerHTML = "";

    groups.forEach(group => {
        const row = document.createElement("tr");
        row.dataset.uid = group.uid;

        row.innerHTML = `
            <td><strong>${group.name}</strong></td>
            <td>${group.abbreviation}</td>
            <td>${group.description || ""}</td>
        <td class="text-center">
                <button type="button" class="btn btn-sm btn-primary edit-group" data-uid="${group.uid}">✏️</button>
                <button type="button" class="btn btn-sm btn-danger delete-group" data-uid="${group.uid}">🗑️</button>
        </td>
    `;

        tbody.appendChild(row);
    });
}

async function loadUserGroups(userUid) {
    try {
        const response = await service.sendRequest({
            module: "admin",
            function: "list_user_groups",
            data: { uid: userUid }
        });

        if (response && response.code === 0 && response.data && response.data.groups) {
            const groupsSpan = document.getElementById(`groups-${userUid}`);
            if (groupsSpan) {
                groupsSpan.innerHTML = response.data.groups.map(groupUid =>
                    `<span class="badge badge-info">${groupUid}</span>`
                ).join(" ");
            }
        }
    } catch (error) {
        console.error("Error loading user groups:", error);
    }
}

async function createUser(name, abbreviation, security, groupUid) {
    try {
        // Step 1: Create user
        const createResponse = await service.sendRequest({
            module: "admin",
            function: "create_user",
            data: {
                name: name,
                abbreviation: abbreviation,
                security: security
            }
        });

        if (createResponse && createResponse.code === 0 && createResponse.data) {
            const userUid = createResponse.data.uid;

            // Step 2: Add user to group (if group specified)
            if (groupUid) {
                await addUserToGroup(userUid, groupUid);
            }

            // Reload users to show the new user
            await loadUsers();
            return true;
        } else {
            console.error("Failed to create user:", createResponse);
            return false;
        }
    } catch (error) {
        console.error("Error creating user:", error);
        return false;
    }
}

async function addUserToGroup(userUid, groupUid) {
    try {
        const response = await service.sendRequest({
            module: "admin",
            function: "add_user_to_group",
            data: {
                user_uid: userUid,
                group_uid: groupUid
            }
        });

        if (response && response.code === 0) {
            // Reload user groups to show updated membership
            await loadUserGroups(userUid);
            return true;
        } else {
            console.error("Failed to add user to group:", response);
            return false;
        }
    } catch (error) {
        console.error("Error adding user to group:", error);
        return false;
    }
}

async function removeUserFromGroup(userUid, groupUid) {
    try {
        const response = await service.sendRequest({
            module: "admin",
            function: "remove_user_from_group",
            data: {
                user_uid: userUid,
                group_uid: groupUid
            }
        });

        if (response && response.code === 0) {
            // Reload user groups to show updated membership
            await loadUserGroups(userUid);
            return true;
        } else {
            console.error("Failed to remove user from group:", response);
            return false;
        }
    } catch (error) {
        console.error("Error removing user from group:", error);
        return false;
    }
}

async function updateUser(uid, name, abbreviation, security) {
    try {
        const response = await service.sendRequest({
            module: "admin",
            function: "update_user",
            data: {
                uid: uid,
                name: name,
                abbreviation: abbreviation,
                security: security
            }
        });

        if (response && response.code === 0) {
            await loadUsers();
            return true;
        } else {
            console.error("Failed to update user:", response);
            return false;
        }
    } catch (error) {
        console.error("Error updating user:", error);
        return false;
    }
}

async function deleteUser(uid) {
    try {
        const response = await service.sendRequest({
            module: "admin",
            function: "delete_user",
            data: { uid: uid }
        });

        if (response && response.code === 0) {
            await loadUsers();
            return true;
        } else {
            console.error("Failed to delete user:", response);
            return false;
        }
    } catch (error) {
        console.error("Error deleting user:", error);
        return false;
    }
}

async function createGroup(name, abbreviation, description) {
    try {
        const response = await service.sendRequest({
            module: "admin",
            function: "create_group",
            data: {
                name: name,
                abbreviation: abbreviation,
                description: description
            }
        });

        if (response && response.code === 0) {
            await loadGroups();
            return true;
        } else {
            console.error("Failed to create group:", response);
            return false;
        }
    } catch (error) {
        console.error("Error creating group:", error);
        return false;
    }
}

async function updateGroup(uid, name, abbreviation, description) {
    try {
        const response = await service.sendRequest({
            module: "admin",
            function: "update_group",
            data: {
                uid: uid,
                name: name,
                abbreviation: abbreviation,
                description: description
            }
        });

        if (response && response.code === 0) {
            await loadGroups();
            return true;
        } else {
            console.error("Failed to update group:", response);
            return false;
        }
    } catch (error) {
        console.error("Error updating group:", error);
        return false;
    }
}

async function deleteGroup(uid) {
    try {
        const response = await service.sendRequest({
            module: "admin",
            function: "delete_group",
            data: { uid: uid }
        });

        if (response && response.code === 0) {
            await loadGroups();
            return true;
        } else {
            console.error("Failed to delete group:", response);
            return false;
        }
    } catch (error) {
        console.error("Error deleting group:", error);
        return false;
    }
}

function createUserModal() {
    const backdrop = document.createElement('div');
    backdrop.className = 'modal-backdrop';

    const modal = document.createElement('div');
    modal.className = 'modal';

    modal.innerHTML = `
        <div class="modal-header">
            <h3 class="modal-title">Neuen Benutzer hinzufügen</h3>
        </div>
        <div class="modal-body">
            <div class="form-group">
                <label for="user-name-input">Name:</label>
                <input type="text" id="user-name-input" class="form-control" placeholder="Vollständiger Name eingeben" required>
            </div>
            <div class="form-group">
                <label for="user-abbreviation-input">Abkürzung:</label>
                <input type="text" id="user-abbreviation-input" class="form-control" placeholder="Abkürzung eingeben" required>
            </div>
            <div class="form-group">
                <label for="user-security-input">Passwort:</label>
                <input type="password" id="user-security-input" class="form-control" placeholder="Passwort eingeben" required>
            </div>
            <div class="form-group">
                <label for="user-group-select">Gruppe (optional):</label>
                <select id="user-group-select" class="form-control">
                    <option value="">Keine Gruppe auswählen</option>
                </select>
            </div>
        </div>
        <div class="modal-footer">
            <button type="button" id="cancel-user" class="btn btn-secondary">Abbrechen</button>
            <button type="button" id="create-user" class="btn btn-success">Erstellen</button>
        </div>
    `;

    backdrop.appendChild(modal);

    const nameInput = modal.querySelector('#user-name-input');
    const abbreviationInput = modal.querySelector('#user-abbreviation-input');
    const securityInput = modal.querySelector('#user-security-input');
    const groupSelect = modal.querySelector('#user-group-select');
    const cancelBtn = modal.querySelector('#cancel-user');
    const createBtn = modal.querySelector('#create-user');

    // Load groups for selection
    loadGroupsForSelect(groupSelect);

    setTimeout(() => nameInput.focus(), 100);

    cancelBtn.addEventListener('click', () => {
        document.body.removeChild(backdrop);
    });

    createBtn.addEventListener('click', async () => {
        const name = nameInput.value.trim();
        const abbreviation = abbreviationInput.value.trim();
        const security = securityInput.value.trim();
        const groupUid = groupSelect.value;

        if (!name || !abbreviation || !security) {
            alert('Bitte füllen Sie alle Pflichtfelder aus.');
            return;
        }

        createBtn.disabled = true;
        createBtn.textContent = 'Erstelle...';

        const success = await createUser(name, abbreviation, security, groupUid);

        if (success) {
            document.body.removeChild(backdrop);
        } else {
            alert('Fehler beim Erstellen des Benutzers. Bitte versuchen Sie es erneut.');
            createBtn.disabled = false;
            createBtn.textContent = 'Erstellen';
        }
    });

    backdrop.addEventListener('click', (e) => {
        if (e.target === backdrop) {
            document.body.removeChild(backdrop);
        }
    });

    return backdrop;
}

function createGroupModal() {
    const backdrop = document.createElement('div');
    backdrop.className = 'modal-backdrop';

    const modal = document.createElement('div');
    modal.className = 'modal';

    modal.innerHTML = `
        <div class="modal-header">
            <h3 class="modal-title">Neue Gruppe hinzufügen</h3>
        </div>
        <div class="modal-body">
            <div class="form-group">
                <label for="group-name-input">Name:</label>
                <input type="text" id="group-name-input" class="form-control" placeholder="Gruppenname eingeben" required>
            </div>
            <div class="form-group">
                <label for="group-abbreviation-input">Abkürzung:</label>
                <input type="text" id="group-abbreviation-input" class="form-control" placeholder="Abkürzung eingeben" required>
            </div>
            <div class="form-group">
                <label for="group-description-input">Beschreibung:</label>
                <input type="text" id="group-description-input" class="form-control" placeholder="Beschreibung eingeben">
            </div>
        </div>
        <div class="modal-footer">
            <button type="button" id="cancel-group" class="btn btn-secondary">Abbrechen</button>
            <button type="button" id="create-group" class="btn btn-success">Erstellen</button>
        </div>
    `;

    backdrop.appendChild(modal);

    const nameInput = modal.querySelector('#group-name-input');
    const abbreviationInput = modal.querySelector('#group-abbreviation-input');
    const descriptionInput = modal.querySelector('#group-description-input');
    const cancelBtn = modal.querySelector('#cancel-group');
    const createBtn = modal.querySelector('#create-group');

    setTimeout(() => nameInput.focus(), 100);

    cancelBtn.addEventListener('click', () => {
        document.body.removeChild(backdrop);
    });

    createBtn.addEventListener('click', async () => {
        const name = nameInput.value.trim();
        const abbreviation = abbreviationInput.value.trim();
        const description = descriptionInput.value.trim();

        if (!name || !abbreviation) {
            alert('Bitte füllen Sie Name und Abkürzung aus.');
            return;
        }

        createBtn.disabled = true;
        createBtn.textContent = 'Erstelle...';

        const success = await createGroup(name, abbreviation, description);

        if (success) {
            document.body.removeChild(backdrop);
        } else {
            alert('Fehler beim Erstellen der Gruppe. Bitte versuchen Sie es erneut.');
            createBtn.disabled = false;
            createBtn.textContent = 'Erstellen';
        }
    });

    backdrop.addEventListener('click', (e) => {
        if (e.target === backdrop) {
            document.body.removeChild(backdrop);
        }
    });

    return backdrop;
}

async function loadGroupsForSelect(selectElement) {
    try {
        const response = await service.sendRequest({
            module: "admin",
            function: "list_groups",
            data: {}
        });

        if (response && response.code === 0 && response.data && response.data.users) {
            selectElement.innerHTML = '<option value="">Keine Gruppe auswählen</option>';
            response.data.users.forEach(group => {
                const option = document.createElement('option');
                option.value = group.uid;
                option.textContent = `${group.name} (${group.abbreviation})`;
                selectElement.appendChild(option);
            });
        }
    } catch (error) {
        console.error("Error loading groups for select:", error);
    }
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

    // User Management Event Handlers
    const addUserBtn = document.getElementById("add-user");
    const addGroupBtn = document.getElementById("add-group");
    const userTable = document.getElementById("user-table");
    const groupTable = document.getElementById("group-table");

    if (addUserBtn) {
        addUserBtn.addEventListener("click", () => {
            const modal = createUserModal();
            document.body.appendChild(modal);
        });
    }

    if (addGroupBtn) {
        addGroupBtn.addEventListener("click", () => {
            const modal = createGroupModal();
            document.body.appendChild(modal);
        });
    }

    if (userTable) {
        userTable.addEventListener("click", async (e) => {
            if (e.target.classList.contains("edit-user")) {
                const uid = e.target.dataset.uid;
                const row = e.target.closest("tr");
                const currentName = row.cells[0].innerText.trim();
                const currentAbbreviation = row.cells[1].innerText.trim();

                const newName = prompt("Neuer Name:", currentName);
                if (!newName) return;

                const newAbbreviation = prompt("Neue Abkürzung:", currentAbbreviation);
                if (!newAbbreviation) return;

                const success = await updateUser(uid, newName, newAbbreviation, null);
                if (!success) {
                    alert("Fehler beim Aktualisieren des Benutzers.");
                }
            }

            if (e.target.classList.contains("delete-user")) {
                const uid = e.target.dataset.uid;
                const row = e.target.closest("tr");
                const userName = row.cells[0].innerText.trim();

                if (confirm(`Benutzer "${userName}" wirklich löschen?`)) {
                    const success = await deleteUser(uid);
                    if (!success) {
                        alert("Fehler beim Löschen des Benutzers.");
                    }
                }
            }
        });
    }

    if (groupTable) {
        groupTable.addEventListener("click", async (e) => {
            if (e.target.classList.contains("edit-group")) {
                const uid = e.target.dataset.uid;
                const row = e.target.closest("tr");
                const currentName = row.cells[0].innerText.trim();
                const currentAbbreviation = row.cells[1].innerText.trim();
                const currentDescription = row.cells[2].innerText.trim();

                const newName = prompt("Neuer Gruppenname:", currentName);
                if (!newName) return;

                const newAbbreviation = prompt("Neue Abkürzung:", currentAbbreviation);
                if (!newAbbreviation) return;

                const newDescription = prompt("Neue Beschreibung:", currentDescription);

                const success = await updateGroup(uid, newName, newAbbreviation, newDescription);
                if (!success) {
                    alert("Fehler beim Aktualisieren der Gruppe.");
                }
            }

            if (e.target.classList.contains("delete-group")) {
                const uid = e.target.dataset.uid;
                const row = e.target.closest("tr");
                const groupName = row.cells[0].innerText.trim();

                if (confirm(`Gruppe "${groupName}" wirklich löschen?`)) {
                    const success = await deleteGroup(uid);
                    if (!success) {
                        alert("Fehler beim Löschen der Gruppe.");
                    }
                }
            }
        });
    }

    // Load data on page load
    if (userTable || groupTable) {
        loadUsers();
        loadGroups();
    }
});
