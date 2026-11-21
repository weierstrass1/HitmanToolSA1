using HitmanBackend;
using LogRegister;

namespace HitmanTool.Tests;

public class LoggingTests
{
    private record Category : ILogCategory;
    private record OtherCategory : ILogCategory;
    private record Event(string MessageType = "MSG", bool AppearWithoutVerbose = true, bool AppearInErrors = true) : ILoggingRegister
    {
        public ILogCategory Category { get; init; } = new Category();
        public IReadOnlyDictionary<string, string> Parameters { get; init; } = new Dictionary<string, string> { ["name"] = "Mario" };
    }

    [Fact]
    public void Register_preserves_insertion_order_and_exposes_readonly_live_view()
    {
        var register = new LogRegisterSystem(); var view = register.GetRegisters();
        Assert.Empty(view); Assert.False(register.HasLogsOfType<Category>());
        var a = new Event(); var b = new Event("SECOND");
        register.Add(a); register.Add(b);
        Assert.Equal(new[] { a, b }, view);
        Assert.True(register.HasLogsOfType<Category>());
        Assert.False(register.HasLogsOfType<OtherCategory>());
        Assert.Throws<NotSupportedException>(() => ((IList<ILoggingRegister>)view).Clear());
    }

    [Fact]
    public void Renderer_replaces_parameters_preserves_missing_values_and_marks_correct_ranges()
    {
        using var fs = new Sandbox();
        var renderer = new LogRenderer(fs.Write("templates.json", "{\"MSG\":\"Hello {name}: {missing}, {name}!\"}"));
        var entry = new Event(); var result = renderer.Render(entry);
        Assert.Equal("[MSG]: Hello Mario: {missing}, Mario!", result.Text);
        Assert.Same(entry.Category, result.Category);
        Assert.Equal(SpanType.Prefix, result.Spans[0].Type);
        Assert.Equal("[MSG]: ", result.Text.Substring(result.Spans[0].Start, result.Spans[0].Length));
        Assert.Equal(new[] { "Mario", "{missing}", "Mario" }, result.Spans.Skip(1).Select(s => result.Text.Substring(s.Start, s.Length)));
        var chunks = new List<string>();
        result.Render((text, _, _, must) => { Assert.True(must); chunks.Add(text); }, true);
        Assert.Equal(result.Text + "\n", string.Concat(chunks));
    }

    [Fact]
    public void Unknown_template_preserves_category_for_rendering()
    {
        using var fs = new Sandbox();
        var entry = new Event("MISSING");
        var result = new LogRenderer(fs.Write("templates.json", "{}")).Render(entry);
        Assert.Contains("MISSING", result.Text);
        Assert.Same(entry.Category, result.Category);
    }

    [Theory]
    [InlineData(false, false, false, false, false)]
    [InlineData(false, false, true, false, true)]
    [InlineData(true, false, false, false, true)]
    [InlineData(false, true, true, false, false)]
    [InlineData(false, true, false, true, true)]
    public void RenderAll_propagates_visibility_flags(bool verbose, bool error, bool normal, bool onError, bool expected)
    {
        using var fs = new Sandbox();
        var renderer = new LogRenderer(fs.Write("templates.json", "{\"MSG\":\"text\"}"));
        var flags = new List<bool>();
        renderer.RenderAll([new Event("MSG", normal, onError)], (_, _, _, must) => flags.Add(must), verbose, error);
        Assert.NotEmpty(flags); Assert.All(flags, flag => Assert.Equal(expected, flag));
    }

    [Fact]
    public void Renderer_reports_missing_config_and_accepts_null_mapping()
    {
        using var fs = new Sandbox();
        Assert.Throws<FileNotFoundException>(() => new LogRenderer("missing.json"));
        Assert.Contains("UNKNOWN", new LogRenderer(fs.Write("null.json", "null")).Render(new Event()).Text);
    }

    [Fact]
    public void RenderResult_emits_gaps_spans_tail_and_newline_in_order()
    {
        var category = new Category();
        var result = new LogRenderResult { Category = category, Text = "aBCd", Spans = [new() { Start = 1, Length = 2, Type = SpanType.Parameter, Category = category }] };
        var parts = new List<(string, SpanType)>();
        result.Render((text, cat, kind, _) => { Assert.Same(category, cat); parts.Add((text, kind)); });
        Assert.Equal(new[] { ("a", SpanType.NormalText), ("BC", SpanType.Parameter), ("d", SpanType.NormalText), ("\n", SpanType.NormalText) }, parts);
    }

    [Fact]
    public void HitmanLog_routes_errors_and_saves_uncolored_text_in_insertion_order()
    {
        using var fs = new Sandbox();
        var log = new HitmanLog();
        log.Title("Stage"); log.Info("Plain"); log.Info("READ_PIXI_LIST", ("file", "list.txt"));
        log.Success("Success"); log.Warning("Warning"); log.Error("Failure");
        var stdout = new StringWriter(); var stderr = new StringWriter();
        var oldOut = Console.Out; var oldError = Console.Error;
        try { Console.SetOut(stdout); Console.SetError(stderr); log.Write("log.txt"); }
        finally { Console.SetOut(oldOut); Console.SetError(oldError); }
        Assert.Contains("Stage", stdout.ToString()); Assert.Contains("list.txt", stdout.ToString());
        Assert.DoesNotContain("Failure", stdout.ToString()); Assert.Contains("Failure", stderr.ToString());
        string saved = File.ReadAllText("log.txt");
        foreach (string expected in new[] { "Stage", "Plain", "list.txt", "Success", "Warning", "Failure" }) Assert.Contains(expected, saved);
        Assert.DoesNotContain("\u001b", saved, StringComparison.Ordinal);
        Assert.True(saved.IndexOf("Stage", StringComparison.Ordinal) < saved.IndexOf("Failure", StringComparison.Ordinal));
    }

    [Fact]
    public void Hitman_events_are_visible_in_normal_and_error_rendering()
    {
        var log = new HitmanLog(); log.Info("Example");
        var field = typeof(HitmanLog).GetField("register", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        var entry = Assert.Single(((LogRegisterSystem)field.GetValue(log)!).GetRegisters());
        Assert.True(entry.AppearWithoutVerbose);
        Assert.True(entry.AppearInErrors);
        Assert.Equal("INFO", entry.MessageType);
        Assert.Equal("Example", entry.Parameters["message"]);
    }
}
