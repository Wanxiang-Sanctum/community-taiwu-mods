using System.Collections;
using System.Diagnostics.CodeAnalysis;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Wanxiang.Taiwu.Logging;

namespace Wanxiang.Xiangshu.Frontend.Chat;

[SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "Unity constructs this MonoBehaviour through GameObject.AddComponent at runtime.")]
internal sealed class XiangshuChatWindow : MonoBehaviour
{
    private const string HeaderIconSprite = "map_icon_xiangshu";
    private const string AssistantBubbleSprite = "ui9_back_mousetip_base_npcthink_1";
    private const string UserBubbleSprite = "ui9_back_mousetip_base_npcthink_2";
    private const float PanelPreferredWidth = 620f;
    private const float PanelPreferredHeight = 720f;
    private const float PanelMinimumWidth = 460f;
    private const float PanelMinimumHeight = 520f;
    private const float PanelMargin = 32f;

    private static readonly TaiwuLogger Log = TaiwuLogger.ForTag("Wanxiang.Xiangshu");
    private static readonly Color PanelColor = new(0.055f, 0.049f, 0.041f, 0.97f);
    private static readonly Color PanelEdgeColor = new(0.42f, 0.25f, 0.13f, 0.9f);
    private static readonly Color HeaderColor = new(0.12f, 0.087f, 0.058f, 0.98f);
    private static readonly Color MessageAreaColor = new(0.035f, 0.033f, 0.031f, 0.72f);
    private static readonly Color InputColor = new(0.08f, 0.074f, 0.063f, 0.98f);
    private static readonly Color AssistantBubbleColor = new(0.18f, 0.15f, 0.11f, 0.96f);
    private static readonly Color UserBubbleColor = new(0.095f, 0.15f, 0.17f, 0.96f);
    private static readonly Color AccentColor = new(0.82f, 0.59f, 0.28f, 1f);
    private static readonly Color TextColor = new(0.92f, 0.88f, 0.78f, 1f);
    private static readonly Color MutedTextColor = new(0.67f, 0.62f, 0.52f, 1f);
    private static readonly Color ButtonColor = new(0.24f, 0.16f, 0.08f, 1f);
    private static readonly Color DisabledButtonColor = new(0.13f, 0.105f, 0.08f, 1f);

    private static TMP_FontAsset? s_gameFontAsset;
    private static Material? s_gameFontMaterial;
    private static TMP_SpriteAsset? s_gameSpriteAsset;

    private AgentChatSession? _session;
    private RectTransform? _panelRect;
    private RectTransform? _messageContent;
    private ScrollRect? _scrollRect;
    private DisableHotkeyInputField? _inputField;
    private Button? _sendButton;
    private CImage? _sendButtonImage;
    private TextMeshProUGUI? _sendButtonText;
    private bool _uiBuilt;
    private bool _scrollToBottom;

    public bool IsVisible { get; private set; }

    public static XiangshuChatWindow Create(AgentChatSession session)
    {
        GameObject root = new("Wanxiang.Xiangshu.ChatWindow", typeof(RectTransform));
        DontDestroyOnLoad(root);
        XiangshuChatWindow window = root.AddComponent<XiangshuChatWindow>();
        window.Initialize(session);
        return window;
    }

    public void Toggle()
    {
        SetVisible(!IsVisible);
    }

    public void SetVisible(bool visible)
    {
        if (visible && !EnsureUiBuilt())
        {
            return;
        }

        IsVisible = visible;
        gameObject.SetActive(visible);
        LogVisibilityChange(visible);

        if (!visible)
        {
            _inputField?.DeactivateInputField();
            return;
        }

        transform.SetAsLastSibling();
        DrainSessionEvents();
        _scrollToBottom = true;
        FocusInputField();
    }

    public void DestroyWindow()
    {
        Destroy(gameObject);
    }

    private void Initialize(AgentChatSession session)
    {
        _session = session;
        IsVisible = false;
        gameObject.SetActive(false);
    }

    [SuppressMessage(
        "CodeQuality",
        "IDE0051:Remove unused private members",
        Justification = "Unity invokes Update by method name.")]
    [SuppressMessage(
        "Roslynator",
        "RCS1213:Remove unused member declaration",
        Justification = "Unity invokes Update by method name.")]
    private void Update()
    {
        DrainSessionEvents();
        UpdateSendButtonState();

        if (_inputField?.isFocused == true
            && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            && !Input.GetKey(KeyCode.LeftShift)
            && !Input.GetKey(KeyCode.RightShift))
        {
            SendCurrentInput();
        }
    }

    [SuppressMessage(
        "CodeQuality",
        "IDE0051:Remove unused private members",
        Justification = "Unity invokes LateUpdate by method name.")]
    [SuppressMessage(
        "Roslynator",
        "RCS1213:Remove unused member declaration",
        Justification = "Unity invokes LateUpdate by method name.")]
    private void LateUpdate()
    {
        ApplyPanelLayout();

        if (!_scrollToBottom || _scrollRect is null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();
        _scrollRect.verticalNormalizedPosition = 0f;
        _scrollToBottom = false;
    }

    private void DrainSessionEvents()
    {
        if (_session is null)
        {
            return;
        }

        while (_session.TryDequeueEvent(out AgentChatSessionEvent sessionEvent))
        {
            if (sessionEvent.Kind == AgentChatSessionEventKind.MessageAdded
                && sessionEvent.Message is not null)
            {
                AddMessage(sessionEvent.Message);
            }
        }
    }

    private void SendCurrentInput()
    {
        if (_session is null || _inputField is null)
        {
            return;
        }

        string content = _inputField.text;

        if (string.IsNullOrWhiteSpace(content))
        {
            return;
        }

        _inputField.SetTextWithoutNotify(string.Empty);
        _session.SubmitUserMessage(content);
        UpdateSendButtonState();
        FocusInputField();
    }

    private void UpdateSendButtonState()
    {
        if (_sendButton is null || _inputField is null || _sendButtonImage is null)
        {
            return;
        }

        bool canSend = !string.IsNullOrWhiteSpace(_inputField.text);
        _sendButton.interactable = canSend;
        _sendButtonImage.color = canSend ? ButtonColor : DisabledButtonColor;

        SetSendButtonTextColor(canSend ? TextColor : MutedTextColor);
    }

    private void AddMessage(AgentChatMessage message)
    {
        if (_messageContent is null)
        {
            return;
        }

        bool isUser = message.Role == AgentChatRole.User;
        GameObject row = CreateChild("MessageRow", _messageContent);
        HorizontalLayoutGroup rowLayout = row.AddComponent<HorizontalLayoutGroup>();
        rowLayout.childAlignment = isUser ? TextAnchor.UpperRight : TextAnchor.UpperLeft;
        rowLayout.childControlHeight = true;
        rowLayout.childControlWidth = false;
        rowLayout.childForceExpandHeight = false;
        rowLayout.childForceExpandWidth = false;
        rowLayout.padding = new RectOffset(14, 14, 6, 6);
        _ = row.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        if (!isUser)
        {
            BuildMessageIcon(row.transform);
        }

        GameObject bubble = CreateChild(isUser ? "PlayerBubble" : "XiangshuBubble", row.transform);
        CImage bubbleImage = AddImage(
            bubble,
            isUser ? UserBubbleColor : AssistantBubbleColor,
            isUser ? UserBubbleSprite : AssistantBubbleSprite);
        bubbleImage.raycastTarget = false;
        VerticalLayoutGroup bubbleLayout = bubble.AddComponent<VerticalLayoutGroup>();
        bubbleLayout.childControlHeight = true;
        bubbleLayout.childControlWidth = true;
        bubbleLayout.childForceExpandHeight = false;
        bubbleLayout.childForceExpandWidth = true;
        bubbleLayout.padding = new RectOffset(14, 14, 10, 10);
        bubbleLayout.spacing = 5f;
        LayoutElement bubbleLayoutElement = bubble.AddComponent<LayoutElement>();
        bubbleLayoutElement.preferredWidth = 430f;
        _ = bubble.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        TextMeshProUGUI speaker = CreateText(
            isUser ? "玩家" : "相枢",
            bubble.transform,
            16f,
            isUser ? new Color(0.72f, 0.88f, 0.9f, 1f) : AccentColor,
            FontStyles.Bold);
        speaker.text = isUser ? "玩家" : "相枢";

        TextMeshProUGUI body = CreateText(
            "MessageText",
            bubble.transform,
            18f,
            TextColor,
            FontStyles.Normal);
        body.text = message.Content;

        LayoutRebuilder.ForceRebuildLayoutImmediate(_messageContent);
        _scrollToBottom = true;
    }

    private static void BuildMessageIcon(Transform parent)
    {
        GameObject iconObject = CreateChild("XiangshuIcon", parent);
        CImage icon = AddImage(iconObject, new Color(0.76f, 0.47f, 0.22f, 0.96f), HeaderIconSprite);
        icon.raycastTarget = false;
        LayoutElement layout = iconObject.AddComponent<LayoutElement>();
        layout.preferredWidth = 34f;
        layout.preferredHeight = 34f;
        layout.minWidth = 34f;
        layout.minHeight = 34f;
    }

    private void BuildUi(UIManager uiManager)
    {
        RectTransform rootRect = GetComponent<RectTransform>();
        StretchToParent(rootRect);

        GameObject panel = CreateChild("Panel", transform);
        EnsureInteractivePanel(panel, uiManager);
        _panelRect = panel.GetComponent<RectTransform>();
        _panelRect.anchorMin = new Vector2(1f, 0.5f);
        _panelRect.anchorMax = new Vector2(1f, 0.5f);
        _panelRect.pivot = new Vector2(1f, 0.5f);
        ApplyPanelLayout();
        _ = AddImage(panel, PanelColor);

        GameObject edge = CreateChild("Edge", panel.transform);
        RectTransform edgeRect = edge.GetComponent<RectTransform>();
        edgeRect.anchorMin = new Vector2(0f, 0f);
        edgeRect.anchorMax = new Vector2(0f, 1f);
        edgeRect.pivot = new Vector2(0f, 0.5f);
        edgeRect.sizeDelta = new Vector2(4f, 0f);
        edgeRect.anchoredPosition = Vector2.zero;
        CImage edgeImage = AddImage(edge, PanelEdgeColor);
        edgeImage.raycastTarget = false;

        GameObject column = CreateChild("Column", panel.transform);
        RectTransform columnRect = column.GetComponent<RectTransform>();
        StretchToParent(columnRect);
        columnRect.offsetMin = new Vector2(12f, 12f);
        columnRect.offsetMax = new Vector2(-12f, -12f);

        VerticalLayoutGroup panelLayout = column.AddComponent<VerticalLayoutGroup>();
        panelLayout.childControlHeight = true;
        panelLayout.childControlWidth = true;
        panelLayout.childForceExpandHeight = false;
        panelLayout.childForceExpandWidth = true;
        panelLayout.spacing = 10f;

        BuildHeader(column.transform);
        BuildMessageArea(column.transform);
        BuildInputArea(column.transform);
    }

    private void BuildHeader(Transform parent)
    {
        GameObject header = CreateChild("Header", parent);
        _ = AddImage(header, HeaderColor);
        LayoutElement headerLayoutElement = header.AddComponent<LayoutElement>();
        headerLayoutElement.preferredHeight = 58f;

        HorizontalLayoutGroup headerLayout = header.AddComponent<HorizontalLayoutGroup>();
        headerLayout.childAlignment = TextAnchor.MiddleCenter;
        headerLayout.childControlHeight = true;
        headerLayout.childControlWidth = true;
        headerLayout.childForceExpandHeight = true;
        headerLayout.childForceExpandWidth = false;
        headerLayout.padding = new RectOffset(14, 8, 0, 0);
        headerLayout.spacing = 12f;

        GameObject iconObject = CreateChild("HeaderIcon", header.transform);
        CImage icon = AddImage(iconObject, new Color(0.72f, 0.44f, 0.19f, 1f), HeaderIconSprite);
        icon.raycastTarget = false;
        LayoutElement iconLayout = iconObject.AddComponent<LayoutElement>();
        iconLayout.preferredWidth = 38f;
        iconLayout.preferredHeight = 38f;

        TextMeshProUGUI title = CreateText("Title", header.transform, 24f, TextColor, FontStyles.Bold);
        title.text = "相枢";
        title.alignment = TextAlignmentOptions.MidlineLeft;
        LayoutElement titleLayout = title.gameObject.AddComponent<LayoutElement>();
        titleLayout.flexibleWidth = 1f;

        Button close = CreateButton("CloseButton", header.transform, "×", 40f, 40f);
        close.onClick.AddListener(() => SetVisible(visible: false));
    }

    private void BuildMessageArea(Transform parent)
    {
        GameObject scrollObject = CreateChild("Messages", parent);
        LayoutElement scrollLayout = scrollObject.AddComponent<LayoutElement>();
        scrollLayout.flexibleHeight = 1f;
        scrollLayout.minHeight = 300f;

        _ = AddImage(scrollObject, MessageAreaColor);
        _scrollRect = scrollObject.AddComponent<ScrollRect>();
        _scrollRect.horizontal = false;
        _scrollRect.movementType = ScrollRect.MovementType.Clamped;
        _scrollRect.scrollSensitivity = 36f;

        GameObject viewport = CreateChild("Viewport", scrollObject.transform);
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        StretchToParent(viewportRect);
        CImage viewportImage = AddImage(viewport, new Color(0f, 0f, 0f, 0.08f));
        viewportImage.raycastTarget = false;
        viewport.AddComponent<Mask>().showMaskGraphic = false;

        GameObject content = CreateChild("Content", viewport.transform);
        _messageContent = content.GetComponent<RectTransform>();
        _messageContent.anchorMin = new Vector2(0f, 1f);
        _messageContent.anchorMax = new Vector2(1f, 1f);
        _messageContent.pivot = new Vector2(0.5f, 1f);
        _messageContent.anchoredPosition = Vector2.zero;
        _messageContent.sizeDelta = new Vector2(0f, 0f);

        VerticalLayoutGroup contentLayout = content.AddComponent<VerticalLayoutGroup>();
        contentLayout.childControlHeight = true;
        contentLayout.childControlWidth = true;
        contentLayout.childForceExpandHeight = false;
        contentLayout.childForceExpandWidth = true;
        contentLayout.spacing = 0f;
        contentLayout.padding = new RectOffset(0, 0, 10, 10);
        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        _scrollRect.viewport = viewportRect;
        _scrollRect.content = _messageContent;
    }

    private void BuildInputArea(Transform parent)
    {
        GameObject inputArea = CreateChild("InputArea", parent);
        LayoutElement inputAreaLayoutElement = inputArea.AddComponent<LayoutElement>();
        inputAreaLayoutElement.preferredHeight = 116f;

        HorizontalLayoutGroup inputAreaLayout = inputArea.AddComponent<HorizontalLayoutGroup>();
        inputAreaLayout.childAlignment = TextAnchor.MiddleCenter;
        inputAreaLayout.childControlHeight = true;
        inputAreaLayout.childControlWidth = true;
        inputAreaLayout.childForceExpandHeight = true;
        inputAreaLayout.childForceExpandWidth = false;
        inputAreaLayout.padding = new RectOffset(12, 12, 12, 12);
        inputAreaLayout.spacing = 10f;

        GameObject inputObject = CreateChild("InputField", inputArea.transform);
        CImage inputImage = AddImage(inputObject, InputColor);
        LayoutElement inputLayout = inputObject.AddComponent<LayoutElement>();
        inputLayout.flexibleWidth = 1f;
        inputLayout.minHeight = 92f;

        _inputField = inputObject.AddComponent<DisableHotkeyInputField>();
        _inputField.lineType = TMP_InputField.LineType.MultiLineNewline;
        _inputField.characterLimit = 4000;
        _inputField.targetGraphic = inputImage;
        _inputField.caretColor = AccentColor;
        _inputField.selectionColor = new Color(0.8f, 0.55f, 0.25f, 0.32f);

        GameObject textViewport = CreateChild("TextViewport", inputObject.transform);
        RectTransform textViewportRect = textViewport.GetComponent<RectTransform>();
        textViewportRect.anchorMin = Vector2.zero;
        textViewportRect.anchorMax = Vector2.one;
        textViewportRect.offsetMin = new Vector2(12f, 8f);
        textViewportRect.offsetMax = new Vector2(-12f, -8f);
        _ = textViewport.AddComponent<RectMask2D>();

        TextMeshProUGUI placeholder = CreateText(
            "Placeholder",
            textViewport.transform,
            17f,
            MutedTextColor,
            FontStyles.Normal);
        placeholder.text = "与相枢言说";
        StretchToParent(placeholder.rectTransform);

        TextMeshProUGUI inputText = CreateText(
            "Text",
            textViewport.transform,
            17f,
            TextColor,
            FontStyles.Normal);
        inputText.text = string.Empty;
        StretchToParent(inputText.rectTransform);

        _inputField.textViewport = textViewportRect;
        _inputField.textComponent = inputText;
        _inputField.placeholder = placeholder;
        _inputField.onValueChanged.AddListener(_ => UpdateSendButtonState());

        _sendButton = CreateButton("SendButton", inputArea.transform, "送出", 88f, 92f);
        _sendButton.onClick.AddListener(SendCurrentInput);
        _sendButtonText = _sendButton.GetComponentInChildren<TextMeshProUGUI>();
        _sendButtonImage = _sendButton.GetComponent<CImage>();
        UpdateSendButtonState();
    }

    private bool EnsureUiBuilt()
    {
        if (_uiBuilt)
        {
            return true;
        }

        UIManager uiManager = UIManager.Instance;

        if (uiManager is null)
        {
            Log.Warning("chat window cannot build because UIManager is unavailable");
            return false;
        }

        RectTransform layer = uiManager.GetLayer(UILayer.LayerVeryTop)
            ?? uiManager.GetLayer(UILayer.LayerPopUp);

        if (layer is null)
        {
            Log.Warning("chat window cannot build because no Taiwu UI layer is available");
            return false;
        }

        transform.SetParent(layer, worldPositionStays: false);
        transform.SetAsLastSibling();
        CaptureGameTextStyle();
        BuildUi(uiManager);
        _uiBuilt = true;
        return true;
    }

    private static void EnsureInteractivePanel(
        GameObject panel,
        UIManager uiManager)
    {
        Canvas canvas = panel.GetComponent<Canvas>();
        canvas ??= panel.AddComponent<Canvas>();

        canvas.enabled = true;
        canvas.overrideSorting = false;
        canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1
            | AdditionalCanvasShaderChannels.TexCoord2
            | AdditionalCanvasShaderChannels.Normal
            | AdditionalCanvasShaderChannels.Tangent;

        ConchShipGraphicRaycaster raycaster = panel.GetComponent<ConchShipGraphicRaycaster>();
        raycaster ??= panel.AddComponent<ConchShipGraphicRaycaster>();

        raycaster.enabled = true;
        raycaster.TargetCamera = uiManager.UiCamera;
    }

    private void LogVisibilityChange(bool visible)
    {
        bool activeSelf = gameObject.activeSelf;
        bool activeInHierarchy = gameObject.activeInHierarchy;

        Log.Info(
            "chat window visibility changed",
            new
            {
                visible,
                activeSelf,
                activeInHierarchy,
                parent = transform.parent?.name,
                panelWidth = _panelRect?.rect.width ?? 0f,
                panelHeight = _panelRect?.rect.height ?? 0f,
                panelX = _panelRect?.anchoredPosition.x ?? 0f,
                panelY = _panelRect?.anchoredPosition.y ?? 0f,
            });
    }

    private void ApplyPanelLayout()
    {
        if (_panelRect is null)
        {
            return;
        }

        Vector2 parentSize = transform is RectTransform rootRect
            ? rootRect.rect.size
            : Vector2.zero;
        float width = GetResponsiveSize(
            parentSize.x,
            PanelPreferredWidth,
            PanelMinimumWidth);
        float height = GetResponsiveSize(
            parentSize.y,
            PanelPreferredHeight,
            PanelMinimumHeight);
        float margin = Mathf.Min(PanelMargin, Mathf.Max(12f, parentSize.x * 0.025f));

        _panelRect.anchoredPosition = new Vector2(-margin, 0f);
        _panelRect.sizeDelta = new Vector2(width, height);
    }

    private static float GetResponsiveSize(
        float availableSize,
        float preferredSize,
        float minimumSize)
    {
        if (availableSize <= 0f)
        {
            return preferredSize;
        }

        float maximumSize = Mathf.Max(minimumSize, availableSize - (PanelMargin * 2f));
        return Mathf.Clamp(preferredSize, minimumSize, maximumSize);
    }

    private void FocusInputField()
    {
        DisableHotkeyInputField? inputField = _inputField;
        if (inputField?.gameObject.activeInHierarchy != true)
        {
            return;
        }

        EventSystem.current?.SetSelectedGameObject(inputField.gameObject);

        inputField.Select();
        inputField.ActivateInputField();
        _ = StartCoroutine(FocusInputFieldAtEndOfFrame());
    }

    private IEnumerator FocusInputFieldAtEndOfFrame()
    {
        yield return null;

        DisableHotkeyInputField? inputField = _inputField;
        if (!IsVisible || inputField?.gameObject.activeInHierarchy != true)
        {
            yield break;
        }

        EventSystem.current?.SetSelectedGameObject(inputField.gameObject);

        inputField.ActivateInputField();
    }

    private static GameObject CreateChild(
        string name,
        Transform parent)
    {
        GameObject child = new(name, typeof(RectTransform));
        child.transform.SetParent(parent, worldPositionStays: false);
        return child;
    }

    private static TextMeshProUGUI CreateText(
        string name,
        Transform parent,
        float fontSize,
        Color color,
        FontStyles fontStyle)
    {
        GameObject textObject = CreateInactiveChild(name, parent);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        ApplyGameTextStyle(text);
        text.fontSize = fontSize;
        text.color = color;
        text.fontStyle = fontStyle;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
        textObject.SetActive(true);
        return text;
    }

    private static Button CreateButton(
        string name,
        Transform parent,
        string label,
        float width,
        float height)
    {
        GameObject buttonObject = CreateChild(name, parent);
        CImage image = AddImage(buttonObject, ButtonColor);
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.15f, 1.08f, 0.94f, 1f);
        colors.pressedColor = new Color(0.82f, 0.72f, 0.58f, 1f);
        colors.disabledColor = Color.white;
        button.colors = colors;
        LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
        layout.preferredWidth = width;
        layout.preferredHeight = height;

        TextMeshProUGUI text = CreateText("Label", buttonObject.transform, 18f, TextColor, FontStyles.Bold);
        text.text = label;
        text.alignment = TextAlignmentOptions.Center;
        StretchToParent(text.rectTransform);
        return button;
    }

    private static GameObject CreateInactiveChild(
        string name,
        Transform parent)
    {
        GameObject child = new(name, typeof(RectTransform));
        child.SetActive(false);
        child.transform.SetParent(parent, worldPositionStays: false);
        return child;
    }

    private static CImage AddImage(
        GameObject target,
        Color color,
        string? spriteName = null)
    {
        CImage image = target.AddComponent<CImage>();
        image.color = color;

        if (!string.IsNullOrWhiteSpace(spriteName))
        {
            image.type = spriteName.StartsWith("ui9_back_", StringComparison.Ordinal)
                ? Image.Type.Sliced
                : Image.Type.Simple;
            image.SetSpriteOnly(spriteName);
        }

        return image;
    }

    private void SetSendButtonTextColor(Color color)
    {
        if (_sendButtonText is null)
        {
            return;
        }

        _sendButtonText.color = color;
    }

    private static void StretchToParent(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private static void CaptureGameTextStyle()
    {
        if (s_gameFontAsset is not null)
        {
            return;
        }

        UIManager uiManager = UIManager.Instance;

        if (uiManager is null)
        {
            return;
        }

        foreach (TextMeshProUGUI text in uiManager.GetComponentsInChildren<TextMeshProUGUI>(includeInactive: true))
        {
            if (text is null || text.font is null)
            {
                continue;
            }

            s_gameFontAsset = text.font;
            s_gameFontMaterial = text.fontSharedMaterial;
            s_gameSpriteAsset = text.spriteAsset;
            return;
        }
    }

    private static void ApplyGameTextStyle(TextMeshProUGUI text)
    {
        CaptureGameTextStyle();

        if (s_gameFontAsset is not null)
        {
            text.font = s_gameFontAsset;
        }

        if (s_gameFontMaterial is not null)
        {
            text.fontSharedMaterial = s_gameFontMaterial;
        }

        if (s_gameSpriteAsset is not null)
        {
            text.spriteAsset = s_gameSpriteAsset;
        }
    }
}
