using System.Globalization;
using System.Text.RegularExpressions;

namespace HitmanFormatReader;

/// <summary>Reads the historical format based on ASM tables.</summary>
internal static class LegacyHitboxInfoReader
{
    /// <summary>Pattern for a legacy table label with optional inline data.</summary>
    private static readonly Regex Header = new(
        @"^(?<name>[A-Za-z_][A-Za-z0-9_-]*):\s*(?<data>.*)$");
    /// <summary>Pattern for a legacy routine name.</summary>
    private static readonly Regex Symbol = new(@"\A[A-Za-z_][A-Za-z0-9_]*\z");

    /// <summary>One row in a legacy table and its source line.</summary>
    private readonly record struct Row(string Text, int Line);
    /// <summary>Numeric value read from a legacy table and its source line.</summary>
    private readonly record struct Value(int Number, int Line);

    /// <summary>Validates legacy tables and resolves poses, geometry, and actions with their original semantics.</summary>
    public static HitboxInfo ReadHitboxInfo(
        string input, string source = "<hitboxinfo>", Func<string, bool>? routineExists = null)
    {
        var errors = new List<string>();
        var tables = ReadTables(input, errors);
        var poses = ReadValues(tables, "PoseHitBoxes", "db", byte.MaxValue, errors);
        var xs = ReadWords("HitboxXOffset");
        var ys = ReadWords("HitboxYOffset");
        var xFlipped = ReadWords("HitboxXOffsetFlip");
        var yFlipped = ReadWords("HitboxYOffsetFlip");
        var widths = ReadWords("HitboxWidth");
        var heights = ReadWords("HitboxHeight");
        var action1 = ReadWords("HitboxAction1");
        var action2 = ReadWords("HitboxAction2");
        var actions = ReadRoutines(tables, "Actions", routineExists, errors);
        var dependencies = ReadRoutines(tables, "Dependencies", routineExists, errors);

        int hitboxCount = widths.Count;
        if (poses.Count == 0)
            errors.Add("PoseHitBoxes must contain at least one pose ending in $FF.");
        else if (poses[^1].Number != 0xFF)
            errors.Add($"line {poses[^1].Line}, PoseHitBoxes: last pose is missing its $FF terminator.");

        int poseNumber = 0;
        var poseHitboxes = new List<PoseHitboxes>();
        var pose = new List<int>();
        foreach (Value entry in poses)
        {
            if (entry.Number == 0xFF)
            {
                poseHitboxes.Add(new() { Hitboxes = pose.ToArray() });
                pose.Clear();
                poseNumber++;
            }
            else
            {
                if (entry.Number >= hitboxCount)
                    errors.Add($"line {entry.Line}, PoseHitBoxes: pose {poseNumber} references ${entry.Number:X2}, but only {hitboxCount} hitboxes are defined.");
                pose.Add(entry.Number);
            }
        }

        RequireCount("HitboxXOffset", xs);
        RequireCount("HitboxYOffset", ys);
        RequireCount("HitboxXOffsetFlip", xFlipped);
        RequireCount("HitboxYOffsetFlip", yFlipped);
        RequireCount("HitboxHeight", heights, required: true);
        RequireCount("HitboxAction1", action1);
        RequireCount("HitboxAction2", action2);
        ValidateActions("HitboxAction1", action1);
        ValidateActions("HitboxAction2", action2);
        if (hitboxCount > byte.MaxValue)
            errors.Add("HitboxWidth defines more than 255 hitboxes. $FF is reserved as the pose terminator.");

        if (errors.Count != 0)
            throw new InvalidDataException(string.Join(Environment.NewLine, errors.Select(error => $"{source}: {error}")));

        var hitboxes = new List<Hitbox>(hitboxCount);
        for (int i = 0; i < hitboxCount; i++)
        {
            int x = At(xs, i);
            int y = At(ys, i);
            hitboxes.Add(new()
            {
                XOffset = x,
                XOffsetFlip = At(xFlipped, i, x),
                YOffset = y,
                YOffsetFlip = At(yFlipped, i, y),
                Width = widths[i].Number,
                Height = heights[i].Number,
                Action1 = action1.Count == 0 ? "Nothing" : actions[action1[i].Number / 2],
                Action2 = action2.Count == 0 ? "Nothing" : actions[action2[i].Number / 2],
                Dependencies = dependencies
            });
        }

        return new HitboxInfo { PosesHitboxes = poseHitboxes, Hitboxes = hitboxes };

        // Reads the operands of a word table.
        List<Value> ReadWords(string table) => ReadValues(tables, table, "dw", ushort.MaxValue, errors);

        // Requires one table value per box where applicable.
        void RequireCount(string table, List<Value> values, bool required = false)
        {
            if ((required || tables.ContainsKey(table)) && values.Count != hitboxCount)
                errors.Add($"{table}: expected {hitboxCount} values (one per HitboxWidth), found {values.Count}.");
        }

        // Checks that each action offset is even and within the routine table.
        void ValidateActions(string table, List<Value> values)
        {
            foreach (Value value in values)
                if ((value.Number & 1) != 0 || value.Number / 2 >= actions.Length)
                    errors.Add($"line {value.Line}, {table}: ${value.Number:X4} is not an even byte offset into Actions ({actions.Length} entries).");
        }
    }

    /// <summary>Returns the value at the given column or its fallback for an empty table.</summary>
    private static int At(List<Value> values, int index, int fallback = 0) =>
        values.Count == 0 ? fallback : values[index].Number;

    /// <summary>Groups rows by table label and collects structural errors.</summary>
    private static Dictionary<string, List<Row>> ReadTables(string input, List<string> errors)
    {
        var tables = new Dictionary<string, List<Row>>(StringComparer.Ordinal);
        List<Row>? current = null;
        using var reader = new StringReader(input);
        int lineNumber = 0;
        while (reader.ReadLine() is { } text)
        {
            lineNumber++;
            int comment = text.IndexOf(';');
            string line = (comment >= 0 ? text[..comment] : text).Trim();
            if (line.Length == 0)
                continue;

            Match header = Header.Match(line);
            if (header.Success)
            {
                string name = header.Groups["name"].Value;
                current = new();
                if (!tables.TryAdd(name, current))
                    errors.Add($"line {lineNumber}: duplicate table '{name}'.");
                line = header.Groups["data"].Value.Trim();
                if (line.Length == 0)
                    continue;
            }

            if (current == null)
                errors.Add($"line {lineNumber}: expected a table label.");
            else
                current.Add(new(line, lineNumber));
        }
        return tables;
    }

    /// <summary>Reads hexadecimal table operands and checks their bounds.</summary>
    private static List<Value> ReadValues(Dictionary<string, List<Row>> tables, string table,
        string directive, int maximum, List<string> errors)
    {
        var values = new List<Value>();
        foreach (Row row in tables.GetValueOrDefault(table) ?? [])
        {
            foreach (string token in ReadTokens(row, table, directive, errors))
            {
                if (token.StartsWith('$') &&
                    int.TryParse(token.AsSpan(1), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out int value) &&
                    value >= 0 && value <= maximum)
                    values.Add(new(value, row.Line));
                else
                    errors.Add($"line {row.Line}, {table}: invalid {directive} hexadecimal value '{token}' (max ${maximum:X}).");
            }
        }
        return values;
    }

    /// <summary>Reads routine names and checks their existence when a validator is provided.</summary>
    private static string[] ReadRoutines(Dictionary<string, List<Row>> tables, string table,
        Func<string, bool>? routineExists, List<string> errors)
    {
        var routines = new List<string>();
        foreach (Row row in tables.GetValueOrDefault(table) ?? [])
        {
            foreach (string token in ReadTokens(row, table, "dw", errors))
            {
                if (!Symbol.IsMatch(token))
                    errors.Add($"line {row.Line}, {table}: invalid routine name '{token}'.");
                else if (routineExists != null && !routineExists(token))
                    errors.Add($"line {row.Line}, {table}: routine 'Routines/{token}.asm' was not found.");

                routines.Add(token);
            }
        }
        return routines.ToArray();
    }

    /// <summary>Validates the ASM directive and splits row operands.</summary>
    private static string[] ReadTokens(Row row, string table, string directive, List<string> errors)
    {
        int separator = row.Text.IndexOfAny([' ', '\t']);
        if (separator < 0 || !row.Text[..separator].Equals(directive, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add($"line {row.Line}, {table}: expected '{directive}' followed by values.");
            return [];
        }
        return row.Text[(separator + 1)..].Split(',', StringSplitOptions.TrimEntries);
    }
}

