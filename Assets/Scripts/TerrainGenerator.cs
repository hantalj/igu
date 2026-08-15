using UnityEngine;

/// <summary>
/// Generates the ground at runtime from Perlin noise (elevation -> biome),
/// porting igrunner's Main.cs.
///
/// This uses Grid.CellToWorld for isometric position math (Unity's Tilemap
/// system), but spawns each tile as its own SpriteRenderer rather than
/// painting a Tilemap. TilemapRenderer's automatic compositing broke down
/// for this art: each tile sprite is 128px tall against a 32px row-step
/// (a 4-row-deep overlap, since the cube's front-face "skirt" needs to be
/// covered by up to 3 rows of tiles in front of it), and neither
/// Individual+CustomAxis nor IsometricZAsY+Orthographic sorting composited
/// that correctly -- verified by screenshot, changing sort mode made no
/// visible difference at all, which pointed at TilemapRenderer's mesh
/// batching itself rather than sort configuration. Explicit per-tile
/// sortingOrder (mirroring the y-sort approach from the Godot version)
/// composites it correctly.
/// </summary>
[RequireComponent(typeof(Grid))]
public class TerrainGenerator : MonoBehaviour
{
	public int gridWidth = 16;
	public int gridHeight = 16;

	public float noiseFrequency = 0.12f;
	public float noiseSeedOffset = 1337f;

	public Sprite waterSprite;
	public Sprite sandSprite;
	public Sprite grassSprite;
	public Sprite dirtSprite;
	public Sprite stoneSprite;
	public Sprite snowSprite;

	private Grid _grid;

	private void Awake()
	{
		_grid = GetComponent<Grid>();
	}

	private void Start()
	{
		Generate();
	}

	public void Generate()
	{
		for (int i = transform.childCount - 1; i >= 0; i--)
			DestroyImmediate(transform.GetChild(i).gameObject);

		string forcedTile = System.Environment.GetEnvironmentVariable("IGU_FORCE_TILE");
		string sizeOverride = System.Environment.GetEnvironmentVariable("IGU_GRID_SIZE");
		int w = gridWidth, h = gridHeight;
		if (!string.IsNullOrEmpty(sizeOverride))
			w = h = int.Parse(sizeOverride);

		for (int x = 0; x < w; x++)
		{
			for (int y = 0; y < h; y++)
			{
				Sprite sprite = string.IsNullOrEmpty(forcedTile)
					? SpriteFor(Elevation(x, y))
					: ForcedSprite(forcedTile);
				SpawnTile(x, y, sprite);
			}
		}
	}

	private void SpawnTile(int x, int y, Sprite sprite)
	{
		var go = new GameObject($"Tile_{x}_{y}");
		go.transform.SetParent(transform);
		go.transform.position = _grid.CellToWorld(new Vector3Int(x, y, 0));

		var renderer = go.AddComponent<SpriteRenderer>();
		renderer.sprite = sprite;
		// Negative: a tile's bottom skirt only gets covered by a
		// SMALLER-sum neighbor's render range, not a larger one (checked by
		// direct calculation -- a tile's rendered vertical span is
		// [worldY - 0.75*spriteHeight, worldY + 0.25*spriteHeight], and only
		// the smaller-sum neighbor's span reaches down to this tile's tip).
		// Getting this backwards (larger sum on top) left small fragments of
		// farther-back tiles visible at ground-type transition edges, since
		// same-type neighbors hide the same mistake by looking identical.
		// Scaled by 100 to leave room for the player's finer-grained,
		// continuous sortingOrder (see PlayerMovement.UpdateSortingOrder)
		// to interleave between rows without exactly tying a tile's order.
		renderer.sortingOrder = -(x + y) * 100;
	}

	private Sprite ForcedSprite(string name) => name switch
	{
		"water" => waterSprite,
		"sand" => sandSprite,
		"dirt" => dirtSprite,
		"stone" => stoneSprite,
		"snow" => snowSprite,
		_ => grassSprite,
	};

	private float Elevation(int x, int y)
	{
		return Mathf.PerlinNoise(
			(x + noiseSeedOffset) * noiseFrequency,
			(y + noiseSeedOffset) * noiseFrequency) - 0.5f;
	}

	// Elevation-based biome bands: water in the low spots, snow on the peaks.
	private Sprite SpriteFor(float elevation)
	{
		if (elevation < -0.22f) return waterSprite;
		if (elevation < -0.12f) return sandSprite;
		if (elevation < 0.08f) return grassSprite;
		if (elevation < 0.18f) return dirtSprite;
		if (elevation < 0.28f) return stoneSprite;
		return snowSprite;
	}
}
