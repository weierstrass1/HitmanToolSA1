using AsarCLR;
using HitmanBackend;

namespace HitmanTool.Tests;

public class AsarTests
{
    // Synthetic ROM fixture with no Nintendo data or changes to user ROMs.
    internal static byte[] Rom() => new byte[0x100000];

    [WindowsNativeFact]
    public void Native_adapter_initializes_queries_state_and_closes()
    {
        Assert.True(Asar.Init(), "Could not initialize asar.dll; check the architecture and native dependencies.");
        try
        {
            Assert.True(Asar.Version() >= 10000); Assert.True(Asar.ApiVersion() >= 303);
            Assert.True(Asar.MaxRomSize() >= 0x100000);
            Assert.True(Asar.Reset());
            Assert.Empty(Asar.GetErrors()); Assert.Empty(Asar.GetWarnings()); Assert.Empty(Asar.GetPrints());
            Assert.Empty(Asar.GetLabels()); Assert.Empty(Asar.GetWrittenBlocks());
            Assert.Equal(-1, Asar.GetLabelVal("Missing"));
            Assert.Equal(7d, Asar.Math("1+(2*3)", out var error)); Assert.True(string.IsNullOrEmpty(error));
            Asar.Math("(", out error); Assert.False(string.IsNullOrEmpty(error));
        }
        finally { Asar.Close(); }
    }

    [WindowsNativeFact]
    public void Native_patch_exports_labels_defines_symbols_prints_and_written_ranges()
    {
        using var fs = new Sandbox();
        Assert.True(Asar.Init());
        try
        {
            var patch = fs.Write("valid.asm", "lorom\n!Value = $2A\norg $008000\nMarker:\ndb !Value\nprint \"patched\"\n");
            var rom = Rom();
            Assert.True(Asar.Patch(patch, ref rom));
            Assert.Equal(0x2A, rom[0]); Assert.Equal(0x100000, rom.Length);
            Assert.Equal(MapperType.LoRom, Asar.GetMapper());
            Assert.Equal(0x8000, Asar.GetLabelVal("Marker"));
            Assert.Contains(Asar.GetLabels(), l => l.Name == "Marker" && l.Location == 0x8000);
            Assert.Equal("$2A", Asar.GetDefine("Value"));
            Assert.Contains(Asar.GetAllDefines(), d => d.Name == "Value" && d.Contents == "$2A");
            Assert.Equal("db $2A", Asar.ResolveDefines("db !Value", false));
            Assert.Equal(new[] { "patched" }, Asar.GetPrints());
            Assert.Contains("Marker", Asar.GetSymbolsFile()!);
            var blocks = Asar.GetWrittenBlocks();
            Assert.True(blocks.Any(b => b.Pcoffset == 0 && (b.Snesoffset & 0x7FFFFF) == 0x8000 && b.Numbytes >= 1),
                string.Join("; ", blocks.Select(b => $"PC={b.Pcoffset:X} SNES={b.Snesoffset:X} bytes={b.Numbytes}")));
            Assert.Empty(Asar.GetErrors()); Assert.Empty(Asar.GetWarnings());
        }
        finally { Asar.Close(); }
    }

    [WindowsNativeFact]
    public void Native_patch_reports_errors_and_reset_clears_them()
    {
        using var fs = new Sandbox(); Assert.True(Asar.Init());
        try
        {
            var rom = Rom();
            Assert.False(Asar.Patch(fs.Write("invalid.asm", "lorom\norg $008000\nerror \"expected failure\"\n"), ref rom));
            var errors = Asar.GetErrors(); Assert.NotEmpty(errors);
            Assert.Contains(errors, e => e.Fullerrdata.Contains("expected failure"));
            Assert.True(Asar.Reset()); Assert.Empty(Asar.GetErrors());
            Assert.Contains(errors, e => e.Fullerrdata.Contains("expected failure"));
        }
        finally { Asar.Close(); }
    }

    [Fact]
    public void File_patch_overloads_modify_destination_and_preserve_source_copy()
    {
        using var fs = new Sandbox();
        var patch = fs.Write("patch with spaces.asm", "lorom\norg $008000\ndb $42\nprint \"done\"\n");
        File.WriteAllBytes("input.sfc", Rom());
        Assert.Contains("done", PatchApplier.Apply("input.sfc", patch, "output with spaces.sfc"));
        Assert.Equal(0, File.ReadAllBytes("input.sfc")[0]);
        Assert.Equal(0x42, File.ReadAllBytes("output with spaces.sfc")[0]);
        Assert.Contains("done", PatchApplier.Apply("input.sfc", patch));
        Assert.Equal(0x42, File.ReadAllBytes("input.sfc")[0]);
    }

    [Fact]
    public void Buffer_patch_copies_bytes_back()
    {
        using var fs = new Sandbox(); var rom = Rom();
        var patch = fs.Write("buffer.asm", "lorom\norg $008000\ndb $23\n");
        PatchApplier.Apply(rom, patch);
        Assert.True(rom[0] == 0x23, $"Expected $23 at offset 0; found it at offset {Array.IndexOf(rom, (byte)0x23)}.");
    }

    [Fact]
    public void Buffer_patch_rejects_expansion_without_replacing_original_array()
    {
        using var fs = new Sandbox(); var rom = Rom(); var before = rom.ToArray();
        var patch = fs.Write("expand.asm", "lorom\norg $208000\ndb $55\n");
        Assert.Contains("expanded", Assert.Throws<InvalidOperationException>(() => PatchApplier.Apply(rom, patch)).Message);
        Assert.Equal(before, rom);
    }

    [Fact]
    public void File_patch_propagates_asar_diagnostics()
    {
        using var fs = new Sandbox(); File.WriteAllBytes("rom.sfc", Rom());
        var patch = fs.Write("bad.asm", "error \"intentional failure\"\n");
        var ex = Assert.Throws<InvalidOperationException>(() => PatchApplier.Apply("rom.sfc", patch));
        Assert.Contains("intentional failure", ex.Message); Assert.Contains("exit code", ex.Message);
    }

    [WindowsNativeFact]
    public void Native_patch_accepts_include_paths_and_additional_defines()
    {
        using var fs = new Sandbox(); Assert.True(Asar.Init());
        try
        {
            var include = fs.Write("includes/value.asm", "db !FromHost\n");
            var patch = fs.Write("main.asm", "lorom\norg $008000\nincsrc \"value.asm\"\n");
            var rom = Rom();
            Assert.True(Asar.Patch(patch, ref rom, [Path.GetDirectoryName(include)!],
                additionalDefines: new() { ["FromHost"] = "$6B" }));
            Assert.Equal(0x6B, rom[0]);
        }
        finally { Asar.Close(); }
    }

    [Fact]
    public void Generated_hitbox_tables_assemble_to_expected_words()
    {
        using var fs = new Sandbox(); var box = Samples.Box();
        var data = HitboxTables.Build([Samples.Info("A", [box], [0], [])], [box], ["Nothing"]);
        var patch = fs.Write("tables.asm", "lorom\norg $008000\n" + data + "Hitman_Callback_Nothing:\nRTL\n");
        File.WriteAllBytes("tables.sfc", Rom());
        PatchApplier.Apply("tables.sfc", patch);
        byte[] rom = File.ReadAllBytes("tables.sfc");
        // Context offsets [0,2], sequence starts [0,4], first geometry offset 0, terminator FFFF.
        Assert.Equal(new byte[] { 0, 0, 2, 0, 0, 0, 4, 0, 0, 0, 255, 255, 255, 255 }, rom[..14]);
    }

    [Fact]
    public void Executable_environment_override_is_used_and_restored()
    {
        using var fs = new Sandbox();
        string? previous = Environment.GetEnvironmentVariable("HITMAN_ASAR_EXE");
        try
        {
            Environment.SetEnvironmentVariable("HITMAN_ASAR_EXE", Path.Combine(fs.Root, "missing-asar.exe"));
            var patch = fs.Write("valid.asm", "lorom\norg $008000\ndb $01\n");
            File.WriteAllBytes("rom.sfc", Rom());
            Assert.Throws<System.ComponentModel.Win32Exception>(() => PatchApplier.Apply("rom.sfc", patch));
        }
        finally { Environment.SetEnvironmentVariable("HITMAN_ASAR_EXE", previous); }
    }
}
