namespace Wanxiang.Taiwu.DynamicScripting;

/// <summary>
/// 表示入口方法已被调用的脚本。
/// </summary>
/// <param name="outcome">入口方法结果。</param>
public sealed class DynamicScriptInvokedResult(
    DynamicScriptInvocationOutcome outcome) : DynamicScriptRunResult
{
    /// <summary>
    /// 获取入口方法结果。
    /// </summary>
    public DynamicScriptInvocationOutcome Outcome { get; } =
        outcome ?? throw new ArgumentNullException(nameof(outcome));
}
