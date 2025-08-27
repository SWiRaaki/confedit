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
            if (selected) {
                alert("Vergleich gestartet fuer Version: " + selected.value);
            } else {
                alert("Bitte zuerst eine Version auswaehlen!");
            }
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
            const newRow = userTable.insertRow();
            newRow.innerHTML = `
                <td>neuerBenutzer</td>
                <td>Viewer</td>
                <td><input type="checkbox"></td>
                <td><input type="checkbox" checked></td>
                <td><input type="checkbox"></td>
                <td><input type="checkbox"></td>
                <td>
                  <button type="button" class="edit-user">??</button>
                  <button type="button" class="delete-user">???</button>
                </td>
            `;
        });
        userTable.addEventListener("click", (e) => {
            if (e.target.classList.contains("edit-user")) {
                const row = e.target.closest("tr");
                alert("Bearbeite Benutzer: " + row.cells[0].innerText);
            }
            if (e.target.classList.contains("delete-user")) {
                const row = e.target.closest("tr");
                if (confirm("Benutzer wirklich loeschen?")) {
                    row.remove();
                }
            }
        });
    }
});