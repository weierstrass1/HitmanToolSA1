namespace HitmanFormatReader;

/// <summary>Text of one input line with its source file and line number.</summary>
internal readonly record struct HitboxLine(string Source, int Number, string Text)
{
    /// <summary>Creates a format error with the location of the input line.</summary>
    internal InvalidDataException Error(string message) => new($"{Source}: line {Number}: {message}");
}

/// <summary>A section header and the rows belonging to it.</summary>
internal sealed record HitboxSection(string Name, string Value, HitboxLine Header, List<HitboxLine> Rows)
{
    /// <summary>Strips comments and groups rows under headers while preserving their locations.</summary>
    internal static List<HitboxSection> Read(string input, string source)
    {
        using var reader = new StringReader(input);
        var sections = new List<HitboxSection>();
        HitboxSection? current = null;
        int number = 0;
        while (reader.ReadLine() is { } raw)
        {
            number++;
            string text = raw.Split(';', 2)[0].Trim();
            if (text.Length == 0)
                continue;

            var line = new HitboxLine(source, number, text);
            int colon = text.IndexOf(':');
            if (colon >= 0)
            {
                current = new(text[..colon].TrimEnd(), text[(colon + 1)..].Trim(), line, []);
                sections.Add(current);
            }
            else if (current != null)
                current.Rows.Add(line);
            else
                throw line.Error($"Expected a section, found '{text}'.");
        }
        return sections;
    }

    /// <summary>Requires a header with no inline value.</summary>
    internal void RequireBlock()
    {
        if (Value.Length != 0)
            throw Header.Error($"Unexpected value after '{Name}:'.");
    }

    /// <summary>Reads the global offset in the header and rejects unexpected rows.</summary>
    internal int ReadOffset()
    {
        int offset = unchecked((short)HitboxValues.Word(Value, Header));
        if (Rows.Count != 0)
            throw Rows[0].Error($"Expected a section, found '{Rows[0].Text}'.");
        return offset;
    }
}

