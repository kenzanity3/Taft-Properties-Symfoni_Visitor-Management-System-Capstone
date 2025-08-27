using System.ComponentModel.DataAnnotations;

namespace VisitorManagementSystem_Capstone.ViewModels
{
    // AppointmentViewModel.cs
    public class AppointmentViewModel
    {
        [Required(ErrorMessage = "Contact number is required")]
        [Phone(ErrorMessage = "Invalid phone number format")]
        public string ContactNumber { get; set; }

        public string? VisitCode { get; set; }

        [Required(ErrorMessage = "Room number is required")]
        public string RoomNumber { get; set; }

        [Required(ErrorMessage = "Tower is required")]
        public string Tower { get; set; }

        [Required(ErrorMessage = "Room owner full name is required")]
        public string RoomOwnerFullName { get; set; }

        [Required(ErrorMessage = "Appointment date is required")]
        [DataType(DataType.Date)]
        [FutureDate(ErrorMessage = "Appointment date must be in the future")]
        public DateOnly? AppointmentDate { get; set; }

        [Required(ErrorMessage = "Purpose of visit is required")]
        [StringLength(1000, ErrorMessage = "Purpose cannot exceed 1000 characters")]
        public string PurposeOfVisit { get; set; }
    }

    public class FutureDateAttribute : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            if (value is DateOnly date)
            {
                return date > DateOnly.FromDateTime(DateTime.Now);
            }
            return false;
        }
    }
}