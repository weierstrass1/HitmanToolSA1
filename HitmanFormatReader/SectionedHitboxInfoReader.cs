using System.Text.RegularExpressions;

namespace HitmanFormatReader;

/// <summary>Coordinates section parsing for the alternative format.</summary>
internal sealed class SectionedHitboxInfoReader(string source, Func<string, bool>? routineExists)
{
    /// <summary>Matches a hitbox section header with a decimal ID.</summary>
    private static readonly Regex BoxHeader = new(@"^Hitbox(?<id>[0-9]+)$");

    /// <summary>Validates section headers and resolves global geometry, box definitions, and pose references.</summary>
    internal HitboxInfo Read(string input)
    {
        var sections = new Dictionary<string, HitboxSection>(StringComparer.Ordinal);
        var boxes = new Dictionary<int, HitboxSection>();
        foreach (var section in HitboxSection.Read(input, source))
        {
            if (!sections.TryAdd(section.Name, section))
                throw section.Header.Error($"Duplicate section '{section.Name}'.");
            if (section.Name is "HitboxesPerPose" or "AllHitboxCallbacks" or "AllHitboxes" or "AllHitboxXOffset" or "AllHitboxYOffset")
                continue;

            section.RequireBlock();
            var match = BoxHeader.Match(section.Name);
            if (!match.Success)
                throw section.Header.Error($"Unknown section '{section.Name}'.");
            int id = HitboxValues.Id(match.Groups["id"].Value, section.Header);
            if (!boxes.TryAdd(id, section))
                throw section.Header.Error($"Duplicate Hitbox{id}.");
        }

        var boxReader = new HitboxDefinitionReader(routineExists);
        int x = sections.GetValueOrDefault("AllHitboxXOffset")?.ReadOffset() ?? 0;
        int y = sections.GetValueOrDefault("AllHitboxYOffset")?.ReadOffset() ?? 0;
        var geometry = sections.GetValueOrDefault("AllHitboxes");
        if (geometry != null && (sections.ContainsKey("AllHitboxXOffset") || sections.ContainsKey("AllHitboxYOffset")))
            throw geometry.Header.Error("AllHitboxes cannot be combined with AllHitboxXOffset or AllHitboxYOffset.");
        var globals = HitboxDefinitionReader.ReadGlobalGeometry(geometry, x, y);
        var defaults = sections.TryGetValue("AllHitboxCallbacks", out var callbacks)
            ? boxReader.ReadDefaults(callbacks) : new Dictionary<string, string>(StringComparer.Ordinal);
        var indices = new Dictionary<int, int>();
        var hitboxes = new List<Hitbox>();
        foreach (var (id, section) in boxes)
        {
            indices.Add(id, hitboxes.Count);
            hitboxes.Add(boxReader.Read(section, id, globals, defaults));
        }

        if (!sections.TryGetValue("HitboxesPerPose", out var poses))
            throw new InvalidDataException($"{source}: HitboxesPerPose must declare at least one pose.");
        return new HitboxInfo { Hitboxes = hitboxes, PosesHitboxes = HitboxPoseReader.Read(poses, indices) };
    }
}

