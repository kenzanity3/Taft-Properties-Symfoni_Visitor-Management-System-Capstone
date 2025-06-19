using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace VisitorManagementSystem_Captstone.NewModel
{
    public class User
    {
        /// <summary>
        /// Primary key identifier for the user, generated as a GUID string
        /// </summary>
        [Key,ValidateNever]
        public string UserId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// User's first name (required field)
        /// </summary>
        [Required]  // Cannot be null or empty; enforced in database and model
        public required string FirstName { get; set; }

        /// <summary>
        /// User's middle name (optional; can be left null)
        /// </summary>
        public string? MiddleName { get; set; }

        /// <summary>
        /// User's last name (required field)
        /// </summary>
        [Required]
        public required string LastName { get; set; }

        /// <summary>
        /// User's birth date (required field, using DateOnly for clarity)
        /// </summary>
        [Required]
        public required DateOnly DateOfBirth { get; set; }

        /// <summary>
        /// Unique username for login/identification (required field)
        /// </summary>
        [Required]
        public required string Username { get; set; }

        /// <summary>
        /// User's hashed password (required field)
        /// </summary>
        [Required]
        public required string Password { get; set; }

        /// <summary>
        /// User's email address, validated as a proper email format (required)
        /// </summary>
        [Required]
        [DataType(DataType.EmailAddress)]  // Enforces valid email pattern
        public required string Email { get; set; }

        /// <summary>
        /// Path to the user's profile picture; defaults to "default.png"
        /// </summary>
        [Required]
        public string ProfilePicture { get; set; } = "default.png";

        /// <summary>
        /// Indicates whether the account is currently active (true) or deactivated (false)
        /// </summary>
        public bool AccountStatus { get; set; } = true;

        /// <summary>
        /// User's street address (defaults to "default street")
        /// </summary>
        [Required]
        public required string StreetAddress { get; set; }

        /// <summary>
        /// User's city of residence (defaults to "default city")
        /// </summary>
        [Required]
        public required string City { get; set; }

        /// <summary>
        /// User's country of residence (defaults to "default country")
        /// </summary>
        [Required]
        public required string Country { get; set; }

        /// <summary>
        /// Date when the account was deactivated (nullable if still active)
        /// </summary>
        public DateOnly? DeactivatedDate { get; set; }

        /// <summary>
        /// AdminId of the administrator who deactivated the user (nullable).
        /// </summary>
        public string? DeactivatedBy { get; set; }
    }
}
