const componentTypeInfo = {
    Hull: {
        name: "Корпус",
        icon: "icon-ship"
    },
    Engine: {
        name: "Двигатель",
        icon: "icon-energy"
    },
    Reactor: {
        name: "Реактор",
        icon: "icon-power"
    },
    ControlSystem: {
        name: "Система управления",
        icon: "icon-command"
    },
    ColonyModule: {
        name: "Колонизационный модуль",
        icon: "icon-planet"
    }
};
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
    researchCompletionRequested: false,
    productionStatus: null
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
    laboratoryLevel: document.querySelector("#laboratoryLevel"),
    productionComplexLevel:
        document.querySelector("#productionComplexLevel"),
    productionLineCount:
        document.querySelector("#productionLineCount"),
    componentCatalog:
        document.querySelector("#componentCatalog"),
    componentInventory:
        document.querySelector("#componentInventory"),
    productionLines:
        document.querySelector("#productionLines")
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
function getComponentStats(component) {
    const stats = [];

    if (component.capacity !== undefined) {
        stats.push(`Вместимость ${formatNumber(component.capacity)}`);
    }

    if (component.structuralIntegrity !== undefined) {
        stats.push(
            `Прочность ${formatNumber(component.structuralIntegrity)}`
        );
    }

    if (component.volume !== undefined) {
        stats.push(`Объём ${formatNumber(component.volume)}`);
    }

    if (component.inSystemSpeed !== undefined) {
        stats.push(
            `Локальная скорость ${formatNumber(component.inSystemSpeed)}`
        );
    }

    if (component.interSystemSpeed !== undefined) {
        stats.push(
            `Межсистемная скорость ${formatNumber(component.interSystemSpeed)}`
        );
    }

    if (component.energyOutput !== undefined) {
        stats.push(
            `Энергия +${formatNumber(component.energyOutput)}`
        );
    }

    if (component.energyConsumption !== undefined) {
        stats.push(
            `Потребление ${formatNumber(component.energyConsumption)}`
        );
    }

    if (component.commandRating !== undefined) {
        stats.push(
            `Управление ${formatNumber(component.commandRating)}`
        );
    }

    return stats;
}

function getComponentName(code) {
    const component = state.productionStatus?.catalog
        .find(item => item.code === code);

    return component?.name ?? code;
}

function updateProductionCountdowns() {
    document
        .querySelectorAll("[data-production-completes-at]")
        .forEach(element => {
            const completesAt =
                element.dataset.productionCompletesAt;

            if (!completesAt) {
                element.textContent = "Ожидает запуска";
                return;
            }

            const milliseconds =
                new Date(completesAt).getTime() - Date.now();

            const seconds = Math.max(
                0,
                Math.ceil(milliseconds / 1000)
            );

            const minutes = Math.floor(seconds / 60);
            const remainingSeconds = seconds % 60;

            element.textContent = minutes > 0
                ? `${minutes}:` +
                    remainingSeconds.toString().padStart(2, "0")
                : `${remainingSeconds} сек.`;

            if (seconds === 0) {
                element.textContent = "Завершение...";
            }
        });
}

function renderProduction(status) {
    state.productionStatus = status;

    elements.productionComplexLevel.textContent =
        status.productionComplexLevel;

    elements.productionLineCount.textContent =
        status.lineCount;

    const lineOptions = Array.from(
        { length: status.lineCount },
        (_, index) => `
            <option value="${index + 1}">
                Линия ${index + 1}
            </option>
        `
    ).join("");

    elements.componentCatalog.innerHTML = status.catalog
        .map(component => {
            const typeInfo =
                componentTypeInfo[component.type] ?? {
                    name: component.type,
                    icon: "icon-factory"
                };

            const stats = getComponentStats(component)
                .map(stat => `
                    <span class="component-stat">${stat}</span>
                `)
                .join("");

            const disabled =
                !component.unlocked ||
                status.lineCount < 1;

            const requirement = component.unlocked
                ? `Время производства: ` +
                    `${component.productionSeconds} сек.`
                : `Требуется: ${component.requiredTechnology} ` +
                    `ур. ${component.requiredTechnologyLevel}`;

            return `
                <article class="
                    component-card
                    ${component.unlocked ? "" : "locked"}
                ">
                    <div class="component-icon">
                        <svg>
                            <use href="#${typeInfo.icon}"></use>
                        </svg>
                    </div>

                    <div class="component-body">
                        <div class="component-heading">
                            <h4>${component.name}</h4>
                            <span class="component-type">
                                ${typeInfo.name}
                            </span>
                        </div>

                        <div class="component-race">
                            Инженерная школа: ${component.race}
                        </div>

                        <div class="component-stats">
                            ${stats}
                        </div>

                        <div class="cost-row">
                            <span class="cost-item">
                                Материалы ·
                                ${formatNumber(component.cost.materials)}
                            </span>
                            <span class="cost-item">
                                Дейтерий ·
                                ${formatNumber(component.cost.deuterium)}
                            </span>
                        </div>

                        <div class="unlock-requirement">
                            ${requirement}
                        </div>

                        <div class="production-controls">
                            <input
                                type="number"
                                min="1"
                                max="100"
                                value="1"
                                aria-label="Количество"
                                data-quantity-for="${component.code}">

                            <select
                                aria-label="Производственная линия"
                                data-line-for="${component.code}">
                                ${lineOptions}
                            </select>

                            <button
                                class="produce-button"
                                data-produce="${component.code}"
                                ${disabled ? "disabled" : ""}>
                                Запустить производство
                            </button>
                        </div>
                    </div>
                </article>
            `;
        })
        .join("");

    elements.componentCatalog
        .querySelectorAll("[data-produce]")
        .forEach(button => {
            button.addEventListener("click", () => {
                startProduction(button.dataset.produce);
            });
        });

    const inventoryItems = status.catalog
        .map(component => {
            const inventory = status.inventory.find(
                item => item.componentCode === component.code
            );

            return {
                name: component.name,
                quantity: inventory?.quantity ?? 0
            };
        })
        .filter(item => item.quantity > 0);

    elements.componentInventory.innerHTML =
        inventoryItems.length > 0
            ? inventoryItems
                .map(item => `
                    <div class="inventory-item">
                        <span title="${item.name}">
                            ${item.name}
                        </span>
                        <strong>${item.quantity}</strong>
                    </div>
                `)
                .join("")
            : `
                <div class="empty-state">
                    Склад комплектующих пуст.
                </div>
            `;

    if (status.lineCount < 1) {
        elements.productionLines.innerHTML = `
            <div class="empty-state">
                Постройте Производственный комплекс,
                чтобы открыть производственные линии.
            </div>
        `;

        return;
    }

    elements.productionLines.innerHTML = Array.from(
        { length: status.lineCount },
        (_, index) => {
            const lineNumber = index + 1;

            const orders = status.orders
                .filter(order => order.lineNumber === lineNumber)
                .sort(
                    (left, right) =>
                        left.queuePosition - right.queuePosition
                );

            const orderMarkup = orders.length > 0
                ? orders.map(order => `
                    <div class="line-order">
                        <strong>
                            ${getComponentName(order.componentCode)}
                            × ${order.quantity}
                        </strong>
                        <small
                            data-production-completes-at="${
                                order.completesAt ?? ""
                            }">
                            ${order.startedAt
                                ? "Расчёт времени..."
                                : "Ожидает запуска"}
                        </small>
                    </div>
                `).join("")
                : `
                    <div class="empty-state">
                        Линия свободна
                    </div>
                `;

            return `
                <div class="production-line">
                    <div class="line-heading">
                        <strong>Линия ${lineNumber}</strong>
                        <span class="${orders.length ? "busy" : ""}">
                            ${orders.length ? "Работает" : "Свободна"}
                        </span>
                    </div>

                    ${orderMarkup}
                </div>
            `;
        }
    ).join("");

    updateProductionCountdowns();
}

async function startProduction(componentCode) {
    const quantityInput = document.querySelector(
        `[data-quantity-for="${componentCode}"]`
    );

    const lineSelect = document.querySelector(
        `[data-line-for="${componentCode}"]`
    );

    const quantity = Number(quantityInput.value);
    const lineNumber = Number(lineSelect.value);

    try {
        const status = await api(
            `/api/game/production/lines/${lineNumber}/orders` +
            `?planetId=${state.activePlanetId}`,
            {
                method: "POST",
                body: JSON.stringify({
                    componentCode,
                    quantity
                })
            }
        );

        showMessage("Производственный заказ добавлен.");
        renderProduction(status);
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

        const [buildings, research, production] = await Promise.all([
            api(
                `/api/game/buildings/?planetId=${state.activePlanetId}`
            ),
            api(
                `/api/game/research/?planetId=${state.activePlanetId}`
            ),
            api(
                `/api/game/production/?planetId=${state.activePlanetId}`
            )
        ]);

        renderBuildings(buildings);
        renderResearch(research);
        renderProduction(production);
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
window.setInterval(updateProductionCountdowns, 1000);



