using HitmanBackend.DTO;
using System.Text;
using System.Text.RegularExpressions;

namespace HitmanBackend;

/// <summary>Set of response routines for the supported vanilla event families.</summary>
public sealed record VanillaCallbackProfile(
    string? SpriteSprite,
    string? Fireball,
    string? YoshiFireball,
    string? Quake,
    string? CapeDive,
    string? Explosion)
{
    // Keep this order in sync with the offsets in DispatchVanillaCallback.asm.
    /// <summary>Pointer order expected by the ASM dispatcher.</summary>
    public string?[] Entries => [SpriteSprite, Fireball, YoshiFireball, Quake, CapeDive, Explosion];
}

/// <summary>Assigns vanilla callback profiles and generates their long-pointer table.</summary>
public static class VanillaCallbacks
{
    /// <summary>Treats blank routine names as absent.</summary>
    private static string? DefinedName(string? name) =>
        string.IsNullOrWhiteSpace(name) ? null : name;

    /// <summary>Resolves legacy fallbacks, deduplicates profiles, and writes each sprite's profile ID.</summary>
    public static VanillaCallbackProfile[] Prepare(SpriteCustomProperties[] sprites)
    {
        var profiles = new List<VanillaCallbackProfile>();
        var ids = new Dictionary<VanillaCallbackProfile, int>();

        foreach (var sprite in sprites)
        {
            string? legacy = DefinedName(sprite.VanillaInteractionCallback);
            var contexts = sprite.VanillaCallbacks;
            var profile = new VanillaCallbackProfile(
                DefinedName(contexts?.SpriteSprite) ?? legacy,
                DefinedName(contexts?.Fireball) ?? legacy,
                DefinedName(contexts?.YoshiFireball) ?? legacy,
                DefinedName(contexts?.Quake) ?? legacy,
                DefinedName(contexts?.CapeDive) ?? legacy,
                legacy);

            int id = 0;
            if (profile.Entries.Any(name => name != null) && !ids.TryGetValue(profile, out id))
            {
                if (profiles.Count == byte.MaxValue)
                    throw new InvalidDataException("At most 255 vanilla callback profiles are supported.");

                profiles.Add(profile);
                id = profiles.Count;
                ids.Add(profile, id);
            }

            sprite.Properties = sprite.Properties.Where(p => p.PropertyName != "VanillaCallbackID")
                .Append(new CustomPropertyPerSprite { PropertyName = "VanillaCallbackID", Value = id.ToString("X2") })
                .ToArray();
        }

        foreach (var name in GetNames(profiles).Concat(sprites.SelectMany(s => s.VanillaInteractionDependencies)))
            if (!Regex.IsMatch(name, @"\A[A-Za-z_][A-Za-z0-9_]*\z"))
                throw new InvalidDataException($"Invalid vanilla callback name: {name}");

        return profiles.ToArray();
    }

    /// <summary>Enumerates distinct routine names used by the profiles in sorted order.</summary>
    public static IEnumerable<string> GetNames(IEnumerable<VanillaCallbackProfile> profiles) =>
        profiles.SelectMany(profile => profile.Entries).OfType<string>()
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal);

    /// <summary>Emits six pointers per profile and reserves profile zero for absent callbacks.</summary>
    public static string BuildTable(IEnumerable<VanillaCallbackProfile> profiles)
    {
        var result = new StringBuilder();
        result.AppendLine("HitmanVanillaCallbackTable:");
        result.AppendLine("	dl $000000,$000000,$000000,$000000,$000000,$000000");
        foreach (var profile in profiles)
        {
            var addresses = profile.Entries.Select(name => name == null ? "$000000" : $"Hitman_Callback_{name}");
            result.AppendLine($"	dl {string.Join(',', addresses)}");
        }
        return result.ToString();
    }
}
