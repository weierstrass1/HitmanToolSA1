using HitmanBackend;
using HitmanBackend.DTO;
using HitmanFormatReader;
using Tool = HitmanBackend.HitmanTool;

namespace HitmanTool.Tests;

public class BackendTests
{
    [Theory]
    [InlineData(2, "db", "$12")]
    [InlineData(3, "dw", "$012")]
    [InlineData(4, "dw", "$0012")]
    [InlineData(5, "dl", "$00012")]
    [InlineData(6, "dl", "$000012")]
    public void Hex_formatter_selects_directive_and_padding(int size, string directive, string value) =>
        Assert.Equal($"\n\t{directive} {value},${255.ToString($"X{size}")}", HexReader.ValuesToString([18, 255], size));

    [Fact]
    public void Hex_formatter_handles_empty_and_linebreaks()
    {
        Assert.Empty(HexReader.ValuesToString(null, 2));
        Assert.Empty(HexReader.ValuesToString([], 2));
        var text = HexReader.ValuesToString(Enumerable.Range(0, 17).ToArray(), 2);
        Assert.Contains(";0000-0010\n\tdb $10", text);
    }

    [Fact]
    public void Hex_reader_extracts_only_dollar_prefixed_hexadecimal_literals() =>
        Assert.Equal([10, 255, 0x123456], HexReader.GetValues("10, $0a $FF $123456"));

    [Fact]
    public void Hex_reader_does_not_treat_nonhexadecimal_letters_as_hex_digits() =>
        Assert.Equal([0xAB], HexReader.GetValues("$ABz"));

    [Fact]
    public void Context_defines_accumulate_pose_counts()
    {
        Assert.Equal("!AHitboxContext = $0000\n!BHitboxContext = $0002\n".Replace("\n", Environment.NewLine),
            TableCreator.Defines([Samples.Info("A", [], [], []), Samples.Info("B", [], [])]));
        Assert.Empty(TableCreator.Defines([]));
    }

    [Fact]
    public void Routine_includes_keep_order_inside_namespace() =>
        Assert.Equal("namespace Hitman_Callback\n\tincsrc \"../Routines/Z.asm\"\n\tincsrc \"../Routines/A.asm\"\nnamespace off\n".Replace("\n", Environment.NewLine),
            TableCreator.RoutinesIncludes(["Z", "A"]));

    [Fact]
    public void Discovery_linked_context_overrides_central_and_missing_directory_is_allowed()
    {
        using var fs = new Sandbox();
        fs.Write("central/Context.hitboxinfo", "invalid");
        var linked = fs.Write("linked/Context.hitboxinfo", Samples.Sectioned);
        var info = Assert.Single(Tool.GetAllHitboxInfo("central", [linked]));
        Assert.Equal("Context", info.ContextName);
        Assert.Single(info.Hitboxes);
        Assert.Empty(Tool.GetAllHitboxInfo("absent"));
        Assert.Single(Tool.GetAllHitboxInfo("absent", [linked], new HitmanLog()));
    }

    [Fact]
    public void Discovery_rejects_duplicate_linked_names()
    {
        using var fs = new Sandbox();
        var a = fs.Write("a/Box.hitboxinfo", Samples.Sectioned);
        var b = fs.Write("b/Box.hitboxinfo", Samples.Sectioned);
        var ex = Assert.Throws<InvalidDataException>(() => Tool.GetAllHitboxInfo("absent", [a, b]));
        Assert.Contains(a, ex.Message);
        Assert.Contains(b, ex.Message);
    }

    [Fact]
    public void Discovery_aggregates_file_errors_and_rejects_invalid_context_names()
    {
        using var fs = new Sandbox();
        fs.Write("inputs/bad-name.hitboxinfo", Samples.Sectioned);
        fs.Write("inputs/Broken.hitboxinfo", "not a table");
        var ex = Assert.Throws<InvalidDataException>(() => Tool.GetAllHitboxInfo("inputs"));
        Assert.Contains("bad-name", ex.Message);
        Assert.Contains("Broken", ex.Message);
    }

    [Fact]
    public void Discovery_reports_missing_linked_file()
    {
        using var fs = new Sandbox();
        Assert.Throws<InvalidDataException>(() => Tool.GetAllHitboxInfo("absent", ["Missing.hitboxinfo"]));
    }

    [Fact]
    public void Historical_boxes_and_actions_are_distinct_in_first_appearance_order()
    {
        var first = Samples.Box(first: "A", second: "B", dependencies: ["C", "A"]);
        var second = Samples.Box(1, "B", "D");
        var boxes = Tool.GetAllHitboxes([Samples.Info("A", [first, second]), Samples.Info("B", [Samples.Box(first: "A", second: "B")])]);
        Assert.Equal(new[] { first, second }, boxes);
        Assert.Equal(new[] { "A", "B", "C", "D" }, Tool.GetAllActions(boxes));
        Assert.Empty(Tool.GetAllHitboxes([]));
        Assert.Empty(Tool.GetAllActions([]));
    }

    [Fact]
    public void Hitbox_tables_deduplicate_geometry_profiles_and_pose_sequences()
    {
        var a = Samples.Box();
        var b = Samples.Box(first: "Contact");
        var info = Samples.Info("A", [a, b], [0, 1], [0, 1], [], [1, 0]);
        string text = HitboxTables.Build([info], [a, b], ["Nothing", "Contact"]);
        Assert.Equal([0, 0, 2, 4], Samples.Words(text, "ContextPosesHitboxes"));
        Assert.Equal([0, 6, 8], Samples.Words(text, "PoseHitboxesIndexers"));
        Assert.Equal([0, 0, 65535, 65535, 0, 0, 65535], Samples.Words(text, "PoseHitboxes"));
        Assert.Equal([0, 2, 0, 0, 2, 0, 0], Samples.Words(text, "PoseHitboxProfiles"));
        Assert.Equal([0, 1, 0, 0, 1, 0, 0], Samples.Words(text, "PoseHitboxCallbackIds"));
        Assert.Equal([16], Samples.Words(text, "HitboxWidths"));
        Assert.Equal([0, 3], Samples.Words(text, "HitboxActions1"));
        Assert.Equal([65535, 65535], Samples.Words(text, "HitboxSpriteActions1"));
        Assert.Contains("dl Hitman_Callback_Contact", text);
    }

    [Fact]
    public void Hitbox_tables_keep_nonplayer_profiles_separate_and_use_long_pointer_offsets()
    {
        var a = Samples.Box(callbacks: new Dictionary<string, string> { ["OnSpriteCollision"] = "Other" });
        var b = Samples.Box(1, callbacks: new Dictionary<string, string> { ["OnClusterNonCollision"] = "Other" });
        string text = HitboxTables.Build([Samples.Info("A", [a, b], [0, 1])], [a, b], ["Nothing", "Other"]);
        Assert.Equal([65535, 3, 65535], Samples.Words(text, "HitboxSpriteActions1"));
        Assert.Equal([65535, 65535, 3], Samples.Words(text, "HitboxClusterActions2"));
        Assert.Equal([0, 1], Samples.Words(text, "HitBoxXOffsets"));
    }

    [Fact]
    public void Hitbox_tables_reject_more_than_one_bank()
    {
        var boxes = Enumerable.Range(0, 6000).Select(x => Samples.Box(x)).ToArray();
        var info = Samples.Info("Huge", boxes, Enumerable.Range(0, boxes.Length).ToArray());
        Assert.Contains("exceed", Assert.Throws<InvalidDataException>(() => HitboxTables.Build([info], boxes.ToList(), ["Nothing"])).Message);
    }

    [Fact]
    public void External_defines_are_sorted_relative_and_exclude_generated_defines()
    {
        using var fs = new Sandbox();
        fs.Write("extra/Z.asm", ""); fs.Write("extra/A.asm", ""); fs.Write("extra/HitmanDefines.asm", ""); fs.Write("extra/note.txt", "");
        Assert.Equal("incsrc \"../../extra/A.asm\"\nincsrc \"../../extra/Z.asm\"", ExtraDefines.BuildIncsrcFile("extra"));
        Assert.Empty(ExtraDefines.BuildIncsrcFile("absent"));
    }

    [Fact]
    public void Property_defines_allocate_each_slot_table_and_pair_mask()
    {
        var properties = new[] { Prop("ActionFlag"), Prop("Other") };
        string text = CustomPropertiesManager.GeneratePropertyDefines(properties);
        Assert.Contains("!Hitman_Prop_ActionFlag = !HitmanCustomProperties+(!MaxSprites*$00)", text);
        Assert.Contains("!Hitman_Prop_Other = !HitmanCustomProperties+(!MaxSprites*$01)", text);
        Assert.Contains("!Hitman_Prop_SpriteActionFlag = !Hitman_Prop_ActionFlag", text);
        Assert.Contains("!HitmanPairMask = !HitmanCustomProperties+(!MaxSprites*$02)", text);
        Assert.Contains("(((!MaxSprites*(!MaxSprites-1)/2)+7)/8)", text);
        Assert.Contains("!HitmanRAMEnd = !HitmanPairMask+!HitmanPairMaskSize", text);
        Assert.DoesNotContain("= !Hitman_Prop_ActionFlag", CustomPropertiesManager.GeneratePropertyDefines([Prop("ActionFlag"), Prop("SpriteActionFlag")]));
    }

    [Fact]
    public void Property_setup_initializes_custom_and_vanilla_branches()
    {
        string text = CustomPropertiesManager.GetPropSetup([Prop("Flag")]);
        Assert.Contains("AND #$08", text);
        Assert.Contains("LDA CustomProperty_Custom_Flag,y", text);
        Assert.Contains("LDA CustomProperty_Vanilla_Flag,y", text);
        Assert.Equal(2, text.Split("STA !Hitman_Prop_Flag,x").Length - 1);
        Assert.Equal(2, text.Split("RTS").Length - 1);
    }

    [Fact]
    public void Property_json_loading_is_sorted_and_linked_properties_override_only_custom()
    {
        using var fs = new Sandbox();
        fs.Write("CustomProperties/Z.json", "{\"Name\":\"Z\",\"DefaultValueVanilla\":\"00\",\"DefaultValueCustom\":\"01\"}");
        fs.Write("CustomProperties/A.json", "{\"Name\":\"A\",\"DefaultValueVanilla\":\"00\",\"DefaultValueCustom\":\"01\"}");
        Assert.Equal(new[] { "A", "Z" }, CustomPropertiesManager.GetProperties().Select(p => p.Name));
        fs.Write("CustomProperties/PerSprite/custom.json", "{\"Name\":\"Same\",\"IsCustom\":true,\"Properties\":[]}");
        fs.Write("CustomProperties/PerSprite/vanilla.json", "{\"Name\":\"Same\",\"IsCustom\":false,\"Properties\":[]}");
        var linked = fs.Write("linked.json", "{\"Name\":\"Original\",\"IsCustom\":false,\"Properties\":[]}");
        var sprites = CustomPropertiesManager.GetPropertiesPerSprite(new Dictionary<string, string> { ["Same"] = linked });
        Assert.Equal(2, sprites.Length);
        Assert.All(sprites, s => Assert.Equal("Same", s.Name));
        Assert.Single(sprites, s => s.IsCustom);
        fs.Write("null.json", "null");
        Assert.Throws<InvalidDataException>(() => CustomPropertiesManager.GetPropertiesPerSprite(new Dictionary<string, string> { ["Null"] = "null.json" }));
    }

    [Fact]
    public void Property_tables_have_256_values_each_with_overrides_at_pixi_slots()
    {
        using var fs = new Sandbox();
        fs.Write("list.txt", "AB Custom.json\n");
        var vanilla = Samples.Sprite(id: "FE");
        var custom = Samples.Sprite("Custom", true, null);
        vanilla.Properties = [new() { PropertyName = "Flag", Value = "12" }];
        custom.Properties = [new() { PropertyName = "Flag", Value = "34" }];
        string text = CustomPropertiesManager.GeneratePropertiesTables([Prop("Flag")], [vanilla, custom], "list.txt", new HitmanLog());
        var tables = text.Split("CustomProperty_Custom_Flag:");
        int[] Read(string body) => body.Split('\n').Select(l => l.Trim()).Where(l => l.StartsWith("db "))
            .SelectMany(l => l[3..].Split(',')).Select(v => Convert.ToInt32(v[1..], 16)).ToArray();
        var v = Read(tables[0]); var c = Read(tables[1]);
        Assert.Equal(256, v.Length); Assert.Equal(256, c.Length);
        Assert.Equal(0x12, v[254]); Assert.Equal(0x34, c[171]);
        Assert.All(v.Where((_, i) => i != 254), n => Assert.Equal(0, n));
        Assert.All(c.Where((_, i) => i != 171), n => Assert.Equal(1, n));
    }

    private static CustomProperty Prop(string name) => new() { Name = name, DefaultValueVanilla = "00", DefaultValueCustom = "01" };
}
