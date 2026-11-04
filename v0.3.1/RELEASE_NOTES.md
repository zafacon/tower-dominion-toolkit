# Tower Dominion Toolkit v0.3.1

This update adds two community challenges and improves challenge selection, enforcement, and readability.

- **Low Tech** (`@cookiebat`): Tech is capped at zero, including initial gains.
- **Forced Deployment** (`@darksoulsgreat6205`): place only the leftmost available blueprint; no selling or cancelling.
- Challenge-imposed blueprint bans now exclude Mission control for Forced Deployment and Mission control plus Scout Hideout for Cheapest Tower Only.
- Challenge details across Commander, Frontier and the in-run HUD use a wider scrollable popup with combined restrictions.
- **Cheapest Tower Only and Forced Deployment are incompatible.** Selection and preset loading reject this combination with a descriptive error; `show challenges <id>` identifies known conflicts. `select challenges all` is rejected atomically when both are registered.

Install with the game closed by extracting the ZIP into the game directory. Replace **all four** Toolkit DLLs together (`Mods/` and `UserLibs/`). Open the console with backtick and run `status toolkit` or `help`.

Automated validation, packaging, and in-game incompatibility enforcement were confirmed. Full coexistence regression with Tower Dominion Leak Fix remains pending.
