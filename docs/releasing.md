# Release process

The private `archlinux` repository is authoritative. The public Zafacon repository on `pcastaneda.edufile.local` has independent history; never push private history directly to GitHub. See `docs/pending-challenges-low-tech-forced-deployment.md` for workstation paths and release-asset branch details.

## Prepare and validate locally

1. Update `Directory.Build.props`, `CHANGELOG.md`, `README.md`, command and installation docs, and release notes. Use a new version; do not reuse an existing tag.
2. Review the full diff, untracked files, author metadata, credentials, game assets and licenses. Preserve local `research/` and `deployment-backups/`; do not include them in the public source or ZIP.
3. With game references installed, run:

```bash
dotnet test tests/TowerDominion.Toolkit.Tests/TowerDominion.Toolkit.Tests.csproj -c Release --nologo
dotnet format whitespace tests/TowerDominion.Toolkit.Tests/TowerDominion.Toolkit.Tests.csproj --verify-no-changes --no-restore
./scripts/package-game.sh '/path/to/Tower Dominion' ./dist
unzip -l dist/tower-dominion-toolkit-vX.Y.Z-melonloader.zip
(cd dist && sha256sum -c tower-dominion-toolkit-vX.Y.Z-melonloader.zip.sha256)
git diff --check
```

4. Verify the ZIP contains exactly four matching Toolkit DLLs, `INSTALL.txt`, and `SHA256SUMS`; verify the internal checksums. Never bundle game/MelonLoader assemblies.
5. Deploy the four matching DLLs only while the game is closed, after backing up the installed copies. Confirm MelonLoader startup, command help/status, challenge selection and incompatibility errors, run-start restrictions, game restart persistence, and coexistence with Tower Dominion Leak Fix. Record any unverified tests honestly.

## Publish after explicit approval

1. Commit and push reviewed private changes to the private remote. Prepare the public source snapshot separately on `pcastaneda.edufile.local` and review the diff before pushing it to `github.com:zafacon/tower-dominion-toolkit.git`.
2. Place `vX.Y.Z/` release ZIP, ZIP `.sha256`, and `RELEASE_NOTES.md` on the public `release-assets` branch, then push that branch and public source. Public CI tests game-independent projects; it cannot build the game adapter without proprietary references.
3. Push the **public** `vX.Y.Z` tag only after source and assets are available. The public `.github/workflows/release.yml` publishes matching release assets from `release-assets` on tag push. Do not assume the older v0.1.0 manual-release instructions apply.
4. Verify CI, GitHub Actions release job, attached assets, checksums and release page. Keep existing tags and release history intact.

Do not publish or tag while release gates are unresolved or without user approval.
