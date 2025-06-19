using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VisitorManagementSystem_Captstone.NewModel
{
    public class RoomOwner
    {
        /// <summary>
        /// Primary key identifier for the room owner
        /// </summary>
        [Key, ValidateNever]
        public string OwnerId { get; set; } = null!;

        /// <summary>
        /// Unique code associated with a visit, which will be converted into a QR code for access pass scanning and tracking
        /// </summary>
        [Required]
        public required string VisitCode { get; set; }

        /// <summary>
        /// Foreign key to the User table
        /// </summary>
        [ValidateNever]
        public string UserId { get; set; } = null!;

        /// <summary>
        /// Navigation property to the associated User entity
        /// </summary>
        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }
    }
}
