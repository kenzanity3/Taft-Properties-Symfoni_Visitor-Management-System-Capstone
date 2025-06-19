using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VisitorManagementSystem_Captstone.NewModel
{
    public class FacilityLog
    {
        /// <summary>
        /// Primary key for each facility usage record
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int FacilityLogId { get; set; }

        /// <summary>
        /// Foreign key to the facility being used
        /// </summary>
        [Required]
        public required string FacilityId { get; set; }

        [ForeignKey(nameof(FacilityId))]
        public Facility? Facility { get; set; }

        /// <summary>
        /// Foreign key to the visitor using the facility
        /// </summary>
        [Required]
        public required string VisitorId { get; set; }

        [ForeignKey(nameof(VisitorId))]
        public Visitor? Visitor { get; set; }

        /// <summary>
        /// Foreign key to the staff member who approved or logged the entry
        /// </summary>
        [Required]
        public required string StaffId { get; set; }

        [ForeignKey(nameof(StaffId))]
        public Staff? Staff { get; set; }

        /// <summary>
        /// Time the visitor entered the facility
        /// </summary>
        [Required]
        public required DateTime TimeIn { get; set; } = DateTime.Now;

        /// <summary>
        /// Time the visitor exited the facility
        /// </summary>
        public DateTime? TimeOut { get; set; }

        /// <summary>
        /// Current status of the visit (e.g., Pending, Approved, Denied, Completed)
        /// </summary>
        [Required]
        public required string Status { get; set; } = "Pending";
    }
}
