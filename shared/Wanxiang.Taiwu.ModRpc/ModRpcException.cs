namespace Wanxiang.Taiwu.ModRpc;

/// <summary>
/// 表示 ModRpc 传输边界报告或检测到的失败。
/// </summary>
public sealed class ModRpcException : Exception
{
    /// <summary>
    /// 初始化 <see cref="ModRpcException"/> 类的新实例。
    /// </summary>
    public ModRpcException()
    {
    }

    /// <summary>
    /// 使用错误消息初始化 <see cref="ModRpcException"/> 类的新实例。
    /// </summary>
    /// <param name="message">错误消息。</param>
    public ModRpcException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// 使用错误消息和内部异常初始化 <see cref="ModRpcException"/> 类的新实例。
    /// </summary>
    /// <param name="message">错误消息。</param>
    /// <param name="innerException">导致当前异常的异常。</param>
    public ModRpcException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
