using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VisitorManagementSystem_Captstone.NewModel
{
    public class AccountActionLog
    {
        /// <summary>
        /// Unique identifier for each logged action
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public required int LogId { get; set; }

        /// <summary>
        /// Type/category of the action (e.g., VisitRequest, AccountDeactivation, Approval)
        /// </summary>
        [Required]
        public required string ActionType { get; set; }

        /// <summary>
        /// Descriptive message or content for the action (e.g., "Visitor John requested access to Room 402")
        /// </summary>
        [Required]
        public required string ActionText { get; set; }

        /// <summary>
        /// Date and time when the action occurred
        /// </summary>
        [Required]
        public required DateTime ActionDateTime { get; set; } = DateTime.Now;

        /// <summary>
        /// (Optional) Foreign key to the user affected by the action (can be null)
        /// </summary>
        public string? TargetUserId { get; set; }

        [ForeignKey(nameof(TargetUserId))]
        public User? TargetUser { get; set; }

        /// <summary>
        /// Foreign key to the user who performed the action (e.g., staff/admin)
        /// </summary>
        [Required]
        public required string UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? ActorUser { get; set; }
    }
}
