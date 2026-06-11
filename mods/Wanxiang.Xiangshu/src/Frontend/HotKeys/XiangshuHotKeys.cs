using Game.Views.Bottom;
using HarmonyLib;
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
        Log.Info(
            "chat hotkey registered",
            new
            {
                hotkey = "Ctrl+Backslash",
            });
    }

    public static void PatchViewBottomUpdate(Harmony harmony)
    {
        _ = harmony
            .CreateClassProcessor(typeof(ViewBottomUpdatePatch))
            .Patch();
    }
}

internal static class FrontendHotkeyBridge
{
    private static readonly TaiwuLogger Log = TaiwuLogger.ForTag("Wanxiang.Xiangshu");

    private static FrontendPlugin? s_plugin;
    private static int s_lastHandledFrame = -1;

    public static void Attach(FrontendPlugin plugin)
    {
        s_plugin = plugin;
    }

    public static void Detach(FrontendPlugin plugin)
    {
        if (ReferenceEquals(s_plugin, plugin))
        {
            s_plugin = null;
        }
    }

    public static void OnViewBottomUpdate(ViewBottom viewBottom)
    {
        if (!viewBottom.Interactable)
        {
            return;
        }

        CheckAndLaunch();
    }

    private static void CheckAndLaunch()
    {
        FrontendPlugin? plugin = s_plugin;
        if (plugin is null)
        {
            return;
        }

        if (!plugin.IsChatWindowVisible && !CanTriggerInCurrentUi())
        {
            return;
        }

        if (XiangshuHotKeys.ToggleChat.Check(
                UIElement.Bottom,
                holdCheck: false,
                downCheck: false,
                isIgnoreBlockHotKey: plugin.IsChatWindowVisible,
                fnKeyCheckNone: false,
                isIgnoreElement: true))
        {
            if (s_lastHandledFrame == Time.frameCount)
            {
                return;
            }

            s_lastHandledFrame = Time.frameCount;
            Log.Info(
                "chat hotkey accepted",
                new
                {
                    frame = Time.frameCount,
                });
            plugin.ToggleChatWindow();
        }
    }

    private static bool CanTriggerInCurrentUi()
    {
        UIManager uiManager = UIManager.Instance;
        return uiManager.IsFocusElement(UIElement.StateMainWorld)
            || uiManager.IsFocusElement(UIElement.WorldMap)
            || uiManager.IsFocusElement(UIElement.Bottom);
    }
}

[HarmonyPatch(typeof(ViewBottom), "Update")]
internal static class ViewBottomUpdatePatch
{
    public static void Postfix(ViewBottom __instance)
    {
        FrontendHotkeyBridge.OnViewBottomUpdate(__instance);
    }
}
