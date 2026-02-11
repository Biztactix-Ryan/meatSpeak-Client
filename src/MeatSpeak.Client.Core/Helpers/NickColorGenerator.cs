namespace MeatSpeak.Client.Core.Helpers;

public static class NickColorGenerator
{
    // Discord-like nick color palette
    private static readonly string[] Colors =
    [
        "#E74C3C", "#E67E22", "#F1C40F", "#2ECC71", "#1ABC9C",
        "#3498DB", "#9B59B6", "#E91E63", "#00BCD4", "#FF5722",
        "#795548", "#607D8B", "#8BC34A", "#FFEB3B", "#FF9800",
        "#9C27B0", "#673AB7", "#3F51B5", "#2196F3", "#00ACC1",
    ];

    public static string GetColor(string nick)
    {
        var hash = ComputeHash(nick.ToLowerInvariant());
        return Colors[Math.Abs(hash) % Colors.Length];
    }

    private static int ComputeHash(string input)
    {
        // DJB2 hash
        int hash = 5381;
        foreach (var c in input)
            hash = ((hash << 5) + hash) + c;
        return hash;
    }
}
