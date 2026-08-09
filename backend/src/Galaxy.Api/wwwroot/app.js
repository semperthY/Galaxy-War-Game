const buildingInfo = {
    MaterialsExtractor: {
        name: "Экстрактор материалов",
        icon: "icon-extractor",
        description: "Добывает основные материалы для строительства и производства."
    },
    DeuteriumExtractor: {
        name: "Экстрактор дейтерия",
        icon: "icon-deuterium",
        description: "Производит топливо для энергетики и космических технологий."
    },
    PowerPlant: {
        name: "Электростанция",
        icon: "icon-power",
        description: "Обеспечивает инфраструктуру планеты необходимой энергией."
    },
    Warehouse: {
        name: "Планетарный склад",
        icon: "icon-warehouse",
        description: "Увеличивает максимальный объём хранимых ресурсов."
    },
    ResearchLaboratory: {
        name: "Исследовательский центр",
        icon: "icon-research",
        description: "Открывает новые технологии, здания и комплектующие."
    },
    ProductionComplex: {
        name: "Производственный комплекс",
        icon: "icon-factory",
        description: "Производит корабельные компоненты на независимых линиях."
    },
    AssemblyComplex: {
        name: "Сборочный комплекс",
        icon: "icon-ship",
        description: "Собирает корабли по сохранённым инженерным проектам."
    }
};

const state = {
    planets: [],
    activePlanetId: null,
    buildingQueue: null,
    queueCompletionRequested: false
};

const elements = {
    planetSelect: document.querySelector("#planetSelect"),
    planetName: document.querySelector("#planetName"),
    coordinates: document.querySelector("#coordinates"),
    materials: document.querySelector("#materials"),
    materialsCapacity: document.querySelector("#materialsCapacity"),
    deuterium: document.querySelector("#deuterium"),
    deuteriumCapacity: document.querySelector("#deuteriumCapacity"),
    energy: document.querySelector("#energy"),
    efficiency: document.querySelector("#efficiency"),
    heroEfficiency: document.querySelector("#heroEfficiency"),
    buildingSites: document.querySelector("#buildingSites"),
    buildingGrid: document.querySelector("#buildingGrid"),
    queueStatus: document.querySelector("#queueStatus"),
    refreshButton: document.querySelector("#refreshButton"),
    message: document.querySelector("#message")
};

async function api(path, options = {}) {
    const response = await fetch(path, {
        headers: {
            "Content-Type": "application/json",
            ...options.headers
        },
        ...options
    });

    if (!response.ok) {
        let message = `Ошибка сервера: ${response.status}`;

        try {
            const error = await response.json();
            message = error.error ?? message;
        } catch {
        }

        throw new Error(message);
    }

    return response.json();
}

function formatNumber(value) {
    return new Intl.NumberFormat("ru-RU", {
        maximumFractionDigits: 2
    }).format(value);
}

function showMessage(text, isError = false) {
    elements.message.textContent = text;
    elements.message.classList.remove("hidden", "error");

    if (isError) {
        elements.message.classList.add("error");
    }

    window.setTimeout(() => {
        elements.message.classList.add("hidden");
    }, 4500);
}

function renderPlanetSelector() {
    elements.planetSelect.innerHTML = state.planets
        .map(planet => `
            <option
                value="${planet.id}"
                ${planet.id === state.activePlanetId ? "selected" : ""}>
                ${planet.name} · ${planet.galaxy}:${planet.system}:${planet.position}
            </option>
        `)
        .join("");
}

function renderPlanet(planet) {
    const efficiency =
        formatNumber(planet.productionEfficiency * 100);

    elements.planetName.textContent = planet.name;
    elements.coordinates.textContent =
        `Сектор ${planet.galaxy} · Система ${planet.system} · Орбита ${planet.position}`;

    elements.materials.textContent =
        formatNumber(planet.materials);

    elements.materialsCapacity.textContent =
        `${formatNumber(planet.materials)} / ` +
        `${formatNumber(planet.materialsCapacity)}`;

    elements.deuterium.textContent =
        formatNumber(planet.deuterium);

    elements.deuteriumCapacity.textContent =
        `${formatNumber(planet.deuterium)} / ` +
        `${formatNumber(planet.deuteriumCapacity)}`;

    elements.energy.textContent =
        `${formatNumber(planet.energyProduction)} / ` +
        `${formatNumber(planet.energyConsumption)}`;

    elements.efficiency.textContent =
        `Эффективность ${efficiency}%`;

    elements.heroEfficiency.textContent =
        `Эффективность ${efficiency}%`;

    elements.buildingSites.textContent =
        `${planet.usedBuildingSites} / ${planet.buildingSiteCapacity}`;
}

function updateQueueCountdown() {
    const queue = state.buildingQueue;

    if (!queue) {
        return;
    }

    const remainingMilliseconds =
        new Date(queue.completesAt).getTime() - Date.now();

    const seconds = Math.max(
        0,
        Math.ceil(remainingMilliseconds / 1000)
    );

    const minutes = Math.floor(seconds / 60);
    const remainingSeconds = seconds % 60;

    const formattedTime = minutes > 0
        ? `${minutes}:${remainingSeconds.toString().padStart(2, "0")}`
        : `${remainingSeconds} сек.`;

    const title =
        buildingInfo[queue.building]?.name ??
        queue.building;

    const statusText =
        elements.queueStatus.querySelector("strong");

    if (statusText) {
        statusText.textContent =
            `${title} · ур. ${queue.level} · ${formattedTime}`;
    }

    if (
        seconds === 0 &&
        !state.queueCompletionRequested
    ) {
        state.queueCompletionRequested = true;
        loadDashboard();
    }
}

function updateQueueState(status) {
    if (status.queuedBuilding === null) {
        state.buildingQueue = null;
        state.queueCompletionRequested = false;

        elements.queueStatus.classList.remove("busy");
        elements.queueStatus.innerHTML = `
            <span class="queue-indicator"></span>
            <div>
                <small>СТРОИТЕЛЬНАЯ ОЧЕРЕДЬ</small>
                <strong>Свободна</strong>
            </div>
        `;

        return;
    }

    const queueKey =
        `${status.queuedBuilding}:` +
        `${status.queuedBuildingLevel}:` +
        `${status.buildingCompletesAt}`;

    if (state.buildingQueue?.key !== queueKey) {
        state.queueCompletionRequested = false;
    }

    state.buildingQueue = {
        key: queueKey,
        building: status.queuedBuilding,
        level: status.queuedBuildingLevel,
        completesAt: status.buildingCompletesAt
    };

    elements.queueStatus.classList.add("busy");
    elements.queueStatus.innerHTML = `
        <span class="queue-indicator"></span>
        <div>
            <small>СТРОИТЕЛЬНАЯ ОЧЕРЕДЬ</small>
            <strong></strong>
        </div>
    `;

    updateQueueCountdown();
}
function renderBuildings(status) {
    const queueBusy = status.queuedBuilding !== null;

    updateQueueState(status);


    elements.buildingGrid.innerHTML = status.buildings
        .map(building => {
            const info = buildingInfo[building.building] ?? {
                name: building.building,
                icon: "icon-buildings",
                description: "Планетарная инфраструктура."
            };

            const materials =
                formatNumber(building.nextLevelCost.materials);

            const deuterium =
                formatNumber(building.nextLevelCost.deuterium);

            const noFreeSite =
                building.currentLevel === 0 &&
                status.usedBuildingSites >= status.buildingSiteCapacity;

            const affordable =
                status.materials >= building.nextLevelCost.materials &&
                status.deuterium >= building.nextLevelCost.deuterium;

            const disabled =
                queueBusy ||
                noFreeSite ||
                !affordable;

            return `
                <article class="building-card">
                    <div class="building-icon">
                        <svg>
                            <use href="#${info.icon}"></use>
                        </svg>
                    </div>

                    <div class="building-content">
                        <div class="building-title">
                            <h3>${info.name}</h3>
                            <span class="level-badge">
                                УР. ${building.currentLevel}
                            </span>
                        </div>

                        <p class="building-description">
                            ${info.description}
                        </p>

                        <div class="cost-row">
                            <span class="cost-item">
                                Материалы · ${materials}
                            </span>
                            <span class="cost-item">
                                Дейтерий · ${deuterium}
                            </span>
                        </div>

                        <button
                            class="build-button"
                            data-building="${building.building}"
                            ${disabled ? "disabled" : ""}>
                            Улучшить до уровня ${building.currentLevel + 1}
                        </button>
                    </div>
                </article>
            `;
        })
        .join("");

    elements.buildingGrid
        .querySelectorAll("[data-building]")
        .forEach(button => {
            button.addEventListener("click", () => {
                startBuilding(button.dataset.building);
            });
        });
}

async function loadDashboard() {
    try {
        state.planets = await api("/api/game/planets");

        if (
            !state.activePlanetId ||
            !state.planets.some(x => x.id === state.activePlanetId)
        ) {
            state.activePlanetId = state.planets[0].id;
        }

        renderPlanetSelector();

        const activePlanet = state.planets.find(
            planet => planet.id === state.activePlanetId
        );

        renderPlanet(activePlanet);

        const buildings = await api(
            `/api/game/buildings/?planetId=${state.activePlanetId}`
        );

        renderBuildings(buildings);
    } catch (error) {
        showMessage(error.message, true);
    }
}

async function startBuilding(building) {
    try {
        await api(
            `/api/game/buildings/${building}/start` +
            `?planetId=${state.activePlanetId}`,
            {
                method: "POST"
            }
        );

        showMessage("Строительный проект запущен.");
        await loadDashboard();
    } catch (error) {
        showMessage(error.message, true);
    }
}

elements.planetSelect.addEventListener("change", event => {
    state.activePlanetId = event.target.value;
    loadDashboard();
});

elements.refreshButton.addEventListener("click", loadDashboard);

loadDashboard();
window.setInterval(loadDashboard, 5000);
window.setInterval(updateQueueCountdown, 1000);

