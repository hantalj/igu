/// <summary>
/// Tiny static bridge between MainMenuController and TerrainGenerator: set
/// before loading the game scene, read once by TerrainGenerator on Awake.
/// Not a MonoBehaviour, so it survives the scene load without needing
/// DontDestroyOnLoad or a persistent GameObject.
/// </summary>
public static class GameSession
{
	/// <summary>
	/// Null = use TerrainGenerator's own default seed (the fixed "demo"
	/// terrain). Set to a value = "New Game" mode, a freshly rolled seed.
	/// </summary>
	public static float? NoiseSeedOverride;
}
