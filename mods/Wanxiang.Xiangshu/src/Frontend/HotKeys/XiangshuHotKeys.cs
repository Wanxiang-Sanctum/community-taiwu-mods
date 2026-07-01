using System;
using FrameWork.UISystem.UIElements;
using UnityEngine;
using Wanxiang.Taiwu.Logging;

namespace Wanxiang.Xiangshu.Frontend.HotKeys;

internal static class XiangshuHotKeys
{
    /// <summary>
    /// Preferred stable id for player custom hotkey persistence.
    /// </summary>
    private const byte PreferredToggleChatCommandId = 101;

    private static readonly TaiwuLogger Log = TaiwuLogger.ForTag("Wanxiang.Xiangshu");

    internal static HotKeyCommand? ToggleChat { get; private set; }

    public static void RegisterWithGameCommandKit()
    {
        CommandKitBase kit = CommandKitBase.MapCommandKit;

        if (ToggleChat is not null
            && kit.GroupCommand.Any(command => ReferenceEquals(command, ToggleChat)))
        {
            return;
        }

        byte? commandId = TryAllocateToggleChatCommandId(kit);
        if (commandId is null)
        {
            Log.Error(
                "MapCommandKit 没有可用命令 id，聊天热键注册失败",
                new
                {
                    preferredCommandId = PreferredToggleChatCommandId,
                });
            return;
        }

        if (commandId != PreferredToggleChatCommandId)
        {
            Log.Warning(
                "首选聊天热键命令 id 已被占用，已使用备用命令 id 注册",
                new
                {
                    preferredCommandId = PreferredToggleChatCommandId,
                    commandId,
                });
        }

        HotKeyCommand toggleChat = CreateToggleChatCommand(commandId.Value);
        ToggleChat = toggleChat;
        kit.GroupCommand =
        [
            .. kit.GroupCommand,
            toggleChat,
        ];
        CommandKitBase.Init();
    }

    private static HotKeyCommand CreateToggleChatCommand(byte commandId)
    {
        return new HotKeyCommand(
            commandId,
            LanguageKey.LK_Mod,
            KeyCode.Backslash,
            KeyCode.LeftControl);
    }

    private static byte? TryAllocateToggleChatCommandId(CommandKitBase kit)
    {
        if (!kit.GroupCommand.Any(command => command.Id == PreferredToggleChatCommandId))
        {
            return PreferredToggleChatCommandId;
        }

        for (int commandId = byte.MaxValue; commandId > 0; commandId--)
        {
            byte candidate = (byte)commandId;
            if (candidate != PreferredToggleChatCommandId
                && kit.GroupCommand.All(command => command.Id != candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    public static bool CanOpenChatInCurrentUi()
    {
        return GameApp.Instance is not null
            && GameApp.Instance.GetCurrentGameStateName() == EGameState.InGame
            && UIManager.Instance is not null;
    }

    public static bool IsToggleChatPressed(bool ignoreChatInputFocus)
    {
        HotKeyCommand? toggleChat = ToggleChat;
        if (toggleChat is null)
        {
            return false;
        }

        if (!ignoreChatInputFocus)
        {
            return toggleChat.Check(
                UIElement.Bottom,
                holdCheck: false,
                downCheck: true,
                isIgnoreBlockHotKey: false,
                fnKeyCheckNone: false,
                isIgnoreElement: true);
        }

        if (UIManager.Instance?.BlockHotKey == true)
        {
            return false;
        }

        HotKeyGroup keyGroup = toggleChat.KeyGroup;
        return IsKeyDown(keyGroup.Key, keyGroup.FunctionKey, checkIsFunctionKey: true)
            || IsKeyDown(keyGroup.MouseKey, keyGroup.FunctionMouseKey, checkIsFunctionKey: false);
    }

    private static bool IsKeyDown(
        KeyCode key,
        KeyCode functionKey,
        bool checkIsFunctionKey)
    {
        if (key == KeyCode.None)
        {
            return false;
        }

        bool keyDown = IsTriggerKeyDown(key);
        if (checkIsFunctionKey && IsModifierKey(key))
        {
            return keyDown;
        }

        return IsFunctionKeyPressed(functionKey) && keyDown;
    }

    private static bool IsTriggerKeyDown(KeyCode key)
    {
        if (IsMouseScrollKey(key))
        {
            if (CScrollRect.IsPointerOverAnyCScrollRect)
            {
                return false;
            }

            float scrollY = Input.mouseScrollDelta.y;
            return key == MapCommandKit.ViewScrollUp.KeyGroup.Key
                ? scrollY > 0.5f
                : scrollY < -0.5f;
        }

        return Input.GetKeyDown(key);
    }

    private static bool IsMouseScrollKey(KeyCode key)
    {
        return key == MapCommandKit.ViewScrollUp.KeyGroup.Key
            || key == MapCommandKit.ViewScrollDown.KeyGroup.Key;
    }

    private static bool IsFunctionKeyPressed(KeyCode key)
    {
        if (key == KeyCode.None)
        {
            return true;
        }

        if (key is KeyCode.LeftControl or KeyCode.RightControl)
        {
            return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        }

        if (key is KeyCode.LeftShift or KeyCode.RightShift)
        {
            return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        }

        if (key is KeyCode.LeftAlt or KeyCode.RightAlt)
        {
            return Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
        }

        return Input.GetKey(key);
    }

    private static bool IsModifierKey(KeyCode key)
    {
        return key is KeyCode.LeftControl
            or KeyCode.RightControl
            or KeyCode.LeftShift
            or KeyCode.RightShift
            or KeyCode.LeftAlt
            or KeyCode.RightAlt;
    }
}

internal sealed class FrontendHotkeyDriver : IDisposable
{
    private readonly YieldHelper _yieldHelper;
    private FrontendPlugin? _plugin;
    private bool _disposed;

    public static FrontendHotkeyDriver Create(FrontendPlugin plugin)
    {
        YieldHelper yieldHelper = SingletonObject.getInstance<YieldHelper>();
        FrontendHotkeyDriver driver = new(plugin, yieldHelper);
        yieldHelper.StartUpdate(driver.Update);
        return driver;
    }

    private FrontendHotkeyDriver(
        FrontendPlugin plugin,
        YieldHelper yieldHelper)
    {
        _yieldHelper = yieldHelper;
        _plugin = plugin;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _plugin = null;
        _yieldHelper.StopUpdate(Update);
    }

    private void Update(float _)
    {
        if (_disposed)
        {
            return;
        }

        FrontendPlugin? plugin = _plugin;
        if (plugin is null)
        {
            return;
        }

        bool chatWindowVisible = plugin.IsChatWindowVisible;
        if (!chatWindowVisible && !XiangshuHotKeys.CanOpenChatInCurrentUi())
        {
            return;
        }

        if (!XiangshuHotKeys.IsToggleChatPressed(ignoreChatInputFocus: plugin.IsChatInputSelected))
        {
            return;
        }

        plugin.ToggleChatWindow();
    }
}
