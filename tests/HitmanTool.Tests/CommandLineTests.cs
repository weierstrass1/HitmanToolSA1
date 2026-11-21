using System.Reflection;
using System.Runtime.ExceptionServices;
using HitmanBackend;
using HitmanBackend.DTO;
using System.Text.Json;

namespace HitmanTool.Tests;

public class CommandLineTests
{
    // Invokes the entry point while preserving its production visibility.
    private static object? Invoke(string method, params object?[] args)
    {
        var type = Assembly.Load("HitmanTool").GetType("Program", true)!;
        try { return type.GetMethod(method, BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, args); }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        { ExceptionDispatchInfo.Capture(ex.InnerException).Throw(); throw; }
    }

    private static void SettingsFiles(Sandbox fs)
    {
        fs.Write("input.sfc", "synthetic input"); fs.Write("output.sfc", "unchanged output");
        fs.Write("pixi/list.txt", "");
        fs.Write("Json/Settings.json", JsonSerializer.Serialize(new Settings
        { InputPath = "input.sfc", OutputPath = "output.sfc", PixiFolder = "pixi", PixiList = "pixi/list.txt" }));
    }

    private static void GenerationFiles(Sandbox fs)
    {
        SettingsFiles(fs);
        fs.Write("CustomProperties/Flag.json", "{\"Name\":\"Flag\",\"DefaultValueVanilla\":\"00\",\"DefaultValueCustom\":\"01\"}");
        fs.Write("HitboxInfo/Example.hitboxinfo", Samples.Sectioned);
        fs.Write("Patch/BaseRoutines/Base.asm", "RTL\n"); fs.Write("Routines/Nothing.asm", "RTL\n");
        fs.Write("ASM/HitmanDefines.asm", "!HitmanCustomProperties = $7FA000\n!MaxSprites = 12\n");
    }

    [Fact]
    public void Saved_settings_are_loaded_normalized_and_persisted()
    {
        using var fs = new Sandbox(); SettingsFiles(fs);
        object?[] args = [new[] { "-use-settings" }, null];
        Assert.Equal(true, Invoke("buildSettings", args));
        var settings = Assert.IsType<Settings>(args[1]);
        Assert.Equal("input.sfc", settings.InputPath);
        Assert.Equal("output.sfc", settings.OutputPath);
        var saved = JsonSerializer.Deserialize<Settings>(File.ReadAllText("Json/Settings.json"))!;
        Assert.Equal(settings.PixiList, saved.PixiList);
    }

    [Fact]
    public void Interactive_settings_accept_quoted_existing_paths()
    {
        using var fs = new Sandbox(); SettingsFiles(fs);
        var old = Console.In;
        try
        {
            Console.SetIn(new StringReader("\"input.sfc\"\n\"output.sfc\"\n\"pixi\"\n\"pixi/list.txt\"\n"));
            object?[] args = [Array.Empty<string>(), null];
            Assert.Equal(true, Invoke("buildSettings", args));
            Assert.Equal("input.sfc", Assert.IsType<Settings>(args[1]).InputPath);
        }
        finally { Console.SetIn(old); }
    }

    [Theory]
    [InlineData("\n")]
    [InlineData("missing.sfc\n")]
    [InlineData("input.sfc\n\n")]
    [InlineData("input.sfc\noutput.sfc\nmissing\n")]
    [InlineData("input.sfc\noutput.sfc\npixi\nmissing.txt\n")]
    public void Invalid_interactive_settings_stop_without_overwriting_configuration(string input)
    {
        using var fs = new Sandbox(); SettingsFiles(fs); var before = File.ReadAllText("Json/Settings.json");
        var old = Console.In;
        try { Console.SetIn(new StringReader(input)); Assert.Equal(false, Invoke("buildSettings", Array.Empty<string>(), null)); }
        finally { Console.SetIn(old); }
        Assert.Equal(before, File.ReadAllText("Json/Settings.json"));
    }

    [Fact]
    public void Generate_only_runs_pipeline_without_changing_rom_or_installing_pixi_files()
    {
        using var fs = new Sandbox(); GenerationFiles(fs);
        fs.Write("Patch/Routines/Obsolete.asm", "RTL\n");
        Assert.Equal(0, Invoke("Run", new[] { "-use-settings", "--generate-only" }, new HitmanLog()));
        Assert.Equal("synthetic input", File.ReadAllText("input.sfc"));
        Assert.Equal("unchanged output", File.ReadAllText("output.sfc"));
        Assert.False(File.Exists("test.smc")); Assert.False(Directory.Exists("pixi/routines"));
        Assert.False(File.Exists("Patch/Routines/Obsolete.asm"));
        Assert.Equal("RTL\n", File.ReadAllText("Patch/Routines/Nothing.asm"));
        Assert.Contains("ContextPosesHitboxes:", File.ReadAllText("Patch/Files/Data.asm"));
        Assert.Contains("!ExampleHitboxContext = $0000", File.ReadAllText("Patch/Files/HitmanDefines.asm"));
        foreach (string name in new[] { "Includes", "RoutinesTable", "CustomPropertiesTables", "CustomPropertiesSetup", "VanillaCallbacks", "ExtraDefinesIncludes" })
            Assert.True(File.Exists($"Patch/Files/{name}.asm"), name);
    }

    [Fact]
    public void Installer_patches_synthetic_rom_and_exports_routines_and_defines()
    {
        using var fs = new Sandbox(); GenerationFiles(fs);
        File.WriteAllBytes("input.sfc", AsarTests.Rom());
        fs.Write("Patch/Hitman.asm", "lorom\norg $008000\ndb $77\n");
        fs.Write("PixiRoutines/Test.asm", "RTL\n");
        Directory.CreateDirectory("pixi/routines"); Directory.CreateDirectory("pixi/asm/ExtraDefines");
        Assert.Equal(0, Invoke("Run", new[] { "-use-settings" }, new HitmanLog()));
        Assert.Equal(0, File.ReadAllBytes("input.sfc")[0]);
        Assert.Equal(0x77, File.ReadAllBytes("output.sfc")[0]);
        Assert.Equal("RTL\n", File.ReadAllText("pixi/routines/Test.asm"));
        Assert.Equal(File.ReadAllText("Patch/Files/HitmanDefines.asm"), File.ReadAllText("pixi/asm/ExtraDefines/HitmanDefines.asm"));
        Assert.False(File.Exists("test.smc"));
    }

    [Fact]
    public void Main_success_returns_zero_and_always_writes_log()
    {
        using var fs = new Sandbox(); GenerationFiles(fs);
        Assert.Equal(0, Invoke("Main", (object)new[] { "-use-settings", "--generate-only" }));
        Assert.Contains("generated", File.ReadAllText("log.txt"));
    }

    [Fact]
    public void Run_returns_failure_when_interactive_settings_are_invalid()
    {
        using var fs = new Sandbox(); SettingsFiles(fs); var old = Console.In;
        try { Console.SetIn(new StringReader("\n")); Assert.Equal(1, Invoke("Run", Array.Empty<string>(), new HitmanLog())); }
        finally { Console.SetIn(old); }
    }

    [Fact]
    public void Main_returns_failure_and_writes_log_when_settings_cannot_be_read()
    {
        using var fs = new Sandbox();
        Assert.Equal(1, Invoke("Main", (object)Array.Empty<string>()));
        Assert.Contains("Settings.json", File.ReadAllText("log.txt"));
    }
}
