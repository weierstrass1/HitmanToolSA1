using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Text.Json;
using HitmanBackend;
using HitmanBackend.DTO;

namespace HitmanTool.Tests;

[Trait("Category", "Uberasm")]
public class UberasmLinkTests
{
    [Theory]
    [InlineData("level", "level")]
    [InlineData("overworld", "overworld")]
    [InlineData("game_mode", "gamemode")]
    [InlineData("Game Mode", "gamemode")]
    [InlineData("global", "global")]
    [InlineData("statusbar", "statusbar")]
    [InlineData("macrolibrary", "library")]
    [InlineData("macrolib", "library")]
    public void Section_entries_resolve_sources_and_hitboxes_relative_to_their_owners(string section, string directory)
    {
        using var fs = new Sandbox();
        fs.Write("uber/lists/list.txt", $"{section}:\n105 \"Nested Folder/Zone.asm\"\n");
        fs.Write($"uber/{directory}/Nested Folder/Zone.asm", ";#inchbi \"../Data/Zone.hitboxinfo\"\n");
        string expected = fs.Write($"uber/{directory}/Data/Zone.hitboxinfo", Samples.Sectioned);
        Assert.Equal(new[] { expected }, UberasmInputs.DiscoverHitboxes("uber", "lists/list.txt"));
    }

    [Theory]
    [InlineData("global")]
    [InlineData("statusbar")]
    [InlineData("macrolib")]
    [InlineData("MACROLIBRARY")]
    public void Inline_entries_resolve_from_tool_root_even_after_a_section(string kind)
    {
        using var fs = new Sandbox();
        fs.Write("uber/list.txt", $"level:\n{kind}: \"Other Folder/Root.asm\" ; comment\n");
        fs.Write("uber/Other Folder/Root.asm", ";#inchbi \"Root.hitboxinfo\"");
        string expected = fs.Write("uber/Other Folder/Root.hitboxinfo", Samples.Legacy);
        Assert.Equal(new[] { expected }, UberasmInputs.DiscoverHitboxes("uber", "list.txt"));
    }

    [Fact]
    public void Multiple_entries_parameters_comments_and_repeated_sources_are_supported()
    {
        using var fs = new Sandbox();
        fs.Write("uber/list.txt", "level:\n105 A.asm:main, \"B; With Spaces.asm\":init ; Missing.asm\n106 A.asm\nunknown:\n107 Missing.asm\n");
        fs.Write("uber/level/A.asm", ";#inchbi \"A.hitboxinfo\"");
        fs.Write("uber/level/B; With Spaces.asm", ";#inchbi \"B.hitboxinfo\"");
        string a = fs.Write("uber/level/A.hitboxinfo", Samples.Sectioned);
        string b = fs.Write("uber/level/B.hitboxinfo", Samples.Legacy);
        Assert.Equal(new[] { a, b }, UberasmInputs.DiscoverHitboxes("uber", "list.txt", new HitmanLog()));
    }

    [Fact]
    public void Windows_separators_are_normalized_in_list_and_directives()
    {
        using var fs = new Sandbox();
        fs.Write("uber/config/list.txt", "level:\n105 Nested\\Zone.asm\n");
        fs.Write("uber/level/Nested/Zone.asm", ";#inchbi \"..\\Data\\Zone.hitboxinfo\"");
        string expected = fs.Write("uber/level/Data/Zone.hitboxinfo", Samples.Sectioned);
        Assert.Equal(new[] { expected }, UberasmInputs.DiscoverHitboxes("uber", "config\\list.txt"));
    }

    [Fact]
    public void Empty_list_disables_discovery_without_accessing_the_folder()
    {
        using var fs = new Sandbox();
        Assert.Empty(UberasmInputs.DiscoverHitboxes("absent", ""));
    }

    [Fact]
    public void Missing_source_reports_list_line_and_full_path()
    {
        using var fs = new Sandbox(); fs.Write("uber/list.txt", "; comment\nlevel:\n105 Missing.asm\n");
        var ex = Assert.Throws<FileNotFoundException>(() => UberasmInputs.DiscoverHitboxes("uber", "list.txt").ToArray());
        Assert.Contains("list.txt:3", ex.Message);
        Assert.Equal(Path.GetFullPath("uber/level/Missing.asm"), ex.FileName);
    }

    [Theory]
    [InlineData(";#inchbi Missing.hitboxinfo", "Invalid Hitman directive")]
    [InlineData(";#inchbi \"Wrong.json\"", ".hitboxinfo")]
    [InlineData(";#inchmprop \"Props.json\"", "normal sprite entry in PIXI")]
    public void Invalid_or_sprite_only_directives_report_source_and_line(string directive, string reason)
    {
        using var fs = new Sandbox(); fs.Write("uber/list.txt", "level:\n105 Zone.asm\n");
        fs.Write("uber/level/Zone.asm", "; header\n" + directive);
        var ex = Assert.Throws<InvalidDataException>(() => UberasmInputs.DiscoverHitboxes("uber", "list.txt").ToArray());
        Assert.Contains("Zone.asm", ex.Message); Assert.Contains("line 2", ex.Message); Assert.Contains(reason, ex.Message);
    }

    [Fact]
    public void Missing_hitbox_reports_the_declaring_source()
    {
        using var fs = new Sandbox(); fs.Write("uber/list.txt", "level:\n105 Zone.asm");
        fs.Write("uber/level/Zone.asm", ";#inchbi \"Missing.hitboxinfo\"");
        var ex = Assert.Throws<FileNotFoundException>(() => UberasmInputs.DiscoverHitboxes("uber", "list.txt").ToArray());
        Assert.Contains("Zone.asm", ex.Message); Assert.Contains("line 1", ex.Message);
        Assert.Equal(Path.GetFullPath("uber/level/Missing.hitboxinfo"), ex.FileName);
    }

    [Fact]
    public void Missing_list_is_not_silently_ignored()
    {
        using var fs = new Sandbox(); Directory.CreateDirectory("uber");
        Assert.Throws<FileNotFoundException>(() => UberasmInputs.DiscoverHitboxes("uber", "missing.txt").ToArray());
        Assert.Throws<FileNotFoundException>(() => UberasmInputs.ResolveMacroLibrary("uber", "missing.txt"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("; macrolib: ignored.asm\nlevel:\n105 Zone.asm\n")]
    public void Macro_library_defaults_to_other_directory_when_not_configured(string listContent)
    {
        using var fs = new Sandbox(); fs.Write("uber/list.txt", listContent);
        Assert.Equal(Path.GetFullPath("uber/other/macro_library.asm"), UberasmInputs.ResolveMacroLibrary("uber", "list.txt"));
        Assert.Equal(Path.GetFullPath("uber/other/macro_library.asm"), UberasmInputs.ResolveMacroLibrary("uber", ""));
    }

    [Theory]
    [InlineData("macrolib")]
    [InlineData("MacroLibrary")]
    public void Configured_macro_library_uses_root_relative_quoted_path(string kind)
    {
        using var fs = new Sandbox(); fs.Write("uber/config/list.txt", $"{kind}: \"Custom Folder\\Macros.asm\" ; comment\n");
        Assert.Equal(Path.GetFullPath("uber/Custom Folder/Macros.asm"), UberasmInputs.ResolveMacroLibrary("uber", "config/list.txt"));
    }

    [Fact]
    public void Invalid_macro_library_path_is_rejected()
    {
        using var fs = new Sandbox(); fs.Write("uber/list.txt", "macrolib: wrong.txt");
        Assert.Contains("macro library", Assert.Throws<InvalidDataException>(() => UberasmInputs.ResolveMacroLibrary("uber", "list.txt")).Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("; Existing library")]
    [InlineData("; Existing library\n")]
    [InlineData("; Existing library\r\n")]
    public void Publication_preserves_library_content_and_newline_style(string content)
    {
        using var fs = new Sandbox();
        string source = fs.Write("generated/HitmanDefines.asm", "!ZoneHitboxContext = $0000\n");
        string library = fs.Write("uber/other/macro_library.asm", content);
        ExtraDefines.PublishToUberasm(source, library);
        string newline = content.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        string separator = content.Length == 0 || content.EndsWith('\n') ? "" : newline;
        Assert.Equal(content + separator + "incsrc \"HitmanDefines.asm\"" + newline, File.ReadAllText(library));
        Assert.Equal(File.ReadAllBytes(source), File.ReadAllBytes("uber/other/HitmanDefines.asm"));
    }

    [Theory]
    [InlineData("incsrc \"HitmanDefines.asm\"\n")]
    [InlineData("  INCSRC \"./HitmanDefines.asm\" ; installed\r\n")]
    public void Reinstallation_refreshes_defines_without_modifying_existing_include(string include)
    {
        using var fs = new Sandbox();
        string source = fs.Write("generated.asm", "!Value = $01\n");
        string library = fs.Write("uber/custom/Macros.asm", "; Dynamic Xtreme\n" + include);
        byte[] before = File.ReadAllBytes(library);
        ExtraDefines.PublishToUberasm(source, library);
        File.WriteAllText(source, "!Value = $02\n");
        ExtraDefines.PublishToUberasm(source, library);
        Assert.Equal(before, File.ReadAllBytes(library));
        Assert.Equal("!Value = $02\n", File.ReadAllText("uber/custom/HitmanDefines.asm"));
    }

    [Fact]
    public void Commented_include_does_not_prevent_an_active_link()
    {
        using var fs = new Sandbox();
        string source = fs.Write("generated.asm", "!Value = $01\n");
        string library = fs.Write("uber/other/macro_library.asm", "; incsrc \"HitmanDefines.asm\"\nincsrc \"DynamicXtreme.asm\"\n");
        ExtraDefines.PublishToUberasm(source, library);
        Assert.Equal("; incsrc \"HitmanDefines.asm\"\nincsrc \"DynamicXtreme.asm\"\nincsrc \"HitmanDefines.asm\"\n", File.ReadAllText(library));
    }

    [Fact]
    public void Missing_generated_defines_does_not_change_library_or_old_published_copy()
    {
        using var fs = new Sandbox();
        string library = fs.Write("uber/other/macro_library.asm", "; preserved\n");
        fs.Write("uber/other/HitmanDefines.asm", "old defines");
        Assert.Throws<FileNotFoundException>(() => ExtraDefines.PublishToUberasm("missing.asm", library));
        Assert.Equal("; preserved\n", File.ReadAllText(library));
        Assert.Equal("old defines", File.ReadAllText("uber/other/HitmanDefines.asm"));
    }

    private static object? Run(bool generateOnly)
    {
        var program = Assembly.Load("HitmanTool").GetType("Program", true)!;
        string[] args = generateOnly ? ["-use-settings", "--generate-only"] : ["-use-settings"];
        try { return program.GetMethod("Run", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [args, new HitmanLog()]); }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        { ExceptionDispatchInfo.Capture(ex.InnerException).Throw(); throw; }
    }

    private static Settings Prepare(Sandbox fs, string list = "list.txt")
    {
        fs.Write("input.sfc", ""); File.WriteAllBytes("input.sfc", new byte[0x100000]);
        fs.Write("output.sfc", "existing output"); fs.Write("pixi/list.txt", "");
        fs.Write("CustomProperties/Flag.json", "{\"Name\":\"Flag\",\"DefaultValueVanilla\":\"00\",\"DefaultValueCustom\":\"01\"}");
        fs.Write("Patch/BaseRoutines/Base.asm", "RTL\n"); fs.Write("Routines/Nothing.asm", "RTL\n");
        fs.Write("ASM/HitmanDefines.asm", "!HitmanCustomProperties = $7FA000\n!MaxSprites = 12\n");
        fs.Write("PixiRoutines/Test.asm", "RTL\n");
        fs.Write("Patch/Hitman.asm", "lorom\norg $07F7D6\ndl $008100\norg $008100\ndl $008200\norg $008200\nRTL\n");
        fs.Write("uber/other/macro_library.asm", "; Existing library\n");
        fs.Write("uber/list.txt", "level:\n105 Zone.asm\n");
        fs.Write("uber/level/Zone.asm", ";#inchbi \"Zone.hitboxinfo\"\n");
        fs.Write("uber/level/Zone.hitboxinfo", Samples.Sectioned);
        var settings = new Settings { InputPath = "input.sfc", OutputPath = "output.sfc", PixiFolder = "pixi", PixiList = "pixi/list.txt",
            UberasmFolder = "uber", UberasmList = list };
        Save(fs, settings);
        return settings;
    }

    private static void Save(Sandbox fs, Settings settings) => fs.Write("Json/Settings.json", JsonSerializer.Serialize(settings));

    [Fact]
    public void Generation_reads_shared_pixi_and_uber_resource_once_without_publishing_external_files()
    {
        using var fs = new Sandbox(); Prepare(fs);
        fs.Write("pixi/list.txt", "01 Shared.asm\n");
        fs.Write("pixi/sprites/Shared.asm", ";#inchbi \"../../uber/level/Zone.hitboxinfo\"\n");
        fs.Write("HitboxInfo/Zone.hitboxinfo", "invalid central resource overridden by link");
        byte[] library = File.ReadAllBytes("uber/other/macro_library.asm");
        Assert.Equal(0, Run(true));
        string defines = File.ReadAllText("Patch/Files/HitmanDefines.asm");
        Assert.Equal(1, defines.Split("!ZoneHitboxContext =").Length - 1);
        Assert.Equal(new[] { 0 }, Samples.Words(File.ReadAllText("Patch/Files/Data.asm"), "ContextPosesHitboxes"));
        Assert.Equal(library, File.ReadAllBytes("uber/other/macro_library.asm"));
        Assert.False(File.Exists("uber/other/HitmanDefines.asm"));
        Assert.Equal("existing output", File.ReadAllText("output.sfc"));
    }

    [Fact]
    public void Different_linked_files_with_same_context_name_report_both_paths()
    {
        using var fs = new Sandbox(); Prepare(fs);
        fs.Write("pixi/list.txt", "01 Shared.asm\n");
        fs.Write("pixi/sprites/Shared.asm", ";#inchbi \"Zone.hitboxinfo\"\n");
        string pixi = fs.Write("pixi/sprites/Zone.hitboxinfo", Samples.Sectioned);
        var ex = Assert.Throws<InvalidDataException>(() => Run(true));
        Assert.Contains(pixi, ex.Message); Assert.Contains(Path.GetFullPath("uber/level/Zone.hitboxinfo"), ex.Message);
    }

    [Fact]
    public void Installed_link_can_assemble_exported_context_and_routine_against_patched_rom()
    {
        using var fs = new Sandbox(); Prepare(fs);
        Assert.Equal(0, Run(false));
        Assert.Equal(File.ReadAllBytes("pixi/asm/ExtraDefines/HitmanDefines.asm"), File.ReadAllBytes("uber/other/HitmanDefines.asm"));
        string library = File.ReadAllText("uber/other/macro_library.asm");
        Assert.Contains("; Existing library", library);
        var consumer = fs.Write("consumer.asm", "lorom\nincsrc \"uber/other/macro_library.asm\"\norg $008300\ndw !ZoneHitboxContext\nJSL !Hitman_Base_Base\n");
        PatchApplier.Apply("output.sfc", consumer, "consumer.sfc");
        Assert.Equal(new byte[] { 0, 0, 0x22, 0, 0x82, 0 }, File.ReadAllBytes("consumer.sfc")[0x300..0x306]);
        Assert.Equal(0, Run(false));
        Assert.Equal(library, File.ReadAllText("uber/other/macro_library.asm"));
    }

    [Fact]
    public void Folder_only_configuration_publishes_defines_without_discovering_inputs()
    {
        using var fs = new Sandbox(); Prepare(fs, "");
        fs.Write("uber/list.txt", "level:\n105 Missing.asm\n");
        Assert.Equal(0, Run(false));
        Assert.True(File.Exists("uber/other/HitmanDefines.asm"));
        Assert.DoesNotContain("!ZoneHitboxContext", File.ReadAllText("uber/other/HitmanDefines.asm"));
    }

    [Fact]
    public void Configured_macro_library_receives_defines_in_its_own_directory()
    {
        using var fs = new Sandbox(); Prepare(fs);
        fs.Write("uber/list.txt", "macrolib: \"Custom Folder/Library.asm\"\nlevel:\n105 Zone.asm\n");
        fs.Write("uber/Custom Folder/Library.asm", "; Custom library\n");
        Assert.Equal(0, Run(false));
        Assert.True(File.Exists("uber/Custom Folder/HitmanDefines.asm"));
        Assert.False(File.Exists("uber/other/HitmanDefines.asm"));
        Assert.Equal("; Existing library\n", File.ReadAllText("uber/other/macro_library.asm"));
    }

    [Fact]
    public void Missing_library_is_rejected_before_patching_or_overwriting_output()
    {
        using var fs = new Sandbox(); Prepare(fs); File.Delete("uber/other/macro_library.asm");
        var ex = Assert.Throws<FileNotFoundException>(() => Run(false));
        Assert.Contains("macro library", ex.Message);
        Assert.Equal("existing output", File.ReadAllText("output.sfc"));
        Assert.False(File.Exists("Patch/Files/Data.asm"));
    }

    [Fact]
    public void Generate_only_does_not_require_an_external_macro_library()
    {
        using var fs = new Sandbox(); Prepare(fs); File.Delete("uber/other/macro_library.asm");
        Assert.Equal(0, Run(true));
        Assert.Contains("!ZoneHitboxContext", File.ReadAllText("Patch/Files/HitmanDefines.asm"));
        Assert.False(File.Exists("uber/other/HitmanDefines.asm"));
    }

    [Fact]
    public void List_without_tool_folder_is_rejected()
    {
        using var fs = new Sandbox(); var settings = Prepare(fs); settings.UberasmFolder = ""; Save(fs, settings);
        Assert.Contains("UberasmFolder", Assert.Throws<InvalidDataException>(() => Run(true)).Message);
    }

    [Fact]
    public void Missing_tool_folder_is_rejected_even_in_generation_mode()
    {
        using var fs = new Sandbox(); var settings = Prepare(fs); settings.UberasmFolder = "missing"; Save(fs, settings);
        Assert.Throws<DirectoryNotFoundException>(() => Run(true));
    }

    [Fact]
    public void Failed_assembly_does_not_publish_uber_defines_or_modify_library()
    {
        using var fs = new Sandbox(); Prepare(fs);
        fs.Write("Patch/Hitman.asm", "error \"expected assembly failure\"\n");
        fs.Write("uber/other/HitmanDefines.asm", "previous defines");
        byte[] library = File.ReadAllBytes("uber/other/macro_library.asm");
        Assert.Throws<InvalidOperationException>(() => Run(false));
        Assert.Equal(library, File.ReadAllBytes("uber/other/macro_library.asm"));
        Assert.Equal("previous defines", File.ReadAllText("uber/other/HitmanDefines.asm"));
        Assert.Equal("existing output", File.ReadAllText("output.sfc"));
    }
}
