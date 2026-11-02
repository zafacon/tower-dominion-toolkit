# Completed features — do not revisit without a reported bug

Last reviewed: 2026-11-01. This is the working scope boundary for Tower Dominion Toolkit. Do not reopen, redesign, or proactively troubleshoot the items below unless a reproducible bug, regression, or explicit new requirement is reported. Preserve current game behavior.

## Confirmed in live gameplay

- **In-game toolkit console:** Overlay opens and accepts commands; caret navigation, held-key deletion, editing, and partial-name / shared-prefix tab completion have been fixed and live-verified. Human-readable entity listings and command completion are implemented.
- **Entity catalog:** Game catalog lists foes, final waves, blueprints, doctrines, and Frontier modifiers. Final-wave enemies are categorized as waves, distinct from regular foes.
- **Banning foes and final waves:** `ban` / `unban` work in gameplay. Banning a final wave uses native behavior for its associated special foes; no additional special-foe suppression should be added. Do not treat an intentionally empty permitted foe pool as a configuration error. Native swarm selection fallback remains in place to avoid the previously observed crash.
- **Blueprint and doctrine bans:** Enforcement and console completion are implemented, with automated coverage. Revisit only if a specific failure is reported.
- **Active-ban lifetime:** Active bans apply only to the current game session and reset at restart. They are **not** restored from `bans.json`. User confirmed this works after the 2026-11-01 deployment.
- **Named banlists:** `save banned as <name>`, `load banlist <name>`, `unload banlist <name>`, `delete banlist <name>`, `list banlists`, and `list banlist <name>` are the persistent mechanism for ban configurations. Named lists survive restart; active bans do not. User confirmed the resulting behavior works.
- **Frontier modifier selection:** `select frontier <names|all>` and `unselect frontier <names|all>` select actual Frontier modifiers, not Cursed World doctrines. Selections persist across restarts and are restored through native UI toggles. Native Frontier score/total updates correctly, deselection updates it, and Start Run eligibility works. This was verified in live tests and explicitly reconfirmed by the user on 2026-11-01. Do not revisit the previously solved negative-total / disabled-Start-Run issue.
- **Seed override:** `seed set`, `seed clear`, and `seed status` have an implemented game adapter, command transport, and independent post-application verification path. Previous live verification is recorded in repository history; do not reopen absent a new issue.

## Completed cleanup

- Removed `validate bans` and its native threat-availability observer as confusing and unnecessary (`b389221`). Do not restore it unless explicitly requested.
- Switched active bans to session-only without changing persistent named banlists or Frontier persistence (`d1e53a9`).
- Toolkit is deployed as `Mods/TowerDominionToolkit.dll` with Model/Modules/Runtime dependencies under `UserLibs/`. All 42 automated tests passed and the Release build had zero warnings/errors at the last deployment.

## Explicit boundaries / not a new work queue

- The existence of 14 native threat definitions, including post-wave-35 availability thresholds, is an observation, **not** a known defect or a reason to revisit bans.
- The earlier `validate bans` pool-size warnings were diagnostic interpretations, **not** gameplay bug reports.
- Do not change working ban filtering, native fallback, Frontier score synchronization, or persistence simply because internal code looks unusual.
- This document does not certify every edge case. New reproducible bugs and explicitly requested features can reopen a completed area.

## Verification trail

See commits `04e5cd4` (console live fixes), `fdce322` (live foe/final-wave bans), `9096da5`, `54f37c9`, `93fb1d9`, `c1c386a` (Frontier live checks), `b389221` (remove validator), and `d1e53a9` (session-only bans). The repository has no configured Git remote; these commits are local.

## Starting tiles (verified shapes and orientations)

S, X, L.C, L.A, T, T.C and T.A have been visually verified on all four HQ-relative faces. The four-face preset describes potential entrances, not a requirement to spawn four. Runtime overrides occur only during the game's native starting-zone creation coroutine and only at matching HQ-relative coordinates; inactive faces are untouched. D1/D2/D3 are expected to create 1/2/3 entrances respectively, subject to in-game validation of native entrance selection.

## Starting-tile relative configurations and console mosaics (2026-11-01)

- `set starting tiles <shape[,shape...]>` infers difficulty from the number of shapes: one for D1, two for D2, three for D3, four for D4. Supported shapes are `S`, `X`, `L.C`, `L.A`, `T`, `T.C`, and `T.A`. `list starting tiles` and `clear starting tiles <1|2|3|4>` retain the existing command interface. Relative selections are persisted in `UserData/TowerDominionToolkit/StartingTiles/relative-selection.json` and can be saved in named starting-tile presets; legacy absolute presets remain supported.
- Canonical visual entrance layouts: D1 north, D2 north/south (opposite), D3 north/east/south, D4 north/east/south/west. The relative shape mapper uses active native HQ entrances during actual runs; the canonical orientations are for consistent previews. Adjacent D2 native entrance arrangements have not been validated and are not supported by the current relative mapper.
- The console shows read-only 3×3 mosaics for active D1–D4 configurations and named starting-tile presets. The center HQ is a solid blue-gray tile with a centered `HQ` label and **no road marks**. Surrounding tiles render the actual `CardinalPaths` geometry, with thinner (12/192 texture pixels) light roads inset three pixels from tile edges to avoid visual overlap. The previews have spacing between them, dark opaque backgrounds, and subtle tile boundaries. No gameplay logic is driven by the previews.
- Rendering compatibility: this game's IL2CPP bindings throw `System.NotSupportedException: Method unstripping failed` for `GUI.DrawTexture`, even when the texture is valid. The working solution is a cached, CPU-generated 192×192 `Texture2D` assigned temporarily as `GUI.skin.box.normal.background`, rendered via `GUI.Box`, and then restored. If unsupported, a GUI.Box fallback remains. Do not replace this with `GUI.DrawTexture` without verifying the actual runtime binding.
- Latest visual revision: `257c414` (HQ roads removed and peripheral roads narrowed/inset). The opaque texture renderer was visually confirmed in-game in the preceding revision. The latest visual revision has passed 65 automated tests and a Release build with zero warnings/errors and was deployed with the game closed; its final appearance has not yet been reconfirmed in-game.
