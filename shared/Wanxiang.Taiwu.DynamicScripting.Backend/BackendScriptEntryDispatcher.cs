using System.Diagnostics.CodeAnalysis;
using GameData.Common;
using GameData.GameDataBridge;

namespace Wanxiang.Taiwu.DynamicScripting.Backend;

/// <summary>
/// Dispatches dynamic script entry calls to the current backend thread or GameData main loop.
/// </summary>
public sealed class BackendScriptEntryDispatcher : IDynamicScriptEntryDispatcher, IDisposable
{
    private readonly object _syncRoot = new();
    private readonly Queue<PendingEntryInvocation> _pendingInvocations = new();

    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="BackendScriptEntryDispatcher"/> class.
    /// </summary>
    public BackendScriptEntryDispatcher()
    {
        if (!IsOnBackendMainThread())
        {
            throw new InvalidOperationException(
                "Backend script entry dispatcher must be created on the GameData main thread.");
        }

        GameDataBridge.StartNextFrame(DrainPendingInvocations);
    }

    /// <inheritdoc />
    public Task<object?> InvokeAsync(
        Func<object?> invokeEntry,
        DynamicScriptEntryThread entryThread,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(invokeEntry);
        cancellationToken.ThrowIfCancellationRequested();

        return entryThread switch
        {
            DynamicScriptEntryThread.Current => Task.FromResult(invokeEntry()),
            DynamicScriptEntryThread.MainThread => InvokeOnMainThreadAsync(invokeEntry, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(
                nameof(entryThread),
                entryThread,
                "Unsupported script entry thread."),
        };
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Queue<PendingEntryInvocation> pendingInvocations;

        lock (_syncRoot)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            pendingInvocations = DequeuePendingInvocationsLocked();
        }

        RejectPendingInvocations(pendingInvocations);
    }

    private Task<object?> InvokeOnMainThreadAsync(
        Func<object?> invokeEntry,
        CancellationToken cancellationToken)
    {
        if (IsOnBackendMainThread())
        {
            lock (_syncRoot)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
            }

            return Task.FromResult(invokeEntry());
        }

        TaskCompletionSource<object?> completionSource = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        PendingEntryInvocation pendingInvocation = new(
            invokeEntry,
            completionSource,
            cancellationToken);

        lock (_syncRoot)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _pendingInvocations.Enqueue(pendingInvocation);
        }

        return completionSource.Task;
    }

    private void DrainPendingInvocations()
    {
        Queue<PendingEntryInvocation> pendingInvocations;

        lock (_syncRoot)
        {
            if (_disposed)
            {
                return;
            }

            pendingInvocations = DequeuePendingInvocationsLocked();
        }

        while (pendingInvocations.TryDequeue(out PendingEntryInvocation? pendingInvocation))
        {
            pendingInvocation.Invoke();
        }

        lock (_syncRoot)
        {
            if (_disposed)
            {
                return;
            }
        }

        GameDataBridge.StartNextFrame(DrainPendingInvocations);
    }

    private Queue<PendingEntryInvocation> DequeuePendingInvocationsLocked()
    {
        Queue<PendingEntryInvocation> pendingInvocations = new();
        while (_pendingInvocations.TryDequeue(out PendingEntryInvocation? pendingInvocation))
        {
            pendingInvocations.Enqueue(pendingInvocation);
        }

        return pendingInvocations;
    }

    private static void RejectPendingInvocations(Queue<PendingEntryInvocation> pendingInvocations)
    {
        while (pendingInvocations.TryDequeue(out PendingEntryInvocation? pendingInvocation))
        {
            pendingInvocation.Reject();
        }
    }

    private static bool IsOnBackendMainThread()
    {
        return DataContextManager.IsMainThread(
            Environment.CurrentManagedThreadId);
    }

    private sealed class PendingEntryInvocation(
        Func<object?> invokeEntry,
        TaskCompletionSource<object?> completionSource,
        CancellationToken cancellationToken)
    {
        [SuppressMessage(
            "Design",
            "CA1031:Do not catch general exception types",
            Justification = "Queued script invocation exceptions are marshaled back through the awaiting task.")]
        public void Invoke()
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _ = completionSource.TrySetCanceled(cancellationToken);
                return;
            }

            try
            {
                _ = completionSource.TrySetResult(invokeEntry());
            }
            catch (Exception ex)
            {
                _ = completionSource.TrySetException(ex);
            }
        }

        public void Reject()
        {
            _ = completionSource.TrySetException(
                new ObjectDisposedException(nameof(BackendScriptEntryDispatcher)));
        }
    }
}
