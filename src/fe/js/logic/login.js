document.addEventListener("DOMContentLoaded", () => {
    const form = document.getElementById("loginForm");
    const hint = document.getElementById("loginHint");

    form.addEventListener("submit", async (e) => {
        e.preventDefault();

        const user = document.getElementById("user").value.trim();
        const security = document.getElementById("security").value.trim();

        if (!user || !security) {
            hint.textContent = "Bitte alle Felder ausfüllen.";
            hint.className = "hint error";
            return;
        }

        try {
            const response = await fetch("/api", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({
                    module: "auth",
                    function: "login",
                    data: { user, security }
                })
            });

            const result = await response.json();

            if (result.code === 0 && result.data?.auth) {
                localStorage.setItem("authToken", result.data.auth);

                hint.textContent = "Login erfolgreich. Weiterleitung...";
                hint.className = "hint ok";

                setTimeout(() => {
                    window.location.href = "main.html";
                }, 1000);
            } else {
                hint.textContent = result.errors?.[0]?.msg || "Login fehlgeschlagen.";
                hint.className = "hint error";
            }
        } catch (err) {
            console.error(err);
            hint.textContent = "Serverfehler – bitte später erneut versuchen.";
            hint.className = "hint error";
        }
    });
});
