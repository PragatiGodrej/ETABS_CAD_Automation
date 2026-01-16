// ============================================================================
// FILE: Importers/CADImporter.cs
// ============================================================================
using ETABSv1;
using ETABS_CAD_Automation.Core;
using ETABS_CAD_Automation.Models;
using netDxf;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace ETABS_CAD_Automation.Importers
{
    public class CADImporter
    {
        private cSapModel sapModel;
        private MaterialManager materialManager;
        private StoryManager storyManager;


        public CADImporter(cSapModel model)
        {
            sapModel = model;
            materialManager = new MaterialManager(model);
            storyManager = new StoryManager(model);
        }

        public bool ImportMultiFloorTypeCAD(
            List<FloorTypeConfig> floorConfigs,
            List<double> storyHeights,
            List<string> storyNames,
            string seismicZone)
        {
            try
            {
                sapModel.SetModelIsLocked(false);

                // STEP 1: Define materials
                materialManager.DefineMaterials();

                // STEP 2: Define all stories with custom names
                storyManager.DefineStoriesWithCustomNames(storyHeights, storyNames);

                // STEP 3: Import each floor type
                int currentStoryIndex = 0;
                double currentElevation = 0;

                foreach (var floorConfig in floorConfigs)
                {
                    MessageBox.Show(
                        $"Importing {floorConfig.Name}...\n\n" +
                        $"Floors: {floorConfig.Count}\n" +
                        $"Height: {floorConfig.Height}m\n" +
                        $"CAD: {System.IO.Path.GetFileName(floorConfig.CADFilePath)}",
                        "Import Progress");

                    // Load CAD file for this floor type
                    DxfDocument dxfDoc = DxfDocument.Load(floorConfig.CADFilePath);

                    if (dxfDoc == null)
                    {
                        MessageBox.Show($"Failed to load CAD file for {floorConfig.Name}", "Error");
                        return false;
                    }

                    // Create importers for this CAD file
                    BeamImporter beamImporter = new BeamImporter(sapModel, dxfDoc);
                    WallImporter wallImporter = new WallImporter(sapModel, dxfDoc, floorConfig.Height);
                    SlabImporter slabImporter = new SlabImporter(sapModel, dxfDoc);

                    // Define sections
                    wallImporter.DefineSections();
                    beamImporter.DefineSections();
                    slabImporter.DefineSections();

                    // Import this floor type for all its instances
                    for (int floor = 0; floor < floorConfig.Count; floor++)
                    {
                        string currentStoryName = storyNames[currentStoryIndex];

                        // Import walls (from base of story)
                        wallImporter.ImportWalls(
                            floorConfig.LayerMapping,
                            currentElevation,
                            currentStoryIndex);

                        // Import beams and slabs (at top of story)
                        double topElevation = currentElevation + floorConfig.Height;

                        beamImporter.ImportBeams(
                            floorConfig.LayerMapping,
                            topElevation,
                            currentStoryIndex + 1);

                        slabImporter.ImportSlabs(
                            floorConfig.LayerMapping,
                            topElevation,
                            currentStoryIndex + 1);

                        // Move to next story
                        currentElevation += floorConfig.Height;
                        currentStoryIndex++;
                    }

                    sapModel.View.RefreshView(0, false);
                }

                sapModel.View.RefreshView(0, true);

                MessageBox.Show(
                    $"✅ Import completed successfully!\n\n" +
                    $"Building Structure:\n" +
                    BuildImportSummary(floorConfigs, storyHeights.Count, currentElevation),
                    "Import Success");

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Import failed:\n" +
                    ex.Message + "\n\n" +
                    ex.StackTrace,
                    "Error");
                return false;
            }
        }

        private string BuildImportSummary(List<FloorTypeConfig> configs, int totalStories, double totalHeight)
        {
            string summary = "";
            foreach (var config in configs)
            {
                summary += $"- {config.Name}: {config.Count} floor(s) × {config.Height:F2}m\n";
            }
            summary += $"\nTotal Stories: {totalStories}\n";
            summary += $"Total Height: {totalHeight:F2}m";
            return summary;
        }



        // Helper classes
        private class FrameData
        {
            public double X1, Y1, Z1, X2, Y2, Z2;
            public string Section;
        }

        private class AreaData
        {
            public double[] XCoords, YCoords, ZCoords;
            public string Property;
        }
    }
}
