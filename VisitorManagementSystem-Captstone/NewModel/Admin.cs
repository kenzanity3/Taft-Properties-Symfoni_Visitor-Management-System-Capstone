using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VisitorManagementSystem_Captstone.NewModel
{
    public class Admin
    {
        /// <summary>
        /// Primary key identifier for the Admin
        /// </summary>
        [Key, ValidateNever]
        public string AdminId { get; set; } = null!;

        /// <summary>
        /// Date when the admin role was assigned
        /// </summary>
        public DateOnly DateAssigned { get; set; } = DateOnly.FromDateTime(DateTime.Now);

        /// <summary>
        /// Foreign key linking this admin to their user account
        /// </summary>
        [ValidateNever]
        public string UserId { get; set; } = null!;

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

    }
}
