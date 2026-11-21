using System.Text.RegularExpressions;
using static HitmanBackend.LinkedInputs;

namespace HitmanBackend;

/// <summary>Discovers hitbox resources registered through an UberASMTool list.</summary>
public static class UberasmInputs
{
    /// <summary>Matches quoted and unquoted ASM paths in a list entry.</summary>
    private static readonly Regex AsmPath = new(
        """(?:"(?<path>[^"]+\.asm)"|(?<![^,\s])(?<path>[^",\s]+\.asm))(?=[,:\s]|$)""",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    /// <summary>Matches list directives with a root-relative ASM file.</summary>
    private static readonly Regex InlineSource = new(
        @"^(?<kind>global|statusbar|macrolib(?:rary)?)\s*:\s*(?<source>.+)$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    /// <summary>Resolves the configured macro library or the default UberASMTool library.</summary>
    public static string ResolveMacroLibrary(string folder, string list)
    {
        if (list.Length != 0)
        {
            string listPath = Resolve(folder, list);
            foreach (string line in File.ReadLines(listPath))
            {
                Match directive = InlineSource.Match(StripComment(line));
                if (!directive.Success || !directive.Groups["kind"].Value.StartsWith("macrolib", StringComparison.OrdinalIgnoreCase))
                    continue;

                Match entry = AsmPath.Match(directive.Groups["source"].Value);
                if (!entry.Success)
                    throw new InvalidDataException($"Invalid macro library path in UberASMTool list '{listPath}'.");
                return Resolve(folder, entry.Groups["path"].Value);
            }
        }
        return Resolve(folder, "other/macro_library.asm");
    }

    /// <summary>Collects hitbox paths relative to their declaring ASM files, scanning each source once.</summary>
    public static IEnumerable<string> DiscoverHitboxes(string folder, string list, HitmanLog? log = null)
    {
        if (string.IsNullOrEmpty(list))
            yield break;

        string listPath = Resolve(folder, list);
        log?.Info($"Reading UberASMTool list '{listPath}'.");
        var sources = new HashSet<string>(PathComparer);
        string? directory = null;
        int lineNumber = 0;
        foreach (string originalLine in File.ReadLines(listPath))
        {
            lineNumber++;
            string line = StripComment(originalLine);
            if (line.EndsWith(':'))
            {
                directory = line[..^1].Replace("_", "").Replace(" ", "").ToLowerInvariant() switch
                {
                    "level" => "level",
                    "overworld" => "overworld",
                    "gamemode" => "gamemode",
                    "global" => "global",
                    "statusbar" => "statusbar",
                    "macrolibrary" or "macrolib" => "library",
                    _ => null
                };
                continue;
            }
            Match inline = InlineSource.Match(line);
            string? sourceDirectory = directory;
            if (inline.Success)
            {
                sourceDirectory = "";
                line = inline.Groups["source"].Value;
            }
            if (sourceDirectory == null)
                continue;

            foreach (Match entry in AsmPath.Matches(line))
            {
                string source = Resolve(Path.Combine(folder, sourceDirectory), entry.Groups["path"].Value);
                if (!sources.Add(source))
                    continue;
                if (!File.Exists(source))
                    throw new FileNotFoundException($"{listPath}:{lineNumber}: UberASMTool source '{source}' was not found.", source);
                foreach (string hitbox in ReadDirectives(source, allowProperties: false).Hitboxes)
                    yield return hitbox;
            }
        }
    }
}
