window.LivingGalaxyUi = (() => {
    const state = { fleets: [], assembly: null, planet: null, selectedFleetId: null, draftFleetId: null, draft: [], reserveDraft: {}, system: null, battles: [] };
    let context;
    const $ = selector => document.querySelector(selector);
    const esc = value => String(value ?? "").replace(/[&<>"']/g, char => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[char]));
    const names = { Landed: "На планете", Orbiting: "На орбите", Executing: "В полёте", Patrolling: "Патруль", Mining: "Добыча", InBattle: "В бою", Flight: "Полёт", Patrol: "Патруль", Attack: "Атака", Return: "Возврат", Recon: "Разведка", Mine: "Добыча", LoadUnload: "Погрузка / выгрузка" };

    function init(options) {
        context = options;
        document.querySelectorAll("[data-fleet-tab]").forEach(button => button.addEventListener("click", () => showTab(button.dataset.fleetTab)));
        $("#createFleetButton")?.addEventListener("click", createFleet);
        $("#fleetReservePicker")?.addEventListener(
            "input",
            reserveQuantityChanged
        );
        $("#planFleetSelect")?.addEventListener("change", event => selectFleet(event.target.value));
        $("#addCommandButton")?.addEventListener("click", addCommand);
        $("#savePlanButton")?.addEventListener("click", savePlan);
        $("#launchFleetButton")?.addEventListener("click", launch);
        $("#scanSystemButton")?.addEventListener("click", scanFromInputs);
        $("#fleetGrid")?.addEventListener("click", fleetAction);
        $("#flightPlanList")?.addEventListener("click", commandAction);
        $("#spaceObjectGrid")?.addEventListener("click", targetAction);
        $("#contactGrid")?.addEventListener("click", targetAction);
        $("#serviceGrid")?.addEventListener("click", serviceAction);
        $("#battleGrid")?.addEventListener("click", battleAction);
    }

    function showTab(tab) {
        document.querySelectorAll("[data-fleet-tab]").forEach(x => x.classList.toggle("active", x.dataset.fleetTab === tab));
        document.querySelectorAll("[data-fleet-view]").forEach(x => x.classList.toggle("active", x.dataset.fleetView === tab));
    }

    async function render(fleets, assembly, planet) {
        state.fleets = fleets; state.assembly = assembly; state.planet = planet;
        if (!fleets.some(x => x.id === state.selectedFleetId)) state.selectedFleetId = fleets[0]?.id ?? null;
        if (state.draftFleetId !== state.selectedFleetId) {
            state.draftFleetId = state.selectedFleetId;
            state.draft = selectedFleet()?.commands?.filter(x => x.status === "Planned").map(stripCommand) ?? [];
        }
        $("#fleetSummary").textContent = `${fleets.length} групп · ${fleets.filter(x => x.status === "Orbiting").length} на орбите`;
        renderReserve(); renderFleetGrid(); renderFleetSelect(); renderPlan(); renderService();
    }

    function renderReserve() {
        const reserve = state.assembly?.reserve ?? [];
        const groups = groupReserve(reserve);
        const availableBlueprints = new Set(
            groups.map(group => group.blueprintId)
        );

        Object.keys(state.reserveDraft).forEach(blueprintId => {
            if (!availableBlueprints.has(blueprintId)) {
                delete state.reserveDraft[blueprintId];
            }
        });

        $("#fleetReservePicker").innerHTML = groups.length
            ? groups.map(group => {
                const selected = Math.min(
                    state.reserveDraft[group.blueprintId] ?? 0,
                    group.ships.length
                );
                state.reserveDraft[group.blueprintId] = selected;

                return `<article class="reserve-pick">
                    <svg width="22" height="22">
                        <use href="#icon-ship"></use>
                    </svg>
                    <span>
                        <strong>${esc(group.blueprintName)}</strong>
                        <small>
                            Mk.${group.blueprintVersion} · в резерве
                            ${group.ships.length}
                        </small>
                    </span>
                    <div class="reserve-quantity-control">
                        <small>В ФЛОТ</small>
                        <input
                            class="reserve-quantity"
                            type="number"
                            min="0"
                            max="${group.ships.length}"
                            inputmode="numeric"
                            value="${selected}"
                            aria-label="Количество ${esc(group.blueprintName)}"
                            data-reserve-blueprint="${group.blueprintId}">
                    </div>
                </article>`;
            }).join("")
            : `<div class="empty-living">
                Соберите корабли — свободный резерв пуст.
            </div>`;
    }

    function groupReserve(reserve) {
        return Array.from(reserve.reduce((result, ship) => {
            const group = result.get(ship.blueprintId) ?? {
                blueprintId: ship.blueprintId,
                blueprintName: ship.blueprintName,
                blueprintVersion: ship.blueprintVersion,
                ships: []
            };

            group.ships.push(ship);
            result.set(ship.blueprintId, group);
            return result;
        }, new Map()).values()).sort((left, right) =>
            left.blueprintName.localeCompare(right.blueprintName, "ru")
        );
    }

    function reserveQuantityChanged(event) {
        const input = event.target.closest("[data-reserve-blueprint]");
        if (!input) return;

        const blueprintId = input.dataset.reserveBlueprint;
        if (input.value.trim() === "") {
            delete state.reserveDraft[blueprintId];
            return;
        }

        const maximum = Number(input.max);
        const quantity = Number(input.value);
        state.reserveDraft[blueprintId] = Number.isInteger(quantity)
            ? Math.min(maximum, Math.max(0, quantity))
            : 0;
    }

    function renderFleetGrid() {
        $("#fleetGrid").innerHTML = state.fleets.length ? state.fleets.map(fleet => `<article class="fleet-card ${fleet.id === state.selectedFleetId ? "selected" : ""}" data-fleet-id="${fleet.id}"><header><strong>${esc(fleet.name)}</strong><span class="status-pill ${fleet.status === "InBattle" ? "danger" : ""}">${names[fleet.status] ?? fleet.status}</span></header><p>${fleet.ships.length} кораблей · ${fleet.galaxyNumber}:${fleet.systemNumber}:${fleet.position}<br>Трюм ${fmt(fleet.materialsCargo + fleet.deuteriumCargo)} / ${fmt(fleet.cargoCapacity)} · топливо ${fmt(fleet.fuelReserve)}</p><div class="battle-actions"><button data-action="plan">Полётник</button>${fleet.status === "Orbiting" ? `<button data-action="land">Посадка</button>` : ""}</div></article>`).join("") : `<div class="empty-living">Сформируйте первую группу из кораблей резерва.</div>`;
    }

    function renderFleetSelect() {
        $("#planFleetSelect").innerHTML = state.fleets.map(x => `<option value="${x.id}" ${x.id === state.selectedFleetId ? "selected" : ""}>${esc(x.name)} · ${names[x.status] ?? x.status}</option>`).join("");
    }

    function renderPlan() {
        const fleet = selectedFleet();
        if (!fleet) { $("#flightPlanList").innerHTML = `<div class="empty-living">Сначала сформируйте флот.</div>`; return; }
        const commands = fleet.status === "Landed" || fleet.status === "Orbiting" ? state.draft : fleet.commands;
        $("#flightPlanList").innerHTML = commands.length ? commands.map((command, index) => {
            const sequence = command.sequence ?? index + 1;
            const locked = !["Landed", "Orbiting"].includes(fleet.status) && sequence !== fleet.editableSequence;
            const target = command.targetFleetId ? "флот" : command.targetObjectId ? "объект" : [command.targetGalaxy, command.targetSystem, command.targetPosition].filter(x => x != null).join(":");
            return `<article class="command-card ${locked ? "locked" : ""} ${command.status === "Active" ? "current" : ""}"><span class="command-number">${sequence}</span><div><header><strong>${names[command.type] ?? command.type}</strong><small>${command.status ?? "Planned"}</small></header><small>${target || "контекстная цель"}${command.outcome ? ` · ${esc(command.outcome)}` : ""}</small></div>${!locked && ["Landed", "Orbiting"].includes(fleet.status) ? `<button data-remove-command="${index}">×</button>` : ""}</article>`;
        }).join("") : `<div class="empty-living">Полётный лист пуст. Добавьте первую команду.</div>`;
        const editableBeforeStart = ["Landed", "Orbiting"].includes(fleet.status);
        $("#savePlanButton").hidden = !editableBeforeStart; $("#launchFleetButton").hidden = !editableBeforeStart;
        $("#addCommandButton").textContent = editableBeforeStart ? "Добавить команду" : `Назначить команду № ${fleet.editableSequence}`;
        renderTargets();
    }

    function renderService() {
        const landed = state.fleets.filter(x => x.status === "Landed").flatMap(fleet => fleet.ships.map(ship => ({ fleet, ship })));
        $("#serviceGrid").innerHTML = landed.length ? landed.map(({ fleet, ship }) => `<article class="service-card"><header><strong>${esc(ship.name)}</strong><span class="status-pill">${esc(fleet.name)}</span></header><p>Корпус ${fmt(ship.hull)} / ${fmt(ship.maxHull)} · щит ${fmt(ship.shield)} / ${fmt(ship.maxShield)}</p><div class="battle-actions">${ship.shield < ship.maxShield ? `<button data-service="ShieldRecharge" data-ship="${ship.id}">Зарядить щит</button>` : ""}${ship.hull < ship.maxHull ? `<button data-service="HullRepair" data-ship="${ship.id}">Ремонт корпуса</button>` : ""}</div></article>`).join("") : `<div class="empty-living">Посадите повреждённый флот на домашнюю планету.</div>`;
    }

    async function renderOperations(system) {
        state.system = system;
        $("#operationsGalaxy").value = system.galaxy; $("#operationsSystem").value = system.system;
        const objects = [...system.fields.map(x => ({ ...x, kind: "field" })), ...system.debris.map(x => ({ ...x, name: "Поле обломков", kind: "debris" }))];
        $("#spaceObjectGrid").innerHTML = objects.length ? objects.map(x => { const max = (x.maxMaterials ?? x.materials) + (x.maxDeuterium ?? x.deuterium); const current = x.materials + x.deuterium; return `<article class="space-card"><header><strong>${esc(x.name)}</strong><span class="status-pill">P${x.position}</span></header><p>${fmt(x.materials)} M · ${fmt(x.deuterium)} D${x.threat ? `<br>Риск ${x.threat}/5 · поток ${fmt(x.throughputPerHour)}/ч` : ""}</p><div class="resource-meter"><i style="width:${Math.min(100, max ? current / max * 100 : 0)}%"></i></div><button data-target-object="${x.id}" data-position="${x.position}">В полётник</button></article>`; }).join("") : `<div class="empty-living">Объекты не обнаружены.</div>`;
        $("#contactGrid").innerHTML = system.fleets.length ? system.fleets.map(x => `<article class="space-card"><header><strong>${esc(x.name)}</strong><span class="status-pill ${x.isPirate ? "danger" : ""}">${x.isPirate ? "Пираты" : x.isOwn ? "Свой" : "Чужой"}</span></header><p>P${x.position} · ${names[x.status] ?? x.status} · ${x.shipCount} кораблей</p>${x.canAttack ? `<button data-target-fleet="${x.id}" data-position="${x.position}">Подготовить атаку</button>` : ""}</article>`).join("") : `<div class="empty-living">Открытых контактов нет.</div>`;
    }

    function renderBattles(battles) {
        state.battles = battles; const active = battles.filter(x => x.status !== "Completed");
        $("#battleSummary").textContent = active.length ? `${active.length} активных боёв` : "Нет активных боёв";
        $("#battleGrid").innerHTML = battles.length ? battles.map(battle => { const fleet = state.fleets.find(x => x.id === battle.attackerFleetId || x.id === battle.defenderFleetId); const reports = Array.isArray(battle.report) ? battle.report : []; return `<article class="battle-card"><header><div><small>БОЙ ${battle.id.slice(0, 8)}</small><strong>Раунд ${battle.round}</strong></div><span class="status-pill ${battle.status !== "Completed" ? "danger" : ""}">${battle.status}</span></header><div class="battle-report">${reports.length ? reports.map(x => `<div>${esc(x)}</div>`).join("") : "Ожидание первого расчёта."}</div>${battle.status !== "Completed" && fleet ? `<div class="battle-actions"><select data-battle-priority><option value="Weakest">Слабейшая цель</option><option value="Shields">Сильный щит</option><option value="Firepower">Максимальный урон</option></select><button data-battle="${battle.id}" data-fleet="${fleet.id}" data-order="fight">Продолжить бой</button><button data-battle="${battle.id}" data-fleet="${fleet.id}" data-order="retreat">Отступить</button></div>` : ""}</article>`; }).join("") : `<div class="empty-living">Боевых рапортов пока нет.</div>`;
    }

    async function createFleet() {
        const groups = groupReserve(state.assembly?.reserve ?? []);
        const shipIds = groups.flatMap(group => {
            const quantity = Math.min(
                group.ships.length,
                Math.max(0, state.reserveDraft[group.blueprintId] ?? 0)
            );
            return group.ships.slice(0, quantity).map(ship => ship.id);
        });

        if (shipIds.length === 0) {
            context.message("Укажите количество кораблей для флота.", true);
            return;
        }

        try {
            await context.api("/api/game/living-galaxy/fleets", {
                method: "POST",
                body: JSON.stringify({
                    planetId: state.planet.id,
                    name: $("#newFleetName").value ||
                        "Экспедиционная группа",
                    shipIds
                })
            });
            state.reserveDraft = {};
            context.message("Флот сформирован и готов к полётному листу.");
            await context.reload();
        } catch (error) {
            context.message(error.message, true);
        }
    }
    function selectFleet(id) { state.selectedFleetId = id; state.draftFleetId = id; state.draft = selectedFleet()?.commands?.filter(x => x.status === "Planned").map(stripCommand) ?? []; renderFleetGrid(); renderPlan(); renderService(); }
    function selectedFleet() { return state.fleets.find(x => x.id === state.selectedFleetId); }
    function stripCommand(x) { return { type: x.type, speedMode: x.speedMode, targetGalaxy: x.targetGalaxy, targetSystem: x.targetSystem, targetPosition: x.targetPosition, targetFleetId: x.targetFleetId, targetObjectId: x.targetObjectId, durationMinutes: x.durationMinutes, manifestMaterials: x.manifestMaterials, manifestDeuterium: x.manifestDeuterium }; }
    function readCommand() { const target = $("#commandTarget").selectedOptions[0]?.dataset ?? {}; const type = $("#commandType").value; return { type, speedMode: $("#commandSpeed").value, targetGalaxy: num("#commandGalaxy"), targetSystem: num("#commandSystem"), targetPosition: num("#commandPosition"), targetFleetId: type === "Attack" ? target.fleetId ?? null : null, targetObjectId: type === "Mine" ? target.objectId ?? null : null, durationMinutes: num("#commandDuration") ?? 30, manifestMaterials: num("#commandMaterials") ?? 0, manifestDeuterium: num("#commandDeuterium") ?? 0 }; }
    async function addCommand() { const fleet = selectedFleet(); if (!fleet) return; const command = readCommand(); if (["Landed", "Orbiting"].includes(fleet.status)) { state.draft.push(command); renderPlan(); } else try { await context.api(`/api/game/living-galaxy/fleets/${fleet.id}/next-command`, { method: "PUT", body: JSON.stringify({ command }) }); context.message("Следующая команда изменена."); await context.reload(); } catch (error) { context.message(error.message, true); } }
    async function savePlan() { const fleet = selectedFleet(); if (!fleet) return; try { await context.api(`/api/game/living-galaxy/fleets/${fleet.id}/plan`, { method: "PUT", body: JSON.stringify({ commands: state.draft }) }); context.message("Полётный лист сохранён."); await context.reload(); } catch (error) { context.message(error.message, true); } }
    async function launch() { const fleet = selectedFleet(); if (!fleet) return; try { if (state.draft.length) await context.api(`/api/game/living-galaxy/fleets/${fleet.id}/plan`, { method: "PUT", body: JSON.stringify({ commands: state.draft }) }); await context.api(`/api/game/living-galaxy/fleets/${fleet.id}/launch`, { method: "POST" }); context.message("Полётный лист запущен. Текущая команда заблокирована."); await context.reload(); } catch (error) { context.message(error.message, true); } }
    async function fleetAction(event) { const card = event.target.closest("[data-fleet-id]"); if (!card) return; selectFleet(card.dataset.fleetId); if (event.target.dataset.action === "plan") showTab("plan"); if (event.target.dataset.action === "land") try { await context.api(`/api/game/living-galaxy/fleets/${card.dataset.fleetId}/land`, { method: "POST" }); context.message("Флот совершил посадку и защищён."); await context.reload(); } catch (error) { context.message(error.message, true); } }
    function commandAction(event) { if (event.target.dataset.removeCommand == null) return; state.draft.splice(Number(event.target.dataset.removeCommand), 1); renderPlan(); }
    function targetAction(event) { const button = event.target.closest("[data-target-object],[data-target-fleet]"); if (!button) return; $("#commandGalaxy").value = state.system.galaxy; $("#commandSystem").value = state.system.system; $("#commandPosition").value = button.dataset.position; $("#commandType").value = button.dataset.targetFleet ? "Attack" : "Mine"; if (button.dataset.targetFleet) { localStorage.setItem("livingTargetFleet", button.dataset.targetFleet); localStorage.removeItem("livingTargetObject"); } else { localStorage.setItem("livingTargetObject", button.dataset.targetObject); localStorage.removeItem("livingTargetFleet"); } context.openPage("fleet"); showTab("plan"); renderTargets(); }
    function renderTargets() { const select = $("#commandTarget"); const fleetTarget = localStorage.getItem("livingTargetFleet"); const objectTarget = localStorage.getItem("livingTargetObject"); const options = [`<option value="">По координатам</option>`]; if (objectTarget) options.push(`<option selected data-object-id="${objectTarget}">Выбранное поле / обломки</option>`); if (fleetTarget) options.push(`<option selected data-fleet-id="${fleetTarget}">Выбранный чужой флот</option>`); select.innerHTML = options.join(""); }
    async function scanFromInputs() { await loadSystem(num("#operationsGalaxy"), num("#operationsSystem")); }
    async function loadSystem(galaxy = state.planet?.galaxy, system = state.planet?.system) { if (!galaxy || !system) return; try { renderOperations(await context.api(`/api/game/living-galaxy/system?galaxy=${galaxy}&system=${system}`)); } catch (error) { context.message(error.message, true); } }
    async function loadBattles() { try { renderBattles(await context.api("/api/game/living-galaxy/battles")); } catch (error) { context.message(error.message, true); } }
    async function serviceAction(event) { const button = event.target.closest("[data-service]"); if (!button) return; try { await context.api(`/api/game/living-galaxy/ships/${button.dataset.ship}/service`, { method: "POST", body: JSON.stringify({ type: button.dataset.service }) }); context.message("Корабль принят в орбитальный сервис."); await context.reload(); } catch (error) { context.message(error.message, true); } }
    async function battleAction(event) { const button = event.target.closest("[data-order]"); if (!button) return; const priority = button.closest(".battle-card")?.querySelector("[data-battle-priority]")?.value ?? "Weakest"; try { await context.api(`/api/game/living-galaxy/battles/${button.dataset.battle}/orders`, { method: "POST", body: JSON.stringify({ fleetId: button.dataset.fleet, targetPriority: priority, retreat: button.dataset.order === "retreat" }) }); context.message("Боевой приказ принят до конца раунда."); await loadBattles(); } catch (error) { context.message(error.message, true); } }
    function num(selector) { const value = Number($(selector).value); return Number.isFinite(value) && value >= 0 ? value : null; }
    function fmt(value) { return new Intl.NumberFormat("ru-RU", { maximumFractionDigits: 1 }).format(value ?? 0); }
    return { init, render, loadSystem, loadBattles };
})();
