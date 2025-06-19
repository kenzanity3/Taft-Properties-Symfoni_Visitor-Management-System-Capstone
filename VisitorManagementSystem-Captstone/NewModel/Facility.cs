using System.ComponentModel.DataAnnotations;

namespace VisitorManagementSystem_Captstone.NewModel
{
    public class Facility
    {
        /// <summary>
        /// Unique identifier for the facility (e.g., Gym001, Pool02)
        /// </summary>
        [Key]
        public required string FacilityId { get; set; }

        /// <summary>
        /// Name of the facility (e.g., Swimming Pool, Function Hall)
        /// </summary>
        [Required]
        public required string Name { get; set; }

        /// <summary>
        /// Maximum number of people allowed to use the facility at a time
        /// </summary>
        [Required]
        public required int Capacity { get; set; }

        /// <summary>
        /// Optional description about the facility, rules, or purpose
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Time when the facility becomes available for use each day
        /// </summary>
        [Required]
        public required TimeOnly OpenTime { get; set; }

        /// <summary>
        /// Time when the facility closes each day
        /// </summary>
        [Required]
        public required TimeOnly ClosingTime { get; set; }

        /// <summary>
        /// Operational status of the facility (e.g., Open, Closed, Under Maintenance)
        /// </summary>
        [Required]
        public required string Status { get; set; } = "Open";

        /// <summary>
        /// Tower or building where the facility is located
        /// </summary>
        [Required]
        public required string Tower { get; set; }

        /// <summary>
        /// Floor level of the facility within the tower
        /// </summary>
        [Required]
        public required int Floor { get; set; }
    }
}
