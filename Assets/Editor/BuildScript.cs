using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildScript
{
	[MenuItem("Tools/Build Mac Standalone")]
	public static void BuildMacStandalone()
	{
		// MSAA blends sprite edge pixels with whatever is behind them,
		// which looks like the wrong tile "bleeding through" at edges for
		// pixel art with this much sprite overlap. Off for crisp edges.
		QualitySettings.antiAliasing = 0;

		string outputPath = Environment.GetEnvironmentVariable("IGU_BUILD_PATH");
		if (string.IsNullOrEmpty(outputPath))
			outputPath = "Builds/igu.app";

		var options = new BuildPlayerOptions
		{
			scenes = new[] { "Assets/Scenes/Main.unity" },
			locationPathName = outputPath,
			target = BuildTarget.StandaloneOSX,
			options = BuildOptions.None,
		};

		BuildReport report = BuildPipeline.BuildPlayer(options);
		Debug.Log($"Build result: {report.summary.result}, size: {report.summary.totalSize}, errors: {report.summary.totalErrors}");

		if (report.summary.result != BuildResult.Succeeded)
			EditorApplication.Exit(1);
	}
}
