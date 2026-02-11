using MeatSpeak.Client.Core.State;

namespace MeatSpeak.Client.Core.Tests.State;

public class IsupportTokensTests
{
    [Fact]
    public void ParseTokens_ParsesKeyValuePairs()
    {
        var tokens = new IsupportTokens();
        tokens.ParseTokens(["NETWORK=TestNet", "CHANTYPES=#&", "NICKLEN=30"]);

        Assert.Equal("TestNet", tokens.Network);
        Assert.Equal("#&", tokens.ChanTypes);
        Assert.Equal(30, tokens.NickLen);
    }

    [Fact]
    public void ParseTokens_ParsesFlags()
    {
        var tokens = new IsupportTokens();
        tokens.ParseTokens(["WHOX"]);

        Assert.True(tokens.SupportsWhox);
    }

    [Fact]
    public void ParseTokens_RemovesNegatedTokens()
    {
        var tokens = new IsupportTokens();
        tokens.ParseTokens(["NETWORK=OldNet"]);
        tokens.ParseTokens(["-NETWORK"]);

        Assert.Null(tokens.Network);
    }

    [Fact]
    public void ParsePrefix_ParsesStandard()
    {
        var tokens = new IsupportTokens();
        tokens.ParseTokens(["PREFIX=(ov)@+"]);

        var (modes, prefixes) = tokens.ParsePrefix();

        Assert.Equal(["o", "v"], modes);
        Assert.Equal(["@", "+"], prefixes);
    }

    [Fact]
    public void ParsePrefix_ParsesExtended()
    {
        var tokens = new IsupportTokens();
        tokens.ParseTokens(["PREFIX=(qaohv)~&@%+"]);

        var (modes, prefixes) = tokens.ParsePrefix();

        Assert.Equal(5, modes.Length);
        Assert.Equal("q", modes[0]);
        Assert.Equal("~", prefixes[0]);
    }

    [Fact]
    public void ParsePrefix_DefaultWhenNotSet()
    {
        var tokens = new IsupportTokens();

        var (modes, prefixes) = tokens.ParsePrefix();

        Assert.Equal(["o", "v"], modes);
        Assert.Equal(["@", "+"], prefixes);
    }

    [Fact]
    public void IsChannelName_DefaultChanTypes()
    {
        var tokens = new IsupportTokens();

        Assert.True(tokens.IsChannelName("#channel"));
        Assert.True(tokens.IsChannelName("&channel"));
        Assert.False(tokens.IsChannelName("nick"));
    }

    [Fact]
    public void IsChannelName_CustomChanTypes()
    {
        var tokens = new IsupportTokens();
        tokens.ParseTokens(["CHANTYPES=#"]);

        Assert.True(tokens.IsChannelName("#channel"));
        Assert.False(tokens.IsChannelName("&channel"));
    }

    [Fact]
    public void Defaults_AreReasonable()
    {
        var tokens = new IsupportTokens();

        Assert.Equal("#&", tokens.ChanTypes);
        Assert.Equal(30, tokens.NickLen);
        Assert.Equal(390, tokens.TopicLen);
        Assert.Equal(20, tokens.MaxChannels);
    }
}
