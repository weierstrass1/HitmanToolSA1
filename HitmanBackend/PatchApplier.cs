using System.Diagnostics;

namespace HitmanBackend;

/// <summary>Runs Asar against ROM files and returns its output.</summary>
public class PatchApplier
{
    /// <summary>Patches the specified ROM file in place and returns Asar output.</summary>
    public static string Apply(string rompath, string patchPath) =>
        Apply(rompath, patchPath, rompath);

    /// <summary>Patches a temporary copy and publishes it to the destination after Asar succeeds.</summary>
    public static string Apply(string rompath, string patchPath, string outputPath)
    {
        long length = new FileInfo(rompath).Length;
        var directory = Directory.CreateTempSubdirectory("hitman-asar-");
        string stagedRom = GetStagedRomPath(directory, length);
        try
        {
            File.Copy(rompath, stagedRom);
            string result = RunAsar(stagedRom, patchPath);
            File.Copy(stagedRom, outputPath, overwrite: true);
            return result;
        }
        finally
        {
            File.Delete(stagedRom);
            directory.Delete();
        }
    }

    /// <summary>Chooses the extension Asar expects for the ROM's actual copier header layout.</summary>
    private static string GetStagedRomPath(DirectoryInfo directory, long length)
    {
        const int bankSize = 0x8000;
        const int copierHeaderSize = 0x200;
        // Asar treats .smc files as having a copier header.
        string name = length % bankSize == copierHeaderSize ? "input.smc" : "input.sfc";
        return Path.Combine(directory.FullName, name);
    }

    /// <summary>Runs the external assembler on an already staged ROM and captures its diagnostics.</summary>
    private static string RunAsar(string stagedRom, string patchPath)
    {
        string executable = GetAsarExecutable();
        var start = new ProcessStartInfo(executable)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        start.ArgumentList.Add("--no-title-check");
        start.ArgumentList.Add(patchPath);
        start.ArgumentList.Add(stagedRom);

        using var process = Process.Start(start)
            ?? throw new InvalidOperationException($"Could not start Asar at '{executable}'.");
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        process.WaitForExit();
        Task.WaitAll(stdout, stderr);

        if (process.ExitCode != 0)
            throw new InvalidOperationException(
                $"Asar patch '{patchPath}' failed (exit code {process.ExitCode}).\n{stdout.Result}\n{stderr.Result}");
        return string.IsNullOrWhiteSpace(stderr.Result) ? stdout.Result : stdout.Result + stderr.Result;
    }

    /// <summary>Patches a ROM buffer through a temporary file. Use the file overload if the patch expands the ROM.</summary>
    public static string Apply(byte[] romData, string patchPath)
    {
        var directory = Directory.CreateTempSubdirectory("hitman-asar-");
        string rom = GetStagedRomPath(directory, romData.LongLength);
        try
        {
            File.WriteAllBytes(rom, romData);
            string result = RunAsar(rom, patchPath);
            byte[] patched = File.ReadAllBytes(rom);
            if (patched.Length > romData.Length)
                throw new InvalidOperationException("Asar expanded the ROM. Use the file-based Apply overload to keep the new size.");
            patched.CopyTo(romData, 0);
            return result;
        }
        finally
        {
            File.Delete(rom);
            directory.Delete();
        }
    }

    /// <summary>Uses the environment override or the executable distributed for the current platform.</summary>
    private static string GetAsarExecutable()
    {
        string? configured = Environment.GetEnvironmentVariable("HITMAN_ASAR_EXE");
        if (!string.IsNullOrWhiteSpace(configured))
            return configured;

        string fileName = OperatingSystem.IsWindows() ? "asar.exe" :
            OperatingSystem.IsLinux() ? "asar" :
            throw new PlatformNotSupportedException("Bundled Asar is available for Windows and Linux.");

        string path = Path.Combine(AppContext.BaseDirectory, "Asar", fileName);
        if (!File.Exists(path))
            throw new FileNotFoundException("Bundled Asar executable was not found.", path);

        if (OperatingSystem.IsLinux())
        {
            UnixFileMode mode = File.GetUnixFileMode(path);
            if ((mode & UnixFileMode.UserExecute) == 0)
                File.SetUnixFileMode(path, mode | UnixFileMode.UserExecute);
        }
        return path;
    }
}
