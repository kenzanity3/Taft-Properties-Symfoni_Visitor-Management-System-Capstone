using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VisitorManagementSystem_Captstone.NewModel
{
    public class RoomOccupant
    {
        /// <summary>
        /// Primary key identifier for the ownership/occupancy record
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public required int OccupantId { get; set; }

        /// <summary>
        /// Date the room owner or occupant moved in
        /// </summary>
        [Required]
        public required DateOnly MoveInDate { get; set; }

        /// <summary>
        /// Current occupancy status (e.g., Active, Inactive, MovedOut)
        /// </summary>
        [Required]
        public required string OccupancyStatus { get; set; } = "Active";

        /// <summary>
        /// Type of ownership/occupancy (e.g., Owner, Tenant, Temporary)
        /// </summary>
        [Required]
        public required string OwnerType { get; set; }

        /// <summary>
        /// Date when this ownership assignment was created/approved
        /// </summary>
        [Required]
        public required DateOnly DateAssigned { get; set; } = DateOnly.FromDateTime(DateTime.Now);

        /// <summary>
        /// Date the occupant moved out (null if still occupying)
        /// </summary>
        public DateOnly? MoveOutDate { get; set; }

        /// <summary>
        /// Foreign key to the RoomOwner (who is assigned to the room)
        /// </summary>
        [Required]
        public required string OwnerId { get; set; }

        [ForeignKey(nameof(OwnerId))]
        public RoomOwner? RoomOwner { get; set; }

        /// <summary>
        /// Foreign key to the Room being occupied
        /// </summary>
        [Required]
        public required string RoomNumber { get; set; }

        [ForeignKey(nameof(RoomNumber))]
        public Room? Room { get; set; }
    }
}
