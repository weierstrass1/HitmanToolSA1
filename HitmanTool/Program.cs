using HitmanBackend;
using HitmanBackend.DTO;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

internal class Program
{
    private static void Main(string[] args)
    {
        if(!buildSettings(args, out Settings setting))
            return;

        bool error = false;
        List<HitboxInfo> hitboxInfos = HitmanTool.GetAllHitboxInfo("HitboxInfo");
        List<Hitbox> hitboxes = HitmanTool.GetAllHitboxes(hitboxInfos);
        List<string> actionsList = HitmanTool.GetAllActions(hitboxes);
        HitmanTool.RemapHitboxInfos(hitboxInfos, hitboxes);
        List<PoseHitboxes> poseHitboxes = HitmanTool.GetAllPoseHitboxes(hitboxInfos);
        string contextPoseHitboxesTable = TableCreator.ContextPoseHitboxesTable(hitboxInfos, poseHitboxes);
        string PoseHitboxesIndexersTable = TableCreator.PoseHitboxesIndexersTable(poseHitboxes);
        string PoseHitboxesTable = TableCreator.PoseHitboxesTable(poseHitboxes);
        string HitboxXOffsetTable = TableCreator.HitboxXOffsetTable(hitboxes);
        string HitboxXOffsetFlipTable = TableCreator.HitboxXOffsetFlipTable(hitboxes);
        string HitboxYOffsetTable = TableCreator.HitboxYOffsetTable(hitboxes);
        string HitboxYOffsetFlipTable = TableCreator.HitboxYOffsetFlipTable(hitboxes);
        string HitboxWidthsTable = TableCreator.HitboxWidthsTable(hitboxes);
        string HitboxHeightsTable = TableCreator.HitboxHeightsTable(hitboxes);
        string HitboxActions1Table = TableCreator.HitboxActions1Table(hitboxes, actionsList);
        string HitboxActions2Table = TableCreator.HitboxActions2Table(hitboxes, actionsList);
        string ActionsTable = TableCreator.ActionsTable(actionsList);

        StringBuilder filecontent = new();
        filecontent.AppendLine($"ContextPosesHitboxes:{contextPoseHitboxesTable}");
        filecontent.AppendLine($"PoseHitboxesIndexers:{PoseHitboxesIndexersTable}");
        filecontent.AppendLine($"PoseHitboxes:{PoseHitboxesTable}");
        filecontent.AppendLine($"HitBoxXOffsets:{HitboxXOffsetTable}");
        filecontent.AppendLine($"HitBoxXOffsetsFlip:{HitboxXOffsetFlipTable}");
        filecontent.AppendLine($"HitBoxYOffsets:{HitboxYOffsetTable}");
        filecontent.AppendLine($"HitBoxYOffsetsFlip:{HitboxYOffsetFlipTable}");
        filecontent.AppendLine($"HitboxWidths:{HitboxWidthsTable}");
        filecontent.AppendLine($"HitboxHeights:{HitboxHeightsTable}");
        filecontent.AppendLine($"HitboxActions1:{HitboxActions1Table}");
        filecontent.AppendLine($"HitboxActions2:{HitboxActions2Table}");
        filecontent.AppendLine($"HitboxActions:{ActionsTable}");

        if (!Directory.Exists("Patch"))
            Directory.CreateDirectory("Patch");

        string filePath = Path.Combine("Patch", "Files");

        if (!Directory.Exists(filePath))
            Directory.CreateDirectory(filePath);

        string routinesPath = Path.Combine("Patch", "Routines");

        if (!Directory.Exists(routinesPath))
            Directory.CreateDirectory(routinesPath);

        string baseRoutinesPath = Path.Combine("Patch", "BaseRoutines");

        if (!Directory.Exists(baseRoutinesPath))
            Directory.CreateDirectory(baseRoutinesPath);

        string DataPath = Path.Combine(filePath, "Data.asm");

        if (File.Exists(DataPath))
            File.Delete(DataPath);
        File.WriteAllText(DataPath, filecontent.ToString());

        string Defines = Path.Combine(filePath, "HitmanDefines.asm");

        string Includes = Path.Combine(filePath, "Includes.asm");

        string[] actionFiles = Directory.GetFiles("Routines", "*.asm")
            .Select(x => Path.GetFileNameWithoutExtension(x))
            .ToArray();

        string[] oldRoutinesFiles = Directory.GetFiles(routinesPath);

        foreach (var routine in oldRoutinesFiles)
            File.Delete(routine);

        foreach (var action in actionsList)
        {
            if (!actionFiles.Contains(action))
            {
                Console.WriteLine($"Routine {action}.asm doesn't exist in Routines folder.");
                error = true;
                continue;
            }
            File.Copy(Path.Combine("Routines", $"{action}.asm"), Path.Combine(routinesPath, $"{action}.asm"));
        }

        string[] baseRoutines = Directory.GetFiles(baseRoutinesPath)
            .Select(x => Path.GetFileNameWithoutExtension(x))
            .Order()
            .ToArray();

        StringBuilder routinesTable = new();
        StringBuilder routinesAddress = new();
        StringBuilder baseincludes = new();
        baseincludes.AppendLine("namespace Hitman_Base");

        int addressAdder = 0;

        foreach (var routine in baseRoutines)
        {
            baseincludes.AppendLine($"\tincsrc \"../BaseRoutines/{routine}.asm\"");
            routinesTable.AppendLine($"\tdl Hitman_Base_{routine}");
            routinesAddress.AppendLine($"!Hitman_Base_{routine} = Read3($07F7D6)+${addressAdder.ToString("X4")}");
            addressAdder += 3;
        }
        baseincludes.AppendLine("namespace off");

        foreach (var routine in actionsList)
        {
            routinesTable.AppendLine($"\tdl Hitman_Callback_{routine}");
            routinesAddress.AppendLine($"!Hitman_Callback_{routine} = Read3($07F7D6)+${addressAdder.ToString("X4")}");
            addressAdder += 3;
        }

        string routinesIncludes = TableCreator.RoutinesIncludes(actionsList);

        string includeContent = $"{baseincludes}\n{routinesIncludes}";
        if (File.Exists(Includes))
            File.Delete(Includes);
        File.WriteAllText(Includes, includeContent);

        string defaultDefines = File.ReadAllText(Path.Combine("ASM", "HitmanDefines.asm"));
        string contextDefines = TableCreator.Defines(hitboxInfos);


        var properties = CustomPropertiesManager.GetProperties();
        var spriteProperties = CustomPropertiesManager.GetPropertiesPerSprite();
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

        string propTable = CustomPropertiesManager.GeneratePropertiesTables(properties, spriteProperties, setting.PixiList);

        if (File.Exists(propTablePath))
            File.Delete(propTablePath);
        File.WriteAllText(propTablePath, propTable);

        string propsetup = CustomPropertiesManager.GetPropSetup(properties);
        string propSetupPath = Path.Combine(filePath, "CustomPropertiesSetup.asm");

        if (File.Exists(propSetupPath))
            File.Delete(propSetupPath);
        File.WriteAllText(propSetupPath, propsetup);

        string pixiRoutinesPath = Path.Combine("PixiRoutines");
        string targetPixiRoutinePath = Path.Combine(setting.PixiFolder, "routines");
        string[] files = Directory.GetFiles(pixiRoutinesPath, "*.asm", SearchOption.AllDirectories);

        string relPath;
        foreach(var file in files)
        {
            relPath = $".{file[pixiRoutinesPath.Length..]}";
            File.Copy(file, Path.Combine(targetPixiRoutinePath, relPath), true);
        }

        File.Copy(setting.InputPath, "test.smc", true);

        string PatchPath = Path.Combine("Patch", "Hitman.asm");

        PatchApplier.Apply("test.smc", PatchPath);

        File.Copy("test.smc", setting.OutputPath, true);
        File.Delete("test.smc");

        string extradefinesPath = Path.Combine(setting.PixiFolder, 
            "asm", "ExtraDefines", "HitmanDefines.asm");

        File.Copy(Defines, extradefinesPath, true);
    }
    private static bool buildSettings(string[] args, out Settings settings)
    {
        string settingsPath = Path.Combine("Json", "Settings.json");
        string settingcontent = File.ReadAllText(settingsPath);
        settings = JsonConvert.DeserializeObject<Settings>(settingcontent)!;

        bool doInput = true;
        bool doOutput = true;
        bool doPixiFolder = true;
        bool dolist = true;

        if(args != null && args.Length > 0 && args[0].ToLower() == "-use-settings")
        {
            doInput = string.IsNullOrWhiteSpace(settings.InputPath) || 
                !Path.Exists(settings.InputPath);
            doOutput = string.IsNullOrWhiteSpace(settings.OutputPath) ||
                !Path.Exists(settings.OutputPath);
            doPixiFolder = string.IsNullOrWhiteSpace(settings.PixiFolder) ||
                !Path.Exists(settings.PixiFolder);
            dolist = string.IsNullOrWhiteSpace(settings.PixiList) ||
                !Path.Exists(settings.PixiList);
        }

        if(doInput)
        {
            Console.WriteLine("Write ROM Input Path (you can also Drag n Drop your rom to the console)");
            settings.InputPath = Console.ReadLine().Replace("\"", "");
            if (string.IsNullOrWhiteSpace(settings.InputPath) ||
                !Path.Exists(settings.InputPath))
            {
                Console.WriteLine("Input Path is not Valid.");
                return false;
            }
        }
        if (doOutput)
        {
            Console.WriteLine("Write ROM Out Path (you can also Drag n Drop your rom to the console)");
            settings.OutputPath = Console.ReadLine().Replace("\"", "");
            if (string.IsNullOrWhiteSpace(settings.OutputPath) ||
                !Path.Exists(settings.OutputPath))
            {
                Console.WriteLine("Output Path is not Valid.");
                return false;
            }
        }
        if (doPixiFolder)
        {
            Console.WriteLine("Write Pixi Folder Path (you can also Drag n Drop your Pixi Folder to the console)");
            settings.PixiFolder = Console.ReadLine().Replace("\"", "");
            if (string.IsNullOrWhiteSpace(settings.PixiFolder) ||
                !Path.Exists(settings.PixiFolder))
            { 
                Console.WriteLine("Pixi Folder is not Valid.");
                return false;
            }
        }
        if (dolist)
        {
            Console.WriteLine("Write Pixi List Path (you can also Drag n Drop your Pixi List to the console)");
            settings.PixiList = Console.ReadLine().Replace("\"", "");
            if (string.IsNullOrWhiteSpace(settings.PixiList) ||
                !Path.Exists(settings.PixiList))
            {
                Console.WriteLine("Pixi List Path is not Valid.");
                return false;
            }
        }

        settings.InputPath = Path.GetRelativePath(@".\", settings.InputPath);
        settings.OutputPath = Path.GetRelativePath(@".\", settings.OutputPath);
        settings.PixiFolder = Path.GetRelativePath(@".\", settings.PixiFolder);
        settings.PixiList = Path.GetRelativePath(@".\", settings.InputPath);

        string jsonSet = JsonConvert.SerializeObject(settings, Formatting.Indented);

        File.Delete(settingsPath);
        File.WriteAllText(settingsPath, jsonSet);

        return true;
    }
}