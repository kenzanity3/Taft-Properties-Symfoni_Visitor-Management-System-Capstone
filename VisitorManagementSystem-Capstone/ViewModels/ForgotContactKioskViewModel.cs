using VisitorManagementSystem_Capstone.Models;

namespace VisitorManagementSystem_Capstone.ViewModels
{
    public class ForgotContactKioskViewModel
    {
        public List<VisitLog> VisitLog { get; set; } = new List<VisitLog>();
        public IFormFile? ProfilePicture { get; set; }
        public string? VisitorFullName { get; set; }
        public string? RoomNumber { get; set; }
        public string? RoomOwnerName { get; set; }
        public string? PurposeOfVisit { get; set; }
        public string? ContactNumber { get; set; }
    }
}