using Microsoft.AspNet.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VisitorManagementSystem_Capstone.Models
{
    /// <summary>
    /// Represents the record of a visitor's log including request details, verification status,
    /// associated visitor and room owner, visit purpose, and other tracking information.
    /// </summary>
    public class VisitLog
    {
        /// <summary>
        /// Primary key identifier for the visit log.
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int VisitLogId { get; set; }

        /// <summary>
        /// date when the visit was requested.
        /// </summary>
        public DateOnly IssueDate { get; set; } = DateOnly.FromDateTime(DateTime.Now);

        /// <summary>
        /// Scheduled appointment date and time for the visit. This field is optional.
        /// </summary>
        public DateOnly? AppointmentDate { get; set; }

        /// <summary>
        /// Verification status of the visit request:
        /// null if pending, false if denied, true if approved.
        /// </summary>
        public bool? VerificationStatus { get; set; }

        /// <summary>
        /// Date and time when the Room Owner verified the visitor's identity, if applicable.
        /// </summary>
        public DateTime? VerifiedDateTime { get; set; }


        /// <summary>
        /// Purpose of the visit (e.g., Delivery, Maintenance, Guest).
        /// </summary>
        [Required]
        public required string PurposeOfVisit { get; set; }

        /// <summary>
        /// Foreign key referencing the check-in and check-out record for the visit.
        /// </summary>
        public int? CheckInOutId { get; set; } = null;

        /// <summary>
        /// Navigation property to the CheckInOut entity linked to this visit.
        /// </summary>
        public CheckInOut? CheckInOut { get; set; }

        /// <summary>
        /// Foreign key referencing the visitor who initiated the request.
        /// </summary>
        [Required]
        public required string VisitorUserId { get; set; }

        /// <summary>
        /// Navigation property to the visitor entity.
        /// </summary>
        [ForeignKey(nameof(VisitorUserId))]
        public Visitor? Visitor { get; set; }

        /// <summary>
        /// Foreign key referencing the room owner being visited.
        /// </summary>
        [Required]
        public required string OwnerUserId { get; set; }

        /// <summary>
        /// Navigation property to the room owner entity.
        /// </summary>
        [ForeignKey(nameof(OwnerUserId))]
        public RoomOwner? RoomOwner { get; set; }

        /// <summary>
        /// Foreign key referencing the room being visited.
        /// </summary>
        [Required]
        public required int RoomId { get; set; }

        /// <summary>
        /// Navigation property to the room entity.
        /// </summary>
        [ForeignKey(nameof(RoomId))]
        public Room? Room { get; set; }

        /// <summary>
        /// Indicates who created the visit request: "visitor", "staff", or "admin"
        /// </summary>
        [Required]
        public string CreatedBy { get; set; } = "visitor";

        /// <summary>
        /// Foreign key referencing the staff and admin who created the request (if applicable)
        /// </summary>
        [StringLength(450)]
        public string? AuthorizedUserId { get; set; }

        /// <summary>
        /// Navigation property to the User entity (if staff or admin created the request) to get the full name of staff or admin
        /// </summary>
        [ForeignKey(nameof(AuthorizedUserId))]
        public User? AuthorizedUser { get; set; }


        /// <summary>
        /// Soft Deletion Status(true = active, false = delete)
        /// </summary>
        public bool logStatus { get; set; } = true;
    }
}