using System;
using UnityEngine;
using Wanxiang.Taiwu.Logging;

namespace Wanxiang.Xiangshu.Frontend.HotKeys;

internal static class XiangshuHotKeys
{
    private const byte ToggleChatCommandId = 101;

    private static readonly TaiwuLogger Log = TaiwuLogger.ForTag("Wanxiang.Xiangshu");

    internal static readonly HotKeyCommand ToggleChat = new(
        ToggleChatCommandId,
        LanguageKey.LK_Mod,
        KeyCode.Backslash,
        KeyCode.LeftControl);

    public static void RegisterWithGameCommandKit()
    {
        CommandKitBase kit = CommandKitBase.MapCommandKit;

        if (kit.GroupCommand.Any(command => ReferenceEquals(command, ToggleChat)))
        {
            return;
        }

        if (kit.GroupCommand.Any(command => command.Id == ToggleChatCommandId))
        {
            Log.Warning(
                "cannot register chat hotkey in MapCommandKit because command id is already used",
                new
                {
                    commandId = ToggleChatCommandId,
                    fallbackHotkey = "Ctrl+Backslash",
                });
            return;
        }

        kit.GroupCommand =
        [
            .. kit.GroupCommand,
            ToggleChat,
        ];
        CommandKitBase.Init();
    }

    public static bool CanOpenChatInCurrentUi()
    {
        UIManager uiManager = UIManager.Instance;
        if (uiManager is null)
        {
            return false;
        }

        return uiManager.IsFocusElement(UIElement.StateMainWorld)
            || uiManager.IsFocusElement(UIElement.WorldMap)
            || uiManager.IsFocusElement(UIElement.Bottom);
    }

    public static bool IsToggleChatPressed(bool ignoreBlockHotKey)
    {
        return ToggleChat.Check(
            UIElement.Bottom,
            holdCheck: false,
            downCheck: true,
            isIgnoreBlockHotKey: ignoreBlockHotKey,
            fnKeyCheckNone: false,
            isIgnoreElement: true);
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

        if (!XiangshuHotKeys.IsToggleChatPressed(ignoreBlockHotKey: chatWindowVisible))
        {
            return;
        }

        plugin.ToggleChatWindow();
    }
}
