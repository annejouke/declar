namespace Declar.Core.Files;

/// <summary>
/// Provides query and manipulation helpers for INI files on disk.
/// </summary>
public sealed class Ini
{
    private readonly string filePath;

    /// <summary>
    /// Creates a new INI file helper for a specific path.
    /// </summary>
    /// <param name="filePath">Absolute or relative path to the INI file.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="filePath"/> is empty.</exception>
    public Ini(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path is required.", nameof(filePath));
        }

        this.filePath = filePath;
    }

    /// <summary>
    /// Gets the configured file path for this INI helper.
    /// </summary>
    public string FilePath => filePath;

    /// <summary>
    /// Checks whether a key exists in the INI file.
    /// </summary>
    /// <param name="key">Key name to search for.</param>
    /// <param name="section">
    /// Optional section name. When omitted, all sections are searched and the first match is used.
    /// </param>
    /// <returns><c>true</c> when the key exists; otherwise <c>false</c>.</returns>
    public async Task<bool> KeyExistsAsync(string key, string? section = null)
    {
        return (await FindKeyValueAsync(key, section)).HasValue;
    }

    /// <summary>
    /// Gets the value for a key in the INI file.
    /// </summary>
    /// <param name="key">Key name to search for.</param>
    /// <param name="section">
    /// Optional section name. When omitted, all sections are searched and the first match is used.
    /// </param>
    /// <returns>The key value when found; otherwise <c>null</c>.</returns>
    public async Task<string?> GetValueAsync(string key, string? section = null)
    {
        var result = await FindKeyValueAsync(key, section);
        return result?.Value;
    }

    /// <summary>
    /// Comments the first matching non-commented line by prefixing it with <c>; </c>.
    /// </summary>
    /// <param name="line">Line content to comment.</param>
    /// <returns><c>true</c> if a line was changed; otherwise <c>false</c>.</returns>
    public async Task<bool> CommentLineAsync(string line)
    {
        if (string.IsNullOrWhiteSpace(line) || !File.Exists(filePath))
        {
            return false;
        }

        var target = line.Trim();
        var lines = (await File.ReadAllLinesAsync(filePath)).ToList();

        for (var i = 0; i < lines.Count; i++)
        {
            var trimmed = lines[i].Trim();
            if (trimmed.Length == 0 || IsCommented(trimmed))
            {
                continue;
            }

            if (!string.Equals(trimmed, target, StringComparison.Ordinal))
            {
                continue;
            }

            var leadingWhitespaceLength = lines[i].Length - lines[i].TrimStart().Length;
            var leadingWhitespace = lines[i][..leadingWhitespaceLength];
            var uncommented = lines[i].TrimStart();
            lines[i] = $"{leadingWhitespace}; {uncommented}";
            await File.WriteAllLinesAsync(filePath, lines);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Uncomments the first matching commented line by removing a leading <c>;</c> or <c>#</c> marker.
    /// </summary>
    /// <param name="line">Line content to uncomment.</param>
    /// <returns><c>true</c> if a line was changed; otherwise <c>false</c>.</returns>
    public async Task<bool> UncommentLineAsync(string line)
    {
        if (string.IsNullOrWhiteSpace(line) || !File.Exists(filePath))
        {
            return false;
        }

        var target = line.Trim();
        var lines = (await File.ReadAllLinesAsync(filePath)).ToList();

        for (var i = 0; i < lines.Count; i++)
        {
            var lineValue = lines[i];
            var firstNonWhitespaceIndex = FindFirstNonWhitespaceIndex(lineValue);
            if (firstNonWhitespaceIndex < 0)
            {
                continue;
            }

            var marker = lineValue[firstNonWhitespaceIndex];
            if (marker != ';' && marker != '#')
            {
                continue;
            }

            var uncommentedBody = lineValue[(firstNonWhitespaceIndex + 1)..].TrimStart();
            if (!string.Equals(uncommentedBody, target, StringComparison.Ordinal))
            {
                continue;
            }

            var leadingWhitespace = lineValue[..firstNonWhitespaceIndex];
            lines[i] = $"{leadingWhitespace}{uncommentedBody}";
            await File.WriteAllLinesAsync(filePath, lines);
            return true;
        }

        return false;
    }

    private async Task<(string Section, string Key, string Value)?> FindKeyValueAsync(string key, string? section)
    {
        if (string.IsNullOrWhiteSpace(key) || !File.Exists(filePath))
        {
            return null;
        }

        var wantedKey = key.Trim();
        var wantedSection = string.IsNullOrWhiteSpace(section) ? null : section.Trim();

        var lines = await File.ReadAllLinesAsync(filePath);
        string? currentSection = null;

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || IsCommented(line))
            {
                continue;
            }

            if (line.StartsWith("[", StringComparison.Ordinal) && line.EndsWith("]", StringComparison.Ordinal))
            {
                var sectionName = line[1..^1].Trim();
                currentSection = sectionName.Length == 0 ? null : sectionName;
                continue;
            }

            if (!TryParseKeyValue(line, out var parsedKey, out var parsedValue))
            {
                continue;
            }

            if (!string.Equals(parsedKey, wantedKey, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (wantedSection is not null
                && !string.Equals(currentSection, wantedSection, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return (currentSection ?? string.Empty, parsedKey, parsedValue);
        }

        return null;
    }

    private static bool IsCommented(string line)
    {
        return line.StartsWith(";", StringComparison.Ordinal) || line.StartsWith("#", StringComparison.Ordinal);
    }

    private static int FindFirstNonWhitespaceIndex(string line)
    {
        for (var i = 0; i < line.Length; i++)
        {
            if (!char.IsWhiteSpace(line[i]))
            {
                return i;
            }
        }

        return -1;
    }

    private static bool TryParseKeyValue(string line, out string key, out string value)
    {
        var separatorIndex = line.IndexOf('=');
        if (separatorIndex <= 0)
        {
            key = string.Empty;
            value = string.Empty;
            return false;
        }

        key = line[..separatorIndex].Trim();
        value = line[(separatorIndex + 1)..].Trim();
        return key.Length > 0;
    }
}