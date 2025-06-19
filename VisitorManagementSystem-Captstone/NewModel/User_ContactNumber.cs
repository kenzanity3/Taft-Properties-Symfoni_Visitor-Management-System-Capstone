using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VisitorManagementSystem_Captstone.NewModel
{
    public class User_ContactNumber
    {
        /// <summary>
        /// The contact number associated with the user
        /// </summary>
        [Key]
        [Phone]
        public required string ContactNumber { get; set; }

        /// <summary>
        /// Foreign key to the User table
        /// </summary>
        [ValidateNever]
        public  string UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

    }
}
