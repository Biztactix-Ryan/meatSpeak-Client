using MeatSpeak.Client.Core.Connection;
using MeatSpeak.Identity;
using MeatSpeak.Identity.Auth;
using MeatSpeak.Identity.Trust;

namespace MeatSpeak.Client.Core.Identity;

public sealed class AuthenticationService
{
    private readonly IdentityManager _identityManager;
    private readonly TofuStore _tofuStore;

    public AuthenticationService(IdentityManager identityManager, TofuStore tofuStore)
    {
        _identityManager = identityManager;
        _tofuStore = tofuStore;
    }

    public bool VerifyServerHello(ServerHello hello, byte[] serverPublicKey)
    {
        return MutualAuth.VerifyServerHello(hello, serverPublicKey);
    }

    public ClientHello CreateClientHello(
        string userUid,
        string kid,
        byte[] serverNonce,
        string serverUid)
    {
        var keyPair = _identityManager.GetKeyPair();
        var uid = Uid.Parse(userUid);
        var srvUid = Uid.Parse(serverUid);
        return MutualAuth.CreateClientHello(uid, kid, keyPair, serverNonce, srvUid);
    }

    public async Task<TofuResult> VerifyServerTofuAsync(string entityId, byte[] publicKey, CancellationToken ct = default)
    {
        return await _tofuStore.VerifyAsync(entityId, publicKey, ct);
    }
}
