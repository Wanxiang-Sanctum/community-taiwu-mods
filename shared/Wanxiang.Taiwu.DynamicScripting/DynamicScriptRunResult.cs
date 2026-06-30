namespace Wanxiang.Taiwu.DynamicScripting;

/// <summary>
/// 表示编译并尝试调用动态脚本后的结果。
/// </summary>
public abstract class DynamicScriptRunResult
{
    /// <summary>
    /// 创建带有 JSON 编码返回值的已调用结果。
    /// </summary>
    /// <param name="returnValueJson">JSON 编码后的返回值。</param>
    /// <returns>已调用结果。</returns>
    public static DynamicScriptRunResult InvokedWithReturnValue(string returnValueJson)
    {
        return new DynamicScriptInvokedResult(
            new DynamicScriptReturnValueOutcome(returnValueJson));
    }

    /// <summary>
    /// 创建表示入口方法未被调用的脚本结果。
    /// </summary>
    /// <param name="reason">未发生调用的原因。</param>
    /// <param name="details">可选失败诊断信息。</param>
    /// <returns>未调用结果。</returns>
    public static DynamicScriptRunResult NotInvoked(
        string reason,
        DynamicScriptNotInvokedDetails? details = null)
    {
        return new DynamicScriptNotInvokedResult(reason, details);
    }

    /// <summary>
    /// 创建表示入口方法抛出异常或已取消的已调用结果。
    /// </summary>
    /// <param name="message">异常或取消消息。</param>
    /// <returns>已调用结果。</returns>
    public static DynamicScriptRunResult InvokedWithException(string message)
    {
        return new DynamicScriptInvokedResult(
            new DynamicScriptExceptionOutcome(message));
    }

    private protected DynamicScriptRunResult()
    {
    }
}
