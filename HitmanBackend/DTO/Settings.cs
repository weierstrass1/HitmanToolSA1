
namespace HitmanBackend.DTO
{
    /// <summary>Input and output paths persisted between runs.</summary>
    [Serializable]
    public class Settings
    {
        /// <summary>Input ROM for installation.</summary>
        public string InputPath { get; set; }
        /// <summary>Destination ROM after installing Hitman.</summary>
        public string OutputPath { get; set; }
        /// <summary>PIXI directory that receives generated routines and defines.</summary>
        public string PixiFolder { get; set; }
        /// <summary>Path to the PIXI list used for resource discovery.</summary>
        public string PixiList { get; set; }
        /// <summary>Optional UberASMTool root directory that receives generated defines.</summary>
        public string UberasmFolder { get; set; } = "";
        /// <summary>Optional UberASMTool insertion list, relative to its root directory.</summary>
        public string UberasmList { get; set; } = "";
    }
}
