using HitmanBackend.DTO;
using Newtonsoft.Json;
using System.Text;
using System.Text.RegularExpressions;

namespace HitmanBackend;
/// <summary>Loads properties and emits their RAM layout, ROM tables, and slot initialization.</summary>
public class CustomPropertiesManager
{
    /// <summary>Loads global property definitions from their JSON files.</summary>
    public static CustomProperty[] GetProperties()
    {
        string[] propertyFiles = Directory.GetFiles("CustomProperties", "*.json").Order(StringComparer.Ordinal).ToArray();
        CustomProperty[] props = new CustomProperty[propertyFiles.Length];
        string content;
        for (int i = 0; i < propertyFiles.Length; i++) 
        {
            content = File.ReadAllText(propertyFiles[i]);
            props[i] = JsonConvert.DeserializeObject<CustomProperty>(content)!;
        }

        return props;
    }
    /// <summary>Combines central per-sprite properties with those linked from PIXI.</summary>
    public static SpriteCustomProperties[] GetPropertiesPerSprite(
        IReadOnlyDictionary<string, string>? linkedProperties = null)
    {
        string directory = Path.Combine("CustomProperties", "PerSprite");
        var traditional = Directory.Exists(directory)
            ? Directory.EnumerateFiles(directory, "*.json").Select(ReadProperties).ToList()
            : new List<SpriteCustomProperties>();

        if (linkedProperties != null)
        {
            traditional.RemoveAll(sprite => sprite.IsCustom && linkedProperties.ContainsKey(sprite.Name));
            foreach ((string name, string path) in linkedProperties)
            {
                var sprite = ReadProperties(path);
                sprite.Name = name;
                sprite.IsCustom = true;
                traditional.Add(sprite);
            }
        }

        return traditional.ToArray();
    }

    /// <summary>Deserializes a sprite property file.</summary>
    private static SpriteCustomProperties ReadProperties(string file) =>
        JsonConvert.DeserializeObject<SpriteCustomProperties>(File.ReadAllText(file))
        ?? throw new InvalidDataException($"Could not read Hitman properties from '{file}'.");

    /// <summary>Allocates RAM for per-slot properties and the collision pair mask.</summary>
    public static string GeneratePropertyDefines(CustomProperty[] props)
    {
        StringBuilder sb = new();
        int i = 0;
        foreach (CustomProperty prop in props)
        {
            sb.AppendLine($"!Hitman_Prop_{prop.Name} = !HitmanCustomProperties+(!MaxSprites*${i.ToString("X2")})");
            i++;
        }
        if (props.Any(p => p.Name == "ActionFlag") && !props.Any(p => p.Name == "SpriteActionFlag"))
            sb.AppendLine("!Hitman_Prop_SpriteActionFlag = !Hitman_Prop_ActionFlag");
        sb.AppendLine($"!HitmanPairMask = !HitmanCustomProperties+(!MaxSprites*${props.Length:X2})");
        // For n = MaxSprites - 1, sum ceil(i / 8) for rows i = 0 through n.
        sb.AppendLine("!HitmanPairMaskSize = ((((!MaxSprites-1)>>3)+1)*((!MaxSprites-1)-(4*((!MaxSprites-1)>>3))))");
        sb.AppendLine("!HitmanRAMEnd = !HitmanPairMask+!HitmanPairMaskSize");
        return sb.ToString();
    }
    /// <summary>Emits vanilla and custom values indexed by sprite number.</summary>
    public static string GeneratePropertiesTables(CustomProperty[] props, SpriteCustomProperties[] spriteProps, string pixiListPath, HitmanLog? log = null)
    {
        SpriteCustomProperties[] CustomSprites = spriteProps.Where(sp => sp.IsCustom).ToArray();
        SpriteCustomProperties[] VanillaSprites = spriteProps
            .Where(sp => !sp.IsCustom && !string.IsNullOrWhiteSpace(sp.ID) && Regex.IsMatch(sp.ID, @"[0-9a-fA-F]{2}"))
            .ToArray();
        var spriteNames = CustomSprites.ToDictionary(sp => sp.Name, sp => sp);

        foreach (var entry in PixiInputs.ReadEntries(pixiListPath))
        {
            if (entry.Directory == "sprites" && spriteNames.TryGetValue(entry.Name, out var sprite))
                sprite.ID = entry.Slot.ToString("X2");
        }
        CustomSprites = CustomSprites.Where(sp => !string.IsNullOrWhiteSpace(sp.ID)).ToArray();

        StringBuilder sbCustom = new();
        StringBuilder sbVanilla = new();
        Dictionary<int, string> vals;

        log?.Title("Custom sprite properties");
        foreach (var prop in props)
        {
            sbCustom.AppendLine($"CustomProperty_Custom_{prop.Name}:");
            vals = getPropValues(prop.Name, CustomSprites, log);
            sbCustom.AppendLine(buildTable(prop.DefaultValueCustom, vals));
        }
        log?.Title("Vanilla sprite properties");
        foreach (var prop in props)
        {
            sbVanilla.AppendLine($"CustomProperty_Vanilla_{prop.Name}:");
            vals = getPropValues(prop.Name, VanillaSprites, log);
            sbVanilla.AppendLine(buildTable(prop.DefaultValueVanilla, vals));
        }

        return $"{sbVanilla}\n{sbCustom}";
    }
    /// <summary>Collects explicit assignments for one property, keyed by sprite number.</summary>
    private static Dictionary<int,string> getPropValues(string propName, SpriteCustomProperties[] spriteProps, HitmanLog? log)
    {
        log?.Info("PROCESS_PROPERTY", ("property", propName));
        var values = new Dictionary<int, string>();
        foreach (var sprite in spriteProps)
        {
            var property = sprite.Properties.FirstOrDefault(p => p.PropertyName == propName);
            if (property == null)
                continue;

            log?.Info("SPRITE_PROPERTY", ("slot", sprite.ID!), ("sprite", sprite.Name),
                ("property", propName), ("value", property.Value));
            values.Add(Convert.ToInt32(sprite.ID, 16), property.Value);
        }
        return values;
    }
    /// <summary>Emits all 256 values for a property, filling omitted entries with its default.</summary>
    private static string buildTable(string defaultValue, Dictionary<int, string> perSprite)
    {
        StringBuilder sb = new();
        string[] vals = new string[16];
        for (int i = 0, j = 0; i < 256; i++, j++)
        {
            if(j == 16)
            {
                sb.AppendLine($"\tdb {string.Join(',', vals)}");
                j = 0;
            }
            vals[j] = $"${(perSprite.ContainsKey(i) ? perSprite[i] : defaultValue)}";
        }
        sb.AppendLine($"\tdb {string.Join(',', vals)}");
        return sb.ToString();
    }

    /// <summary>Generates ASM to populate properties when a sprite slot is initialized.</summary>
    public static string GetPropSetup(CustomProperty[] props)
    {
        StringBuilder sb = new();
        sb.AppendLine("CustomPropertiesSetup:");
        sb.AppendLine("\tLDA !ExtraBits,x");
        sb.AppendLine("\tAND #$08");
        sb.AppendLine("\tBEQ +");
        sb.AppendLine("\tLDA !CustomSpriteNumber,x");
        sb.AppendLine("\tTAY");
        foreach (CustomProperty prop in props)
        {
            sb.AppendLine($"\tLDA CustomProperty_Custom_{prop.Name},y");
            sb.AppendLine($"\tSTA !Hitman_Prop_{prop.Name},x");
        }
        sb.AppendLine("RTS");
        sb.AppendLine("+");
        sb.AppendLine("\tLDA !SpriteNumber,x");
        sb.AppendLine("\tTAY");
        foreach (CustomProperty prop in props)
        {
            sb.AppendLine($"\tLDA CustomProperty_Vanilla_{prop.Name},y");
            sb.AppendLine($"\tSTA !Hitman_Prop_{prop.Name},x");
        }
        sb.AppendLine("RTS");
        return sb.ToString();
    }
}
