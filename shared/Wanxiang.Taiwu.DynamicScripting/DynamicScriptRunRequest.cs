namespace Wanxiang.Taiwu.DynamicScripting;

/// <summary>
/// 描述一次动态脚本执行请求。
/// </summary>
/// <param name="script">要编译并执行的完整 C# 编译单元。</param>
/// <param name="entryThread">用于调用入口方法的宿主线程。</param>
public sealed class DynamicScriptRunRequest(
    string script,
    DynamicScriptEntryThread entryThread = DynamicScriptEntryThread.Current)
{
    /// <summary>
    /// 获取要编译并执行的完整 C# 编译单元。
    /// </summary>
    public string Script { get; } = script ?? throw new ArgumentNullException(nameof(script));

    /// <summary>
    /// 获取用于调用入口方法的宿主线程。
    /// </summary>
    public DynamicScriptEntryThread EntryThread { get; } =
        ValidateEntryThread(entryThread);

    private static DynamicScriptEntryThread ValidateEntryThread(
        DynamicScriptEntryThread entryThread)
    {
        return entryThread is DynamicScriptEntryThread.Current
                or DynamicScriptEntryThread.MainThread
            ? entryThread
            : throw new ArgumentOutOfRangeException(
                nameof(entryThread),
                entryThread,
                "Unsupported script entry thread.");
    }
}
