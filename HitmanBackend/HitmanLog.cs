using System.Text;
using LogRegister;

namespace HitmanBackend;

/// <summary>Collects Hitman events for console output and the log file.</summary>
public sealed class HitmanLog
{
    /// <summary>Holds events until the log is written.</summary>
    private readonly LogRegisterSystem register = new();

    /// <summary>Records the title of an installation stage.</summary>
    public void Title(string title) => Add(Level.Title, "TITLE", ("title", title));
    /// <summary>Records a plain informational message.</summary>
    public void Info(string message) => Add(Level.Info, "INFO", ("message", message));
    /// <summary>Records a templated message with parameters that can be highlighted.</summary>
    public void Info(string template, params (string Name, string Value)[] values) =>
        Add(Level.Info, template, values);
    /// <summary>Records a successful outcome.</summary>
    public void Success(string message) => Add(Level.Success, "OK", ("message", message));
    /// <summary>Records a diagnostic that allows processing to continue.</summary>
    public void Warning(string message) => Add(Level.Warning, "WARNING", ("message", message));
    /// <summary>Records a failure for the error stream.</summary>
    public void Error(string message) => Add(Level.Error, "ERROR", ("message", message));

    /// <summary>Queues an event with its level, template, and parameters.</summary>
    private void Add(Level level, string template, params (string Name, string Value)[] values) =>
        register.Add(new Entry(level, template, values));

    /// <summary>Renders messages to the console and saves a plain-text copy to the given path.</summary>
    public void Write(string path)
    {
        var renderer = new LogRenderer(Path.Combine(AppContext.BaseDirectory, "Logging", "LogMessages.json"));
        var plainText = new StringBuilder();
        foreach (var entry in register.GetRegisters())
        {
            var level = (Level)entry.Category;
            bool error = ReferenceEquals(level, Level.Error);
            var writer = error ? Console.Error : Console.Out;
            bool useColor = error ? !Console.IsErrorRedirected : !Console.IsOutputRedirected;

            try
            {
                renderer.Render(entry).Render((text, _, span, _) =>
                {
                    if (span == SpanType.Prefix)
                        return;

                    if (useColor)
                    {
                        Console.ForegroundColor = span == SpanType.Parameter && entry.MessageType != "INFO"
                            ? level.ParameterColor
                            : level.Color;
                    }
                    writer.Write(text);
                    plainText.Append(text);
                });
            }
            finally
            {
                if (useColor)
                    Console.ResetColor();
            }
        }
        File.WriteAllText(path, plainText.ToString());
    }

    /// <summary>Console colors for a message and its parameters.</summary>
    private sealed record Level(ConsoleColor Color, ConsoleColor ParameterColor) : ILogCategory
    {
        /// <summary>Records the title of an installation stage.</summary>
        public static Level Title { get; } = new(ConsoleColor.Magenta, ConsoleColor.DarkMagenta);
        /// <summary>Informational level and its output colors.</summary>
        public static Level Info { get; } = new(ConsoleColor.Gray, ConsoleColor.Cyan);
        /// <summary>Records a successful outcome.</summary>
        public static Level Success { get; } = new(ConsoleColor.Green, ConsoleColor.Green);
        /// <summary>Records a diagnostic that allows processing to continue.</summary>
        public static Level Warning { get; } = new(ConsoleColor.Yellow, ConsoleColor.Yellow);
        /// <summary>Records a failure for the error stream.</summary>
        public static Level Error { get; } = new(ConsoleColor.Red, ConsoleColor.Red);
    }

    /// <summary>Log event carrying a message level, template, and parameters.</summary>
    private sealed class Entry(Level level, string template, (string Name, string Value)[] values)
        : ILoggingRegister
    {
        /// <summary>Indicates that the event is visible in normal mode.</summary>
        public bool AppearWithoutVerbose => true;
        /// <summary>Indicates that the event is visible after a failure.</summary>
        public bool AppearInErrors => true;
        /// <summary>Output level of the event.</summary>
        public ILogCategory Category => level;
        /// <summary>Template key for the event.</summary>
        public string MessageType => template;
        /// <summary>Values substituted into the event template.</summary>
        public IReadOnlyDictionary<string, string> Parameters { get; } =
            values.ToDictionary(value => value.Name, value => value.Value, StringComparer.Ordinal);
    }
}
