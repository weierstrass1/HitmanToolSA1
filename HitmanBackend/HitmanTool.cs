using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HitmanBackend
{
    public class HitmanTool
    {
        public static List<HitboxInfo> GetAllHitboxInfo(string inputPath)
        {
            string[] filesPaths = Directory.GetFiles(inputPath);
            List<HitboxInfo> hitboxinfos = new();

            string content;
            HitboxInfo hi;
            foreach (string file in filesPaths)
            {
                content = File.ReadAllText(file);
                hi = TablesReader.ReadHitboxInfo(content);
                hi.ContextName = Path.GetFileNameWithoutExtension(file);
                hitboxinfos.Add(hi);
            }

            return hitboxinfos;
        }
        public static List<Hitbox> GetAllHitboxes(List<HitboxInfo> hitboxinfos)
        {
            List<Hitbox> hitboxes = new();

            foreach (HitboxInfo info in hitboxinfos)
            {
                foreach (Hitbox hitbox in info.Hitboxes)
                {
                    if (!hitboxes.Contains(hitbox))
                        hitboxes.Add(hitbox);
                }
            }

            return hitboxes;
        }
        public static void RemapHitboxInfos(List<HitboxInfo> hitboxinfos, List<Hitbox> hitboxes)
        {
            Hitbox hb;
            Dictionary<int, int> added = new();
            List<Hitbox> hbs = new();
            foreach (HitboxInfo info in hitboxinfos)
            {
                foreach (var hitboxIndex in info.PosesHitboxes)
                {
                    hbs.Clear();
                    for (int i = 0; i < hitboxIndex.Hitboxes.Length; i++)
                    { 
                        hb = info.Hitboxes[hitboxIndex.Hitboxes[i]];
                        hitboxIndex.Hitboxes[i] = hitboxes.IndexOf(hb) * 2;
                        if (!added.ContainsKey(hitboxIndex.Hitboxes[i]))
                            added.Add(hitboxIndex.Hitboxes[i], 0);
                        added[hitboxIndex.Hitboxes[i]]++;
                    }
                }
            }
        }
        public static List<PoseHitboxes> GetAllPoseHitboxes(List<HitboxInfo> hitboxinfos)
        {
            List<PoseHitboxes> hitboxes = new();

            foreach (HitboxInfo info in hitboxinfos)
            {
                foreach (PoseHitboxes poseHitboxes in info.PosesHitboxes)
                {
                    if (!hitboxes.Contains(poseHitboxes))
                        hitboxes.Add(poseHitboxes);
                }
            }

            return hitboxes;
        }
        public static List<string> GetAllActions(List<Hitbox> hitboxes)
        {
            List<string> actionsList = new();
            foreach(var hitbox in hitboxes)
            {
                if(!actionsList.Contains(hitbox.Action1))
                    actionsList.Add(hitbox.Action1);
                if (!actionsList.Contains(hitbox.Action2))
                    actionsList.Add(hitbox.Action2);
            }
            return actionsList;
        }
    }
}
