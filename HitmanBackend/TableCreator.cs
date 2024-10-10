using System;
using System.Text;

namespace HitmanBackend
{
    public class TableCreator
    {
        public static string ContextPoseHitboxesTable(List<HitboxInfo> hitboxinfos, List<PoseHitboxes> poseHitboxes)
        {
            List<int> posehitboxesIndexes = new();
            foreach(var hitboxinfo in hitboxinfos)
            {
                foreach(var ph in hitboxinfo.PosesHitboxes)
                {
                    posehitboxesIndexes.Add(poseHitboxes.IndexOf(ph) * 2);
                }
            }
            return HexReader.ValuesToString(posehitboxesIndexes.ToArray(),
                                4, 16);
        }
        public static string PoseHitboxesIndexersTable(List<PoseHitboxes> poseHitboxes)
        {
            List<int> PoseHitboxesIndexers = new();
            int counter = 0;
            foreach (var poshitboxe in poseHitboxes)
            {
                PoseHitboxesIndexers.Add(counter * 2);
                counter += poshitboxe.Hitboxes.Length + 1;
            }
            return HexReader.ValuesToString(PoseHitboxesIndexers.ToArray(),
                    4, 16);
        }
        public static string PoseHitboxesTable(List<PoseHitboxes> poseHitboxes)
        {
            StringBuilder sb = new();
            int[] arr;
            string line;
            int counter = 0;
            foreach (var poshitboxe in poseHitboxes)
            {
                arr = new int[poshitboxe.Hitboxes.Length + 1]; 
                poshitboxe.Hitboxes.CopyTo(arr, 0);
                arr[^1] = 65535;
                line = HexReader
                        .ValuesToString(arr, 4, arr.Length)
                        .Trim();
                line = '\t' + line + $"\t;{(counter * 2):X4}-";
                counter += arr.Length;
                line += $"{((counter - 1) * 2):X4}";
                sb.AppendLine(line);
            }
            return '\n' + sb.ToString();
        }
        public static string HitboxXOffsetTable(List<Hitbox> hitboxes)
        {
            int[] xoffsets = hitboxes.Select(hb => hb.XOffset).ToArray();
            return HexReader.ValuesToString(xoffsets, 4, 16);
        }
        public static string HitboxXOffsetFlipTable(List<Hitbox> hitboxes)
        {
            int[] xoffsetsFlips = hitboxes.Select(hb => hb.XOffsetFlip).ToArray();
            return HexReader.ValuesToString(xoffsetsFlips, 4, 16);
        }
        public static string HitboxYOffsetTable(List<Hitbox> hitboxes)
        {
            int[] yoffsets = hitboxes.Select(hb => hb.YOffset).ToArray();
            return HexReader.ValuesToString(yoffsets, 4, 16);
        }
        public static string HitboxYOffsetFlipTable(List<Hitbox> hitboxes)
        {
            int[] yoffsetsFlips = hitboxes.Select(hb => hb.YOffsetFlip).ToArray();
            return HexReader.ValuesToString(yoffsetsFlips, 4, 16);
        }
        public static string HitboxWidthsTable(List<Hitbox> hitboxes)
        {
            int[] ws = hitboxes.Select(hb => hb.Width).ToArray();
            return HexReader.ValuesToString(ws, 4, 16);
        }
        public static string HitboxHeightsTable(List<Hitbox> hitboxes)
        {
            int[] hs = hitboxes.Select(hb => hb.Height).ToArray();
            return HexReader.ValuesToString(hs, 4, 16);
        }
        public static string HitboxActions1Table(List<Hitbox> hitboxes, List<string> actions)
        {
            int[] a1s = hitboxes.Select(hb => actions.IndexOf(hb.Action1)*3).ToArray();
            return HexReader.ValuesToString(a1s, 4, 16);
        }
        public static string HitboxActions2Table(List<Hitbox> hitboxes, List<string> actions)
        {
            int[] a2s = hitboxes.Select(hb => actions.IndexOf(hb.Action2)*3).ToArray();
            return HexReader.ValuesToString(a2s, 4, 16);
        }
        public static string ActionsTable(List<string> actions)
        {
            StringBuilder sb = new();
            foreach (string action in actions)
                sb.AppendLine($"\tdl Hitman_Callback_{action}");
            return '\n'+sb.ToString();
        }
        public static string Defines(List<HitboxInfo> infos)
        {
            StringBuilder sb = new StringBuilder();
            int i = 0;
            foreach (HitboxInfo info in infos)
            {
                sb.AppendLine($"!{info.ContextName}HitboxContext = ${i:X4}");
                i += info.PosesHitboxes.Count;
            }
            return sb.ToString();
        }
        public static string RoutinesIncludes(List<string> actions)
        {
            StringBuilder sb = new();
            sb.AppendLine("namespace Hitman_Callback");
            foreach (var action in actions)
                sb.AppendLine($"\tincsrc \"Routines/{action}.asm\"");
            sb.AppendLine("namespace off");
            return sb.ToString();
        }
    }
}
