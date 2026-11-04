# Console commands — v0.3.0

Open the console with backtick (`\``). Use `help`, `help <verb>`, `help <domain>`, or `help <verb> <domain>` for the live registered syntax. Tab completion uses the same command registry.

The canonical form is **`<verb> [qualifier] <domain> [arguments]`**. This replaces several v0.1.x domain-first commands. Names with spaces may be quoted; selection members are comma-separated; `all` selects the complete catalog.

## Inspect and modify catalogs

- `list foes`, `list waves`, `list blueprints`, `list doctrines`
- `list banned foes` (likewise waves, blueprints, doctrines)
- `ban foes <names|all>` / `unban foes <names|all>` (likewise waves, blueprints, doctrines)

`foes` includes only regular-wave swarm candidates eligible for bans; unused enemy assets are excluded. `waves` is the separate final-wave catalog. Use Tab after `ban foes ` to complete valid foe names.
- `list bans`, `clear bans`
- `list frontiers`, `list selected frontiers`, `select frontiers <names|all>`, `unselect frontiers <names|all>`, `clear frontiers`

## Challenges

- `list challenges`, `list selected challenges`, `list active challenges`
- `show challenges <id>`: name, author, ID, and description
- `select challenges <ids|all>`, `unselect challenges <ids|all>`, `clear challenges`
- Challenges: `cheapest-tower-only` (Cheapest Tower Only, `@DiVerrencia`), `chain-of-command` (Chain of Command, `@DiVerrencia`), `left-turn-only` (Left Turn Only, `@bigbossodin`), `eight-ball` (8-Ball, `@GameFamiliar`), and `frozen-logistics` (Frozen Logistics). Selections persist; active challenges are locked for the current run.
- Read-only indicators on Commander selection, Frontier selection, and the in-run HUD open challenge details. Selection is console-only.

## Settings and presets

- `set seed <value>`, `clear seed`, `status seed`
- `set starting tiles <shapes>`, `list starting tiles`, `inspect starting tiles`, `clear starting tiles [D1|D2|D3|D4]`. Shape count determines difficulty; relative orientations are used.
- `set frontier diagnostics on|off`, `status frontier diagnostics`
- `status toolkit`
- For `frontiers`, `challenges`, `bans`, and `starting tiles`: `save <domain> as <name>`, `load <domain> <name>`, `list <domain> presets`, `inspect <domain> preset <name>`, `delete <domain> preset <name>`.

Use the in-game `help` for argument validation, dynamically available catalogs, and additional registered operations. Old `seed set`, `select frontier`, `challenge ...`, `list banlists`, and `activate tileset` forms are not canonical in v0.3.0.
