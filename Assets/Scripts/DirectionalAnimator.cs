using UnityEngine;

/// <summary>
/// Swaps a SpriteRenderer's sprite across pre-sliced frame arrays for 8
/// compass directions x {walk, idle}, mirroring the AtlasTexture/SpriteFrames
/// approach from the Godot version's Player.cs -- a lightweight custom
/// component instead of a full AnimatorController state machine.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class DirectionalAnimator : MonoBehaviour
{
	[System.Serializable]
	public class DirectionalClip
	{
		public Sprite[] frames;
		public float fps = 10f;
	}

	// Index 0 = dir1 ... index 7 = dir8, matching the asset's naming.
	public DirectionalClip[] walkClips = new DirectionalClip[8];
	public DirectionalClip[] idleClips = new DirectionalClip[8];

	private SpriteRenderer _renderer;
	private int _facingDir = 8; // default: facing down, toward the camera
	private bool _walking;
	private float _frameTimer;
	private int _frameIndex;

	private void Awake()
	{
		_renderer = GetComponent<SpriteRenderer>();
	}

	private void Start()
	{
		ApplyFrame();
	}

	public void SetState(int facingDir, bool walking)
	{
		if (facingDir == _facingDir && walking == _walking)
			return;

		_facingDir = facingDir;
		_walking = walking;
		_frameIndex = 0;
		_frameTimer = 0f;
		ApplyFrame();
	}

	private void Update()
	{
		DirectionalClip clip = CurrentClip();
		if (clip == null || clip.frames == null || clip.frames.Length == 0)
			return;

		_frameTimer += Time.deltaTime;
		float frameDuration = 1f / clip.fps;
		if (_frameTimer < frameDuration)
			return;

		_frameTimer -= frameDuration;
		_frameIndex = (_frameIndex + 1) % clip.frames.Length;
		ApplyFrame();
	}

	private void ApplyFrame()
	{
		DirectionalClip clip = CurrentClip();
		if (clip != null && clip.frames != null && clip.frames.Length > 0)
			_renderer.sprite = clip.frames[_frameIndex % clip.frames.Length];
	}

	private DirectionalClip CurrentClip() => (_walking ? walkClips : idleClips)[_facingDir - 1];
}
