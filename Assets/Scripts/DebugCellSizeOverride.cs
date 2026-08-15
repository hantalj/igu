using System;
using System.Globalization;
using UnityEngine;

/// <summary>
/// Debug-only: overrides the Grid's cell size from env vars at runtime, so
/// spacing can be probed without rebuilding the scene. Inert unless set.
/// </summary>
[RequireComponent(typeof(Grid))]
public class DebugCellSizeOverride : MonoBehaviour
{
	private void Awake()
	{
		string x = Environment.GetEnvironmentVariable("IGU_CELL_SIZE_X");
		string y = Environment.GetEnvironmentVariable("IGU_CELL_SIZE_Y");
		if (string.IsNullOrEmpty(x) && string.IsNullOrEmpty(y))
			return;

		var grid = GetComponent<Grid>();
		Vector3 size = grid.cellSize;
		if (!string.IsNullOrEmpty(x)) size.x = float.Parse(x, CultureInfo.InvariantCulture);
		if (!string.IsNullOrEmpty(y)) size.y = float.Parse(y, CultureInfo.InvariantCulture);
		grid.cellSize = size;
		Debug.Log($"DebugCellSizeOverride: cellSize={size}");

		if (Environment.GetEnvironmentVariable("IGU_PROBE_CELLS") == "1")
		{
			foreach (var cell in new[] { new Vector3Int(0, 0, 0), new Vector3Int(1, 0, 0), new Vector3Int(0, 1, 0), new Vector3Int(2, 0, 0), new Vector3Int(1, 1, 0) })
				Debug.Log($"CellToWorld{cell} = {grid.CellToWorld(cell)}");
		}
	}
}
