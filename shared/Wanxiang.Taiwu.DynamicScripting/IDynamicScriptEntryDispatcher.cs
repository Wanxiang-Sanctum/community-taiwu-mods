namespace Wanxiang.Taiwu.DynamicScripting;

/// <summary>
/// 将动态脚本入口调用分派到宿主拥有的线程策略。
/// </summary>
public interface IDynamicScriptEntryDispatcher
{
    /// <summary>
    /// 按请求的线程策略调用给定入口委托。
    /// </summary>
    /// <param name="invokeEntry">调用脚本入口方法的委托。</param>
    /// <param name="entryThread">请求的入口调用线程。</param>
    /// <param name="cancellationToken">分派操作使用的取消令牌。</param>
    /// <returns>脚本入口方法产生的原始返回值。</returns>
    Task<object?> InvokeAsync(
        Func<object?> invokeEntry,
        DynamicScriptEntryThread entryThread,
        CancellationToken cancellationToken);
}
