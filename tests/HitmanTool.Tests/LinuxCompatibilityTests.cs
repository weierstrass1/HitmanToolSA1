using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.Versioning;
using System.Text.Json;
using HitmanBackend;
using HitmanBackend.DTO;

namespace HitmanTool.Tests;

[Trait("Category", "Linux")]
[SupportedOSPlatform("linux")]
public class LinuxCompatibilityTests
{
    private static object? InvokeProgram(string method, params object?[] args)
    {
        var program = Assembly.Load("HitmanTool").GetType("Program", true)!;
        try { return program.GetMethod(method, BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, args); }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        { ExceptionDispatchInfo.Capture(ex.InnerException).Throw(); throw; }
    }

    [LinuxFact]
    public void Bundled_linux_asar_is_an_elf_executable_and_patches_a_synthetic_rom()
    {
        using var fs = new Sandbox();
        string executable = Path.Combine(AppContext.BaseDirectory, "Asar", "asar");
        Assert.True(File.Exists(executable), "The Linux Asar executable must be copied to the test output.");
        Assert.Equal(new byte[] { 0x7F, (byte)'E', (byte)'L', (byte)'F' }, File.ReadAllBytes(executable)[..4]);
        var old = Environment.GetEnvironmentVariable("HITMAN_ASAR_EXE");
        try
        {
            Environment.SetEnvironmentVariable("HITMAN_ASAR_EXE", null);
            var patch = fs.Write("patch with spaces.asm", "lorom\norg $008000\ndb $5A\nprint \"linux-asar-ok\"\n");
            File.WriteAllBytes("input with spaces.sfc", AsarTests.Rom());
            string output = PatchApplier.Apply("input with spaces.sfc", patch, "output with spaces.sfc");
            Assert.Contains("linux-asar-ok", output);
            Assert.Equal(0x5A, File.ReadAllBytes("output with spaces.sfc")[0]);
            Assert.Equal(0, File.ReadAllBytes("input with spaces.sfc")[0]);
        }
        finally { Environment.SetEnvironmentVariable("HITMAN_ASAR_EXE", old); }
    }

    [LinuxFact]
    public void Bundled_asar_gains_user_execute_permission_without_losing_other_mode_bits()
    {
        using var fs = new Sandbox();
        string executable = Path.Combine(AppContext.BaseDirectory, "Asar", "asar");
        var oldMode = File.GetUnixFileMode(executable);
        var oldOverride = Environment.GetEnvironmentVariable("HITMAN_ASAR_EXE");
        var initial = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead | UnixFileMode.OtherRead;
        try
        {
            Environment.SetEnvironmentVariable("HITMAN_ASAR_EXE", null);
            File.SetUnixFileMode(executable, initial);
            Assert.Equal(initial, File.GetUnixFileMode(executable));
            File.WriteAllBytes("rom.sfc", AsarTests.Rom());
            var patch = fs.Write("permission.asm", "lorom\norg $008000\ndb $6B\n");
            PatchApplier.Apply("rom.sfc", patch);
            Assert.Equal(initial | UnixFileMode.UserExecute, File.GetUnixFileMode(executable));
            Assert.Equal(0x6B, File.ReadAllBytes("rom.sfc")[0]);
        }
        finally
        {
            File.SetUnixFileMode(executable, oldMode);
            Environment.SetEnvironmentVariable("HITMAN_ASAR_EXE", oldOverride);
        }
    }

    [LinuxFact]
    public void Environment_override_passes_separate_arguments_and_captures_stdout_and_stderr()
    {
        using var fs = new Sandbox();
        string runner = fs.Write("tools with spaces/asar fixture", "#!/bin/sh\nset -eu\n[ \"$#\" -eq 3 ]\n[ \"$1\" = '--no-title-check' ]\n[ \"$2\" = \"$EXPECTED_PATCH\" ]\n[ -f \"$3\" ]\nprintf 'fixture stdout\\n'\nprintf 'fixture stderr\\n' >&2\n");
        File.SetUnixFileMode(runner, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        string patch = fs.Write("patch with spaces.asm", "");
        var old = Environment.GetEnvironmentVariable("HITMAN_ASAR_EXE");
        var oldPatch = Environment.GetEnvironmentVariable("EXPECTED_PATCH");
        try
        {
            Environment.SetEnvironmentVariable("HITMAN_ASAR_EXE", runner);
            Environment.SetEnvironmentVariable("EXPECTED_PATCH", patch);
            var original = AsarTests.Rom(); File.WriteAllBytes("input.sfc", original);
            string output = PatchApplier.Apply("input.sfc", patch, "output with spaces.sfc");
            Assert.Contains("fixture stdout", output); Assert.Contains("fixture stderr", output);
            Assert.Equal(original, File.ReadAllBytes("output with spaces.sfc"));
        }
        finally
        {
            Environment.SetEnvironmentVariable("HITMAN_ASAR_EXE", old);
            Environment.SetEnvironmentVariable("EXPECTED_PATCH", oldPatch);
        }
    }

    [LinuxFact]
    public void Failed_linux_process_keeps_existing_destination_and_reports_exit_code()
    {
        using var fs = new Sandbox();
        string runner = fs.Write("failing asar", "#!/bin/sh\nprintf 'linux failure\\n' >&2\nexit 17\n");
        File.SetUnixFileMode(runner, UnixFileMode.UserRead | UnixFileMode.UserExecute);
        var old = Environment.GetEnvironmentVariable("HITMAN_ASAR_EXE");
        try
        {
            Environment.SetEnvironmentVariable("HITMAN_ASAR_EXE", runner);
            File.WriteAllBytes("input.sfc", AsarTests.Rom());
            File.WriteAllBytes("output.sfc", [1, 2, 3]);
            var ex = Assert.Throws<InvalidOperationException>(() => PatchApplier.Apply("input.sfc", "unused.asm", "output.sfc"));
            Assert.Contains("exit code 17", ex.Message); Assert.Contains("linux failure", ex.Message);
            Assert.Equal(new byte[] { 1, 2, 3 }, File.ReadAllBytes("output.sfc"));
        }
        finally { Environment.SetEnvironmentVariable("HITMAN_ASAR_EXE", old); }
    }

    [LinuxFact]
    public void Pixi_resolves_windows_separators_across_descriptor_asm_and_directives()
    {
        using var fs = new Sandbox();
        fs.Write("list.txt", "01 \"Folder With Spaces\\Sprite.json\"\n");
        fs.Write("pixi/sprites/Folder With Spaces/Sprite.json", JsonSerializer.Serialize(new { Type = 1, AsmFile = "Code\\Main.asm" }));
        fs.Write("pixi/sprites/Folder With Spaces/Code/Main.asm", ";#inchbi \"..\\Data\\Boxes.hitboxinfo\"\n;#inchmprop \"..\\Data\\Props.json\"\n");
        string box = fs.Write("pixi/sprites/Folder With Spaces/Data/Boxes.hitboxinfo", Samples.Sectioned);
        string props = fs.Write("pixi/sprites/Folder With Spaces/Data/Props.json", "{}");
        var result = PixiInputs.Discover("pixi", "list.txt");
        Assert.Equal(new[] { box }, result.Hitboxes);
        Assert.Equal(props, result.Properties["Sprite"]);
    }

    [LinuxFact]
    public void Pixi_keeps_different_case_paths_distinct_on_a_case_sensitive_filesystem()
    {
        using var fs = new Sandbox();
        string upper = fs.Write("pixi/sprites/Boxes.hitboxinfo", Samples.Sectioned);
        string lower = fs.Write("pixi/sprites/boxes.hitboxinfo", Samples.Legacy);
        Assert.NotEqual(File.ReadAllText(upper), File.ReadAllText(lower));
        fs.Write("pixi/sprites/Main.asm", ";#inchbi \"Boxes.hitboxinfo\"\n;#inchbi \"boxes.hitboxinfo\"\n");
        fs.Write("list.txt", "01 Main.asm\n02 Main.asm\n");
        Assert.Equal(new[] { upper, lower }, PixiInputs.Discover("pixi", "list.txt").Hitboxes);
    }

    [LinuxFact]
    public void External_defines_use_forward_slashes_with_spaces_in_linux_paths()
    {
        using var fs = new Sandbox();
        fs.Write("External Defines/Value.asm", "");
        Assert.Equal("incsrc \"../../External Defines/Value.asm\"", ExtraDefines.BuildIncsrcFile("External Defines"));
    }

    [LinuxTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Settings_normalize_quoted_windows_paths_before_validation(bool interactive)
    {
        using var fs = new Sandbox();
        fs.Write("Data Files/input.sfc", "input"); fs.Write("Data Files/output.sfc", "output"); fs.Write("Pixi Folder/list.txt", "");
        var settings = new Settings { InputPath = " \"Data Files\\input.sfc\" ", OutputPath = " \"Data Files\\output.sfc\" ",
            PixiFolder = " \"Pixi Folder\" ", PixiList = " \"Pixi Folder\\list.txt\" " };
        fs.Write("Json/Settings.json", JsonSerializer.Serialize(settings));
        var old = Console.In;
        try
        {
            Console.SetIn(new StringReader(interactive
                ? string.Join('\n', settings.InputPath, settings.OutputPath, settings.PixiFolder, settings.PixiList) + "\n" : ""));
            object?[] args = [interactive ? Array.Empty<string>() : new[] { "-use-settings" }, null];
            Assert.Equal(true, InvokeProgram("buildSettings", args));
            var actual = Assert.IsType<Settings>(args[1]);
            Assert.Equal("Data Files/input.sfc", actual.InputPath); Assert.Equal("Data Files/output.sfc", actual.OutputPath);
            Assert.Equal("Pixi Folder", actual.PixiFolder); Assert.Equal("Pixi Folder/list.txt", actual.PixiList);
            var saved = JsonSerializer.Deserialize<Settings>(File.ReadAllText("Json/Settings.json"))!;
            Assert.Equal(actual.PixiList, saved.PixiList);
        }
        finally { Console.SetIn(old); }
    }
}
