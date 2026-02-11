using MeatSpeak.Identity.Trust;

namespace MeatSpeak.Client.Core.Identity;

public sealed class TofuStore
{
    private readonly TofuVerifier _verifier;
    private readonly ITofuStore _backing;

    public TofuStore(ITofuStore backingStore)
    {
        _backing = backingStore;
        _verifier = new TofuVerifier(backingStore);
    }

    public async Task<TofuResult> VerifyAsync(string entityId, byte[] publicKey, CancellationToken ct = default)
    {
        return await _verifier.VerifyAsync(entityId, publicKey, TofuSources.ServerDomain, ct);
    }

    public async Task<IReadOnlyList<TofuPin>> GetAllPinsAsync(CancellationToken ct = default)
    {
        return await _backing.GetAllPinsAsync(ct);
    }

    public static string ComputeFingerprint(byte[] publicKey)
    {
        return TofuVerifier.ComputeFingerprint(publicKey);
    }
}
