

// ============================================================================
// FILE: Importers/CADImporterEnhanced.cs (FINAL CORRECTED - UNIT CONTEXT FIX)
// ============================================================================
using ETABS_CAD_Automation.Core;
using ETABS_CAD_Automation.Models;
using ETABSv1;
using netDxf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
    Dictionary<string, int> slabThicknesses,
    List<string> wallGrades,        // ⭐ NEW
    List<int> floorsPerGrade)       // ⭐ NEW
        {
            try
            {
                sapModel.SetModelIsLocked(false);

                // ═══════════════════════════════════════════════════════════════
                // ⭐ CRITICAL FIX: SET UNIT CONTEXT FIRST - BEFORE ANY OPERATIONS
                // ═══════════════════════════════════════════════════════════════
                eUnits previousUnits = sapModel.GetPresentUnits();

                System.Diagnostics.Debug.WriteLine("\n╔════════════════════════════════════════════════════════╗");
                System.Diagnostics.Debug.WriteLine("║     ETABS UNIT CONTEXT INITIALIZATION                  ║");
                System.Diagnostics.Debug.WriteLine("╚════════════════════════════════════════════════════════╝");
                System.Diagnostics.Debug.WriteLine($"Previous ETABS units: {previousUnits}");
                System.Diagnostics.Debug.WriteLine("Setting units to: N_m_C (Newton, meter, Celsius)");

                sapModel.SetPresentUnits(eUnits.N_m_C);

                eUnits currentUnits = sapModel.GetPresentUnits();
                System.Diagnostics.Debug.WriteLine($"Confirmed ETABS units: {currentUnits}");

                if (currentUnits != eUnits.N_m_C)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ WARNING: Failed to set units to N_m_C!");
                    MessageBox.Show(
                        "Warning: Failed to set ETABS to meter units.\n" +
                        "Story heights may be misinterpreted.",
                        "Unit System Warning",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("✓ Unit context established: All operations will use METERS");
                }
                System.Diagnostics.Debug.WriteLine("════════════════════════════════════════════════════════\n");

                // ═══════════════════════════════════════════════════════════════
                // ⭐ NEW: CREATE GRADE SCHEDULE MANAGER
                // ═══════════════════════════════════════════════════════════════
                GradeScheduleManager gradeSchedule = new GradeScheduleManager(
                    wallGrades,
                    floorsPerGrade);

                // Validate grade schedule matches total floors
                int totalStories = storyHeights.Count;
                if (!gradeSchedule.ValidateTotalFloors(totalStories))
                {
                    MessageBox.Show(
                        $"Grade schedule floor count doesn't match total stories!\n\n" +
                        $"Expected: {totalStories}\n" +
                        $"Got: {floorsPerGrade.Sum()}\n\n" +
                        $"Please check your grade configuration.",
                        "Configuration Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return false;
                }

                // Display grade schedule summary
                System.Diagnostics.Debug.WriteLine("\n" + gradeSchedule.GetScheduleSummary());

                // Calculate total typical floors for wall thickness calculation
                int totalTypicalFloors = CalculateTotalTypicalFloors(floorConfigs);

                // Show design notes (with grade schedule)
                ShowDesignNotes(totalTypicalFloors, seismicZone, beamDepths, slabThicknesses, gradeSchedule);

                // STEP 1: Define materials (now in meter context)
                System.Diagnostics.Debug.WriteLine("Step 1: Defining materials (in meter context)...");
                materialManager.DefineMaterials();

                // STEP 2: Define all stories with custom names (now in meter context)
                System.Diagnostics.Debug.WriteLine("Step 2: Defining stories (in meter context)...");
                storyManager.DefineStoriesWithCustomNames(storyHeights, storyNames);

                // STEP 3: Load wall sections from template (now in meter context)
                System.Diagnostics.Debug.WriteLine("Step 3: Loading wall sections from template...");
                WallThicknessCalculator.LoadAvailableWallSections(sapModel);

                // STEP 4: Import each floor type
                System.Diagnostics.Debug.WriteLine("Step 4: Importing floor types...\n");

                int currentStoryIndex = 0;

                foreach (var floorConfig in floorConfigs)
                {
                    MessageBox.Show(
                        $"Importing {floorConfig.Name}...\n\n" +
                        $"Floors: {floorConfig.Count}\n" +
                        $"Height: {floorConfig.Height}m\n" +
                        $"CAD: {System.IO.Path.GetFileName(floorConfig.CADFilePath)}\n\n" +
                        $"Unit Context: METERS (N_m_C)\n" +
                        $"All coordinates and elevations are in meters.",
                        "Import Progress");

                    // Load CAD file for this floor type
                    DxfDocument dxfDoc = DxfDocument.Load(floorConfig.CADFilePath);

                    if (dxfDoc == null)
                    {
                        MessageBox.Show($"Failed to load CAD file for {floorConfig.Name}", "Error");
                        return false;
                    }

                    // ⭐ CORRECTED: Pass gradeSchedule to ALL importers
                    BeamImporterEnhanced beamImporter = new BeamImporterEnhanced(
                        sapModel,
                        dxfDoc,
                        seismicZone,
                        totalTypicalFloors,
                        beamDepths,
                        gradeSchedule);  // ✅ NOW PASSED!

                    WallImporterEnhanced wallImporter = new WallImporterEnhanced(
                        sapModel,
                        dxfDoc,
                        floorConfig.Height,
                        totalTypicalFloors,
                        seismicZone,
                        gradeSchedule);// ✅ NOW PASSED!
                       

                    SlabImporterEnhanced slabImporter = new SlabImporterEnhanced(
                        sapModel,
                        dxfDoc,
                        slabThicknesses,
                        gradeSchedule);  // ✅ NOW PASSED!

                    // Import this floor type for all its instances
                    for (int floor = 0; floor < floorConfig.Count; floor++)
                    {
                        // Get elevations from StoryManager (in METERS)
                        double baseElevation = storyManager.GetStoryBaseElevation(currentStoryIndex);
                        double topElevation = storyManager.GetStoryTopElevation(currentStoryIndex);

                        // Get concrete grades for this story
                        string wallGrade = gradeSchedule.GetWallGradeForStory(currentStoryIndex);
                        string beamSlabGrade = gradeSchedule.GetBeamSlabGradeForStory(currentStoryIndex);

                        System.Diagnostics.Debug.WriteLine(
                            $"\n>>> Importing {storyNames[currentStoryIndex]} | " +
                            $"Base: {baseElevation:F3}m | Top: {topElevation:F3}m | " +
                            $"Height: {floorConfig.Height:F3}m");
                        System.Diagnostics.Debug.WriteLine(
                            $"    Concrete Grades: Wall={wallGrade}, Beam/Slab={beamSlabGrade}");
                        System.Diagnostics.Debug.WriteLine(
                            $"    Unit Context: METERS (all values interpreted as meters by ETABS)");

                        // Import walls at BASE of story (elevation in meters)
                        wallImporter.ImportWalls(
                            floorConfig.LayerMapping,
                            baseElevation,
                            currentStoryIndex);

                        // Import beams and slabs at TOP of story (elevation in meters)
                        beamImporter.ImportBeams(
                            floorConfig.LayerMapping,
                            topElevation,
                            currentStoryIndex);

                        slabImporter.ImportSlabs(
                            floorConfig.LayerMapping,
                            topElevation,
                            currentStoryIndex);

                        currentStoryIndex++;
                    }

                    System.Diagnostics.Debug.WriteLine($"\n{floorConfig.Name} Statistics:");
                    System.Diagnostics.Debug.WriteLine(wallImporter.GetImportStatistics());

                    sapModel.View.RefreshView(0, false);
                }

                sapModel.View.RefreshView(0, true);

                // Final verification
                System.Diagnostics.Debug.WriteLine("\n╔════════════════════════════════════════════════════════╗");
                System.Diagnostics.Debug.WriteLine("║     FINAL VERIFICATION                                 ║");
                System.Diagnostics.Debug.WriteLine("╚════════════════════════════════════════════════════════╝");

                eUnits finalUnits = sapModel.GetPresentUnits();
                System.Diagnostics.Debug.WriteLine($"Final ETABS units: {finalUnits}");
                System.Diagnostics.Debug.WriteLine($"Total building height: {storyManager.GetTotalBuildingHeight():F3}m");
                System.Diagnostics.Debug.WriteLine("════════════════════════════════════════════════════════\n");

                MessageBox.Show(
                    $"✅ Import completed successfully!\n\n" +
                    $"Building Structure:\n" +
                    BuildImportSummary(floorConfigs, storyHeights.Count,
                        storyManager.GetTotalBuildingHeight(),
                        totalTypicalFloors, seismicZone, beamDepths, slabThicknesses) + "\n\n" +
                    gradeSchedule.GetScheduleSummary() + "\n" +
                    $"Unit System: N_m_C (meters)\n" +
                    $"All dimensions are in meters.",
                    "Import Success");

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Import failed:\n" + ex.Message + "\n\n" + ex.StackTrace,
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

        //private void ShowDesignNotes(int totalTypicalFloors, string seismicZone,
        //    Dictionary<string, int> beamDepths, Dictionary<string, int> slabThicknesses)
        //{
        //    string notes = WallThicknessCalculator.GetDesignNotes(totalTypicalFloors, seismicZone);

        //    notes += "\n\nWall Thickness Preview:\n";
        //    notes += GenerateThicknessTable(totalTypicalFloors, seismicZone);

        //    notes += "\n\nBeam Configuration:\n";
        //    notes += GenerateBeamConfigTable(seismicZone, beamDepths, totalTypicalFloors);

        //    notes += "\n\nSlab Configuration:\n";
        //    notes += GenerateSlabConfigTable(slabThicknesses);

        //    notes += "\n\n⚠️ UNIT SYSTEM:\n";
        //    notes += "ETABS will be set to N_m_C (meters)\n";
        //    notes += "All dimensions (X, Y, Z) will be in meters\n";
        //    notes += "Story heights: As configured in UI (meters)\n";
        //    notes += "CAD coordinates: Converted to meters via X_TO_M, Y_TO_M";

        //    var result = MessageBox.Show(
        //        notes + "\n\nProceed with these design parameters?",
        //        "Design Standards - TDD/PKO Guidelines",
        //        MessageBoxButtons.YesNo,
        //        MessageBoxIcon.Information);

        //    if (result != DialogResult.Yes)
        //    {
        //        throw new Exception("Import cancelled by user");
        //    }
        //}
        private void ShowDesignNotes(
    int totalTypicalFloors,
    string seismicZone,
    Dictionary<string, int> beamDepths,
    Dictionary<string, int> slabThicknesses,
    GradeScheduleManager gradeSchedule)  // ⭐ ADD PARAMETER
        {
            string notes = WallThicknessCalculator.GetDesignNotes(totalTypicalFloors, seismicZone);

            // ⭐ ADD GRADE SCHEDULE TO NOTES
            notes += "\n\n═══════════════════════════════════════";
            notes += "\n" + gradeSchedule.GetScheduleSummary();
            notes += "═══════════════════════════════════════";

            notes += "\n\nWall Thickness Preview:\n";
            notes += GenerateThicknessTable(totalTypicalFloors, seismicZone);

            notes += "\n\nBeam Configuration:\n";
            notes += GenerateBeamConfigTable(seismicZone, beamDepths, totalTypicalFloors);

            notes += "\n\nSlab Configuration:\n";
            notes += GenerateSlabConfigTable(slabThicknesses);

            notes += "\n\n⚠️ UNIT SYSTEM:\n";
            notes += "ETABS will be set to N_m_C (meters)\n";
            notes += "All dimensions (X, Y, Z) will be in meters\n";
            notes += "Story heights: As configured in UI (meters)\n";
            notes += "CAD coordinates: Converted to meters via X_TO_M, Y_TO_M";

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
            double totalHeight, int typicalFloors, string seismicZone,
            Dictionary<string, int> beamDepths, Dictionary<string, int> slabThicknesses)
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
