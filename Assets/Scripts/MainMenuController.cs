using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Wires the three main menu buttons and handles the "skip the menu, go
/// straight to the demo" bypass used for quick manual testing and for the
/// verification screenshots (mirrors AutoScreenshot/DebugSceneDump's
/// env-var-gated pattern elsewhere in the project).
/// </summary>
public class MainMenuController : MonoBehaviour
{
	private const string GameSceneName = "Main";

	private void Start()
	{
		if (ShouldBypassMenu())
		{
			StartDemo();
			return;
		}

		// Verification-only: lets a headless test build trigger a button's
		// action directly, since there's no mouse to click with. Mirrors
		// PlayerMovement's IGU_TEST_KEY pattern.
		switch (System.Environment.GetEnvironmentVariable("IGU_TEST_ACTION"))
		{
			case "newgame": StartNewGame(); break;
			case "demo": StartDemo(); break;
			case "exit": ExitGame(); break;
		}
	}

	public void StartNewGame()
	{
		GameSession.NoiseSeedOverride = Random.Range(0f, 100000f);
		SceneManager.LoadScene(GameSceneName);
	}

	public void StartDemo()
	{
		GameSession.NoiseSeedOverride = null; // TerrainGenerator's own fixed seed
		SceneManager.LoadScene(GameSceneName);
	}

	public void ExitGame()
	{
		Application.Quit();
#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying = false;
#endif
	}

	private static bool ShouldBypassMenu()
	{
		if (System.Environment.GetEnvironmentVariable("IGU_SKIP_MENU") == "1")
			return true;

		foreach (string arg in System.Environment.GetCommandLineArgs())
			if (arg == "-skipmenu")
				return true;

		return false;
	}
}
