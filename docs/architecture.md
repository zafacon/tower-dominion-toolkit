# Architecture

## Boundaries

- **Model:** Tower Dominion entities, state contracts, operations, events, property authority, and capability discovery. No MelonLoader, Harmony, IL2CPP, or Unity references.
- **Modules:** Independent gameplay capabilities using only Model contracts. No direct game access and no module-to-module references.
- **Runtime:** Adapters for game state observation and application. Future MelonLoader/Harmony implementation belongs here.
- **Interfaces.Console:** Optional interaction adapter. Other interfaces may be added independently.

## Capability-driven model evolution

When introducing a module:
1. Identify required model properties, operations, and events.
2. Reuse existing typed contracts where possible.
3. Document missing capabilities before extending the model.
4. Implement model and runtime support independently of the module.
5. Verify module behavior against model fakes before in-game testing.

Capability availability and module activation are different concepts.

## Authority and synchronization

Authority is defined per property and may change with game lifecycle. Requested, accepted, applied, and verified are distinct states; never imply game application from a stored request.

## Lifecycle

Modules are registered on game launch, enabled by default, and disabled by explicit configuration. Changes take effect on the next launch. Disabled modules retain persisted settings but do not execute.

## Packaging

Repository projects are architectural boundaries, not a final distribution decision. Do not assume multiple user-installed DLLs. The original console repository is a reference, not a build dependency.

## Runtime compatibility

The installed Tower Dominion MelonLoader `net6/MelonLoader.runtimeconfig.json` specifies `net6.0` and `Microsoft.NETCore.App` version `6.0.0`. All production projects therefore target `net6.0`. Tests may target a newer framework because they do not load inside the game.

## Initial registration and startup contracts

The capability registry tracks availability independently from module registration. A module declares required capabilities and is initialized only when all are available at startup. Disabled modules are not initialized. A failed initialization does not prevent other modules from starting. The initial configuration is immutable in memory and defaults to enabled; persistent file loading and re-evaluation when transient game capabilities become available are future work.

## Lifecycle-aware availability and configuration

The capability registry emits change notifications when a descriptor changes. The module coordinator initializes enabled modules once their declared requirements are available; it does not initialize disabled modules or retry failed initializations automatically. An initialized module remains active if a capability later becomes unavailable; individual operations must check live availability rather than assume that startup availability persists. Configuration is loaded once at launch and is not hot-reloaded.

`ModuleConfigurationStore` saves JSON with a `DisabledModules` array. Missing files mean all modules are enabled. Invalid configuration is an error rather than silently replacing user preferences. The caller chooses the configuration path; no game-specific path is hardcoded.

Architecture tests validate project references and scan Model/Modules for common runtime namespace imports. They are a guardrail, not a substitute for code review.

## Game-specific MelonLoader adapter (initial)

The separate `TowerDominion.Toolkit.Game` project is the only project allowed to reference Harmony, MelonLoader, and IL2CPP game assemblies. It targets net6.0 and is built with `-p:TowerDominionGamePath='/path/to/Tower Dominion'`. It contains a MelonMod entry point and a postfix for `RandomService.GenerateSeeds`, following the existing console mod's numeric-versus-encoded seed handling. The game adapter is not installed automatically; no deployment or live game verification has been performed.

The adapter loads `UserData/TowerDominionToolkit/modules.json` at launch and defaults to enabled modules if the file is absent. The seed module currently has no command/UI/API to submit an override, so the hook observes generated seeds but cannot yet be used interactively. Game project DLL dependencies must be packaged and loader resolution tested before deployment. Do not deploy alongside the existing console without first deciding how the two seed Harmony patches will coexist.

**Verification caveat:** the initial seed service treats the returned encoded value from the apply callback as verified. That is only callback-level confirmation, not independent observation of later game state; this distinction must be corrected before claiming end-to-end verification.

## Seed commands and verification (pre-deployment)

`SeedCommands` is a transport-neutral command handler with `seed set <value>`, `seed clear`, and `seed status`. It is tested but is not yet connected to a game console, keyboard input, or external API. The game adapter exposes the seed module internally only; do not claim the commands are available in game.

An override is now marked `Applied` after the game callback returns its encoded seed, not `Verified`. Verification requires an independent later game-state observation and has not yet been wired into the game adapter. The seed generation hook remains a potential conflict with the original console mod's hook; do not deploy both without testing the interaction. No game files have been modified.

## Isolated seed command transport

The game adapter polls `UserData/TowerDominionToolkit/seed-command.txt` on its main-thread `OnUpdate` callback. Write one command (`seed set <value>`, `seed clear`, or `seed status`) to the file; after consumption, the adapter removes the request and writes `seed-response.txt`. This is an intentionally minimal, local, single-command-at-a-time test interface; it is not a durable command queue and has not been exercised in a live game. The handler is available only while the seed module is enabled. It does not install or change the original console.

Build the game adapter with `dotnet build src/TowerDominion.Toolkit.Game/TowerDominion.Toolkit.Game.csproj -p:TowerDominionGamePath='/path/to/Tower Dominion'`. The output is `TowerDominionToolkit.dll` with referenced toolkit assemblies; deployment and MelonLoader dependency resolution are not yet validated. Do not copy it into the game's Mods directory until an isolated deployment and coexistence strategy is agreed.

## Staged deployment packaging

Run `scripts/package-game.sh '/path/to/Tower Dominion' '/path/to/output'` to build a Release mod and create `tower-dominion-toolkit-stage.zip` containing `Mods/TowerDominionToolkit.dll`, three `UserLibs/TowerDominion.Toolkit.*.dll` dependencies, installation notes, and SHA-256 checksums. Packaging does not copy files into the game. The original console mod remains installed and also patches `RandomService.GenerateSeeds`; do not install the new mod alongside it without testing Harmony patch order and interactions. Actual MelonLoader assembly resolution and game behavior are not yet verified.

## Independent runtime seed verification

After an override is applied in the GenerateSeeds Harmony callback, the game adapter retains the RandomService instance until the next OnUpdate, then separately reads GetEncodedSeed. The model verifies a matching applied seed or fails the operation on a mismatch. A failed runtime read is logged and leaves the operation Applied, not Verified. This code has unit coverage but is not yet deployed to the running game; restart is required for a live test.

## Coordinator lifetime

The MelonLoader adapter retains the module coordinator for the whole game session, subscribes to active module transitions (including deferred activation), and unsubscribes/disposes during OnDeinitializeMelon. Modules activate once when their capabilities become available; startup settings remain fixed for the session.

## Console parity migration — phase 1

The command registry (`Modules/CommandRegistry.cs`) is transport-neutral and owns command dispatch and help. The file inbox now routes seed commands through that registry without changing the on-disk seed-command/seed-response protocol. The game-independent `Model/EntityCatalog.cs` owns category snapshots, stable IDs, display names, aliases, and unambiguous exact resolution. The catalog is not yet populated from IL2CPP managed data; no `list` commands are advertised until a game adapter provides real entries. Next work: extract and validate the original console's catalog discovery into a Toolkit adapter, then expose list commands through the registry. The original console remains a reference, not a runtime dependency.

### Live catalog adapter (awaiting deployment)

`GameCatalogAdapter` polls managed-data readiness on the game thread and builds snapshots for foes, final-wave characters, blueprints, doctrines, and Frontier modifiers. Frontier classification follows the existing console's `SkillType.Regular && ExpertCost > 0` filter, not the Cursed World doctrine list. The adapter separates final-wave swarm IDs from ordinary foes and preserves game identifiers and aliases for later ban and selection modules. `list <category>` is routed through the shared command registry and explicitly reports not-ready rather than pretending an unloaded catalog is empty. Counts and Frontier classification require live validation before declaring catalog parity. No DLLs were deployed as part of this change.

## Frontier modifier phase 2 — persistence slice

`FrontierSelections` stores stable IDs in `UserData/TowerDominionToolkit/frontier-selection.json` with atomic file replacement. `FrontierCommands` validates every requested modifier against the loaded catalog before mutating state; `all` is scoped to the Frontier category only. `list selected-frontiers` reports desired persistent selections. **This is desired state, not confirmed game selection.** The legacy console's deferred `ChallengeUI.SetData` checkbox replay was observed to produce mismatched modifier counts and a disabled Start Run button. Do not port that patch unchanged. A separate game adapter must establish the authoritative UI selection/count path and verify Start Run eligibility. The game was running during this implementation; no deployment was attempted.

### Frontier UI authority investigation (2026-11-01)

Inspection of the game's generated `Assembly-CSharp.dll` metadata establishes these native interfaces:

- `Il2CppNvizzio.Game.UI.ChallengeUI.SetData(SkillData, UnityAction<bool, SkillData>)` binds each toggle to a callback; `ChallengeUI.OnValueChanged(bool)` handles its local event.
- `Il2CppNvizzio.Game.UI.Views.ExpertModeMenu.Initialize()` and `Clear()` govern setup lifecycle.
- `ExpertModeMenu.OnToggleChallengeUI(bool, SkillData)` receives selections; `AddOwnedExpertSkillData(SkillData)` and `RemoveOwnedExpertSkillData(SkillData)` manage its owned skill data; `UpdateScore(bool)` and `OnLevelChanged()` maintain score/progression display.

The original console patch in `Patches/BanEnforcementPatches.cs` sets `styledToggle.isOn = true` and additionally invokes `styledToggle.onValueChanged.Invoke(true)` after one frame. This can cause duplicate callbacks; the user's observed negative counts and unavailable Start Run demonstrate that UI state alone is insufficient. This is a hypothesis about the cause, **not yet proven** by native implementation tracing. Next inspect the ExpertModeMenu state fields and actual callback registration, then implement a single-path, idempotent restore after menu initialization, with diagnostics comparing desired IDs, selected game IDs, score, and start eligibility. Avoid directly incrementing scores or invoking callbacks twice. No DLL replacement while the game is running.

### Read-only Frontier tracing

`frontier-diagnostics on|off|status` controls opt-in Harmony probes for `ExpertModeMenu.Initialize`, `Clear`, `OnToggleChallengeUI`, and `ChallengeUI.SetData`. Logs include callback transitions, score, level, and Start button interactability. Diagnostics are disabled by default, perform no UI mutation, and must be validated on a real menu before enabling any automatic restore behavior. Probe implementation is isolated in `Game/FrontierDiagnosticsPatch.cs`.

### Live manual toggle baseline (2026-11-01, build 89de3b1)

With `frontier-diagnostics on`, `ExpertModeMenu.Initialize` created 14 Frontier toggles, all initially off, score 0, Start disabled. Manual selection of Feral World (`35492be6-cf0b-4b63-b880-e18082699a91`) triggered **two** `OnToggleChallengeUI(true, SkillData)` invocations; first changed score 0→1 and Start disabled→enabled, second left score 1 and Start enabled. Manual deselection likewise triggered two callbacks, with score 1→0 then unchanged, and Start enabled→disabled. Repeated toggles remained 0/1. Progress level floats changed during each callback due to animation and are not reliable as instantaneous selection counts. Thus duplicate callbacks are present in unmodified game interaction, and native score changes appear idempotent; do not infer that the prior bug was solely caused by duplicate callback invocation. Restore should use `styledToggle.isOn = true` only after native callback wiring, without manually invoking `onValueChanged`, and must be verified live before enabling by default. Do not force `startButton.interactable` or change `currentScore` directly.

### Frontier restoration candidate

`FrontierMenuInitializeProbe` schedules one-frame-delayed restoration after native `ExpertModeMenu.Initialize`. It finds `ChallengeUI` children of the menu root, matches saved stable IDs against `ChallengeUI.Data.UniqueID`, and sets `styledToggle.isOn` **only when off**, without explicitly invoking the callback or modifying score/start state. It logs restored count, resulting score and Start interactability. This is compiled but **not live validated**; test with a saved selection after a game restart, including deselect and reenter behavior. No deployment while game is running.

### Frontier restoration live verification (2026-11-01)

Deployed commit `0c1e006` with game closed, then restarted the game. `select frontier "Feral world"` persisted one ID (`35492be6-cf0b-4b63-b880-e18082699a91`) while game was open. On entering Frontier selection screen, the game log reported `Restored 1 of 1 saved modifiers; game score=1, startInteractable=True`. No restoration errors observed. This validates single-modifier automatic selection and Start eligibility for this session, **not yet** restart persistence, multiple modifiers, deselection, or repeat-entry behavior. Additional regression testing needed before Phase 2 is complete.

### Frontier restart persistence verification (2026-11-01)

After restarting Tower Dominion, a fresh `Latest.log` shows Toolkit startup at 07:43:14 and automatic restoration at 07:43:29: `Restored 1 of 1 saved modifiers; game score=1, startInteractable=True`. The persisted selection JSON still contains the Feral World ID. This confirms cross-process persistence for one selected modifier. Multiple selections, unselect commands while the menu is open, and repeat-entry behavior remain unverified.

### Multiple Frontier restoration live verification (2026-11-01)

On next game startup (07:44:43), the pending `select frontier "Jagged terrain","Foggy world"` command was consumed and persisted two additional IDs alongside Feral World. The next Frontier menu logged `Restored 3 of 3 saved modifiers; game score=5, startInteractable=True` at 07:44:55. The combined score is the game's actual modifier-cost total, not the number of toggles. No errors were observed. Still need unselect/re-entry and manual toggle behavior validation.

### Frontier deselection and menu re-entry verification (2026-11-01)

While game was open, `unselect frontier "Jagged terrain"` removed one saved ID, leaving Feral World and Foggy World. Reentering Frontier selection screen yielded `Restored 2 of 2 saved modifiers; game score=3, startInteractable=True` at 07:47:51, versus the earlier three-modifier score of 5. No negative score observed. Note that changing persistence does not immediately alter an already-open menu; re-entry is currently required. Manual toggle interaction and clearing all selections remain to test.

### Phase 2 completion and Phase 3 console candidate (2026-11-01)

User confirmed the zero-selection/Start-disabled UI test after `unselect frontier all`; the saved file was independently verified as `[]`. Phase 2 live regression checklist is complete. Phase 3 introduces an IMGUI overlay in the game adapter using the existing transport-neutral `CommandRegistry` shared with the file inbox. Backquote opens/closes, Escape closes, Enter executes, Up/Down history, Tab command/list/Frontier-name completion, wheel/Page Up/Down scroll, and Ctrl+V paste. Unity EventSystem is temporarily suspended while visible and restored on close/deinit. This is a **compiled candidate, not live tested**. Input capture and rendering must be validated in-game before marking Phase 3 complete. Do not deploy while the game is running.

### Phase 3 input editing follow-up

Live console smoke test confirmed overlay, `help`, Tab command completion, Up/Down history, and close/reopen controls. User found that Left/Right did not move a cursor and held Backspace deleted only once. Added cursor-position editing (Left/Right, Home/End, Delete, Backspace, insertion/paste at caret), held-key repeat for cursor movement and deletion, and Ctrl+Home/End for output navigation. The edit is compiled and unit regression suite passes, but requires another live UI check. Do not replace the loaded DLL while the game is running.

### Phase 3 live verification complete (2026-11-01)

After deploying commit `ce9bdcf` with the game closed, the user confirmed the previously failing cursor movement and held-Backspace behavior are fixed in-game. Earlier live testing had already confirmed console visibility, `help`, Tab completion, command history, and close/reopen. Phase 3 interactive console is accepted; Phase 4 is persistent bans and run-generation enforcement.

### Phase 4 initial persistent ban state (2026-11-01)

Introduced `BanSelections` with category-qualified stable IDs for foes, waves, blueprints and doctrines, atomic JSON persistence in `bans.json`, idempotent changes, and all-or-nothing name resolution through the shared catalog. Registered `ban <category> <names|all>`, `unban <category> <names|all>`, and `banned` commands through the existing shared registry. **This is configuration only: no ban enforcement patches are installed yet.** In particular, a banned foe such as Medacris may still spawn until enforcement is implemented and live verified. No game deployment in this step.

### Phase 4 foe and final-wave enforcement candidate

`SwarmBanPatch` filters banned swarm candidates immediately before `EnemySwarmService.SelectPredeterminedSwarm`, then restores the original candidate list after selection to avoid modifying shared game data. Uses `CharacterData.ResourceLoadEnemy` to normalize the runtime `CharacterId` to the catalog `UniqueID` (the earlier console had a Medacris ban regression from ID mismatches). Final-wave pools use the separate `waves` category. If excluding entries leaves fewer candidates than required, the patch logs a warning and retains the native pool rather than breaking generation. This means bans are **best effort** when a pool cannot satisfy the requested count. Build and existing tests pass; **live enforcement has not been verified**, and no DLLs have been deployed while the game is open. Blueprint/doctrine enforcement remains outstanding.

### Phase 4 blueprint/doctrine candidate enforcement

`RewardBanPatches` postfixes the native random blueprint and doctrine reward services. When a banned ID is returned, each patch retries up to 256 times with thread-local recursion protection; doctrine retries preserve the requested `SkillType`. On exhaustion the native banned result remains and a warning is logged, so enforcement is not guaranteed when all candidates are banned. The console now completes ban categories and names and supports `list banned`; added two persistence/atomicity tests (40 total). This is compiled but not yet deployed or tested in a live run. Need to verify all four categories in-game, particularly Medacris and the previously duplicated Hell from Above entry.

### Phase 4 live user verification (2026-11-01)

User confirmed `ban foes Medacris` worked in-game. User subsequently confirmed that both banning and unbanning the final wave **Hell from Above** worked in-game. Saved state inspection showed the Medacris stable ID under `foes` and `FW01` under `waves` during testing. No corresponding exclusion event appeared in the inspected log, so the game behavior confirmation is user-reported. Blueprint and doctrine ban enforcement still await live tests.

### Console discoverability improvement (2026-11-01)

Catalog `list` output and `banned` output now present names rather than stable IDs (IDs remain in persistence). Tab completion for Frontier and ban entity names now matches substrings, not only prefixes; a single match inserts the full quoted name, and multiple matches are listed. Ban/unban commands also accept a unique case-insensitive name substring, rejecting ambiguous matches without changing state. This change is built and unit-tested, but **not deployed while the game is running**. Names that cannot be localized by the game may still fall back to IDs; investigate such entries separately rather than fabricating labels.

### Phase 4 named Banlists port (2026-11-01)

The original console's named Banlist feature is now implemented in the Toolkit with `save banned as <name>`, `load banlist <name>`, `unload banlist <name>`, `delete banlist <name>`, `list banlists`, and `list banlist <name>`. Presets are stored as category-qualified stable IDs in `UserData/TowerDominionToolkit/Banlists/`, separate from persistent active `bans.json`. Loading replaces all active categories atomically, unloading removes the saved IDs from active bans, and deleting a preset leaves active bans untouched. The implementation includes unit tests for round-trip behavior, persistence, and filename validation. It has not yet been deployed or tested in-game.
