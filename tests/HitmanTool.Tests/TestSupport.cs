global using Xunit;
using HitmanFormatReader;
using HitmanBackend.DTO;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace HitmanTool.Tests;

// Run sequentially because the APIs share the working directory, console, and environment variables.
public sealed class Sandbox : IDisposable
{
    private readonly string previous = Environment.CurrentDirectory;
    public string Root { get; } = Path.Combine(Path.GetTempPath(), "hitman-tests-" + Guid.NewGuid());
    public Sandbox() { Directory.CreateDirectory(Root); Environment.CurrentDirectory = Root; }
    public string Write(string path, string text)
    {
        string full = Path.GetFullPath(Path.Combine(Root, path));
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, text);
        return full;
    }
    public void Dispose() { Environment.CurrentDirectory = previous; Directory.Delete(Root, true); }
}

internal static class Samples
{
    public const string Sectioned = "Hitbox7:\n .XOffset $0002\n .YOffset $FFFE\n .Width $0008\n .Height $000C\nHitboxesPerPose:\n .Pose0 7\n";
    public const string Legacy = "PoseHitBoxes: db $00,$FF,$FF\nHitboxWidth: dw $0008\nHitboxHeight: dw $000C\nHitboxXOffset: dw $FFFE\n";
    public static Hitbox Box(int x = 0, string first = "Nothing", string second = "Nothing",
        string[]? dependencies = null, IReadOnlyDictionary<string, string>? callbacks = null) => new()
    {
        XOffset = x, XOffsetFlip = x, YOffset = 0, YOffsetFlip = 0, Width = 16, Height = 16,
        Action1 = first, Action2 = second, Dependencies = dependencies ?? [], Callbacks = callbacks ?? new Dictionary<string, string>()
    };
    public static HitboxInfo Info(string name, Hitbox[] boxes, params int[][] poses) => new()
    {
        ContextName = name, Hitboxes = boxes.ToList(),
        PosesHitboxes = poses.Select(p => new PoseHitboxes { Hitboxes = p }).ToList()
    };
    public static SpriteCustomProperties Sprite(string name = "Test", bool custom = false, string? id = "01",
        string? legacy = null, VanillaCallbackContexts? callbacks = null) => new()
    {
        Name = name, IsCustom = custom, ID = id, Properties = [],
        VanillaInteractionCallback = legacy, VanillaCallbacks = callbacks
    };
    // Use an independent parser to verify the generated tables.
    public static int[] Words(string text, string label)
    {
        string body = text.Split(label + ":", 2)[1];
        var lines = body.Split('\n').TakeWhile(l => !l.TrimEnd().EndsWith(':'));
        return lines.Select(l => l.Split(';')[0].Trim()).Where(l => l.StartsWith("dw "))
            .SelectMany(l => l[3..].Split(',')).Select(v => Convert.ToInt32(v.Trim()[1..], 16)).ToArray();
    }
}
