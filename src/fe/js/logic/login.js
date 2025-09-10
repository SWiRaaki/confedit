document.addEventListener("DOMContentLoaded", () => {
    const form = document.getElementById("loginForm");
    const hint = document.getElementById("loginHint");

    dataSender.connectWS("ws://localhost:8080");

    const handleLoginResponse = (msg) => {
        try {
            const response = JSON.parse(msg.data);

            if (response.code === 0 && response.data?.auth) {
                localStorage.setItem("authToken", response.data.auth);

                hint.textContent = "Login erfolgreich. Weiterleitung...";
                hint.className = "hint ok";

                setTimeout(() => {
                    window.location.href = "main.html";
                }, 1000);
            } else {
                hint.textContent = response.errors?.[0]?.msg || "Login fehlgeschlagen.";
                hint.className = "hint error";
            }
        } catch (err) {
            console.error(err);
            hint.textContent = "Serverfehler – bitte später erneut versuchen.";
            hint.className = "hint error";
        }
    };

    const wsInterval = setInterval(() => {
        if (dataSender.ws && dataSender.ws.readyState === WebSocket.OPEN) {
            dataSender.ws.onmessage = handleLoginResponse;
            clearInterval(wsInterval);
        }
    }, 100);

    form.addEventListener("submit", (e) => {
        e.preventDefault();

        const user = document.getElementById("user").value.trim();
        const security = document.getElementById("security").value.trim();

        // test
        if (user === "admin" && security === "admin") {
            hint.textContent = "Login erfolgreich. Weiterleitung...";
            hint.className = "hint ok";
            setTimeout(() => {
                window.location.href = "../main.html";
            }, 500);
            return;
        }

        if (!user || !security) {
            hint.textContent = "Bitte alle Felder ausfüllen.";
            hint.className = "hint error";
            return;
        }

        const items = [
            { name: "user", value: user, type: "string", children: [], meta: {} },
            { name: "security", value: security, type: "string", children: [], meta: {} }
        ];

        const request = {
            module: form.dataset.module || "auth",
            function: form.dataset.function || "login",
            data: {
                config: form.dataset.config || "myConfig",
                uid: crypto.randomUUID(),
                Items: items
            }
        };

        dataSender.sendRaw(request);
    });

});
