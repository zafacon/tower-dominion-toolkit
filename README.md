# Tower Dominion Toolkit

A Tower Dominion-specific modular toolkit for observing and interacting with the game through a shared model.

## Status

Active MelonLoader toolkit with an in-game console, canonical command engine, entity catalog, seed overrides, banlists, persistent Frontier selections, and selectable gameplay challenges. See [completed features](docs/completed-features.md) for verified functionality and areas that should not be revisited without a bug report.

## Installation and commands

Install the versioned MelonLoader ZIP with the game closed, merging its `Mods/` and `UserLibs/` folders. All four Toolkit DLLs must come from the same release. See [installation and troubleshooting](docs/installation.md) and the [console command reference](docs/commands.md).

Current release: **0.3.1**; see [changelog](CHANGELOG.md). Compatibility with [Tower Dominion Leak Fix](https://github.com/cparedes/tower-dominion-leak-fix) is intended; exhaustive coexistence testing is pending.

## Principles

- Capability modules depend on shared model contracts, never on other modules or game internals.
- The model grows incrementally with capability requirements.
- Strongly typed contracts are paired with discoverable capability availability.
- Property authority is defined per property.
- Runtime adapters translate between the model and Tower Dominion.
- Interfaces (including the console) consume the model; the console is optional.
- Modules activate automatically unless disabled in configuration; changes take effect on game restart.

See [architecture](docs/architecture.md) and [contributing](docs/contributing.md).

## Development

Production projects target .NET 6, matching the installed Tower Dominion MelonLoader runtime. Tests target .NET 10. A .NET 10 SDK can build both.

```bash
dotnet build TowerDominion.Toolkit.slnx
dotnet format TowerDominion.Toolkit.slnx --verify-no-changes
# Package locally (requires installed game + MelonLoader):
./scripts/package-game.sh "/path/to/Tower Dominion" ./dist
```

No game binaries or proprietary assets are distributed here.

The existing `tower-dominion-console` repository remains independent and untouched.

## How this was made

Tower Dominion Toolkit was developed collaboratively by **Pablo Castañeda** and **OpenAI's ChatGPT**. Pablo directed the feature design, defined the expected gameplay behavior, and tested changes in the running game. ChatGPT assisted with inspecting game interfaces, implementing the mod, debugging, documentation, and automated tests.

- **Game integration:** The Toolkit uses MelonLoader and the game's generated IL2CPP C# bindings to observe and interact with Tower Dominion. It keeps game-specific integration separate from shared model contracts and capability modules.
- **Iterative gameplay testing:** Features were implemented in small increments and checked in live runs, including enemy and final-wave bans, Frontier modifier selection and scoring, seed overrides, starting-tile layouts, and the in-game console.
- **Automated verification:** Unit and integration-style tests cover the game-independent contracts and command behavior. Live game testing remains necessary for Unity UI and runtime patches.

This is an independent community mod, not an official Tower Dominion project. **Review the code before using it**, as with any third-party mod; issues and corrections are welcome.

## License

MIT © 2026 Game Familiar. See [LICENSE](LICENSE).

## Source and compatibility

This project is an independent community modification, not affiliated with the game's developers. It does not include Tower Dominion, MelonLoader, or proprietary game assemblies. Building the game adapter requires a local game installation with generated IL2CPP assemblies.
