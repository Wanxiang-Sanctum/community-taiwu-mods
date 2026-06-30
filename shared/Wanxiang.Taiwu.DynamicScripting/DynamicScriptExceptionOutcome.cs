namespace Wanxiang.Taiwu.DynamicScripting;

/// <summary>
/// 表示已抛出异常或已取消的脚本入口方法。
/// </summary>
/// <param name="message">异常或取消消息。</param>
public sealed class DynamicScriptExceptionOutcome(
    string message) : DynamicScriptInvocationOutcome
{
    /// <summary>
    /// 获取异常或取消消息。
    /// </summary>
    public string Message { get; } =
        message ?? throw new ArgumentNullException(nameof(message));
}
