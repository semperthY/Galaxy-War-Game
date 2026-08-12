window.AssemblyUi = (() => {
    const state = {
        status: null,
        blueprints: [],
        selectedBlueprintId: null,
        quantity: 1,
        activeOrderId: null,
        completionRequested: false
    };

    let context;
    let timerStarted = false;

    const elements = {};

    function init(options) {
        context = options;

        Object.assign(elements, {
            level: document.querySelector("#assemblyComplexLevel"),
            blueprint: document.querySelector("#assemblyBlueprint"),
            quantity: document.querySelector("#assemblyQuantity"),
            requirements: document.querySelector("#assemblyRequirements"),
            button: document.querySelector("#assemblyButton"),
            queue: document.querySelector("#assemblyQueue"),
            reserveCount: document.querySelector("#reserveCount"),
            reserveGrid: document.querySelector("#reserveGrid")
        });

        elements.blueprint.addEventListener("change", () => {
            state.selectedBlueprintId = elements.blueprint.value;
            renderRequirements();
        });

        elements.quantity.addEventListener("input", () => {
            state.quantity = clampQuantity(elements.quantity.value);
            elements.quantity.value = state.quantity;
            renderRequirements();
        });

        elements.button.addEventListener("click", startOrder);

        if (!timerStarted) {
            window.setInterval(updateCountdowns, 1000);
            timerStarted = true;
        }
    }

    function clampQuantity(value) {
        return Math.min(20, Math.max(1, Number(value) || 1));
    }

    function selectedBlueprint() {
        return state.blueprints.find(
            blueprint => blueprint.id === state.selectedBlueprintId
        );
    }

    function inventoryQuantity(componentCode) {
        return state.status?.inventory.find(
            item => item.componentCode === componentCode
        )?.quantity ?? 0;
    }

    function renderRequirements() {
        const blueprint = selectedBlueprint();

        if (!blueprint) {
            elements.requirements.innerHTML = `
                <div class="empty-state">
                    Сначала сохраните проект корабля.
                </div>
            `;
            elements.button.disabled = true;
            return;
        }

        let hasEverything = true;

        elements.requirements.innerHTML =
            blueprint.design.requiredComponents.map(requirement => {
                const required = requirement.quantity * state.quantity;
                const available = inventoryQuantity(
                    requirement.componentCode
                );
                const enough = available >= required;

                hasEverything &&= enough;

                return `
                    <div class="assembly-requirement ${enough ? "ready" : "missing"}">
                        <span>
                            ${context.componentName(requirement.componentCode)}
                        </span>
                        <strong>${available} / ${required}</strong>
                    </div>
                `;
            }).join("");

        elements.button.disabled =
            state.status.assemblyComplexLevel < 1 ||
            !hasEverything;
    }

    function formatCountdown(completesAt) {
        if (!completesAt) {
            return "Ожидает запуска";
        }

        const seconds = Math.max(
            0,
            Math.ceil(
                (new Date(completesAt).getTime() - Date.now()) / 1000
            )
        );

        const minutes = Math.floor(seconds / 60);
        const remainder = seconds % 60;

        return minutes > 0
            ? `${minutes}:${remainder.toString().padStart(2, "0")}`
            : `${seconds} сек.`;
    }

    function updateCountdowns() {
        document
            .querySelectorAll("[data-assembly-completes]")
            .forEach(element => {
                const completesAt = element.dataset.assemblyCompletes;
                element.textContent = formatCountdown(completesAt);

                if (
                    new Date(completesAt).getTime() <= Date.now() &&
                    !state.completionRequested
                ) {
                    state.completionRequested = true;
                    context.reload();
                }
            });
    }

    function renderQueue() {
        elements.queue.innerHTML = state.status.orders.length > 0
            ? state.status.orders.map(order => `
                <article class="assembly-order ${order.completesAt ? "active" : "waiting"}">
                    <span class="assembly-position">
                        ${order.queuePosition.toString().padStart(2, "0")}
                    </span>
                    <div>
                        <strong>
                            ${order.blueprintName} Mk.${order.blueprintVersion}
                        </strong>
                        <small>Серия: ${order.quantity} шт.</small>
                    </div>
                    <time ${order.completesAt
                        ? `data-assembly-completes="${order.completesAt}"`
                        : ""}>
                        ${formatCountdown(order.completesAt)}
                    </time>
                </article>
            `).join("")
            : '<div class="empty-state">Сборочная линия свободна.</div>';

        updateCountdowns();
    }

    function renderReserve() {
        const groups = Array.from(
            state.status.reserve.reduce((result, ship) => {
                const key = ship.blueprintId;
                const group = result.get(key) ?? {
                    blueprintId: ship.blueprintId,
                    blueprintName: ship.blueprintName,
                    blueprintVersion: ship.blueprintVersion,
                    ships: []
                };

                group.ships.push(ship);
                result.set(key, group);
                return result;
            }, new Map()).values()
        ).sort((left, right) =>
            left.blueprintName.localeCompare(right.blueprintName, "ru")
        );

        elements.reserveCount.textContent =
            `${state.status.reserve.length} кораблей`;

        elements.reserveGrid.innerHTML = groups.length > 0
            ? groups.map(group => `
                <article class="reserve-ship">
                    <div class="reserve-ship-icon">
                        <svg><use href="#icon-ship"></use></svg>
                        <i></i>
                    </div>
                    <div>
                        <small>ПРОЕКТ · ГОТОВЫ К НАЗНАЧЕНИЮ</small>
                        <h4>
                            ${group.blueprintName} Mk.${group.blueprintVersion}
                        </h4>
                        <p>
                            В резерве: <strong>${group.ships.length}</strong>
                        </p>
                    </div>
                </article>
            `).join("")
            : '<div class="empty-state">В резерве пока нет кораблей.</div>';
    }

    function render(status, blueprints) {
        state.status = status;
        state.blueprints = blueprints;
        elements.level.textContent = status.assemblyComplexLevel;

        const activeOrder = status.orders.find(order => order.completesAt);

        if (activeOrder?.id !== state.activeOrderId) {
            state.activeOrderId = activeOrder?.id ?? null;
            state.completionRequested = false;
        }

        if (!blueprints.some(x => x.id === state.selectedBlueprintId)) {
            state.selectedBlueprintId = blueprints[0]?.id ?? null;
        }

        elements.blueprint.innerHTML = blueprints.length > 0
            ? blueprints.map(blueprint => `
                <option value="${blueprint.id}">
                    ${blueprint.name} Mk.${blueprint.version}
                </option>
            `).join("")
            : '<option value="">Нет сохранённых проектов</option>';

        if (state.selectedBlueprintId) {
            elements.blueprint.value = state.selectedBlueprintId;
        }

        elements.quantity.value = state.quantity;

        renderRequirements();
        renderQueue();
        renderReserve();
    }

    async function startOrder() {
        const blueprint = selectedBlueprint();

        if (!blueprint) {
            return;
        }

        try {
            const status = await context.api(
                `/api/game/assembly/orders?planetId=${context.planetId()}`,
                {
                    method: "POST",
                    body: JSON.stringify({
                        blueprintId: blueprint.id,
                        quantity: state.quantity
                    })
                }
            );

            context.message("Корабли поставлены в очередь сборки.");
            render(status, state.blueprints);
            await context.reload();
        } catch (error) {
            context.message(error.message, true);
        }
    }

    return { init, render };
})();
