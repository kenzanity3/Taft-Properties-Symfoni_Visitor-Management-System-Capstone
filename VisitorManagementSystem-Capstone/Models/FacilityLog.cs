using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VisitorManagementSystem_Capstone.Models
{
    /// <summary>
    /// Represents a log entry for a user's facility access, including pass details, usage date, and check-in/out information.
    /// </summary>
    public class FacilityLog
    {
        /// <summary>
        /// Primary key for each facility usage record.
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int FacilityLogId { get; set; }

        /// <summary>
        /// The date the pass was issued. Required and stores only the date without time.
        /// </summary>
        [Required]
        public DateOnly IssueDate { get; set; } = DateOnly.FromDateTime(DateTime.Now);

        /// <summary>
        /// Foreign key referencing the facility being used.
        /// </summary>
        [Required]
        public string FacilityId { get; set; }

        /// <summary>
        /// Navigation property for the associated facility.
        /// </summary>
        [ForeignKey(nameof(FacilityId))]
        public Facility? Facility { get; set; }

        /// <summary>
        /// Foreign key referencing the user accessing the facility.
        /// </summary>
        [Required]
        public string? UserId { get; set; }

        /// <summary>
        /// Navigation property for the associated user.
        /// </summary>
        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        /// <summary>
        /// Foreign key referencing the check-in and check-out record.
        /// </summary>
        public int CheckInOutId { get; set; }

        /// <summary>
        /// Navigation property to the associated CheckInOut entity,
        /// which contains the check-in and check-out timestamps.
        /// </summary>
        [ForeignKey(nameof(CheckInOutId))]
        public CheckInOut? CheckInOut { get; set; }

        /// <summary>
        /// Soft Deletion Status (true = active, false = delete)
        /// </summary>
        public bool logStatus { get; set; } = true;
    }
}