// ============================================================================
// FILE: UI/MainForm.cs
// ============================================================================
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using ETABS_CAD_Automation.Core;
using ETABS_CAD_Automation.Importers;
using ETABS_CAD_Automation.Models;

namespace ETABS_CAD_Automation
{
    public partial class MainForm : Form
    {
        private ETABSController etabs;

        public MainForm()
        {
            InitializeComponent();
            etabs = new ETABSController();
        }

        private void btnStartETABS_Click(object sender, EventArgs e)
        {
            try
            {
                if (etabs.Connect())
                {
                    MessageBox.Show(
                        "ETABS Connected Successfully!\n\nYou can now import CAD files.",
                        "Success",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show(
                        "ETABS Connection Failed.\n\nPlease ensure:\n" +
                        "1. ETABS is installed\n" +
                        "2. ETABS is running\n" +
                        "3. You have proper permissions",
                        "Connection Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error connecting to ETABS:\n{ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void btnImportCAD_Click(object sender, EventArgs e)
        {
            if (etabs.SapModel == null)
            {
                MessageBox.Show(
                    "Please connect to ETABS first.",
                    "Not Connected",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (ImportConfigForm importForm = new ImportConfigForm())
                {
                    if (importForm.ShowDialog() == DialogResult.OK)
                    {
                        var floorConfigs = importForm.FloorConfigs;
                        string seismicZone = importForm.SeismicZone;

                        // Calculate total stories and heights
                        int totalStories = 0;
                        List<double> storyHeights = new List<double>();
                        List<string> storyNames = new List<string>();

                        int storyNumber = 1;

                        foreach (var config in floorConfigs)
                        {
                            for (int i = 0; i < config.Count; i++)
                            {
                                storyHeights.Add(config.Height);

                                string storyName = "";
                                if (config.Name == "Basement")
                                    storyName = $"Basement{i + 1}";
                                else if (config.Name == "Podium")
                                    storyName = $"Podium{i + 1}";
                                else if (config.Name == "EDeck")
                                    storyName = "EDeck";
                                else if (config.Name == "Typical")
                                    storyName = $"Story{i + 1}";

                                storyNames.Add(storyName);
                                totalStories++;
                                storyNumber++;
                            }
                        }

                        double totalHeight = CalculateTotalHeight(storyHeights);

                        // Build confirmation message
                        string heightBreakdown = BuildHeightBreakdown(storyHeights, storyNames);

                        var result = MessageBox.Show(
                            $"Final Import Configuration:\n\n" +
                            $"Total Stories: {totalStories}\n" +
                            $"Total Building Height: {totalHeight:F2}m\n" +
                            $"Seismic Zone: {seismicZone}\n\n" +
                            heightBreakdown + "\n" +
                            "Proceed with import?",
                            "⚠️ Final Confirmation",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question);

                        if (result != DialogResult.Yes)
                        {
                            MessageBox.Show("Import cancelled.", "Cancelled");
                            return;
                        }

                        // Import with multi-floor-type configuration
                        CADImporter importer = new CADImporter(etabs.SapModel);
                        bool success = importer.ImportMultiFloorTypeCAD(
                            floorConfigs,
                            storyHeights,
                            storyNames,
                            seismicZone);

                        if (success)
                        {
                            MessageBox.Show(
                                "✅ Import completed successfully!\n\n" +
                                "Building Structure Created:\n" +
                                $"- Total Stories: {totalStories}\n" +
                                $"- Building Height: {totalHeight:F2}m\n" +
                                $"- Seismic Zone: {seismicZone}\n\n" +
                                "View Your Building:\n" +
                                "1. Check bottom of ETABS window\n" +
                                "2. Use story dropdown to navigate floors\n" +
                                "3. Each floor type has its own unique layout",
                                "Import Success!",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                        }
                        else
                        {
                            MessageBox.Show(
                                "Import completed but some elements may not have been created.\n" +
                                "Please review the ETABS model.",
                                "Warning",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error during import:\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}",
                    "Import Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private string BuildHeightBreakdown(List<double> storyHeights, List<string> storyNames)
        {
            string breakdown = "Story Height Breakdown:\n";
            double cumulativeHeight = 0;

            for (int i = 0; i < storyHeights.Count; i++)
            {
                cumulativeHeight += storyHeights[i];
                breakdown += $"{storyNames[i]}: {storyHeights[i]:F2}m (Elevation: {cumulativeHeight:F2}m)\n";
            }

            return breakdown;
        }

        private double CalculateTotalHeight(List<double> storyHeights)
        {
            double total = 0;
            foreach (double height in storyHeights)
            {
                total += height;
            }
            return total;
        }
    }
}
