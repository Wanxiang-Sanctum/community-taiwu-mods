namespace Xiangshu.Mcp;

[AttributeUsage(AttributeTargets.Method)]
public sealed class XiangshuMcpToolAttribute(string name) : Attribute
{
    public string Name { get; } = string.IsNullOrWhiteSpace(name)
        ? throw new ArgumentException("Tool name is required.", nameof(name))
        : name;

    public string? Title { get; set; }
}
