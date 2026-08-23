using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Builds the MainMenu scene from code, same rationale as SceneBuilder: a
/// hand-authored Unity UI hierarchy in YAML is error-prone, so construct it
/// via the Editor API and save the result.
/// </summary>
public static class MainMenuBuilder
{
	private const string ScenePath = "Assets/Scenes/MainMenu.unity";
	private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

	private static readonly Color GoldText = new Color(0.933f, 0.871f, 0.706f);
	private static readonly Color DarkOutline = new Color(0.08f, 0.05f, 0.03f, 0.9f);

	[MenuItem("Tools/Build Main Menu Scene")]
	public static void BuildMainMenuScene()
	{
		var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

		BuildCamera();
		Canvas canvas = BuildCanvas();
		BuildEventSystem();
		BuildBackground(canvas.transform);
		BuildTitle(canvas.transform);

		Sprite buttonSprite = LoadUiSprite("button_normal");
		RectTransform newGameBtn = BuildButton(canvas.transform, buttonSprite, "NewGameButton",
			"START NEW GAME", new Vector2(0.5f, 0.5f), new Vector2(0f, 70f));
		RectTransform demoBtn = BuildButton(canvas.transform, buttonSprite, "DemoButton",
			"START DEMO", new Vector2(0.5f, 0.5f), new Vector2(0f, -28f));
		RectTransform exitBtn = BuildButton(canvas.transform, buttonSprite, "ExitButton",
			"EXIT IGU", new Vector2(0.5f, 0f), new Vector2(0f, 90f));

		var controllerGo = new GameObject("MainMenuController");
		var controller = controllerGo.AddComponent<MainMenuController>();
		// UnityEventTools.AddPersistentListener (not onClick.AddListener) --
		// AddListener only registers a *runtime* listener. Called from this
		// editor script, that registration lives in the transient
		// scene-building process's memory and is never written into the
		// saved .unity file, so none of the three buttons actually did
		// anything when clicked in the built game despite compiling and
		// "working" under the IGU_TEST_ACTION hook, which invokes the C#
		// methods directly and so never exercised the click wiring at all.
		// AddPersistentListener is the editor-scripting equivalent of
		// wiring the listener by hand in the Inspector, and does serialize.
		UnityEventTools.AddPersistentListener(newGameBtn.GetComponent<Button>().onClick, controller.StartNewGame);
		UnityEventTools.AddPersistentListener(demoBtn.GetComponent<Button>().onClick, controller.StartDemo);
		UnityEventTools.AddPersistentListener(exitBtn.GetComponent<Button>().onClick, controller.ExitGame);

		EditorSceneManager.SaveScene(scene, ScenePath);
		Debug.Log($"Saved scene to {ScenePath}");
	}

	private static void BuildCamera()
	{
		var cameraGo = new GameObject("Main Camera");
		cameraGo.tag = "MainCamera";
		var camera = cameraGo.AddComponent<Camera>();
		camera.orthographic = true;
		camera.clearFlags = CameraClearFlags.SolidColor;
		camera.backgroundColor = new Color(0.05f, 0.05f, 0.06f);
		cameraGo.transform.position = new Vector3(0f, 0f, -10f);
		cameraGo.AddComponent<AudioListener>();
		cameraGo.AddComponent<AutoScreenshot>();
	}

	private static Canvas BuildCanvas()
	{
		var canvasGo = new GameObject("Canvas");
		var canvas = canvasGo.AddComponent<Canvas>();
		canvas.renderMode = RenderMode.ScreenSpaceOverlay;

		var scaler = canvasGo.AddComponent<CanvasScaler>();
		scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		scaler.referenceResolution = ReferenceResolution;
		scaler.matchWidthOrHeight = 0.5f;

		canvasGo.AddComponent<GraphicRaycaster>();
		return canvas;
	}

	private static void BuildEventSystem()
	{
		var esGo = new GameObject("EventSystem");
		esGo.AddComponent<EventSystem>();
		esGo.AddComponent<StandaloneInputModule>();
	}

	private static void BuildBackground(Transform canvasTransform)
	{
		var go = new GameObject("Background");
		go.transform.SetParent(canvasTransform, false);
		var rect = go.AddComponent<RectTransform>();
		rect.anchorMin = Vector2.zero;
		rect.anchorMax = Vector2.one;
		rect.offsetMin = Vector2.zero;
		rect.offsetMax = Vector2.zero;

		var image = go.AddComponent<Image>();
		image.sprite = LoadUiSprite("menu_background");
		image.type = Image.Type.Simple;
		image.preserveAspect = false;
	}

	private static void BuildTitle(Transform canvasTransform)
	{
		var go = new GameObject("Title");
		go.transform.SetParent(canvasTransform, false);
		var image = go.AddComponent<Image>();
		image.sprite = LoadUiSprite("title_igu");
		image.preserveAspect = true;

		var rect = go.GetComponent<RectTransform>();
		rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
		rect.pivot = new Vector2(0.5f, 1f);
		rect.anchoredPosition = new Vector2(0f, -50f);
		rect.sizeDelta = new Vector2(620f, 234f); // matches title_igu's 900x340 aspect
	}

	private static RectTransform BuildButton(Transform canvasTransform, Sprite sprite, string name,
		string label, Vector2 anchor, Vector2 anchoredPosition)
	{
		var go = new GameObject(name);
		go.transform.SetParent(canvasTransform, false);

		var rect = go.AddComponent<RectTransform>();
		rect.anchorMin = rect.anchorMax = anchor;
		rect.pivot = new Vector2(0.5f, 0.5f);
		rect.sizeDelta = new Vector2(520f, 82f);
		rect.anchoredPosition = anchoredPosition;

		var image = go.AddComponent<Image>();
		image.sprite = sprite;
		image.type = Image.Type.Sliced; // stretch the bar without distorting the rounded ends
		image.pixelsPerUnitMultiplier = 1f;

		var button = go.AddComponent<Button>();
		button.targetGraphic = image;
		var colors = button.colors;
		colors.highlightedColor = new Color(1.15f, 1.1f, 0.95f);
		colors.pressedColor = new Color(0.85f, 0.8f, 0.7f);
		button.colors = colors;

		var textGo = new GameObject("Label");
		textGo.transform.SetParent(go.transform, false);
		var textRect = textGo.AddComponent<RectTransform>();
		textRect.anchorMin = Vector2.zero;
		textRect.anchorMax = Vector2.one;
		textRect.offsetMin = Vector2.zero;
		textRect.offsetMax = Vector2.zero;

		var text = textGo.AddComponent<Text>();
		text.text = label;
		text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
		text.fontStyle = FontStyle.Bold;
		text.fontSize = 30;
		text.alignment = TextAnchor.MiddleCenter;
		text.color = GoldText;

		var outline = textGo.AddComponent<Outline>();
		outline.effectColor = DarkOutline;
		outline.effectDistance = new Vector2(1.5f, -1.5f);

		return rect;
	}

	// Slicing needs a border set on the sprite so the rounded ends and gems
	// don't stretch -- set once here rather than requiring manual Sprite
	// Editor steps.
	private static Sprite LoadUiSprite(string name)
	{
		string path = $"Assets/Art/UI/{name}.png";
		var importer = (TextureImporter)AssetImporter.GetAtPath(path);
		if (importer != null && name == "button_normal")
		{
			importer.spriteBorder = new Vector4(40, 30, 40, 30);
			importer.SaveAndReimport();
		}
		return AssetDatabase.LoadAssetAtPath<Sprite>(path);
	}
}
