#if NETSTANDARD2_1
using Cysharp.Threading.Tasks;
using GameData.Domains.Mod;
using GameData.Serializer;
using GameData.Utilities;

namespace Wanxiang.Taiwu.ModRpc;

internal static class FrontendTransport
{
    internal static void Send(
        string modId,
        string methodName,
        SerializableModData parameter)
    {
        ModDomainMethod.Call.CallModMethodWithParam(
            modId,
            methodName,
            parameter);
    }

    internal static UniTask<SerializableModData> InvokeAsync(
        string modId,
        string methodName,
        SerializableModData parameter,
        CancellationToken cancellationToken = default)
    {
        return InvokeAsync(
            callback => ModDomainMethod.AsyncCall.CallModMethodWithParamAndRet(
                null,
                modId,
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
        // The game dispatcher reports immediate failures synchronously; preserve them on the returned task.
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
        // Callback exceptions should complete the awaiter instead of escaping through the game bridge.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            _ = completionSource.TrySetException(ex);
        }
    }
}
#endif
