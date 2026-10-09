# Chuchu Games – Grid Puzzle

Engine-free building blocks for "place things in a grid" puzzles (`noEngineReferences`, so it also compiles in plain .NET).

- `Grid<T>`: a fixed rows × cols grid. Row 0 is the **bottom** (side-view buildings: row = floor). Supports `Neighbours(cell, Direction, range)` with orthogonal directions only, plus top-row, bottom-row and edge-column helpers.
- `Cell`, `Direction` (flags: `Left`, `Right`, `Up`, `Down`, `Sides`, `Vertical`, `All`).
- `TagLayers`: per-cell tags with a permanent base layer and a temporary modifier layer. Effective tags = (base ∪ added) − removed, so removals win. `IsTemporary(tag)` tells you when to show a "temporary" badge.

- `PlacementSearch.Run(slotCount, domains, evaluate, options)`: an exhaustive depth-first search that gives each item a distinct slot from its candidate list. It tries the most-constrained items first, scores each full arrangement with your callback, and reports the best score, how many arrangements reach it, and how many hit a target score (stopping early if you ask). It also supports an optional `KeepBranch` pruning callback and a leaf cap, and reports `Exhaustive = false` when capped. Ghost Hotel's Night Validator is built on it.

Planned: a seeded generator (endless mode).

## Install in another project
Copy this folder into `Packages/`, or add it by git URL once it has its own repo.
