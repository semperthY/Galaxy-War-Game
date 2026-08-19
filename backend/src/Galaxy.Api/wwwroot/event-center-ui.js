window.EventCenterUi = (() => {
    let context;
    let feed = { unreadCount: 0, events: [] };
    let filter = "unread";
    let selectedId = null;
    let timerStarted = false;
    const $ = selector => document.querySelector(selector);
    const esc = value => String(value ?? "").replace(/[&<>"']/g, char => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[char]));

    function init(options) {
        context = options;
        $("#eventCenter")?.addEventListener("click", handleClick);
        if (!timerStarted) {
            timerStarted = true;
            window.setInterval(updateArrivalTimers, 1000);
        }
    }

    function render(nextFeed) {
        feed = nextFeed ?? { unreadCount: 0, events: [] };
        if (!visibleEvents().some(event => event.id === selectedId)) {
            selectedId = visibleEvents()[0]?.id ?? null;
        }
        renderAll();
    }

    function visibleEvents() {
        return filter === "unread"
            ? feed.events.filter(event => !event.readAt)
            : feed.events;
    }

    function renderAll() {
        const events = visibleEvents();
        $("#eventUnreadCount").textContent = `${feed.unreadCount} новых`;
        document.querySelectorAll("[data-event-filter]").forEach(button =>
            button.classList.toggle("active", button.dataset.eventFilter === filter));
        $("#eventList").innerHTML = events.length
            ? events.map(event => `<button class="event-list-item ${event.readAt ? "" : "unread"} ${event.id === selectedId ? "selected" : ""}" data-event-id="${event.id}"><span class="event-signal ${event.type === "IncomingAttack" ? "danger" : "scan"}"></span><span><strong>${esc(event.title)}</strong><small>${esc(event.body)}</small><time>${formatDate(event.createdAt)}</time></span></button>`).join("")
            : `<div class="empty-living">${filter === "unread" ? "Новых событий нет." : "Архив событий пуст."}</div>`;
        renderDetail();
    }

    function renderDetail() {
        const event = feed.events.find(item => item.id === selectedId);
        const detail = $("#eventDetail");
        if (!event) {
            detail.innerHTML = `<div class="empty-living">Выберите сообщение слева.</div>`;
            return;
        }
        const data = event.data ?? {};
        const coordinates = [data.galaxy, data.system, data.position].filter(value => value != null).join(":");
        const contacts = event.type === "ReconReport"
            ? `<div class="event-contact-list">${(data.contacts ?? []).length
                ? data.contacts.map(contact => `<article class="event-contact"><span><strong>${esc(contact.name)}</strong><small>${contact.isPirate ? "Пираты" : "Флот"} · ${contact.shipCount} кораблей · P${contact.position}</small></span>${contact.canAttack ? `<button data-event-attack data-event-id="${event.id}" data-target-fleet="${contact.fleetId}" data-target-name="${esc(contact.name)}" data-galaxy="${data.galaxy}" data-system="${data.system}" data-position="${contact.position}">Атаковать</button>` : ""}</article>`).join("")
                : `<div class="empty-living">Контактов не обнаружено.</div>`}</div>`
            : "";
        const attack = event.type === "IncomingAttack"
            ? `<div class="incoming-attack-card"><strong>${esc(data.attackerName)} → ${esc(data.targetName)}</strong><span>Цель: ${coordinates || "неизвестно"}</span><time data-event-arrival="${esc(data.arrivesAt)}"></time><button data-event-defense data-event-id="${event.id}">Открыть управление флотами</button></div>`
            : "";
        detail.innerHTML = `<header><div><span class="section-label">${event.type === "IncomingAttack" ? "БОЕВАЯ ТРЕВОГА" : "РАЗВЕДДАННЫЕ"}</span><h3>${esc(event.title)}</h3></div><time>${formatDate(event.createdAt)}</time></header><p>${esc(event.body)}</p>${coordinates ? `<div class="event-coordinates">Координаты: <strong>${coordinates}</strong></div>` : ""}${contacts}${attack}`;
        updateArrivalTimers();
    }

    async function handleClick(event) {
        try {
        const filterButton = event.target.closest("[data-event-filter]");
        if (filterButton) {
            filter = filterButton.dataset.eventFilter;
            selectedId = visibleEvents()[0]?.id ?? null;
            renderAll();
            return;
        }
        if (event.target.closest("[data-event-read-all]")) {
            await context.api("/api/game/events/read-all", { method: "POST" });
            feed.events.forEach(item => item.readAt ??= new Date().toISOString());
            feed.unreadCount = 0;
            context.unreadChanged(0);
            renderAll();
            return;
        }
        const attackButton = event.target.closest("[data-event-attack]");
        if (attackButton) {
            window.localStorage.setItem("livingAttackIntent", JSON.stringify({
                eventId: attackButton.dataset.eventId,
                targetFleetId: attackButton.dataset.targetFleet,
                targetName: attackButton.dataset.targetName,
                galaxy: Number(attackButton.dataset.galaxy),
                system: Number(attackButton.dataset.system),
                position: Number(attackButton.dataset.position)
            }));
            await markRead(attackButton.dataset.eventId);
            context.openPage("fleet");
            return;
        }
        const defenseButton = event.target.closest("[data-event-defense]");
        if (defenseButton) {
            await markRead(defenseButton.dataset.eventId);
            context.openPage("fleet");
            return;
        }
        const item = event.target.closest("[data-event-id]");
        if (item) {
            selectedId = item.dataset.eventId;
            await markRead(selectedId);
            renderAll();
        }
        } catch (error) {
            context.message(error.message, true);
        }
    }

    async function markRead(eventId) {
        const gameEvent = feed.events.find(item => item.id === eventId);
        if (!gameEvent || gameEvent.readAt) return;
        await context.api(`/api/game/events/${eventId}/read`, { method: "POST" });
        gameEvent.readAt = new Date().toISOString();
        feed.unreadCount = Math.max(0, feed.unreadCount - 1);
        context.unreadChanged(feed.unreadCount);
    }

    function updateArrivalTimers() {
        document.querySelectorAll("[data-event-arrival]").forEach(element => {
            const target = new Date(element.dataset.eventArrival).getTime();
            if (!Number.isFinite(target)) { element.textContent = "Время прибытия неизвестно"; return; }
            const seconds = Math.max(0, Math.ceil((target - Date.now()) / 1000));
            const hours = Math.floor(seconds / 3600);
            const minutes = Math.floor(seconds % 3600 / 60);
            const rest = seconds % 60;
            element.textContent = seconds === 0
                ? "Флот прибыл к цели"
                : `До прибытия ${hours ? `${hours}:` : ""}${String(minutes).padStart(2, "0")}:${String(rest).padStart(2, "0")}`;
        });
    }

    function formatDate(value) {
        return new Intl.DateTimeFormat("ru-RU", { day: "2-digit", month: "2-digit", hour: "2-digit", minute: "2-digit" }).format(new Date(value));
    }

    return { init, render };
})();
