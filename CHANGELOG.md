# Changelog

## [Unreleased]

- Restricted the `foes` catalog and ban completion to characters actually present in configured regular-wave swarms, excluding unused enemy assets.
- Resolved short swarm IDs through game character resources, keeping canonical GUIDs for ban enforcement and separating final-wave candidates.
- Verified in-game: 22 foe candidates, Tab completion and foe bans working.

## [0.3.0] — Expanded challenges and Frontier reliability

- Added **Chain of Command**: buildings must connect orthogonally to the HQ network; selling and cancelling are blocked and their buttons visibly disabled. Yellow crosses mark available network-adjacent tiles, regardless of other native building-placement limitations.
- Added **8-Ball** (single reinforcement bundle), **Frozen Logistics** (recurring production caps with corrected displayed income), and **Left Turn Only** (restrict upgrades to the left path).
- Added Frontier Start protection while saved modifiers restore and the game's native dotation score finishes updating.
- Kept the challenge indicator visible on the Commander screen when Start replaces Next without Frontier modifiers.
- Fixed Chain of Command overlay visibility between consecutive runs and avoided a native special-reward initialization hook that crashes Frontier setup.
- Expanded game-independent tests to 400 passing cases; challenge placement, button behavior, and Frontier UI were additionally exercised in-game.

## [0.2.1] — HUD lifecycle and console wrapping

- Guarded challenge HUD positioning against destroyed IL2CPP objects during post-game transitions, hiding the indicator when the gameplay HUD is unavailable.
- Wrapped long console output within the available panel width, including `foes` listings, and adjusted row layout to prevent overlap.

## [0.2.0] — Command engine and challenges

- Replaced legacy command dispatch with a canonical, verb-first command grammar shared by execution, contextual help, and Tab completion. Commands now return structured results; some older command forms were removed. Run `help` to discover current syntax.
- Added persistent challenge selections, run-locked active challenges, challenge presets, and the first challenge, **Cheapest Tower Only** (suggested by `@DiVerrencia`).
- Added read-only challenge indicators and shared detail popups to Commander selection, Frontier selection, and the in-run HUD.
- Added optional author attribution to challenge descriptions and `show challenges <id>`.
- Restricted the F8 native HUD inspection probe to Debug builds; release packages exclude it.
- Expanded game-independent automated coverage to 379 passing tests. Current challenge, commands, and challenge UI were verified in-game by the project maintainer.

**Compatibility:** v0.2.0 uses the canonical command forms; consult `docs/commands.md` and in-game `help` when migrating from v0.1.x. Update all four Toolkit DLLs together.

## [0.1.1] — Preview and console improvements

- Improved console readability with opaque background and panels.
- Fixed IL2CPP-compatible rendering of console panel backgrounds.
- Corrected starting-tile preview orientation using HQ-outward clockwise/anticlockwise conventions, without changing game placement.
- Expanded console help to cover command variants and focused topics.
- Clarified relative starting-tile configuration syntax.

## [0.1.0] — Initial release

- In-game Toolkit console and entity catalog.
- Seed override and verification.
- Session-only bans with persistent named banlists.
- Persistent Frontier modifier selection.
- Relative starting-tile configurations, named presets and console previews.
- Version reporting and main-menu Toolkit version label.

Validated with MelonLoader 0.7.3 and Tower Dominion Leak Fix 1.5.0: all ten mods loaded, with Frontier selection, starting-tile configuration, and foe bans exercised in-game.
