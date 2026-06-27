using System;
using System.IO;
using FrameWork.UISystem.UIElements;
using Game.Views.CharacterMenu;
using TMPro;
using UnityEngine;

namespace Wanxiang.Xiangshu.Frontend.Chat;

internal static class GameUiResources
{
    private static ViewCharacterMenu? s_characterMenuView;
    private static CButton? s_commonCloseButton;
    private static GameTextStyle? s_commonTextStyle;
    private static string? s_scrollbarHandleMarkName;

    internal static CButton CommonCloseButton => LoadCommonCloseButton();

    internal static GameTextStyle CommonTextStyle => LoadCommonTextStyle();

    internal static string ScrollbarHandleMarkName => LoadScrollbarHandleMarkName();

    private static CButton LoadCommonCloseButton()
    {
        if (s_commonCloseButton != null)
        {
            return s_commonCloseButton;
        }

        ViewCharacterMenu view = LoadCharacterMenuView();
        if (view.closeButton == null)
        {
            throw new InvalidOperationException("ViewCharacterMenu prefab is missing its close button.");
        }

        s_commonCloseButton = view.closeButton;
        return s_commonCloseButton;
    }

    private static GameTextStyle LoadCommonTextStyle()
    {
        if (s_commonTextStyle != null)
        {
            return s_commonTextStyle;
        }

        ViewCharacterMenu view = LoadCharacterMenuView();
        TextMeshProUGUI sourceText = view.subPageTitle
            ?? throw new InvalidOperationException("ViewCharacterMenu prefab is missing its sub page title text.");
        if (sourceText.font == null)
        {
            throw new InvalidOperationException("ViewCharacterMenu sub page title is missing its font asset.");
        }

        s_commonTextStyle = new GameTextStyle(
            sourceText.font,
            sourceText.fontSharedMaterial,
            sourceText.spriteAsset);
        return s_commonTextStyle;
    }

    private static ViewCharacterMenu LoadCharacterMenuView()
    {
        if (s_characterMenuView != null)
        {
            return s_characterMenuView;
        }

        GameObject prefab = LoadPrefab(UIElement.CharacterMenu);
        s_characterMenuView = prefab.GetComponent<ViewCharacterMenu>()
            ?? throw new InvalidOperationException("ViewCharacterMenu prefab is missing ViewCharacterMenu component.");
        return s_characterMenuView;
    }

    private static GameObject LoadPrefab(UIElement element)
    {
        string prefabPath = Path.Combine(UIElement.rootPrefabPath, element._path);
        return ResLoader.SyncLoad<GameObject>(prefabPath)
            ?? throw new InvalidOperationException($"Unable to load UI prefab: {prefabPath}");
    }

#pragma warning disable CS0612 // CScrollbarLegacy exposes the game's serialized normal handle mark.
    private static string LoadScrollbarHandleMarkName()
    {
        if (!string.IsNullOrEmpty(s_scrollbarHandleMarkName))
        {
            return s_scrollbarHandleMarkName;
        }

        GameObject prefab = LoadPrefab(UIElement.SystemOption);

        CScrollbarLegacy scrollbar = prefab.GetComponentInChildren<CScrollbarLegacy>(includeInactive: true)
            ?? throw new InvalidOperationException("SystemOption prefab is missing a legacy scrollbar.");
        if (string.IsNullOrEmpty(scrollbar.NormalHandleMark))
        {
            throw new InvalidOperationException("SystemOption legacy scrollbar is missing its normal handle mark.");
        }

        s_scrollbarHandleMarkName = scrollbar.NormalHandleMark;
        return s_scrollbarHandleMarkName;
    }
#pragma warning restore CS0612
}

internal sealed class GameTextStyle
{
    internal GameTextStyle(
        TMP_FontAsset font,
        Material? fontMaterial,
        TMP_SpriteAsset? spriteAsset)
    {
        Font = font;
        FontMaterial = fontMaterial;
        SpriteAsset = spriteAsset;
    }

    internal TMP_FontAsset Font { get; }

    internal Material? FontMaterial { get; }

    internal TMP_SpriteAsset? SpriteAsset { get; }
}
