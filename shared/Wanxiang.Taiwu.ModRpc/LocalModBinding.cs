namespace Wanxiang.Taiwu.ModRpc;

internal static class LocalModBinding
{
    private static readonly object SyncRoot = new();

    private static string? s_localModId;

    internal static void Bind(string localModId)
    {
        string validatedLocalModId = Guard.RequiredText(localModId, nameof(localModId));

        lock (SyncRoot)
        {
            if (s_localModId is null)
            {
                s_localModId = validatedLocalModId;
                return;
            }

            if (!string.Equals(s_localModId, validatedLocalModId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "RpcPeer is already bound to a different local mod id.");
            }
        }
    }

    internal static string RequireLocalModId()
    {
        lock (SyncRoot)
        {
            return s_localModId
                ?? throw new InvalidOperationException(
                    "RpcPeer.Bind(localModId) must be called before using ModRpc.");
        }
    }
}
