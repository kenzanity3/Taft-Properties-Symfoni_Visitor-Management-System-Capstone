using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VisitorManagementSystem_Captstone.NewModel
{
    public class Room
    {
        /// <summary>
        /// Unique room number identifier (e.g., 101, 302)
        /// </summary>
        [Key]
        public required string RoomNumber { get; set; }

        /// <summary>
        /// Floor level where the room is located
        /// </summary>
        [Required]
        public required int FloorLevel { get; set; }

        /// <summary>
        /// Name or label of the tower/building the room belongs to (e.g., Tower A)
        /// </summary>
        [Required]
        public required string Tower { get; set; }
        [Required]
        public required int Capacity { get; set; } = 5;
        public required string RoomType { get; set; } = "Conference";   


    }
}
