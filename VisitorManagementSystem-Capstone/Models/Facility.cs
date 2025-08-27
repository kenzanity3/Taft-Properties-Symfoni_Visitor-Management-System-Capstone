using System.ComponentModel.DataAnnotations;

namespace VisitorManagementSystem_Capstone.Models
{
    /// <summary>
    /// Represents a facility within the premises that can be accessed or reserved by visitors or residents.
    /// </summary>
    public class Facility
    {
        /// <summary>
        /// Unique identifier for the facility (e.g., Gym001, Pool02).
        /// </summary>
        [Key, Required]
        public string? FacilityId { get; set; }

        /// <summary>
        /// Name of the facility (e.g., Swimming Pool, Function Hall).
        /// </summary>
        [Required]
        public string? Name { get; set; }

        /// <summary>
        /// Maximum number of people allowed to use the facility at one time.
        /// </summary>
        [Required]
        public int? Capacity { get; set; }

        /// <summary>
        /// Optional description providing additional details, rules, or usage purpose of the facility.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Time when the facility becomes available for use each day.
        /// </summary>
        [Required]
        public TimeOnly? OpenTime { get; set; }

        /// <summary>
        /// Time when the facility closes each day.
        /// </summary>
        [Required]
        public TimeOnly? ClosingTime { get; set; }

        /// <summary>
        /// Operational status of the facility. True = Open, False = Closed, Null = Under Maintenance.
        /// </summary>
        public bool FacilityStatus { get; set; }

        [Required]
        public string FloorLevel { get; set; } = "Second Floor";
    }
}