using HarmonyLib;
using UnityEngine;

namespace Wanxiang.Xiangshu.Frontend;

#pragma warning disable CS0612 // UI_Bottom is still the current main interface update hook in the decompiled frontend.

internal static class XiangshuHotKeys
{
    private const byte LaunchAgentDiagnosticCommandId = 100;

    internal static readonly HotKeyCommand LaunchAgentDiagnostic = new(
        LaunchAgentDiagnosticCommandId,
        LanguageKey.LK_Mod,
        KeyCode.F10,
        KeyCode.LeftControl);

    public static void RegisterWithGameCommandKit()
    {
        CommandKitBase kit = CommandKitBase.MapCommandKit;

        if (kit.GroupCommand.Any(command => ReferenceEquals(command, LaunchAgentDiagnostic)))
        {
            return;
        }

        if (kit.GroupCommand.Any(command => command.Id == LaunchAgentDiagnosticCommandId))
        {
            throw new InvalidOperationException(
                $"Cannot register Wanxiang.Xiangshu hotkey command because MapCommandKit command id {LaunchAgentDiagnosticCommandId} is already used.");
        }

        kit.GroupCommand =
        [
            .. kit.GroupCommand,
            LaunchAgentDiagnostic,
        ];
        CommandKitBase.Init();
    }
}

internal static class FrontendHotkeyBridge
{
    private static FrontendPlugin? s_plugin;

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

    public static void OnUiBottomUpdate(UI_Bottom uiBottom)
    {
        FrontendPlugin? plugin = s_plugin;

        if (plugin is null)
        {
            return;
        }

        UIElement element = uiBottom.Element;

        if (element is null)
        {
            return;
        }

        if (XiangshuHotKeys.LaunchAgentDiagnostic.Check(
                element,
                holdCheck: false,
                downCheck: false,
                isIgnoreBlockHotKey: false,
                fnKeyCheckNone: false))
        {
            plugin.LaunchAgentDiagnostic();
        }
    }
}

[HarmonyPatch(typeof(UI_Bottom), "Update")]
internal static class UiBottomUpdatePatch
{
    public static void Postfix(UI_Bottom __instance)
    {
        FrontendHotkeyBridge.OnUiBottomUpdate(__instance);
    }
}

#pragma warning restore CS0612
