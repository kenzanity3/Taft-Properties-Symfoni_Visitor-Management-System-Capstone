using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VisitorManagementSystem_Captstone.NewModel
{
    public class Staff
    {
        /// <summary>
        /// Primary key identifier for the staff
        /// </summary>
        [Key, ValidateNever]
        public required string StaffId { get; set; } = null!;

        /// <summary>
        /// Date the staff member was hired
        /// </summary>
        public required DateOnly DateHired { get; set; }

        /// <summary>
        /// Job position or role of the staff member (e.g., guard, receptionist)
        /// </summary>
        public required string Position { get; set; }

        /// <summary>
        /// Tower or facility assignment for the staff (e.g., Tower A)
        /// </summary>
        public required string AssignedTower { get; set; }

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
