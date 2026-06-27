using System.Diagnostics.CodeAnalysis;
using Cysharp.Threading.Tasks;
using FrameWork.UISystem.UIElements;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Wanxiang.Taiwu.Logging;
using Wanxiang.Taiwu.InstantNotifications;
using Wanxiang.Xiangshu.Frontend.ItemGrafts;

using InstantNotificationConfig = Config.InstantNotification;

namespace Wanxiang.Xiangshu.Frontend.Chat;

[SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "Unity constructs this MonoBehaviour through GameObject.AddComponent at runtime.")]
[RequireComponent(typeof(Canvas))]
[RequireComponent(typeof(CanvasScaler))]
[RequireComponent(typeof(ConchShipGraphicRaycaster))]
[RequireComponent(typeof(CanvasGroup))]
internal sealed class XiangshuChatWindow : MonoBehaviour
{
    internal const string RootGameObjectName = "Wanxiang.Xiangshu.ChatWindow";

    private const string HeaderIconSprite = "map_icon_xiangshu";
    private const string HeaderPortraitTexturePath =
        "RemakeResources/Textures/GameLineScroll/npcface_image_2001_0";
    private const string HeaderPortraitFrameSprite = "gamelinescroll_icon_big_charm_2001";
    private const string AssistantBubbleSprite = "ui9_back_mousetip_base_npcthink_1";
    private const string UserBubbleSprite = "ui9_back_mousetip_base_npcthink_2";
    private const string ScrollbarHandleSprite = "sp_gn_gundong_7";
    private const string HeaderCloseButtonNormalSprite = "ui9_btn_close_0";
    private const string HeaderCloseButtonHighlightedSprite = "ui9_btn_close_1";
    private const string HeaderCloseButtonDisabledSprite = "ui9_btn_close_3";
    private const float PreferredPanelWidth = 860f;
    private const float PreferredPanelHeight = 820f;
    private const float MinimumPanelWidth = 640f;
    private const float MinimumPanelHeight = 580f;
    private const float PanelScreenMargin = 36f;
    private const float HeaderHeight = 88f;
    private const float HeaderPortraitSize = 64f;
    private const float HeaderIconSize = 26f;
    private const float HeaderResetButtonWidth = 58f;
    private const float HeaderResetButtonHeight = 38f;
    private const float HeaderCloseButtonSize = 34f;
    private const float HeaderReplyIndicatorWidth = 130f;
    private const float HeaderReplyIndicatorHeight = 36f;
    private const float ScrollbarReservedWidth = 30f;
    private const float ScrollbarRailWidth = 13f;
    private const float ScrollbarHandleWidth = 6f;
    private const float ScrollbarRightInset = 10f;
    private const float ScrollbarVerticalInset = 12f;
    private const float ScrollbarHandleMarkWidth = 8f;
    private const float MessageRowEdgePadding = 18f;
    private const float MessageRowOppositeGutter = 72f;
    private const float MessageBubbleWidthRatio = 0.92f;
    private const float MinimumMessageBubbleWidth = 300f;
    private const float PreferredMessageBubbleWidth = 660f;
    private const float MaximumMessageBubbleWidth = 680f;
    private const float MinimumDraggedPanelVisibleMargin = 8f;
    private const float InputAreaHeight = 108f;
    private const float InputFieldHeight = 100f;
    private const float SendButtonWidth = 74f;
    private const float SendButtonHeight = 100f;
    private const float FallbackCanvasScaleFactor = 1f;
    private const float DefaultReferencePixelsPerUnit = 100f;
    private const float HeaderTitleFontSize = 26f;
    private const float ReplyIndicatorFontSize = 16f;
    private const float MessageSpeakerFontSize = 18f;
    private const float MessageBodyFontSize = 24f;
    private const float InputTextFontSize = 20f;
    private const float ButtonLabelFontSize = 20f;
    private const float HeaderControlFontSize = 18f;
    private const float HeaderControlHoverFrameThickness = 2f;
    private const int CanvasSortingOrder = 32000;
    private const string HostUnavailableButtonLabel = "离身";
    private const string HiddenAssistantMessageNotificationText = "钵中低语复起。";
    private const short HiddenAssistantMessageNotificationTemplateId = InstantNotificationConfig.DefKey.WalkThroughAbyss;
    private static readonly TaiwuLogger Log = TaiwuLogger.ForTag("Wanxiang.Xiangshu");
    private static readonly Color PanelColor = new(0.078f, 0.113f, 0.108f, 0.97f);
    private static readonly Color PanelOuterLineColor = new(0.020f, 0.029f, 0.027f, 0.98f);
    private static readonly Color MessageAreaColor = new(0.017f, 0.030f, 0.028f, 0.95f);
    private static readonly Color InputColor = new(0.052f, 0.037f, 0.027f, 0.96f);
    private static readonly Color DisabledInputColor = new(0.038f, 0.034f, 0.030f, 0.86f);
    private static readonly Color InputBorderColor = new(0.38f, 0.22f, 0.08f, 0.90f);
    private static readonly Color InputCaretColor = new(0.95f, 0.72f, 0.34f, 0.96f);
    private static readonly Color AssistantBubbleColor = new(0.30f, 0.25f, 0.15f, 0.92f);
    private static readonly Color UserBubbleColor = new(0.09f, 0.20f, 0.21f, 0.92f);
    private static readonly Color AssistantBubbleOutlineColor = new(0.72f, 0.45f, 0.18f, 0.40f);
    private static readonly Color UserBubbleOutlineColor = new(0.25f, 0.58f, 0.60f, 0.38f);
    private static readonly Color AccentColor = new(0.82f, 0.59f, 0.28f, 1f);
    private static readonly Color TextColor = new(0.92f, 0.88f, 0.78f, 1f);
    private static readonly Color MutedTextColor = new(0.67f, 0.62f, 0.52f, 1f);
    private static readonly Color ButtonColor = new(0.14f, 0.085f, 0.040f, 0.98f);
    private static readonly Color InterruptButtonColor = new(0.23f, 0.115f, 0.045f, 0.98f);
    private static readonly Color DisabledButtonColor = new(0.075f, 0.064f, 0.052f, 0.88f);
    private static readonly Color HeaderControlHoverFrameColor = new(0.68f, 0.64f, 0.54f, 0.66f);
    private static readonly Color ScrollbarTrackColor = new(0.018f, 0.015f, 0.012f, 0.46f);
    private static readonly Color ScrollbarHandleColor = new(0.34f, 0.20f, 0.08f, 0.76f);
    private static readonly Color ScrollbarHandleMarkColor = new(0.83f, 0.48f, 0.18f, 0.95f);

    private delegate void SpriteStateMutator(ref SpriteState state, Sprite sprite);

    private static TMP_FontAsset? s_gameFontAsset;
    private static Material? s_gameFontMaterial;
    private static TMP_SpriteAsset? s_gameSpriteAsset;

    private AgentChatSession? _session;
    private RectTransform? _panelRect;
    private RectTransform? _messageContent;
    private ScrollRect? _scrollRect;
    private DisableHotkeyInputField? _inputField;
    private Canvas? _rootCanvas;
    private CanvasScaler? _rootCanvasScaler;
    private ConchShipGraphicRaycaster? _rootRaycaster;
    private CanvasGroup? _rootCanvasGroup;
    private CImage? _inputFieldImage;
    private Button? _sendButton;
    private CImage? _sendButtonImage;
    private TextMeshProUGUI? _sendButtonText;
    private GameObject? _replyIndicator;
    private TextMeshProUGUI? _replyIndicatorText;
    private ChatParticipantIdentity? _participants;
    private readonly List<LayoutElement> _messageBubbleLayouts = [];
    private readonly List<TextMeshProUGUI> _playerSpeakerTexts = [];
    private readonly HashSet<string> _renderedMessageIds = new(StringComparer.Ordinal);
    private bool _uiBuilt;
    private bool _scrollToBottomScheduled;
    private float _lastMessageBubbleWidth = PreferredMessageBubbleWidth;
    private Vector2 _panelOffset;
    private Vector2 _panelDragStartPointer;
    private Vector2 _panelDragStartOffset;
    private bool _panelDragActive;

    public bool IsVisible { get; private set; }

    public static XiangshuChatWindow Create(ChatParticipantIdentity participants)
    {
        GameObject root = new(
            RootGameObjectName,
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(ConchShipGraphicRaycaster),
            typeof(CanvasGroup));
        DontDestroyOnLoad(root);
        XiangshuChatWindow window = root.AddComponent<XiangshuChatWindow>();
        window.Initialize(participants);
        return window;
    }

    public void BindSession(AgentChatSession? session)
    {
        if (ReferenceEquals(_session, session))
        {
            return;
        }

        _session = session;

        if (_uiBuilt)
        {
            ReloadVisibleMessages();

            if (_session is not null)
            {
                UpdateSendButtonState();
                UpdateReplyIndicator();
            }
        }
    }

    public void SetVisible(bool visible)
    {
        if (visible && _session is null)
        {
            throw new InvalidOperationException("Cannot show Xiangshu chat window without a bound chat session.");
        }

        if (visible && !EnsureUiBuilt())
        {
            return;
        }

        IsVisible = visible;
        ApplyRootVisibility(visible);

        if (!visible)
        {
            _inputField?.DeactivateInputField();
            UpdateInputFieldStateVisual();
            return;
        }

        transform.SetAsLastSibling();
        _participants?.Refresh();
        DrainSessionEvents();
        ScheduleScrollToBottom();
        FocusInputField();
    }

    internal static IDisposable BeginPlayerViewCaptureExclusion()
    {
        List<VisibilityState> visibilityStates = [];

        foreach (XiangshuChatWindow window in Resources.FindObjectsOfTypeAll<XiangshuChatWindow>())
        {
            if (!window.IsVisible || !window.gameObject.activeInHierarchy)
            {
                continue;
            }

            visibilityStates.Add(window.ExcludeFromPlayerViewCapture());
        }

        return new PlayerViewCaptureExclusionScope(visibilityStates);
    }

    public void DestroyWindow()
    {
        Destroy(gameObject);
    }

    private void Initialize(ChatParticipantIdentity participants)
    {
        _participants = participants;
        _rootCanvas = GetRequiredRootComponent<Canvas>();
        _rootCanvasScaler = GetRequiredRootComponent<CanvasScaler>();
        _rootRaycaster = GetRequiredRootComponent<ConchShipGraphicRaycaster>();
        _rootCanvasGroup = GetRequiredRootComponent<CanvasGroup>();
        _participants.PlayerNameChanged += UpdatePlayerSpeakerLabels;
        IsVisible = false;
        ApplyRootVisibility(visible: false);
    }

    private T GetRequiredRootComponent<T>()
        where T : Component
    {
        return GetComponent<T>()
            ?? throw new InvalidOperationException(
                $"Xiangshu chat window root is missing required {typeof(T).Name} component.");
    }

    private void ApplyRootVisibility(bool visible)
    {
        Canvas rootCanvas = _rootCanvas
            ?? throw new InvalidOperationException("Xiangshu chat window root Canvas is not initialized.");
        CanvasGroup rootCanvasGroup = _rootCanvasGroup
            ?? throw new InvalidOperationException("Xiangshu chat window root CanvasGroup is not initialized.");
        ConchShipGraphicRaycaster rootRaycaster = _rootRaycaster
            ?? throw new InvalidOperationException("Xiangshu chat window root raycaster is not initialized.");

        rootCanvas.enabled = visible;
        rootCanvasGroup.alpha = visible ? 1f : 0f;
        rootCanvasGroup.blocksRaycasts = visible;
        rootCanvasGroup.interactable = visible;
        rootRaycaster.enabled = visible;
    }

    private VisibilityState ExcludeFromPlayerViewCapture()
    {
        CanvasGroup canvasGroup = _rootCanvasGroup
            ?? throw new InvalidOperationException("Xiangshu chat window root CanvasGroup is not initialized.");
        VisibilityState state = new(canvasGroup, canvasGroup.alpha);
        canvasGroup.alpha = 0f;
        return state;
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

        if (!IsVisible)
        {
            return;
        }

        UpdateSendButtonState();
        UpdateReplyIndicator();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SetVisible(visible: false);
            return;
        }

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
        if (!IsVisible)
        {
            return;
        }

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
            if (sessionEvent.Kind == AgentChatSessionEventKind.MessagesReset)
            {
                ReloadVisibleMessages();
                continue;
            }

            if (sessionEvent.Kind == AgentChatSessionEventKind.MessageAdded
                && sessionEvent.Message is not null)
            {
                NotifyHiddenAssistantMessage(sessionEvent.Message);
                AddMessage(sessionEvent.Message);
            }
        }

        UpdateSendButtonState();
    }

    private void NotifyHiddenAssistantMessage(AgentChatMessage message)
    {
        if (IsVisible || message.Role != AgentChatRole.Assistant)
        {
            return;
        }

        InstantNotificationPublisher.Push(
            HiddenAssistantMessageNotificationTemplateId,
            HiddenAssistantMessageNotificationText);
    }

    private void ActivateSendButton()
    {
        if (_session is null)
        {
            return;
        }

        if (!XiangshuGraftState.IsHostInTaiwuInventory)
        {
            UpdateSendButtonState();
            return;
        }

        if (_session.IsReplying)
        {
            InterruptReply();
            return;
        }

        if (_session.RequiresReset)
        {
            return;
        }

        SendCurrentInput();
    }

    private void SendCurrentInput()
    {
        if (_session is null
            || _inputField is null
            || _session.IsReplying
            || _session.RequiresReset
            || !XiangshuGraftState.IsHostInTaiwuInventory)
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

    private void InterruptReply()
    {
        if (_session?.RequiresReset != false)
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

        _ = _session.RequestInterrupt(participants.PlayerName!);
        UpdateSendButtonState();
        FocusInputField();
    }

    private void ResetChatSession()
    {
        AgentChatSession? session = _session;

        if (session is null)
        {
            return;
        }

        session.Reset();
        UpdateSendButtonState();
        FocusInputField();
    }

    private void UpdateSendButtonState()
    {
        if (_sendButton is not { } sendButton
            || _inputField is not { } inputField
            || _sendButtonImage is not { } sendButtonImage
            || _sendButtonText is not { } sendButtonText)
        {
            return;
        }

        AgentChatSession? session = _session;

        if (session is null)
        {
            return;
        }

        bool isReplying = session.IsReplying;
        bool requiresReset = session.RequiresReset;
        bool isPlayerReady = _participants?.IsPlayerNameReady == true;
        bool hostInTaiwuInventory = XiangshuGraftState.IsHostInTaiwuInventory;

        inputField.interactable = !requiresReset;
        UpdateInputFieldStateVisual();

        if (requiresReset)
        {
            inputField.DeactivateInputField();
            sendButton.interactable = false;
            sendButtonImage.color = DisabledButtonColor;
            sendButtonText.text = "需重置";
            sendButtonText.color = MutedTextColor;
            return;
        }

        if (!hostInTaiwuInventory)
        {
            sendButton.interactable = false;
            sendButtonImage.color = DisabledButtonColor;
            sendButtonText.text = HostUnavailableButtonLabel;
            sendButtonText.color = MutedTextColor;
            return;
        }

        if (isReplying)
        {
            sendButton.interactable = isPlayerReady;
            sendButtonImage.color = isPlayerReady ? InterruptButtonColor : DisabledButtonColor;
            sendButtonText.text = isPlayerReady ? "且慢" : "止息中";
            sendButtonText.color = isPlayerReady ? TextColor : MutedTextColor;
            return;
        }

        bool canSend = !string.IsNullOrWhiteSpace(inputField.text)
            && isPlayerReady;
        sendButton.interactable = canSend;
        sendButtonImage.color = canSend ? ButtonColor : DisabledButtonColor;

        sendButtonText.text = "传念";
        sendButtonText.color = canSend ? TextColor : MutedTextColor;
    }

    private void AddMessage(AgentChatMessage message)
    {
        if (_messageContent is null)
        {
            return;
        }

        if (!_renderedMessageIds.Add(message.Id))
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
        rowLayout.spacing = 0f;
        rowLayout.padding = new RectOffset(
            (int)(isUser ? MessageRowOppositeGutter : MessageRowEdgePadding),
            (int)(isUser ? MessageRowEdgePadding : MessageRowOppositeGutter),
            6,
            6);
        _ = row.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject bubble = CreateChild(isUser ? "PlayerBubble" : "XiangshuBubble", row.transform);
        AddMessageBubbleBackground(bubble, isUser);
        VerticalLayoutGroup bubbleLayout = bubble.AddComponent<VerticalLayoutGroup>();
        bubbleLayout.childControlHeight = true;
        bubbleLayout.childControlWidth = true;
        bubbleLayout.childForceExpandHeight = false;
        bubbleLayout.childForceExpandWidth = true;
        bubbleLayout.padding = isUser
            ? new RectOffset(16, 24, 11, 12)
            : new RectOffset(24, 16, 11, 12);
        bubbleLayout.spacing = 5f;
        LayoutElement bubbleLayoutElement = bubble.AddComponent<LayoutElement>();
        bubbleLayoutElement.preferredWidth = GetMessageBubbleWidth();
        bubbleLayoutElement.flexibleWidth = 0f;
        _messageBubbleLayouts.Add(bubbleLayoutElement);
        _ = bubble.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        TextMeshProUGUI speaker = CreateText(
            message.SpeakerName,
            bubble.transform,
            MessageSpeakerFontSize,
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
            MessageBodyFontSize,
            TextColor,
            FontStyles.Normal);
        body.text = message.Content;

        LayoutRebuilder.ForceRebuildLayoutImmediate(_messageContent);
        ScheduleScrollToBottom();
    }

    private void ReplayVisibleMessages()
    {
        AgentChatSession? session = _session;

        if (session is null)
        {
            return;
        }

        foreach (AgentChatMessage message in session.CreateVisibleMessagesSnapshot())
        {
            AddMessage(message);
        }
    }

    private void ReloadVisibleMessages()
    {
        if (_messageContent is null)
        {
            return;
        }

        for (int index = _messageContent.childCount - 1; index >= 0; index--)
        {
            Destroy(_messageContent.GetChild(index).gameObject);
        }

        _messageBubbleLayouts.Clear();
        _playerSpeakerTexts.Clear();
        _renderedMessageIds.Clear();
        ReplayVisibleMessages();
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

        float usableWidth = Mathf.Max(0f, contentWidth - MessageRowEdgePadding - MessageRowOppositeGutter);

        if (usableWidth <= 0f)
        {
            return PreferredMessageBubbleWidth;
        }

        float maximumWidth = Mathf.Min(MaximumMessageBubbleWidth, usableWidth);
        float minimumWidth = Mathf.Min(MinimumMessageBubbleWidth, maximumWidth);
        return Mathf.Clamp(usableWidth * MessageBubbleWidthRatio, minimumWidth, maximumWidth);
    }

    private static void AddMessageBubbleBackground(
        GameObject bubble,
        bool isUser)
    {
        GameObject background = CreateChild("Background", bubble.transform);
        RectTransform backgroundRect = background.GetComponent<RectTransform>();
        StretchToParent(backgroundRect);
        background.AddComponent<LayoutElement>().ignoreLayout = true;

        CImage backgroundImage = AddSpriteImage(
            background,
            isUser ? UserBubbleColor : AssistantBubbleColor,
            isUser ? UserBubbleSprite : AssistantBubbleSprite);
        backgroundImage.raycastTarget = false;

        Outline outline = background.AddComponent<Outline>();
        outline.effectColor = isUser ? UserBubbleOutlineColor : AssistantBubbleOutlineColor;
        outline.effectDistance = isUser
            ? new Vector2(-2f, -2f)
            : new Vector2(2f, -2f);

        if (isUser)
        {
            backgroundRect.localScale = new Vector3(-1f, 1f, 1f);
        }
    }

    private void BuildUi()
    {
        RectTransform rootRect = GetComponent<RectTransform>();
        StretchToParent(rootRect);

        GameObject panel = CreateChild("Panel", transform);
        _panelRect = panel.GetComponent<RectTransform>();
        _panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        _panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        _panelRect.pivot = new Vector2(0.5f, 0.5f);
        ApplyPanelLayout();
        _ = AddSolidImage(panel, PanelColor);
        Outline panelOutline = panel.AddComponent<Outline>();
        panelOutline.effectColor = PanelOuterLineColor;
        panelOutline.effectDistance = new Vector2(2f, -2f);

        GameObject column = CreateChild("Column", panel.transform);
        RectTransform columnRect = column.GetComponent<RectTransform>();
        StretchToParent(columnRect);
        columnRect.offsetMin = new Vector2(16f, 16f);
        columnRect.offsetMax = new Vector2(-16f, -16f);

        VerticalLayoutGroup panelLayout = column.AddComponent<VerticalLayoutGroup>();
        panelLayout.childControlHeight = true;
        panelLayout.childControlWidth = true;
        panelLayout.childForceExpandHeight = false;
        panelLayout.childForceExpandWidth = true;
        panelLayout.spacing = 12f;

        BuildHeader(column.transform);
        BuildMessageArea(column.transform);
        BuildInputArea(column.transform);
    }

    private void BuildHeader(Transform parent)
    {
        GameObject header = CreateChild("Header", parent);
        _ = AddSolidImage(header, Color.clear);
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
        headerLayout.padding = new RectOffset(14, 8, 8, 8);
        headerLayout.spacing = 12f;

        Button reset = CreateTransparentTextButton(
            "ResetButton",
            header.transform,
            "重置",
            HeaderResetButtonWidth,
            HeaderResetButtonHeight,
            HeaderControlFontSize);
        reset.onClick.AddListener(ResetChatSession);

        GameObject portraitFrame = CreateChild("XiangshuPortraitFrame", header.transform);
        CImage portraitFrameImage = AddSpriteImage(portraitFrame, new Color(0.42f, 0.25f, 0.13f, 0.82f), HeaderPortraitFrameSprite);
        portraitFrameImage.raycastTarget = false;
        portraitFrameImage.preserveAspect = true;
        _ = SetFixedLayoutSize(portraitFrame, HeaderPortraitSize, HeaderPortraitSize);

        GameObject portraitObject = CreateChild("XiangshuPortrait", portraitFrame.transform);
        RectTransform portraitRect = portraitObject.GetComponent<RectTransform>();
        StretchToParent(portraitRect);
        portraitRect.offsetMin = new Vector2(5f, 5f);
        portraitRect.offsetMax = new Vector2(-5f, -5f);
        CRawImage portrait = AddTextureImage(portraitObject, Color.white, HeaderPortraitTexturePath);
        portrait.raycastTarget = false;

        GameObject iconObject = CreateChild("XiangshuIcon", portraitFrame.transform);
        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(1f, 0f);
        iconRect.anchorMax = new Vector2(1f, 0f);
        iconRect.pivot = new Vector2(1f, 0f);
        iconRect.anchoredPosition = new Vector2(2f, -2f);
        CImage icon = AddSpriteImage(iconObject, new Color(0.92f, 0.61f, 0.24f, 1f), HeaderIconSprite);
        icon.raycastTarget = false;
        icon.preserveAspect = true;
        _ = SetFixedLayoutSize(iconObject, HeaderIconSize, HeaderIconSize);

        TextMeshProUGUI title = CreateText(
            "Title",
            header.transform,
            HeaderTitleFontSize,
            TextColor,
            FontStyles.Bold);
        title.text = "相枢";
        title.alignment = TextAlignmentOptions.MidlineLeft;
        LayoutElement titleLayout = title.gameObject.AddComponent<LayoutElement>();
        titleLayout.flexibleWidth = 1f;

        BuildReplyIndicator(header.transform);

        Button close = CreateSpriteSwapButton(
            "CloseButton",
            header.transform,
            HeaderCloseButtonSize,
            HeaderCloseButtonSize,
            HeaderCloseButtonNormalSprite,
            HeaderCloseButtonHighlightedSprite,
            HeaderCloseButtonDisabledSprite);
        close.onClick.AddListener(() => SetVisible(visible: false));

        HeaderDragHandle dragHandle = header.AddComponent<HeaderDragHandle>();
        dragHandle.Initialize(this);
    }

    private void BuildReplyIndicator(Transform parent)
    {
        GameObject indicator = CreateChild("ReplyIndicator", parent);
        _replyIndicator = indicator;
        _ = SetFixedLayoutSize(
            indicator,
            HeaderReplyIndicatorWidth,
            HeaderReplyIndicatorHeight);

        _replyIndicatorText = CreateText(
            "Label",
            indicator.transform,
            ReplyIndicatorFontSize,
            TextColor,
            FontStyles.Bold);
        _replyIndicatorText.text = "窸窣作响";
        _replyIndicatorText.alignment = TextAlignmentOptions.Center;
        StretchToParent(_replyIndicatorText.rectTransform);
        _replyIndicatorText.rectTransform.offsetMin = new Vector2(8f, 3f);
        _replyIndicatorText.rectTransform.offsetMax = new Vector2(-8f, -2f);

        indicator.SetActive(false);
    }

    private void BuildMessageArea(Transform parent)
    {
        GameObject scrollObject = CreateChild("Messages", parent);
        LayoutElement scrollLayout = scrollObject.AddComponent<LayoutElement>();
        scrollLayout.flexibleHeight = 1f;
        scrollLayout.minHeight = 360f;

        _ = AddSolidImage(scrollObject, MessageAreaColor);
        _scrollRect = scrollObject.AddComponent<ScrollRect>();
        _scrollRect.horizontal = false;
        _scrollRect.movementType = ScrollRect.MovementType.Clamped;
        _scrollRect.scrollSensitivity = 36f;

        GameObject viewport = CreateChild("Viewport", scrollObject.transform);
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        StretchToParent(viewportRect);
        viewportRect.offsetMax = new Vector2(-ScrollbarReservedWidth, 0f);
        CImage viewportImage = AddSolidImage(viewport, new Color(0f, 0f, 0f, 0.08f));
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
        _scrollRect.verticalScrollbar = BuildVerticalScrollbar(scrollObject.transform);
        _scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
    }

    private void BuildInputArea(Transform parent)
    {
        GameObject inputArea = CreateChild("InputArea", parent);
        LayoutElement inputAreaLayoutElement = inputArea.AddComponent<LayoutElement>();
        inputAreaLayoutElement.minHeight = InputAreaHeight;
        inputAreaLayoutElement.preferredHeight = InputAreaHeight;
        inputAreaLayoutElement.flexibleHeight = 0f;

        HorizontalLayoutGroup inputAreaLayout = inputArea.AddComponent<HorizontalLayoutGroup>();
        inputAreaLayout.childAlignment = TextAnchor.MiddleCenter;
        inputAreaLayout.childControlHeight = true;
        inputAreaLayout.childControlWidth = true;
        inputAreaLayout.childForceExpandHeight = false;
        inputAreaLayout.childForceExpandWidth = false;
        inputAreaLayout.padding = new RectOffset(0, 0, 4, 4);
        inputAreaLayout.spacing = 10f;

        GameObject inputObject = CreateInactiveChild("InputField", inputArea.transform);
        CImage inputImage = AddSolidImage(inputObject, InputColor);
        _inputFieldImage = inputImage;
        Outline inputBorder = inputObject.AddComponent<Outline>();
        inputBorder.effectColor = InputBorderColor;
        inputBorder.effectDistance = new Vector2(2f, -2f);
        LayoutElement inputLayout = inputObject.AddComponent<LayoutElement>();
        inputLayout.flexibleWidth = 1f;
        inputLayout.minHeight = InputFieldHeight;
        inputLayout.preferredHeight = InputFieldHeight;
        inputLayout.flexibleHeight = 0f;

        _inputField = inputObject.AddComponent<DisableHotkeyInputField>();
        _inputField.transition = Selectable.Transition.None;
        _inputField.lineType = TMP_InputField.LineType.MultiLineNewline;
        _inputField.characterLimit = 4000;
        _inputField.targetGraphic = inputImage;
        _inputField.customCaretColor = true;
        _inputField.caretColor = InputCaretColor;
        _inputField.caretWidth = 3;
        _inputField.caretBlinkRate = 0.8f;
        _inputField.selectionColor = new Color(0.8f, 0.55f, 0.25f, 0.24f);

        GameObject textViewport = CreateChild("TextViewport", inputObject.transform);
        RectTransform textViewportRect = textViewport.GetComponent<RectTransform>();
        textViewportRect.anchorMin = Vector2.zero;
        textViewportRect.anchorMax = Vector2.one;
        textViewportRect.offsetMin = new Vector2(12f, 7f);
        textViewportRect.offsetMax = new Vector2(-12f, -7f);
        _ = textViewport.AddComponent<RectMask2D>();

        TextMeshProUGUI inputText = CreateText(
            "Text",
            textViewport.transform,
            InputTextFontSize,
            TextColor,
            FontStyles.Normal);
        inputText.text = string.Empty;
        inputText.alignment = TextAlignmentOptions.TopLeft;
        StretchToParent(inputText.rectTransform);
        inputText.margin = Vector4.zero;

        _inputField.textViewport = textViewportRect;
        _inputField.textComponent = inputText;
        _inputField.onValueChanged.AddListener(_ => UpdateSendButtonState());
        UpdateInputFieldStateVisual();
        inputObject.SetActive(true);

        _sendButton = CreateButton("SendButton", inputArea.transform, "传念", SendButtonWidth, SendButtonHeight);
        _sendButton.onClick.AddListener(ActivateSendButton);
        _sendButtonText = _sendButton.GetComponentInChildren<TextMeshProUGUI>();
        _sendButtonImage = _sendButton.GetComponent<CImage>();
        UpdateSendButtonState();
    }

    private bool EnsureUiBuilt()
    {
        if (!EnsureAttachedToGameUi())
        {
            return false;
        }

        if (_uiBuilt)
        {
            return true;
        }

        CaptureGameTextStyle();
        BuildUi();
        ReplayVisibleMessages();
        _uiBuilt = true;
        return true;
    }

    private bool EnsureAttachedToGameUi()
    {
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

        Camera uiCamera = uiManager.UiCamera;

        if (uiCamera is null)
        {
            Log.Warning("chat window cannot build because UIManager has no UI camera");

            return false;
        }

        AttachToGameUiLayer(layer);
        ConfigureRootCanvas(uiCamera, layer);
        return true;
    }

    private void AttachToGameUiLayer(RectTransform layer)
    {
        bool shouldSyncLayer = transform.parent != layer
            || gameObject.layer != layer.gameObject.layer;

        if (transform.parent != layer)
        {
            transform.SetParent(layer, worldPositionStays: false);
            StretchToParent((RectTransform)transform);
            transform.SetAsLastSibling();
        }

        if (shouldSyncLayer)
        {
            SetLayerRecursively(transform, layer.gameObject.layer);
        }
    }

    private void ConfigureRootCanvas(
        Camera uiCamera,
        RectTransform gameUiLayer)
    {
        Canvas canvas = _rootCanvas
            ?? throw new InvalidOperationException("Xiangshu chat window root Canvas is not initialized.");

        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = uiCamera;
        canvas.overrideSorting = true;
        canvas.sortingOrder = CanvasSortingOrder;

        CanvasScaler scaler = _rootCanvasScaler
            ?? throw new InvalidOperationException("Xiangshu chat window root CanvasScaler is not initialized.");
        ConfigureRootCanvasScaler(scaler, gameUiLayer);

        ConchShipGraphicRaycaster raycaster = _rootRaycaster
            ?? throw new InvalidOperationException("Xiangshu chat window root raycaster is not initialized.");
        raycaster.enabled = true;
        raycaster.TargetCamera = uiCamera;
    }

    private static void ConfigureRootCanvasScaler(
        CanvasScaler scaler,
        RectTransform gameUiLayer)
    {
        CanvasScaler? gameScaler = gameUiLayer.GetComponentInParent<CanvasScaler>();

        if (gameScaler is not null)
        {
            CopyCanvasScalerSettings(gameScaler, scaler);
            return;
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = FallbackCanvasScaleFactor;
        scaler.referencePixelsPerUnit = DefaultReferencePixelsPerUnit;
    }

    private static void CopyCanvasScalerSettings(
        CanvasScaler source,
        CanvasScaler target)
    {
        target.uiScaleMode = source.uiScaleMode;
        target.referencePixelsPerUnit = source.referencePixelsPerUnit;
        target.scaleFactor = source.scaleFactor;
        target.referenceResolution = source.referenceResolution;
        target.screenMatchMode = source.screenMatchMode;
        target.matchWidthOrHeight = source.matchWidthOrHeight;
        target.physicalUnit = source.physicalUnit;
        target.fallbackScreenDPI = source.fallbackScreenDPI;
        target.defaultSpriteDPI = source.defaultSpriteDPI;
        target.dynamicPixelsPerUnit = source.dynamicPixelsPerUnit;
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

        _panelOffset = ClampPanelOffset(
            _panelOffset,
            parentSize,
            width,
            height,
            verticalMargin);
        _panelRect.anchoredPosition = _panelOffset;
        _panelRect.sizeDelta = new Vector2(width, height);
        ReflowMessageBubbles();
    }

    private void BeginPanelDrag(PointerEventData eventData)
    {
        if (!TryGetPointerLocalPosition(eventData, out Vector2 pointer))
        {
            return;
        }

        _panelDragStartPointer = pointer;
        _panelDragStartOffset = _panelOffset;
        _panelDragActive = true;
    }

    private void DragPanel(PointerEventData eventData)
    {
        if (!_panelDragActive || !TryGetPointerLocalPosition(eventData, out Vector2 pointer))
        {
            return;
        }

        _panelOffset = _panelDragStartOffset + pointer - _panelDragStartPointer;
        ApplyPanelLayout();
    }

    private void EndPanelDrag()
    {
        _panelDragActive = false;
    }

    private bool TryGetPointerLocalPosition(
        PointerEventData eventData,
        out Vector2 localPosition)
    {
        localPosition = default;

        if (_panelRect?.parent is not RectTransform parentRect)
        {
            return false;
        }

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect,
            eventData.position,
            eventData.pressEventCamera,
            out localPosition);
    }

    private Vector2 GetRootLayoutSize()
    {
        if (transform is RectTransform rootRect
            && rootRect.rect.width > 0f
            && rootRect.rect.height > 0f)
        {
            return rootRect.rect.size;
        }

        if (transform.parent is RectTransform parentRect)
        {
            return parentRect.rect.size;
        }

        return new Vector2(Screen.width, Screen.height);
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

    private static Vector2 ClampPanelOffset(
        Vector2 offset,
        Vector2 parentSize,
        float panelWidth,
        float panelHeight,
        float verticalMargin)
    {
        if (parentSize.x <= 0f || parentSize.y <= 0f)
        {
            return offset;
        }

        const float horizontalVisibleMargin = MinimumDraggedPanelVisibleMargin;
        float verticalVisibleMargin = Mathf.Min(
            MinimumDraggedPanelVisibleMargin,
            verticalMargin);
        float minimumX = (-parentSize.x * 0.5f) + horizontalVisibleMargin + (panelWidth * 0.5f);
        float maximumX = (parentSize.x * 0.5f) - horizontalVisibleMargin - (panelWidth * 0.5f);
        float minimumY = (-parentSize.y * 0.5f) + verticalVisibleMargin + (panelHeight * 0.5f);
        float maximumY = (parentSize.y * 0.5f) - verticalVisibleMargin - (panelHeight * 0.5f);

        return new Vector2(
            ClampOrCenter(offset.x, minimumX, maximumX),
            ClampOrCenter(offset.y, minimumY, maximumY));
    }

    private static float ClampOrCenter(
        float value,
        float minimum,
        float maximum)
    {
        return minimum <= maximum
            ? Mathf.Clamp(value, minimum, maximum)
            : (minimum + maximum) * 0.5f;
    }

    private void FocusInputField()
    {
        DisableHotkeyInputField? inputField = _inputField;
        if (_session?.RequiresReset == true
            || inputField?.gameObject.activeInHierarchy != true)
        {
            return;
        }

        EventSystem.current?.SetSelectedGameObject(inputField.gameObject);

        inputField.Select();
        inputField.ActivateInputField();
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
        if (!IsVisible
            || _session?.RequiresReset == true
            || inputField?.gameObject.activeInHierarchy != true)
        {
            return;
        }

        EventSystem.current?.SetSelectedGameObject(inputField.gameObject);

        inputField.ActivateInputField();
    }

    private void UpdateInputFieldStateVisual()
    {
        if (_inputFieldImage is { } inputFieldImage)
        {
            inputFieldImage.color = _session?.RequiresReset == true
                ? DisabledInputColor
                : InputColor;
        }
    }

    private void UpdateReplyIndicator()
    {
        GameObject? indicator = _replyIndicator;

        if (indicator is null)
        {
            return;
        }

        bool isReplying = _session?.IsReplying == true;

        if (indicator.activeSelf != isReplying)
        {
            indicator.SetActive(isReplying);
        }

        if (!isReplying)
        {
            return;
        }

        if (_replyIndicatorText is { } indicatorText)
        {
            const int dotCount = 3;
            const float dotStepSeconds = 0.34f;
            int dots = ((int)(Time.unscaledTime / dotStepSeconds) % dotCount) + 1;
            indicatorText.text = "窸窣作响" + new string('.', dots);
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
        GameObject child = new(name, typeof(RectTransform))
        {
            layer = parent.gameObject.layer,
        };
        child.transform.SetParent(parent, worldPositionStays: false);
        return child;
    }

    private static void SetLayerRecursively(
        Transform root,
        int layer)
    {
        root.gameObject.layer = layer;

        foreach (Transform child in root)
        {
            SetLayerRecursively(child, layer);
        }
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
        CImage image = AddSolidImage(buttonObject, ButtonColor);
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.15f, 1.08f, 0.94f, 1f);
        colors.pressedColor = new Color(0.82f, 0.72f, 0.58f, 1f);
        colors.disabledColor = Color.white;
        button.colors = colors;
        _ = SetFixedLayoutSize(buttonObject, width, height);

        TextMeshProUGUI text = CreateText(
            "Label",
            buttonObject.transform,
            ButtonLabelFontSize,
            TextColor,
            FontStyles.Bold);
        text.text = label;
        text.alignment = TextAlignmentOptions.Center;
        StretchToParent(text.rectTransform);
        return button;
    }

    private static Button CreateTransparentTextButton(
        string name,
        Transform parent,
        string label,
        float width,
        float height,
        float fontSize)
    {
        GameObject buttonObject = CreateChild(name, parent);
        CImage hitArea = AddSolidImage(buttonObject, Color.clear);
        GameObject hoverFrame = BuildHoverFrame(
            buttonObject,
            HeaderControlHoverFrameColor,
            HeaderControlHoverFrameThickness);
        ButtonHoverFrame buttonHoverFrame = buttonObject.AddComponent<ButtonHoverFrame>();
        buttonHoverFrame.Initialize(hoverFrame);

        Button button = buttonObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.targetGraphic = hitArea;
        _ = SetFixedLayoutSize(buttonObject, width, height);

        TextMeshProUGUI text = CreateText(
            "Label",
            buttonObject.transform,
            fontSize,
            TextColor,
            FontStyles.Bold);
        text.text = label;
        text.alignment = TextAlignmentOptions.Center;
        StretchToParent(text.rectTransform);
        return button;
    }

    private static CButton CreateSpriteSwapButton(
        string name,
        Transform parent,
        float width,
        float height,
        string normalSpriteName,
        string highlightedSpriteName,
        string disabledSpriteName)
    {
        GameObject buttonObject = CreateChild(name, parent);
        CImage image = AddSpriteImage(buttonObject, Color.white, normalSpriteName);
        image.preserveAspect = true;

        CButton button = buttonObject.AddComponent<CButton>();
        button.transition = Selectable.Transition.SpriteSwap;
        button.targetGraphic = image;
        _ = SetFixedLayoutSize(buttonObject, width, height);

        LoadButtonStateSprite(
            button,
            highlightedSpriteName,
            ApplyCloseButtonHighlightedSprite);
        LoadButtonStateSprite(
            button,
            disabledSpriteName,
            ApplyDisabledSprite);

        return button;
    }

    private static void LoadButtonStateSprite(
        CButton button,
        string spriteName,
        SpriteStateMutator mutate)
    {
        _ = AtlasInfo.Instance.GetSprite(
            spriteName,
            sprite =>
            {
                if (button == null || sprite == null)
                {
                    return;
                }

                SpriteState spriteState = button.spriteState;
                mutate(ref spriteState, sprite);
                button.spriteState = spriteState;
            });
    }

    private static void ApplyCloseButtonHighlightedSprite(ref SpriteState state, Sprite sprite)
    {
        state.highlightedSprite = sprite;
        state.selectedSprite = sprite;
        state.pressedSprite = sprite;
    }

    private static void ApplyDisabledSprite(ref SpriteState state, Sprite sprite)
    {
        state.disabledSprite = sprite;
    }

    private static GameObject BuildHoverFrame(
        GameObject target,
        Color color,
        float thickness)
    {
        GameObject frame = CreateChild("HoverFrame", target.transform);
        frame.AddComponent<LayoutElement>().ignoreLayout = true;
        StretchToParent(frame.GetComponent<RectTransform>());

        CreateBorderLine(frame.transform, "Top", color, true, 1f, thickness);
        CreateBorderLine(frame.transform, "Bottom", color, true, 0f, thickness);
        CreateBorderLine(frame.transform, "Left", color, false, 0f, thickness);
        CreateBorderLine(frame.transform, "Right", color, false, 1f, thickness);

        frame.SetActive(false);
        return frame;
    }

    private static void CreateBorderLine(
        Transform parent,
        string name,
        Color color,
        bool horizontal,
        float anchor,
        float thickness)
    {
        GameObject line = CreateChild(name, parent);
        RectTransform rect = line.GetComponent<RectTransform>();
        if (horizontal)
        {
            rect.anchorMin = new Vector2(0f, anchor);
            rect.anchorMax = new Vector2(1f, anchor);
            rect.pivot = new Vector2(0.5f, anchor);
            rect.sizeDelta = new Vector2(0f, thickness);
        }
        else
        {
            rect.anchorMin = new Vector2(anchor, 0f);
            rect.anchorMax = new Vector2(anchor, 1f);
            rect.pivot = new Vector2(anchor, 0.5f);
            rect.sizeDelta = new Vector2(thickness, 0f);
        }

        rect.anchoredPosition = Vector2.zero;
        CImage image = AddSolidImage(line, color);
        image.raycastTarget = false;
    }

    private static Scrollbar BuildVerticalScrollbar(Transform parent)
    {
        GameObject track = CreateChild("Scrollbar", parent);
        RectTransform trackRect = track.GetComponent<RectTransform>();
        trackRect.anchorMin = new Vector2(1f, 0f);
        trackRect.anchorMax = new Vector2(1f, 1f);
        trackRect.pivot = new Vector2(1f, 0.5f);
        trackRect.anchoredPosition = new Vector2(-ScrollbarRightInset, 0f);
        trackRect.sizeDelta = new Vector2(ScrollbarRailWidth, -(ScrollbarVerticalInset * 2f));
        CImage trackImage = AddSolidImage(track, ScrollbarTrackColor);
        trackImage.raycastTarget = true;

        Scrollbar scrollbar = track.AddComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scrollbar.transition = Selectable.Transition.None;

        GameObject slidingArea = CreateChild("SlidingArea", track.transform);
        RectTransform slidingAreaRect = slidingArea.GetComponent<RectTransform>();
        StretchToParent(slidingAreaRect);
        const float horizontalSlidingInset = (ScrollbarRailWidth - ScrollbarHandleWidth) * 0.5f;
        slidingAreaRect.offsetMin = new Vector2(horizontalSlidingInset, 5f);
        slidingAreaRect.offsetMax = new Vector2(-horizontalSlidingInset, -5f);

        GameObject handle = CreateChild("Handle", slidingArea.transform);
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        StretchToParent(handleRect);
        CImage handleImage = AddSolidImage(handle, ScrollbarHandleColor);
        handleImage.raycastTarget = true;

        GameObject handleMark = CreateChild("HandleMark", handle.transform);
        RectTransform handleMarkRect = handleMark.GetComponent<RectTransform>();
        handleMarkRect.anchorMin = new Vector2(0.5f, 0f);
        handleMarkRect.anchorMax = new Vector2(0.5f, 1f);
        handleMarkRect.pivot = new Vector2(0.5f, 0.5f);
        handleMarkRect.anchoredPosition = Vector2.zero;
        handleMarkRect.sizeDelta = new Vector2(ScrollbarHandleMarkWidth, 0f);
        CImage handleMarkImage = AddSpriteImage(handleMark, ScrollbarHandleMarkColor, ScrollbarHandleSprite);
        handleMarkImage.raycastTarget = false;
        handleMarkImage.preserveAspect = true;

        scrollbar.targetGraphic = handleImage;
        scrollbar.handleRect = handleRect;
        return scrollbar;
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
        GameObject child = new(name, typeof(RectTransform))
        {
            layer = parent.gameObject.layer,
        };
        child.SetActive(false);
        child.transform.SetParent(parent, worldPositionStays: false);
        return child;
    }

    private static CImage AddSolidImage(
        GameObject target,
        Color color)
    {
        CImage image = target.AddComponent<CImage>();
        image.color = color;
        return image;
    }

    private static CImage AddSpriteImage(
        GameObject target,
        Color color,
        string spriteName)
    {
        CImage image = target.AddComponent<CImage>();
        image.color = color;

        image.type = spriteName.StartsWith("ui9_back_", StringComparison.Ordinal)
            ? Image.Type.Sliced
            : Image.Type.Simple;
        image.SetEnabled(shouldBeEnabled: false);
        image.SetSprite(
            spriteName,
            onSpriteChange: () => UpdateSpriteImageEnabled(image));
        UpdateSpriteImageEnabled(image);

        return image;
    }

    private static void UpdateSpriteImageEnabled(CImage image)
    {
        if (image == null)
        {
            return;
        }

        image.SetEnabled(image.sprite != null);
    }

    private static CRawImage AddTextureImage(
        GameObject target,
        Color color,
        string texturePath)
    {
        CRawImage image = target.AddComponent<CRawImage>();
        image.color = color;
        image.enabled = false;

        ResLoader.Load<Texture2D>(
            texturePath,
            texture =>
            {
                if (image == null)
                {
                    return;
                }

                image.texture = texture;
                image.enabled = true;
            },
            path =>
            {
                if (image != null)
                {
                    image.texture = null;
                    image.enabled = false;
                }

                Log.Warning(
                    "chat window texture failed to load",
                    new
                    {
                        path,
                    });
            });

        return image;
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

    private sealed class ButtonHoverFrame : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        private GameObject? _frame;
        private bool _hovered;
        private bool _selected;

        public void Initialize(GameObject frame)
        {
            _frame = frame;
            UpdateVisibility();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovered = true;
            UpdateVisibility();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
            UpdateVisibility();
        }

        public void OnSelect(BaseEventData eventData)
        {
            _selected = true;
            UpdateVisibility();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            _selected = false;
            UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            _frame?.SetActive(_hovered || _selected);
        }
    }

    private sealed class HeaderDragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private XiangshuChatWindow? _window;

        public void Initialize(XiangshuChatWindow window)
        {
            _window = window;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _window?.BeginPanelDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            _window?.DragPanel(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _window?.EndPanelDrag();
        }
    }

    private readonly struct VisibilityState(
        CanvasGroup group,
        float alpha)
    {
        public CanvasGroup Group { get; } = group;

        public float Alpha { get; } = alpha;
    }

    private sealed class PlayerViewCaptureExclusionScope(List<VisibilityState> visibilityStates) : IDisposable
    {
        private List<VisibilityState>? _visibilityStates = visibilityStates;

        public void Dispose()
        {
            List<VisibilityState>? statesToRestore = _visibilityStates;
            if (statesToRestore is null)
            {
                return;
            }

            _visibilityStates = null;

            for (int index = statesToRestore.Count - 1; index >= 0; index--)
            {
                VisibilityState state = statesToRestore[index];
                if (state.Group is { } group)
                {
                    group.alpha = state.Alpha;
                }
            }
        }
    }
}
