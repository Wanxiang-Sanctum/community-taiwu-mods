namespace Wanxiang.Taiwu.DynamicScripting;

/// <summary>
/// 表示未进入入口方法的脚本。
/// </summary>
/// <param name="reason">未发生调用的原因。</param>
/// <param name="details">可选失败诊断信息。</param>
public sealed class DynamicScriptNotInvokedResult(
    string reason,
    DynamicScriptNotInvokedDetails? details = null) : DynamicScriptRunResult
{
    /// <summary>
    /// 获取未发生调用的原因。
    /// </summary>
    public string Reason { get; } =
        reason ?? throw new ArgumentNullException(nameof(reason));

    /// <summary>
    /// 获取可选失败诊断信息。
    /// </summary>
    public DynamicScriptNotInvokedDetails? Details { get; } = details;
}
