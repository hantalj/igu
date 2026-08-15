using UnityEngine;

public class DebugSceneDump : MonoBehaviour
{
	private int _frame;

	private void Update()
	{
		if (System.Environment.GetEnvironmentVariable("IGU_DUMP_SCENE") != "1")
			return;

		_frame++;
		if (_frame != 1 && _frame != 55)
			return;

		Debug.Log($"[frame {_frame}] Screen: {Screen.width}x{Screen.height}");

		foreach (var cam in Camera.allCameras)
			Debug.Log($"[frame {_frame}] Camera '{cam.name}': pos={cam.transform.position} rot={cam.transform.rotation.eulerAngles} " +
				$"cullingMask={cam.cullingMask} nearClip={cam.nearClipPlane} farClip={cam.farClipPlane} depth={cam.depth} " +
				$"targetTexture={cam.targetTexture} enabled={cam.enabled} isActiveAndEnabled={cam.isActiveAndEnabled}");

		foreach (var sr in FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Exclude))
		{
			if (sr.gameObject.name.StartsWith("Tile_"))
				continue; // skip the 256 terrain tiles, too noisy
			Debug.Log($"[frame {_frame}] SpriteRenderer '{sr.gameObject.name}' layer={sr.gameObject.layer} pos={sr.transform.position} " +
				$"worldPos={sr.transform.position} sprite={sr.sprite} enabled={sr.enabled} sortingOrder={sr.sortingOrder} " +
				$"sortingLayerID={sr.sortingLayerID} color={sr.color} isVisible={sr.isVisible} bounds={sr.bounds}");
		}
	}
}
