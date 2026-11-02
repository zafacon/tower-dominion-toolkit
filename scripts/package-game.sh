#!/usr/bin/env bash
set -euo pipefail

if [[ $# -ne 2 ]]; then
  echo "Usage: $0 <Tower Dominion game directory> <output directory>" >&2
  exit 2
fi

game_dir="$(realpath "$1")"
output_dir="$(realpath -m "$2")"
repo_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
project="$repo_dir/src/TowerDominion.Toolkit.Game/TowerDominion.Toolkit.Game.csproj"
version="$(sed -n 's/.*<Version>\([^<]*\)<\/Version>.*/\1/p' "$repo_dir/Directory.Build.props" | head -1)"

if [[ ! "$version" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
  echo "Cannot determine a valid release version." >&2
  exit 1
fi
if [[ ! -f "$game_dir/MelonLoader/net6/MelonLoader.dll" ]]; then
  echo "Missing MelonLoader installation: $game_dir" >&2
  exit 1
fi

dotnet build "$project" --configuration Release "-p:TowerDominionGamePath=$game_dir" --nologo

source_dir="$repo_dir/src/TowerDominion.Toolkit.Game/bin/Release/net6.0"
stage="$(mktemp -d)"
trap 'rm -rf "$stage"' EXIT
mkdir -p "$stage/Mods" "$stage/UserLibs" "$output_dir"
cp "$source_dir/TowerDominionToolkit.dll" "$stage/Mods/"
for name in Model Modules Runtime; do
  cp "$source_dir/TowerDominion.Toolkit.$name.dll" "$stage/UserLibs/"
done
cat > "$stage/INSTALL.txt" <<'DOC'
Tower Dominion Toolkit — MelonLoader installation
1. Install MelonLoader for Tower Dominion separately.
2. Close the game.
3. Extract the Mods/ and UserLibs/ directories into the Tower Dominion game directory.
4. Start the game and open the Toolkit console with the backtick key.
5. Type help or version to verify the installation.

Do not install alongside TowerDominionConsole.dll unless patch coexistence has been validated.
The package contains Toolkit binaries only, not game or MelonLoader files.
DOC
archive="$output_dir/tower-dominion-toolkit-v$version-melonloader.zip"
(
  cd "$stage"
  sha256sum Mods/*.dll UserLibs/*.dll > SHA256SUMS
  zip -q -r "$archive" Mods UserLibs INSTALL.txt SHA256SUMS
)
(cd "$output_dir" && sha256sum "$(basename "$archive")" > "$(basename "$archive").sha256")
echo "Package: $archive"
echo "Checksum: $archive.sha256"
