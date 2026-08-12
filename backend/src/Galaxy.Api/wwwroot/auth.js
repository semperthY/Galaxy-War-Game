const accessStep = document.querySelector("#accessStep");
const raceStep = document.querySelector("#raceStep");
const loginForm = document.querySelector("#loginForm");
const registerForm = document.querySelector("#registerForm");
const accessMessage = document.querySelector("#accessMessage");
const raceMessage = document.querySelector("#raceMessage");
const confirmRace = document.querySelector("#confirmRace");
let selectedRace = null;

async function request(path, options = {}) {
    const response = await fetch(path, {
        ...options,
        headers: { "Content-Type": "application/json", ...options.headers }
    });
    const data = response.status === 204 ? null : await response.json().catch(() => null);
    if (!response.ok) throw new Error(data?.error || "Сервер не смог выполнить запрос.");
    return data;
}

function showRaceStep(session) {
    accessStep.hidden = true;
    raceStep.hidden = false;
    document.title = `${session.commanderName} — выбор расы`;
}

function enterGame() { window.location.replace("/game/overview"); }

function handleSession(session) {
    if (!session?.authenticated) return;
    if (session.requiresRaceSelection) showRaceStep(session);
    else enterGame();
}

function switchTab(register) {
    loginForm.hidden = register;
    registerForm.hidden = !register;
    document.querySelector("#loginTab").classList.toggle("active", !register);
    document.querySelector("#registerTab").classList.toggle("active", register);
    accessMessage.textContent = "";
}

document.querySelector("#loginTab").addEventListener("click", () => switchTab(false));
document.querySelector("#registerTab").addEventListener("click", () => switchTab(true));

loginForm.addEventListener("submit", async event => {
    event.preventDefault();
    accessMessage.textContent = "Выполняется вход…";
    try {
        const values = Object.fromEntries(new FormData(loginForm));
        handleSession(await request("/api/auth/login", { method: "POST", body: JSON.stringify(values) }));
    } catch (error) { accessMessage.textContent = error.message; }
});

registerForm.addEventListener("submit", async event => {
    event.preventDefault();
    accessMessage.textContent = "Создаём учётную запись…";
    try {
        const values = Object.fromEntries(new FormData(registerForm));
        handleSession(await request("/api/auth/register", { method: "POST", body: JSON.stringify(values) }));
    } catch (error) { accessMessage.textContent = error.message; }
});

document.querySelectorAll(".race-card").forEach(card => card.addEventListener("click", () => {
    selectedRace = card.dataset.race;
    document.querySelectorAll(".race-card").forEach(item => item.classList.toggle("selected", item === card));
    confirmRace.disabled = false;
    raceMessage.textContent = "";
}));

confirmRace.addEventListener("click", async () => {
    if (!selectedRace) return;
    confirmRace.disabled = true;
    raceMessage.textContent = "Подготавливаем стартовый мир…";
    try {
        await request("/api/auth/race", { method: "POST", body: JSON.stringify({ race: selectedRace }) });
        enterGame();
    } catch (error) {
        raceMessage.textContent = error.message;
        confirmRace.disabled = false;
    }
});

request("/api/auth/me").then(handleSession).catch(() => {});
