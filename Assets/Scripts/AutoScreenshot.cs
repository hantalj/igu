using System;
using UnityEngine;

/// <summary>
/// Verification hook: inert unless IGU_SCREENSHOT_PATH is set in the
/// environment, in which case it waits a few frames (letting the scene
/// finish Start()), captures a screenshot, then quits. Mirrors the
/// IGRUNNER_SCREENSHOT hook used to verify the Godot version.
/// </summary>
public class AutoScreenshot : MonoBehaviour
{
	private int _framesUntilCapture = -1;
	private int _framesUntilQuit = -1;
	private string _path;

	private void Start()
	{
		_path = Environment.GetEnvironmentVariable("IGU_SCREENSHOT_PATH");
		if (string.IsNullOrEmpty(_path))
			return;

		string framesEnv = Environment.GetEnvironmentVariable("IGU_SCREENSHOT_FRAMES");
		_framesUntilCapture = string.IsNullOrEmpty(framesEnv) ? 30 : int.Parse(framesEnv);
	}

	private void Update()
	{
		if (_framesUntilCapture > 0)
		{
			_framesUntilCapture--;
			if (_framesUntilCapture == 0)
			{
				ScreenCapture.CaptureScreenshot(_path);
				_framesUntilQuit = 5;
			}
			return;
		}

		if (_framesUntilQuit > 0)
		{
			_framesUntilQuit--;
			if (_framesUntilQuit == 0)
				Application.Quit();
		}
	}
}
