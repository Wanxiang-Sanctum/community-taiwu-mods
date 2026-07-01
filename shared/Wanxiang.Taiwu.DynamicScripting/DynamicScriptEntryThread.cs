namespace Wanxiang.Taiwu.DynamicScripting;

/// <summary>
/// 选择用于调用已编译脚本入口方法的宿主线程。
/// </summary>
public enum DynamicScriptEntryThread
{
    /// <summary>
    /// 在当前调用方线程上调用入口方法。
    /// </summary>
    Current = 0,

    /// <summary>
    /// 在宿主主线程上调用入口方法。
    /// </summary>
    MainThread = 1,
}
