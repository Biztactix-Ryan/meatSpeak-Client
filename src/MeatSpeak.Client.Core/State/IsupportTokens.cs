namespace MeatSpeak.Client.Core.State;

public sealed class IsupportTokens
{
    private readonly Dictionary<string, string?> _tokens = new(StringComparer.OrdinalIgnoreCase);

    public string? Network => GetValue("NETWORK");
    public string ChanTypes => GetValue("CHANTYPES") ?? "#&";
    public string? Prefix => GetValue("PREFIX");
    public string? ChanModes => GetValue("CHANMODES");
    public int MaxChannels => int.TryParse(GetValue("MAXCHANNELS") ?? GetValue("CHANLIMIT")?.Split(':').LastOrDefault(), out var v) ? v : 20;
    public int NickLen => int.TryParse(GetValue("NICKLEN"), out var v) ? v : 30;
    public int TopicLen => int.TryParse(GetValue("TOPICLEN"), out var v) ? v : 390;
    public int MaxTargets => int.TryParse(GetValue("MAXTARGETS"), out var v) ? v : 4;
    public string? StatusMsg => GetValue("STATUSMSG");
    public bool SupportsWhox => _tokens.ContainsKey("WHOX");

    public string? GetValue(string key) =>
        _tokens.TryGetValue(key, out var value) ? value : null;

    public void ParseTokens(IEnumerable<string> tokens)
    {
        foreach (var token in tokens)
        {
            if (token.StartsWith('-'))
            {
                _tokens.Remove(token[1..]);
                continue;
            }

            var eqIndex = token.IndexOf('=');
            if (eqIndex >= 0)
                _tokens[token[..eqIndex]] = token[(eqIndex + 1)..];
            else
                _tokens[token] = null;
        }
    }

    public (string[] Modes, string[] Prefixes) ParsePrefix()
    {
        var raw = Prefix;
        if (string.IsNullOrEmpty(raw) || !raw.StartsWith('('))
            return (["o", "v"], ["@", "+"]);

        var close = raw.IndexOf(')');
        if (close < 0)
            return (["o", "v"], ["@", "+"]);

        var modes = raw[1..close].Select(c => c.ToString()).ToArray();
        var prefixes = raw[(close + 1)..].Select(c => c.ToString()).ToArray();
        return (modes, prefixes);
    }

    public bool IsChannelName(string target) =>
        target.Length > 0 && ChanTypes.Contains(target[0]);
}
