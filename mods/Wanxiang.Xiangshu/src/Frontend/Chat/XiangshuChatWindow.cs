using System.Diagnostics.CodeAnalysis;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Wanxiang.Xiangshu.Frontend.Chat;

[SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "Unity constructs this MonoBehaviour through GameObject.AddComponent at runtime.")]
internal sealed class XiangshuChatWindow : MonoBehaviour
{
    private static readonly Color PanelColor = new(0.08f, 0.075f, 0.065f, 0.94f);
    private static readonly Color HeaderColor = new(0.15f, 0.12f, 0.08f, 0.98f);
    private static readonly Color AssistantBubbleColor = new(0.18f, 0.17f, 0.145f, 0.96f);
    private static readonly Color UserBubbleColor = new(0.12f, 0.19f, 0.21f, 0.96f);
    private static readonly Color AccentColor = new(0.75f, 0.58f, 0.26f, 1f);
    private static readonly Color TextColor = new(0.92f, 0.88f, 0.78f, 1f);
    private static readonly Color MutedTextColor = new(0.72f, 0.68f, 0.6f, 1f);

    private AgentChatSession? _session;
    private RectTransform? _messageContent;
    private ScrollRect? _scrollRect;
    private DisableHotkeyInputField? _inputField;
    private Button? _sendButton;
    private TextMeshProUGUI? _sendButtonText;
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
        IsVisible = visible;
        gameObject.SetActive(visible);

        if (!visible)
        {
            _inputField?.DeactivateInputField();
            return;
        }

        _scrollToBottom = true;
        _inputField?.Select();
        _inputField?.ActivateInputField();
    }

    public void DestroyWindow()
    {
        Destroy(gameObject);
    }

    private void Initialize(AgentChatSession session)
    {
        _session = session;
        BuildUi();
        SetVisible(visible: false);
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
        _inputField.ActivateInputField();
    }

    private void UpdateSendButtonState()
    {
        if (_sendButton is null || _inputField is null)
        {
            return;
        }

        bool canSend = !string.IsNullOrWhiteSpace(_inputField.text);
        _sendButton.interactable = canSend;

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

        GameObject bubble = CreateChild(isUser ? "PlayerBubble" : "XiangshuBubble", row.transform);
        Image bubbleImage = bubble.AddComponent<Image>();
        bubbleImage.color = isUser ? UserBubbleColor : AssistantBubbleColor;
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

    private void BuildUi()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 6000;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        _ = gameObject.AddComponent<GraphicRaycaster>();

        RectTransform rootRect = GetComponent<RectTransform>();
        StretchToParent(rootRect);

        GameObject panel = CreateChild("Panel", transform);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 0.5f);
        panelRect.anchorMax = new Vector2(1f, 0.5f);
        panelRect.pivot = new Vector2(1f, 0.5f);
        panelRect.anchoredPosition = new Vector2(-32f, 0f);
        panelRect.sizeDelta = new Vector2(560f, 640f);
        panel.AddComponent<Image>().color = PanelColor;

        VerticalLayoutGroup panelLayout = panel.AddComponent<VerticalLayoutGroup>();
        panelLayout.childControlHeight = true;
        panelLayout.childControlWidth = true;
        panelLayout.childForceExpandHeight = false;
        panelLayout.childForceExpandWidth = true;
        panelLayout.padding = new RectOffset(1, 1, 1, 1);
        panelLayout.spacing = 0f;

        BuildHeader(panel.transform);
        BuildMessageArea(panel.transform);
        BuildInputArea(panel.transform);
    }

    private void BuildHeader(Transform parent)
    {
        GameObject header = CreateChild("Header", parent);
        header.AddComponent<Image>().color = HeaderColor;
        LayoutElement headerLayoutElement = header.AddComponent<LayoutElement>();
        headerLayoutElement.preferredHeight = 48f;

        HorizontalLayoutGroup headerLayout = header.AddComponent<HorizontalLayoutGroup>();
        headerLayout.childAlignment = TextAnchor.MiddleCenter;
        headerLayout.childControlHeight = true;
        headerLayout.childControlWidth = true;
        headerLayout.childForceExpandHeight = true;
        headerLayout.childForceExpandWidth = false;
        headerLayout.padding = new RectOffset(18, 8, 0, 0);
        headerLayout.spacing = 12f;

        TextMeshProUGUI title = CreateText("Title", header.transform, 22f, TextColor, FontStyles.Bold);
        title.text = "相枢";
        LayoutElement titleLayout = title.gameObject.AddComponent<LayoutElement>();
        titleLayout.flexibleWidth = 1f;

        Button close = CreateButton("CloseButton", header.transform, "×", 38f, 34f);
        close.onClick.AddListener(() => SetVisible(visible: false));
    }

    private void BuildMessageArea(Transform parent)
    {
        GameObject scrollObject = CreateChild("Messages", parent);
        LayoutElement scrollLayout = scrollObject.AddComponent<LayoutElement>();
        scrollLayout.flexibleHeight = 1f;
        scrollLayout.minHeight = 300f;

        Image scrollImage = scrollObject.AddComponent<Image>();
        scrollImage.color = new Color(0.055f, 0.052f, 0.045f, 0.96f);
        _scrollRect = scrollObject.AddComponent<ScrollRect>();
        _scrollRect.horizontal = false;

        GameObject viewport = CreateChild("Viewport", scrollObject.transform);
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        StretchToParent(viewportRect);
        viewport.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.08f);
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
        inputObject.AddComponent<Image>().color = new Color(0.1f, 0.095f, 0.085f, 1f);
        LayoutElement inputLayout = inputObject.AddComponent<LayoutElement>();
        inputLayout.flexibleWidth = 1f;
        inputLayout.minHeight = 92f;

        _inputField = inputObject.AddComponent<DisableHotkeyInputField>();
        _inputField.lineType = TMP_InputField.LineType.MultiLineNewline;
        _inputField.characterLimit = 4000;

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
        UpdateSendButtonState();
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
        GameObject textObject = CreateChild(name, parent);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.color = color;
        text.fontStyle = fontStyle;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
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
        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.2f, 0.155f, 0.08f, 1f);
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
        layout.preferredWidth = width;
        layout.preferredHeight = height;

        TextMeshProUGUI text = CreateText("Label", buttonObject.transform, 18f, TextColor, FontStyles.Bold);
        text.text = label;
        text.alignment = TextAlignmentOptions.Center;
        StretchToParent(text.rectTransform);
        return button;
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
}
