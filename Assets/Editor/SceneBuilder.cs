using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Builds the Main scene from code rather than hand-authoring Unity's YAML
/// scene format. Run via:
///   Unity -batchmode -nographics -quit -projectPath . -executeMethod SceneBuilder.BuildMainScene
/// </summary>
public static class SceneBuilder
{
	private const string ScenePath = "Assets/Scenes/Main.unity";

	// Tile art is 111x128px: a 111x64 top-face diamond (imported at 111 PPU,
	// i.e. 1 world unit wide) with a 64px front-face "skirt" below it.
	private static readonly Vector3 IsoCellSize = new Vector3(1f, 64f / 111f, 1f);

	[MenuItem("Tools/Build Main Scene")]
	public static void BuildMainScene()
	{
		var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

		BuildGrid();
		PlayerBuilder.Build();

		EditorSceneManager.SaveScene(scene, ScenePath);
		Debug.Log($"Saved scene to {ScenePath}");
	}

	private static void BuildGrid()
	{
		var gridGo = new GameObject("Grid");
		var grid = gridGo.AddComponent<Grid>();
		grid.cellLayout = GridLayout.CellLayout.Isometric;
		grid.cellSize = IsoCellSize;
		grid.cellGap = Vector3.zero;
		gridGo.AddComponent<DebugCellSizeOverride>();

		// TerrainGenerator lives directly on the Grid GameObject (not a
		// child) because it needs *this* configured Grid via
		// GetComponent<Grid>() -- putting it on a child previously caused
		// [RequireComponent(typeof(Grid))] to silently add a second, default
		// (Rectangle, cellSize 1x1) Grid onto that child, which
		// GetComponent<Grid>() found instead of the real one, and every
		// position came out wrong with no error anywhere.
		var terrain = gridGo.AddComponent<TerrainGenerator>();
		terrain.waterSprite = LoadTileSprite("water");
		terrain.sandSprite = LoadTileSprite("sand");
		terrain.grassSprite = LoadTileSprite("grass");
		terrain.dirtSprite = LoadTileSprite("dirt");
		terrain.stoneSprite = LoadTileSprite("stone");
		terrain.snowSprite = LoadTileSprite("snow");
	}

	private static Sprite LoadTileSprite(string name) =>
		AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/Tiles/{name}.png");
}
