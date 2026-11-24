# Console commands — v0.3.1

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

- `list rules`, `list enabled rules`, `show rules <id>`, `enable rules <ids|all>`, `disable rules <ids|all>`, `clear rules`
- Gameplay Rules in this pre-release: `bryta-nest-reroll`, `rear-guard-longest-path`, `no-discovery`, `full-discovery`, `neutral-tile-preview`, `no-duplicate-rewards`. `no-discovery` and `full-discovery` are mutually exclusive.
- `list challenges`, `list selected challenges`, `list active challenges`
- `show challenges <id>`: name, author, ID, description, and known incompatibilities (incompatible selections and preset loads are rejected with an explanation)
- `select challenges <ids|all>`, `unselect challenges <ids|all>`, `clear challenges`
- Challenges: `cheapest-tower-only` (Cheapest Tower Only, `@DiVerrencia`), `chain-of-command` (Chain of Command, `@DiVerrencia`), `left-turn-only` (Left Turn Only, `@bigbossodin`), `eight-ball` (8-Ball, `@GameFamiliar`), `frozen-logistics` (Frozen Logistics), `low-tech` (Low Tech, `@cookiebat`), and `forced-deployment` (Forced Deployment, `@darksoulsgreat6205`). Selections persist; active challenges are locked for the current run.
- Cheapest Tower Only and Forced Deployment cannot be selected together. Incompatible selections, `select challenges all`, and incompatible preset loads fail atomically with an explanation. Use `show challenges <id>` to inspect known conflicts.
- Read-only indicators on Commander selection, Frontier selection, and the in-run HUD open challenge details and known incompatibilities. Selection is console-only.

## Settings and presets

- `set seed <value>`, `clear seed`, `status seed`
- `set starting tiles <shapes>`, `list starting tiles`, `inspect starting tiles`, `clear starting tiles [D1|D2|D3|D4]`. Shape count determines difficulty; relative orientations are used.
- `set frontier diagnostics on|off`, `status frontier diagnostics`
- `status toolkit`
- For `frontiers`, `challenges`, `rules`, `bans`, `pins`, and `starting tiles`: `save <domain> as <name>`, `load <domain> <name>`, `list <domain> presets`, `inspect <domain> preset <name>`, `delete <domain> preset <name>`.

Use the in-game `help` for argument validation, dynamically available catalogs, and additional registered operations. Old `seed set`, `select frontier`, `challenge ...`, `list banlists`, and `activate tileset` forms are not canonical in v0.3.0.

### Encounter pins

- `pin foes <names|all>` / `unpin foes <names|all>`; similarly `pin waves` / `unpin waves`.
- `list pinned foes`, `list pinned waves`, `list pins`, `clear pins` (clears active pins in both categories without deleting presets).
- Pins are session-only, and can be explicitly saved with `save pins as <name>`, restored with `load pins <name>`, listed with `list pins presets`, inspected with `inspect pins preset <name>`, and removed with `delete pins preset <name>`.
- Foe pins use the same regular-wave encounter catalog and native eligibility rules as bans; there is no separate tier restriction. Final-wave pins use the same final-wave catalog as bans and respect native availability. Pins have been live-tested with Medacris and Bryta in separate runs, Powerful Host in a final-wave selection, and Breeding Grounds on Difficulty 2 (excluded on Difficulty 1); inclusion remains subject to native eligibility, available nonmandatory choice slots, and duplicate restrictions.
- Ban and pin commands reject direct conflicts; loading conflicting presets is rejected without replacing the current configuration.
- Existing foe bans apply to the regular-wave foe catalog; final-wave bans use the separate wave catalog. Native availability and choice-slot constraints also apply to pins.
