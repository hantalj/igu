using UnityEngine;
using UnityEngine.EventSystems;
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

		// Verification-only: simulates a real click through Unity's actual
		// UI event system (IPointerClickHandler), exercising the Button
		// component and its serialized onClick listeners exactly like a
		// physical click would -- unlike an earlier version of this hook
		// that called the controller method directly, which "passed" even
		// when the buttons' click wiring was completely broken (see
		// MainMenuBuilder's AddPersistentListener comment).
		string targetButton = System.Environment.GetEnvironmentVariable("IGU_TEST_CLICK");
		if (!string.IsNullOrEmpty(targetButton))
			SimulateClick(targetButton);
	}

	private static void SimulateClick(string buttonName)
	{
		var buttonGo = GameObject.Find(buttonName);
		if (buttonGo == null)
		{
			Debug.LogError($"IGU_TEST_CLICK: no GameObject named '{buttonName}' found");
			return;
		}
		ExecuteEvents.Execute(buttonGo, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
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

	public void ExitGame() => GameExit.Quit();

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
