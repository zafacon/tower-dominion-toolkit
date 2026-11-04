## Tower Dominion Toolkit v0.3.0

Adds four gameplay challenges alongside Cheapest Tower Only: **Chain of Command**, **8-Ball**, **Frozen Logistics**, and **Left Turn Only**.

- Chain of Command limits construction to the HQ-connected building network, disables selling/cancelling, and displays yellow crosses at network expansion positions.
- Frontier setup waits for saved modifiers and native score/dotation updates before allowing Start.
- The selected-challenge indicator remains visible on Commander selection without Frontier modifiers.
- Corrected Chain of Command overlay visibility between runs and avoided a crash-prone special reward hook.
- 400 automated tests passed, with additional in-game verification.

**Installation:** Close Tower Dominion, extract the ZIP into the game directory (merging `Mods/` and `UserLibs/`), and restart. Install all four Toolkit DLLs together. Requires MelonLoader; no game files are included.
