function setError(el, message) {
    if (!el) return;
    let hint = el.parentNode.querySelector(".hint");
    if (!hint) {
        hint = document.createElement("div");
        hint.className = "hint";
        el.parentNode.appendChild(hint);
    }
    hint.textContent = message || "";
    hint.className = "hint " + (message ? "error" : "ok");
}

function clearError(el) {
    if (!el) return;
    const hint = el.parentNode.querySelector(".hint");
    if (hint) {
        hint.textContent = "";
        hint.className = "hint";
    }
}

function setGlobalError(msg) {
    const container = document.getElementById("globalErrors");
    if (!container) return;
    if (!msg) {
        container.style.display = "none";
        container.textContent = "";
    } else {
        container.style.display = "block";
        container.textContent = msg;
    }
}

function handleServerErrors(errors) {
    if (!errors) return;

    if (errors.global) {
        setGlobalError(errors.global);
    }

    if (errors.fields) {
        for (const [fieldName, message] of Object.entries(errors.fields)) {
            const el = document.querySelector(`[name="${fieldName}"]`);
            if (el) {
                setError(el, message);
            }
        }
    }
}

function clearAllErrors(form) {
    if (form) {
        form.querySelectorAll(".hint").forEach(h => h.remove());
    }
    setGlobalError("");
}
