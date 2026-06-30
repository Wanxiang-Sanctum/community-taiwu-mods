namespace Wanxiang.Taiwu.DynamicScripting;

/// <summary>
/// 表示已成功返回的脚本入口方法。
/// </summary>
/// <param name="returnValueJson">JSON 编码后的返回值。</param>
public sealed class DynamicScriptReturnValueOutcome(
    string returnValueJson) : DynamicScriptInvocationOutcome
{
    /// <summary>
    /// 获取 JSON 编码后的返回值。
    /// </summary>
    public string ReturnValueJson { get; } =
        returnValueJson ?? throw new ArgumentNullException(nameof(returnValueJson));
}
