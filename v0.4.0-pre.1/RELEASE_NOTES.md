# Tower Dominion Toolkit v0.4.0-pre.1

## Corrective RNG and gameplay rules — pre-release

- Adds isolated corrective RNG streams and persistence for Toolkit-controlled gameplay decisions without advancing Unity shared global RNG.
- Adds persistent Gameplay Rules: Bryta Nest Reroll, Rear Guard Longest Path, No Discovery, Full Discovery, Neutral Tile Preview, and No Duplicate Rewards.
- Neutral discovery preview/placement keeps the selected neutral identity and eventual tile stable through zone transforms and commit.
- No Duplicate Rewards redraws repeated random blueprint/doctrine identities within a reward-choice bundle through native weighted selectors.
- Improves mapped gameplay-input isolation while the Toolkit console is open.
- 503 automated tests pass; extended live testing covered corrective neutral placement, Discovery behavior, Bryta rerolls, Rear Guard, and duplicate blueprint rewards.

This is a **pre-release intended for broader player testing**, not the stable release. Native hooks, doctrine duplicate replacement, exhausted reward pools, and every rule combination have not been exhaustively live-tested.

Install the ZIP with the game closed and replace all Toolkit DLLs together. Requires MelonLoader. When reporting gameplay-rule issues, include MelonLoader/Latest.log when possible.
