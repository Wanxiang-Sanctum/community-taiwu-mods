using System;
using System.Diagnostics.CodeAnalysis;
using Cysharp.Threading.Tasks;
using FrameWork.UISystem.UIElements;
using Game.Views.MouseTips;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Wanxiang.Taiwu.Logging;
using Wanxiang.Taiwu.InstantNotifications;
using Wanxiang.Xiangshu.Frontend.ItemGrafts;

using CraftToolConfig = Config.CraftTool;
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

    private static string HeaderIconSprite => CraftToolConfig.DefValue.Medicine0.Icon;
    private static string HeaderIconBackgroundSprite => ItemView.GetGradeBack(CraftToolConfig.DefValue.Medicine0.Grade);
    private const string AssistantBubbleSprite = AiActionAreaNormal.bubbleBgInProcess;
    private const string UserBubbleSprite = AiActionAreaNormal.bubbleBgNotFinish;
    private const float PreferredPanelWidth = 860f;
    private const float PreferredPanelHeight = 820f;
    private const float MinimumPanelWidth = 640f;
    private const float PanelScreenMargin = 36f;
    private const float MinimumPanelScreenMargin = 12f;
    private const int PanelContentInset = 16;
    private const float PanelSectionSpacing = 12f;
    private const float HeaderIconFrameSize = 64f;
    private const int HeaderLeftPadding = 14;
    private const int HeaderRightPadding = 8;
    private const int HeaderVerticalPadding = 12;
    private const float HeaderHeight = HeaderIconFrameSize + (HeaderVerticalPadding * 2f);
    private const float HeaderIconInset = 7f;
    private const float HeaderResetButtonWidth = 64f;
    private const float HeaderResetButtonHeight = 40f;
    private const float HeaderReplyIndicatorWidth = 130f;
    private const float HeaderReplyIndicatorHeight = 40f;
    private const float HeaderCloseButtonSize = 56f;
    private const float HeaderItemSpacing = 12f;
    private const float ScrollbarReservedWidth = 30f;
    private const float ScrollbarRailWidth = 13f;
    private const float ScrollbarHandleWidth = 6f;
    private const float ScrollbarRightInset = 10f;
    private const float ScrollbarVerticalInset = 12f;
    private const float ScrollbarHandleMarkWidth = 8f;
    private const float MessageRowEdgePadding = 20f;
    private const float MessageRowOppositeGutter = 86f;
    private const float MessageBubbleWidthRatio = 0.94f;
    private const float MinimumMessageBubbleWidth = 280f;
    private const float PreferredMessageBubbleWidth = 620f;
    private const float MaximumMessageBubbleWidth = 640f;
    private const float MinimumMessageAreaHeight = 360f;
    private const int MessageContentVerticalPadding = 14;
    private const float MinimumDraggedPanelVisibleMargin = 8f;
    private const float InputVisibleLineCount = 3.5f;
    private const float InputLinePitch = 24.5f;
    private const float InputViewportHorizontalPadding = 12f;
    private const float InputViewportVerticalPadding = 7f;
    private const int InputAreaVerticalPadding = 4;
    private const float InputAreaSpacing = 10f;
    private const float InputFieldHeight =
        (InputLinePitch * InputVisibleLineCount) + (InputViewportVerticalPadding * 2f);
    private const float InputAreaHeight = InputFieldHeight + (InputAreaVerticalPadding * 2f);
    private const float SendButtonWidth = 80f;
    private const float SendButtonHeight = 76f;
    private const float MinimumPanelHeight =
        HeaderHeight
        + MinimumMessageAreaHeight
        + InputAreaHeight
        + (PanelSectionSpacing * 2f)
        + (PanelContentInset * 2f);
    private const float HeaderTitleFontSize = 26f;
    private const float ReplyIndicatorFontSize = 15f;
    private const float MessageSpeakerFontSize = 17f;
    private const float MessageBodyFontSize = 22f;
    private const float InputTextFontSize = 20f;
    private const float ButtonLabelFontSize = 20f;
    private const float HeaderControlFontSize = 17f;
    private const float HeaderControlHoverFrameThickness = 2f;
    private const int CanvasSortingOrder = 32000;
    private const string HostUnavailableButtonLabel = "离身";
    private const string HiddenAssistantMessageNotificationText = "钵中低语复起。";
    private const short HiddenAssistantMessageNotificationTemplateId = InstantNotificationConfig.DefKey.WalkThroughAbyss;
    private static readonly TaiwuLogger Log = TaiwuLogger.ForTag("Wanxiang.Xiangshu");
    private static readonly Color PanelColor = new(0.078f, 0.113f, 0.108f, 0.97f);
    private static readonly Color PanelOuterLineColor = new(0.020f, 0.029f, 0.027f, 0.98f);
    private static readonly Color MessageAreaColor = new(0.012f, 0.024f, 0.022f, 0.96f);
    private static readonly Color InputColor = new(0.056f, 0.040f, 0.030f, 0.96f);
    private static readonly Color DisabledInputColor = new(0.038f, 0.035f, 0.031f, 0.86f);
    private static readonly Color InputBorderColor = new(0.46f, 0.26f, 0.08f, 0.90f);
    private static readonly Color InputCaretColor = new(0.95f, 0.72f, 0.34f, 0.96f);
    private static readonly Color InputSelectionColor = new(0.82f, 0.56f, 0.25f, 0.24f);
    private static readonly Color AssistantBubbleColor = new(0.18f, 0.15f, 0.09f, 0.86f);
    private static readonly Color UserBubbleColor = new(0.045f, 0.14f, 0.15f, 0.88f);
    private static readonly Color AssistantBubbleOutlineColor = new(0.70f, 0.42f, 0.16f, 0.34f);
    private static readonly Color UserBubbleOutlineColor = new(0.25f, 0.58f, 0.60f, 0.34f);
    private static readonly Color AccentColor = new(0.82f, 0.59f, 0.28f, 1f);
    private static readonly Color UserSpeakerColor = new(0.72f, 0.88f, 0.90f, 1f);
    private static readonly Color TextColor = new(0.92f, 0.88f, 0.78f, 1f);
    private static readonly Color MutedTextColor = new(0.64f, 0.60f, 0.50f, 1f);
    private static readonly Color ButtonColor = new(0.20f, 0.105f, 0.035f, 0.98f);
    private static readonly Color InterruptButtonColor = new(0.30f, 0.125f, 0.035f, 0.98f);
    private static readonly Color DisabledButtonColor = new(0.075f, 0.064f, 0.052f, 0.88f);
    private static readonly Color HeaderControlHoverFrameColor = new(0.68f, 0.64f, 0.54f, 0.66f);
    private static readonly Color ScrollbarTrackColor = new(0.018f, 0.015f, 0.012f, 0.46f);
    private static readonly Color ScrollbarHandleColor = new(0.34f, 0.20f, 0.08f, 0.76f);
    private static readonly Color ScrollbarHandleMarkColor = new(0.83f, 0.48f, 0.18f, 0.95f);

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
    private string? _lastUiAttachFailureReason;

    public bool IsVisible { get; private set; }

    internal bool IsInputSelected
    {
        get
        {
            DisableHotkeyInputField? inputField = _inputField;
            return IsVisible
                && inputField?.gameObject.activeInHierarchy == true
                && EventSystem.current?.currentSelectedGameObject == inputField.gameObject;
        }
    }

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
            isUser ? UserSpeakerColor : AccentColor,
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
            isUser ? UserBubbleSprite : AssistantBubbleSprite,
            Image.Type.Sliced);
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
        columnRect.offsetMin = new Vector2(PanelContentInset, PanelContentInset);
        columnRect.offsetMax = new Vector2(-PanelContentInset, -PanelContentInset);

        VerticalLayoutGroup panelLayout = column.AddComponent<VerticalLayoutGroup>();
        panelLayout.childControlHeight = true;
        panelLayout.childControlWidth = true;
        panelLayout.childForceExpandHeight = false;
        panelLayout.childForceExpandWidth = true;
        panelLayout.spacing = PanelSectionSpacing;

        BuildHeader(column.transform);
        BuildMessageArea(column.transform);
        BuildInputArea(column.transform);
    }

    private void BuildHeader(Transform parent)
    {
        GameObject header = CreateChild("Header", parent);
        _ = AddSolidImage(header, Color.clear);
        _ = SetFixedLayoutHeight(header, HeaderHeight);

        HorizontalLayoutGroup headerLayout = header.AddComponent<HorizontalLayoutGroup>();
        headerLayout.childAlignment = TextAnchor.MiddleLeft;
        headerLayout.childControlHeight = true;
        headerLayout.childControlWidth = true;
        headerLayout.childForceExpandHeight = false;
        headerLayout.childForceExpandWidth = false;
        headerLayout.padding = new RectOffset(
            HeaderLeftPadding,
            HeaderRightPadding,
            HeaderVerticalPadding,
            HeaderVerticalPadding);
        headerLayout.spacing = HeaderItemSpacing;

        Button reset = CreateHeaderTextButton(
            "ResetButton",
            header.transform,
            "重置",
            HeaderResetButtonWidth,
            HeaderResetButtonHeight,
            HeaderControlFontSize);
        reset.onClick.AddListener(ResetChatSession);

        GameObject iconFrame = CreateChild("XiangshuIconFrame", header.transform);
        CImage iconBackground = AddSpriteImage(iconFrame, Color.white, HeaderIconBackgroundSprite);
        iconBackground.raycastTarget = false;
        iconBackground.preserveAspect = true;
        _ = SetFixedLayoutSize(iconFrame, HeaderIconFrameSize, HeaderIconFrameSize);

        GameObject iconObject = CreateChild("XiangshuIcon", iconFrame.transform);
        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        StretchToParent(iconRect);
        iconRect.offsetMin = new Vector2(HeaderIconInset, HeaderIconInset);
        iconRect.offsetMax = new Vector2(-HeaderIconInset, -HeaderIconInset);
        CImage icon = AddSpriteImage(iconObject, Color.white, HeaderIconSprite);
        icon.raycastTarget = false;
        icon.preserveAspect = true;

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

        CButton close = CreateCommonCloseButton(
            "CloseButton",
            header.transform);
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
            MutedTextColor,
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
        _ = SetFlexibleLayoutHeight(scrollObject, MinimumMessageAreaHeight);

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
        contentLayout.padding = new RectOffset(
            0,
            0,
            MessageContentVerticalPadding,
            MessageContentVerticalPadding);
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
        _ = SetFixedLayoutHeight(inputArea, InputAreaHeight);

        HorizontalLayoutGroup inputAreaLayout = inputArea.AddComponent<HorizontalLayoutGroup>();
        inputAreaLayout.childAlignment = TextAnchor.MiddleCenter;
        inputAreaLayout.childControlHeight = true;
        inputAreaLayout.childControlWidth = true;
        inputAreaLayout.childForceExpandHeight = false;
        inputAreaLayout.childForceExpandWidth = false;
        inputAreaLayout.padding = new RectOffset(
            0,
            0,
            InputAreaVerticalPadding,
            InputAreaVerticalPadding);
        inputAreaLayout.spacing = InputAreaSpacing;

        GameObject inputObject = CreateInactiveChild("InputField", inputArea.transform);
        CImage inputImage = AddSolidImage(inputObject, InputColor);
        _inputFieldImage = inputImage;
        Outline inputBorder = inputObject.AddComponent<Outline>();
        inputBorder.effectColor = InputBorderColor;
        inputBorder.effectDistance = new Vector2(2f, -2f);
        LayoutElement inputLayout = SetFixedLayoutHeight(inputObject, InputFieldHeight);
        inputLayout.flexibleWidth = 1f;

        _inputField = inputObject.AddComponent<DisableHotkeyInputField>();
        _inputField.transition = Selectable.Transition.None;
        _inputField.lineType = TMP_InputField.LineType.MultiLineNewline;
        _inputField.characterLimit = 4000;
        _inputField.targetGraphic = inputImage;
        _inputField.customCaretColor = true;
        _inputField.caretColor = InputCaretColor;
        _inputField.caretWidth = 3;
        _inputField.caretBlinkRate = 0.8f;
        _inputField.selectionColor = InputSelectionColor;

        GameObject textViewport = CreateChild("TextViewport", inputObject.transform);
        RectTransform textViewportRect = textViewport.GetComponent<RectTransform>();
        textViewportRect.anchorMin = Vector2.zero;
        textViewportRect.anchorMax = Vector2.one;
        textViewportRect.offsetMin = new Vector2(
            InputViewportHorizontalPadding,
            InputViewportVerticalPadding);
        textViewportRect.offsetMax = new Vector2(
            -InputViewportHorizontalPadding,
            -InputViewportVerticalPadding);
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
            return LogUiAttachFailure("ui-manager-unavailable");
        }

        RectTransform layer = uiManager.GetLayer(UILayer.LayerVeryTop);

        if (layer is null)
        {
            return LogUiAttachFailure("top-ui-layer-unavailable");
        }

        Camera uiCamera = uiManager.UiCamera;

        if (uiCamera is null)
        {
            return LogUiAttachFailure("ui-camera-unavailable");
        }

        _lastUiAttachFailureReason = null;
        AttachToGameUiLayer(layer);
        ConfigureRootCanvas(uiCamera, layer);
        return true;
    }

    private bool LogUiAttachFailure(string reason)
    {
        if (string.Equals(_lastUiAttachFailureReason, reason, StringComparison.Ordinal))
        {
            return false;
        }

        _lastUiAttachFailureReason = reason;
        Log.Warning(
            "太吾 UI 不可用，聊天窗口暂不能创建",
            new
            {
                reason,
            });
        return false;
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
        CanvasScaler gameScaler = gameUiLayer.GetComponentInParent<CanvasScaler>()
            ?? throw new InvalidOperationException("Taiwu UI layer is missing its CanvasScaler.");
        CopyCanvasScalerSettings(gameScaler, scaler);
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

        return Mathf.Min(PanelScreenMargin, Mathf.Max(MinimumPanelScreenMargin, availableSize * 0.025f));
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
            static ex => Log.Error(ex, "聊天输入框重新聚焦失败"));
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
            static ex => Log.Error(ex, "聊天窗口滚动到底部失败"));
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

    private static Button CreateHeaderTextButton(
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
            MutedTextColor,
            FontStyles.Bold);
        text.text = label;
        text.alignment = TextAlignmentOptions.Center;
        StretchToParent(text.rectTransform);
        return button;
    }

    private static CButton CreateCommonCloseButton(
        string name,
        Transform parent)
    {
        CButton sourceButton = GameUiResources.CommonCloseButton;
        Image sourceImage = sourceButton.targetGraphic as Image
            ?? throw new InvalidOperationException("Game common close button is missing an image target graphic.");
        if (sourceImage.sprite == null)
        {
            throw new InvalidOperationException("Game common close button image is missing its normal sprite.");
        }

        Vector2 buttonSize = FitWithin(
            GetSourceButtonSize(sourceButton),
            HeaderCloseButtonSize,
            HeaderCloseButtonSize);
        return CreateSpriteSwapButton(
            name,
            parent,
            buttonSize.x,
            buttonSize.y,
            sourceButton,
            sourceImage);
    }

    private static CButton CreateSpriteSwapButton(
        string name,
        Transform parent,
        float width,
        float height,
        CButton sourceButton,
        Image sourceImage)
    {
        GameObject buttonObject = CreateChild(name, parent);
        CImage image = AddSpriteImage(
            buttonObject,
            sourceImage.color,
            sourceImage.sprite,
            sourceImage.type);
        image.preserveAspect = sourceImage.preserveAspect;

        CButton button = buttonObject.AddComponent<CButton>();
        button.transition = Selectable.Transition.SpriteSwap;
        button.targetGraphic = image;
        button.spriteState = sourceButton.spriteState;
        button.colors = sourceButton.colors;
        button.animationTriggers = sourceButton.animationTriggers;
        _ = SetFixedLayoutSize(buttonObject, width, height);

        return button;
    }

    private static Vector2 GetSourceButtonSize(CButton sourceButton)
    {
        RectTransform sourceRect = sourceButton.GetComponent<RectTransform>()
            ?? throw new InvalidOperationException("Game common close button is missing its RectTransform.");

        Vector2 rectSize = sourceRect.rect.size;
        if (rectSize.x > 0f && rectSize.y > 0f)
        {
            return rectSize;
        }

        Vector2 sizeDelta = sourceRect.sizeDelta;
        if (sizeDelta.x > 0f && sizeDelta.y > 0f)
        {
            return sizeDelta;
        }

        throw new InvalidOperationException("Game common close button has no positive RectTransform size.");
    }

    private static Vector2 FitWithin(
        Vector2 size,
        float maximumWidth,
        float maximumHeight)
    {
        if (size.x <= maximumWidth && size.y <= maximumHeight)
        {
            return size;
        }

        float scale = Mathf.Min(maximumWidth / size.x, maximumHeight / size.y);
        return size * scale;
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
        CImage handleMarkImage = AddSpriteImage(
            handleMark,
            ScrollbarHandleMarkColor,
            GameUiResources.ScrollbarHandleMarkName);
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

        LayoutElement layout = GetOrAddLayoutElement(target);
        layout.minWidth = width;
        layout.preferredWidth = width;
        layout.flexibleWidth = 0f;
        layout.minHeight = height;
        layout.preferredHeight = height;
        layout.flexibleHeight = 0f;
        return layout;
    }

    private static LayoutElement SetFixedLayoutHeight(
        GameObject target,
        float height)
    {
        LayoutElement layout = GetOrAddLayoutElement(target);
        layout.minHeight = height;
        layout.preferredHeight = height;
        layout.flexibleHeight = 0f;
        return layout;
    }

    private static LayoutElement SetFlexibleLayoutHeight(
        GameObject target,
        float minimumHeight)
    {
        LayoutElement layout = GetOrAddLayoutElement(target);
        layout.minHeight = minimumHeight;
        layout.preferredHeight = minimumHeight;
        layout.flexibleHeight = 1f;
        return layout;
    }

    private static LayoutElement GetOrAddLayoutElement(GameObject target)
    {
        return target.GetComponent<LayoutElement>()
            ?? target.AddComponent<LayoutElement>();
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
        Image.Type imageType = spriteName.StartsWith("ui9_back_", StringComparison.Ordinal)
            ? Image.Type.Sliced
            : Image.Type.Simple;
        return AddSpriteImage(target, color, spriteName, imageType);
    }

    private static CImage AddSpriteImage(
        GameObject target,
        Color color,
        string spriteName,
        Image.Type imageType)
    {
        CImage image = target.AddComponent<CImage>();
        image.color = color;
        image.type = imageType;
        image.SetEnabled(shouldBeEnabled: false);
        image.SetSprite(
            spriteName,
            onSpriteChange: () => UpdateSpriteImageEnabled(image));
        UpdateSpriteImageEnabled(image);

        return image;
    }

    private static CImage AddSpriteImage(
        GameObject target,
        Color color,
        Sprite sprite,
        Image.Type imageType)
    {
        CImage image = target.AddComponent<CImage>();
        image.color = color;
        image.type = imageType;
        image.sprite = sprite;
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

    private static void ApplyGameTextStyle(TextMeshProUGUI text)
    {
        GameTextStyle style = GameUiResources.CommonTextStyle;

        text.font = style.Font;

        if (style.FontMaterial is not null)
        {
            text.fontSharedMaterial = style.FontMaterial;
        }

        if (style.SpriteAsset is not null)
        {
            text.spriteAsset = style.SpriteAsset;
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
