namespace HitmanBackend.DTO;
[Serializable]
public class CustomProperty
{
    public required string Name { get; set; }
    public required string DefaultValueVanilla { get; set; }
    public required string DefaultValueCustom { get; set; }
}
