using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using static HitmanBackend.LinkedInputs;

namespace HitmanBackend;

/// <summary>Resolves entries in a PIXI list to hitbox and property inputs.</summary>
public static class PixiInputs
{
    /// <summary>Pattern for a PIXI entry with a hexadecimal ID and a path.</summary>
    private static readonly Regex ListEntry = new(
        """^(?:[0-9a-f]{2,3}:)?(?<slot>[0-9a-f]{2})\s+(?:"(?<path>[^"]+)"|(?<path>\S+?))(?:\s+(?:display|nodisplay))?$""",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    /// <summary>Resolves the ASM for listed PIXI resources and collects their linked files without repeating paths.</summary>
    /// <param name="pixiDirectory">PIXI root directory.</param>
    /// <param name="pixiListPath">Path to the insertion list.</param>
    /// <param name="log">Optional log for missing descriptors in the PIXI list.</param>
    /// <returns>Hitbox paths and per-resource property file mappings.</returns>
    public static (IReadOnlyList<string> Hitboxes, IReadOnlyDictionary<string, string> Properties) Discover(
        string pixiDirectory, string pixiListPath, HitmanLog? log = null)
    {
        var pathComparer = PathComparer;
        var sources = new Dictionary<string, (string[] Hitboxes, string? Properties)>(pathComparer);
        var foundInputs = new HashSet<string>(pathComparer);
        var hitboxes = new List<string>();
        var properties = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var entry in ReadEntries(pixiListPath))
        {
            string descriptor = Resolve(Path.Combine(pixiDirectory, entry.Directory), entry.Path);
            if (!File.Exists(descriptor))
            {
                // Some legacy lists are used only to assign property IDs.
                log?.Warning($"{pixiListPath}:{entry.LineNumber}: PIXI resource '{descriptor}' was not found. Hitman directives were skipped.");
                continue;
            }

            string? source = ResolveAsm(descriptor);
            if (source == null)
                continue;

            if (!sources.TryGetValue(source, out var files))
            {
                files = ReadDirectives(source);
                sources.Add(source, files);
            }

            foreach (string hitbox in files.Hitboxes)
                if (foundInputs.Add(hitbox))
                    hitboxes.Add(hitbox);

            if (files.Properties == null)
                continue;
            if (entry.Directory != "sprites")
                throw new InvalidDataException($"Hitman properties in '{source}' require a normal sprite entry.");
            if (properties.TryGetValue(entry.Name, out string? previous) &&
                !pathComparer.Equals(previous, files.Properties))
                throw new InvalidDataException($"PIXI entries named '{entry.Name}' reference different Hitman properties files.");
            properties[entry.Name] = files.Properties;
        }

        return (hitboxes, properties);
    }

    /// <summary>PIXI entry with its ID, resource path, and sprite-family directory.</summary>
    internal sealed record Entry(byte Slot, string Path, string Directory, int LineNumber)
    {
        /// <summary>Name linking the PIXI entry to its custom properties.</summary>
        public string Name => System.IO.Path.GetFileNameWithoutExtension(Path.Replace('\\', '/'));
    }

    /// <summary>Reads hexadecimal IDs and paths using the current sprite section.</summary>
    internal static IEnumerable<Entry> ReadEntries(string pixiListPath)
    {
        string section = "SPRITE";
        int lineNumber = 0;
        foreach (string originalLine in File.ReadLines(pixiListPath))
        {
            lineNumber++;
            string line = StripComment(originalLine);
            if (line.EndsWith(':'))
            {
                section = line[..^1].Replace("_", "").Replace(" ", "").ToUpperInvariant();
                continue;
            }

            Match entry = ListEntry.Match(line);
            if (!entry.Success)
                continue;

            byte slot = byte.Parse(entry.Groups["slot"].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            string? directory = section switch
            {
                "SPRITE" when slot < 0xC0 => "sprites",
                "SPRITE" when slot < 0xD0 => "shooters",
                "SPRITE" when slot < 0xE0 => "generators",
                "CLUSTER" => "cluster",
                "EXTENDED" => "extended",
                "MINOREXTENDED" => "misc_sprites/minorextended",
                "BOUNCE" => "misc_sprites/bounce",
                "SMOKE" => "misc_sprites/smoke",
                "SPINNINGCOIN" => "misc_sprites/spinningcoin",
                "SCORE" => "misc_sprites/score",
                _ => null
            };
            if (directory != null)
                yield return new(slot, entry.Groups["path"].Value, directory, lineNumber);
        }
    }

    /// <summary>Resolves a direct ASM entry or the ASM file named in a JSON or CFG descriptor.</summary>
    private static string? ResolveAsm(string entryPath)
    {
        string? asm;
        switch (Path.GetExtension(entryPath).ToLowerInvariant())
        {
            case ".asm":
                return entryPath;
            case ".json":
                using (var description = JsonDocument.Parse(File.ReadAllText(entryPath)))
                {
                    var root = description.RootElement;
                    if (root.TryGetProperty("Type", out var type) && type.TryGetByte(out byte value) && value == 0)
                        return null;
                    asm = root.TryGetProperty("AsmFile", out var file) ? file.GetString() : null;
                }
                break;
            case ".cfg":
                string[] fields = File.ReadLines(entryPath).Select(StripComment)
                    .Where(line => line.Length != 0).Take(5).ToArray();
                if (fields.Length == 0 ||
                    (byte.TryParse(fields[0], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte cfgType) && cfgType == 0))
                    return null;
                asm = fields.Length == 5 ? fields[4].Trim('"') : null;
                break;
            default:
                return null;
        }

        if (string.IsNullOrWhiteSpace(asm))
            throw new InvalidDataException($"PIXI entry '{entryPath}' does not specify a primary ASM file.");
        string source = Resolve(Path.GetDirectoryName(entryPath)!, asm);
        if (!File.Exists(source))
            throw new FileNotFoundException($"Primary ASM '{source}' referenced by PIXI descriptor '{entryPath}' was not found.", source);
        return source;
    }

}
