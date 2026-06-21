#if NETSTANDARD2_1
using Cysharp.Threading.Tasks;
#endif
using GameData.Serializer;
using GameData.Utilities;

namespace Wanxiang.Taiwu.AsyncInterop;

/// <summary>
/// 表示太吾异步领域方法使用的回调形状。
/// </summary>
/// <param name="offset">结果在原始数据池中的偏移量。</param>
/// <param name="dataPool">包含序列化结果的原始数据池。</param>
public delegate void TaiwuAsyncCallback(int offset, RawDataPool dataPool);

/// <summary>
/// 将太吾基于回调的异步调用转换为当前运行侧使用的可等待类型。
/// </summary>
public static class TaiwuAsyncCall
{
#if NETSTANDARD2_1
    /// <summary>
    /// 派发太吾异步调用，并把回调结果反序列化为 <typeparamref name="TResult"/>。
    /// </summary>
    /// <typeparam name="TResult">期望的结果类型。</typeparam>
    /// <param name="dispatch">向太吾异步 API 注册回调的派发动作。</param>
    /// <returns>由太吾回调完成的 UniTask。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="dispatch"/> 为 null。</exception>
    public static UniTask<TResult> InvokeAsync<TResult>(
        Action<TaiwuAsyncCallback> dispatch)
    {
        return InvokeAsync(dispatch, Deserialize<TResult>);
    }

    /// <summary>
    /// 派发太吾异步调用，并用自定义读取器读取回调结果。
    /// </summary>
    /// <typeparam name="TResult">期望的结果类型。</typeparam>
    /// <param name="dispatch">向太吾异步 API 注册回调的派发动作。</param>
    /// <param name="readResult">从原始数据池读取回调结果的函数。</param>
    /// <returns>由太吾回调完成的 UniTask。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="dispatch"/> 或 <paramref name="readResult"/> 为 null。</exception>
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
        // 通过返回的 UniTask 暴露派发器的同步失败。
        catch (Exception ex)
#pragma warning restore CA1031
        {
            _ = completionSource.TrySetException(ex);
        }

        return completionSource.Task;
    }
#endif

#if NET8_0
    /// <summary>
    /// 派发太吾异步调用，并把回调结果反序列化为 <typeparamref name="TResult"/>。
    /// </summary>
    /// <typeparam name="TResult">期望的结果类型。</typeparam>
    /// <param name="dispatch">向太吾异步 API 注册回调的派发动作。</param>
    /// <returns>由太吾回调完成的 Task。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="dispatch"/> 为 null。</exception>
    public static Task<TResult> InvokeAsync<TResult>(
        Action<TaiwuAsyncCallback> dispatch)
    {
        return InvokeAsync(dispatch, Deserialize<TResult>);
    }

    /// <summary>
    /// 派发太吾异步调用，并用自定义读取器读取回调结果。
    /// </summary>
    /// <typeparam name="TResult">期望的结果类型。</typeparam>
    /// <param name="dispatch">向太吾异步 API 注册回调的派发动作。</param>
    /// <param name="readResult">从原始数据池读取回调结果的函数。</param>
    /// <returns>由太吾回调完成的 Task。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="dispatch"/> 或 <paramref name="readResult"/> 为 null。</exception>
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
        // 通过返回的 Task 暴露派发器的同步失败。
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
        // 用回调失败完成 UniTask，避免异常被游戏派发器吞掉。
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
        // 用回调失败完成 Task，避免异常被游戏派发器吞掉。
        catch (Exception ex)
#pragma warning restore CA1031
        {
            _ = completionSource.TrySetException(ex);
        }
    }
#endif
}
