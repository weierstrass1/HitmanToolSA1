using HitmanFormatReader;

using System.Text;

namespace HitmanBackend
{
    /// <summary>Generates context defines and routine includes.</summary>
    public class TableCreator
    {
        /// <summary>Assigns each context its starting index by accumulating pose counts.</summary>
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
        /// <summary>Emits includes for selected routines inside the callback namespace.</summary>
        public static string RoutinesIncludes(List<string> actions)
        {
            StringBuilder sb = new();
            sb.AppendLine("namespace Hitman_Callback");
            foreach (var action in actions)
                sb.AppendLine($"\tincsrc \"../Routines/{action}.asm\"");
            sb.AppendLine("namespace off");
            return sb.ToString();
        }
    }
}
