using UnityEngine;

/// <summary>
/// Once gameplay has started (Main scene, reached via either Start New Game
/// or Start Demo), pressing Escape exits the game entirely. The main menu
/// scene has no equivalent shortcut -- it already has its own Exit Igu
/// button, and an accidental Escape-quit before the player has even chosen
/// what to play would be surprising.
/// </summary>
public class QuitOnEscape : MonoBehaviour
{
	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Escape))
			GameExit.Quit();
	}
}
