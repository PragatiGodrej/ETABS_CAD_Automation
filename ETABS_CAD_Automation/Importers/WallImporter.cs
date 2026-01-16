// ============================================================================
// FILE: Importers/WallImporter.cs
// ============================================================================
using ETABSv1;
using netDxf;
using netDxf.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ETABS_CAD_Automation.Importers
{
    public class WallImporter
    {
        private cSapModel sapModel;
        private DxfDocument dxfDoc;
        private double floorHeight;
        private const double MM_TO_M = 0.001;
        private double M(double mm) => mm * MM_TO_M;

        private int wallsCreated = 0;
        private int wallsFailed = 0;

        public WallImporter(cSapModel model, DxfDocument doc, double height)
        {
            sapModel = model;
            dxfDoc = doc;
            floorHeight = height;
        }

        public void DefineSections()
        {
            try
            {
                sapModel.PropArea.SetWall("WALL300", eWallPropType.Specified, eShellType.ShellThin,
                    "CONC", 0.30, 12, "CONC", "CONC");

                sapModel.PropArea.SetWall("WALL250", eWallPropType.Specified, eShellType.ShellThin,
                    "CONC", 0.25, 12, "CONC", "CONC");

                sapModel.PropArea.SetWall("WALL230", eWallPropType.Specified, eShellType.ShellThin,
                    "CONC", 0.23, 12, "CONC", "CONC");

                sapModel.PropArea.SetWall("WALL200", eWallPropType.Specified, eShellType.ShellThin,
                    "CONC", 0.20, 12, "CONC", "CONC");

                sapModel.PropArea.SetWall("WALL175", eWallPropType.Specified, eShellType.ShellThin,
                    "CONC", 0.175, 12, "CONC", "CONC");

                sapModel.PropArea.SetWall("WALL150", eWallPropType.Specified, eShellType.ShellThin,
                    "CONC", 0.15, 12, "CONC", "CONC");

                sapModel.PropArea.SetWall("WALL125", eWallPropType.Specified, eShellType.ShellThin,
                    "CONC", 0.125, 12, "CONC", "CONC");

                sapModel.PropArea.SetWall("WALL100", eWallPropType.Specified, eShellType.ShellThin,
                    "CONC", 0.10, 12, "CONC", "CONC");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error defining wall sections: {ex.Message}");
                throw;
            }
        }

        public void ImportWalls(Dictionary<string, string> layerMapping, double elevation, int story)
        {
            wallsCreated = 0;
            wallsFailed = 0;

            var wallLayers = layerMapping.Where(x => x.Value == "Wall").Select(x => x.Key).ToList();

            if (wallLayers.Count == 0)
                return;

            foreach (string layerName in wallLayers)
            {
                string section = DetermineWallSection(layerName);

                var lines = dxfDoc.Entities.Lines.Where(l => l.Layer.Name == layerName).ToList();
                foreach (netDxf.Entities.Line line in lines)
                {
                    if (CreateWallFromLine(line, elevation, section, story))
                        wallsCreated++;
                    else
                        wallsFailed++;
                }

                var polylines = dxfDoc.Entities.Polylines2D.Where(p => p.Layer.Name == layerName).ToList();
                foreach (Polyline2D poly in polylines)
                {
                    int count = CreateWallFromPolyline(poly, elevation, section, story);
                    wallsCreated += count;
                }
            }
        }

        private bool CreateWallFromLine(netDxf.Entities.Line line, double elevation,
            string section, int story)
        {
            try
            {
                double length = Math.Sqrt(
                    Math.Pow(line.EndPoint.X - line.StartPoint.X, 2) +
                    Math.Pow(line.EndPoint.Y - line.StartPoint.Y, 2));

                if (length < 100)
                    return false;

                string storyName = GetStoryName(story);
                string[] pts = new string[4];

                sapModel.PointObj.AddCartesian(M(line.StartPoint.X), M(line.StartPoint.Y),
                    elevation, ref pts[0], "Global");
                sapModel.PointObj.AddCartesian(M(line.EndPoint.X), M(line.EndPoint.Y),
                    elevation, ref pts[1], "Global");
                sapModel.PointObj.AddCartesian(M(line.EndPoint.X), M(line.EndPoint.Y),
                    elevation + floorHeight, ref pts[2], "Global");
                sapModel.PointObj.AddCartesian(M(line.StartPoint.X), M(line.StartPoint.Y),
                    elevation + floorHeight, ref pts[3], "Global");

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

        private int CreateWallFromPolyline(Polyline2D poly, double elevation,
            string section, int story)
        {
            try
            {
                var vertices = poly.Vertexes;
                if (vertices == null || vertices.Count < 2)
                    return 0;

                string storyName = GetStoryName(story);
                int count = 0;

                for (int i = 0; i < vertices.Count - 1; i++)
                {
                    if (CreateWallSegment(
                        M(vertices[i].Position.X), M(vertices[i].Position.Y),
                        M(vertices[i + 1].Position.X), M(vertices[i + 1].Position.Y),
                        elevation, section, storyName))
                    {
                        count++;
                    }
                }

                if (poly.IsClosed && vertices.Count > 2)
                {
                    if (CreateWallSegment(
                        M(vertices[vertices.Count - 1].Position.X),
                        M(vertices[vertices.Count - 1].Position.Y),
                        M(vertices[0].Position.X), M(vertices[0].Position.Y),
                        elevation, section, storyName))
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

        private bool CreateWallSegment(double x1, double y1, double x2, double y2,
            double elevation, string section, string storyName)
        {
            try
            {
                double length = Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
                if (length < 0.1)
                    return false;

                string[] pts = new string[4];

                sapModel.PointObj.AddCartesian(x1, y1, elevation, ref pts[0], "Global");
                sapModel.PointObj.AddCartesian(x2, y2, elevation, ref pts[1], "Global");
                sapModel.PointObj.AddCartesian(x2, y2, elevation + floorHeight, ref pts[2], "Global");
                sapModel.PointObj.AddCartesian(x1, y1, elevation + floorHeight, ref pts[3], "Global");

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

        private string DetermineWallSection(string layerName)
        {
            string upper = layerName.ToUpper();

            if (upper.Contains("300") || upper.Contains("_300") || upper.Contains("-300"))
                return "WALL300";
            else if (upper.Contains("250") || upper.Contains("_250") || upper.Contains("-250"))
                return "WALL250";
            else if (upper.Contains("230") || upper.Contains("_230") || upper.Contains("-230"))
                return "WALL230";
            else if (upper.Contains("200") || upper.Contains("_200") || upper.Contains("-200"))
                return "WALL200";
            else if (upper.Contains("175") || upper.Contains("_175") || upper.Contains("-175"))
                return "WALL175";
            else if (upper.Contains("150") || upper.Contains("_150") || upper.Contains("-150"))
                return "WALL150";
            else if (upper.Contains("125") || upper.Contains("_125") || upper.Contains("-125"))
                return "WALL125";
            else if (upper.Contains("100") || upper.Contains("_100") || upper.Contains("-100"))
                return "WALL100";

            if (upper.Contains("SHEAR") || upper.Contains("CORE") || upper.Contains("LIFT") ||
                upper.Contains("STAIRCASE") || upper.Contains("SHAFT") || upper.Contains("ELEVATOR"))
                return "WALL250";

            if (upper.Contains("BASEMENT") || upper.Contains("RETAINING") || upper.Contains("FOUNDATION"))
                return "WALL300";

            if (upper.Contains("EXTERNAL") || upper.Contains("EXTERIOR") || upper.Contains("OUTER") ||
                upper.Contains("BOUNDARY") || upper.Contains("PERIMETER") || upper.Contains("FACADE"))
                return "WALL230";

            if (upper.Contains("INTERNAL") || upper.Contains("INTERIOR") || upper.Contains("INNER") ||
                upper.Contains("ROOM") || upper.Contains("APARTMENT") || upper.Contains("UNIT"))
                return "WALL175";

            if (upper.Contains("PARTITION") || upper.Contains("DIVIDER") || upper.Contains("SEPARATION") ||
                upper.Contains("TOILET") || upper.Contains("BATHROOM") || upper.Contains("WC"))
                return "WALL150";

            if (upper.Contains("LIGHT") || upper.Contains("THIN") || upper.Contains("NONLOAD"))
                return "WALL125";

            if (upper.Contains("PODIUM"))
                return "WALL230";
            if (upper.Contains("EDECK") || upper.Contains("GROUND"))
                return "WALL200";

            return "WALL200";
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
            return $"Walls Created: {wallsCreated}, Failed: {wallsFailed}";
        }

        public void ResetStatistics()
        {
            wallsCreated = 0;
            wallsFailed = 0;
        }
    }
}
