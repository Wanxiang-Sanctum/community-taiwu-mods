using System.Diagnostics.CodeAnalysis;
using Cysharp.Threading.Tasks;
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
    private const float PreferredPanelWidth = 620f;
    private const float PreferredPanelHeight = 720f;
    private const float MinimumPanelWidth = 460f;
    private const float MinimumPanelHeight = 520f;
    private const float PanelScreenMargin = 32f;
    private const float HeaderHeight = 58f;
    private const float HeaderIconSize = 38f;
    private const float HeaderCloseButtonSize = 40f;
    private const float MessageRowHorizontalPadding = 14f;
    private const float MessageBubbleWidthRatio = 0.76f;
    private const float MinimumMessageBubbleWidth = 220f;
    private const float PreferredMessageBubbleWidth = 430f;
    private const float MaximumMessageBubbleWidth = 460f;

    private static readonly TaiwuLogger Log = TaiwuLogger.ForTag("Wanxiang.Xiangshu");
    private static readonly Color PanelColor = new(0.055f, 0.049f, 0.041f, 0.97f);
    private static readonly Color PanelEdgeColor = new(0.42f, 0.25f, 0.13f, 0.9f);
    private static readonly Color HeaderColor = new(0.12f, 0.087f, 0.058f, 0.98f);
    private static readonly Color MessageAreaColor = new(0.035f, 0.033f, 0.031f, 0.72f);
    private static readonly Color InputColor = new(0.08f, 0.074f, 0.063f, 0.98f);
    private static readonly Color FocusedInputColor = new(0.105f, 0.092f, 0.071f, 0.99f);
    private static readonly Color InputFocusOutlineColor = new(0.82f, 0.59f, 0.28f, 0.72f);
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
    private CImage? _inputFieldImage;
    private Outline? _inputFocusOutline;
    private Button? _sendButton;
    private CImage? _sendButtonImage;
    private TextMeshProUGUI? _sendButtonText;
    private ChatParticipantIdentity? _participants;
    private readonly List<LayoutElement> _messageBubbleLayouts = [];
    private readonly List<TextMeshProUGUI> _playerSpeakerTexts = [];
    private bool _uiBuilt;
    private bool _inputFocused;
    private bool _scrollToBottomScheduled;
    private float _lastMessageBubbleWidth = PreferredMessageBubbleWidth;

    public bool IsVisible { get; private set; }

    public static XiangshuChatWindow Create(
        AgentChatSession session,
        ChatParticipantIdentity participants)
    {
        GameObject root = new("Wanxiang.Xiangshu.ChatWindow", typeof(RectTransform));
        DontDestroyOnLoad(root);
        XiangshuChatWindow window = root.AddComponent<XiangshuChatWindow>();
        window.Initialize(session, participants);
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
            SetInputFocused(focused: false);
            return;
        }

        transform.SetAsLastSibling();
        _participants?.Refresh();
        DrainSessionEvents();
        ScheduleScrollToBottom();
        FocusInputField();
    }

    public void DestroyWindow()
    {
        Destroy(gameObject);
    }

    private void Initialize(
        AgentChatSession session,
        ChatParticipantIdentity participants)
    {
        _session = session;
        _participants = participants;
        _participants.PlayerNameChanged += UpdatePlayerSpeakerLabels;
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
        if (IsVisible)
        {
            _participants?.Refresh();
        }

        DrainSessionEvents();
        UpdateSendButtonState();
        UpdateInputFocusVisual();

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
        Justification = "Unity invokes OnDestroy by method name.")]
    [SuppressMessage(
        "Roslynator",
        "RCS1213:Remove unused member declaration",
        Justification = "Unity invokes OnDestroy by method name.")]
    private void OnDestroy()
    {
        ChatParticipantIdentity? participants = _participants;

        if (participants is null)
        {
            return;
        }

        participants.PlayerNameChanged -= UpdatePlayerSpeakerLabels;
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
    }

    private void DrainSessionEvents()
    {
        if (_session is null)
        {
            return;
        }

        while (_session.TryDequeueEvent(out AgentChatSessionEvent sessionEvent))
        {
            AddMessage(sessionEvent.Message);
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

        ChatParticipantIdentity? participants = _participants;

        if (participants is null)
        {
            return;
        }

        participants.Refresh();

        if (!participants.IsPlayerNameReady)
        {
            UpdateSendButtonState();
            return;
        }

        _inputField.SetTextWithoutNotify(string.Empty);
        _session.SubmitUserMessage(
            content,
            participants.PlayerName!);
        UpdateSendButtonState();
        FocusInputField();
    }

    private void UpdateSendButtonState()
    {
        if (_sendButton is null || _inputField is null || _sendButtonImage is null)
        {
            return;
        }

        bool canSend = !string.IsNullOrWhiteSpace(_inputField.text)
            && _participants?.IsPlayerNameReady == true;
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
        rowLayout.childControlWidth = true;
        rowLayout.childForceExpandHeight = false;
        rowLayout.childForceExpandWidth = false;
        rowLayout.padding = new RectOffset(
            (int)MessageRowHorizontalPadding,
            (int)MessageRowHorizontalPadding,
            6,
            6);
        _ = row.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

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
        bubbleLayoutElement.preferredWidth = GetMessageBubbleWidth();
        bubbleLayoutElement.flexibleWidth = 0f;
        _messageBubbleLayouts.Add(bubbleLayoutElement);
        _ = bubble.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        TextMeshProUGUI speaker = CreateText(
            message.SpeakerName,
            bubble.transform,
            16f,
            isUser ? new Color(0.72f, 0.88f, 0.9f, 1f) : AccentColor,
            FontStyles.Bold);
        speaker.text = message.SpeakerName;
        speaker.alignment = isUser
            ? TextAlignmentOptions.MidlineRight
            : TextAlignmentOptions.MidlineLeft;

        if (isUser)
        {
            _playerSpeakerTexts.Add(speaker);
        }

        TextMeshProUGUI body = CreateText(
            "MessageText",
            bubble.transform,
            18f,
            TextColor,
            FontStyles.Normal);
        body.text = message.Content;

        LayoutRebuilder.ForceRebuildLayoutImmediate(_messageContent);
        ScheduleScrollToBottom();
    }

    private void UpdatePlayerSpeakerLabels()
    {
        ChatParticipantIdentity? participants = _participants;

        if (participants?.IsPlayerNameReady != true)
        {
            return;
        }

        string playerName = participants.PlayerName!;

        for (int index = _playerSpeakerTexts.Count - 1; index >= 0; index--)
        {
            TextMeshProUGUI speaker = _playerSpeakerTexts[index];

            if (speaker == null)
            {
                _playerSpeakerTexts.RemoveAt(index);
                continue;
            }

            speaker.text = playerName;
        }
    }

    private void ReflowMessageBubbles()
    {
        float width = GetMessageBubbleWidth();

        if (Mathf.Approximately(_lastMessageBubbleWidth, width))
        {
            return;
        }

        _lastMessageBubbleWidth = width;

        for (int index = _messageBubbleLayouts.Count - 1; index >= 0; index--)
        {
            LayoutElement layout = _messageBubbleLayouts[index];

            if (layout == null)
            {
                _messageBubbleLayouts.RemoveAt(index);
                continue;
            }

            layout.preferredWidth = width;
        }

        if (_messageContent is not null)
        {
            LayoutRebuilder.MarkLayoutForRebuild(_messageContent);
        }
    }

    private float GetMessageBubbleWidth()
    {
        float contentWidth = 0f;

        if (_messageContent is not null)
        {
            contentWidth = _messageContent.rect.width;
        }

        if (contentWidth <= 0f && _scrollRect?.viewport is not null)
        {
            contentWidth = _scrollRect.viewport.rect.width;
        }

        if (contentWidth <= 0f && _panelRect is not null)
        {
            contentWidth = _panelRect.rect.width - 24f;
        }

        if (contentWidth <= 0f)
        {
            return PreferredMessageBubbleWidth;
        }

        float usableWidth = Mathf.Max(0f, contentWidth - (MessageRowHorizontalPadding * 2f));

        if (usableWidth <= 0f)
        {
            return PreferredMessageBubbleWidth;
        }

        float maximumWidth = Mathf.Min(MaximumMessageBubbleWidth, usableWidth);
        float minimumWidth = Mathf.Min(MinimumMessageBubbleWidth, maximumWidth);
        return Mathf.Clamp(usableWidth * MessageBubbleWidthRatio, minimumWidth, maximumWidth);
    }

    private void BuildUi()
    {
        RectTransform rootRect = GetComponent<RectTransform>();
        StretchToParent(rootRect);

        GameObject panel = CreateChild("Panel", transform);
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
        headerLayoutElement.minHeight = HeaderHeight;
        headerLayoutElement.preferredHeight = HeaderHeight;
        headerLayoutElement.flexibleHeight = 0f;

        HorizontalLayoutGroup headerLayout = header.AddComponent<HorizontalLayoutGroup>();
        headerLayout.childAlignment = TextAnchor.MiddleLeft;
        headerLayout.childControlHeight = true;
        headerLayout.childControlWidth = true;
        headerLayout.childForceExpandHeight = false;
        headerLayout.childForceExpandWidth = false;
        headerLayout.padding = new RectOffset(14, 8, 0, 0);
        headerLayout.spacing = 12f;

        GameObject iconObject = CreateChild("HeaderIcon", header.transform);
        CImage icon = AddImage(iconObject, new Color(0.72f, 0.44f, 0.19f, 1f), HeaderIconSprite);
        icon.raycastTarget = false;
        icon.preserveAspect = true;
        _ = SetFixedLayoutSize(iconObject, HeaderIconSize, HeaderIconSize);

        TextMeshProUGUI title = CreateText("Title", header.transform, 24f, TextColor, FontStyles.Bold);
        title.text = "相枢";
        title.alignment = TextAlignmentOptions.MidlineLeft;
        LayoutElement titleLayout = title.gameObject.AddComponent<LayoutElement>();
        titleLayout.flexibleWidth = 1f;

        Button close = CreateButton(
            "CloseButton",
            header.transform,
            "×",
            HeaderCloseButtonSize,
            HeaderCloseButtonSize);
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
        _inputFieldImage = inputImage;
        _inputFocusOutline = inputObject.AddComponent<Outline>();
        _inputFocusOutline.effectColor = InputFocusOutlineColor;
        _inputFocusOutline.effectDistance = new Vector2(2f, -2f);
        _inputFocusOutline.enabled = false;
        LayoutElement inputLayout = inputObject.AddComponent<LayoutElement>();
        inputLayout.flexibleWidth = 1f;
        inputLayout.minHeight = 92f;

        _inputField = inputObject.AddComponent<DisableHotkeyInputField>();
        _inputField.transition = Selectable.Transition.None;
        _inputField.lineType = TMP_InputField.LineType.MultiLineNewline;
        _inputField.characterLimit = 4000;
        _inputField.targetGraphic = inputImage;
        _inputField.customCaretColor = true;
        _inputField.caretColor = AccentColor;
        _inputField.caretWidth = 2;
        _inputField.caretBlinkRate = 0.8f;
        _inputField.selectionColor = new Color(0.8f, 0.55f, 0.25f, 0.32f);
        _inputField.onSelect.AddListener(_ => SetInputFocused(focused: true));
        _inputField.onDeselect.AddListener(_ => SetInputFocused(focused: false));

        GameObject textViewport = CreateChild("TextViewport", inputObject.transform);
        RectTransform textViewportRect = textViewport.GetComponent<RectTransform>();
        textViewportRect.anchorMin = Vector2.zero;
        textViewportRect.anchorMax = Vector2.one;
        textViewportRect.offsetMin = new Vector2(12f, 8f);
        textViewportRect.offsetMax = new Vector2(-12f, -8f);
        _ = textViewport.AddComponent<RectMask2D>();

        TextMeshProUGUI inputText = CreateText(
            "Text",
            textViewport.transform,
            17f,
            TextColor,
            FontStyles.Normal);
        inputText.text = string.Empty;
        inputText.alignment = TextAlignmentOptions.TopLeft;
        StretchToParent(inputText.rectTransform);

        _inputField.textViewport = textViewportRect;
        _inputField.textComponent = inputText;
        _inputField.onValueChanged.AddListener(_ => UpdateSendButtonState());
        SetInputFocused(focused: false);

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
        EnsureLayerRaycaster(layer, uiManager);
        CaptureGameTextStyle();
        BuildUi();
        _uiBuilt = true;
        return true;
    }

    private static void EnsureLayerRaycaster(
        RectTransform layer,
        UIManager uiManager)
    {
        Canvas canvas = layer.GetComponentInParent<Canvas>();

        if (canvas is null)
        {
            Log.Warning(
                "chat window cannot attach raycaster because no parent canvas was found",
                new
                {
                    layer = layer.name,
                });
            return;
        }

        ConchShipGraphicRaycaster raycaster = canvas.GetComponent<ConchShipGraphicRaycaster>();
        raycaster ??= canvas.gameObject.AddComponent<ConchShipGraphicRaycaster>();

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
                canvas = _panelRect?.GetComponentInParent<Canvas>()?.name,
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

        Vector2 parentSize = GetRootLayoutSize();
        float horizontalMargin = GetPanelMargin(parentSize.x);
        float verticalMargin = GetPanelMargin(parentSize.y);
        float width = ClampPanelExtent(
            parentSize.x,
            PreferredPanelWidth,
            MinimumPanelWidth,
            horizontalMargin);
        float height = ClampPanelExtent(
            parentSize.y,
            PreferredPanelHeight,
            MinimumPanelHeight,
            verticalMargin);

        _panelRect.anchoredPosition = new Vector2(-horizontalMargin, 0f);
        _panelRect.sizeDelta = new Vector2(width, height);
        ReflowMessageBubbles();
    }

    private Vector2 GetRootLayoutSize()
    {
        if (transform is RectTransform rootRect
            && rootRect.rect.width > 0f
            && rootRect.rect.height > 0f)
        {
            return rootRect.rect.size;
        }

        return transform.parent is RectTransform parentRect
            ? parentRect.rect.size
            : Vector2.zero;
    }

    private static float GetPanelMargin(float availableSize)
    {
        if (availableSize <= 0f)
        {
            return PanelScreenMargin;
        }

        return Mathf.Min(PanelScreenMargin, Mathf.Max(12f, availableSize * 0.025f));
    }

    private static float ClampPanelExtent(
        float availableSize,
        float preferredSize,
        float minimumSize,
        float margin)
    {
        if (availableSize <= 0f)
        {
            return preferredSize;
        }

        float maximumSize = Mathf.Max(0f, availableSize - (margin * 2f));

        if (maximumSize <= 0f)
        {
            return preferredSize;
        }

        float effectiveMinimumSize = Mathf.Min(minimumSize, maximumSize);
        return Mathf.Clamp(preferredSize, effectiveMinimumSize, maximumSize);
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
        SetInputFocused(focused: true);
        RefocusInputNextFrameAsync(WindowLifetimeToken).Forget(
            static ex => Log.Error(ex, "chat input refocus failed"));
    }

    private async UniTask RefocusInputNextFrameAsync(CancellationToken cancellationToken)
    {
        try
        {
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        DisableHotkeyInputField? inputField = _inputField;
        if (!IsVisible || inputField?.gameObject.activeInHierarchy != true)
        {
            return;
        }

        EventSystem.current?.SetSelectedGameObject(inputField.gameObject);

        inputField.ActivateInputField();
        SetInputFocused(focused: true);
    }

    private void UpdateInputFocusVisual()
    {
        bool focused = _inputField?.isFocused == true;

        if (_inputFocused == focused)
        {
            return;
        }

        SetInputFocused(focused);
    }

    private void SetInputFocused(bool focused)
    {
        _inputFocused = focused;

        if (_inputFieldImage is { } inputFieldImage)
        {
            inputFieldImage.color = focused ? FocusedInputColor : InputColor;
        }

        if (_inputFocusOutline is { } inputFocusOutline)
        {
            inputFocusOutline.enabled = focused;
        }
    }

    private void ScheduleScrollToBottom()
    {
        if (_scrollToBottomScheduled)
        {
            return;
        }

        _scrollToBottomScheduled = true;
        ScrollToBottomNextFrameAsync(WindowLifetimeToken).Forget(
            static ex => Log.Error(ex, "chat scroll-to-bottom failed"));
    }

    private async UniTask ScrollToBottomNextFrameAsync(CancellationToken cancellationToken)
    {
        try
        {
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return;
        }
        finally
        {
            _scrollToBottomScheduled = false;
        }

        if (!IsVisible || _scrollRect is null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();
        _scrollRect.verticalNormalizedPosition = 0f;
    }

    private CancellationToken WindowLifetimeToken => this.GetCancellationTokenOnDestroy();

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
        _ = SetFixedLayoutSize(buttonObject, width, height);

        TextMeshProUGUI text = CreateText("Label", buttonObject.transform, 18f, TextColor, FontStyles.Bold);
        text.text = label;
        text.alignment = TextAlignmentOptions.Center;
        StretchToParent(text.rectTransform);
        return button;
    }

    private static LayoutElement SetFixedLayoutSize(
        GameObject target,
        float width,
        float height)
    {
        RectTransform rectTransform = target.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(width, height);

        LayoutElement layout = target.GetComponent<LayoutElement>()
            ?? target.AddComponent<LayoutElement>();
        layout.minWidth = width;
        layout.preferredWidth = width;
        layout.flexibleWidth = 0f;
        layout.minHeight = height;
        layout.preferredHeight = height;
        layout.flexibleHeight = 0f;
        return layout;
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
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = Vector2.zero;
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
