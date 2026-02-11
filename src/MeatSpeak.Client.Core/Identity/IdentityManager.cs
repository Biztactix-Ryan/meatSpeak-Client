using MeatSpeak.Identity.Crypto;

namespace MeatSpeak.Client.Core.Identity;

public sealed class IdentityManager : IDisposable
{
    private IdentityKeyPair? _keyPair;
    private readonly string _keyStorePath;

    public bool HasKeyPair => _keyPair is not null;
    public byte[]? PublicKey => _keyPair?.PublicKey;

    public IdentityManager(string keyStorePath)
    {
        _keyStorePath = keyStorePath;
    }

    public void LoadOrGenerate()
    {
        var privateKeyPath = Path.Combine(_keyStorePath, "identity.key");
        var publicKeyPath = Path.Combine(_keyStorePath, "identity.pub");

        if (File.Exists(privateKeyPath))
        {
            var privateKey = File.ReadAllBytes(privateKeyPath);
            _keyPair = IdentityKeyPair.FromPrivateKey(privateKey);
            Array.Clear(privateKey, 0, privateKey.Length);
        }
        else
        {
            Directory.CreateDirectory(_keyStorePath);
            _keyPair = IdentityKeyPair.Generate();
            var privateKey = _keyPair.GetPrivateKey();
            File.WriteAllBytes(privateKeyPath, privateKey);
            File.WriteAllBytes(publicKeyPath, _keyPair.PublicKey);
            Array.Clear(privateKey, 0, privateKey.Length);
        }
    }

    public byte[] Sign(byte[] data)
    {
        if (_keyPair is null)
            throw new InvalidOperationException("No identity key pair loaded");
        return _keyPair.Sign(data);
    }

    public IdentityKeyPair GetKeyPair()
    {
        if (_keyPair is null)
            throw new InvalidOperationException("No identity key pair loaded");
        return _keyPair;
    }

    public void Dispose()
    {
        _keyPair?.Dispose();
    }
}
