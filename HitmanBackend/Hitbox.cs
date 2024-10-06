using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HitmanBackend
{
    public class Hitbox
    {
        public required int XOffset { get; init; }
        public required int XOffsetFlip { get; init; }
        public required int YOffset { get; init; }
        public required int YOffsetFlip { get; init; }
        public required int Width { get; init; }
        public required int Height { get; init; }
        public required string Action1 { get; init; }
        public required string Action2 { get; init; }
        public override int GetHashCode()
        {
            string key = $"{XOffset}-{XOffsetFlip}-{YOffset}-{YOffsetFlip}-{Width}-{Height}-{Action1}-{Action2}";
            return key.GetHashCode();
        }
        public override bool Equals(object? obj)
        {
            if (obj is not Hitbox hb)
                return false;
            return XOffset == hb.XOffset && XOffsetFlip == hb.XOffsetFlip &&
                YOffset == hb.YOffset && YOffsetFlip == hb.YOffsetFlip &&
                Width == hb.Width && Height == hb.Height &&
                Action1 == hb.Action1 && Action2 == hb.Action2;
        }
    }
}
