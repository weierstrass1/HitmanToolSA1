namespace HitmanBackend.DTO;
/// <summary>Assignment of a property to a specific sprite.</summary>
[Serializable]
public class CustomPropertyPerSprite
{
    /// <summary>Name of the property being overridden.</summary>
    public string PropertyName {  get; set; }
    /// <summary>Hexadecimal value that replaces the property default.</summary>
    public string Value { get; set; }
}
