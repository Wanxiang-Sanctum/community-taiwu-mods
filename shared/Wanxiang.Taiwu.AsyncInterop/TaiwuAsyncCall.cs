#if NETSTANDARD2_1
using Cysharp.Threading.Tasks;
#endif
using GameData.Serializer;
using GameData.Utilities;

namespace Wanxiang.Taiwu.AsyncInterop;

public delegate void TaiwuAsyncCallback(int offset, RawDataPool dataPool);

public static class TaiwuAsyncCall
{
#if NETSTANDARD2_1
    public static UniTask<TResult> InvokeAsync<TResult>(
        Action<TaiwuAsyncCallback> dispatch)
    {
        return InvokeAsync(dispatch, Deserialize<TResult>);
    }

    public static UniTask<TResult> InvokeAsync<TResult>(
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
#endif

#if NET8_0
    public static Task<TResult> InvokeAsync<TResult>(
        Action<TaiwuAsyncCallback> dispatch)
    {
        return InvokeAsync(dispatch, Deserialize<TResult>);
    }

    public static Task<TResult> InvokeAsync<TResult>(
        Action<TaiwuAsyncCallback> dispatch,
        Func<int, RawDataPool, TResult> readResult)
    {
        ArgumentNullException.ThrowIfNull(dispatch);
        ArgumentNullException.ThrowIfNull(readResult);

        TaskCompletionSource<TResult> completionSource =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        try
        {
            dispatch((offset, dataPool) => Complete(completionSource, readResult, offset, dataPool));
        }
#pragma warning disable CA1031
        // Surface immediate dispatcher failures through the returned Task.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            _ = completionSource.TrySetException(ex);
        }

        return completionSource.Task;
    }
#endif

    private static TResult Deserialize<TResult>(int offset, RawDataPool dataPool)
    {
#if NET8_0
        ArgumentNullException.ThrowIfNull(dataPool);
#else
        if (dataPool is null)
        {
            throw new ArgumentNullException(nameof(dataPool));
        }
#endif

        TResult result = default!;
        _ = SerializerHolder<TResult>.Deserialize(dataPool, offset, ref result);
        return result;
    }

#if NETSTANDARD2_1
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
#endif

#if NET8_0
    private static void Complete<TResult>(
        TaskCompletionSource<TResult> completionSource,
        Func<int, RawDataPool, TResult> readResult,
        int offset,
        RawDataPool dataPool)
    {
        try
        {
            _ = completionSource.TrySetResult(readResult(offset, dataPool));
        }
#pragma warning disable CA1031
        // Complete the Task with callback failures instead of letting the game dispatcher swallow them.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            _ = completionSource.TrySetException(ex);
        }
    }
#endif
}
