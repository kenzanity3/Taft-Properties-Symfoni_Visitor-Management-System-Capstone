using System.ComponentModel.DataAnnotations;

namespace VisitorManagementSystem_Capstone.ViewModels
{  
    // ViewModel for visitor request
    public class VisitorRequestViewModel
    {
        [Required]
        public string FirstName { get; set; }

        public string MiddleName { get; set; }

        [Required]
        public string LastName { get; set; }

        // ContactNumber is optional and will be set to null
        public string? ContactNumber { get; set; }

        public IFormFile ProfilePicture { get; set; }

        [Required]
        public string Tower { get; set; }

        [Required]
        public string RoomNumber { get; set; }

        [Required]
        [MaxLength(1000)]
        public string PurposeOfVisit { get; set; }
    }
 }

