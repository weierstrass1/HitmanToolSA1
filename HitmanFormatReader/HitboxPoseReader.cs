using System.Text.RegularExpressions;

namespace HitmanFormatReader;

/// <summary>Reads pose selectors and ordered lists of boxes.</summary>
internal static class HitboxPoseReader
{
    /// <summary>Pattern for a pose declaration and its optional box list.</summary>
    private static readonly Regex Pose = new(@"^\.Pose(?<ids>[0-9]+(?:\.\.[0-9]+)?|\[[0-9, \t]+\])(?:\s+(?<boxes>.+))?$");

    /// <summary>Expands pose selectors, resolves box indices, and leaves undeclared poses empty.</summary>
    internal static List<PoseHitboxes> Read(HitboxSection section, IReadOnlyDictionary<int, int> indices)
    {
        section.RequireBlock();
        var poses = new Dictionary<int, (int[] Boxes, HitboxLine Line)>();
        foreach (var line in section.Rows)
        {
            var match = Pose.Match(line.Text);
            if (!match.Success)
                throw line.Error($"Invalid pose declaration '{line.Text}'.");

            string list = match.Groups["boxes"].Value;
            int[] boxes = list.Length == 0 ? [] : ReadList(list, line);
            if (boxes.Distinct().Count() != boxes.Length)
                throw line.Error("The same hitbox appears twice in one pose.");

            foreach (int id in ReadSelector(match.Groups["ids"].Value, line))
                if (!poses.TryAdd(id, (boxes, line)))
                    throw line.Error($"Pose{id} is already declared on line {poses[id].Line.Number}.");
        }

        if (poses.Count == 0)
            throw section.Header.Error("HitboxesPerPose must declare at least one pose.");

        var result = Enumerable.Range(0, poses.Keys.Max() + 1)
            .Select(_ => new PoseHitboxes { Hitboxes = [] }).ToList();
        foreach (var (id, pose) in poses)
        {
            var boxes = pose.Boxes.Select(box => indices.TryGetValue(box, out int index)
                ? index : throw pose.Line.Error($"Pose{id} refers to undefined Hitbox{box}."));
            result[id] = new PoseHitboxes { Hitboxes = boxes.ToArray() };
        }
        return result;
    }

    /// <summary>Parses a comma-separated list of decimal IDs.</summary>
    private static int[] ReadList(string value, HitboxLine line) =>
        value.Split(',', StringSplitOptions.TrimEntries).Select(id => HitboxValues.Id(id, line)).ToArray();

    /// <summary>Expands a single pose, an inclusive range, or an explicit list.</summary>
    private static IEnumerable<int> ReadSelector(string value, HitboxLine line)
    {
        if (value.StartsWith('['))
            return ReadList(value[1..^1], line);

        var bounds = value.Split("..");
        int start = HitboxValues.Id(bounds[0], line);
        int end = bounds.Length == 1 ? start : HitboxValues.Id(bounds[1], line);
        if (end < start)
            throw line.Error($"Inverted pose range '{value}'.");
        return Enumerable.Range(start, end - start + 1);
    }
}

