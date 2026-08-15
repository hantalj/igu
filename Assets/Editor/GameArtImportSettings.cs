using UnityEditor;
using UnityEngine;

/// <summary>
/// Configures import settings for the game's art automatically, so sprite
/// slicing/pivots/PPU don't have to be set by hand in the editor GUI.
///
/// Both tiles and the knight character share the same Pixels Per Unit (111,
/// the tile art's pixel width) so 1 world unit == 1 tile-width, and both use
/// a custom pivot placed at the "ground anchor" point of the art rather than
/// the sprite's geometric center:
///   - Tiles (111x128): the top face is a 111x64 diamond occupying the top
///     half of the image, with a 64px visible "skirt" below it for the
///     cube's front faces. The diamond's own center (the point that should
///     land on the Tilemap's grid cell) is at pixel (55.5, 32) from the
///     top-left, i.e. pivot (0.5, 0.75) in Unity's bottom-left-origin,
///     normalized convention.
///   - Knight frames (256x256): the character's feet sit at approximately
///     pixel y=138 from the top of the canvas (measured across the Walk/Idle
///     sheets), i.e. pivot (0.5, 0.461).
/// </summary>
public class GameArtImportSettings : AssetPostprocessor
{
	private const string TilesPath = "Assets/Art/Tiles/";
	private const string KnightPath = "Assets/Art/Knight/";

	private const float PixelsPerUnit = 111f;
	private const int KnightCellSize = 256;

	private static readonly Vector2 TilePivot = new Vector2(0.5f, 0.75f);
	private static readonly Vector2 KnightPivot = new Vector2(0.5f, 0.461f);

	private void OnPreprocessTexture()
	{
		if (assetPath.StartsWith(TilesPath))
		{
			ConfigureCommon();
			var importer = (TextureImporter)assetImporter;
			importer.spriteImportMode = SpriteImportMode.Single;

			var settings = new TextureImporterSettings();
			importer.ReadTextureSettings(settings);
			settings.spriteAlignment = (int)SpriteAlignment.Custom;
			settings.spritePivot = TilePivot;
			settings.spriteMeshType = SpriteMeshType.FullRect;
			importer.SetTextureSettings(settings);
		}
		else if (assetPath.StartsWith(KnightPath))
		{
			ConfigureCommon();
			var importer = (TextureImporter)assetImporter;
			importer.spriteImportMode = SpriteImportMode.Multiple;
			importer.spritesheet = SliceGrid(assetPath);
		}
	}

	private void ConfigureCommon()
	{
		var importer = (TextureImporter)assetImporter;
		importer.textureType = TextureImporterType.Sprite;
		importer.spritePixelsPerUnit = PixelsPerUnit;
		importer.filterMode = FilterMode.Point;
		importer.mipmapEnabled = false;
		importer.wrapMode = TextureWrapMode.Clamp;
		importer.alphaIsTransparency = true;
		importer.textureCompression = TextureImporterCompression.Uncompressed;
	}

	private SpriteMetaData[] SliceGrid(string path)
	{
		var importer = (TextureImporter)assetImporter;
		importer.GetSourceTextureWidthAndHeight(out int width, out int height);

		int cols = width / KnightCellSize;
		int rows = height / KnightCellSize;
		string baseName = System.IO.Path.GetFileNameWithoutExtension(path);

		var frames = new SpriteMetaData[cols * rows];
		for (int row = 0; row < rows; row++)
		{
			for (int col = 0; col < cols; col++)
			{
				// Row 0 is the top row of the source image, but Unity's rect
				// origin is bottom-left, so flip vertically here.
				int frameIndex = row * cols + col;
				float rectY = height - (row + 1) * KnightCellSize;

				frames[frameIndex] = new SpriteMetaData
				{
					name = $"{baseName}_{frameIndex}",
					rect = new Rect(col * KnightCellSize, rectY, KnightCellSize, KnightCellSize),
					alignment = (int)SpriteAlignment.Custom,
					pivot = KnightPivot,
				};
			}
		}
		return frames;
	}
}
