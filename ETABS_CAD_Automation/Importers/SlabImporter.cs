
// ============================================================================
// FILE: Importers/SlabImporter.cs (FIXED VERSION)
// ============================================================================
using ETABSv1;
using netDxf;
using netDxf.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using static netDxf.Entities.HatchBoundaryPath;

namespace ETABS_CAD_Automation.Importers
{
    public class SlabImporter
    {
        private cSapModel sapModel;
        private DxfDocument dxfDoc;
        private const double MM_TO_M = 0.001;
        private const double CLOSURE_TOLERANCE = 10000.0; // 10000mm (10m) tolerance - auto-close large gaps
        private const double MIN_AREA = 0.01; // 0.01 m² minimum area

        private double M(double mm) => mm * MM_TO_M;

        public SlabImporter(cSapModel model, DxfDocument doc)
        {
            sapModel = model;
            dxfDoc = doc;
        }

        public void DefineSections()
        {
            try
            {
                sapModel.PropArea.SetSlab("SLAB100", eSlabType.Slab, eShellType.ShellThin,
                    "CONC", 0.10, 12, "CONC", "CONC");

                sapModel.PropArea.SetSlab("SLAB125", eSlabType.Slab, eShellType.ShellThin,
                    "CONC", 0.125, 12, "CONC", "CONC");

                sapModel.PropArea.SetSlab("SLAB150", eSlabType.Slab, eShellType.ShellThin,
                    "CONC", 0.15, 12, "CONC", "CONC");

                sapModel.PropArea.SetSlab("SLAB175", eSlabType.Slab, eShellType.ShellThin,
                    "CONC", 0.175, 12, "CONC", "CONC");

                sapModel.PropArea.SetSlab("SLAB200", eSlabType.Slab, eShellType.ShellThin,
                    "CONC", 0.20, 12, "CONC", "CONC");

                sapModel.PropArea.SetSlab("SLAB225", eSlabType.Slab, eShellType.ShellThin,
                    "CONC", 0.225, 12, "CONC", "CONC");

                sapModel.PropArea.SetSlab("SLAB250", eSlabType.Slab, eShellType.ShellThin,
                    "CONC", 0.25, 12, "CONC", "CONC");

                System.Diagnostics.Debug.WriteLine("✓ Slab sections defined successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error defining slab sections: {ex.Message}");
                throw;
            }
        }

        public void ImportSlabs(Dictionary<string, string> layerMapping, double elevation, int story)
        {
            var slabLayers = layerMapping.Where(x => x.Value == "Slab").Select(x => x.Key).ToList();

            if (slabLayers.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine($"⚠ No slab layers found in mapping for story {story}");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"\n========== IMPORTING SLABS - Story {story} at {elevation}m ==========");

            int successCount = 0;
            int failCount = 0;
            int skippedCount = 0;

            foreach (string layerName in slabLayers)
            {
                string section = DetermineSlabSection(layerName);
                System.Diagnostics.Debug.WriteLine($"\n--- Layer: {layerName} → Section: {section} ---");

                // Process Polylines2D
                var polylines = dxfDoc.Entities.Polylines2D
                    .Where(p => p.Layer.Name == layerName).ToList();

                System.Diagnostics.Debug.WriteLine($"Found {polylines.Count} polylines");

                foreach (var poly in polylines)
                {
                    var result = CreateSlabFromPolyline(poly, elevation, section, story);
                    if (result == SlabCreationResult.Success) successCount++;
                    else if (result == SlabCreationResult.Failed) failCount++;
                    else skippedCount++;
                }

                // Process Hatches
                var hatches = dxfDoc.Entities.Hatches
                    .Where(h => h.Layer.Name == layerName).ToList();

                System.Diagnostics.Debug.WriteLine($"Found {hatches.Count} hatches");

                foreach (var hatch in hatches)
                {
                    var result = CreateSlabFromHatch(hatch, elevation, section, story);
                    if (result == SlabCreationResult.Success) successCount++;
                    else if (result == SlabCreationResult.Failed) failCount++;
                    else skippedCount++;
                }
            }

            System.Diagnostics.Debug.WriteLine($"\n========== SLAB IMPORT SUMMARY ==========");
            System.Diagnostics.Debug.WriteLine($"✓ Success: {successCount}");
            System.Diagnostics.Debug.WriteLine($"❌ Failed: {failCount}");
            System.Diagnostics.Debug.WriteLine($"⊘ Skipped: {skippedCount}");
            System.Diagnostics.Debug.WriteLine($"=========================================\n");
        }

        private enum SlabCreationResult
        {
            Success,
            Failed,
            Skipped
        }

        private SlabCreationResult CreateSlabFromPolyline(Polyline2D poly, double elevation, string section, int story)
        {
            try
            {
                var vertices = poly.Vertexes;
                if (vertices == null || vertices.Count < 3)
                {
                    System.Diagnostics.Debug.WriteLine($"⊘ Skipped: Only {vertices?.Count ?? 0} vertices");
                    return SlabCreationResult.Skipped;
                }

                // Extract vertex positions
                List<netDxf.Vector2> points = new List<netDxf.Vector2>();
                foreach (var v in vertices)
                {
                    points.Add(v.Position);
                }

                // Check closure and auto-close if needed
                if (!IsClosedOrAutoClose(ref points))
                {
                    System.Diagnostics.Debug.WriteLine($"⊘ Skipped: Polyline not closed and cannot auto-close");
                    return SlabCreationResult.Skipped;
                }

                return CreateSlabFromPoints(points, elevation, section, story);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Exception in CreateSlabFromPolyline: {ex.Message}");
                return SlabCreationResult.Failed;
            }
        }

        private SlabCreationResult CreateSlabFromHatch(Hatch hatch, double elevation, string section, int story)
        {
            try
            {
                SlabCreationResult overallResult = SlabCreationResult.Skipped;

                foreach (var boundaryPath in hatch.BoundaryPaths)
                {
                    var edges = boundaryPath.Edges;
                    if (edges.Count == 0)
                        continue;

                    List<netDxf.Vector2> vertices = ExtractHatchBoundaryVertices(edges);

                    if (vertices.Count >= 3)
                    {
                        // Check closure
                        if (!IsClosedOrAutoClose(ref vertices))
                        {
                            System.Diagnostics.Debug.WriteLine($"⊘ Skipped: Hatch boundary not closed");
                            continue;
                        }

                        var result = CreateSlabFromPoints(vertices, elevation, section, story);
                        if (result == SlabCreationResult.Success)
                            overallResult = SlabCreationResult.Success;
                        else if (result == SlabCreationResult.Failed && overallResult != SlabCreationResult.Success)
                            overallResult = SlabCreationResult.Failed;
                    }
                }

                return overallResult;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Exception in CreateSlabFromHatch: {ex.Message}");
                return SlabCreationResult.Failed;
            }
        }

        private List<netDxf.Vector2> ExtractHatchBoundaryVertices(IReadOnlyList<HatchBoundaryPath.Edge> edges)
        {
            List<netDxf.Vector2> vertices = new List<netDxf.Vector2>();

            foreach (var edge in edges)
            {
                if (edge is HatchBoundaryPath.Line lineEdge)
                {
                    vertices.Add(lineEdge.Start);
                }
                else if (edge is HatchBoundaryPath.Arc arcEdge)
                {
                    // Tessellate arc into line segments
                    var arcPoints = TessellateArc(arcEdge);
                    vertices.AddRange(arcPoints);
                }
                else if (edge is HatchBoundaryPath.Ellipse ellipseEdge)
                {
                    // Tessellate ellipse into line segments
                    var ellipsePoints = TessellateEllipse(ellipseEdge);
                    vertices.AddRange(ellipsePoints);
                }
                else if (edge is HatchBoundaryPath.Spline splineEdge)
                {
                    // Use control points (simplified approach)
                    foreach (var cp in splineEdge.ControlPoints)
                    {
                        vertices.Add(new netDxf.Vector2(cp.X, cp.Y));
                    }
                }
            }

            return vertices;
        }

        private List<netDxf.Vector2> TessellateArc(HatchBoundaryPath.Arc arc)
        {
            List<netDxf.Vector2> points = new List<netDxf.Vector2>();
            int segments = 16; // Number of segments to approximate the arc

            double startAngle = arc.StartAngle * Math.PI / 180.0;
            double endAngle = arc.EndAngle * Math.PI / 180.0;

            // Handle arc direction
            if (endAngle < startAngle)
                endAngle += 2 * Math.PI;

            double angleStep = (endAngle - startAngle) / segments;

            for (int i = 0; i <= segments; i++)
            {
                double angle = startAngle + i * angleStep;
                double x = arc.Center.X + arc.Radius * Math.Cos(angle);
                double y = arc.Center.Y + arc.Radius * Math.Sin(angle);
                points.Add(new netDxf.Vector2(x, y));
            }

            return points;
        }

        private List<netDxf.Vector2> TessellateEllipse(HatchBoundaryPath.Ellipse ellipse)
        {
            List<netDxf.Vector2> points = new List<netDxf.Vector2>();
            int segments = 24; // Number of segments

            double startAngle = ellipse.StartAngle * Math.PI / 180.0;
            double endAngle = ellipse.EndAngle * Math.PI / 180.0;

            if (endAngle < startAngle)
                endAngle += 2 * Math.PI;

            double angleStep = (endAngle - startAngle) / segments;

            // Calculate semi-major and semi-minor axes
            double majorAxis = Math.Sqrt(ellipse.EndMajorAxis.X * ellipse.EndMajorAxis.X +
                                         ellipse.EndMajorAxis.Y * ellipse.EndMajorAxis.Y);
            double minorAxis = majorAxis * ellipse.MinorRatio;

            for (int i = 0; i <= segments; i++)
            {
                double angle = startAngle + i * angleStep;
                // Simplified ellipse parametric equation
                double x = ellipse.Center.X + majorAxis * Math.Cos(angle);
                double y = ellipse.Center.Y + minorAxis * Math.Sin(angle);
                points.Add(new netDxf.Vector2(x, y));
            }

            return points;
        }

        private bool IsClosedOrAutoClose(ref List<netDxf.Vector2> points)
        {
            if (points.Count < 3)
                return false;

            var first = points[0];
            var last = points[points.Count - 1];

            double gap = Math.Sqrt(
                Math.Pow(last.X - first.X, 2) +
                Math.Pow(last.Y - first.Y, 2));

            if (gap < CLOSURE_TOLERANCE)
            {
                // Remove last point if it's very close to first (near duplicate)
                if (gap < 0.1) // Less than 0.1mm
                {
                    points.RemoveAt(points.Count - 1);
                    System.Diagnostics.Debug.WriteLine($"  Removed duplicate last vertex (gap: {gap:F4}mm)");
                }
                else
                {
                    // Auto-close by adding a line segment
                    System.Diagnostics.Debug.WriteLine($"  Auto-closing polyline (gap: {gap:F2}mm)");
                }
                return true;
            }

            System.Diagnostics.Debug.WriteLine($"  Gap too large: {gap:F2}mm (max: {CLOSURE_TOLERANCE}mm)");
            return false;
        }

        private SlabCreationResult CreateSlabFromPoints(List<netDxf.Vector2> points, double elevation,
            string section, int story)
        {
            try
            {
                if (points == null || points.Count < 3)
                {
                    System.Diagnostics.Debug.WriteLine($"⊘ Insufficient vertices: {points?.Count ?? 0}");
                    return SlabCreationResult.Skipped;
                }

                // Validate area is not too small
                double polygonArea = CalculatePolygonArea(points);
                if (Math.Abs(polygonArea) < MIN_AREA)
                {
                    System.Diagnostics.Debug.WriteLine($"⊘ Area too small: {Math.Abs(polygonArea):F4} m² (min: {MIN_AREA} m²)");
                    return SlabCreationResult.Skipped;
                }

                // Ensure counter-clockwise winding (positive area)
                if (polygonArea < 0)
                {
                    points.Reverse();
                    System.Diagnostics.Debug.WriteLine($"  Reversed winding order (was clockwise)");
                }

                // Remove collinear/duplicate points
                var cleanedPoints = RemoveDuplicateAndCollinearPoints(points);

                if (cleanedPoints.Count < 3)
                {
                    System.Diagnostics.Debug.WriteLine($"⊘ After cleaning, only {cleanedPoints.Count} vertices remain");
                    return SlabCreationResult.Skipped;
                }

                // Check for self-intersection
                if (HasSelfIntersection(cleanedPoints))
                {
                    System.Diagnostics.Debug.WriteLine($"⚠ Warning: Polygon may be self-intersecting - attempting import anyway");
                }

                int n = cleanedPoints.Count;
                string[] pts = new string[n];
                string storyName = GetStoryName(story);

                // Create points in ETABS
                for (int i = 0; i < n; i++)
                {
                    string pointName = "";
                    sapModel.PointObj.AddCartesian(
                        M(cleanedPoints[i].X),
                        M(cleanedPoints[i].Y),
                        elevation,
                        ref pointName,
                        "Global");
                    pts[i] = pointName;
                }

                // Create area object
                string areaName = "";
                int ret = sapModel.AreaObj.AddByPoint(n, ref pts, ref areaName, section);

                if (ret == 0 && !string.IsNullOrEmpty(areaName))
                {
                    sapModel.AreaObj.SetGroupAssign(areaName, storyName);
                    System.Diagnostics.Debug.WriteLine(
                        $"✓ SUCCESS: {areaName} | Section: {section} | Vertices: {n} | " +
                        $"Area: {Math.Abs(CalculatePolygonArea(cleanedPoints)):F2} m²");
                    return SlabCreationResult.Success;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"❌ FAILED: AddByPoint returned {ret}");
                    System.Diagnostics.Debug.WriteLine($"   First point: ({M(cleanedPoints[0].X):F3}, {M(cleanedPoints[0].Y):F3}, {elevation:F3})");
                    System.Diagnostics.Debug.WriteLine($"   Vertices: {n}, Area: {Math.Abs(CalculatePolygonArea(cleanedPoints)):F2} m²");

                    // Try reversing winding order as last resort
                    cleanedPoints.Reverse();
                    System.Diagnostics.Debug.WriteLine($"   Retrying with reversed order...");

                    // Recreate points with reversed order
                    for (int i = 0; i < n; i++)
                    {
                        string pointName = "";
                        sapModel.PointObj.AddCartesian(
                            M(cleanedPoints[i].X),
                            M(cleanedPoints[i].Y),
                            elevation,
                            ref pointName,
                            "Global");
                        pts[i] = pointName;
                    }

                    ret = sapModel.AreaObj.AddByPoint(n, ref pts, ref areaName, section);

                    if (ret == 0 && !string.IsNullOrEmpty(areaName))
                    {
                        sapModel.AreaObj.SetGroupAssign(areaName, storyName);
                        System.Diagnostics.Debug.WriteLine($"✓ SUCCESS (reversed): {areaName}");
                        return SlabCreationResult.Success;
                    }

                    return SlabCreationResult.Failed;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Exception: {ex.Message}");
                return SlabCreationResult.Failed;
            }
        }

        private List<netDxf.Vector2> RemoveDuplicateAndCollinearPoints(List<netDxf.Vector2> points)
        {
            if (points.Count < 3)
                return points;

            List<netDxf.Vector2> cleaned = new List<netDxf.Vector2>();
            const double epsilon = 0.001; // 0.001mm tolerance

            for (int i = 0; i < points.Count; i++)
            {
                var current = points[i];
                var next = points[(i + 1) % points.Count];

                // Skip duplicate points
                double dist = Math.Sqrt(
                    Math.Pow(next.X - current.X, 2) +
                    Math.Pow(next.Y - current.Y, 2));

                if (dist > epsilon)
                {
                    cleaned.Add(current);
                }
            }

            return cleaned;
        }

        private double CalculatePolygonArea(List<netDxf.Vector2> points)
        {
            if (points.Count < 3)
                return 0;

            double polygonArea = 0;
            for (int i = 0; i < points.Count; i++)
            {
                int j = (i + 1) % points.Count;
                polygonArea += points[i].X * points[j].Y;
                polygonArea -= points[j].X * points[i].Y;
            }

            // Return signed area (positive = CCW, negative = CW)
            return (polygonArea / 2.0) * MM_TO_M * MM_TO_M; // Convert to m²
        }

        private bool HasSelfIntersection(List<netDxf.Vector2> points)
        {
            if (points.Count < 4)
                return false;

            // Check if any non-adjacent edges intersect
            for (int i = 0; i < points.Count; i++)
            {
                int i1 = i;
                int i2 = (i + 1) % points.Count;

                for (int j = i + 2; j < points.Count; j++)
                {
                    // Don't check adjacent edges or last edge with first edge
                    if (j == i || Math.Abs(j - i) == 1 || (i == 0 && j == points.Count - 1))
                        continue;

                    int j1 = j;
                    int j2 = (j + 1) % points.Count;

                    if (DoLineSegmentsIntersect(points[i1], points[i2], points[j1], points[j2]))
                        return true;
                }
            }

            return false;
        }

        private bool DoLineSegmentsIntersect(netDxf.Vector2 p1, netDxf.Vector2 p2,
                                             netDxf.Vector2 p3, netDxf.Vector2 p4)
        {
            double d = (p2.X - p1.X) * (p4.Y - p3.Y) - (p2.Y - p1.Y) * (p4.X - p3.X);

            if (Math.Abs(d) < 0.0001) // Parallel or collinear
                return false;

            double t = ((p3.X - p1.X) * (p4.Y - p3.Y) - (p3.Y - p1.Y) * (p4.X - p3.X)) / d;
            double u = ((p3.X - p1.X) * (p2.Y - p1.Y) - (p3.Y - p1.Y) * (p2.X - p1.X)) / d;

            return (t >= 0 && t <= 1 && u >= 0 && u <= 1);
        }

        private string DetermineSlabSection(string layerName)
        {
            if (string.IsNullOrWhiteSpace(layerName))
                return "SLAB150";

            string upper = layerName.ToUpperInvariant();

            // Explicit thickness (highest priority)
            if (upper.Contains("250") || upper.Contains("_250")) return "SLAB250";
            if (upper.Contains("225") || upper.Contains("_225")) return "SLAB225";
            if (upper.Contains("200") || upper.Contains("_200")) return "SLAB200";
            if (upper.Contains("175") || upper.Contains("_175")) return "SLAB175";
            if (upper.Contains("150") || upper.Contains("_150")) return "SLAB150";
            if (upper.Contains("125") || upper.Contains("_125")) return "SLAB125";
            if (upper.Contains("100") || upper.Contains("_100")) return "SLAB100";

            // Structural/heavy-duty
            if (upper.Contains("TRANSFER") || upper.Contains("PODIUM") || upper.Contains("PARKING"))
                return "SLAB250";

            // Roof/stairs/basement
            if (upper.Contains("ROOF") || upper.Contains("TERRACE") || upper.Contains("STAIR"))
                return "SLAB200";

            if (upper.Contains("BASEMENT") || upper.Contains("LOWER"))
                return "SLAB200";

            // Residential
            if (upper.Contains("RESIDENTIAL") || upper.Contains("APARTMENT") || upper.Contains("FLAT") ||
                upper.Contains("LIVINGROOM") || upper.Contains("KITCHEN") || upper.Contains("BEDROOM") ||
                upper.Contains("TOILET") || upper.Contains("BATHROOM"))
                return "SLAB150";

            // Light-duty
            if (upper.Contains("CORRIDOR") || upper.Contains("LOBBY") || upper.Contains("BALCONY") ||
                upper.Contains("SERVICE") || upper.Contains("UTILITY") || upper.Contains("CHAJJA"))
                return "SLAB125";

            return "SLAB150";
        }

        private string GetStoryName(int story)
        {
            try
            {
                int numStories = 0;
                string[] storyNames = null;
                int ret = sapModel.Story.GetNameList(ref numStories, ref storyNames);

                if (ret == 0 && storyNames != null && story - 1 >= 0 && story - 1 < storyNames.Length)
                {
                    return storyNames[story - 1];
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting story name: {ex.Message}");
            }

            return story == 0 ? "Base" : $"Story{story}";
        }
    }
}