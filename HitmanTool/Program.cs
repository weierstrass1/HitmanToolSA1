using HitmanFormatReader;
using HitmanBackend;
using HitmanBackend.DTO;
using Newtonsoft.Json;
using System.Text;

/// <summary>Entry point and coordinator for Hitman installation.</summary>
internal class Program
{
    /// <summary>Runs the tool, logs failures, and returns the process exit code.</summary>
    private static int Main(string[] args)
    {
        var log = new HitmanLog();
        log.Title("Hitman Tool");
        try
        {
            return Run(args, log);
        }
        catch (Exception ex)
        {
            log.Error(ex is IOException or InvalidDataException or JsonException or InvalidOperationException
                ? ex.Message : ex.ToString());
            return 1;
        }
        finally
        {
            try
            {
                log.Write("log.txt");
            }
            catch (IOException ex)
            {
                Console.Error.WriteLine($"Could not write log.txt: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.Error.WriteLine($"Could not write log.txt: {ex.Message}");
            }
        }
    }

    /// <summary>Reads the resources, generates ASM tables, and applies the patch unless generation-only mode is active.</summary>
    private static int Run(string[] args, HitmanLog log)
    {
        if (!buildSettings(args, out Settings setting))
        {
            log.Error("Installation stopped because the settings could not be read.");
            return 1;
        }

        log.Info("READ_PIXI_LIST", ("file", Path.GetFullPath(setting.PixiList)));
        bool generateOnly = args.Contains("--generate-only");
        string? uberasmLibrary = null;
        if (setting.UberasmFolder.Length != 0)
        {
            if (!Directory.Exists(setting.UberasmFolder))
                throw new DirectoryNotFoundException($"UberASMTool folder '{setting.UberasmFolder}' was not found.");

            if (!generateOnly)
            {
                uberasmLibrary = UberasmInputs.ResolveMacroLibrary(setting.UberasmFolder, setting.UberasmList);
                if (!File.Exists(uberasmLibrary))
                    throw new FileNotFoundException($"UberASMTool macro library '{uberasmLibrary}' was not found.", uberasmLibrary);
            }
        }
        else if (setting.UberasmList.Length != 0)
        {
            throw new InvalidDataException("UberasmFolder is required when UberasmList is configured.");
        }

        var properties = CustomPropertiesManager.GetProperties();
        var linkedInputs = PixiInputs.Discover(setting.PixiFolder, setting.PixiList, log);
        var linkedHitboxes = linkedInputs.Hitboxes
            .Concat(UberasmInputs.DiscoverHitboxes(setting.UberasmFolder, setting.UberasmList, log))
            .Distinct(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        var spriteProperties = CustomPropertiesManager.GetPropertiesPerSprite(linkedInputs.Properties);
        var vanillaCallbacks = VanillaCallbacks.Prepare(spriteProperties);
        List<HitboxInfo> hitboxInfos = HitmanTool.GetAllHitboxInfo("HitboxInfo", linkedHitboxes,
            log, name => File.Exists(Path.Combine("Routines", $"{name}.asm")));
        log.Info("GENERATING_TABLES", ("contexts", hitboxInfos.Count.ToString()));
        List<Hitbox> hitboxes = HitmanTool.GetAllHitboxes(hitboxInfos);
        List<string> actionsList = HitmanTool.GetAllActions(hitboxes);
        if (!actionsList.Contains("Nothing"))
            actionsList.Add("Nothing");
        foreach (var callback in VanillaCallbacks.GetNames(vanillaCallbacks).Concat(spriteProperties.SelectMany(s => s.VanillaInteractionDependencies)))
            if (!actionsList.Contains(callback))
                actionsList.Add(callback);
        foreach (var info in hitboxInfos)
        {
            foreach (var box in info.Hitboxes)
            {
                foreach (var (field, routine) in box.Callbacks.OrderBy(entry => entry.Key, StringComparer.Ordinal))
                {
                    log.Info("HITBOX_CALLBACK", ("context", info.ContextName),
                        ("box", $"Hitbox{box.SourceId}"), ("field", field), ("routine", routine));
                    if (!actionsList.Contains(routine))
                        actionsList.Add(routine);
                }
                foreach (string dependency in box.Dependencies)
                    if (!actionsList.Contains(dependency))
                        actionsList.Add(dependency);
            }
        }

        string[] baseRoutines = Directory.GetFiles(Path.Combine("Patch", "BaseRoutines"), "*.asm")
            .Select(x => Path.GetFileNameWithoutExtension(x))
            .Order(StringComparer.Ordinal)
            .ToArray();

        RoutineDependencies.Expand(actionsList, "Routines", baseRoutines, log);
        log.Info("ROUTINE_TOTAL", ("count", actionsList.Count.ToString()));
        string hitboxData = HitboxTables.Build(hitboxInfos, hitboxes, actionsList);

        if (!Directory.Exists("Patch"))
            Directory.CreateDirectory("Patch");

        string filePath = Path.Combine("Patch", "Files");

        if (!Directory.Exists(filePath))
            Directory.CreateDirectory(filePath);

        string routinesPath = Path.Combine("Patch", "Routines");

        if (!Directory.Exists(routinesPath))
            Directory.CreateDirectory(routinesPath);




        string DataPath = Path.Combine(filePath, "Data.asm");

        if (File.Exists(DataPath))
            File.Delete(DataPath);
        File.WriteAllText(DataPath, hitboxData);
        File.WriteAllText(Path.Combine(filePath, "VanillaCallbacks.asm"), VanillaCallbacks.BuildTable(vanillaCallbacks));

        string Defines = Path.Combine(filePath, "HitmanDefines.asm");

        string Includes = Path.Combine(filePath, "Includes.asm");

        string[] oldRoutinesFiles = Directory.GetFiles(routinesPath);

        foreach (var routine in oldRoutinesFiles)
            File.Delete(routine);

        foreach (var action in actionsList)
        {
            File.Copy(Path.Combine("Routines", $"{action}.asm"), Path.Combine(routinesPath, $"{action}.asm"));
        }

        StringBuilder routinesTable = new();
        StringBuilder routinesAddress = new();
        StringBuilder baseincludes = new();
        baseincludes.AppendLine("namespace Hitman_Base");

        int addressAdder = 0;

        foreach (var routine in baseRoutines)
        {
            baseincludes.AppendLine($"\tincsrc \"../BaseRoutines/{routine}.asm\"");
            routinesTable.AppendLine($"\tdl Hitman_Base_{routine}");
            routinesAddress.AppendLine($"!Hitman_Base_{routine} = read3(read3($07F7D6)+${addressAdder.ToString("X4")})");
            addressAdder += 3;
        }
        baseincludes.AppendLine("namespace off");

        foreach (var routine in actionsList)
        {
            routinesTable.AppendLine($"\tdl Hitman_Callback_{routine}");
            routinesAddress.AppendLine($"!Hitman_Callback_{routine} = read3(read3($07F7D6)+${addressAdder.ToString("X4")})");
            addressAdder += 3;
        }

        string routinesIncludes = TableCreator.RoutinesIncludes(actionsList);

        string includeContent = $"{baseincludes}\n{routinesIncludes}";
        if (File.Exists(Includes))
            File.Delete(Includes);
        File.WriteAllText(Includes, includeContent);

        string defaultDefines = File.ReadAllText(Path.Combine("ASM", "HitmanDefines.asm"));
        string contextDefines = TableCreator.Defines(hitboxInfos);


        string propsDefines = CustomPropertiesManager.GeneratePropertyDefines(properties);

        string definesContent = $"{defaultDefines}\n{routinesAddress}\n{contextDefines}\n{propsDefines}";
        if (File.Exists(Defines))
            File.Delete(Defines);
        File.WriteAllText(Defines, definesContent);

        string routinesTablePath = Path.Combine(filePath, "RoutinesTable.asm");

        if (File.Exists(routinesTablePath))
            File.Delete(routinesTablePath);
        File.WriteAllText(routinesTablePath, routinesTable.ToString());

        string propTablePath = Path.Combine(filePath, "CustomPropertiesTables.asm");

        string propTable = CustomPropertiesManager.GeneratePropertiesTables(properties, spriteProperties, setting.PixiList, log);

        if (File.Exists(propTablePath))
            File.Delete(propTablePath);
        File.WriteAllText(propTablePath, propTable);

        string propsetup = CustomPropertiesManager.GetPropSetup(properties);
        string propSetupPath = Path.Combine(filePath, "CustomPropertiesSetup.asm");

        if (File.Exists(propSetupPath))
            File.Delete(propSetupPath);
        File.WriteAllText(propSetupPath, propsetup);

        string extraDefines = Path.Combine(setting.PixiFolder, "asm", "ExtraDefines");

        string extraDefinesIncs = ExtraDefines.BuildIncsrcFile(extraDefines);

        string extradefinesResultPath = Path.Combine("Patch", "Files", "ExtraDefinesIncludes.asm");

        if (File.Exists(extradefinesResultPath))
            File.Delete(extradefinesResultPath);
        File.WriteAllText(extradefinesResultPath, extraDefinesIncs);

        if (generateOnly)
        {
            log.Success("Hitman tables and defines generated.");
            return 0;
        }

        string pixiRoutinesPath = Path.Combine("PixiRoutines");
        string targetPixiRoutinePath = Path.Combine(setting.PixiFolder, "routines");
        foreach (string file in Directory.EnumerateFiles(pixiRoutinesPath, "*.asm", SearchOption.AllDirectories))
        {
            string relativePath = Path.GetRelativePath(pixiRoutinesPath, file);
            string destination = Path.Combine(targetPixiRoutinePath, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(file, destination, overwrite: true);
        }

        Directory.CreateDirectory(extraDefines);
        string extradefinesPath = Path.Combine(extraDefines, "HitmanDefines.asm");

        string PatchPath = Path.Combine("Patch", "Hitman.asm");

        log.Title("Assembly");
        log.Info("APPLY_PATCH", ("file", PatchPath));
        string asarOutput = PatchApplier.Apply(setting.InputPath, PatchPath, setting.OutputPath);
        using (var output = new StringReader(asarOutput))
        {
            while (output.ReadLine() is { } line)
            {
                if (line.Contains("warning:", StringComparison.OrdinalIgnoreCase))
                    log.Warning(line);
                else if (line.Length > 0)
                    log.Info(line);
            }
        }

        File.Copy(Defines, extradefinesPath, true);
        if (uberasmLibrary != null)
        {
            ExtraDefines.PublishToUberasm(Defines, uberasmLibrary);
            log.Info($"Linked Hitman defines into '{uberasmLibrary}'.");
        }
        log.Info("ROUTINES_INSTALLED", ("count", actionsList.Count.ToString()));
        log.Success($"Hitman installed on {Path.GetFullPath(setting.OutputPath)}.");
        return 0;
    }
    /// <summary>Normalizes a saved or interactive path before validating it on the current OS.</summary>
    private static string NormalizeSettingsPath(string? input)
    {
        string path = input?.Trim() ?? string.Empty;
        if (path.Length >= 2 && path[0] == '"' && path[^1] == '"')
            path = path[1..^1];

        return path.Replace('\\', Path.DirectorySeparatorChar);
    }

    /// <summary>Reads an optional path, retrying invalid entries until the user skips or reaches EOF.</summary>
    private static string ReadOptionalPath(string prompt, Func<string, bool> exists)
    {
        while (true)
        {
            Console.WriteLine(prompt);
            string path = NormalizeSettingsPath(Console.ReadLine());
            if (path.Length == 0 || exists(path))
                return path;

            Console.WriteLine($"Path '{path}' was not found. Enter another path or press Enter to skip.");
        }
    }

    /// <summary>Loads saved paths, validates them, and prompts for missing paths.</summary>
    private static bool buildSettings(string[] args, out Settings settings)
    {
        string settingsPath = Path.Combine("Json", "Settings.json");
        string settingcontent = File.ReadAllText(settingsPath);
        settings = JsonConvert.DeserializeObject<Settings>(settingcontent)!;

        settings.InputPath = NormalizeSettingsPath(settings.InputPath);
        settings.OutputPath = NormalizeSettingsPath(settings.OutputPath);
        settings.PixiFolder = NormalizeSettingsPath(settings.PixiFolder);
        settings.PixiList = NormalizeSettingsPath(settings.PixiList);
        settings.UberasmFolder = NormalizeSettingsPath(settings.UberasmFolder);
        settings.UberasmList = NormalizeSettingsPath(settings.UberasmList);

        bool useSettings = args is { Length: > 0 } &&
            args[0].Equals("-use-settings", StringComparison.OrdinalIgnoreCase);
        bool doInput = true;
        bool doOutput = true;
        bool doPixiFolder = true;
        bool dolist = true;

        if (useSettings)
        {
            doInput = string.IsNullOrWhiteSpace(settings.InputPath) || 
                !File.Exists(settings.InputPath);
            doOutput = string.IsNullOrWhiteSpace(settings.OutputPath) ||
                !File.Exists(settings.OutputPath);
            doPixiFolder = string.IsNullOrWhiteSpace(settings.PixiFolder) ||
                !Directory.Exists(settings.PixiFolder);
            dolist = string.IsNullOrWhiteSpace(settings.PixiList) ||
                !File.Exists(settings.PixiList);
        }

        if(doInput)
        {
            Console.WriteLine("Write ROM Input Path (you can also Drag n Drop your rom to the console)");
            settings.InputPath = NormalizeSettingsPath(Console.ReadLine());
            if (string.IsNullOrWhiteSpace(settings.InputPath) ||
                !File.Exists(settings.InputPath))
            {
                Console.WriteLine("Input Path is not Valid.");
                return false;
            }
        }
        if (doOutput)
        {
            Console.WriteLine("Write ROM Out Path (you can also Drag n Drop your rom to the console)");
            settings.OutputPath = NormalizeSettingsPath(Console.ReadLine());
            if (string.IsNullOrWhiteSpace(settings.OutputPath) ||
                !File.Exists(settings.OutputPath))
            {
                Console.WriteLine("Output Path is not Valid.");
                return false;
            }
        }
        if (doPixiFolder)
        {
            Console.WriteLine("Write Pixi Folder Path (you can also Drag n Drop your Pixi Folder to the console)");
            settings.PixiFolder = NormalizeSettingsPath(Console.ReadLine());
            if (string.IsNullOrWhiteSpace(settings.PixiFolder) ||
                !Directory.Exists(settings.PixiFolder))
            { 
                Console.WriteLine("Pixi Folder is not Valid.");
                return false;
            }
        }
        if (dolist)
        {
            Console.WriteLine("Write Pixi List Path (you can also Drag n Drop your Pixi List to the console)");
            settings.PixiList = NormalizeSettingsPath(Console.ReadLine());
            if (string.IsNullOrWhiteSpace(settings.PixiList) ||
                !File.Exists(settings.PixiList))
            {
                Console.WriteLine("Pixi List Path is not Valid.");
                return false;
            }
        }

        if (!useSettings)
        {
            settings.UberasmFolder = ReadOptionalPath(
                "UberASMTool folder (optional, press Enter to skip):", Directory.Exists);
            string folder = settings.UberasmFolder;
            settings.UberasmList = folder.Length == 0
                ? ""
                : ReadOptionalPath(
                    "UberASMTool list (optional, press Enter to export only the defines):",
                    path => File.Exists(Path.Combine(folder, path)));
        }

        settings.InputPath = Path.GetRelativePath(".", settings.InputPath);
        settings.OutputPath = Path.GetRelativePath(".", settings.OutputPath);
        settings.PixiFolder = Path.GetRelativePath(".", settings.PixiFolder);
        settings.PixiList = Path.GetRelativePath(".", settings.PixiList);

        string jsonSet = JsonConvert.SerializeObject(settings, Formatting.Indented);

        File.Delete(settingsPath);
        File.WriteAllText(settingsPath, jsonSet);

        return true;
    }
}
