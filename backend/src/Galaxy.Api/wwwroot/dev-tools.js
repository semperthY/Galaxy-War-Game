window.DevToolsUi = (() => {
    let context;
    let buttons = [];

    async function init(options) {
        context = options;
        buttons = [
            ...document.querySelectorAll("[data-dev-supply]")
        ];

        if (buttons.length === 0) {
            return;
        }

        try {
            await context.api("/api/dev/status");

            for (const button of buttons) {
                button.hidden = false;
                button.addEventListener("click", grantSupply);
            }
        } catch {
            for (const button of buttons) {
                button.hidden = true;
            }
        }
    }

    async function grantSupply() {
        for (const button of buttons) {
            button.disabled = true;
        }

        try {
            const result = await context.api(
                `/api/dev/supply?planetId=${context.planetId()}`,
                {
                    method: "POST"
                }
            );

            context.message(
                `Добавлено: +${result.materialsGranted} материалов, ` +
                `+${result.deuteriumGranted} дейтерия и ` +
                `+${result.componentQuantityGranted} каждого из ` +
                `${result.componentTypes} типов комплектующих.`
            );

            await context.reload();
        } catch (error) {
            context.message(error.message, true);
        } finally {
            for (const button of buttons) {
                button.disabled = false;
            }
        }
    }

    return { init };
})();
