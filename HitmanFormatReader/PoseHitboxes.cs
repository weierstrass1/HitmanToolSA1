namespace HitmanFormatReader
{
    /// <summary>Ordered references to the boxes of one pose.</summary>
    public class PoseHitboxes
    {
        /// <summary>Local box indices in evaluation order.</summary>
        public required int[] Hitboxes { get; init; }
        /// <summary>Hashes the ordered sequence of box references.</summary>
        public override int GetHashCode()
        {
            string key = string.Join('-', Hitboxes.Select(i => i.ToString()).ToArray());
            return key.GetHashCode();
        }
        /// <summary>Compares two poses by the order and values of their box references.</summary>
        public override bool Equals(object? obj)
        {
            if (obj is not PoseHitboxes ph)
                return false;
            if (Hitboxes == null && ph.Hitboxes == null)
                return true;
            if (Hitboxes == null || ph.Hitboxes == null)
                return false;
            if(Hitboxes.Length != ph.Hitboxes.Length) 
                return false;
            for (int i = 0; i < Hitboxes.Length; i++)
                if (!Hitboxes[i].Equals(ph.Hitboxes[i]))
                    return false;
            return true;
        }
    }
}

