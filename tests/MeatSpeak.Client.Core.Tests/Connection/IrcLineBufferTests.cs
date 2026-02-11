using System.Text;
using MeatSpeak.Client.Core.Connection;

namespace MeatSpeak.Client.Core.Tests.Connection;

public class IrcLineBufferTests
{
    [Fact]
    public void ParseLine_StandardMessage_ReturnsParsedMessage()
    {
        var line = ":server 001 nick :Welcome to the server"u8;
        var msg = IrcLineBuffer.ParseLine(line);

        Assert.NotNull(msg);
        Assert.Equal("001", msg!.Command);
        Assert.Equal("server", msg.Prefix);
        Assert.Equal("nick", msg.GetParam(0));
        Assert.Equal("Welcome to the server", msg.Trailing);
    }

    [Fact]
    public void ParseLine_Ping_ReturnsParsedMessage()
    {
        var line = "PING :server123"u8;
        var msg = IrcLineBuffer.ParseLine(line);

        Assert.NotNull(msg);
        Assert.Equal("PING", msg!.Command);
        Assert.Equal("server123", msg.Trailing);
    }

    [Fact]
    public void ParseLine_PrivMsg_ParsesCorrectly()
    {
        var line = ":nick!user@host PRIVMSG #channel :Hello world"u8;
        var msg = IrcLineBuffer.ParseLine(line);

        Assert.NotNull(msg);
        Assert.Equal("PRIVMSG", msg!.Command);
        Assert.Equal("nick!user@host", msg.Prefix);
        Assert.Equal("#channel", msg.GetParam(0));
        Assert.Equal("Hello world", msg.Trailing);
    }

    [Fact]
    public void ParseLine_EmptyLine_ReturnsNull()
    {
        var msg = IrcLineBuffer.ParseLine(ReadOnlySpan<byte>.Empty);
        Assert.Null(msg);
    }

    [Fact]
    public void ParseLine_JoinMessage_ParsesCorrectly()
    {
        var line = ":nick!user@host JOIN :#channel"u8;
        var msg = IrcLineBuffer.ParseLine(line);

        Assert.NotNull(msg);
        Assert.Equal("JOIN", msg!.Command);
        Assert.Equal("#channel", msg.Trailing);
    }

    [Fact]
    public void ParseLine_WithTags_ParsesCorrectly()
    {
        var line = "@time=2024-01-01T00:00:00Z :nick!user@host PRIVMSG #chan :hi"u8;
        var msg = IrcLineBuffer.ParseLine(line);

        Assert.NotNull(msg);
        Assert.Equal("PRIVMSG", msg!.Command);
        Assert.Contains("time=", msg.Tags);
    }

    [Fact]
    public async Task ReadLinesAsync_MultipleLines_ParsesAll()
    {
        var data = ":server 001 nick :Welcome\r\nPING :test\r\n"u8.ToArray();
        var stream = new MemoryStream(data);
        var buffer = new IrcLineBuffer(stream);

        var messages = new List<MeatSpeak.Protocol.IrcMessage>();
        await foreach (var msg in buffer.ReadLinesAsync())
        {
            messages.Add(msg);
        }

        Assert.Equal(2, messages.Count);
        Assert.Equal("001", messages[0].Command);
        Assert.Equal("PING", messages[1].Command);
    }

    [Fact]
    public void ParseLine_NickCommand_ParsesCorrectly()
    {
        var line = ":oldnick!user@host NICK newnick"u8;
        var msg = IrcLineBuffer.ParseLine(line);

        Assert.NotNull(msg);
        Assert.Equal("NICK", msg!.Command);
        Assert.Equal("newnick", msg.GetParam(0));
    }

    [Fact]
    public void ParseLine_NumericWithMultipleParams_ParsesCorrectly()
    {
        // ISUPPORT
        var line = ":server 005 nick CHANTYPES=# PREFIX=(ov)@+ NETWORK=TestNet :are supported"u8;
        var msg = IrcLineBuffer.ParseLine(line);

        Assert.NotNull(msg);
        Assert.Equal("005", msg!.Command);
        Assert.Equal("nick", msg.GetParam(0));
        Assert.Equal("CHANTYPES=#", msg.GetParam(1));
        Assert.Equal("PREFIX=(ov)@+", msg.GetParam(2));
        Assert.Equal("NETWORK=TestNet", msg.GetParam(3));
    }
}
