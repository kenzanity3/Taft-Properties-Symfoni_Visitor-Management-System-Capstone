// Provides data annotation attributes for model validation
using System.ComponentModel.DataAnnotations;

namespace VisitorManagementSystem_Captstone.Models
{
    /// <summary>
    /// Represents a user in the Visitor Management System
    /// </summary>
    public class User
    {
        /// <summary>
        /// Primary key identifier for the user
        /// </summary>
        [Key,]  // Marks this property as the primary key in the database
        public int UserId { get; set; }

        /// <summary>
        /// User's first name (required field)
        /// </summary>
        [Required]  // Indicates this field cannot be null or empty
        public string FirstName { get; set; }

        /// <summary>
        /// User's middle name (required field)
        /// </summary>
        [Required]
        public string MiddleName { get; set; }

        /// <summary>
        /// Unique username for login/identification (required field)
        /// </summary>
        [Required]
        public string LastName { get; set; }

        /// <summary>
        /// Unique username for login/identification (required field)
        /// </summary>
        [Required]

        public string Username { get; set; }

        /// <summary>
        /// User's password (nullable for cases like admin-created accounts)
        /// </summary>
        public string? Password { get; set; }  // The ? makes it nullable

        /// <summary>
        /// Type of user account (e.g., "Admin", "Staff", "Visitor")
        /// </summary>
        [Required]
        public string AccountType { get; set; }

        /// <summary>
        /// User's email address (required and validated as proper email format)
        /// </summary>
        [Required]
        [DataType(DataType.EmailAddress)]  // Validates the string is in email format
        public string Email { get; set; }

        /// <summary>
        /// Flag indicating whether the user account is active
        /// </summary>
        [Required]
        public bool IsActive { get; set; }
    }
}