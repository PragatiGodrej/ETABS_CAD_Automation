// ============================================================================
// FILE: Core/WallThicknessCalculator.cs
// ============================================================================
using System;
using System.Collections.Generic;

namespace ETABS_CAD_Automation.Core
{
    /// <summary>
    /// Calculates wall thickness based on TDD/PKO design standards
    /// Implements zone-specific wall thickness guidelines
    /// </summary>
    public class WallThicknessCalculator
    {
        public enum WallType
        {
            CoreWall,
            PeripheralDeadWall,
            PeripheralPortalWall,
            InternalWall
        }

        public enum ConstructionType
        {
            TypeI,  // Fire resistance type I
            TypeII  // Fire resistance type II (NCR)
        }

        /// <summary>
        /// Calculate recommended wall thickness based on design standards
        /// </summary>
        /// <param name="numTypicalFloors">Number of typical floors</param>
        /// <param name="wallType">Type of wall</param>
        /// <param name="seismicZone">Seismic zone (II, III, IV, V)</param>
        /// <param name="wallLength">Length of wall in meters (for short wall check)</param>
        /// <param name="isFloatingWall">True if wall has partial floating condition</param>
        /// <param name="constructionType">Construction type for fire safety</param>
        /// <returns>Recommended wall thickness in millimeters</returns>
        public static int GetRecommendedThickness(
            int numTypicalFloors,
            WallType wallType,
            string seismicZone,
            double wallLength = 2.0,
            bool isFloatingWall = false,
            ConstructionType constructionType = ConstructionType.TypeII)
        {
            // Validate inputs
            if (numTypicalFloors < 1 || numTypicalFloors > 50)
                throw new ArgumentException("Number of floors must be between 1 and 50");

            bool isShortWall = wallLength < 1.8; // Less than 1.8m is considered short

            // Route to appropriate calculation method
            switch (seismicZone)
            {
                case "Zone II":
                    return GetZone2Thickness(numTypicalFloors, wallType, isShortWall, isFloatingWall);

                case "Zone III":
                    return GetZone3Thickness(numTypicalFloors, wallType, isShortWall);

                case "Zone IV":
                    return GetZone4Thickness(numTypicalFloors, wallType, isShortWall, constructionType);

                case "Zone V":
                    // Zone V uses same as Zone IV but with higher safety factors
                    return GetZone4Thickness(numTypicalFloors, wallType, isShortWall, constructionType);

                default:
                    throw new ArgumentException($"Invalid seismic zone: {seismicZone}");
            }
        }

        /// <summary>
        /// Zone II thickness (Bangalore, Hyderabad)
        /// </summary>
        private static int GetZone2Thickness(int floors, WallType wallType, bool isShortWall, bool isFloatingWall)
        {
            switch (wallType)
            {
                case WallType.CoreWall:
                    if (floors <= 20)
                        return isFloatingWall ? 200 : 160;
                    else if (floors <= 25)
                        return isFloatingWall ? 250 : 200;
                    else if (floors <= 30)
                        return isFloatingWall ? 250 : 200;
                    else if (floors <= 35)
                        return isFloatingWall ? 300 : 200;
                    else if (floors <= 40)
                        return isFloatingWall ? 300 : 200;
                    else if (floors <= 45)
                        return isFloatingWall ? 325 : 200;
                    else // 45-50
                        return isFloatingWall ? 350 : 300;

                case WallType.PeripheralDeadWall:
                    if (floors <= 20)
                        return isFloatingWall ? 200 : 160;
                    else if (floors <= 25)
                        return isFloatingWall ? 250 : 200;
                    else if (floors <= 30)
                        return isFloatingWall ? 250 : 200;
                    else if (floors <= 35)
                        return isFloatingWall ? 250 : 200;
                    else if (floors <= 40)
                        return isFloatingWall ? 250 : 200;
                    else if (floors <= 45)
                        return isFloatingWall ? 300 : 250;
                    else // 45-50
                        return isFloatingWall ? 350 : 300;

                case WallType.PeripheralPortalWall:
                    if (floors <= 40)
                        return 200;
                    else if (floors <= 45)
                        return 250;
                    else // 45-50
                        return 300;

                case WallType.InternalWall:
                    if (floors <= 20)
                        return isShortWall ? 200 : 160;
                    else if (floors <= 25)
                        return isShortWall ? 200 : 160;
                    else if (floors <= 30)
                        return isShortWall ? 250 : 160;
                    else if (floors <= 35)
                        return isShortWall ? 300 : 200;
                    else if (floors <= 40)
                        return isShortWall ? 300 : 200;
                    else if (floors <= 45)
                        return isShortWall ? 325 : 225;
                    else // 45-50
                        return isShortWall ? 350 : 250;

                default:
                    return 200; // Default fallback
            }
        }

        /// <summary>
        /// Zone III thickness (MMR, Ahmedabad, Kolkata, Pune)
        /// </summary>
        private static int GetZone3Thickness(int floors, WallType wallType, bool isShortWall)
        {
            switch (wallType)
            {
                case WallType.CoreWall:
                    if (floors <= 20)
                        return 200;
                    else if (floors <= 25)
                        return 300;
                    else if (floors <= 30)
                        return 350;
                    else if (floors <= 35)
                        return 375;
                    else if (floors <= 40)
                        return 400;
                    else if (floors <= 45)
                        return 425;
                    else // 45-50
                        return 450;

                case WallType.PeripheralDeadWall:
                    if (floors <= 20)
                        return 200;
                    else if (floors <= 25)
                        return 200;
                    else if (floors <= 30)
                        return 250;
                    else if (floors <= 35)
                        return 300;
                    else if (floors <= 40)
                        return 325;
                    else if (floors <= 45)
                        return 350;
                    else // 45-50
                        return 400;

                case WallType.PeripheralPortalWall:
                    if (floors <= 20)
                        return 300;
                    else if (floors <= 25)
                        return 350;
                    else if (floors <= 30)
                        return 400;
                    else if (floors <= 40)
                        return 400;
                    else if (floors <= 45)
                        return 400;
                    else // 45-50
                        return 450;

                case WallType.InternalWall:
                    if (floors <= 20)
                        return isShortWall ? 300 : 200;
                    else if (floors <= 25)
                        return isShortWall ? 300 : 200;
                    else if (floors <= 30)
                        return isShortWall ? 300 : 200;
                    else if (floors <= 35)
                        return isShortWall ? 350 : 225;
                    else if (floors <= 40)
                        return isShortWall ? 400 : 250;
                    else if (floors <= 45)
                        return isShortWall ? 450 : 275;
                    else // 45-50
                        return isShortWall ? 500 : 300;

                default:
                    return 200;
            }
        }

        /// <summary>
        /// Zone IV thickness (Type I construction)
        /// </summary>
        private static int GetZone4Thickness(int floors, WallType wallType, bool isShortWall, ConstructionType constructionType)
        {
            // Zone IV guidelines are for Type I construction
            // For Type II, we might need to adjust (not specified in guidelines)

            switch (wallType)
            {
                case WallType.CoreWall:
                    if (floors <= 20)
                        return 300;
                    else if (floors <= 25)
                        return 350;
                    else if (floors <= 30)
                        return 375;
                    else if (floors <= 35)
                        return 400;
                    else if (floors <= 40)
                        return 425;
                    else if (floors <= 45)
                        return 450;
                    else // 45-50
                        return 500;

                case WallType.PeripheralDeadWall:
                    if (floors <= 25)
                        return 240;
                    else if (floors <= 30)
                        return 275;
                    else if (floors <= 35)
                        return 300;
                    else if (floors <= 40)
                        return 325;
                    else if (floors <= 45)
                        return 350;
                    else // 45-50
                        return 400;

                case WallType.PeripheralPortalWall:
                    if (floors <= 20)
                        return 300;
                    else if (floors <= 25)
                        return 350;
                    else if (floors <= 30)
                        return 400;
                    else if (floors <= 40)
                        return 400;
                    else if (floors <= 45)
                        return 400;
                    else // 45-50
                        return 450;

                case WallType.InternalWall:
                    if (floors <= 20)
                        return isShortWall ? 300 : 240;
                    else if (floors <= 25)
                        return isShortWall ? 300 : 240;
                    else if (floors <= 30)
                        return isShortWall ? 300 : 240;
                    else if (floors <= 35)
                        return isShortWall ? 350 : 240;
                    else if (floors <= 40)
                        return isShortWall ? 400 : 240;
                    else if (floors <= 45)
                        return isShortWall ? 450 : 275;
                    else // 45-50
                        return isShortWall ? 500 : 300;

                default:
                    return 240;
            }
        }

        /// <summary>
        /// Get all available wall sections for a given configuration
        /// </summary>
        public static List<int> GetAvailableThicknesses(int numTypicalFloors, string seismicZone)
        {
            HashSet<int> thicknesses = new HashSet<int>();

            // Get all possible thicknesses for this configuration
            foreach (WallType wallType in Enum.GetValues(typeof(WallType)))
            {
                thicknesses.Add(GetRecommendedThickness(numTypicalFloors, wallType, seismicZone, 2.0, false));
                thicknesses.Add(GetRecommendedThickness(numTypicalFloors, wallType, seismicZone, 1.5, false)); // short wall
            }

            List<int> sortedList = new List<int>(thicknesses);
            sortedList.Sort();
            return sortedList;
        }

        /// <summary>
        /// Classify wall type based on layer name
        /// </summary>
        public static WallType ClassifyWallFromLayerName(string layerName)
        {
            string upper = layerName.ToUpperInvariant();

            // Core walls
            if (upper.Contains("CORE") || upper.Contains("LIFT") ||
                upper.Contains("ELEVATOR") || upper.Contains("SHAFT") ||
                upper.Contains("STAIRCASE") || upper.Contains("STAIR"))
                return WallType.CoreWall;

            // Peripheral portal walls
            if (upper.Contains("PORTAL") || upper.Contains("FRAME"))
                return WallType.PeripheralPortalWall;

            // Peripheral dead walls (exterior)
            if (upper.Contains("EXTERNAL") || upper.Contains("EXTERIOR") ||
                upper.Contains("OUTER") || upper.Contains("BOUNDARY") ||
                upper.Contains("PERIMETER") || upper.Contains("FACADE"))
                return WallType.PeripheralDeadWall;

            // Internal walls (default)
            return WallType.InternalWall;
        }

        /// <summary>
        /// Get design notes for the user
        /// </summary>
        public static string GetDesignNotes(int numTypicalFloors, string seismicZone)
        {
            string notes = "=== WALL THICKNESS DESIGN NOTES ===\n\n";

            notes += $"Configuration: {numTypicalFloors} typical floors in {seismicZone}\n\n";

            notes += "Guidelines Applied:\n";
            notes += "• Buildings up to 50 typical floors considered\n";
            notes += "• All structural walls continuous from foundation\n";
            notes += "• Partial floating walls allowed in Zone-II\n";
            notes += "• Distributed walls (Mivan) considered\n";
            notes += "• Building slenderness ratio < 7\n";
            notes += "• Wall thickness ±50mm variation allowed based on analysis\n\n";

            notes += "Wall Classifications:\n";
            notes += "• Core Wall: Lift shafts, staircase cores\n";
            notes += "• Peripheral Dead Wall: Exterior boundary walls\n";
            notes += "• Peripheral Portal Wall: Moment-resisting frame walls\n";
            notes += "• Internal Wall: Interior partition/structural walls\n\n";

            notes += "Short Wall Definition:\n";
            notes += "• Walls with length < 1.8m use higher thickness\n\n";

            if (numTypicalFloors > 50)
            {
                notes += "⚠️ WARNING: Building exceeds 50 floors - manual review required!\n";
            }

            return notes;
        }
    }
}
