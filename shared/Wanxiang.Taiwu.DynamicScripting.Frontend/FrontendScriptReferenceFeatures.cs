namespace Wanxiang.Taiwu.DynamicScripting.Frontend;

/// <summary>
/// Frontend runtime features that may require explicit dynamic script references.
/// </summary>
[Flags]
public enum FrontendScriptReferenceFeatures
{
    /// <summary>
    /// Do not expose frontend-only dynamic script references.
    /// </summary>
    None = 0,

    /// <summary>
    /// Expose Cysharp UniTask so scripts can await frontend UniTask APIs inside a Task entry method.
    /// </summary>
    UniTask = 1,
}
