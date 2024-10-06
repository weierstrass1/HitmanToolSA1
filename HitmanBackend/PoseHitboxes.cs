using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HitmanBackend
{
    public class PoseHitboxes
    {
        public required int[] Hitboxes { get; init; }
        public override int GetHashCode()
        {
            string key = string.Join('-', Hitboxes.Select(i => i.ToString()).ToArray());
            return key.GetHashCode();
        }
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
