namespace FarmOS.SharedKernel;

public static class StringNormalization
{
    /// <summary>
    /// Normalizes a name for dedup comparison: lowercase, trim, collapse whitespace,
    /// strip punctuation, handle "Last, First" -> "First Last".
    /// </summary>
    public static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "";

        var trimmed = name.Trim().ToLowerInvariant();

        // Handle "Last, First" format
        if (trimmed.Contains(','))
        {
            var parts = trimmed.Split(',', 2);
            if (parts.Length == 2)
                trimmed = $"{parts[1].Trim()} {parts[0].Trim()}";
        }

        // Strip punctuation except spaces
        var cleaned = new string(trimmed.Where(c => char.IsLetterOrDigit(c) || c == ' ').ToArray());

        // Collapse whitespace
        return string.Join(' ', cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>
    /// Levenshtein distance similarity scoring.
    /// Returns a 0.0-1.0 score (1.0 = identical).
    /// </summary>
    public static decimal Similarity(string a, string b)
    {
        if (a == b) return 1.0m;
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return 0.0m;

        var na = NormalizeName(a);
        var nb = NormalizeName(b);
        if (na == nb) return 1.0m;

        var distance = LevenshteinDistance(na, nb);
        var maxLen = Math.Max(na.Length, nb.Length);
        return maxLen == 0 ? 1.0m : Math.Round(1.0m - (decimal)distance / maxLen, 2);
    }

    private static int LevenshteinDistance(string s, string t)
    {
        var n = s.Length;
        var m = t.Length;
        var d = new int[n + 1, m + 1];

        for (var i = 0; i <= n; i++) d[i, 0] = i;
        for (var j = 0; j <= m; j++) d[0, j] = j;

        for (var i = 1; i <= n; i++)
        for (var j = 1; j <= m; j++)
        {
            var cost = s[i - 1] == t[j - 1] ? 0 : 1;
            d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
        }

        return d[n, m];
    }
}
