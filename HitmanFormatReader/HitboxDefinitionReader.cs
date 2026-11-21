using System.Text.RegularExpressions;

namespace HitmanFormatReader;

/// <summary>Reads geometry and callback declarations in the alternative format.</summary>
internal sealed class HitboxDefinitionReader(Func<string, bool>? routineExists)
{
    /// <summary>Pattern matching a valid ASM routine name.</summary>
    private static readonly Regex Routine = new(@"\A[A-Za-z_][A-Za-z0-9_]*\z");
    /// <summary>Pattern matching callback fields accepted by the alternative reader.</summary>
    private static readonly Regex Callback = new(@"\AOn(?:Player|Sprite|Extended|Cluster)(?:Collision|NonCollision)\z");

    /// <summary>Validates the global callbacks inherited by boxes.</summary>
    internal Dictionary<string, string> ReadDefaults(HitboxSection section)
    {
        section.RequireBlock();
        var callbacks = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var line in section.Rows)
        {
            var (field, value) = ReadField(line);
            ReadCallback(callbacks, field, value, line);
        }
        return callbacks;
    }

    /// <summary>Global offsets and pivots with the mirroring convention of the chosen syntax.</summary>
    internal readonly record struct GlobalGeometry(int XOffset, int YOffset, int? XPivot, int? YPivot,
        bool OffsetAfterMirror);

    /// <summary>Reads the global geometry block or preserves the historical interpretation of standalone offsets.</summary>
    internal static GlobalGeometry ReadGlobalGeometry(HitboxSection? section, int oldX, int oldY)
    {
        if (section == null)
            return new(oldX, oldY, null, null, OffsetAfterMirror: false);

        section.RequireBlock();
        var fields = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var line in section.Rows)
        {
            var (field, value) = ReadField(line);
            if (field is not ("XOffset" or "YOffset" or "XPivot" or "YPivot"))
                throw line.Error($"Unknown AllHitboxes field '.{field}'.");
            if (!fields.TryAdd(field, unchecked((short)HitboxValues.Word(value, line))))
                throw line.Error($"Duplicate field '.{field}'.");
        }

        return new(fields.GetValueOrDefault("XOffset"), fields.GetValueOrDefault("YOffset"),
            fields.TryGetValue("XPivot", out int xPivot) ? xPivot : null,
            fields.TryGetValue("YPivot", out int yPivot) ? yPivot : null, OffsetAfterMirror: true);
    }

    /// <summary>Resolves final geometry and fills omitted callbacks from the file defaults.</summary>
    internal Hitbox Read(HitboxSection section, int id, GlobalGeometry globals,
        Dictionary<string, string> defaults)
    {
        section.RequireBlock();
        var fields = new Dictionary<string, int>(StringComparer.Ordinal);
        var callbacks = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var line in section.Rows)
        {
            var (field, value) = ReadField(line);
            if (Callback.IsMatch(field))
            {
                ReadCallback(callbacks, field, value, line);
                continue;
            }
            int number = field switch
            {
                "Width" or "Height" => ReadDimension(field, value, line),
                "XOffset" or "YOffset" or "XPivot" or "YPivot" => unchecked((short)HitboxValues.Word(value, line)),
                _ => throw line.Error($"Unknown hitbox field '.{field}'.")
            };
            if (!fields.TryAdd(field, number))
                throw line.Error($"Duplicate field '.{field}'.");
        }

        foreach (var (field, value) in defaults)
            callbacks.TryAdd(field, value);

        int x = fields.GetValueOrDefault("XOffset");
        int y = fields.GetValueOrDefault("YOffset");
        int width = fields.GetValueOrDefault("Width", 16);
        int height = fields.GetValueOrDefault("Height", 16);
        int? pivotX = fields.TryGetValue("XPivot", out int localX) ? localX : globals.XPivot;
        int? pivotY = fields.TryGetValue("YPivot", out int localY) ? localY : globals.YPivot;
        int normalX = x + globals.XOffset;
        int normalY = y + globals.YOffset;
        int flipX = Mirror(x, globals.XOffset, width, pivotX, globals.OffsetAfterMirror);
        int flipY = Mirror(y, globals.YOffset, height, pivotY, globals.OffsetAfterMirror);
        return new Hitbox
        {
            XOffset = EncodeOffset(normalX, section.Header), YOffset = EncodeOffset(normalY, section.Header),
            XOffsetFlip = EncodeOffset(flipX, section.Header), YOffsetFlip = EncodeOffset(flipY, section.Header),
            Width = width, Height = height,
            Action1 = "Nothing", Action2 = "Nothing", SourceId = id, Callbacks = callbacks
        };
    }

    /// <summary>Validates the field, routine name, and routine existence when a validator is provided.</summary>
    private void ReadCallback(Dictionary<string, string> callbacks, string field, string value, HitboxLine line)
    {
        if (!Callback.IsMatch(field))
            throw line.Error($"Unknown callback field '.{field}'.");
        if (!Routine.IsMatch(value))
            throw line.Error($"Invalid routine name '{value}'.");
        if (routineExists != null && !routineExists(value))
            throw line.Error($"Routine 'Routines/{value}.asm' was not found.");
        if (!callbacks.TryAdd(field, value))
            throw line.Error($"Duplicate callback '.{field}'.");
    }

    /// <summary>Splits a field from its value, using its source line for diagnostics.</summary>
    private static (string Field, string Value) ReadField(HitboxLine line)
    {
        string text = line.Text;
        int split = text.IndexOfAny([' ', '\t']);
        if (!text.StartsWith('.') || split < 2)
            throw line.Error($"Expected '.Field value', found '{text}'.");
        return (text[1..split], text[split..].Trim());
    }

    /// <summary>Reads a hexadecimal dimension and requires it to be positive.</summary>
    private static int ReadDimension(string field, string value, HitboxLine line)
    {
        int dimension = HitboxValues.Word(value, line);
        return dimension > 0 ? dimension : throw line.Error($".{field} must be positive.");
    }

    /// <summary>Reflects an axis around its effective pivot and applies translation in the format's required order.</summary>
    private static int Mirror(int offset, int translation, int dimension, int? pivot, bool offsetAfterMirror)
    {
        if (pivot is not int axis)
            return offset + translation;

        int reflected = 2 * axis - offset - dimension;
        return offsetAfterMirror ? reflected + translation : reflected - translation;
    }

    /// <summary>Validates a signed 16-bit offset and encodes it as an ASM word.</summary>
    private static int EncodeOffset(int offset, HitboxLine line) => offset is >= short.MinValue and <= short.MaxValue
        ? unchecked((ushort)offset) : throw line.Error("Computed offset exceeds the signed 16-bit range.");
}

