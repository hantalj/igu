# igu

A Unity port of [igrunner](https://github.com/hantalj/igrunner) (the Godot version of this isometric game) — same art, same procedural terrain, same 8-directional character, reworked for Unity 6.

## Tech stack

- **Engine**: Unity 6000.5.5f1
- **Language**: C#
- Same free/CC0 art as igrunner (Kenney Isometric Blocks tiles, Hormelz's 8 Directional Knight Character)

## Requirements

- Unity 6000.5.5f1 (or compatible Unity 6.x)

## Getting started

Open this folder in Unity Hub/Editor and press Play on `Assets/Scenes/Main.unity`, or build headlessly:

```
Unity -batchmode -nographics -quit -projectPath . -executeMethod SceneBuilder.BuildMainScene
Unity -batchmode -nographics -quit -projectPath . -executeMethod BuildScript.BuildMacStandalone
```

WASD/arrow keys move the knight around a 16x16 procedurally generated isometric landscape (water/sand/grass/dirt/stone/snow bands from Perlin noise). The character walks and faces one of 8 directions based on movement, idling facing the same direction when stopped.

## Project layout

- `Assets/Scenes/Main.unity` — the only scene, built entirely from code (see below), not hand-edited in the Editor
- `Assets/Scripts/TerrainGenerator.cs` — generates the ground at runtime from `Mathf.PerlinNoise` (elevation → biome), rendering each tile as its own `SpriteRenderer` positioned via `Grid.CellToWorld`
- `Assets/Scripts/PlayerMovement.cs` — WASD/arrow input projected onto the isometric axes, 8-compass-direction facing, depth-sorting against the ground
- `Assets/Scripts/DirectionalAnimator.cs` — swaps sprite frames across 8 directions x {walk, idle}, a lightweight alternative to a full `AnimatorController`
- `Assets/Scripts/CameraFollow.cs` — keeps the camera on the player without parenting (see gotcha below)
- `Assets/Scripts/AutoScreenshot.cs`, `DebugSceneDump.cs`, `DebugCellSizeOverride.cs` — inert unless specific env vars are set; used throughout development to verify real rendered output rather than trusting code review alone
- `Assets/Editor/SceneBuilder.cs` — builds `Main.unity` from code (`Tools > Build Main Scene`)
- `Assets/Editor/PlayerBuilder.cs` — constructs the Player GameObject and loads/wires its sprite frames
- `Assets/Editor/BuildScript.cs` — headless macOS standalone build (`Tools > Build Mac Standalone`)
- `Assets/Editor/GameArtImportSettings.cs` — configures sprite import settings (pivot, PPU, grid-slicing) automatically on import instead of by hand in the Inspector
- `Assets/Art/Tiles`, `Assets/Art/Knight` — same CC0 art as igrunner (see each folder's `License.txt`)

## Notable differences from the Godot version, and why

**Ground rendering isn't a Tilemap, despite using Unity's Grid/Tilemap system.** `Grid.CellToWorld` handles the isometric position math (confirmed correct independently), but `TilemapRenderer`'s automatic compositing broke down for this art: each tile sprite is 128px tall against a 32px row-step (the cube's front-face "skirt" needs covering by up to 3 rows of tiles in front of it), and neither `Individual`+`CustomAxis` nor `IsometricZAsY`+`Orthographic` sorting composited that correctly — confirmed by screenshot, changing sort mode made no visible difference at all, which pointed at `TilemapRenderer`'s mesh batching itself rather than sort configuration. `TerrainGenerator` uses the Grid purely for position math and renders each tile as its own `SpriteRenderer` with an explicit `sortingOrder`, the same mechanism that worked reliably in the Godot version.

**The isometric projection formula has a negated Y term** compared to igrunner's `Player.cs`, re-derived from scratch rather than blindly translated — Unity's Y+ is screen-up, Godot's Y+ is screen-down, so a literal port would move the character in mirrored vertical directions. Verified by cross-checking specific inputs (pure D, pure S) against the already-confirmed Godot behavior before trusting it.

**The camera doesn't parent to the Player**, even though that's the obvious/common pattern (and is what igrunner does in Godot). Parenting the camera under Player made the Player's own SpriteRenderer stop rendering entirely — reproduced multiple times, root cause not fully isolated, so `CameraFollow.cs` just tracks the player's position in `LateUpdate()` without parenting, which works reliably.

**`SpriteRenderer.sortingOrder` is backed by an `Int16`** despite the C# property being typed `int` — an early attempt to bias the player's sort order by a large constant (`+100000`) silently wrapped around to a large *negative* number, putting the player far behind the terrain instead of in front, with no error anywhere. This is the actual reason the player was invisible for a long stretch of development (a red herring about camera parenting looked plausible at first, since un-parenting happened to coincide with other changes, but the real fix was keeping the sorting bias within Int16 range).
