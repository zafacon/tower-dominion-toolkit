# Changelog

## [Unreleased]

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
