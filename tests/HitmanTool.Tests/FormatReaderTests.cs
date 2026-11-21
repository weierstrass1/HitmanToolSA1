using HitmanFormatReader;

namespace HitmanTool.Tests;

public class FormatReaderTests
{
    [Fact]
    public void Sectioned_resolves_geometry_source_id_and_sparse_poses()
    {
        var info = HitboxInfoReader.ReadHitboxInfo(Samples.Sectioned.Replace(".Pose0", ".Pose2"));
        var box = Assert.Single(info.Hitboxes);
        Assert.Equal((2, 65534, 8, 12, 7), (box.XOffset, box.YOffset, box.Width, box.Height, box.SourceId));
        Assert.Equal(3, info.PosesHitboxes.Count);
        Assert.Empty(info.PosesHitboxes[0].Hitboxes);
        Assert.Empty(info.PosesHitboxes[1].Hitboxes);
        Assert.Equal([0], info.PosesHitboxes[2].Hitboxes);
    }

    [Theory]
    [InlineData(".Pose0..2 7")]
    [InlineData(".Pose[0,1,2] 7")]
    public void Selectors_expand_inclusive_ranges_and_lists(string selector)
    {
        var info = HitboxInfoReader.ReadHitboxInfo(Samples.Sectioned.Replace(".Pose0 7", selector));
        Assert.Equal(3, info.PosesHitboxes.Count);
        Assert.All(info.PosesHitboxes, p => Assert.Equal([0], p.Hitboxes));
    }

    [Fact]
    public void Globals_translate_after_mirroring_and_local_pivots_override_defaults()
    {
        var input = "AllHitboxes:\n .XOffset $0003\n .YOffset $0004\n .XPivot $0010\n .YPivot $0020\n" +
            Samples.Sectioned.Replace(" .Width", " .XPivot $000A\n .Width");
        var box = Assert.Single(HitboxInfoReader.ReadHitboxInfo(input).Hitboxes);
        Assert.Equal((5, 13, 2, 58), (box.XOffset, box.XOffsetFlip, box.YOffset, box.YOffsetFlip));
    }

    [Fact]
    public void Old_global_offsets_translate_before_mirroring()
    {
        var box = Assert.Single(HitboxInfoReader.ReadHitboxInfo("AllHitboxXOffset: $0003\n" +
            Samples.Sectioned.Replace(" .Width", " .XPivot $000A\n .Width")).Hitboxes);
        Assert.Equal(5, box.XOffset);
        Assert.Equal(7, box.XOffsetFlip);
    }

    [Fact]
    public void Callback_defaults_are_inherited_and_local_fields_override()
    {
        var seen = new List<string>();
        var input = "AllHitboxCallbacks:\n .OnPlayerCollision Default\n .OnSpriteCollision Other\n" +
            Samples.Sectioned.Replace(" .Width", " .OnPlayerCollision Local\n .Width");
        var box = Assert.Single(HitboxInfoReader.ReadHitboxInfo(input, "fixture", n => { seen.Add(n); return true; }).Hitboxes);
        Assert.Equal("Local", box.PlayerAction1);
        Assert.Equal("Nothing", box.PlayerAction2);
        Assert.Equal("Other", box.Callbacks["OnSpriteCollision"]);
        Assert.Equal(new[] { "Default", "Other", "Local" }, seen);
    }

    [Fact]
    public void Omitted_geometry_uses_documented_defaults()
    {
        var box = Assert.Single(HitboxInfoReader.ReadHitboxInfo("Hitbox0:\nHitboxesPerPose:\n .Pose0 0").Hitboxes);
        Assert.Equal((0, 0, 0, 0, 16, 16), (box.XOffset, box.XOffsetFlip, box.YOffset, box.YOffsetFlip, box.Width, box.Height));
    }

    public static IEnumerable<object[]> InvalidSections()
    {
        yield return [Samples.Sectioned + "PoseHitBoxes: db $FF", "mixed"];
        yield return [Samples.Sectioned + "Hitbox7:\n", "Duplicate"];
        yield return [Samples.Sectioned + "Hitbox07:\n", "Duplicate"];
        yield return [Samples.Sectioned + "Unknown:\n", "Unknown"];
        yield return [Samples.Sectioned.Replace("Hitbox7:", "Hitbox32768:"), "ID"];
        yield return [Samples.Sectioned.Replace(".Pose0 7", ".Pose0 9"), "undefined"];
        yield return [Samples.Sectioned.Replace(".Pose0 7", ".Pose0 7,7"), "twice"];
        yield return [Samples.Sectioned.Replace(".Pose0 7", ".Pose2..1 7"), "Inverted"];
        yield return [Samples.Sectioned.Replace(".Pose0 7", ".Pose[0,0] 7"), "already"];
        yield return [Samples.Sectioned.Replace(".Pose0 7", ".Pose32768 7"), "ID"];
        yield return [Samples.Sectioned.Replace(".Pose0 7", ""), "at least one"];
        yield return [Samples.Sectioned.Replace(".Width $0008", ".Width $0000"), "positive"];
        yield return [Samples.Sectioned.Replace(".Height $000C", ".Height 12"), "hexadecimal"];
        yield return [Samples.Sectioned.Replace(".Width $0008", ".Width $10000"), "hexadecimal"];
        yield return [Samples.Sectioned.Replace(".Width $0008", ".Width $0008\n .Width $0009"), "Duplicate"];
        yield return [Samples.Sectioned.Replace(".Width $0008", ".Depth $0008"), "Unknown"];
        yield return [Samples.Sectioned.Replace(".Width $0008", "Width $0008"), "Expected"];
        yield return [Samples.Sectioned.Replace("Hitbox7:", "Hitbox7: $01"), "Unexpected"];
        yield return ["AllHitboxes:\n .XOffset $7FFF\n" + Samples.Sectioned, "signed"];
        yield return ["AllHitboxes:\n .Bogus $0001\n" + Samples.Sectioned, "Unknown"];
        yield return ["AllHitboxes:\n .XOffset $0001\n .XOffset $0002\n" + Samples.Sectioned, "Duplicate"];
        yield return ["AllHitboxes:\nAllHitboxXOffset: $0001\n" + Samples.Sectioned, "combined"];
        yield return ["AllHitboxYOffset: $0001\n unexpected\n" + Samples.Sectioned, "Expected"];
        yield return ["AllHitboxCallbacks:\n .OnPlayerCollision bad-name\n" + Samples.Sectioned, "Invalid routine"];
        yield return ["AllHitboxCallbacks:\n .OnEnemyCollision Foo\n" + Samples.Sectioned, "Unknown"];
        yield return ["AllHitboxCallbacks:\n .OnPlayerCollision Foo\n .OnPlayerCollision Bar\n" + Samples.Sectioned, "Duplicate"];
        yield return ["orphan\n" + Samples.Sectioned, "Expected"];
    }

    [Theory, MemberData(nameof(InvalidSections))]
    public void Sectioned_rejects_invalid_declarations_with_source(string input, string reason)
    {
        var ex = Assert.Throws<InvalidDataException>(() => HitboxInfoReader.ReadHitboxInfo(input, "bad.hbi"));
        Assert.Contains("bad.hbi", ex.Message);
        Assert.Contains(reason, ex.Message);
    }

    [Fact]
    public void Missing_callback_reports_routine_and_line()
    {
        var ex = Assert.Throws<InvalidDataException>(() => HitboxInfoReader.ReadHitboxInfo(
            "AllHitboxCallbacks:\n .OnPlayerCollision Missing\n" + Samples.Sectioned, "bad.hbi", _ => false));
        Assert.Contains("line 2", ex.Message);
        Assert.Contains("Missing", ex.Message);
    }

    [Fact]
    public void Legacy_preserves_words_empty_pose_and_offset_fallbacks()
    {
        var info = HitboxInfoReader.ReadHitboxInfo(Samples.Legacy);
        Assert.Equal(2, info.PosesHitboxes.Count);
        Assert.Equal([0], info.PosesHitboxes[0].Hitboxes);
        Assert.Empty(info.PosesHitboxes[1].Hitboxes);
        var box = Assert.Single(info.Hitboxes);
        Assert.Equal((65534, 65534, 0, 0, 8, 12), (box.XOffset, box.XOffsetFlip, box.YOffset, box.YOffsetFlip, box.Width, box.Height));
        Assert.Equal("Nothing", box.Action1);
        Assert.Equal("Nothing", box.Action2);
    }

    [Fact]
    public void Legacy_resolves_action_offsets_dependencies_and_explicit_flips()
    {
        var info = HitboxInfoReader.ReadHitboxInfo(Samples.Legacy +
            "Actions: dw First,Second\nHitboxAction1: dw $0002\nHitboxAction2: dw $0000\nDependencies: dw Extra\nHitboxXOffsetFlip: dw $0004\nHitboxYOffsetFlip: dw $0005", routineExists: _ => true);
        var box = Assert.Single(info.Hitboxes);
        Assert.Equal(("Second", "First", 4, 5), (box.Action1, box.Action2, box.XOffsetFlip, box.YOffsetFlip));
        Assert.Equal(new[] { "Extra" }, box.Dependencies);
    }

    [Theory]
    [InlineData("PoseHitBoxes: db $00", "terminator")]
    [InlineData("PoseHitBoxes: db $FF\nHitboxWidth: dw $01", "HitboxHeight")]
    [InlineData("PoseHitBoxes: db $01,$FF\nHitboxWidth: dw $01\nHitboxHeight: dw $01", "references")]
    [InlineData("PoseHitBoxes: db $100", "invalid")]
    [InlineData("PoseHitBoxes: dw $FF", "expected")]
    [InlineData("PoseHitBoxes: db $FF\nPoseHitBoxes: db $FF", "duplicate")]
    [InlineData("orphan", "label")]
    [InlineData("PoseHitBoxes: db $FF\nActions: dw bad-name", "routine")]
    public void Legacy_rejects_malformed_tables(string input, string reason)
    {
        var ex = Assert.Throws<InvalidDataException>(() => HitboxInfoReader.ReadHitboxInfo(input, "legacy.hbi"));
        Assert.Contains("legacy.hbi", ex.Message);
        Assert.Contains(reason, ex.Message);
    }

    [Theory]
    [InlineData("0001")]
    [InlineData("0004")]
    public void Legacy_rejects_odd_or_out_of_range_action_offsets(string offset)
    {
        Assert.Throws<InvalidDataException>(() => HitboxInfoReader.ReadHitboxInfo(Samples.Legacy +
            $"Actions: dw First\nHitboxAction1: dw ${offset}"));
    }

    [Fact]
    public void Comments_do_not_change_format_detection()
    {
        Assert.Single(HitboxInfoReader.ReadHitboxInfo("; Hitbox0:\n" + Samples.Legacy).Hitboxes);
    }

    [Fact]
    public void Hitbox_equality_and_hash_follow_historical_identity()
    {
        var a = Samples.Box(dependencies: ["Unused"]);
        var b = Samples.Box();
        Assert.True(a.Equals(b));
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.False(a.Equals(null));
        Assert.False(a.Equals("box"));
        Assert.NotEqual(a, Samples.Box(1));
        Assert.NotEqual(a, Samples.Box(first: "Other"));
        Assert.NotEqual(a, Samples.Box(callbacks: new Dictionary<string, string> { ["OnPlayerCollision"] = "Other" }));
    }

    [Fact]
    public void Pose_equality_and_hash_preserve_order_and_length()
    {
        var a = new PoseHitboxes { Hitboxes = [1, 2] };
        var b = new PoseHitboxes { Hitboxes = [1, 2] };
        Assert.True(a.Equals(b));
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.False(a.Equals(new PoseHitboxes { Hitboxes = [2, 1] }));
        Assert.False(a.Equals(new PoseHitboxes { Hitboxes = [1] }));
        Assert.False(a.Equals(null));
        Assert.False(a.Equals("pose"));
    }
}
