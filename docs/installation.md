# Installation and troubleshooting

## Requirements

- Tower Dominion installed locally.
- MelonLoader installed and working with the game (.NET 6 runtime).
- A matching Toolkit release ZIP.

## Install

1. Close Tower Dominion.
2. Download the versioned MelonLoader ZIP from the Toolkit release.
3. Extract the ZIP into the game's installation directory, merging its `Mods/` and `UserLibs/` folders. Do not copy the ZIP itself into `Mods/`.
4. Start the game. The main menu should display `TK: 0.1.0` for the initial release.
5. Press backtick (`\``) to open the console; run `version` and `help`.

**All four DLLs must be updated together.** The main mod is `Mods/TowerDominionToolkit.dll`; the dependencies are `UserLibs/TowerDominion.Toolkit.Model.dll`, `TowerDominion.Toolkit.Modules.dll`, and `TowerDominion.Toolkit.Runtime.dll`.

## Troubleshooting

- **No console or commands:** Confirm MelonLoader starts and all four DLLs came from the same release. Inspect `MelonLoader/Latest.log` for Toolkit errors.
- **No Toolkit menu label:** Confirm the mod loads in the log; the label is only shown on the main menu.
- **Unknown command:** Run `help`, `help <verb>`, or `help starting tiles`, `help banlists`, `help frontier`.
- **Missing game entities:** Wait until the game catalog has loaded before running catalog-dependent commands.
- **Conflicts:** Do not combine with the older `TowerDominionConsole.dll` until its patch coexistence has been validated. Compatibility with Tower Dominion Leak Fix is intended but not yet exhaustively tested.
- **Report a reproducible issue:** Include Toolkit version, game version, MelonLoader version, relevant log excerpt, command entered, and observed versus expected result. Remove personal information from logs before sharing.

Uninstall by closing the game and removing the Toolkit main DLL and its three Toolkit-specific dependency DLLs. UserData configurations can be retained for later reinstallation.
