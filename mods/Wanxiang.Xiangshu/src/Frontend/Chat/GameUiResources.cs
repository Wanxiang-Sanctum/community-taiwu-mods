using System.IO;
using FrameWork.UISystem.UIElements;
using Game.Views.CharacterMenu;
using UnityEngine;
using Wanxiang.Taiwu.Logging;

namespace Wanxiang.Xiangshu.Frontend.Chat;

internal static class GameUiResources
{
    private static readonly TaiwuLogger Log = TaiwuLogger.ForTag("Wanxiang.Xiangshu");

    private static CButton? s_commonCloseButton;
    private static string? s_scrollbarHandleMarkName;

    internal static CButton? CommonCloseButton => LoadCommonCloseButton();

    internal static string? ScrollbarHandleMarkName => LoadScrollbarHandleMarkName();

    private static CButton? LoadCommonCloseButton()
    {
        if (s_commonCloseButton != null)
        {
            return s_commonCloseButton;
        }

        GameObject? prefab = LoadPrefab(UIElement.CharacterMenu);
        if (prefab == null)
        {
            return null;
        }

        ViewCharacterMenu view = prefab.GetComponent<ViewCharacterMenu>();
        if (view == null)
        {
            Log.Warning("ViewCharacterMenu prefab is missing ViewCharacterMenu component.");
            return null;
        }

        s_commonCloseButton = view.closeButton;
        return s_commonCloseButton;
    }

    private static GameObject? LoadPrefab(UIElement element)
    {
        string prefabPath = Path.Combine(UIElement.rootPrefabPath, element._path);
        GameObject prefab = ResLoader.SyncLoad<GameObject>(prefabPath);
        if (prefab == null)
        {
            Log.Warning("Unable to load UI prefab.", new { prefabPath });
        }

        return prefab;
    }

#pragma warning disable CS0612 // CScrollbarLegacy exposes the game's serialized normal handle mark.
    private static string? LoadScrollbarHandleMarkName()
    {
        if (!string.IsNullOrEmpty(s_scrollbarHandleMarkName))
        {
            return s_scrollbarHandleMarkName;
        }

        GameObject? prefab = LoadPrefab(UIElement.SystemOption);
        if (prefab == null)
        {
            return null;
        }

        CScrollbarLegacy scrollbar = prefab.GetComponentInChildren<CScrollbarLegacy>(includeInactive: true);
        if (scrollbar == null || string.IsNullOrEmpty(scrollbar.NormalHandleMark))
        {
            Log.Warning("SystemOption prefab is missing a normal scrollbar handle mark.");
            return null;
        }

        s_scrollbarHandleMarkName = scrollbar.NormalHandleMark;
        return s_scrollbarHandleMarkName;
    }
#pragma warning restore CS0612
}
