using System.Collections.Generic;

namespace ETABS_CAD_Automation.Models
{
    /// <summary>
    /// Configuration for each floor type in the building
    /// </summary>
    public class FloorTypeConfig
    {
        public string Name { get; set; }
        public int Count { get; set; }
        public double Height { get; set; }
        public string CADFilePath { get; set; }
        public Dictionary<string, string> LayerMapping { get; set; }

        public FloorTypeConfig()
        {
            LayerMapping = new Dictionary<string, string>();
        }

        /// <summary>
        /// Total height of all floors of this type
        /// </summary>
        public double TotalHeight => Count * Height;

        /// <summary>
        /// Generate story names for this floor type
        /// </summary>
        public List<string> GenerateStoryNames()
        {
            List<string> names = new List<string>();

            for (int i = 0; i < Count; i++)
            {
                string storyName = "";

                switch (Name)
                {
                    case "Basement":
                        storyName = $"Basement{i + 1}";
                        break;
                    case "Podium":
                        storyName = $"Podium{i + 1}";
                        break;
                    case "EDeck":
                        storyName = "EDeck";
                        break;
                    case "Typical":
                        storyName = $"Story{i + 1}";
                        break;
                    default:
                        storyName = $"{Name}{i + 1}";
                        break;
                }

                names.Add(storyName);
            }

            return names;
        }

        public override string ToString()
        {
            return $"{Name}: {Count} floors × {Height:F2}m = {TotalHeight:F2}m";
        }
    }
}