namespace HitmanBackend.DTO;
/// <summary>Properties and callback configuration for a sprite type.</summary>
[Serializable]
public class SpriteCustomProperties
{
    /// <summary>Hexadecimal number of a vanilla sprite.</summary>
    public string? ID { get; set; }
    /// <summary>Sprite name used to resolve its PIXI entry when custom.</summary>
    public required string Name { get; set; }
    /// <summary>Selects the custom or vanilla property table.</summary>
    public required bool IsCustom { get; set; }
    /// <summary>Callbacks specific to vanilla interaction families.</summary>
    public VanillaCallbackContexts? VanillaCallbacks { get; set; }
    /// <summary>Legacy callback used as a fallback by family and for explosions.</summary>
    public string? VanillaInteractionCallback { get; set; }
    /// <summary>Additional routines requested by the vanilla callback configuration.</summary>
    public string[] VanillaInteractionDependencies { get; set; } = [];
    /// <summary>Property values assigned to the sprite.</summary>
    public required CustomPropertyPerSprite[] Properties {  get; set; }
}

/// <summary>Names of sprite-specific callbacks for vanilla event families.</summary>
public class VanillaCallbackContexts
{
    /// <summary>Callback for regular fireball hits.</summary>
    public string? Fireball { get; set; }
    /// <summary>Callback for Yoshi fireball hits.</summary>
    public string? YoshiFireball { get; set; }
    /// <summary>Callback for quake hits.</summary>
    public string? Quake { get; set; }
    /// <summary>Callback for normal sprite-to-sprite interactions.</summary>
    public string? SpriteSprite { get; set; }
    /// <summary>Callback for a cape dive that hits the ground.</summary>
    public string? CapeDive { get; set; }
}
