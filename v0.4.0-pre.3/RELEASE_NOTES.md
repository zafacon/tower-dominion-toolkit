# Tower Dominion Toolkit v0.4.0-pre.3

## Blueprint Bag and combat display fixes

- Optional persistent Blueprint Bag: store entire blueprint stacks with right-click, retrieve with left-click, and scroll independently of the camera. Enable with `enable qol blueprint-bag`; disabled by default and suppressed during Forced Deployment.
- Armored Staging Ground now includes its own Auctoritas tank damage in the per-wave damage display. End-game aggregation remains pending.
- Chain of Command permits native-valid platform slots and Assault Team deployment.
- Challenge HUD and placement overlays are cleaned up across runs and hidden during reward, defeat and other modal screens.
- Retains the gameplay rules and corrective RNG from pre.2.

508 automated tests passed; Release build completed with zero warnings or errors. Gameplay features received local live validation; broader compatibility testing remains pending. This is a pre-release, not the stable release.

Install with the game closed and replace all five Toolkit DLLs together. Requires MelonLoader. Report reproducible issues with the Toolkit/game versions and relevant MelonLoader log excerpts.
