// ============================================================================
// FILE: Importers/BeamImporter.cs
// ============================================================================
using ETABSv1;
using netDxf;
using netDxf.Entities;
using System.Collections.Generic;
using System.Linq;

namespace ETABS_CAD_Automation.Importers
{
    public class BeamImporter
    {
        private readonly cSapModel sapModel;
        private readonly DxfDocument dxfDoc;
        private const double MM_TO_M = 0.001;
        private double M(double mm) => mm * MM_TO_M;

        public BeamImporter(cSapModel model, DxfDocument doc)
        {
            sapModel = model;
            dxfDoc = doc;
        }

        public void DefineSections()
        {
            sapModel.PropFrame.SetRectangle("B230X450", "CONC", 0.23, 0.45);
            sapModel.PropFrame.SetRectangle("B250X450", "CONC", 0.25, 0.45);
            sapModel.PropFrame.SetRectangle("B300X450", "CONC", 0.30, 0.45);
            sapModel.PropFrame.SetRectangle("B300X500", "CONC", 0.30, 0.50);
            sapModel.PropFrame.SetRectangle("B300X600", "CONC", 0.30, 0.60);
            sapModel.PropFrame.SetRectangle("B400X600", "CONC", 0.40, 0.60);
        }

        public void ImportBeams(Dictionary<string, string> layerMapping, double elevation, int story)
        {
            var beamLayers = layerMapping
                .Where(x => x.Value == "Beam")
                .Select(x => x.Key)
                .ToList();

            foreach (string layerName in beamLayers)
            {
                string section = DetermineBeamSection(layerName);

                foreach (netDxf.Entities.Line line in dxfDoc.Entities.Lines
                    .Where(l => l.Layer.Name == layerName))
                {
                    CreateBeamFromLine(line, elevation, section, story);
                }

                foreach (Polyline2D poly in dxfDoc.Entities.Polylines2D
                    .Where(p => p.Layer.Name == layerName))
                {
                    CreateBeamFromPolyline(poly, elevation, section, story);
                }
            }
        }

        private void CreateBeamFromLine(netDxf.Entities.Line line, double elevation,
            string section, int story)
        {
            string frameName = "";
            string storyName = GetStoryName(story);

            sapModel.FrameObj.AddByCoord(
                M(line.StartPoint.X), M(line.StartPoint.Y), elevation,
                M(line.EndPoint.X), M(line.EndPoint.Y), elevation,
                ref frameName, section, storyName);
        }

        private void CreateBeamFromPolyline(Polyline2D poly, double elevation,
            string section, int story)
        {
            string storyName = GetStoryName(story);
            var vertices = poly.Vertexes;

            for (int i = 0; i < vertices.Count - 1; i++)
            {
                string frameName = "";
                sapModel.FrameObj.AddByCoord(
                    M(vertices[i].Position.X), M(vertices[i].Position.Y), elevation,
                    M(vertices[i + 1].Position.X), M(vertices[i + 1].Position.Y), elevation,
                    ref frameName, section, storyName);
            }

            if (poly.IsClosed && vertices.Count > 2)
            {
                string frameName = "";
                sapModel.FrameObj.AddByCoord(
                    M(vertices[vertices.Count - 1].Position.X),
                    M(vertices[vertices.Count - 1].Position.Y), elevation,
                    M(vertices[0].Position.X), M(vertices[0].Position.Y), elevation,
                    ref frameName, section, storyName);
            }
        }

        private string DetermineBeamSection(string layerName)
        {
            string upper = layerName.ToUpper();

            if (upper.Contains("400X600") || upper.Contains("400_600"))
                return "B400X600";
            if (upper.Contains("300X600") || upper.Contains("300_600"))
                return "B300X600";
            if (upper.Contains("300X500") || upper.Contains("300_500"))
                return "B300X500";
            if (upper.Contains("300X450") || upper.Contains("300_450"))
                return "B300X450";
            if (upper.Contains("250X450") || upper.Contains("250_450"))
                return "B250X450";
            if (upper.Contains("230X450") || upper.Contains("230_450"))
                return "B230X450";

            if (upper.Contains("TRANSFER") || upper.Contains("DEEP"))
                return "B400X600";
            if (upper.Contains("PRIMARY") || upper.Contains("MAIN"))
                return "B300X500";
            if (upper.Contains("SECONDARY") || upper.Contains("SEC"))
                return "B300X450";
            if (upper.Contains("EDGE") || upper.Contains("SPANDREL"))
                return "B230X450";

            return "B250X450";
        }

        private string GetStoryName(int story)
        {
            int numStories = 0;
            string[] storyNames = null;

            sapModel.Story.GetNameList(ref numStories, ref storyNames);

            if (storyNames != null && story > 0 && story <= storyNames.Length)
            {
                return storyNames[story - 1];
            }

            return "Story" + story;
        }
    }
}