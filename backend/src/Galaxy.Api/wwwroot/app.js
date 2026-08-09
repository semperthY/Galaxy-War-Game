const technologyInfo = {
    MaterialsScience: {
        name: "Материаловедение",
        icon: "icon-materials",
        description:
            "Исследование конструкционных материалов, корпусов и промышленной инфраструктуры."
    },
    EnergySystems: {
        name: "Энергетические системы",
        icon: "icon-energy",
        description:
            "Развитие реакторов, электростанций и систем распределения энергии."
    },
    DeuteriumTechnology: {
        name: "Дейтериевые технологии",
        icon: "icon-deuterium",
        description:
            "Повышает эффективность добычи и применения дейтерия."
    },
    ControlSystems: {
        name: "Системы управления",
        icon: "icon-command",
        description:
            "Открывает вычислительные и командные комплексы кораблей."
    },
    Propulsion: {
        name: "Двигательные системы",
        icon: "icon-ship",
        description:
            "Развитие внутри- и межсистемных корабельных двигателей."
    },
    ComponentEngineering: {
        name: "Инженерия компонентов",
        icon: "icon-factory",
        description:
            "Открывает производство сложных корабельных компонентов."
    }
};
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
    queueCompletionRequested: false,
    researchQueue: null,
    researchCompletionRequested: false
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
    message: document.querySelector("#message"),
    researchGrid: document.querySelector("#researchGrid"),
    researchQueueStatus: document.querySelector("#researchQueueStatus"),
    laboratoryLevel: document.querySelector("#laboratoryLevel")
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

function updateResearchCountdown() {
    const queue = state.researchQueue;

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
        technologyInfo[queue.technology]?.name ??
        queue.technology;

    const statusText =
        elements.researchQueueStatus.querySelector("strong");

    if (statusText) {
        statusText.textContent =
            `${title} · ур. ${queue.level} · ${formattedTime}`;
    }

    if (
        seconds === 0 &&
        !state.researchCompletionRequested
    ) {
        state.researchCompletionRequested = true;
        loadDashboard();
    }
}

function updateResearchQueue(status) {
    if (status.queuedTechnology === null) {
        state.researchQueue = null;
        state.researchCompletionRequested = false;

        elements.researchQueueStatus.classList.remove("busy");
        elements.researchQueueStatus.innerHTML = `
            <span class="queue-indicator"></span>
            <div>
                <small>ОЧЕРЕДЬ ИССЛЕДОВАНИЙ</small>
                <strong>Свободна</strong>
            </div>
        `;

        return;
    }

    const queueKey =
        `${status.queuedTechnology}:` +
        `${status.queuedTechnologyLevel}:` +
        `${status.researchCompletesAt}`;

    if (state.researchQueue?.key !== queueKey) {
        state.researchCompletionRequested = false;
    }

    state.researchQueue = {
        key: queueKey,
        technology: status.queuedTechnology,
        level: status.queuedTechnologyLevel,
        completesAt: status.researchCompletesAt
    };

    elements.researchQueueStatus.classList.add("busy");
    elements.researchQueueStatus.innerHTML = `
        <span class="queue-indicator"></span>
        <div>
            <small>ОЧЕРЕДЬ ИССЛЕДОВАНИЙ</small>
            <strong></strong>
        </div>
    `;

    updateResearchCountdown();
}

function renderResearch(status) {
    elements.laboratoryLevel.textContent =
        status.researchLaboratoryLevel;

    updateResearchQueue(status);

    const queueBusy =
        status.queuedTechnology !== null;

    elements.researchGrid.innerHTML = status.technologies
        .map(technology => {
            const info = technologyInfo[technology.technology] ?? {
                name: technology.technology,
                icon: "icon-research",
                description: "Имперская исследовательская программа."
            };

            const targetLevel =
                technology.currentLevel + 1;

            const laboratoryTooLow =
                targetLevel > status.researchLaboratoryLevel;

            const affordable =
                status.materials >= technology.nextLevelCost.materials &&
                status.deuterium >= technology.nextLevelCost.deuterium;

            const disabled =
                queueBusy ||
                laboratoryTooLow ||
                !affordable;

            const requirement = laboratoryTooLow
                ? `Требуется лаборатория уровня ${targetLevel}`
                : "Лаборатория соответствует требованиям";

            return `
                <article class="
                    technology-card
                    ${laboratoryTooLow ? "locked" : ""}
                ">
                    <div class="technology-heading">
                        <div class="technology-icon">
                            <svg>
                                <use href="#${info.icon}"></use>
                            </svg>
                        </div>

                        <span class="technology-level">
                            УР. ${technology.currentLevel}
                        </span>
                    </div>

                    <h3>${info.name}</h3>

                    <p class="technology-description">
                        ${info.description}
                    </p>

                    <div class="technology-requirement">
                        ${requirement}
                    </div>

                    <div class="cost-row">
                        <span class="cost-item">
                            Материалы ·
                            ${formatNumber(technology.nextLevelCost.materials)}
                        </span>
                        <span class="cost-item">
                            Дейтерий ·
                            ${formatNumber(technology.nextLevelCost.deuterium)}
                        </span>
                    </div>

                    <button
                        class="research-button"
                        data-technology="${technology.technology}"
                        ${disabled ? "disabled" : ""}>
                        Исследовать уровень ${targetLevel}
                    </button>
                </article>
            `;
        })
        .join("");

    elements.researchGrid
        .querySelectorAll("[data-technology]")
        .forEach(button => {
            button.addEventListener("click", () => {
                startResearch(button.dataset.technology);
            });
        });
}

async function startResearch(technology) {
    try {
        await api(
            `/api/game/research/${technology}/start` +
            `?planetId=${state.activePlanetId}`,
            {
                method: "POST"
            }
        );

        showMessage("Исследовательская программа запущена.");
        await loadDashboard();
    } catch (error) {
        showMessage(error.message, true);
    }
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

        const [buildings, research] = await Promise.all([
            api(
                `/api/game/buildings/?planetId=${state.activePlanetId}`
            ),
            api(
                `/api/game/research/?planetId=${state.activePlanetId}`
            )
        ]);

        renderBuildings(buildings);
        renderResearch(research);
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
window.setInterval(updateResearchCountdown, 1000);


