# igu

A Unity port of [igrunner](https://github.com/hantalj/igrunner) (the Godot version of this isometric game) — same art, same procedural terrain, same 8-directional character, reworked for Unity 6.

## Tech stack

- **Engine**: Unity 6000.5.5f1
- **Language**: C#
- Same free/CC0 art as igrunner (Kenney Isometric Blocks tiles, Hormelz's 8 Directional Knight Character)

## Requirements

- Unity 6000.5.5f1 (or compatible Unity 6.x)

## Getting started

Open this folder in Unity Hub/Editor and press Play on `Assets/Scenes/MainMenu.unity`, or build headlessly:

```
Unity -batchmode -nographics -quit -projectPath . -executeMethod SceneBuilder.BuildMainScene
Unity -batchmode -nographics -quit -projectPath . -executeMethod MainMenuBuilder.BuildMainMenuScene
Unity -batchmode -nographics -quit -projectPath . -executeMethod BuildScript.BuildMacStandalone
```

The game launches into a main menu (Start New Game / Start Demo / Exit Igu). "Start Demo" loads the fixed-seed terrain everything above was verified against; "Start New Game" rolls a fresh random seed. To skip the menu and launch straight into the demo (handy for quick manual testing), set `IGU_SKIP_MENU=1` or pass `-skipmenu` on the command line.

WASD/arrow keys move the knight around a 16x16 procedurally generated isometric landscape (water/sand/grass/dirt/stone/snow bands from Perlin noise). The character walks and faces one of 8 directions based on movement, idling facing the same direction when stopped. Once gameplay has started (New Game or Demo), Escape quits the game; the main menu itself has no Escape shortcut, since it already has its own Exit Igu button.

## Project layout

- `Assets/Scenes/MainMenu.unity` — entry-point scene, built from code, not hand-edited
- `Assets/Scenes/Main.unity` — the game/demo scene, also built from code
- `Assets/Scripts/TerrainGenerator.cs` — generates the ground at runtime from `Mathf.PerlinNoise` (elevation → biome), rendering each tile as its own `SpriteRenderer` positioned via `Grid.CellToWorld`
- `Assets/Scripts/PlayerMovement.cs` — WASD/arrow input projected onto the isometric axes, 8-compass-direction facing, depth-sorting against the ground
- `Assets/Scripts/DirectionalAnimator.cs` — swaps sprite frames across 8 directions x {walk, idle}, a lightweight alternative to a full `AnimatorController`
- `Assets/Scripts/CameraFollow.cs` — keeps the camera on the player without parenting (see gotcha below)
- `Assets/Scripts/MainMenuController.cs` — wires the three menu buttons and the menu-skip bypass
- `Assets/Scripts/GameSession.cs` — the one piece of state passed from menu to game scene (new-game seed vs. demo's fixed seed)
- `Assets/Scripts/GameExit.cs` — shared `Application.Quit()`/editor-stop logic used by both the menu's Exit Igu button and `QuitOnEscape`
- `Assets/Scripts/QuitOnEscape.cs` — quits the game when Escape is pressed during gameplay (Main scene only)
- `Assets/Scripts/AutoScreenshot.cs`, `DebugSceneDump.cs`, `DebugCellSizeOverride.cs` — inert unless specific env vars are set; used throughout development to verify real rendered output rather than trusting code review alone
- `Assets/Editor/SceneBuilder.cs` — builds `Main.unity` from code (`Tools > Build Main Scene`)
- `Assets/Editor/MainMenuBuilder.cs` — builds `MainMenu.unity` from code (`Tools > Build Main Menu Scene`)
- `Assets/Editor/PlayerBuilder.cs` — constructs the Player GameObject and loads/wires its sprite frames
- `Assets/Editor/BuildScript.cs` — headless macOS standalone build (`Tools > Build Mac Standalone`), includes both scenes with MainMenu first
- `Assets/Editor/GameArtImportSettings.cs` — configures sprite import settings (pivot, PPU, grid-slicing/bilinear-vs-point filtering) automatically on import instead of by hand in the Inspector
- `Assets/Art/Tiles`, `Assets/Art/Knight` — same CC0 art as igrunner (see each folder's `License.txt`)
- `Assets/Art/UI` — main menu art (background, button, title); see that folder's `README.md` for provenance and `generate_menu_art.py` to regenerate

## Notable differences from the Godot version, and why

**Ground rendering isn't a Tilemap, despite using Unity's Grid/Tilemap system.** `Grid.CellToWorld` handles the isometric position math (confirmed correct independently), but `TilemapRenderer`'s automatic compositing broke down for this art: each tile sprite is 128px tall against a 32px row-step (the cube's front-face "skirt" needs covering by up to 3 rows of tiles in front of it), and neither `Individual`+`CustomAxis` nor `IsometricZAsY`+`Orthographic` sorting composited that correctly — confirmed by screenshot, changing sort mode made no visible difference at all, which pointed at `TilemapRenderer`'s mesh batching itself rather than sort configuration. `TerrainGenerator` uses the Grid purely for position math and renders each tile as its own `SpriteRenderer` with an explicit `sortingOrder`, the same mechanism that worked reliably in the Godot version.

**The isometric projection formula has a negated Y term** compared to igrunner's `Player.cs`, re-derived from scratch rather than blindly translated — Unity's Y+ is screen-up, Godot's Y+ is screen-down, so a literal port would move the character in mirrored vertical directions. Verified by cross-checking specific inputs (pure D, pure S) against the already-confirmed Godot behavior before trusting it.

**The camera doesn't parent to the Player**, even though that's the obvious/common pattern (and is what igrunner does in Godot). Parenting the camera under Player made the Player's own SpriteRenderer stop rendering entirely — reproduced multiple times, root cause not fully isolated, so `CameraFollow.cs` just tracks the player's position in `LateUpdate()` without parenting, which works reliably.

**`SpriteRenderer.sortingOrder` is backed by an `Int16`** despite the C# property being typed `int` — an early attempt to bias the player's sort order by a large constant (`+100000`) silently wrapped around to a large *negative* number, putting the player far behind the terrain instead of in front, with no error anywhere. This is the actual reason the player was invisible for a long stretch of development (a red herring about camera parenting looked plausible at first, since un-parenting happened to coincide with other changes, but the real fix was keeping the sorting bias within Int16 range).

**Editor scripts must wire button clicks with `UnityEventTools.AddPersistentListener`, not `onClick.AddListener`.** `MainMenuBuilder` originally used `onClick.AddListener(controller.StartNewGame)` (and the other two buttons) while constructing the scene. `AddListener` only registers a *runtime* listener; called from an editor script executed via `-executeMethod` — a separate process that builds the scene and exits — that registration lives in the transient process's memory and is never serialized into the saved `.unity` file. The scene looked correct, the game compiled, and a test hook that called the controller methods directly even "passed" — but all three menu buttons did nothing when actually clicked, because their `m_PersistentCalls` were empty. `UnityEditor.Events.UnityEventTools.AddPersistentListener` is the editor-scripting equivalent of wiring a listener by hand in the Inspector, and does serialize. Caught only after replacing the direct-method-call test hook with one that simulates a real click through `UnityEngine.EventSystems.ExecuteEvents.Execute(...)` — a reminder that a test which bypasses the actual UI event path can pass while the UI itself is completely broken.

**`com.unity.ugui` had to be added to `Packages/manifest.json` explicitly** for the main menu's `Canvas`/`Button`/`Text` — it's bundled with the Editor install (no network fetch needed) but isn't a default dependency of a project created via `-createProject`, unlike the base 3D/2D modules.

**No font files are checked into this repo.** The menu title is pre-rendered to a PNG using a locally-installed commercial font (Georgia) — shipping a rendered bitmap of text set in a font is fine, but copying the actual `.ttf` into a pushed repo would be redistributing a licensed font binary without rights. Live button/UI text uses Unity's own built-in font instead, which ships with every Unity install.
