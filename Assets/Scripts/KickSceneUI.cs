using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

[ExecuteAlways]
[RequireComponent(typeof(Canvas))]
public class KickSceneUI : MonoBehaviour
{
    [SerializeField] private PlayerKickController playerKickController;
    [SerializeField] private Button kickButton;
    [SerializeField] private Button autoKickButton;
    [SerializeField] private Button resetButton;

    private Image autoKickImage;
    private Text autoKickText;
    private bool lastAutoKickState;
    private bool initialized;

    private void OnEnable()
    {
        EnsureSceneUI();
    }

    private void Awake()
    {
        EnsureSceneUI();
    }

    private void Start()
    {
        BindGameplay();
        UpdateAutoKickAppearance();
    }

    private void Update()
    {
        BindGameplay();
        if (playerKickController == null)
        {
            return;
        }

        if (kickButton != null && kickButton.gameObject.activeSelf != playerKickController.HasNearbyBall)
        {
            kickButton.gameObject.SetActive(playerKickController.HasNearbyBall);
        }

        if (autoKickButton != null && lastAutoKickState != playerKickController.AutoKickEnabled)
        {
            UpdateAutoKickAppearance();
        }
    }

    private void EnsureSceneUI()
    {
        Canvas canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = gameObject.AddComponent<CanvasScaler>();
        }
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        if (GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }

        if (playerKickController == null)
        {
            playerKickController = FindObjectOfType<PlayerKickController>();
        }

        kickButton = FindButton("Kick Button") ?? CreateButton(
            "Kick Button", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f), new Vector2(0f, 35f), new Vector2(270f, 76f),
            new Color(0.08f, 0.32f, 0.68f, 0.96f), "KICK", 30);
        autoKickButton = FindButton("Auto Kick Button") ?? CreateButton(
            "Auto Kick Button", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(250f, 68f),
            new Color(0.68f, 0.12f, 0.1f, 0.96f), "AUTO KICK: OFF", 26);
        resetButton = FindButton("Reset Button") ?? CreateButton(
            "Reset Button", new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(1f, 1f), new Vector2(-24f, -24f), new Vector2(180f, 68f),
            new Color(0.68f, 0.12f, 0.1f, 0.96f), "RESET", 28);

        autoKickImage = autoKickButton.GetComponent<Image>();
        autoKickText = autoKickButton.GetComponentInChildren<Text>();

        if (!Application.isPlaying)
        {
            kickButton.gameObject.SetActive(false);
            MarkSceneDirty();
            return;
        }

        if (!initialized)
        {
            initialized = true;
            BindGameplay();
            kickButton.onClick.AddListener(OnKickClicked);
            autoKickButton.onClick.AddListener(OnAutoKickClicked);
            resetButton.onClick.AddListener(ResetCurrentScene);
            UpdateAutoKickAppearance();
        }

        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }
    }

    private Button FindButton(string buttonName)
    {
        Transform buttonTransform = transform.Find(buttonName);
        return buttonTransform != null ? buttonTransform.GetComponent<Button>() : null;
    }

    private Button CreateButton(
        string objectName,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Vector2 size,
        Color color,
        string labelText,
        int fontSize)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(transform, false);

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = anchorMin;
        buttonRect.anchorMax = anchorMax;
        buttonRect.pivot = pivot;
        buttonRect.anchoredPosition = anchoredPosition;
        buttonRect.sizeDelta = size;

        Image image = buttonObject.GetComponent<Image>();
        image.color = color;
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;

        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        labelObject.transform.SetParent(buttonObject.transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        Text label = labelObject.GetComponent<Text>();
        label.text = labelText;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.fontSize = fontSize;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.raycastTarget = false;

        return button;
    }

    private void BindGameplay()
    {
        if (playerKickController == null)
        {
            playerKickController = FindObjectOfType<PlayerKickController>();
        }
    }

    private void OnKickClicked()
    {
        if (playerKickController != null)
        {
            playerKickController.KickNearestBall();
        }
    }

    private void OnAutoKickClicked()
    {
        if (playerKickController != null)
        {
            playerKickController.ToggleAutoKick();
            UpdateAutoKickAppearance();
        }
    }

    private void UpdateAutoKickAppearance()
    {
        if (autoKickImage == null || autoKickText == null || playerKickController == null)
        {
            return;
        }

        bool enabled = playerKickController.AutoKickEnabled;
        lastAutoKickState = enabled;
        autoKickImage.color = enabled
            ? new Color(0.1f, 0.62f, 0.22f, 0.96f)
            : new Color(0.68f, 0.12f, 0.1f, 0.96f);
        autoKickText.text = enabled ? "AUTO KICK: ON" : "AUTO KICK: OFF";
    }

    private static void ResetCurrentScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void MarkSceneDirty()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying && gameObject.scene.IsValid())
        {
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
#endif
    }
}
