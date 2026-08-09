const buildingLevelSummaries = {
    MaterialsExtractor:
        "Каждый уровень добавляет 30 материалов в час и потребляет ещё 5 единиц энергии.",
    DeuteriumExtractor:
        "Каждый уровень добавляет 10 дейтерия в час и потребляет ещё 10 единиц энергии.",
    PowerPlant:
        "Каждый уровень добавляет 20 единиц производства энергии.",
    Warehouse:
        "Каждый уровень удваивает вместимость: базово 1000 материалов и 500 дейтерия.",
    ResearchLaboratory:
        "Уровень лаборатории ограничивает максимальный уровень исследования и сокращает его продолжительность.",
    ProductionComplex:
        "Каждый уровень открывает дополнительную независимую линию. После первого уровня скорость растёт на 10% за уровень.",
    AssemblyComplex:
        "Каждый уровень после первого ускоряет сборку кораблей на 10%."
};

const technologyLevelSummaries = {
    MaterialsScience:
        "Повышает уровень материаловедения и выполняет требования новых корпусов, компонентов и технологий.",
    EnergySystems:
        "Повышает уровень энергетических систем и открывает более сложные реакторы и энергетические технологии.",
    DeuteriumTechnology:
        "Повышает уровень дейтериевых технологий для топлива, двигателей и высокотехнологичных компонентов.",
    ControlSystems:
        "Повышает уровень систем управления и открывает более сложные командные комплексы.",
    Propulsion:
        "Повышает уровень двигательных технологий и выполняет требования новых двигателей.",
    ComponentEngineering:
        "Повышает уровень инженерии компонентов и открывает специализированные корабельные системы."
};

const staticTooltipDefinitions = [
    {
        selector: ".brand-mark",
        title: "Галактическое командование",
        text: "Главный центр управления вашей космической империей."
    },
    {
        selector: '.nav-item[href="#overview"]',
        title: "Планета",
        text: "Сводка ресурсов, энергетики и состояния активной колонии."
    },
    {
        selector: '.nav-item[href="#buildings"]',
        title: "Строительство",
        text: "Развитие инфраструктуры и специализация планеты."
    },
    {
        selector: '.nav-item[href="#research"]',
        title: "Исследования",
        text: "Имперские технологии, открывающие здания и компоненты."
    },
    {
        selector: '.nav-item[href="#production"]',
        title: "Производство",
        text: "Создание корабельных комплектующих на независимых линиях."
    },
    {
        selector: '.nav-item[href="#ship-designer"]',
        title: "Конструктор кораблей",
        text: "Создание собственных проектов с контролем объёма и энергии."
    },
    {
        selector: ".nav-item.locked",
        title: "Галактика",
        text: "Карта систем, планет и будущих космических операций."
    },
    {
        selector: ".header-resource.materials",
        title: "Материалы",
        text: "Основной ресурс строительства, инфраструктуры, корпусов и компонентов."
    },
    {
        selector: ".header-resource.deuterium",
        title: "Дейтерий",
        text: "Топливо и энергетический ресурс для двигателей и высоких технологий."
    },
    {
        selector: ".header-resource.energy",
        title: "Энергия",
        text: "Производство и потребление энергии активной планеты."
    },
    {
        selector: ".materials-card",
        title: "Материалы",
        text: "Добываются Экстрактором материалов. Производство зависит от доступной энергии."
    },
    {
        selector: ".deuterium-card",
        title: "Дейтерий",
        text: "Добывается Дейтериевым экстрактором. Используется как топливо и технологический ресурс."
    },
    {
        selector: ".energy-card",
        title: "Энергетический баланс",
        text: "При нехватке энергии добывающие здания работают с пониженной эффективностью."
    },
    {
        selector: ".sites-card",
        title: "Строительные площадки",
        text: "Количество разных зданий на планете ограничено. Это формирует специализацию колонии."
    },
    {
        selector: "#refreshButton",
        title: "Обновить данные",
        text: "Немедленно запросить актуальное состояние игры у сервера."
    },
    {
        selector: ".lab-status",
        title: "Исследовательская лаборатория",
        text: "Определяет максимальный уровень исследований и влияет на их скорость."
    }
];
const raceNames = {
    Humans: "Люди",
    Synthetics: "Синтетики",
    Insectoids: "Инсектоиды",
    EnergyForms: "Энергоформы"
};

const componentNames = {
    Hull: "Лёгкий корпус",
    Engine: "Базовый двигатель",
    Reactor: "Базовый реактор",
    ControlSystem: "Система управления",
    ColonyModule: "Колонизационный модуль"
};

function localizeRace(race) {
    return raceNames[race] ?? race;
}

function localizeComponentName(component) {
    if (!component) {
        return "Неизвестный компонент";
    }

    const race =
        localizeRace(component.race);

    const type =
        componentNames[component.type] ??
        component.type;

    return `${race} · ${type}`;
}
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
    productionStatus: null,
    components: [],
    blueprints: [],
    designIsValid: false,
    productionDrafts: {}
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

const designerElements = {
    blueprintName:
        document.querySelector("#blueprintName"),
    designerHull:
        document.querySelector("#designerHull"),
    designerEngine:
        document.querySelector("#designerEngine"),
    designerEngineQuantity:
        document.querySelector("#designerEngineQuantity"),
    designerReactor:
        document.querySelector("#designerReactor"),
    designerReactorQuantity:
        document.querySelector("#designerReactorQuantity"),
    designerControl:
        document.querySelector("#designerControl"),
    designerControlQuantity:
        document.querySelector("#designerControlQuantity"),
    designerSpecial:
        document.querySelector("#designerSpecial"),
    designerSpecialQuantity:
        document.querySelector("#designerSpecialQuantity"),
    saveBlueprintButton:
        document.querySelector("#saveBlueprintButton"),
    designStatus:
        document.querySelector("#designStatus"),
    designValidity:
        document.querySelector("#designValidity"),
    designVolume:
        document.querySelector("#designVolume"),
    designVolumeBar:
        document.querySelector("#designVolumeBar"),
    designEnergy:
        document.querySelector("#designEnergy"),
    designEnergyBar:
        document.querySelector("#designEnergyBar"),
    designIntegrity:
        document.querySelector("#designIntegrity"),
    designLocalSpeed:
        document.querySelector("#designLocalSpeed"),
    designInterSpeed:
        document.querySelector("#designInterSpeed"),
    designCommand:
        document.querySelector("#designCommand"),
    designWarnings:
        document.querySelector("#designWarnings"),
    blueprintGrid:
        document.querySelector("#blueprintGrid")
};

Object.assign(elements, designerElements);
const headerResourceElements = {
    headerMaterials:
        document.querySelector("#headerMaterials"),
    headerDeuterium:
        document.querySelector("#headerDeuterium"),
    headerEnergy:
        document.querySelector("#headerEnergy")
};

Object.assign(elements, headerResourceElements);
function applyStaticTooltips() {
    for (const definition of staticTooltipDefinitions) {
        document
            .querySelectorAll(definition.selector)
            .forEach(element => {
                element.dataset.tooltipTitle =
                    definition.title;

                element.dataset.tooltip =
                    definition.text;

                if (!element.hasAttribute("tabindex")) {
                    element.setAttribute("tabindex", "0");
                }
            });
    }
}

function createTooltipSystem() {
    const tooltip = document.createElement("div");

    tooltip.className = "game-tooltip";
    tooltip.setAttribute("role", "tooltip");

    tooltip.innerHTML = `
        <strong class="tooltip-title"></strong>
        <span class="tooltip-content"></span>
        <small class="tooltip-hint"></small>
    `;

    document.body.appendChild(tooltip);

    const titleElement =
        tooltip.querySelector(".tooltip-title");

    const contentElement =
        tooltip.querySelector(".tooltip-content");

    const hintElement =
        tooltip.querySelector(".tooltip-hint");

    let activeTarget = null;
    let longPressTimer = null;
    let longPressTriggered = false;
    let pointerStartX = 0;
    let pointerStartY = 0;

    function positionTooltip(target) {
        const targetRect =
            target.getBoundingClientRect();

        const tooltipRect =
            tooltip.getBoundingClientRect();

        const spacing = 10;
        const viewportPadding = 8;

        let left =
            targetRect.left +
            targetRect.width / 2 -
            tooltipRect.width / 2;

        left = Math.max(
            viewportPadding,
            Math.min(
                left,
                window.innerWidth -
                tooltipRect.width -
                viewportPadding
            )
        );

        let top =
            targetRect.top -
            tooltipRect.height -
            spacing;

        if (top < viewportPadding) {
            top =
                targetRect.bottom +
                spacing;
        }

        if (
            top + tooltipRect.height >
            window.innerHeight - viewportPadding
        ) {
            top =
                window.innerHeight -
                tooltipRect.height -
                viewportPadding;
        }

        tooltip.style.left = `${left}px`;
        tooltip.style.top = `${top}px`;
    }

    function showTooltip(
        target,
        mobile = false)
    {
        if (!target?.dataset.tooltip) {
            return;
        }

        activeTarget = target;

        titleElement.textContent =
            target.dataset.tooltipTitle ?? "Подсказка";

        contentElement.textContent =
            target.dataset.tooltip;

        hintElement.textContent = mobile
            ? "Коснитесь вне подсказки, чтобы закрыть"
            : "";

        tooltip.classList.add("visible");

        requestAnimationFrame(() => {
            positionTooltip(target);
        });
    }

    function hideTooltip() {
        activeTarget = null;
        tooltip.classList.remove("visible");
    }

    function clearLongPress() {
        if (longPressTimer !== null) {
            window.clearTimeout(longPressTimer);
            longPressTimer = null;
        }
    }

    document.addEventListener(
        "pointerover",
        event => {
            if (event.pointerType !== "mouse") {
                return;
            }

            const target =
                event.target.closest("[data-tooltip]");

            if (target) {
                showTooltip(target);
            }
        }
    );

    document.addEventListener(
        "pointerout",
        event => {
            if (event.pointerType !== "mouse") {
                return;
            }

            const target =
                event.target.closest("[data-tooltip]");

            if (
                target &&
                !target.contains(event.relatedTarget)
            ) {
                hideTooltip();
            }
        }
    );

    document.addEventListener(
        "focusin",
        event => {
            const target =
                event.target.closest("[data-tooltip]");

            if (target) {
                showTooltip(target);
            }
        }
    );

    document.addEventListener(
        "focusout",
        event => {
            if (
                activeTarget &&
                !activeTarget.contains(event.relatedTarget)
            ) {
                hideTooltip();
            }
        }
    );

    document.addEventListener(
        "pointerdown",
        event => {
            if (
                event.pointerType !== "touch" &&
                event.pointerType !== "pen"
            ) {
                return;
            }

            const target =
                event.target.closest("[data-tooltip]");

            if (!target) {
                hideTooltip();
                return;
            }

            clearLongPress();

            longPressTriggered = false;
            pointerStartX = event.clientX;
            pointerStartY = event.clientY;

            longPressTimer = window.setTimeout(
                () => {
                    longPressTriggered = true;
                    showTooltip(target, true);

                    if (navigator.vibrate) {
                        navigator.vibrate(18);
                    }
                },
                550
            );
        }
    );

    document.addEventListener(
        "pointermove",
        event => {
            const distance =
                Math.abs(event.clientX - pointerStartX) +
                Math.abs(event.clientY - pointerStartY);

            if (distance > 12) {
                clearLongPress();
            }
        }
    );

    document.addEventListener(
        "pointerup",
        event => {
            clearLongPress();

            if (longPressTriggered) {
                event.preventDefault();
            }
        }
    );

    document.addEventListener(
        "pointercancel",
        clearLongPress
    );

    document.addEventListener(
        "contextmenu",
        event => {
            if (
                event.target.closest("[data-tooltip]") &&
                longPressTriggered
            ) {
                event.preventDefault();
            }
        }
    );

    window.addEventListener(
        "resize",
        () => {
            if (activeTarget) {
                positionTooltip(activeTarget);
            }
        }
    );

    window.addEventListener(
        "scroll",
        hideTooltip,
        true
    );
}

function getComponentTooltip(component) {
    const stats = getComponentStats(component);

    const production =
        component.canManufacture
            ? "Может производиться вашей расой."
            : "Чужая технология: доступна для установки после покупки или обмена.";

    const characteristics =
        stats.length > 0
            ? ` Характеристики: ${stats.join(", ")}.`
            : "";

    return `${production}${characteristics}`;
}
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

    elements.headerMaterials.textContent =
        formatNumber(planet.materials);

    elements.headerDeuterium.textContent =
        formatNumber(planet.deuterium);

    elements.headerEnergy.textContent =
        `${formatNumber(planet.energyProduction)} / ` +
        `${formatNumber(planet.energyConsumption)}`;
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
        function bindDesignerEvents() {
    if (
        !elements.saveBlueprintButton ||
        elements.saveBlueprintButton.dataset.bound === "true"
    ) {
        return;
    }

    elements.saveBlueprintButton.dataset.bound = "true";

    elements.saveBlueprintButton.addEventListener(
        "click",
        saveBlueprint
    );

    document
        .querySelectorAll(
            "[data-designer-module], [data-designer-quantity]"
        )
        .forEach(element => {
            element.addEventListener(
                "input",
                updateDesignPreview
            );

            element.addEventListener(
                "change",
                updateDesignPreview
            );
        });

    elements.designerHull?.addEventListener(
        "change",
        updateDesignPreview
    );
}

applyStaticTooltips();
createTooltipSystem();
bindDesignerEvents();
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
                    <div
                        class="building-icon"
                        tabindex="0"
                        data-tooltip-title="${info.name}"
                        data-tooltip="${info.description}">
                        <svg>
                            <use href="#${info.icon}"></use>
                        </svg>
                    </div>

                    <div class="building-content">
                        <div class="building-title">
                            <h3>${info.name}</h3>
                            <span
                                class="level-badge"
                                tabindex="0"
                                data-tooltip-title="Развитие: ${info.name}"
                                data-tooltip="${
                                    buildingLevelSummaries[
                                        building.building
                                    ] ?? "Повышает эффективность здания."
                                }">
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
        function bindDesignerEvents() {
    if (
        !elements.saveBlueprintButton ||
        elements.saveBlueprintButton.dataset.bound === "true"
    ) {
        return;
    }

    elements.saveBlueprintButton.dataset.bound = "true";

    elements.saveBlueprintButton.addEventListener(
        "click",
        saveBlueprint
    );

    document
        .querySelectorAll(
            "[data-designer-module], [data-designer-quantity]"
        )
        .forEach(element => {
            element.addEventListener(
                "input",
                updateDesignPreview
            );

            element.addEventListener(
                "change",
                updateDesignPreview
            );
        });

    elements.designerHull?.addEventListener(
        "change",
        updateDesignPreview
    );
}

applyStaticTooltips();
createTooltipSystem();
bindDesignerEvents();
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
                        <div
                            class="technology-icon"
                            tabindex="0"
                            data-tooltip-title="${info.name}"
                            data-tooltip="${info.description}">
                            <svg>
                                <use href="#${info.icon}"></use>
                            </svg>
                        </div>

                        <span
                            class="technology-level"
                            tabindex="0"
                            data-tooltip-title="Уровни: ${info.name}"
                            data-tooltip="${
                                technologyLevelSummaries[
                                    technology.technology
                                ] ?? "Открывает новые технологии."
                            }">
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
        await function bindDesignerEvents() {
    if (
        !elements.saveBlueprintButton ||
        elements.saveBlueprintButton.dataset.bound === "true"
    ) {
        return;
    }

    elements.saveBlueprintButton.dataset.bound = "true";

    elements.saveBlueprintButton.addEventListener(
        "click",
        saveBlueprint
    );

    document
        .querySelectorAll(
            "[data-designer-module], [data-designer-quantity]"
        )
        .forEach(element => {
            element.addEventListener(
                "input",
                updateDesignPreview
            );

            element.addEventListener(
                "change",
                updateDesignPreview
            );
        });

    elements.designerHull?.addEventListener(
        "change",
        updateDesignPreview
    );
}

applyStaticTooltips();
createTooltipSystem();
bindDesignerEvents();
loadDashboard();
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

    return component ? localizeComponentName(component) : code;
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

function captureProductionDrafts() {
    document
        .querySelectorAll("[data-quantity-for]")
        .forEach(input => {
            const code = input.dataset.quantityFor;

            state.productionDrafts[code] = {
                ...state.productionDrafts[code],
                quantity: Math.max(
                    1,
                    Number(input.value) || 1
                )
            };
        });

    document
        .querySelectorAll("[data-line-for]")
        .forEach(select => {
            const code = select.dataset.lineFor;

            state.productionDrafts[code] = {
                ...state.productionDrafts[code],
                lineNumber: Math.max(
                    1,
                    Number(select.value) || 1
                )
            };
        });
}

function updateProductionDraft(event) {
    const element = event.currentTarget;

    const code =
        element.dataset.quantityFor ??
        element.dataset.lineFor;

    const draft = state.productionDrafts[code] ?? {
        quantity: 1,
        lineNumber: 1
    };

    if (element.dataset.quantityFor) {
        draft.quantity = Math.max(
            1,
            Number(element.value) || 1
        );
    }

    if (element.dataset.lineFor) {
        draft.lineNumber = Math.max(
            1,
            Number(element.value) || 1
        );
    }

    state.productionDrafts[code] = draft;
}
function renderProduction(status) {
    captureProductionDrafts();
    state.productionStatus = status;

    elements.productionComplexLevel.textContent =
        status.productionComplexLevel;

    elements.productionLineCount.textContent =
        status.lineCount;

    const createLineOptions = selectedLine =>
        Array.from(
            { length: status.lineCount },
            (_, index) => {
                const lineNumber = index + 1;

                return `
                    <option
                        value="${lineNumber}"
                        ${lineNumber === selectedLine
                            ? "selected"
                            : ""}>
                        Линия ${lineNumber}
                    </option>
                `;
            }
        ).join("");

    elements.componentCatalog.innerHTML = status.catalog
        .map(component => {
            const typeInfo =
                componentTypeInfo[component.type] ?? {
                    name: component.type,
                    icon: "icon-factory"
                };

            const draft =
                state.productionDrafts[component.code] ?? {
                    quantity: 1,
                    lineNumber: 1
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
                    <div
                        class="component-icon"
                        tabindex="0"
                        data-tooltip-title="${
                            localizeComponentName(component)
                        }"
                        data-tooltip="${
                            getComponentTooltip(component)
                        }">
                        <svg>
                            <use href="#${typeInfo.icon}"></use>
                        </svg>
                    </div>

                    <div class="component-body">
                        <div class="component-heading">
                            <h4>${localizeComponentName(component)}</h4>
                            <span class="component-type">
                                ${typeInfo.name}
                            </span>
                        </div>

                        <div class="component-race">
                            Инженерная школа: ${localizeRace(component.race)}
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
                                value="${draft.quantity}"
                                aria-label="Количество"
                                data-quantity-for="${component.code}">

                            <select
                                aria-label="Производственная линия"
                                data-line-for="${component.code}">
                                ${createLineOptions(draft.lineNumber)}
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

    elements.componentCatalog
        .querySelectorAll(
            "[data-quantity-for], [data-line-for]"
        )
        .forEach(element => {
            element.addEventListener(
                "input",
                updateProductionDraft
            );

            element.addEventListener(
                "change",
                updateProductionDraft
            );
        });
    const inventoryItems = status.catalog
        .map(component => {
            const inventory = status.inventory.find(
                item => item.componentCode === component.code
            );

            return {
                name: localizeComponentName(component),
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
        await function bindDesignerEvents() {
    if (
        !elements.saveBlueprintButton ||
        elements.saveBlueprintButton.dataset.bound === "true"
    ) {
        return;
    }

    elements.saveBlueprintButton.dataset.bound = "true";

    elements.saveBlueprintButton.addEventListener(
        "click",
        saveBlueprint
    );

    document
        .querySelectorAll(
            "[data-designer-module], [data-designer-quantity]"
        )
        .forEach(element => {
            element.addEventListener(
                "input",
                updateDesignPreview
            );

            element.addEventListener(
                "change",
                updateDesignPreview
            );
        });

    elements.designerHull?.addEventListener(
        "change",
        updateDesignPreview
    );
}

applyStaticTooltips();
createTooltipSystem();
bindDesignerEvents();
loadDashboard();
    } catch (error) {
        showMessage(error.message, true);
    }
}
function componentOption(component) {
    const manufactureMark =
        component.canManufacture ? "своя технология" : "импорт";

    return `
        <option value="${component.code}">
            ${localizeComponentName(component)} · ${manufactureMark}
        </option>
    `;
}

function sortDesignerComponents(components) {
    return [...components].sort((left, right) => {
        if (left.canManufacture !== right.canManufacture) {
            return left.canManufacture ? -1 : 1;
        }

        return left.name.localeCompare(right.name);
    });
}

function renderDesignerOptions() {
    const byType = type =>
        sortDesignerComponents(
            state.components.filter(
                component => component.type === type
            )
        );

    elements.designerHull.innerHTML =
        byType("Hull").map(componentOption).join("");

    elements.designerEngine.innerHTML =
        byType("Engine").map(componentOption).join("");

    elements.designerReactor.innerHTML =
        byType("Reactor").map(componentOption).join("");

    elements.designerControl.innerHTML =
        byType("ControlSystem").map(componentOption).join("");

    elements.designerSpecial.innerHTML =
        `<option value="">Не устанавливать</option>` +
        byType("ColonyModule").map(componentOption).join("");
}

function selectedComponent(selectElement) {
    if (!selectElement) {
        return undefined;
    }

    return state.components.find(
        component => component.code === selectElement.value
    );
}

function readDesignerModules() {
    const selections = [
        {
            select: elements.designerEngine,
            quantity: elements.designerEngineQuantity
        },
        {
            select: elements.designerReactor,
            quantity: elements.designerReactorQuantity
        },
        {
            select: elements.designerControl,
            quantity: elements.designerControlQuantity
        },
        {
            select: elements.designerSpecial,
            quantity: elements.designerSpecialQuantity
        }
    ];

    return selections
        .filter(selection => selection.select.value)
        .map(selection => ({
            component:
                selectedComponent(selection.select),
            componentCode:
                selection.select.value,
            quantity: Math.max(
                1,
                Number(selection.quantity.value) || 1
            )
        }));
}

function updateDesignPreview() {
    const hull = selectedComponent(
        elements.designerHull
    );

    const modules = readDesignerModules();
    const warnings = [];

    if (!hull) {
        state.designIsValid = false;
        return;
    }

    const hasEngine = modules.some(
        module => module.component?.type === "Engine"
    );

    const hasReactor = modules.some(
        module => module.component?.type === "Reactor"
    );

    const hasControl = modules.some(
        module => module.component?.type === "ControlSystem"
    );

    if (!hasEngine) {
        warnings.push("Не установлен обязательный двигатель.");
    }

    if (!hasReactor) {
        warnings.push("Не установлен обязательный реактор.");
    }

    if (!hasControl) {
        warnings.push("Не установлена система управления.");
    }

    const usedVolume = modules.reduce(
        (total, module) =>
            total +
            (module.component?.volume ?? 0) *
            module.quantity,
        0
    );

    const energyProduction = modules.reduce(
        (total, module) =>
            total +
            (module.component?.energyOutput ?? 0) *
            module.quantity,
        0
    );

    const energyConsumption = modules.reduce(
        (total, module) =>
            total +
            (module.component?.energyConsumption ?? 0) *
            module.quantity,
        0
    );

    const localSpeed = modules.reduce(
        (total, module) =>
            total +
            (module.component?.inSystemSpeed ?? 0) *
            module.quantity,
        0
    );

    const interSpeed = modules.reduce(
        (total, module) =>
            total +
            (module.component?.interSystemSpeed ?? 0) *
            module.quantity,
        0
    );

    const commandRating = modules.reduce(
        (total, module) =>
            total +
            (module.component?.commandRating ?? 0) *
            module.quantity,
        0
    );

    if (usedVolume > hull.capacity) {
        warnings.push(
            `Превышена вместимость корпуса на ` +
            `${formatNumber(usedVolume - hull.capacity)}.`
        );
    }

    if (energyConsumption > energyProduction) {
        warnings.push(
            `Дефицит энергии: ` +
            `${formatNumber(energyConsumption - energyProduction)}.`
        );
    }

    state.designIsValid =
        warnings.length === 0;

    elements.designStatus.textContent =
        state.designIsValid
            ? "Проект готов к сохранению"
            : "Обнаружены инженерные проблемы";

    elements.designValidity.textContent =
        state.designIsValid
            ? "КОНФИГУРАЦИЯ ВАЛИДНА"
            : "ТРЕБУЕТ ДОРАБОТКИ";

    elements.designValidity.classList.toggle(
        "valid",
        state.designIsValid
    );

    elements.designValidity.classList.toggle(
        "invalid",
        !state.designIsValid
    );

    elements.designVolume.textContent =
        `${formatNumber(usedVolume)} / ` +
        `${formatNumber(hull.capacity)}`;

    elements.designEnergy.textContent =
        `${formatNumber(energyConsumption)} / ` +
        `${formatNumber(energyProduction)}`;

    const volumePercent = Math.min(
        100,
        hull.capacity > 0
            ? usedVolume / hull.capacity * 100
            : 0
    );

    const energyPercent = Math.min(
        100,
        energyProduction > 0
            ? energyConsumption / energyProduction * 100
            : 100
    );

    elements.designVolumeBar.style.width =
        `${volumePercent}%`;

    elements.designEnergyBar.style.width =
        `${energyPercent}%`;

    elements.designVolumeBar.classList.toggle(
        "overloaded",
        usedVolume > hull.capacity
    );

    elements.designEnergyBar.classList.toggle(
        "overloaded",
        energyConsumption > energyProduction
    );

    elements.designIntegrity.textContent =
        formatNumber(hull.structuralIntegrity);

    elements.designLocalSpeed.textContent =
        formatNumber(localSpeed);

    elements.designInterSpeed.textContent =
        formatNumber(interSpeed);

    elements.designCommand.textContent =
        formatNumber(commandRating);

    elements.designWarnings.innerHTML =
        warnings.length > 0
            ? warnings
                .map(warning => `
                    <div class="design-warning">
                        ${warning}
                    </div>
                `)
                .join("")
            : `
                <div class="design-warning success">
                    Все обязательные системы установлены.
                    Объём и энергетический баланс соблюдены.
                </div>
            `;

    elements.saveBlueprintButton.disabled =
        !state.designIsValid;
}

function renderBlueprints() {
    elements.blueprintGrid.innerHTML =
        state.blueprints.length > 0
            ? state.blueprints
                .map(blueprint => `
                    <article class="blueprint-card">
                        <div class="component-heading">
                            <h4>${blueprint.name}</h4>
                            <span class="blueprint-version">
                                Mk.${blueprint.version}
                            </span>
                        </div>

                        <p>
                            Корпус: ${getComponentName(blueprint.hullCode)}<br>
                            Модулей: ${
                                blueprint.modules.reduce(
                                    (total, module) =>
                                        total + module.quantity,
                                    0
                                )
                            }
                        </p>

                        <div class="blueprint-stats">
                            <span class="component-stat">
                                Объём
                                ${formatNumber(
                                    blueprint.design.usedVolume
                                )}
                                /
                                ${formatNumber(
                                    blueprint.design.hullCapacity
                                )}
                            </span>
                            <span class="component-stat">
                                Энергия
                                ${formatNumber(
                                    blueprint.design.energyConsumption
                                )}
                                /
                                ${formatNumber(
                                    blueprint.design.energyProduction
                                )}
                            </span>
                        </div>
                    </article>
                `)
                .join("")
            : `
                <div class="empty-state">
                    Сохранённых проектов пока нет.
                </div>
            `;
}

function renderShipDesigner(
    components,
    blueprints)
{
    const firstLoad =
        state.components.length === 0;

    state.components = components;
    state.blueprints = blueprints;

    if (firstLoad) {
        renderDesignerOptions();
    }

    updateDesignPreview();
    renderBlueprints();
}

async function saveBlueprint() {
    const name =
        elements.blueprintName.value.trim();

    if (name.length < 3) {
        showMessage(
            "Название должно содержать минимум 3 символа.",
            true
        );

        return;
    }

    if (!state.designIsValid) {
        showMessage(
            "Исправьте инженерные ошибки проекта.",
            true
        );

        return;
    }

    const modules = readDesignerModules()
        .map(module => ({
            componentCode: module.componentCode,
            quantity: module.quantity
        }));

    try {
        await api(
            "/api/game/blueprints/",
            {
                method: "POST",
                body: JSON.stringify({
                    name,
                    hullCode: elements.designerHull.value,
                    modules
                })
            }
        );

        showMessage("Проект корабля сохранён.");
        await function bindDesignerEvents() {
    if (
        !elements.saveBlueprintButton ||
        elements.saveBlueprintButton.dataset.bound === "true"
    ) {
        return;
    }

    elements.saveBlueprintButton.dataset.bound = "true";

    elements.saveBlueprintButton.addEventListener(
        "click",
        saveBlueprint
    );

    document
        .querySelectorAll(
            "[data-designer-module], [data-designer-quantity]"
        )
        .forEach(element => {
            element.addEventListener(
                "input",
                updateDesignPreview
            );

            element.addEventListener(
                "change",
                updateDesignPreview
            );
        });

    elements.designerHull?.addEventListener(
        "change",
        updateDesignPreview
    );
}

applyStaticTooltips();
createTooltipSystem();
bindDesignerEvents();
loadDashboard();
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

        const [
            buildings,
            research,
            production,
            components,
            blueprints
        ] = await Promise.all([
            api(
                `/api/game/buildings/?planetId=${state.activePlanetId}`
            ),
            api(
                `/api/game/research/?planetId=${state.activePlanetId}`
            ),
            api(
                `/api/game/production/?planetId=${state.activePlanetId}`
            ),
            api("/api/game/components"),
            api("/api/game/blueprints/")
        ]);

        renderBuildings(buildings);
        renderResearch(research);
        renderProduction(production);
        renderShipDesigner(components, blueprints);
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
        await function bindDesignerEvents() {
    if (
        !elements.saveBlueprintButton ||
        elements.saveBlueprintButton.dataset.bound === "true"
    ) {
        return;
    }

    elements.saveBlueprintButton.dataset.bound = "true";

    elements.saveBlueprintButton.addEventListener(
        "click",
        saveBlueprint
    );

    document
        .querySelectorAll(
            "[data-designer-module], [data-designer-quantity]"
        )
        .forEach(element => {
            element.addEventListener(
                "input",
                updateDesignPreview
            );

            element.addEventListener(
                "change",
                updateDesignPreview
            );
        });

    elements.designerHull?.addEventListener(
        "change",
        updateDesignPreview
    );
}

applyStaticTooltips();
createTooltipSystem();
bindDesignerEvents();
loadDashboard();
    } catch (error) {
        showMessage(error.message, true);
    }
}

elements.planetSelect.addEventListener("change", event => {
    state.activePlanetId = event.target.value;
    function bindDesignerEvents() {
    if (
        !elements.saveBlueprintButton ||
        elements.saveBlueprintButton.dataset.bound === "true"
    ) {
        return;
    }

    elements.saveBlueprintButton.dataset.bound = "true";

    elements.saveBlueprintButton.addEventListener(
        "click",
        saveBlueprint
    );

    document
        .querySelectorAll(
            "[data-designer-module], [data-designer-quantity]"
        )
        .forEach(element => {
            element.addEventListener(
                "input",
                updateDesignPreview
            );

            element.addEventListener(
                "change",
                updateDesignPreview
            );
        });

    elements.designerHull?.addEventListener(
        "change",
        updateDesignPreview
    );
}

applyStaticTooltips();
createTooltipSystem();
bindDesignerEvents();
loadDashboard();
});

elements.refreshButton.addEventListener("click", loadDashboard);
elements.saveBlueprintButton.addEventListener(
    "click",
    saveBlueprint
);

document
    .querySelectorAll(
        "[data-designer-module], [data-designer-quantity]"
    )
    .forEach(element => {
        element.addEventListener(
            "input",
            updateDesignPreview
        );

        element.addEventListener(
            "change",
            updateDesignPreview
        );
    });

function bindDesignerEvents() {
    if (
        !elements.saveBlueprintButton ||
        elements.saveBlueprintButton.dataset.bound === "true"
    ) {
        return;
    }

    elements.saveBlueprintButton.dataset.bound = "true";

    elements.saveBlueprintButton.addEventListener(
        "click",
        saveBlueprint
    );

    document
        .querySelectorAll(
            "[data-designer-module], [data-designer-quantity]"
        )
        .forEach(element => {
            element.addEventListener(
                "input",
                updateDesignPreview
            );

            element.addEventListener(
                "change",
                updateDesignPreview
            );
        });

    elements.designerHull?.addEventListener(
        "change",
        updateDesignPreview
    );
}

applyStaticTooltips();
createTooltipSystem();
bindDesignerEvents();
loadDashboard();
window.setInterval(loadDashboard, 5000);
window.setInterval(updateQueueCountdown, 1000);
window.setInterval(updateResearchCountdown, 1000);
window.setInterval(updateProductionCountdowns, 1000);
