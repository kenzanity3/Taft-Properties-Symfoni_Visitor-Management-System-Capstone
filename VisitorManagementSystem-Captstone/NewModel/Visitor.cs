using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VisitorManagementSystem_Captstone.NewModel
{
    public class Visitor
    {
        /// <summary>
        /// Primary key identifier for the visitor
        /// </summary>
        [Key, ValidateNever]
        public string VisitorId { get; set; } = null!;

        /// <summary>
        /// Government-issued or personal identification number provided by the visitor (e.g., driver's license number)
        /// </summary>
        [Required]
        public required string IdNumber { get; set; }

        /// <summary>
        /// Type of ID provided (e.g., Passport, Driver’s License, Company ID)
        /// </summary>
        [Required]
        public required string IDType { get; set; }

        /// <summary>
        /// Classification of the visitor (e.g., Guest, Delivery, Service)
        /// </summary>
        [Required]
        public required string VisitorType { get; set; }

        /// <summary>
        /// Indicates whether the visitor’s identity has been verified by staff
        /// </summary>
        public required bool IsVerified { get; set; } = false;

        /// <summary>
        /// The date and time when the visitor was verified by a staff member (nullable if not yet verified)
        /// </summary>
        public DateTime? VerifiedDateTime { get; set; }

        /// <summary>
        /// Foreign key to the User table (visitor's user account)
        /// </summary>
        [ValidateNever]
        public string UserId { get; set; } = null!;

        /// <summary>
        /// Navigation property to the associated User entity
        /// </summary>
        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        /// <summary>
        /// Foreign key to the Staff table indicating who verified this visitor (nullable if not verified yet)
        /// </summary>
        public string? StaffId { get; set; }

        /// <summary>
        /// Navigation property to the Staff entity who verified the visitor
        /// </summary>
        [ForeignKey(nameof(StaffId))]
        public Staff? VerifiedByStaff { get; set; }
    }
}
