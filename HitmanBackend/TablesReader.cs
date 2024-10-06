using System.Text.RegularExpressions;

namespace HitmanBackend
{
    public partial class TablesReader
    {
        private const string BIG_TABLE_PATTERN = @"[A-Za-z]([A-Za-z0-9]|(_|-))*:([^:]*(\n|$))+";
        private static readonly Regex bigTableRegex = new(BIG_TABLE_PATTERN);
        public static HitboxInfo ReadHitboxInfo(string input)
        {
            Dictionary<string, string> tables = split(input, bigTableRegex);

            HitboxInfo hi = new();

            int[] posesHitboxValues = HexReader.GetValues(tables["PoseHitBoxes"]);

            int j = 0;

            hi.PosesHitboxes = new();
            int[] sub;

            for (int i = 0; i < posesHitboxValues.Length; i++)
            {
                if (posesHitboxValues[i] != 255)
                    continue;
                sub = i <= j ?
                            Array.Empty<int>() :
                            posesHitboxValues[j..i];
                hi.PosesHitboxes.Add(new() { Hitboxes = sub });
                j = i + 1;
            }

            int[] xOffs = HexReader.GetValues(tables["HitboxXOffset"]);
            int[] xOffsFlip = tables.ContainsKey("HitboxXOffsetFlip") ?
                                HexReader.GetValues(tables["HitboxXOffsetFlip"]) :
                                xOffs;
            int[] yOffs = HexReader.GetValues(tables["HitboxYOffset"]);
            int[] yOffsFlip = tables.ContainsKey("HitboxYOffsetFlip") ?
                                HexReader.GetValues(tables["HitboxYOffsetFlip"]) :
                                yOffs;
            int[] ws = HexReader.GetValues(tables["HitboxWidth"]);
            int[] hs = HexReader.GetValues(tables["HitboxHeight"]);
            int[] a1 = HexReader.GetValues(tables["HitboxAction1"]);
            int[] a2 = HexReader.GetValues(tables["HitboxAction2"]);

            string[] actions = tables["Actions"]
                                        .Split('\n')
                                        .Select(x => x.Replace("dw ", "").Trim())
                                        .Where(x => x != "" && x.Length > 0 && x != null)
                                        .ToArray();
            hi.Hitboxes = new();
            for (int i = 0; i < xOffs.Length; i++)
            {
                hi.Hitboxes.Add(new()
                {
                    XOffset = (ushort)xOffs[i],
                    XOffsetFlip = (ushort)xOffsFlip[i],
                    YOffset = (ushort)yOffs[i],
                    YOffsetFlip = (ushort)yOffsFlip[i],
                    Width = (ushort)ws[i],
                    Height = (ushort)hs[i],
                    Action1 = actions[a1[i] / 2],
                    Action2 = actions[a2[i] / 2]
                });
            }
            return hi;
        }
        private static Dictionary<string, string> split(string input, Regex reg)
        {
            MatchCollection matches = reg.Matches(input);
            Dictionary<string,string> vals = new();

            string strMatch;
            string title;
            string content;
            foreach (var match in matches)
            {
                strMatch = match.ToString()!;
                title = strMatch.Split('\n')[0];
                content = strMatch[title.Length..];

                vals.Add(title.Trim().Replace(":",""), content);
            }
            return vals;
        }
    }
}
