using UnityEngine;

/// <summary>
/// Keeps the camera positioned over a target without parenting the camera
/// under it -- parenting the camera directly under the Player caused the
/// Player's own SpriteRenderer to stop rendering entirely (confirmed by a
/// standalone marker object at the same world position rendering fine, and
/// the Player working again once un-parented from its camera; root cause
/// not fully diagnosed, but reproducible and this avoids it).
/// </summary>
public class CameraFollow : MonoBehaviour
{
	public Transform target;
	public Vector3 offset = new Vector3(0f, 0f, -10f);

	private void LateUpdate()
	{
		if (target != null)
			transform.position = target.position + offset;
	}
}
