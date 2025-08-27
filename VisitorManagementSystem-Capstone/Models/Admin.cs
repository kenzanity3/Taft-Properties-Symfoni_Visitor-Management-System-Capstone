using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VisitorManagementSystem_Capstone.Models
{
    /// <summary>
    /// Represents an administrative user in the system.
    /// Inherits identity and login details from the User model.
    /// </summary>
    public class Admin
    {
        /// <summary>
        /// Primary key identifier for the Admin.
        /// This is also a foreign key that references the associated User account.
        /// </summary>
        [Key, ValidateNever]
        public string UserId { get; set; } = null!;

        /// <summary>
        /// Date when the admin role was officially assigned.
        /// Used for role tracking and auditing.
        /// </summary>
        [Required]
        public DateOnly DateAssigned { get; set; } = DateOnly.FromDateTime(DateTime.Now);

        /// <summary>
        /// Navigation property linking this admin to the corresponding User entity.
        /// Allows access to shared user details such as usernameand password.
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