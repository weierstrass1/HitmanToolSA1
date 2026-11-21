using System.Text;
using HitmanFormatReader;

namespace HitmanBackend;

/// <summary>Generates the ROM tables for poses, geometry, and callback profiles.</summary>
public static class HitboxTables
{
    /// <summary>Key for final geometry, including offsets in both orientations.</summary>
    private readonly record struct Geometry(int X, int XFlip, int Y, int YFlip, int Width, int Height);
    /// <summary>Key for legacy actions and callbacks by collision family.</summary>
    private readonly record struct Profile(string Contact, string Miss, string PlayerContact, string PlayerMiss,
        string? SpriteContact, string? SpriteMiss, string? ExtendedContact, string? ExtendedMiss,
        string? ClusterContact, string? ClusterMiss);
    /// <summary>References to the geometry, callback profile, and historical ID of one pose entry.</summary>
    private readonly record struct Entry(int GeometryOffset, int ProfileOffset, int CallbackId);

    /// <summary>Deduplicates geometry, profiles, and ordered poses, preserves legacy callback IDs in Y, and checks the ROM bank budget.</summary>
    /// <param name="infos">Contextos y poses con referencias locales de caja.</param>
    /// <param name="legacyEntries">Entries in the historical order used for callback IDs.</param>
    /// <param name="actions">Nombres de las rutinas en el orden de su tabla de punteros.</param>
    /// <returns>ASM tables ready to include in the patch.</returns>
    public static string Build(List<HitboxInfo> infos, List<Hitbox> legacyEntries, List<string> actions)
    {
        var geometries = new Dictionary<Geometry, int>();
        var profiles = new Dictionary<Profile, int>
        {
            [new("Nothing", "Nothing", "Nothing", "Nothing", null, null, null, null, null, null)] = 0
        };
        var compatibilityIds = legacyEntries.Select((box, id) => (box, id))
            .ToDictionary(entry => entry.box, entry => entry.id);
        var poseOffsets = new Dictionary<Entry[], int>(new PoseComparer());
        var contextPoses = new List<int>();
        var poseStarts = new List<int>();
        var entries = new List<Entry>();

        foreach (var info in infos)
        {
            var boxes = info.Hitboxes.Select(box => new Entry(
                2 * Intern(geometries, new Geometry(box.XOffset, box.XOffsetFlip,
                    box.YOffset, box.YOffsetFlip, box.Width, box.Height)),
                2 * Intern(profiles, GetProfile(box)),
                compatibilityIds[box])).ToArray();
            foreach (var pose in info.PosesHitboxes)
            {
                var sequence = pose.Hitboxes.Select(index => boxes[index]).ToArray();
                if (!poseOffsets.TryGetValue(sequence, out int offset))
                {
                    offset = poseStarts.Count * 2;
                    poseOffsets.Add(sequence, offset);
                    poseStarts.Add(entries.Count * 2);
                    entries.AddRange(sequence);
                    entries.Add(new(0xFFFF, 0, 0));
                }
                contextPoses.Add(offset);
            }
        }

        // All tables below are read with the same DBR. The word offset tables
        // and the long pointers must fit in that bank with the flip pointer tables.
        long size = 16L + 2L * (contextPoses.Count + poseStarts.Count)
            + 6L * entries.Count + 12L * geometries.Count + 20L * profiles.Count + 3L * actions.Count;
        if (size > 0x10000)
            throw new InvalidDataException($"Hitbox tables require {size} bytes and exceed one ROM bank.");

        var data = new StringBuilder();
        Write("ContextPosesHitboxes", contextPoses);
        Write("PoseHitboxesIndexers", poseStarts);
        Write("PoseHitboxes", entries.Select(entry => entry.GeometryOffset));
        Write("PoseHitboxProfiles", entries.Select(entry => entry.ProfileOffset));
        Write("PoseHitboxCallbackIds", entries.Select(entry => entry.CallbackId));

        var geometryRows = geometries.Keys.ToArray();
        Write("HitBoxXOffsets", geometryRows.Select(box => box.X));
        Write("HitBoxXOffsetsFlip", geometryRows.Select(box => box.XFlip));
        Write("HitBoxYOffsets", geometryRows.Select(box => box.Y));
        Write("HitBoxYOffsetsFlip", geometryRows.Select(box => box.YFlip));
        Write("HitboxWidths", geometryRows.Select(box => box.Width));
        Write("HitboxHeights", geometryRows.Select(box => box.Height));

        var profileRows = profiles.Keys.ToArray();
        var actionOffsets = actions.Select((name, id) => (name, Offset: id * 3))
            .ToDictionary(entry => entry.name, entry => entry.Offset, StringComparer.Ordinal);
        WriteActions("HitboxActions1", profileRows.Select(profile => profile.Contact));
        WriteActions("HitboxActions2", profileRows.Select(profile => profile.Miss));
        WriteActions("HitboxPlayerActions1", profileRows.Select(profile => profile.PlayerContact));
        WriteActions("HitboxPlayerActions2", profileRows.Select(profile => profile.PlayerMiss));
        WriteActions("HitboxSpriteActions1", profileRows.Select(profile => profile.SpriteContact));
        WriteActions("HitboxSpriteActions2", profileRows.Select(profile => profile.SpriteMiss));
        WriteActions("HitboxExtendedActions1", profileRows.Select(profile => profile.ExtendedContact));
        WriteActions("HitboxExtendedActions2", profileRows.Select(profile => profile.ExtendedMiss));
        WriteActions("HitboxClusterActions1", profileRows.Select(profile => profile.ClusterContact));
        WriteActions("HitboxClusterActions2", profileRows.Select(profile => profile.ClusterMiss));
        data.AppendLine("HitboxActions:");
        foreach (string action in actions)
            data.AppendLine($"\tdl Hitman_Callback_{action}");
        return data.ToString();

        // Emits word values under the given ASM label.
        void Write(string label, IEnumerable<int> words) =>
            data.AppendLine($"{label}:{HexReader.ValuesToString(words.ToArray(), 4)}");

        // Converts names to long-pointer offsets and emits the sentinel for absent actions.
        void WriteActions(string label, IEnumerable<string?> names) =>
            Write(label, names.Select(name => name == null ? 0xFFFF : actionOffsets[name]));
    }

    /// <summary>Builds the dispatch key from legacy actions and effective callbacks by family.</summary>
    private static Profile GetProfile(Hitbox box) => new(box.Action1, box.Action2, box.PlayerAction1, box.PlayerAction2,
        box.Callbacks.GetValueOrDefault("OnSpriteCollision"), box.Callbacks.GetValueOrDefault("OnSpriteNonCollision"),
        box.Callbacks.GetValueOrDefault("OnExtendedCollision"), box.Callbacks.GetValueOrDefault("OnExtendedNonCollision"),
        box.Callbacks.GetValueOrDefault("OnClusterCollision"), box.Callbacks.GetValueOrDefault("OnClusterNonCollision"));

    /// <summary>Returns the existing index for a key or assigns the next available index.</summary>
    private static int Intern<T>(Dictionary<T, int> indices, T key) where T : notnull
    {
        if (!indices.TryGetValue(key, out int index))
        {
            index = indices.Count;
            indices.Add(key, index);
        }
        return index;
    }

    /// <summary>Compares poses by their ordered sequences of entries.</summary>
    private sealed class PoseComparer : IEqualityComparer<Entry[]>
    {
        /// <summary>Checks whether two poses contain the same entries in the same order.</summary>
        public bool Equals(Entry[]? x, Entry[]? y) => ReferenceEquals(x, y) ||
            (x != null && y != null && x.AsSpan().SequenceEqual(y));

        /// <summary>Hashes the ordered sequence of pose entries.</summary>
        public int GetHashCode(Entry[] entries)
        {
            var hash = new HashCode();
            foreach (var entry in entries)
                hash.Add(entry);
            return hash.ToHashCode();
        }
    }
}

