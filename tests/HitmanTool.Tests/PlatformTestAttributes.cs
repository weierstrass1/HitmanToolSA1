namespace HitmanTool.Tests;

public sealed class LinuxFactAttribute : FactAttribute
{
    public LinuxFactAttribute()
    {
        if (!OperatingSystem.IsLinux()) Skip = "Requires a real Linux runtime and filesystem.";
    }
}

public sealed class LinuxTheoryAttribute : TheoryAttribute
{
    public LinuxTheoryAttribute()
    {
        if (!OperatingSystem.IsLinux()) Skip = "Requires a real Linux runtime and filesystem.";
    }
}

public sealed class WindowsNativeFactAttribute : FactAttribute
{
    public WindowsNativeFactAttribute()
    {
        if (!OperatingSystem.IsWindows()) Skip = "The bundled asar.dll is a Windows x86 library; Linux uses the executable.";
    }
}
