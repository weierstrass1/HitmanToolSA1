namespace HitmanBackend
{
    /// <summary>Prepares includes for external defines supplied by PIXI.</summary>
    public class ExtraDefines
    {
        /// <summary>Copies generated defines and links them in the UberASMTool macro library.</summary>
        public static void PublishToUberasm(string definesPath, string libraryPath)
        {
            const string include = "incsrc \"HitmanDefines.asm\"";
            string content = File.ReadAllText(libraryPath);
            bool linked = File.ReadLines(libraryPath).Any(line =>
                System.Text.RegularExpressions.Regex.IsMatch(LinkedInputs.StripComment(line),
                    """^incsrc\s+"(?:\./)?HitmanDefines\.asm"$""",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase |
                    System.Text.RegularExpressions.RegexOptions.CultureInvariant));

            File.Copy(definesPath, Path.Combine(Path.GetDirectoryName(libraryPath)!, "HitmanDefines.asm"), true);
            if (linked)
                return;

            string newline = content.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
            string separator = content.Length == 0 || content.EndsWith('\n') ? "" : newline;
            File.AppendAllText(libraryPath, separator + include + newline);
        }

        /// <summary>Builds external includes while excluding the Hitman-generated defines.</summary>
        public static string BuildIncsrcFile(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
                return "";
            string[] files = Directory.GetFiles(directoryPath, "*.asm");
            return $"{string.Join('\n', files
                .Where(f => Path.GetFileNameWithoutExtension(f) != "HitmanDefines")
                .Order(StringComparer.Ordinal)
                .Select(f => $"incsrc \"{Path.GetRelativePath(Path.Combine("Patch","Files"),f).Replace('\\', '/')}\""))}";
        }
    }
}
