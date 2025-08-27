using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VisitorManagementSystem_Capstone.Models
{
    /// <summary>
    /// Represents the check-in and check-out times for a visitor or facility log.
    /// Tracks when a user enters and exits the premises.
    /// </summary>
    public class CheckInOut
    {
        /// <summary>
        /// Primary key identifier for the CheckInOut record.
        /// </summary>
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CheckInOutId { get; set; }

        /// <summary>
        /// The timestamp when the user or visitor checked in.
        /// Automatically initialized to the current time.
        /// </summary>
        [Required]
        public DateTime CheckInDateTime { get; set; } = DateTime.Now;

        /// <summary>
        /// The timestamp when the user or visitor checked out.
        /// This field is nullable to allow check-in records that are still active.
        /// </summary>
        public DateTime? CheckOutDateTime { get; set; }

        /// <summary>
        /// The user ID of the staff member who checked out the visitor (if applicable).
        /// </summary>
        public string? CheckedOutBy { get; set; }
    }
}