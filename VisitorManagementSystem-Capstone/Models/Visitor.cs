using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VisitorManagementSystem_Capstone.Models
{
    public class Visitor
    {
        /// <summary>
        /// Primary key, Foreign Key identifier for the visitor. 
        /// This links to the User table and uniquely identifies each visitor.
        /// </summary>
        [Key, ValidateNever]
        public string UserId { get; set; } = null!;

        /// <summary>
        /// Navigation property to the associated User entity, 
        /// allowing access to common user information such as name and credentials.
        /// </summary>
        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        /// <summary>
        /// Path to user's profile picture; defaults to "default.png".
        /// </summary>
        [Required]
        public string ProfilePicture { get; set; } = "/images/default.png";


        /// <summary>
        /// Collection of visit logs associated with this visitor.
        /// </summary>
        [ValidateNever]
        public ICollection<VisitLog> VisitLogs { get; set; } = new List<VisitLog>();
    }
}
