namespace HitmanBackend.DTO;
[Serializable]
public class SpriteCustomProperties
{
    public string? ID { get; set; }
    public required string Name { get; set; }
    public required bool IsCustom { get; set; }
    public required CustomPropertyPerSprite[] Properties {  get; set; }
}
