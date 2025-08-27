using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VisitorManagementSystem_Capstone.Models
{
    /// <summary>
    /// Represents a record of room ownership or occupancy by a RoomOwner.
    /// </summary>
    public class RoomOccupant
    {
        /// <summary>
        /// Primary key identifier for the ownership/occupancy record.
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OccupantId { get; set; }

        /// <summary>
        /// Date the room owner or occupant moved in.
        /// </summary>
        [Required]
        public DateOnly? MoveInDate { get; set; }

        /// <summary>
        /// Date the occupant moved out (null if still occupying).
        /// </summary>
        public DateOnly? MoveOutDate { get; set; }

        /// <summary>
        /// Date when this ownership assignment was created/approved.
        /// </summary>
        [Required]
        public DateOnly DateAssigned { get; set; } = DateOnly.FromDateTime(DateTime.Now);

        /// <summary>
        /// true for Active, false for Inactive, null for MovedOut.
        /// </summary>
        [Required]
        public bool OccupationStatus { get; set; }

        /// <summary>
        /// Foreign key to the RoomOwner (who is assigned to the room).
        /// </summary>
        [Required]
        public string? OwnerUserId { get; set; }

        [ForeignKey(nameof(OwnerUserId))]
        public RoomOwner? RoomOwner { get; set; }

        /// <summary>
        /// Foreign key to the Room being occupied.
        /// </summary>
        [Required]
        public int RoomId { get; set; }

        [ForeignKey(nameof(RoomId))]
        public Room? Room { get; set; }
    }
}