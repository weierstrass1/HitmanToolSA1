namespace HitmanFormatReader
{
    /// <summary>Collision context parsed from one HitboxInfo file.</summary>
    public class HitboxInfo
    {
        /// <summary>Base name used to generate the context define.</summary>
        public string ContextName = string.Empty;
        /// <summary>Poses in index order.</summary>
        public required List<PoseHitboxes> PosesHitboxes;
        /// <summary>Boxes addressed by local indices in pose lists.</summary>
        public required List<Hitbox> Hitboxes;
    }
}

