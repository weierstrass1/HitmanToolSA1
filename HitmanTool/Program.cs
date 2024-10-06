using HitmanBackend;
using System.Collections.Generic;
using System.Text;

internal class Program
{
    private static void Main(string[] args)
    {
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

        if (File.Exists("HitmanToolData.asm"))
            File.Delete("HitmanToolData.asm");
        File.WriteAllText("HitmanToolData.asm",filecontent.ToString());

        if (File.Exists("HitmanToolDefines.asm"))
            File.Delete("HitmanToolDefines.asm");
        File.WriteAllText("HitmanToolDefines.asm", TableCreator.Defines(hitboxInfos));
        
        string routinesIncludes = TableCreator.RoutinesIncludes(actionsList);
        if (File.Exists("HitmanToolIncludes.asm"))
            File.Delete("HitmanToolIncludes.asm");
        File.WriteAllText("HitmanToolIncludes.asm", routinesIncludes);

    }
}