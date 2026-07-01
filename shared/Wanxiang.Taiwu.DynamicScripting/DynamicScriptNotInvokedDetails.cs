using System.Collections.ObjectModel;

namespace Wanxiang.Taiwu.DynamicScripting;

/// <summary>
/// 包含脚本无法编译或引用时的诊断信息。
/// </summary>
/// <param name="referenceDiagnostics">引用解析诊断信息。</param>
/// <param name="compilationDiagnostics">编译诊断信息。</param>
public sealed class DynamicScriptNotInvokedDetails(
    IReadOnlyList<string>? referenceDiagnostics,
    IReadOnlyList<string>? compilationDiagnostics)
{
    private static readonly IReadOnlyList<string> EmptyList =
        new ReadOnlyCollection<string>([]);

    /// <summary>
    /// 获取引用解析诊断信息。
    /// </summary>
    public IReadOnlyList<string> ReferenceDiagnostics { get; } =
        NormalizeList(referenceDiagnostics);

    /// <summary>
    /// 获取编译诊断信息。
    /// </summary>
    public IReadOnlyList<string> CompilationDiagnostics { get; } =
        NormalizeList(compilationDiagnostics);

    private static IReadOnlyList<string> NormalizeList(IReadOnlyList<string>? values)
    {
        return values is { Count: > 0 }
            ? new ReadOnlyCollection<string>([.. values])
            : EmptyList;
    }
}
