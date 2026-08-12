window.GalaxyUi = (() => {
    const state = {
        galaxy: [],
        assembly: null,
        blueprints: [],
        components: [],
        colonization: [],
        activePlanet: null,
        selectedSystemId: null,
        selectedPlanetId: null,
        selectedShipId: null
    };

    let context;
    const elements = {};

    function init(options) {
        context = options;

        Object.assign(elements, {
            systemSelect: document.querySelector("#galaxySystemSelect"),
            systemName: document.querySelector("#galaxySystemName"),
            systemCoordinates: document.querySelector("#galaxySystemCoordinates"),
            systemMap: document.querySelector("#systemMap"),
            targetName: document.querySelector("#colonizationTargetName"),
            targetStatus: document.querySelector("#colonizationTargetStatus"),
            shipSelect: document.querySelector("#colonyShipSelect"),
            colonizeButton: document.querySelector("#colonizeButton")
        });

        elements.systemSelect.addEventListener("change", event => {
            state.selectedSystemId = event.target.value;
            state.selectedPlanetId = null;
            renderSelectedSystem();
        });

        elements.shipSelect.addEventListener("change", event => {
            state.selectedShipId = event.target.value || null;
            renderColonizationPanel();
        });

        elements.colonizeButton.addEventListener(
            "click",
            colonizeSelectedPlanet
        );
    }

    function selectedSystem() {
        return state.galaxy.find(
            system => system.id === state.selectedSystemId
        );
    }

    function selectedPlanet() {
        return selectedSystem()?.planets.find(
            planet => planet.id === state.selectedPlanetId
        );
    }

    function activeSystem() {
        return state.galaxy.find(system =>
            system.galaxy === state.activePlanet?.galaxy &&
            system.system === state.activePlanet?.system
        );
    }

    function colonyShips() {
        const colonyComponentCodes = new Set(
            state.components
                .filter(component => component.type === "ColonyModule")
                .map(component => component.code)
        );

        const colonyBlueprintIds = new Set(
            state.blueprints
                .filter(blueprint => blueprint.modules.some(module =>
                    colonyComponentCodes.has(module.componentCode)
                ))
                .map(blueprint => blueprint.id)
        );

        return state.assembly.reserve.filter(ship =>
            colonyBlueprintIds.has(ship.blueprintId)
        );
    }

    function renderSystemSelector() {
        elements.systemSelect.innerHTML = state.galaxy
            .map(system => `
                <option value="${system.id}">
                    ${system.galaxy}:${system.system} · ${system.name}
                </option>
            `)
            .join("");

        elements.systemSelect.value = state.selectedSystemId;
    }

    function renderSelectedSystem() {
        const system = selectedSystem();

        if (!system) {
            return;
        }

        elements.systemName.textContent = system.name;
        elements.systemCoordinates.textContent =
            `Галактика ${system.galaxy} · Система ${system.system}`;

        elements.systemMap.innerHTML = `
            <div class="system-star">
                <span></span>
                <strong>${system.system}</strong>
            </div>
            <div class="system-planets">
                ${system.planets.map(planet => {
                    const owned = planet.playerId !== null;
                    const selected = planet.id === state.selectedPlanetId;
                    const deploying = state.colonization.some(
                        operation =>
                            operation.targetPlanetId === planet.id
                    );

                    return `
                        <button
                            class="system-planet ${owned ? "owned" : "neutral"} ${deploying ? "deploying" : ""} ${selected ? "selected" : ""}"
                            data-galaxy-planet="${planet.id}"
                            style="--planet-index:${planet.position}">
                            <span class="planet-sphere"></span>
                            <span class="planet-orbit">ОРБИТА ${planet.position}</span>
                            <strong>${planet.name}</strong>
                            <small>
                                ${owned
                                    ? `Колония · ${planet.playerName}`
                                    : deploying
                                        ? "Развёртывание колонии"
                                        : "Нейтральная планета"}
                            </small>
                        </button>
                    `;
                }).join("")}
            </div>
        `;

        elements.systemMap
            .querySelectorAll("[data-galaxy-planet]")
            .forEach(button => {
                button.addEventListener("click", () => {
                    state.selectedPlanetId = button.dataset.galaxyPlanet;
                    renderSelectedSystem();
                });
            });

        renderColonizationPanel();
    }

    function renderColonizationPanel() {
        const planet = selectedPlanet();
        const system = selectedSystem();
        const currentSystem = activeSystem();
        const ships = colonyShips();
        const pendingOperation = state.colonization.find(
            operation => operation.targetPlanetId === planet?.id
        );
        const sameSystem = system?.id === currentSystem?.id;
        const canColonize =
            planet &&
            planet.playerId === null &&
            sameSystem &&
            !pendingOperation &&
            ships.length > 0;

        elements.targetName.textContent =
            planet?.name ?? "Цель не выбрана";

        if (!planet) {
            elements.targetStatus.textContent =
                "Выберите нейтральную планету на карте.";
        } else if (planet.playerId !== null) {
            elements.targetStatus.textContent =
                "Планета уже принадлежит игроку.";
        } else if (pendingOperation) {
            elements.targetStatus.textContent =
                "Развёртывание колонии уже выполняется.";
        } else if (!sameSystem) {
            elements.targetStatus.textContent =
                "Колонизация пока доступна только в активной системе.";
        } else if (ships.length === 0) {
            elements.targetStatus.textContent =
                "В резерве активной планеты нет колонизатора.";
        } else {
            elements.targetStatus.textContent =
                "Развёртывание займёт 30 минут. Колонизатор будет израсходован.";
        }

        if (!ships.some(ship => ship.id === state.selectedShipId)) {
            state.selectedShipId = ships[0]?.id ?? null;
        }

        elements.shipSelect.innerHTML = ships.length > 0
            ? ships.map(ship => `
                <option value="${ship.id}">
                    ${ship.name}
                </option>
            `).join("")
            : '<option value="">Нет доступных кораблей</option>';

        if (state.selectedShipId) {
            elements.shipSelect.value = state.selectedShipId;
        }

        elements.colonizeButton.disabled = !canColonize;
    }

    function render(
        galaxy,
        assembly,
        blueprints,
        components,
        activePlanet,
        colonization)
    {
        state.galaxy = galaxy;
        state.assembly = assembly;
        state.blueprints = blueprints;
        state.components = components;
        state.activePlanet = activePlanet;
        state.colonization = colonization;

        const currentSystem = activeSystem();

        if (!state.galaxy.some(x => x.id === state.selectedSystemId)) {
            state.selectedSystemId = currentSystem?.id ?? galaxy[0]?.id;
        }

        const target = selectedPlanet();

        if (target?.playerId !== null) {
            state.selectedPlanetId = null;
        }

        renderSystemSelector();
        renderSelectedSystem();
    }

    async function colonizeSelectedPlanet() {
        const planet = selectedPlanet();

        if (!planet || !state.selectedShipId) {
            return;
        }

        try {
            await context.api(
                `/api/game/colonization/${planet.id}`,
                {
                    method: "POST",
                    body: JSON.stringify({
                        shipId: state.selectedShipId
                    })
                }
            );

            state.selectedPlanetId = null;
            state.selectedShipId = null;
            context.message(
                "Колонизатор отправлен. Развёртывание займёт 30 минут."
            );
            await context.reload();
        } catch (error) {
            context.message(error.message, true);
        }
    }

    return { init, render };
})();
