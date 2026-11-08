# Tower Dominion Toolkit v0.3.2

## Native encounter pinning

- Added `pin foes` / `unpin foes` and `pin waves` / `unpin waves` to request encounter inclusion in native choice slots, subject to native eligibility and mandatory choices.
- Added `list pinned foes`, `list pinned waves`, `list pins`, and `clear pins`.
- Added named persistent pin presets (`save pins as`, `load pins`, `list pins presets`, `inspect pins preset`, `delete pins preset`). Active pins remain session-only.
- Ban/pin conflicts are rejected atomically, including conflicting preset loads; listing and error messages use readable encounter names.
- In-game verification: Medacris and Bryta regular-wave pins; Powerful Host final-wave pin; Breeding Grounds final-wave pin on Difficulty 2. Breeding Grounds remains excluded on Difficulty 1.
- 419 automated tests passed. Exhaustive native wave/tier combinations and coexistence tests remain unverified.

Install the ZIP with the game closed. Replace all four Toolkit DLLs together. Requires MelonLoader; see the repository installation guide.
