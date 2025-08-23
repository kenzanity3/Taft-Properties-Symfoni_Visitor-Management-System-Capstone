using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VisitorManagementSystem_Capstone.Models
{
    /// <summary>
    /// Represents a specific room within a floor of the facility.
    /// </summary>
    public class Room
    {
        /// <summary>
        /// Unique room id.
        /// </summary>
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RoomId { get; set; }
        /// <summary>
        /// room number  (e.g., 101, 302).
        /// </summary>
        public required string RoomNumber { get; set; }

        /// <summary>
        /// Floor level where the room is located.
        /// </summary>
        [Required]
        public string? FloorLevel { get; set; }

        [Required]
        public string? Tower { get; set; }
    }
}
