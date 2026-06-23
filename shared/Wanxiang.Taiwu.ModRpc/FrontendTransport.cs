#if NETSTANDARD2_1
using Cysharp.Threading.Tasks;
using GameData.Domains.Mod;
using GameData.Serializer;
using GameData.Utilities;

namespace Wanxiang.Taiwu.ModRpc;

internal static class FrontendTransport
{
    internal static void Send(
        string localModId,
        string methodName,
        SerializableModData parameter)
    {
        ModDomainMethod.Call.CallModMethodWithParam(
            localModId,
            methodName,
            parameter);
    }

    internal static UniTask<SerializableModData> InvokeAsync(
        string localModId,
        string methodName,
        SerializableModData parameter,
        CancellationToken cancellationToken = default)
    {
        return InvokeAsync(
            callback => ModDomainMethod.AsyncCall.CallModMethodWithParamAndRet(
                null,
                localModId,
                methodName,
                parameter,
                callback),
            cancellationToken);
    }

    private static UniTask<SerializableModData> InvokeAsync(
        Action<AsyncMethodCallbackDelegate> dispatch,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return UniTask.FromCanceled<SerializableModData>(cancellationToken);
        }

        UniTaskCompletionSource<SerializableModData> completionSource = new();

        try
        {
            dispatch((offset, dataPool) => Complete(completionSource, offset, dataPool));
        }
#pragma warning disable CA1031
        // 游戏派发器会同步报告即时失败；通过返回的任务保留这些失败。
        catch (Exception ex)
#pragma warning restore CA1031
        {
            _ = completionSource.TrySetException(ex);
        }

        return cancellationToken.CanBeCanceled
            ? completionSource.Task.AttachExternalCancellation(cancellationToken)
            : completionSource.Task;
    }

    private static void Complete(
        UniTaskCompletionSource<SerializableModData> completionSource,
        int offset,
        RawDataPool dataPool)
    {
        try
        {
            if (dataPool is null)
            {
                throw new ArgumentNullException(nameof(dataPool));
            }

            SerializableModData result = new();
            _ = SerializerHolder<SerializableModData>.Deserialize(dataPool, offset, ref result);
            _ = completionSource.TrySetResult(result);
        }
#pragma warning disable CA1031
        // 回调异常应完成等待器，避免穿透游戏桥接层。
        catch (Exception ex)
#pragma warning restore CA1031
        {
            _ = completionSource.TrySetException(ex);
        }
    }
}
#endif
