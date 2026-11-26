# Blueprint Bag (QoL)

Optional visual storage for blueprint stacks, especially useful in Endless. Defaults off; no hand limit or native ownership changes.

## Controls

- `enable qol blueprint-bag`, `disable qol blueprint-bag`, `list qol`.
- Right-click a hand card to store its entire stack; left-click a bag card to retrieve it. BAG counts distinct stored types.
- `store bag <blueprint>`, `retrieve bag <blueprint>`, `list bag`, `clear bag`. Contextual completion uses eligible names.
- New copies of a stored type accumulate in the native stack; new types remain in hand. Retrieval appends to the native visual layout. Disabling restores stored stacks. Forced Deployment suppresses the feature without changing the saved preference.

## Implementation

The native BlueprintUI and inventory remain authoritative. The Toolkit removes visual layout entries and hides stored cards with CanvasGroup, restoring those entries on retrieval. Independent bag cards reuse native sprites, geometry, materials and mask settings. The five-column grid scrolls independently of camera zoom. Tooltips are intentionally excluded.

Preference is stored in `qol-blueprint-bag.json`. Run-specific IDs are stored in `Progress.gd.tdtoolkit-qol.json`, bound to the native save by SHA-256. Writes are atomic; missing or mismatched saves restore no stored IDs. New runs and Retry clear bag contents, preserving preference. Native hand initialization removes stale injected controls.

## Validation — 2026-11-25

User validated console/mouse transfers, persistence, empty state, card rendering, native-style controls, scrolling and camera isolation, Retry, and Forced Deployment exclusion including stale-button cleanup. Four persistence regression tests added; full suite 508 passing, Release build zero warnings/errors. Native hooks are not covered by headless tests. Unusual save timing, corrupt metadata, frozen-hand disable and extreme inventory stress remain untested. Included in v0.4.0-pre.3 for broader testing.
