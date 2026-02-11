using MeatSpeak.Client.Core.Identity;
using MeatSpeak.Identity.Trust;

namespace MeatSpeak.Client.Core.Tests.Identity;

public class TofuStoreTests
{
    [Fact]
    public async Task Verify_FirstUse_ReturnsTrustedFirstUse()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"tofu_test_{Guid.NewGuid()}.json");
        try
        {
            var backingStore = new FileTofuStore(tempFile);
            var store = new TofuStore(backingStore);

            var publicKey = new byte[32];
            Random.Shared.NextBytes(publicKey);

            var result = await store.VerifyAsync("test-server", publicKey);
            Assert.Equal(TofuResult.TrustedFirstUse, result);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task Verify_SameKey_ReturnsTrustedPinMatch()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"tofu_test_{Guid.NewGuid()}.json");
        try
        {
            var backingStore = new FileTofuStore(tempFile);
            var store = new TofuStore(backingStore);

            var publicKey = new byte[32];
            Random.Shared.NextBytes(publicKey);

            await store.VerifyAsync("test-server", publicKey);
            var result = await store.VerifyAsync("test-server", publicKey);

            Assert.Equal(TofuResult.TrustedPinMatch, result);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task Verify_DifferentKey_ReturnsKeyChanged()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"tofu_test_{Guid.NewGuid()}.json");
        try
        {
            var backingStore = new FileTofuStore(tempFile);
            var store = new TofuStore(backingStore);

            var key1 = new byte[32];
            Random.Shared.NextBytes(key1);
            var key2 = new byte[32];
            Random.Shared.NextBytes(key2);

            await store.VerifyAsync("test-server", key1);
            var result = await store.VerifyAsync("test-server", key2);

            Assert.Equal(TofuResult.KeyChanged, result);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void ComputeFingerprint_IsConsistent()
    {
        var key = new byte[32];
        Random.Shared.NextBytes(key);

        var fp1 = TofuStore.ComputeFingerprint(key);
        var fp2 = TofuStore.ComputeFingerprint(key);

        Assert.Equal(fp1, fp2);
        Assert.Equal(64, fp1.Length); // 32 bytes hex = 64 chars
    }
}
