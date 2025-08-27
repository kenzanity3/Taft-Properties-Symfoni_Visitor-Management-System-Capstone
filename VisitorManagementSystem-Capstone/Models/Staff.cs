using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VisitorManagementSystem_Capstone.Models
{
    /// <summary>
    /// Represents a staff member within the facility, including their hiring details and job role.
    /// </summary>
    public class Staff
    {
        /// <summary>
        /// Primary key and foreign key identifier for the staff member (linked to User).
        /// </summary>
        [Key, ValidateNever]
        public string? UserId { get; set; } = null!;

        /// <summary>
        /// Date the staff member was hired.
        /// </summary>
        [Required]
        public DateOnly DateHired { get; set; }

        /// <summary>
        /// Foreign key to the staff member's job position or role (e.g., guard, receptionist).
        /// </summary>
        [Required]
        public string? Position { get; set; }

        /// <summary>
        /// Navigation property to the associated User entity.
        /// </summary>
        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        /// <summary>
        /// Foreign key identifier linking the staff member to an account.
        /// </summary>
        public int AccountId { get; set; }

        /// <summary>
        /// Navigation property to the associated Account entity.
        /// </summary>
        [ForeignKey(nameof(AccountId))]
        public Account? Account { get; set; }
    }
}