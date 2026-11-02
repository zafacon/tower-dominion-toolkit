# Release process

The private repository is authoritative. The public Zafacon repository is a distribution mirror; do not mirror private history without an explicit decision on author metadata.

## GitHub CI

The public CI workflow runs tests for Model, Modules and Runtime through the test project. The game adapter cannot be built on a standard hosted runner because it requires the locally installed game and MelonLoader-generated IL2CPP reference assemblies. No game assemblies, MelonLoader binaries, or private credentials should be committed or uploaded as artifacts.

CI has read-only repository permissions, no secrets and no release-publishing permission.

## Local release dry run

On a machine with Tower Dominion and MelonLoader installed:

```bash
dotnet test tests/TowerDominion.Toolkit.Tests/TowerDominion.Toolkit.Tests.csproj -c Release
./scripts/package-game.sh "/path/to/Tower Dominion" ./dist
unzip -l dist/tower-dominion-toolkit-v0.1.0-melonloader.zip
(cd dist && sha256sum -c tower-dominion-toolkit-v0.1.0-melonloader.zip.sha256)
```

Verify the ZIP includes only four Toolkit DLLs, INSTALL.txt and SHA256SUMS. Confirm in-game behavior with the full set of four matching DLLs. Run coexistence checks with Tower Dominion Leak Fix before release approval.

## Publishing gate

The approved public history strategy is a clean initial snapshot. Use Game Familiar <zafaconsolutions@gmail.com> for the initial public commit; preserve the original private repository history unchanged. Review author emails, paths, licenses, and source for private data. Verify that the authenticated GitHub account has repository creation rights in `zafacon`. Do not push the private repository's existing history by default.

After approval, prepare the public source, push to the organization, verify CI, and only then create the `v0.1.0` tag and GitHub release attaching the locally built ZIP and its `.sha256` file. GitHub Actions does not build or publish release binaries.
