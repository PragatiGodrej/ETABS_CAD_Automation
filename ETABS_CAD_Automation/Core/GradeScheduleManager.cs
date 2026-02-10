// ============================================================================
// FILE: Core/GradeScheduleManager.cs
// ============================================================================
using System;
using System.Collections.Generic;
using System.Linq;

namespace ETABS_CAD_Automation.Core
{
    /// <summary>
    /// Manages concrete grade assignments by floor level
    /// Wall grades are specified by user, beam/slab grades are 0.7x wall grade (rounded to nearest 5)
    /// </summary>
    public class GradeScheduleManager
    {
        public class GradeSchedule
        {
            public string WallGrade { get; set; }           // e.g., "M50"
            public int FloorsFromBottom { get; set; }       // Number of floors for this grade
            public string BeamSlabGrade { get; set; }       // Auto-calculated: 0.7x wall grade
        }

        private List<GradeSchedule> gradeSchedules = new List<GradeSchedule>();
        private int totalFloors;

        /// <summary>
        /// Initialize grade schedule manager
        /// </summary>
        /// <param name="wallGrades">List of wall grades from bottom up (e.g., ["M50", "M45", "M40", "M30"])</param>
        /// <param name="floorsPerGrade">Number of floors for each grade (e.g., [11, 10, 10, 8])</param>
        public GradeScheduleManager(List<string> wallGrades, List<int> floorsPerGrade)
        {
            if (wallGrades == null || floorsPerGrade == null)
                throw new ArgumentNullException("Grade schedule parameters cannot be null");

            if (wallGrades.Count != floorsPerGrade.Count)
                throw new ArgumentException("Wall grades and floors per grade must have same count");

            totalFloors = floorsPerGrade.Sum();

            for (int i = 0; i < wallGrades.Count; i++)
            {
                string wallGrade = wallGrades[i];
                int floors = floorsPerGrade[i];

                // Calculate beam/slab grade: 0.7x wall grade, rounded to nearest 5
                string beamSlabGrade = CalculateBeamSlabGrade(wallGrade);

                gradeSchedules.Add(new GradeSchedule
                {
                    WallGrade = wallGrade,
                    FloorsFromBottom = floors,
                    BeamSlabGrade = beamSlabGrade
                });

                System.Diagnostics.Debug.WriteLine(
                    $"Grade Schedule: Floors {GetFloorRangeText(i)} → Wall: {wallGrade}, Beam/Slab: {beamSlabGrade}");
            }
        }

        /// <summary>
        /// Calculate beam/slab grade from wall grade
        /// Rule: 0.7 × wall grade, rounded to nearest 5
        /// </summary>
        //private string CalculateBeamSlabGrade(string wallGrade)
        //{
        //    // Extract numeric value from grade (e.g., "M50" → 50)
        //    int wallGradeValue = ExtractGradeValue(wallGrade);

        //    // Calculate 0.7x
        //    double beamSlabValue = wallGradeValue * 0.7;

        //    // Round to nearest 5
        //    int roundedValue = (int)(Math.Round(beamSlabValue / 5.0) * 5);

        //    // Ensure minimum grade of M20
        //    if (roundedValue < 20)
        //        roundedValue = 20;

        //    return $"M{roundedValue}";
        //}
        private string CalculateBeamSlabGrade(string wallGrade)
        {
            int wallGradeValue = ExtractGradeValue(wallGrade);

            double beamSlabValue = wallGradeValue * 0.7;

            // Always round UP to nearest 5
            int roundedValue = (int)(Math.Ceiling(beamSlabValue / 5.0) * 5);

            // Minimum grade check
            if (roundedValue < 20)
                roundedValue = 20;

            return $"M{roundedValue}";
        }


        /// <summary>
        /// Extract numeric grade value from grade string
        /// </summary>
        private int ExtractGradeValue(string grade)
        {
            if (string.IsNullOrEmpty(grade))
                throw new ArgumentException("Grade cannot be null or empty");

            // Remove 'M' prefix and parse
            string numericPart = grade.ToUpperInvariant().Replace("M", "").Trim();

            if (int.TryParse(numericPart, out int value))
            {
                return value;
            }

            throw new ArgumentException($"Invalid grade format: {grade}. Expected format: M30, M40, etc.");
        }

        /// <summary>
        /// Get wall grade for a specific story (0-based index from bottom)
        /// </summary>
        public string GetWallGradeForStory(int story)
        {
            if (story < 0 || story >= totalFloors)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"⚠️ Story {story} out of range (0-{totalFloors - 1}), using last grade");
                return gradeSchedules.Last().WallGrade;
            }

            int floorsFromBottom = 0;
            foreach (var schedule in gradeSchedules)
            {
                floorsFromBottom += schedule.FloorsFromBottom;
                if (story < floorsFromBottom)
                {
                    return schedule.WallGrade;
                }
            }

            // Fallback to last grade
            return gradeSchedules.Last().WallGrade;
        }

        /// <summary>
        /// Get beam/slab grade for a specific story (0-based index from bottom)
        /// </summary>
        public string GetBeamSlabGradeForStory(int story)
        {
            if (story < 0 || story >= totalFloors)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"⚠️ Story {story} out of range (0-{totalFloors - 1}), using last grade");
                return gradeSchedules.Last().BeamSlabGrade;
            }

            int floorsFromBottom = 0;
            foreach (var schedule in gradeSchedules)
            {
                floorsFromBottom += schedule.FloorsFromBottom;
                if (story < floorsFromBottom)
                {
                    return schedule.BeamSlabGrade;
                }
            }

            // Fallback to last grade
            return gradeSchedules.Last().BeamSlabGrade;
        }

        /// <summary>
        /// Get human-readable floor range for a grade schedule index
        /// </summary>
        private string GetFloorRangeText(int scheduleIndex)
        {
            if (scheduleIndex < 0 || scheduleIndex >= gradeSchedules.Count)
                return "Unknown";

            int startFloor = 0;
            for (int i = 0; i < scheduleIndex; i++)
            {
                startFloor += gradeSchedules[i].FloorsFromBottom;
            }

            int endFloor = startFloor + gradeSchedules[scheduleIndex].FloorsFromBottom - 1;

            return $"{startFloor + 1}-{endFloor + 1}"; // Convert to 1-based for display
        }

        /// <summary>
        /// Get summary of grade schedule
        /// </summary>
        public string GetScheduleSummary()
        {
            string summary = "=== CONCRETE GRADE SCHEDULE ===\n\n";
            summary += $"Total Floors: {totalFloors}\n\n";

            for (int i = 0; i < gradeSchedules.Count; i++)
            {
                var schedule = gradeSchedules[i];
                string floorRange = GetFloorRangeText(i);

                summary += $"Floors {floorRange} ({schedule.FloorsFromBottom} floors):\n";
                summary += $"  Wall Grade: {schedule.WallGrade}\n";
                summary += $"  Beam/Slab Grade: {schedule.BeamSlabGrade}\n\n";
            }

            return summary;
        }

        /// <summary>
        /// Validate that total floors match expected count
        /// </summary>
        public bool ValidateTotalFloors(int expectedFloors)
        {
            bool isValid = totalFloors == expectedFloors;

            if (!isValid)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"⚠️ Grade schedule floor count ({totalFloors}) doesn't match expected ({expectedFloors})");
            }

            return isValid;
        }

        /// <summary>
        /// Get all grade schedules (for UI display)
        /// </summary>
        public List<GradeSchedule> GetAllSchedules()
        {
            return new List<GradeSchedule>(gradeSchedules);
        }
    }
}
