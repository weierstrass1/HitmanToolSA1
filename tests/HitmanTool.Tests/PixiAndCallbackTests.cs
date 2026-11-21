using HitmanBackend;
using HitmanBackend.DTO;

namespace HitmanTool.Tests;

public class PixiAndCallbackTests
{
    [Theory]
    [InlineData("SPRITE", "01", "sprites")]
    [InlineData("SPRITE", "C0", "shooters")]
    [InlineData("SPRITE", "D0", "generators")]
    [InlineData("CLUSTER", "01", "cluster")]
    [InlineData("EXTENDED", "01", "extended")]
    [InlineData("MINOR_EXTENDED", "01", "misc_sprites/minorextended")]
    [InlineData("BOUNCE", "01", "misc_sprites/bounce")]
    [InlineData("SMOKE", "01", "misc_sprites/smoke")]
    [InlineData("SPINNING COIN", "01", "misc_sprites/spinningcoin")]
    [InlineData("SCORE", "01", "misc_sprites/score")]
    public void Pixi_maps_sections_to_resource_families(string section, string slot, string directory)
    {
        using var fs = new Sandbox();
        fs.Write("list.txt", $"{section}:\n{slot} Sprite.asm\n");
        fs.Write($"pixi/{directory}/Sprite.asm", ";#inchbi \"Boxes.hitboxinfo\"");
        var expected = fs.Write($"pixi/{directory}/Boxes.hitboxinfo", Samples.Sectioned);
        Assert.Equal(new[] { expected }, PixiInputs.Discover("pixi", "list.txt").Hitboxes);
    }

    [Theory]
    [InlineData("Sprite.json", "{\"Type\":1,\"AsmFile\":\"Primary.asm\"}")]
    [InlineData("Sprite.cfg", "01\n00\n00\n00\nPrimary.asm\n")]
    [InlineData("Primary.asm", ";#inchbi \"Boxes.hitboxinfo\"\n;#inchmprop \"props.json\"")]
    public void Pixi_resolves_config_and_direct_asm_links(string entry, string content)
    {
        using var fs = new Sandbox();
        fs.Write("list.txt", $"00:01 \"{entry}\" display ; comment\n02 {entry} nodisplay\n");
        fs.Write("pixi/sprites/Primary.asm", ";#inchbi \"Boxes.hitboxinfo\"\n;#inchbi \"Boxes.hitboxinfo\"\n;#inchmprop \"props.json\"");
        fs.Write($"pixi/sprites/{entry}", content);
        var box = fs.Write("pixi/sprites/Boxes.hitboxinfo", Samples.Sectioned);
        var prop = fs.Write("pixi/sprites/props.json", "{}");
        var result = PixiInputs.Discover("pixi", "list.txt");
        Assert.Equal(new[] { box }, result.Hitboxes);
        Assert.Equal(prop, Assert.Single(result.Properties).Value);
    }

    [Theory]
    [InlineData("Sprite.json", "{\"Type\":0}")]
    [InlineData("Sprite.cfg", "00\n")]
    [InlineData("Sprite.cfg", "")]
    [InlineData("Other.txt", "anything")]
    public void Pixi_skips_vanilla_or_unsupported_entries(string entry, string content)
    {
        using var fs = new Sandbox();
        fs.Write("list.txt", $"01 {entry}\n02 Missing.json\nE0 Ignored.asm\nUNKNOWN:\n01 Ignored.asm\n");
        fs.Write($"pixi/sprites/{entry}", content);
        var result = PixiInputs.Discover("pixi", "list.txt");
        Assert.Empty(result.Hitboxes); Assert.Empty(result.Properties);
    }

    [Theory]
    [InlineData(";#inchbi missing", "Invalid Hitman directive")]
    [InlineData(";#inchbi \"wrong.json\"", ".hitboxinfo")]
    [InlineData(";#inchmprop \"wrong.hitboxinfo\"", ".json")]
    [InlineData(";#inchmprop \"props.json\"\n;#inchmprop \"props.json\"", "More than one")]
    public void Pixi_rejects_malformed_directives(string directive, string diagnostic)
    {
        using var fs = new Sandbox();
        fs.Write("list.txt", "01 Sprite.asm");
        fs.Write("pixi/sprites/Sprite.asm", directive);
        fs.Write("pixi/sprites/props.json", "{}");
        var ex = Assert.Throws<InvalidDataException>(() => PixiInputs.Discover("pixi", "list.txt"));
        Assert.Contains(diagnostic, ex.Message);
        Assert.Contains("line", ex.Message);
    }

    [Fact]
    public void Pixi_reports_missing_referenced_input()
    {
        using var fs = new Sandbox();
        fs.Write("list.txt", "01 Sprite.asm"); fs.Write("pixi/sprites/Sprite.asm", ";#inchbi \"missing.hitboxinfo\"");
        Assert.EndsWith("missing.hitboxinfo", Assert.Throws<FileNotFoundException>(() => PixiInputs.Discover("pixi", "list.txt")).FileName);
    }

    [Fact]
    public void Pixi_properties_require_normal_sprite_and_unique_name_mapping()
    {
        using var fs = new Sandbox();
        fs.Write("list.txt", "CLUSTER:\n01 Sprite.asm");
        fs.Write("pixi/cluster/Sprite.asm", ";#inchmprop \"props.json\""); fs.Write("pixi/cluster/props.json", "{}");
        Assert.Throws<InvalidDataException>(() => PixiInputs.Discover("pixi", "list.txt"));
        fs.Write("list.txt", "01 a/Same.asm\n02 b/Same.asm");
        foreach (string dir in new[] { "a", "b" })
        {
            fs.Write($"pixi/sprites/{dir}/Same.asm", ";#inchmprop \"props.json\""); fs.Write($"pixi/sprites/{dir}/props.json", "{}");
        }
        Assert.Throws<InvalidDataException>(() => PixiInputs.Discover("pixi", "list.txt"));
    }

    [Theory]
    [InlineData("Bad.json", "{\"Type\":1}")]
    [InlineData("Bad.cfg", "01\n00\n")]
    public void Pixi_requires_primary_asm_in_custom_config(string name, string contents)
    {
        using var fs = new Sandbox();
        fs.Write("list.txt", $"01 {name}"); fs.Write($"pixi/sprites/{name}", contents);
        Assert.Throws<InvalidDataException>(() => PixiInputs.Discover("pixi", "list.txt"));
    }

    [Fact]
    public void Dependencies_expand_transitively_once_despite_cycles()
    {
        using var fs = new Sandbox();
        fs.Write("routines/A.asm", "JSR C\nJSL B\nJSL A\n");
        fs.Write("routines/B.asm", "JSL C\n"); fs.Write("routines/C.asm", "JSL A\n");
        var list = new List<string> { "A" };
        RoutineDependencies.Expand(list, "routines", [], new HitmanLog());
        Assert.Equal(new[] { "A", "B", "C" }, list);
    }

    [Fact]
    public void Dependencies_ignore_local_labels_base_symbols_comments_strings_and_defines()
    {
        using var fs = new Sandbox();
        fs.Write("routines/A.asm", "Local:\nJSR Local\nJSL Base\n; JSL Ignored\nprint \"Hitman_Callback_Missing\"\nLDA !Hitman_Callback_Missing\nJSL $008000\n");
        foreach (string name in new[] { "Local", "Base", "Ignored" }) fs.Write($"routines/{name}.asm", "RTL");
        var list = new List<string> { "A" };
        RoutineDependencies.Expand(list, "routines", ["Base"], new HitmanLog());
        Assert.Equal(new[] { "A" }, list);
    }

    [Fact]
    public void Dependencies_resolve_explicit_names_and_report_missing_target_location()
    {
        using var fs = new Sandbox();
        fs.Write("routines/A.asm", "dl Hitman_Callback_B"); fs.Write("routines/B.asm", "RTL");
        var list = new List<string> { "A" };
        RoutineDependencies.Expand(list, "routines", [], new HitmanLog());
        Assert.Equal(new[] { "A", "B" }, list);
        fs.Write("routines/A.asm", "; comment\nJSL Hitman_Callback_Missing");
        var ex = Assert.Throws<InvalidDataException>(() => RoutineDependencies.Expand(["A"], "routines", [], new HitmanLog()));
        Assert.Contains("A.asm:2", ex.Message); Assert.Contains("Missing", ex.Message);
        Assert.Throws<FileNotFoundException>(() => RoutineDependencies.Expand(["Absent"], "routines", [], new HitmanLog()));
    }

    [Fact]
    public void Vanilla_profiles_fallback_deduplicate_and_replace_existing_id()
    {
        var a = Samples.Sprite(legacy: "Legacy", callbacks: new() { Fireball = "Fire", Quake = " " });
        var b = Samples.Sprite(legacy: "Legacy", callbacks: new() { Fireball = "Fire" });
        var empty = Samples.Sprite();
        a.Properties = [new() { PropertyName = "VanillaCallbackID", Value = "FF" }, new() { PropertyName = "Other", Value = "AB" }];
        var profiles = VanillaCallbacks.Prepare([a, b, empty]);
        Assert.Equal(new[] { "Legacy", "Fire", "Legacy", "Legacy", "Legacy", "Legacy" }, Assert.Single(profiles).Entries);
        Assert.Equal("01", Assert.Single(a.Properties, p => p.PropertyName == "VanillaCallbackID").Value);
        Assert.Equal("01", Assert.Single(b.Properties).Value);
        Assert.Equal("00", Assert.Single(empty.Properties).Value);
        Assert.Equal("AB", a.Properties.Single(p => p.PropertyName == "Other").Value);
        Assert.Equal(new[] { "Fire", "Legacy" }, VanillaCallbacks.GetNames(profiles));
    }

    [Fact]
    public void Vanilla_table_reserves_zero_and_six_pointers_in_dispatch_order()
    {
        var profile = new VanillaCallbackProfile("S", "F", null, "Q", "C", "E");
        Assert.Equal("HitmanVanillaCallbackTable:\n\tdl $000000,$000000,$000000,$000000,$000000,$000000\n\tdl Hitman_Callback_S,Hitman_Callback_F,$000000,Hitman_Callback_Q,Hitman_Callback_C,Hitman_Callback_E\n".Replace("\n", Environment.NewLine), VanillaCallbacks.BuildTable([profile]));
    }

    [Theory]
    [InlineData("bad-name")]
    [InlineData("0Bad")]
    [InlineData("A\nB")]
    public void Vanilla_rejects_invalid_callback_and_dependency_names(string name)
    {
        Assert.Throws<InvalidDataException>(() => VanillaCallbacks.Prepare([Samples.Sprite(legacy: name)]));
        var sprite = Samples.Sprite(); sprite.VanillaInteractionDependencies = [name];
        Assert.Throws<InvalidDataException>(() => VanillaCallbacks.Prepare([sprite]));
    }

    [Fact]
    public void Vanilla_accepts_255_profiles_and_rejects_the_256th()
    {
        var sprites = Enumerable.Range(0, 256).Select(i => Samples.Sprite(legacy: $"Routine{i}")).ToArray();
        Assert.Equal(255, VanillaCallbacks.Prepare(sprites[..255]).Length);
        Assert.Throws<InvalidDataException>(() => VanillaCallbacks.Prepare(sprites));
    }
}
