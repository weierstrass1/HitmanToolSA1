
namespace HitmanBackend.DTO
{
    [Serializable]
    public class Settings
    {
        public string InputPath { get; set; }
        public string OutputPath { get; set; }
        public string PixiFolder { get; set; }
        public string PixiList { get; set; }
    }
}
