// ============================================================================
// FILE: Core/StoryManager.cs
// ============================================================================
using ETABSv1;
using System;
using System.Collections.Generic;

namespace ETABS_CAD_Automation.Core
{
    public class StoryManager
    {
        private readonly cSapModel sapModel;

        public StoryManager(cSapModel model)
        {
            sapModel = model;
        }

        public void DefineStoriesWithCustomNames(List<double> storyHeights, List<string> storyNames)
        {
            sapModel.SetModelIsLocked(false);

            int numStories = storyHeights.Count;

            if (storyHeights.Count != storyNames.Count)
            {
                throw new ArgumentException("Story heights and names count mismatch");
            }

            double baseElev = 0.0;

            string[] names = new string[numStories];
            double[] elevs = new double[numStories];
            bool[] master = new bool[numStories];
            string[] similar = new string[numStories];
            bool[] splice = new bool[numStories];
            double[] spliceHt = new double[numStories];
            int[] colors = new int[numStories];

            double cumulativeHeight = 0.0;

            for (int i = 0; i < numStories; i++)
            {
                names[i] = storyNames[i];
                elevs[i] = storyHeights[i];
                master[i] = (i == 0);
                similar[i] = (i == 0) ? "" : storyNames[0];
                splice[i] = false;
                spliceHt[i] = 0.0;
                colors[i] = AssignColorByStoryType(storyNames[i]);

                cumulativeHeight += storyHeights[i];
            }

            int ret = sapModel.Story.SetStories_2(
                baseElev, numStories, ref names, ref elevs,
                ref master, ref similar, ref splice, ref spliceHt, ref colors
            );

            if (ret != 0)
            {
                throw new Exception($"Failed to define stories. Error code: {ret}");
            }

            VerifyStories();
            sapModel.View.RefreshView(0, true);
        }

        private int AssignColorByStoryType(string storyName)
        {
            if (storyName.StartsWith("Basement"))
                return 255;
            else if (storyName.StartsWith("Podium"))
                return 65280;
            else if (storyName == "EDeck")
                return 16776960;
            else if (storyName.StartsWith("Story"))
                return 16711680;
            else
                return -1;
        }

        public void DefineStoriesWithVariableHeights(List<double> storyHeights)
        {
            sapModel.SetModelIsLocked(false);

            int numStories = storyHeights.Count;
            double baseElev = 0.0;

            string[] names = new string[numStories];
            double[] elevs = new double[numStories];
            bool[] master = new bool[numStories];
            string[] similar = new string[numStories];
            bool[] splice = new bool[numStories];
            double[] spliceHt = new double[numStories];
            int[] colors = new int[numStories];

            for (int i = 0; i < numStories; i++)
            {
                names[i] = $"Story{i + 1}";
                elevs[i] = storyHeights[i];
                master[i] = (i == 0);
                similar[i] = (i == 0) ? "" : "Story1";
                splice[i] = false;
                spliceHt[i] = 0.0;
                colors[i] = -1;
            }

            int ret = sapModel.Story.SetStories_2(
                baseElev, numStories, ref names, ref elevs,
                ref master, ref similar, ref splice, ref spliceHt, ref colors
            );

            if (ret != 0)
            {
                throw new Exception($"Failed to define stories. Error code: {ret}");
            }

            VerifyStories();
            sapModel.View.RefreshView(0, true);
        }

        public void DefineStories(int numStories, double storyHeight)
        {
            sapModel.SetModelIsLocked(false);

            double baseElev = 0.0;

            string[] names = new string[numStories];
            double[] elevs = new double[numStories];
            bool[] master = new bool[numStories];
            string[] similar = new string[numStories];
            bool[] splice = new bool[numStories];
            double[] spliceHt = new double[numStories];
            int[] colors = new int[numStories];

            for (int i = 0; i < numStories; i++)
            {
                names[i] = $"Story{i + 1}";
                elevs[i] = storyHeight;
                master[i] = (i == 0);
                similar[i] = (i == 0) ? "" : "Story1";
                splice[i] = false;
                spliceHt[i] = 0.0;
                colors[i] = -1;
            }

            int ret = sapModel.Story.SetStories_2(
                baseElev, numStories, ref names, ref elevs,
                ref master, ref similar, ref splice, ref spliceHt, ref colors
            );

            if (ret != 0)
            {
                throw new Exception($"Failed to define stories. Error code: {ret}");
            }

            VerifyStories();
            sapModel.View.RefreshView(0, true);
        }

        private void VerifyStories()
        {
            int numExisting = 0;
            string[] existingStories = null;
            sapModel.Story.GetNameList(ref numExisting, ref existingStories);

            System.Diagnostics.Debug.WriteLine($"Total stories created: {numExisting}");

            if (existingStories != null)
            {
                foreach (string story in existingStories)
                {
                    double elev = 0;
                    sapModel.Story.GetElevation(story, ref elev);
                    System.Diagnostics.Debug.WriteLine($"  - {story} at elevation {elev} m");
                }
            }
        }

        public string GetStoryName(int story)
        {
            return story == 0 ? "Base" : $"Story{story}";
        }

        public double GetStoryElevation(int story, double storyHeight)
        {
            return story * storyHeight;
        }

        public double GetStoryElevationVariable(List<double> storyHeights, int storyIndex)
        {
            if (storyIndex == 0) return 0.0;

            double elevation = 0.0;
            for (int i = 0; i < storyIndex && i < storyHeights.Count; i++)
            {
                elevation += storyHeights[i];
            }

            return elevation;
        }
    }
}