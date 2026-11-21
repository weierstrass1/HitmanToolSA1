using System.Text.RegularExpressions;

namespace HitmanFormatReader;

/// <summary>Shared entry point for both HitboxInfo formats.</summary>
public static class HitboxInfoReader
{
    /// <summary>Header pattern used to detect the input format.</summary>
    private static readonly Regex Headers = new(
        @"(?m)^\s*(?<name>[A-Za-z][A-Za-z0-9]*):");

    /// <summary>Detects the file format, rejects mixed declarations, and returns validated hitbox data.</summary>
    /// <param name="input">Contents of the hitbox file.</param>
    /// <param name="source">Path or name used in diagnostics.</param>
    /// <param name="routineExists">Optional routine name validator.</param>
    /// <returns>Resolved poses and hitboxes.</returns>
    /// <exception cref="InvalidDataException">The file contains invalid declarations.</exception>
    public static HitboxInfo ReadHitboxInfo(string input, string source = "<hitboxinfo>",
        Func<string, bool>? routineExists = null)
    {
        var headers = Headers.Matches(input).Select(match => match.Groups["name"].Value).ToArray();
        bool sectioned = headers.Any(name => name is "HitboxesPerPose" or "AllHitboxCallbacks"
            or "AllHitboxes" or "AllHitboxXOffset" or "AllHitboxYOffset" || Regex.IsMatch(name, @"^Hitbox[0-9]+$"));
        if (sectioned && headers.Contains("PoseHitBoxes"))
            throw new InvalidDataException($"{source}: legacy and sectioned hitbox declarations cannot be mixed.");
        return sectioned ?
            new SectionedHitboxInfoReader(source, routineExists).Read(input) :
            LegacyHitboxInfoReader.ReadHitboxInfo(input, source, routineExists);
    }
}

