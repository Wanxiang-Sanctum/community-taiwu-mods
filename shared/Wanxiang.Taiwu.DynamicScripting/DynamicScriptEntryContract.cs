namespace Wanxiang.Taiwu.DynamicScripting;

/// <summary>
/// 描述由 Mod 专用适配层拥有的脚本入口契约。
/// </summary>
public sealed class DynamicScriptEntryContract
{
    /// <summary>
    /// 初始化 <see cref="DynamicScriptEntryContract"/> 类的新实例。
    /// </summary>
    /// <param name="entryTypeFullName">必需的公开静态脚本入口类型完整名。</param>
    /// <param name="scriptGlobalsType">入口方法唯一参数要求的精确 globals 类型。</param>
    public DynamicScriptEntryContract(
        string entryTypeFullName,
        Type scriptGlobalsType)
    {
#if NET6_0_OR_GREATER
        ArgumentException.ThrowIfNullOrWhiteSpace(entryTypeFullName);
        ArgumentNullException.ThrowIfNull(scriptGlobalsType);
#else
        if (string.IsNullOrWhiteSpace(entryTypeFullName))
        {
            throw new ArgumentException("Entry type full name is required.", nameof(entryTypeFullName));
        }

        if (scriptGlobalsType is null)
        {
            throw new ArgumentNullException(nameof(scriptGlobalsType));
        }
#endif

        EntryTypeFullName = entryTypeFullName;
        ScriptGlobalsType = scriptGlobalsType;
    }

    /// <summary>
    /// 获取必需的公开静态脚本入口类型完整名。
    /// </summary>
    public string EntryTypeFullName { get; }

    /// <summary>
    /// 获取入口方法唯一参数要求的精确 globals 类型。
    /// </summary>
    public Type ScriptGlobalsType { get; }
}
