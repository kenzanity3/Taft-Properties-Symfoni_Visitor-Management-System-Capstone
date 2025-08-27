using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace VisitorManagementSystem_Capstone.Models
{
    /// <summary>
    /// Represents a system user with login credentials and personal information.
    /// </summary>
    public class User
    {
        /// <summary>
        /// Primary key identifier for the user, generated as a GUID string.
        /// </summary>
        [Key, ValidateNever]
        public string? UserId { get; set; }

        /// <summary>
        /// User's first name (required field).
        /// </summary>
        [Required]
        public string? FirstName { get; set; }

        /// <summary>
        /// User's middle name (optional).
        /// </summary>
        public string? MiddleName { get; set; }

        /// <summary>
        /// User's last name (required field).
        /// </summary>
        [Required]
        public string? LastName { get; set; }

        /// <summary>
        /// Primary contact number of the user. This field is required and must be in a valid phone format.
        /// </summary>
        [Phone]
        public string? ContactNumber { get; set; }

        /// <summary>
        /// Account status: true for active, false for inactive.
        /// </summary>
        [Required]
        public bool AccountStatus { get; set; } = true;

        /// <summary>
        /// Date the user account was created.
        /// </summary>
        public DateOnly DateCreated { get; set; } = DateOnly.FromDateTime(DateTime.Now);
    }
}