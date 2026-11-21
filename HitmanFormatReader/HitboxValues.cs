using System.Globalization;

namespace HitmanFormatReader;

/// <summary>Converts literals in the alternative format and reports errors on the source line.</summary>
internal static class HitboxValues
{
    /// <summary>Reads a decimal ID within the supported index range.</summary>
    internal static int Id(string value, HitboxLine line) =>
        int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int id) && id <= 0x7FFF
            ? id : throw line.Error($"Invalid decimal ID '{value}' (expected 0–32767).");

    /// <summary>Reads an unsigned hexadecimal literal of up to 16 bits.</summary>
    internal static int Word(string value, HitboxLine line) =>
        value.StartsWith('$') && ushort.TryParse(value.AsSpan(1), NumberStyles.AllowHexSpecifier,
            CultureInfo.InvariantCulture, out ushort word)
            ? word : throw line.Error($"Expected a hexadecimal word, found '{value}'.");
}

