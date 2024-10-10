using HitmanBackend.DTO;
using Newtonsoft.Json;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace HitmanBackend;
public class CustomPropertiesManager
{
    public static CustomProperty[] GetProperties()
    {
        string[] propertyFiles = Directory.GetFiles("CustomProperties", "*.json");
        CustomProperty[] props = new CustomProperty[propertyFiles.Length];
        string content;
        for (int i = 0; i < propertyFiles.Length; i++) 
        {
            content = File.ReadAllText(propertyFiles[i]);
            props[i] = JsonConvert.DeserializeObject<CustomProperty>(content)!;
        }

        return props;
    }
    public static SpriteCustomProperties[] GetPropertiesPerSprite()
    {
        string directory = Path.Combine("CustomProperties", "PerSprite");
        string[] propertyFiles = Directory.GetFiles(directory, "*.json");
        SpriteCustomProperties[] props = new SpriteCustomProperties[propertyFiles.Length];
        string content;
        for (int i = 0; i < propertyFiles.Length; i++)
        {
            content = File.ReadAllText(propertyFiles[i]);
            props[i] = JsonConvert.DeserializeObject<SpriteCustomProperties>(content)!;
        }

        return props;
    }
    public static string GeneratePropertyDefines(CustomProperty[] props)
    {
        StringBuilder sb = new();
        int i = 0;
        foreach (CustomProperty prop in props)
        {
            sb.AppendLine($"!Hitman_Prop_{prop.Name} = !HitmanCustomProperties+(!MaxSprites*${i.ToString("X2")})");
            i++;
        }
        return sb.ToString();
    }
    public static string GeneratePropertiesTables(CustomProperty[] props, SpriteCustomProperties[] spriteProps, string pixiListPath)
    {
        string listContent = File.ReadAllText(pixiListPath);

        var listContentFiltered = Regex.Matches(listContent, @"[0-9a-fA-F]{2}\s+[A-Za-z0-9-_\/\\]+\.(cfg|json)");

        SpriteCustomProperties[] CustomSprites = spriteProps.Where(sp => sp.IsCustom).ToArray();
        SpriteCustomProperties[] VanillaSprites = spriteProps
            .Where(sp => !sp.IsCustom && !string.IsNullOrWhiteSpace(sp.ID) && Regex.IsMatch(sp.ID, @"[0-9a-fA-F]{2}"))
            .ToArray();
        var spriteNames = CustomSprites
            .ToDictionary(sp => sp.Name, sp => sp);
        string[] split;
        string id;
        string name;

        foreach (Match m in listContentFiltered)
        {
            split = m.Value
                .Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(v => v.Trim())
                .ToArray();
            id = split[0];
            name = Path.GetFileNameWithoutExtension(split[1]);
            if (spriteNames.ContainsKey(name))
                spriteNames[name].ID = id;
        }

        StringBuilder sbCustom = new();
        StringBuilder sbVanilla = new();
        Dictionary<int, string> vals;

        foreach (var prop in props)
        {
            sbCustom.AppendLine($"CustomProperty_Custom_{prop.Name}:");
            vals = getPropValues(prop.Name, CustomSprites);
            sbCustom.AppendLine(buildTable(prop.DefaultValueCustom, vals));
            sbVanilla.AppendLine($"CustomProperty_Vanilla_{prop.Name}:");
            vals = getPropValues(prop.Name, VanillaSprites);
            sbVanilla.AppendLine(buildTable(prop.DefaultValueVanilla, vals));
        }

        return $"{sbVanilla}\n{sbCustom}";
    }
    private static Dictionary<int,string> getPropValues(string propName, SpriteCustomProperties[] spriteProps)
    {
        var props = spriteProps
            .Where(sp => sp.Properties.FirstOrDefault(p => p.PropertyName == propName) != null)
            .ToDictionary(sp => Convert.ToInt32(sp.ID, 16),
                sp => sp.Properties.FirstOrDefault(p => p.PropertyName == propName).Value);
        return props;
    }
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
        return sb.ToString();
    }

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
