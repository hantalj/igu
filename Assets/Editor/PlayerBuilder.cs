using System.Linq;
using UnityEditor;
using UnityEngine;

public static class PlayerBuilder
{
	private const int WalkFrameCount = 11;
	private const int IdleFrameCount = 17;

	public static GameObject Build()
	{
		var go = new GameObject("Player");
		// Grid-center-ish starting position, converted through the same
		// isometric formula as TerrainGenerator (world != raw grid index).
		const float cellSizeY = 64f / 111f;
		const int gx = 8, gy = 8;
		go.transform.position = new Vector3((gx - gy) * 0.5f, (gx + gy) * cellSizeY * 0.5f, 0f);

		go.AddComponent<SpriteRenderer>();

		var animator = go.AddComponent<DirectionalAnimator>();
		for (int dir = 1; dir <= 8; dir++)
		{
			animator.walkClips[dir - 1] = new DirectionalAnimator.DirectionalClip
			{
				frames = LoadFrames($"Assets/Art/Knight/Walk/Knight_Walk_dir{dir}.png", WalkFrameCount),
				fps = 10f,
			};
			animator.idleClips[dir - 1] = new DirectionalAnimator.DirectionalClip
			{
				frames = LoadFrames($"Assets/Art/Knight/Idle/Knight_Idle_dir{dir}.png", IdleFrameCount),
				fps = 7f,
			};
		}

		go.AddComponent<PlayerMovement>();

		var cameraGo = new GameObject("Player Camera");
		cameraGo.tag = "MainCamera";
		var camera = cameraGo.AddComponent<Camera>();
		camera.orthographic = true;
		camera.orthographicSize = 6f;
		camera.clearFlags = CameraClearFlags.SolidColor;
		camera.backgroundColor = new Color(0.15f, 0.15f, 0.18f);
		cameraGo.AddComponent<AudioListener>();
		cameraGo.AddComponent<AutoScreenshot>();
		cameraGo.AddComponent<DebugSceneDump>();
		var follow = cameraGo.AddComponent<CameraFollow>();
		follow.target = go.transform;
		follow.offset = new Vector3(0f, 0f, -10f);
		cameraGo.transform.position = go.transform.position + follow.offset;

		return go;
	}

	private static Sprite[] LoadFrames(string path, int frameCount)
	{
		Sprite[] all = AssetDatabase.LoadAllAssetsAtPath(path)
			.OfType<Sprite>()
			.OrderBy(s => ExtractIndex(s.name))
			.ToArray();

		if (all.Length < frameCount)
			Debug.LogError($"PlayerBuilder: expected {frameCount} frames at {path}, found {all.Length}");

		return all.Take(frameCount).ToArray();
	}

	private static int ExtractIndex(string spriteName)
	{
		int underscore = spriteName.LastIndexOf('_');
		return int.Parse(spriteName.Substring(underscore + 1));
	}
}
