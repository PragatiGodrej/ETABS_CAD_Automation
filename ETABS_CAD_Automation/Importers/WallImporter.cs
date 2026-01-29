
// ============================================================================
// FILE: Importers/WallImporterEnhanced.cs (FIXED - SEPARATE X/Y CONVERSION)
// ============================================================================
using ETABSv1;
using netDxf;
using netDxf.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using ETABS_CAD_Automation.Core;

namespace ETABS_CAD_Automation.Importers
{
    public class WallImporterEnhanced
    {
        private cSapModel sapModel;
        private DxfDocument dxfDoc;
        private double floorHeight;
        private int totalTypicalFloors;
        private string seismicZone;

        // CRITICAL FIX: Separate conversion factors for X and Y
        // If X shows 1850m instead of 18.5m (100x too large), use 0.0001
        // If X shows 185m instead of 18.5m (10x too large), use 0.0001
        // Adjust based on your actual measurements
        private const double X_TO_M = 0.001;  // ADJUST THIS based on diagnostics
        private const double Y_TO_M = 0.001;   // Confirmed correct by user

        private double MX(double xValue) => xValue * X_TO_M;
        private double MY(double yValue) => yValue * Y_TO_M;

        private int wallsCreated = 0;
        private int wallsFailed = 0;
        private Dictionary<string, int> wallTypeCount = new Dictionary<string, int>();

        public WallImporterEnhanced(cSapModel model, DxfDocument doc, double height, int typicalFloors, string zone)
        {
            sapModel = model;
            dxfDoc = doc;
            floorHeight = height;
            totalTypicalFloors = typicalFloors;
            seismicZone = zone;

            // Diagnose coordinate system
            DiagnoseCoordinateSystem();

            // Load available wall sections from template
            WallThicknessCalculator.LoadAvailableWallSections(sapModel);
        }

        /// <summary>
        /// Diagnose what's happening with X and Y coordinates
        /// </summary>
        private void DiagnoseCoordinateSystem()
        {
            System.Diagnostics.Debug.WriteLine("\n========== COORDINATE SYSTEM DIAGNOSTICS ==========");

            try
            {
                // Get first line from DXF
                var testLine = dxfDoc.Entities.Lines.FirstOrDefault();
                if (testLine != null)
                {
                    System.Diagnostics.Debug.WriteLine($"\nDXF Raw Coordinates:");
                    System.Diagnostics.Debug.WriteLine($"  Start X: {testLine.StartPoint.X}, Y: {testLine.StartPoint.Y}");
                    System.Diagnostics.Debug.WriteLine($"  End   X: {testLine.EndPoint.X}, Y: {testLine.EndPoint.Y}");

                    double rawLengthX = Math.Abs(testLine.EndPoint.X - testLine.StartPoint.X);
                    double rawLengthY = Math.Abs(testLine.EndPoint.Y - testLine.StartPoint.Y);

                    System.Diagnostics.Debug.WriteLine($"\nRaw Lengths:");
                    System.Diagnostics.Debug.WriteLine($"  X span: {rawLengthX}");
                    System.Diagnostics.Debug.WriteLine($"  Y span: {rawLengthY}");

                    System.Diagnostics.Debug.WriteLine($"\nAfter conversion with X_TO_M={X_TO_M}, Y_TO_M={Y_TO_M}:");
                    System.Diagnostics.Debug.WriteLine($"  Start: ({MX(testLine.StartPoint.X):F3}m, {MY(testLine.StartPoint.Y):F3}m)");
                    System.Diagnostics.Debug.WriteLine($"  End:   ({MX(testLine.EndPoint.X):F3}m, {MY(testLine.EndPoint.Y):F3}m)");
                    System.Diagnostics.Debug.WriteLine($"  X span: {rawLengthX * X_TO_M:F3}m");
                    System.Diagnostics.Debug.WriteLine($"  Y span: {rawLengthY * Y_TO_M:F3}m");

                    // CREATE A TEST POINT IN ETABS AND READ IT BACK
                    System.Diagnostics.Debug.WriteLine($"\n=== ETABS VERIFICATION TEST ===");

                    string testPoint1 = "";

                    double testX = MX(testLine.StartPoint.X);
                    double testY = MY(testLine.StartPoint.Y);
                    double testZ = 0;

                    System.Diagnostics.Debug.WriteLine($"\nSending to ETABS:");
                    System.Diagnostics.Debug.WriteLine($"  X={testX:F6}m, Y={testY:F6}m, Z={testZ:F6}m");

                    // Create test point
                    int ret1 = sapModel.PointObj.AddCartesian(testX, testY, testZ, ref testPoint1, "Global");

                    if (ret1 == 0 && !string.IsNullOrEmpty(testPoint1))
                    {
                        // Read back coordinates
                        double readX = 0, readY = 0, readZ = 0;
                        int ret2 = sapModel.PointObj.GetCoordCartesian(testPoint1, ref readX, ref readY, ref readZ);

                        if (ret2 == 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"\nETABS stored as:");
                            System.Diagnostics.Debug.WriteLine($"  X={readX:F6}m, Y={readY:F6}m, Z={readZ:F6}m");

                            // Check for discrepancies
                            double xError = Math.Abs(readX - testX);
                            double yError = Math.Abs(readY - testY);

                            if (xError > 0.001)
                            {
                                System.Diagnostics.Debug.WriteLine($"\n⚠️ X-AXIS MISMATCH DETECTED!");
                                System.Diagnostics.Debug.WriteLine($"  Sent: {testX:F6}m");
                                System.Diagnostics.Debug.WriteLine($"  Got:  {readX:F6}m");
                                System.Diagnostics.Debug.WriteLine($"  Ratio: {readX / testX:F6}");

                                if (testX != 0)
                                {
                                    double correctionFactor = testX / readX;
                                    double newXToM = X_TO_M * correctionFactor;
                                    System.Diagnostics.Debug.WriteLine($"  ** USE THIS VALUE: X_TO_M = {newXToM:F9} **");
                                }
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"\n✓ X-axis is CORRECT!");
                            }

                            if (yError > 0.001)
                            {
                                System.Diagnostics.Debug.WriteLine($"\n⚠️ Y-AXIS MISMATCH DETECTED!");
                                System.Diagnostics.Debug.WriteLine($"  Sent: {testY:F6}m");
                                System.Diagnostics.Debug.WriteLine($"  Got:  {readY:F6}m");
                                System.Diagnostics.Debug.WriteLine($"  Ratio: {readY / testY:F6}");

                                if (testY != 0)
                                {
                                    double correctionFactor = testY / readY;
                                    double newYToM = Y_TO_M * correctionFactor;
                                    System.Diagnostics.Debug.WriteLine($"  ** USE THIS VALUE: Y_TO_M = {newYToM:F9} **");
                                }
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"✓ Y-axis is CORRECT!");
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"⚠️ Failed to read back coordinates (ret={ret2})");
                        }

                        System.Diagnostics.Debug.WriteLine($"\n(Note: Test point '{testPoint1}' created - you can delete it manually if needed)");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠️ Failed to create test point (ret={ret1})");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("\n⚠️ No lines found in DXF for diagnostics");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Diagnostic error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            System.Diagnostics.Debug.WriteLine("===================================================\n");
        }

        public void DefineSections()
        {
            System.Diagnostics.Debug.WriteLine(
                "✓ Using wall sections from template - no need to define new sections");
        }

        public void ImportWalls(Dictionary<string, string> layerMapping, double elevation, int story)
        {
            wallsCreated = 0;
            wallsFailed = 0;
            wallTypeCount.Clear();

            var wallLayers = layerMapping.Where(x => x.Value == "Wall").Select(x => x.Key).ToList();
            if (wallLayers.Count == 0) return;

            System.Diagnostics.Debug.WriteLine(
                $"\n========== IMPORTING WALLS - Story {story} ==========");
            System.Diagnostics.Debug.WriteLine(
                $"Building Config: {totalTypicalFloors} typical floors, {seismicZone}");

            foreach (string layerName in wallLayers)
            {
                var wallType = WallThicknessCalculator.ClassifyWallFromLayerName(layerName);
                System.Diagnostics.Debug.WriteLine($"\nLayer: {layerName} → {wallType}");

                // Process lines
                var lines = dxfDoc.Entities.Lines.Where(l => l.Layer.Name == layerName).ToList();
                foreach (netDxf.Entities.Line line in lines)
                {
                    double wallLengthM = CalculateWallLengthInMeters(
                        line.StartPoint.X, line.StartPoint.Y,
                        line.EndPoint.X, line.EndPoint.Y);

                    if (CreateWallFromLineWithAutoThickness(line, elevation, story, wallType, wallLengthM))
                        wallsCreated++;
                    else
                        wallsFailed++;
                }

                // Process polylines
                var polylines = dxfDoc.Entities.Polylines2D.Where(p => p.Layer.Name == layerName).ToList();
                foreach (Polyline2D poly in polylines)
                {
                    int count = CreateWallFromPolylineWithAutoThickness(poly, elevation, story, wallType);
                    wallsCreated += count;
                }
            }

            System.Diagnostics.Debug.WriteLine($"\n========== WALL IMPORT SUMMARY ==========");
            System.Diagnostics.Debug.WriteLine($"✓ Created: {wallsCreated}");
            System.Diagnostics.Debug.WriteLine($"❌ Failed: {wallsFailed}");
            System.Diagnostics.Debug.WriteLine($"\nWall Sections Used:");
            foreach (var kvp in wallTypeCount.OrderBy(x => x.Key))
            {
                System.Diagnostics.Debug.WriteLine($"  {kvp.Key}: {kvp.Value} walls");
            }
            System.Diagnostics.Debug.WriteLine($"=========================================\n");
        }

        private bool CreateWallFromLineWithAutoThickness(
            netDxf.Entities.Line line,
            double elevation,
            int story,
            WallThicknessCalculator.WallType wallType,
            double wallLengthM)
        {
            try
            {
                if (wallLengthM < 0.1) return false;

                string section = WallThicknessCalculator.GetRecommendedWallSection(
                    totalTypicalFloors,
                    wallType,
                    seismicZone,
                    wallLengthM,
                    false);

                string storyName = GetStoryName(story);

                if (!wallTypeCount.ContainsKey(section))
                    wallTypeCount[section] = 0;
                wallTypeCount[section]++;

                string[] pts = new string[4];
                // Use separate X and Y conversions
                sapModel.PointObj.AddCartesian(MX(line.StartPoint.X), MY(line.StartPoint.Y), elevation, ref pts[0], "Global");
                sapModel.PointObj.AddCartesian(MX(line.EndPoint.X), MY(line.EndPoint.Y), elevation, ref pts[1], "Global");
                sapModel.PointObj.AddCartesian(MX(line.EndPoint.X), MY(line.EndPoint.Y), elevation + floorHeight, ref pts[2], "Global");
                sapModel.PointObj.AddCartesian(MX(line.StartPoint.X), MY(line.StartPoint.Y), elevation + floorHeight, ref pts[3], "Global");

                string area = "";
                int ret = sapModel.AreaObj.AddByPoint(4, ref pts, ref area, section);

                if (ret == 0 && !string.IsNullOrEmpty(area))
                {
                    sapModel.AreaObj.SetGroupAssign(area, storyName);
                    System.Diagnostics.Debug.WriteLine(
                        $"  ✓ Wall: {area} | {section} | Length: {wallLengthM:F2}m | Type: {wallType}");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"  ❌ Error: {ex.Message}");
                return false;
            }
        }

        private int CreateWallFromPolylineWithAutoThickness(
            Polyline2D poly,
            double elevation,
            int story,
            WallThicknessCalculator.WallType wallType)
        {
            try
            {
                var vertices = poly.Vertexes;
                if (vertices == null || vertices.Count < 2) return 0;

                string storyName = GetStoryName(story);
                int count = 0;

                for (int i = 0; i < vertices.Count - 1; i++)
                {
                    double wallLengthM = CalculateWallLengthInMeters(
                        vertices[i].Position.X, vertices[i].Position.Y,
                        vertices[i + 1].Position.X, vertices[i + 1].Position.Y);

                    if (CreateWallSegmentWithAutoThickness(
                        MX(vertices[i].Position.X), MY(vertices[i].Position.Y),
                        MX(vertices[i + 1].Position.X), MY(vertices[i + 1].Position.Y),
                        elevation, storyName, wallType, wallLengthM))
                    {
                        count++;
                    }
                }

                if (poly.IsClosed && vertices.Count > 2)
                {
                    double wallLengthM = CalculateWallLengthInMeters(
                        vertices[vertices.Count - 1].Position.X, vertices[vertices.Count - 1].Position.Y,
                        vertices[0].Position.X, vertices[0].Position.Y);

                    if (CreateWallSegmentWithAutoThickness(
                        MX(vertices[vertices.Count - 1].Position.X), MY(vertices[vertices.Count - 1].Position.Y),
                        MX(vertices[0].Position.X), MY(vertices[0].Position.Y),
                        elevation, storyName, wallType, wallLengthM))
                    {
                        count++;
                    }
                }

                return count;
            }
            catch
            {
                return 0;
            }
        }

        private bool CreateWallSegmentWithAutoThickness(
            double x1M, double y1M,
            double x2M, double y2M,
            double elevation,
            string storyName,
            WallThicknessCalculator.WallType wallType,
            double wallLengthM)
        {
            try
            {
                if (wallLengthM < 0.1) return false;

                string section = WallThicknessCalculator.GetRecommendedWallSection(
                    totalTypicalFloors,
                    wallType,
                    seismicZone,
                    wallLengthM,
                    false);

                if (!wallTypeCount.ContainsKey(section))
                    wallTypeCount[section] = 0;
                wallTypeCount[section]++;

                string[] pts = new string[4];
                sapModel.PointObj.AddCartesian(x1M, y1M, elevation, ref pts[0], "Global");
                sapModel.PointObj.AddCartesian(x2M, y2M, elevation, ref pts[1], "Global");
                sapModel.PointObj.AddCartesian(x2M, y2M, elevation + floorHeight, ref pts[2], "Global");
                sapModel.PointObj.AddCartesian(x1M, y1M, elevation + floorHeight, ref pts[3], "Global");

                string area = "";
                int ret = sapModel.AreaObj.AddByPoint(4, ref pts, ref area, section);

                if (ret == 0 && !string.IsNullOrEmpty(area))
                {
                    sapModel.AreaObj.SetGroupAssign(area, storyName);
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private double CalculateWallLengthInMeters(double x1, double y1, double x2, double y2)
        {
            // Use separate conversions for X and Y
            double x1M = MX(x1);
            double y1M = MY(y1);
            double x2M = MX(x2);
            double y2M = MY(y2);

            double dx = x2M - x1M;
            double dy = y2M - y1M;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private string GetStoryName(int story)
        {
            try
            {
                int numStories = 0;
                string[] storyNames = null;
                int ret = sapModel.Story.GetNameList(ref numStories, ref storyNames);

                if (ret == 0 && storyNames != null && story >= 0 && story < storyNames.Length)
                {
                    return storyNames[story];
                }
            }
            catch { }

            return story == 0 ? "Base" : $"Story{story + 1}";
        }

        public string GetImportStatistics()
        {
            string stats = $"Walls Created: {wallsCreated}, Failed: {wallsFailed}\n";
            stats += "\nWall Sections Used:\n";
            foreach (var kvp in wallTypeCount.OrderBy(x => x.Key))
            {
                stats += $"  {kvp.Key}: {kvp.Value} walls\n";
            }
            return stats;
        }

        public void ResetStatistics()
        {
            wallsCreated = 0;
            wallsFailed = 0;
            wallTypeCount.Clear();
        }
    }
}
