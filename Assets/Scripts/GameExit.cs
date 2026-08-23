using UnityEngine;

/// <summary>
/// Shared "quit the whole application" logic, used by both the main menu's
/// Exit Igu button and QuitOnEscape in the game/demo scene, so the two
/// don't drift out of sync.
/// </summary>
public static class GameExit
{
	public static void Quit()
	{
		Application.Quit();
#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying = false;
#endif
	}
}
