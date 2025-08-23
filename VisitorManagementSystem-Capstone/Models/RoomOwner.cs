using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VisitorManagementSystem_Capstone.Models
{
    /// <summary>
    /// Represents a room owner in the system.
    /// </summary>
    public class RoomOwner
    {
        /// <summary>
        /// Primary key, Foreign Key identifier for the room owner.
        /// </summary>
        [Key, ValidateNever]
        public string UserId { get; set; } = null!;

        /// <summary>
        /// Navigation property to the associated User entity.
        /// </summary>
        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        /// <summary>
        /// Path to user's profile picture; defaults to "default.png".
        /// </summary>
        [Required]
        public string RoomOwnerProfilePicture { get; set; } = "/images/default.png";

        /// <summary>
        /// Emergency contact number of the person to reach in case of emergency. Optional but must follow phone format if provided.
        /// </summary>
        [Phone]
        public string? EmergencyContactNumber { get; set; }

        /// <summary>
        /// Name of the emergency contact person. This field is optional.
        /// </summary>
        public string? EmergencyContactName { get; set; }

        /// <summary>
        /// Foreign key identifier linking the staff member to an account.
        /// </summary>
        public int AccountId { get; set; }

        /// <summary>
        /// Navigation property to the associated Account entity.
        /// </summary>
        [ForeignKey(nameof(AccountId))]
        public Account? Account { get; set; }



        /// <summary>
        /// Navigation property for rooms occupied by this owner
        /// </summary>
        public ICollection<RoomOccupant>? OccupiedRooms { get; set; }

    }
}
