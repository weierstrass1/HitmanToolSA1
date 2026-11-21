using System.Text.RegularExpressions;

namespace HitmanBackend;

/// <summary>Reads linked Hitman resources from ASM and resolves external tool paths.</summary>
internal static class LinkedInputs
{
    /// <summary>Compares paths with the casing conventions of the current platform.</summary>
    public static StringComparer PathComparer { get; } =
        OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

    /// <summary>Pattern for a Hitman directive with a quoted path.</summary>
    private static readonly Regex Directive = new(
        """^\s*;#(?<kind>inchbi|inchmprop)\s+"(?<path>[^"]+)"\s*(?:;.*)?$""",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    /// <summary>Recognizes Hitman directives, including malformed ones.</summary>
    private static readonly Regex DirectivePrefix = new(
        """^\s*;#(?:inchbi|inchmprop)(?=\s|$)""",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    /// <summary>Reads Hitman directives from ASM and validates their referenced files.</summary>
    public static (string[] Hitboxes, string? Properties) ReadDirectives(string source, bool allowProperties = true)
    {
        var hitboxes = new List<string>();
        string? properties = null;
        int lineNumber = 0;
        foreach (string line in File.ReadLines(source))
        {
            lineNumber++;
            if (!DirectivePrefix.IsMatch(line))
                continue;

            Match match = Directive.Match(line);
            if (!match.Success)
                throw new InvalidDataException($"Invalid Hitman directive in '{source}', line {lineNumber}.");

            bool isHitbox = match.Groups["kind"].Value.Equals("inchbi", StringComparison.OrdinalIgnoreCase);
            if (!isHitbox && !allowProperties)
                throw new InvalidDataException($"inchmprop in '{source}', line {lineNumber} requires a normal sprite entry in PIXI.");
            string extension = isHitbox ? ".hitboxinfo" : ".json";
            string input = Resolve(Path.GetDirectoryName(source)!, match.Groups["path"].Value);
            if (!Path.GetExtension(input).Equals(extension, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"Hitman directive in '{source}', line {lineNumber} requires a {extension} file.");
            if (!File.Exists(input))
                throw new FileNotFoundException($"File referenced by '{source}', line {lineNumber} was not found.", input);

            if (isHitbox)
                hitboxes.Add(input);
            else if (properties == null)
                properties = input;
            else
                throw new InvalidDataException($"More than one inchmprop directive in '{source}', line {lineNumber}.");
        }
        return (hitboxes.ToArray(), properties);
    }

    /// <summary>Normalizes path separators and resolves the path relative to the given directory.</summary>
    public static string Resolve(string directory, string path) =>
        Path.GetFullPath(Path.Combine(directory, path.Replace('\\', Path.DirectorySeparatorChar)
                                                     .Replace('/', Path.DirectorySeparatorChar)));

    /// <summary>Removes the comment from an external tool list line.</summary>
    public static string StripComment(string line) =>
        Regex.Replace(line, """("[^"]*")|;.*$""", "$1").Trim();
}
