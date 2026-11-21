using System.Text.RegularExpressions;

namespace HitmanBackend;

/// <summary>Resolves static dependencies between the ASM routines selected for inclusion.</summary>
public static class RoutineDependencies
{
    /// <summary>Matches global labels declared within the routine.</summary>
    private static readonly Regex Label = new(@"^\s*(?<name>[A-Za-z_][A-Za-z0-9_]*):");
    /// <summary>Matches direct calls and jumps with literal symbol operands.</summary>
    private static readonly Regex DirectTarget = new(
        @"(?:^|:)\s*(?:JSL|JSR|JML|JMP)(?:\.[bwl])?\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*(?=$|:)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    /// <summary>Matches callback labels in the generated namespace.</summary>
    private static readonly Regex ExplicitTarget = new(@"(?<![!\w.])Hitman_Callback_(?<name>[A-Za-z_][A-Za-z0-9_]*)\b");
    /// <summary>Prefix of ASM callback labels in the generated namespace.</summary>
    private const string CallbackPrefix = "Hitman_Callback_";

    /// <summary>Adds transitive dependencies in place and reports missing explicit references with their file and line.</summary>
    /// <param name="routines">Initial routine list to which dependencies are appended.</param>
    /// <param name="directory">Directory containing callback ASM sources.</param>
    /// <param name="baseRoutines">Base symbols excluded from callback lookup.</param>
    /// <param name="log">Log for detected dependencies.</param>
    public static void Expand(List<string> routines, string directory, IEnumerable<string> baseRoutines, HitmanLog log)
    {
        var files = Directory.EnumerateFiles(directory, "*.asm")
            .ToDictionary(path => Path.GetFileNameWithoutExtension(path), StringComparer.Ordinal);
        var baseNames = baseRoutines.ToHashSet(StringComparer.Ordinal);
        var included = routines.ToHashSet(StringComparer.Ordinal);
        for (int index = 0; index < routines.Count; index++)
        {
            string routine = routines[index];
            if (!files.TryGetValue(routine, out string? path))
                throw new FileNotFoundException($"Routine '{routine}' was not found in '{directory}'.");

            var lines = File.ReadLines(path).Select(line => line.Split(';', 2)[0]).ToArray();
            var local = lines.Select(line => Label.Match(line))
                .Where(match => match.Success)
                .Select(match => match.Groups["name"].Value).ToHashSet(StringComparer.Ordinal);
            var dependencies = new SortedSet<string>(StringComparer.Ordinal);
            for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                string line = Regex.Replace(lines[lineIndex], "\"[^\"]*\"", "");
                foreach (Match target in ExplicitTarget.Matches(line))
                {
                    string name = target.Groups["name"].Value;
                    if (!files.ContainsKey(name))
                        throw new InvalidDataException($"{path}:{lineIndex + 1}: callback '{name}' was not found in '{directory}'.");
                    if (name != routine)
                        dependencies.Add(name);
                }

                foreach (Match target in DirectTarget.Matches(line))
                {
                    string name = target.Groups["name"].Value;
                    if (!name.StartsWith(CallbackPrefix, StringComparison.Ordinal) &&
                        !local.Contains(name) && !baseNames.Contains(name) && files.ContainsKey(name))
                        dependencies.Add(name);
                }
            }

            if (dependencies.Count != 0)
                log.Info("ROUTINE_DEPENDENCIES", ("routine", routine), ("dependencies", string.Join(", ", dependencies)));
            foreach (string dependency in dependencies)
                if (included.Add(dependency))
                    routines.Add(dependency);
        }
    }
}

