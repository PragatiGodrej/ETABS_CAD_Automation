

// ============================================================================
// FILE: Importers/CADImporterEnhanced.cs (UPDATED)
// ============================================================================
using ETABSv1;
using ETABS_CAD_Automation.Core;
using ETABS_CAD_Automation.Models;
using netDxf;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace ETABS_CAD_Automation.Importers
{
    public class CADImporterEnhanced
    {
        private cSapModel sapModel;
        private MaterialManager materialManager;
        private StoryManager storyManager;

        public CADImporterEnhanced(cSapModel model)
        {
            sapModel = model;
            materialManager = new MaterialManager(model);
            storyManager = new StoryManager(model);
        }


        public bool ImportMultiFloorTypeCAD(
            List<FloorTypeConfig> floorConfigs,
            List<double> storyHeights,
            List<string> storyNames,
            string seismicZone,
            Dictionary<string, int> beamDepths,
            Dictionary<string, int> slabThicknesses)  // ADD THIS PARAMETER
        {
            try
            {
                sapModel.SetModelIsLocked(false);

                // Calculate total typical floors for wall thickness calculation
                int totalTypicalFloors = CalculateTotalTypicalFloors(floorConfigs);

                // Show design notes
                ShowDesignNotes(totalTypicalFloors, seismicZone, beamDepths, slabThicknesses); // ADD slabThicknesses

                // STEP 1: Define materials
                materialManager.DefineMaterials();

                // STEP 2: Define all stories with custom names
                storyManager.DefineStoriesWithCustomNames(storyHeights, storyNames);

                // STEP 3: Load wall sections from template
                WallThicknessCalculator.LoadAvailableWallSections(sapModel);

                // STEP 4: Import each floor type
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

                    // Create enhanced importers
                    BeamImporterEnhanced beamImporter = new BeamImporterEnhanced(
                        sapModel,
                        dxfDoc,
                        seismicZone,
                        totalTypicalFloors,
                        beamDepths);

                    WallImporterEnhanced wallImporter = new WallImporterEnhanced(
                        sapModel,
                        dxfDoc,
                        floorConfig.Height,
                        totalTypicalFloors,
                        seismicZone);

                    SlabImporterEnhanced slabImporter = new SlabImporterEnhanced(
                        sapModel,
                        dxfDoc,
                        slabThicknesses); // PASS SLAB THICKNESSES

                    //Import this floor type for all its instances
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

                    System.Diagnostics.Debug.WriteLine($"\n{floorConfig.Name} Statistics:");
                    System.Diagnostics.Debug.WriteLine(wallImporter.GetImportStatistics());

                    sapModel.View.RefreshView(0, false);
                }

                sapModel.View.RefreshView(0, true);

                MessageBox.Show(
                    $"✅ Import completed successfully!\n\n" +
                    $"Building Structure:\n" +
                    BuildImportSummary(floorConfigs, storyHeights.Count, currentElevation,
                        totalTypicalFloors, seismicZone, beamDepths, slabThicknesses),
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

        private int CalculateTotalTypicalFloors(List<FloorTypeConfig> configs)
        {
            foreach (var config in configs)
            {
                if (config.Name == "Typical")
                    return config.Count;
            }

            int total = 0;
            foreach (var config in configs)
            {
                total += config.Count;
            }
            return total;
        }

        private void ShowDesignNotes(int totalTypicalFloors, string seismicZone, Dictionary<string, int> beamDepths, Dictionary<string, int> slabThicknesses)
        {
            string notes = WallThicknessCalculator.GetDesignNotes(totalTypicalFloors, seismicZone);

            notes += "\n\nWall Thickness Preview:\n";
            notes += GenerateThicknessTable(totalTypicalFloors, seismicZone);

            notes += "\n\nBeam Configuration:\n";
            notes += GenerateBeamConfigTable(seismicZone, beamDepths, totalTypicalFloors);


            notes += "\n\nSlab Configuration:\n";
            notes += GenerateSlabConfigTable(slabThicknesses); // ADD THIS

            var result = MessageBox.Show(
                notes + "\n\nProceed with these design parameters?",
                "Design Standards - TDD/PKO Guidelines",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);

            if (result != DialogResult.Yes)
            {
                throw new Exception("Import cancelled by user");
            }
        }

        private string GenerateSlabConfigTable(Dictionary<string, int> slabThicknesses)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("\n┌────────────────────────────┬─────────────────┐");
            sb.AppendLine("│ Slab Type                  │ Thickness       │");
            sb.AppendLine("├────────────────────────────┼─────────────────┤");
            sb.AppendLine($"│ Lobby                      │ {slabThicknesses["Lobby"]}mm           │");
            sb.AppendLine($"│ Stair                      │ {slabThicknesses["Stair"]}mm           │");
            sb.AppendLine($"│ Regular (area-based)       │ 125-250mm       │");
            sb.AppendLine($"│ Cantilever (span-based)    │ 125-200mm       │");
            sb.AppendLine("└────────────────────────────┴─────────────────┘");

            return sb.ToString();
        }
        private string GenerateBeamConfigTable(string zone, Dictionary<string, int> beamDepths, int floors)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("\n┌────────────────────────────┬─────────────────┐");
            sb.AppendLine("│ Beam Type                  │ Section         │");
            sb.AppendLine("├────────────────────────────┼─────────────────┤");

            int gravityWidth = (zone == "Zone II" || zone == "Zone III") ? 200 : 240;

            sb.AppendLine($"│ Internal Gravity           │ {gravityWidth}x{beamDepths["InternalGravity"]}mm      │");
            sb.AppendLine($"│ Cantilever Gravity         │ {gravityWidth}x{beamDepths["CantileverGravity"]}mm      │");

            // Get wall widths for main beams
            int coreWidth = WallThicknessCalculator.GetRecommendedThickness(
                floors, WallThicknessCalculator.WallType.CoreWall, zone, 2.0, false);
            int periDeadWidth = WallThicknessCalculator.GetRecommendedThickness(
                floors, WallThicknessCalculator.WallType.PeripheralDeadWall, zone, 2.0, false);
            int periPortalWidth = WallThicknessCalculator.GetRecommendedThickness(
                floors, WallThicknessCalculator.WallType.PeripheralPortalWall, zone, 2.0, false);
            int internalWidth = WallThicknessCalculator.GetRecommendedThickness(
                floors, WallThicknessCalculator.WallType.InternalWall, zone, 2.0, false);

            sb.AppendLine($"│ Core Main                  │ {coreWidth}x{beamDepths["CoreMain"]}mm      │");
            sb.AppendLine($"│ Peripheral Dead Main       │ {periDeadWidth}x{beamDepths["PeripheralDeadMain"]}mm      │");
            sb.AppendLine($"│ Peripheral Portal Main     │ {periPortalWidth}x{beamDepths["PeripheralPortalMain"]}mm      │");
            sb.AppendLine($"│ Internal Main              │ {internalWidth}x{beamDepths["InternalMain"]}mm      │");

            sb.AppendLine("└────────────────────────────┴─────────────────┘");

            return sb.ToString();
        }

      

        private string GenerateThicknessTable(int floors, string zone)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("\n┌─────────────────────────┬────────────┐");
            sb.AppendLine("│ Wall Type               │ Thickness  │");
            sb.AppendLine("├─────────────────────────┼────────────┤");

            var wallTypes = new[] {
                WallThicknessCalculator.WallType.CoreWall,
                WallThicknessCalculator.WallType.PeripheralDeadWall,
                WallThicknessCalculator.WallType.PeripheralPortalWall,
                WallThicknessCalculator.WallType.InternalWall
            };

            foreach (var wallType in wallTypes)
            {
                int thickness = WallThicknessCalculator.GetRecommendedThickness(
                    floors, wallType, zone, 2.0, false);

                int thicknessShort = WallThicknessCalculator.GetRecommendedThickness(
                    floors, wallType, zone, 1.5, false);

                string thicknessStr = thickness == thicknessShort
                    ? $"{thickness}mm"
                    : $"{thickness}/{thicknessShort}mm";

                sb.AppendLine($"│ {wallType,-23} │ {thicknessStr,10} │");
            }

            sb.AppendLine("└─────────────────────────┴────────────┘");
            sb.AppendLine("\nNote: Format is Normal/Short walls (<1.8m)");

            return sb.ToString();
        }
    


        private string BuildImportSummary(List<FloorTypeConfig> configs, int totalStories,
            double totalHeight, int typicalFloors, string seismicZone, Dictionary<string, int> beamDepths, Dictionary<string, int > slabThicknesses)
        {
            StringBuilder summary = new StringBuilder();

            foreach (var config in configs)
            {
                summary.AppendLine($"- {config.Name}: {config.Count} floor(s) × {config.Height:F2}m");
            }

            summary.AppendLine($"\nTotal Stories: {totalStories}");
            summary.AppendLine($"Total Height: {totalHeight:F2}m");
            summary.AppendLine($"Typical Floors: {typicalFloors}");
            summary.AppendLine($"Seismic Zone: {seismicZone}");

            int gravityWidth = (seismicZone == "Zone II" || seismicZone == "Zone III") ? 200 : 240;
            summary.AppendLine($"\n✓ Gravity beams: {gravityWidth}mm width (zone-based)");
            summary.AppendLine($"✓ Main beams: match wall thickness");
            summary.AppendLine($"✓ Slabs: Lobby {slabThicknesses["Lobby"]}mm, Stair {slabThicknesses["Stair"]}mm");
            summary.AppendLine($"✓ Wall thickness per TDD/PKO standards");
            summary.AppendLine($"✓ Auto-classified wall & beam types");

            return summary.ToString();
        }
    }
}