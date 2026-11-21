using HitmanFormatReader;

namespace HitmanBackend
{
    /// <summary>Discovers and prepares hitbox resources for the generator.</summary>
    public class HitmanTool
    {
        /// <summary>Reads both hitbox formats. A linked input takes precedence over a central file of the same name.</summary>
        public static List<HitboxInfo> GetAllHitboxInfo(
            string inputPath, IEnumerable<string>? linkedInputs = null,
            HitmanLog? log = null, Func<string, bool>? routineExists = null)
        {
            var local = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string file in linkedInputs ?? [])
            {
                string name = Path.GetFileNameWithoutExtension(file);
                if (!local.TryAdd(name, file))
                    throw new InvalidDataException(
                        $"Hitbox context '{name}' is declared by both '{local[name]}' and '{file}'.");
            }

            var files = Directory.Exists(inputPath)
                ? Directory.EnumerateFiles(inputPath)
                : Enumerable.Empty<string>();
            var hitboxInfos = new List<HitboxInfo>();
            var errors = new List<string>();
            foreach (string file in files)
            {
                string name = Path.GetFileNameWithoutExtension(file);
                Add(local.Remove(name, out string? linked) ? linked : file, name);
            }

            foreach ((string name, string file) in local)
                Add(file, name);

            if (errors.Count != 0)
                throw new InvalidDataException(string.Join(Environment.NewLine, errors));

            return hitboxInfos;

            // Reads a context, derives its name from the file, and collects errors with their source paths.
            void Add(string file, string name)
            {
                string path = Path.GetFullPath(file);
                log?.Info("READ_HITBOXES", ("file", path));
                try
                {
                    if (name.Length == 0 || name.Any(c => !char.IsAsciiLetterOrDigit(c) && c != '_'))
                        throw new InvalidDataException($"{path}: the filename must use letters, digits or underscores for the Asar context define.");

                    HitboxInfo info = HitboxInfoReader.ReadHitboxInfo(File.ReadAllText(path), path, routineExists);
                    info.ContextName = name;
                    if (info.Hitboxes.Any(box => box.Callbacks.Keys.Any(field => field is not ("OnPlayerCollision" or "OnPlayerNonCollision"))))
                        log?.Warning($"{path}: non-player hitbox callbacks are included in the routine table; gameplay dispatch is pending.");
                    hitboxInfos.Add(info);
                    log?.Info("HITBOX_COUNTS", ("sprite", name), ("poses", info.PosesHitboxes.Count.ToString()),
                        ("hitboxes", info.Hitboxes.Count.ToString()));
                }
                catch (InvalidDataException ex)
                {
                    errors.Add(ex.Message);
                }
                catch (IOException ex)
                {
                    errors.Add($"{path}: {ex.Message}");
                }
            }
        }

        /// <summary>Collects distinct historical entries in first-seen order to preserve callback IDs.</summary>
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
        /// <summary>Collects unique action names and declared dependencies.</summary>
        public static List<string> GetAllActions(List<Hitbox> hitboxes)
        {
            List<string> actionsList = new();
            foreach(var hitbox in hitboxes)
            {
                if(!actionsList.Contains(hitbox.Action1))
                    actionsList.Add(hitbox.Action1);
                if (!actionsList.Contains(hitbox.Action2))
                    actionsList.Add(hitbox.Action2);
                foreach(var dep in hitbox.Dependencies)
                {
                    if (!actionsList.Contains(dep))
                        actionsList.Add(dep);
                }
            }
            return actionsList;
        }
    }
}
