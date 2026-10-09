#!/usr/bin/env bash
# Ghost Hotel build pipeline (Git Bash on Windows). Unity must be CLOSED for this project.
#
#   Tools/pipeline.sh            # fast tests + Unity tests + import + validate + scenes + Windows build
#   Tools/pipeline.sh fast       # dotnet tests only (no Unity needed, ~1 s)
#   Regenerate placeholder art/audio first with: dotnet run --project Tools/ArtGen
#   UNITY=/path/to/Unity.exe Tools/pipeline.sh
#
# Logs go to Logs/pipeline/. Exits non-zero on the first failure.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
UNITY="${UNITY:-/c/Program Files/Unity/Hub/Editor/6000.0.66f2/Editor/Unity.exe}"
LOGS="$ROOT/Logs/pipeline"
mkdir -p "$LOGS"
win() { cygpath -w "$1"; }

step() { echo; echo "=== $1"; }

step "dotnet tests (engine-free code)"
dotnet test "$ROOT/Tools/DotnetTests" 2>&1 | tail -1
[ "${1:-}" = "fast" ] && exit 0

if [ -f "$ROOT/Temp/UnityLockfile" ] && ! (rm "$ROOT/Temp/UnityLockfile" 2>/dev/null); then
  echo "Unity has this project open. Close it and re-run."; exit 2
fi

unity() { # unity <name> <args...>
  local name="$1"; shift
  if "$UNITY" -batchmode -projectPath "$(win "$ROOT")" -logFile "$(win "$LOGS/$name.log")" "$@"; then
    echo "ok ($name)"
  else
    echo "FAILED ($name) — see Logs/pipeline/$name.log"; grep -E "error CS|ERROR|FAIL|Exception" "$LOGS/$name.log" | head -20; exit 1
  fi
}

step "Unity EditMode tests"
unity tests -runTests -testPlatform EditMode -testResults "$(win "$LOGS/editmode.xml")"
grep -oE 'total="[0-9]+" passed="[0-9]+" failed="[0-9]+"' "$LOGS/editmode.xml" | head -1

step "Import content (JSON → assets)"
unity import -quit -executeMethod GhostHotel.EditorTools.ContentImporter.ImportBatch
grep -E "Import (OK|FAILED)|warning " "$LOGS/import.log" | head -20

step "Link art library"
unity art -quit -executeMethod GhostHotel.EditorTools.ArtGenMenu.LinkBatch
grep -E "\[ArtLibrary\]" "$LOGS/art.log"

step "Night Validator"
unity validate -quit -executeMethod GhostHotel.EditorTools.NightValidatorWindow.ValidateBatch
grep -E "^(PASS|FAIL)" "$LOGS/validate.log"

step "Build scenes"
unity scenes -quit -executeMethod GhostHotel.EditorTools.SceneBuilder.BuildAll

step "Windows player"
unity build -quit -executeMethod GhostHotel.EditorTools.BuildScript.BuildWindows
grep -E "\[BuildScript\]" "$LOGS/build.log"

echo; echo "Pipeline OK → Builds/Windows/GhostHotel.exe"
