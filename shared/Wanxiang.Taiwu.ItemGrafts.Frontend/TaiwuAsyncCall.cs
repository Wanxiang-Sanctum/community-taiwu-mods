using Cysharp.Threading.Tasks;
using GameData.Serializer;
using GameData.Utilities;

namespace Wanxiang.Taiwu.ItemGrafts.Frontend;

internal delegate void TaiwuAsyncCallback(int offset, RawDataPool dataPool);

internal static class TaiwuAsyncCall
{
    internal static UniTask<TResult> InvokeAsync<TResult>(
        Action<TaiwuAsyncCallback> dispatch)
    {
        return InvokeAsync(dispatch, Deserialize<TResult>);
    }

    internal static UniTask<TResult> InvokeAsync<TResult>(
        Action<TaiwuAsyncCallback> dispatch,
        Func<int, RawDataPool, TResult> readResult)
    {
        if (dispatch is null)
        {
            throw new ArgumentNullException(nameof(dispatch));
        }

        if (readResult is null)
        {
            throw new ArgumentNullException(nameof(readResult));
        }

        UniTaskCompletionSource<TResult> completionSource = new();

        try
        {
            dispatch((offset, dataPool) => Complete(completionSource, readResult, offset, dataPool));
        }
#pragma warning disable CA1031
        // Surface immediate dispatcher failures through the returned UniTask.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            _ = completionSource.TrySetException(ex);
        }

        return completionSource.Task;
    }

    private static TResult Deserialize<TResult>(int offset, RawDataPool dataPool)
    {
        if (dataPool is null)
        {
            throw new ArgumentNullException(nameof(dataPool));
        }

        TResult result = default!;
        _ = SerializerHolder<TResult>.Deserialize(dataPool, offset, ref result);
        return result;
    }

    private static void Complete<TResult>(
        UniTaskCompletionSource<TResult> completionSource,
        Func<int, RawDataPool, TResult> readResult,
        int offset,
        RawDataPool dataPool)
    {
        try
        {
            _ = completionSource.TrySetResult(readResult(offset, dataPool));
        }
#pragma warning disable CA1031
        // Complete the UniTask with callback failures instead of letting the game dispatcher swallow them.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            _ = completionSource.TrySetException(ex);
        }
    }
}
