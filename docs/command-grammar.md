# Canonical command grammar — Phase 1 baseline

Status: **Phase 1 consolidated specification**, 2026-11-02. Source: accepted decisions in command-grammar-draft.md. Implementation belongs to Phase 2 onward.

## Grammar

```text
help [verb | domain | verb domain]
<verb> [qualifier] <domain> [arguments]
```

Verbs are globally defined; domains declare supported capabilities. Collection domains are plural, multiword domains use spaces. Qualifiers are reusable and registered per domain. Argument descriptors handle quoted multiword names, comma-separated selections, `all`, and free-form values. Parsing prefers the longest registered domain name and matches complete tokens, not string prefixes. A qualifier may only appear where its registered operation permits it.

## Canonical command matrix

| Domain | Commands |
| --- | --- |
| foes | `list foes`; `list banned foes`; `ban foes <names|all>`; `unban foes <names|all>` |
| waves | `list waves`; `list banned waves`; `ban waves <names|all>`; `unban waves <names|all>` |
| blueprints | `list blueprints`; `list banned blueprints`; `ban blueprints <names|all>`; `unban blueprints <names|all>` |
| doctrines | `list doctrines`; `list banned doctrines`; `ban doctrines <names|all>`; `unban doctrines <names|all>` |
| frontiers | `list frontiers`; `list selected frontiers`; `select frontiers <names|all>`; `unselect frontiers <names|all>`; `clear frontiers` |
| challenges | `list challenges`; `list selected challenges`; `list active challenges`; `select challenges <ids|all>`; `unselect challenges <ids|all>`; `clear challenges` |
| bans | `list bans`; `clear bans` |
| starting tiles | `list starting tiles`; `inspect starting tiles`; `set starting tiles <shapes>`; `clear starting tiles [D1|D2|D3|D4]` |
| seed | `set seed <value>`; `clear seed`; `status seed` |
| frontier diagnostics | `set frontier diagnostics on|off`; `status frontier diagnostics` |
| toolkit | `status toolkit` |

For each of **frontiers, challenges, bans, starting tiles**, also register:
```text
save <domain> as <name>
load <domain> <name>
list <domain> presets
inspect <domain> preset <name>
delete <domain> preset <name>
```

The preset suffix is a registered capability argument, not a separate domain. `list frontiers` lists modifiers; `list frontiers presets` lists named snapshots. `inspect starting tiles` examines current overrides; `inspect starting tiles preset <name>` examines a saved snapshot. `list bans` enumerates active bans across all four banned catalogs; `list banned foes` scopes by catalog.

## Semantics

- `list`: enumerate; `inspect`: detailed read-only description; `status`: operational state.
- `select`/`unselect`: add/remove selected members; `ban`/`unban`: add/remove prohibited members.
- `set`: assign/replace a value; `clear`: reset domain or one keyed override to defaults.
- `save`: capture a named snapshot; `load`: **replace** working state from snapshot; `delete`: remove named snapshot.
- `unload`, `activate tileset`, `deactivate tileset`, obsolete absolute-tile commands and domain-first `challenge ...` are not canonical.
- Starting-tile `set` infers D1–D4 from shape count, replacing only that difficulty. Missing override means native random choice. `clear starting tiles` resets all; `clear starting tiles D3` resets D3.
- Frontiers, challenges and starting-tile overrides are persistent; bans are session-scoped. Presets persist independently. Challenge mutations, including preset loads, respect run locks.
- An invalid, incomplete or ambiguous request does not partially change state.

## Help and completion

`help` summarizes verbs and domains; `help <verb>` lists compatible domains; `help <domain>` lists operations; `help <verb> <domain>` explains precise syntax, arguments, examples, defaults and persistence. All derive from registered operations. Errors show contextual help.

Tab completion uses the same registry: unique match completes; multiple matches extend common prefix and repeated Tab lists candidates; no match leaves input unchanged. Completion is cursor-aware, handles comma-separated members without modifying earlier members, quotes multiword names as needed, and covers help, qualifiers, names and all argument positions. Engine behavior is headless; keyboard/display is a game adapter.

## Ambiguity review

1. `help <verb>` versus `help <domain>`: verbs and domain names are distinct in this matrix, so no collision; registry must detect future collisions at registration or provide explicit disambiguation.
2. `list <qualifier> <domain>` versus `list <domain> presets`: qualifier appears **before** the domain; preset marker appears **after**. Longest-domain matching and token boundaries disambiguate.
3. `load <domain> <name>` versus domain names containing spaces: consume the registered domain first, then parse the snapshot name.
4. `set starting tiles <shapes>` versus `clear starting tiles D3`: infer difficulty only on set; explicit difficulty only for scoped clear.
5. `all` is a reserved selection token; a literal entity named `all` requires quoting or explicit ID resolution.
6. `status toolkit` and `status frontier diagnostics` have no overlap with `inspect` or `list`.
7. Dynamic catalogs may be unavailable outside a run: the engine must report unavailable catalog state, not mistake it for invalid syntax.

## Phase 2 handoff

Implement typed descriptors for verbs, domains, optional qualifiers, arguments, preset capability, help text, completion providers and handlers. Add registration-time conflict detection and a headless fake registry. Do not copy the legacy verb-only router. Phase 2 tests should assert that all matrix entries are discoverable and unsupported combinations are rejected.

## Clarifications for implementation (not grammar blockers)

Exact formatting of detailed inspection output and the `status toolkit` report can be settled with adapters. Preset name conflict/overwrite policy and legacy command aliases are implementation policy, not grammar structure. The `all` token should have explicit quoting tests. Individual catalog entity inspection is **not** required by this matrix unless later registered as a capability.
