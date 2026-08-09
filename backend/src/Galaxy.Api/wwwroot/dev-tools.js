window.DevToolsUi = (() => {
    let context;
    let button;

    async function init(options) {
        context = options;
        button = document.querySelector("#devSupplyButton");

        try {
            await context.api("/api/dev/status");
            button.hidden = false;
            button.addEventListener("click", grantSupply);
        } catch {
            button.hidden = true;
        }
    }

    async function grantSupply() {
        button.disabled = true;

        try {
            const result = await context.api(
                `/api/dev/supply?planetId=${context.planetId()}`,
                {
                    method: "POST"
                }
            );

            context.message(
                `Тестовый запас выдан: ` +
                `${result.materials} материалов, ` +
                `${result.deuterium} дейтерия, ` +
                `${result.componentTypes} типов комплектующих.`
            );

            await context.reload();
        } catch (error) {
            context.message(error.message, true);
        } finally {
            button.disabled = false;
        }
    }

    return { init };
})();
