namespace Wanxiang.Taiwu.DynamicScripting;

/// <summary>
/// 描述动态脚本编译所需的显式程序集引用输入。
/// </summary>
/// <param name="assemblyReferencePaths">要作为编译和运行时引用加入的具体程序集文件。</param>
/// <exception cref="ArgumentException"><paramref name="assemblyReferencePaths"/> 包含 null 或空白路径。</exception>
public sealed class DynamicScriptReferenceOptions(
    IEnumerable<string>? assemblyReferencePaths = null)
{
    /// <summary>
    /// 获取要作为编译和运行时引用加入的具体程序集文件。
    /// </summary>
    public IReadOnlyList<string> AssemblyReferencePaths { get; } =
        NormalizePaths(
            assemblyReferencePaths,
            nameof(assemblyReferencePaths));

    private static List<string> NormalizePaths(
        IEnumerable<string>? paths,
        string parameterName)
    {
        if (paths is null)
        {
            return [];
        }

        List<string> normalizedPaths = [];
        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
        foreach (string? path in paths)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException(
                    "Assembly reference paths cannot contain null or whitespace entries.",
                    parameterName);
            }

            string normalizedPath = Path.GetFullPath(
                Environment.ExpandEnvironmentVariables(path.Trim()));
            if (seen.Add(normalizedPath))
            {
                normalizedPaths.Add(normalizedPath);
            }
        }

        return normalizedPaths;
    }
}
