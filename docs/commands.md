# Console commands

Open the console with the backtick key (`\``). Run `help` for all available forms, `help <verb>` for a specific verb, or `help starting tiles`, `help banlists`, and `help frontier` for focused help.

## Inspection

- `version`: compiled version, revision and feature registration.
- `list foes`, `list waves`, `list blueprints`, `list doctrines`, `list frontiers`: game catalog.
- `list banned` or `banned`: active bans.
- `list selected-frontiers`: persistent Frontier selections.

## Run settings

- `seed set <value>`, `seed clear`, `seed status`: next-run seed override.
- `ban <foes|waves|blueprints|doctrines> <names|all>` and `unban ...`: session-only bans.
- `select frontier <name[,name...]|all>` and `unselect frontier ...`: persistent Frontier modifier selections. Comma-separated names may be quoted.
- `frontier-diagnostics on|off|status`: diagnostic logging.

## Named banlists

- `save banned as <name>`: snapshot active bans.
- `list banlists` and `list banlist <name>`: inspect saved lists.
- `load banlist <name>`: replace active bans.
- `unload banlist <name>`: remove that list's entries from active bans.
- `delete banlist <name>`: delete saved list.

## Starting tiles

- `set starting tiles S,X,L.C`: set **relative** entrance shapes for D3 (three shapes). One to four shapes select D1–D4 respectively.
- `set relative tiles D3 S,X,L.C`: same configuration with an explicit difficulty.
- Supported shapes: `S`, `X`, `L.C`, `L.A`, `T`, `T.C`, `T.A`.
- `list starting tiles`, `list relative tiles`: show configurations.
- `clear starting tiles 3` or `clear relative tiles D3`: remove a difficulty configuration.
- `save starting tiles as <name>`, `load starting tiles <name>`, `unload starting tiles`: manage tile presets.
- `list starting-tile-presets`, `list starting-tile-preset <name>`, `delete starting-tile-preset <name>`: inspect and delete presets.
- `activate tileset <name>` and `deactivate tileset`: manage persistent activation of saved tilesets.

Legacy absolute compass-coordinate tile presets remain accepted for backward compatibility but are not the primary syntax. Relative shapes are mapped to actual HQ entrances at run generation.
