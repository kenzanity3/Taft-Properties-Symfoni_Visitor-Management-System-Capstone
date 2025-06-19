using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VisitorManagementSystem_Captstone.NewModel
{
    public class VisitLog
    {
        /// <summary>
        /// Primary key identifier for the visit
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int VisitId { get; set; }

        /// <summary>
        /// Foreign key to the visitor who initiated the visit
        /// </summary>
        [Required]
        public required string VisitorId { get; set; }

        [ForeignKey(nameof(VisitorId))]
        public Visitor? Visitor { get; set; }

        /// <summary>
        /// Foreign key to the room being visited
        /// </summary>
        [Required]
        public required string RoomNumber { get; set; }

        [ForeignKey(nameof(RoomNumber))]
        public Room? Room { get; set; }

        /// <summary>
        /// Foreign key to the staff who verified the visit
        /// </summary>
        public string? StaffId { get; set; }

        [ForeignKey(nameof(StaffId))]
        public Staff? Staff { get; set; }

        /// <summary>
        /// Foreign key to the room owner being visited
        /// </summary>
        [Required]
        public required string OwnerId { get; set; }

        [ForeignKey(nameof(OwnerId))]
        public RoomOwner? RoomOwner { get; set; }

        /// <summary>
        /// The exact time the visitor entered the premises
        /// </summary>
        public DateTime? TimeIn { get; set; }

        /// <summary>
        /// The exact time the visitor exited the premises
        /// </summary>
        public DateTime? TimeOut { get; set; }

        /// <summary>
        /// Scheduled date of the visit
        /// </summary>
        [Required]
        public required DateOnly Date { get; set; }

        /// <summary>
        /// Access pass number or code used for this visit (can be shown as QR)
        /// </summary>
        [Required]
        public required string PassNo { get; set; }

        /// <summary>
        /// Purpose of the visit (e.g., Delivery, Maintenance, Guest)
        /// </summary>
        [Required]
        public required string PurposeOfVisit { get; set; }

        /// <summary>
        /// Current status of the visit (e.g., Pending, Approved, Denied, Completed)
        /// </summary>
        [Required]
        public required string VisitStatus { get; set; } = "Pending";

        /// <summary>
        /// Date and time when the visit was approved by the room owner
        /// </summary>
        public DateTime? ApprovedDate { get; set; }

        /// <summary>
        /// Type of visit (e.g., Guest, Delivery, Service)
        /// </summary>
        [Required]
        public required string VisitType { get; set; }

        /// <summary>
        /// Status of the pass (e.g., Active, Expired, Used)
        /// </summary>
        [Required]
        public required string PassStatus { get; set; } = "Active";

        /// <summary>
        /// Timestamp when staff verified the visitor's identity
        /// </summary>
        public DateTime? VerifiedDateTime { get; set; }
    }
}
