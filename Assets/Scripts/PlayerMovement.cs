using UnityEngine;

/// <summary>
/// Ports igrunner's Player.cs: WASD/arrow input projected onto the
/// isometric axes, 8-compass-direction facing, and depth-sorting against
/// the ground tiles.
///
/// The isometric projection formula is the same as the Godot version, but
/// with the Y term negated -- Unity's Y+ is screen-up, while Godot's Y+ is
/// screen-down, so a straight port would move the character in mirrored
/// vertical directions. Verified by re-deriving both the movement and the
/// 8-direction compass sector mapping from scratch for Unity's convention
/// and cross-checking specific inputs (e.g. pure D, pure S) against the
/// already-confirmed Godot behavior before trusting it.
/// </summary>
[RequireComponent(typeof(DirectionalAnimator))]
public class PlayerMovement : MonoBehaviour
{
	public float speed = 3f;

	// Must match TerrainGenerator/SceneBuilder's isometric cell size.
	public float cellSizeY = 64f / 111f;

	// Compass order for atan2 in Unity's Y-up space, starting at 0deg=Right,
	// going counter-clockwise in 45-degree sectors.
	private static readonly int[] SectorToDir = { 6, 5, 4, 3, 2, 1, 8, 7 };

	private DirectionalAnimator _animator;
	private SpriteRenderer _renderer;
	private int _facingDir = 8; // default: facing down, toward the camera

	private void Awake()
	{
		_animator = GetComponent<DirectionalAnimator>();
		_renderer = GetComponent<SpriteRenderer>();
	}

	// Verification-only: lets a headless test build simulate a held key via
	// env var, since the legacy Input class can't be scripted from outside.
	private string _testKey;

	private void Start()
	{
		_testKey = System.Environment.GetEnvironmentVariable("IGU_TEST_KEY");
	}

	private void Update()
	{
		float ix = 0f, iy = 0f; // iy follows Godot's convention: W=-1 (up), S=+1 (down)
		if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) || _testKey == "A") ix -= 1f;
		if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) || _testKey == "D") ix += 1f;
		if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) || _testKey == "W") iy -= 1f;
		if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) || _testKey == "S") iy += 1f;

		Vector2 inputDir = new Vector2(ix, iy);
		if (inputDir.sqrMagnitude > 1f)
			inputDir.Normalize();

		// Project screen-space input onto the isometric (2:1 diamond) axes.
		Vector2 isoDir = new Vector2(
			inputDir.x - inputDir.y,
			-(inputDir.x + inputDir.y) * 0.5f);
		if (isoDir.sqrMagnitude > 1f)
			isoDir.Normalize();

		transform.position += (Vector3)(isoDir * speed * Time.deltaTime);

		UpdateAnimation(isoDir);
		UpdateSortingOrder();
	}

	private void UpdateAnimation(Vector2 isoDir)
	{
		bool moving = isoDir.sqrMagnitude > 0.0001f;
		if (moving)
			_facingDir = DirectionFromVector(isoDir);
		_animator.SetState(_facingDir, moving);
	}

	// SpriteRenderer.sortingOrder is backed by an Int16 (max 32767) even
	// though the C# property type is int -- a naive "+100000" bias silently
	// wrapped around to a large *negative* value, which put the player far
	// *behind* the terrain instead of in front, making it fully invisible
	// with no error anywhere. This offset is comfortably below that limit
	// and well above the terrain's own magnitude ((15+15)*100 = 3000), so
	// the player always draws on top of the ground. It's a deliberate
	// simplification: the terrain has no tall structures yet that should
	// occlude the player, so "always on top of ground" is correct for now.
	private const int PlayerSortingBias = 20000;

	// Negated to match TerrainGenerator's sortingOrder convention (smaller
	// grid-sum = drawn later/on top -- see the comment there for why), so
	// this stays consistent if PlayerSortingBias is ever reduced for real
	// interleaving with terrain instead of always drawing on top of it.
	private void UpdateSortingOrder()
	{
		float virtualGridSum = 2f * transform.position.y / cellSizeY;
		_renderer.sortingOrder = -Mathf.RoundToInt(virtualGridSum * 100f) + PlayerSortingBias;
	}

	private static int DirectionFromVector(Vector2 dir)
	{
		float angleDeg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
		if (angleDeg < 0f)
			angleDeg += 360f;
		int sector = Mathf.RoundToInt(angleDeg / 45f) % 8;
		return SectorToDir[sector];
	}
}
