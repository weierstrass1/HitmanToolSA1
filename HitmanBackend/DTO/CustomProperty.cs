namespace HitmanBackend.DTO;
/// <summary>Global definition of a per-slot property and its defaults.</summary>
[Serializable]
public class CustomProperty
{
    /// <summary>Property name exported to ASM.</summary>
    public required string Name { get; set; }
    /// <summary>Initial hexadecimal value for vanilla sprites.</summary>
    public required string DefaultValueVanilla { get; set; }
    /// <summary>Initial hexadecimal value for custom sprites.</summary>
    public required string DefaultValueCustom { get; set; }
}
