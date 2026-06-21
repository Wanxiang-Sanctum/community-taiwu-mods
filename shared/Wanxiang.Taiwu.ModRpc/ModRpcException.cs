namespace Wanxiang.Taiwu.ModRpc;

public sealed class ModRpcException : Exception
{
    public ModRpcException()
    {
    }

    public ModRpcException(string message)
        : base(message)
    {
    }

    public ModRpcException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
