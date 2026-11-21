namespace HitmanFormatReader
{
    /// <summary>Final geometry and declared actions of one box.</summary>
    public class Hitbox
    {
        /// <summary>Normal horizontal offset encoded as a word.</summary>
        public required int XOffset { get; init; }
        /// <summary>Mirrored horizontal offset encoded as a word.</summary>
        public required int XOffsetFlip { get; init; }
        /// <summary>Normal vertical offset encoded as a word.</summary>
        public required int YOffset { get; init; }
        /// <summary>Mirrored vertical offset encoded as a word.</summary>
        public required int YOffsetFlip { get; init; }
        /// <summary>Box width in pixels.</summary>
        public required int Width { get; init; }
        /// <summary>Box height in pixels.</summary>
        public required int Height { get; init; }
        /// <summary>Contact routine in the legacy traversal.</summary>
        public required string Action1 { get; init; }
        /// <summary>No-contact routine in the legacy traversal.</summary>
        public required string Action2 { get; init; }
        /// <summary>Routines explicitly requested by the legacy input.</summary>
        public string[] Dependencies { get; init; } = Array.Empty<string>();
        /// <summary>ID declared in the alternative format, used in diagnostics.</summary>
        public int? SourceId { get; init; }
        /// <summary>Callback names by event after applying file-level inheritance.</summary>
        public IReadOnlyDictionary<string, string> Callbacks { get; init; } = new Dictionary<string, string>();
        /// <summary>Mario contact action, with the legacy action as fallback.</summary>
        public string PlayerAction1 => Callbacks.TryGetValue("OnPlayerCollision", out var value) ? value : Action1;
        /// <summary>Mario no-contact action, with the legacy action as fallback.</summary>
        public string PlayerAction2 => Callbacks.TryGetValue("OnPlayerNonCollision", out var value) ? value : Action2;
        /// <summary>Hashes the fields that define the historical box identity.</summary>
        public override int GetHashCode()
        {
            string key = $"{XOffset}-{XOffsetFlip}-{YOffset}-{YOffsetFlip}-{Width}-{Height}-{Action1}-{Action2}-{PlayerAction1}-{PlayerAction2}";
            return key.GetHashCode();
        }
        /// <summary>Compares the historical box identity used to preserve its callback ID.</summary>
        public override bool Equals(object? obj)
        {
            if (obj is not Hitbox hb)
                return false;
            return XOffset == hb.XOffset && XOffsetFlip == hb.XOffsetFlip &&
                YOffset == hb.YOffset && YOffsetFlip == hb.YOffsetFlip &&
                Width == hb.Width && Height == hb.Height &&
                Action1 == hb.Action1 && Action2 == hb.Action2 && PlayerAction1 == hb.PlayerAction1 && PlayerAction2 == hb.PlayerAction2;
        }
    }
}

