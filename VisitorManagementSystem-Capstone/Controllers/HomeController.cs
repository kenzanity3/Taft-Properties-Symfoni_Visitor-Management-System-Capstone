using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using VisitorManagementSystem_Capstone.Data;
using VisitorManagementSystem_Capstone.Services;
using VisitorManagementSystem_Capstone.ViewModels;

namespace VisitorManagementSystem_Capstone.Controllers
{
    public class HomeController : Controller
    {
        private readonly VisitorManagementSystemDatabaseContext _context;
        private readonly EmailService _emailService;
        private readonly LocalImageService _imageService;
        private readonly POSTViewModel _postViewModel;

        public HomeController(
            VisitorManagementSystemDatabaseContext context,
            EmailService emailService,
            LocalImageService imageService)
        {
            _context = context;
            _emailService = emailService;
            _imageService = imageService;
            _postViewModel = new POSTViewModel();
        }

        public IActionResult SymfoniHome()
        {
            return View();
        }

        // HomeController.cs - Update the CheckAppointmentStatus method
        public async Task<IActionResult> CheckAppointmentStatus(string contactNumber)
        {
            try
            {
                // Validate and normalize contact number
                if (string.IsNullOrWhiteSpace(contactNumber))
                {
                    return Json(new { success = false, message = "Contact number is required" });
                }

                // Remove all non-digit characters and validate format
                var normalizedContact = new string(contactNumber.Where(char.IsDigit).ToArray());
                if (normalizedContact.Length != 11 || !normalizedContact.StartsWith("09"))
                {
                    return Json(new { success = false, message = "Invalid contact number format. Please provide an 11-digit number starting with 09" });
                }

                // Find visitor with matching contact number in a single query
                var visitor = await _context.Visitors
                    .Include(v => v.User)
                    .Include(v => v.VisitLogs)
                        .ThenInclude(vl => vl.Room)
                    .Where(v => v.User != null &&
                               v.User.ContactNumber != null &&
                               v.User.ContactNumber.Trim() == normalizedContact)
                    .Select(v => new
                    {
                        v.UserId,
                        v.User.FirstName,
                        v.User.MiddleName,
                        v.User.LastName,
                        v.User.ContactNumber,
                        VisitLogs = v.VisitLogs
                            .Where(vl => vl.logStatus) // Only active logs
                            .OrderByDescending(vl => vl.AppointmentDate)
                            .ToList()
                    })
                    .FirstOrDefaultAsync();

                if (visitor == null)
                {
                    return Json(new { success = false, message = "No visitor found with this contact number" });
                }

                // Format visitor name
                var visitorName = FormatVisitorName(visitor.FirstName, visitor.MiddleName, visitor.LastName);

                // Mask contact number (show only first 2 and last 2 digits)
                string maskedContact = MaskContactNumber(visitor.ContactNumber);

                // Handle case when no appointments exist
                if (visitor.VisitLogs == null || !visitor.VisitLogs.Any())
                {
                    return Json(new
                    {
                        success = true,
                        hasAppointments = false,
                        visitorName,
                        contactNumber = maskedContact,
                        message = "No appointments found for this visitor"
                    });
                }

                // Return all appointments (not just the latest)
                var appointments = visitor.VisitLogs.Select(appointment => new
                {
                    id = appointment.VisitLogId,
                    roomNumber = appointment.Room?.RoomNumber ?? "N/A",
                    appointmentDate = appointment.AppointmentDate?.ToString("MM/dd/yyyy"),
                    status = GetAppointmentStatus(appointment.VerificationStatus)
                }).ToList();

                return Json(new
                {
                    success = true,
                    hasAppointments = true,
                    visitorName,
                    contactNumber = maskedContact,
                    appointments
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "An error occurred while checking appointment status. Please try again later."
                });
            }
        }

        // Add this helper method to mask contact number
        private string MaskContactNumber(string contactNumber)
        {
            if (string.IsNullOrEmpty(contactNumber) || contactNumber.Length < 4)
                return contactNumber;

            // Show first 2 and last 2 digits, mask the rest
            string firstTwo = contactNumber.Substring(0, 2);
            string lastTwo = contactNumber.Substring(contactNumber.Length - 2);
            string maskedMiddle = new string('*', contactNumber.Length - 4);

            return $"{firstTwo}{maskedMiddle}{lastTwo}";
        }

        // Helper method to format visitor name
        private string FormatVisitorName(string firstName, string middleName, string lastName)
        {
            var nameParts = new List<string> { firstName };
            if (!string.IsNullOrWhiteSpace(middleName))
            {
                nameParts.Add(middleName);
            }
            nameParts.Add(lastName);
            return string.Join(" ", nameParts).Replace("  ", " ").Trim();
        }

        // Helper method to determine appointment status
        private string GetAppointmentStatus(bool? verificationStatus)
        {
            return verificationStatus switch
            {
                null => "pending",
                true => "approved",
                false => "denied"
            };
        }

        //Cancelation of appointment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelAppointment([FromBody] CancelAppointmentRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.contactNumber))
            {
                return Json(new { success = false, message = "Contact number is required" });
            }

            try
            {
                // Normalize contact number (remove non-digits)
                var normalizedContact = new string(request.contactNumber.Where(char.IsDigit).ToArray());

                // Find visitor with matching contact number
                var visitor = await _context.Visitors
                    .Include(v => v.User)
                    .FirstOrDefaultAsync(v => v.User != null &&
                                            v.User.ContactNumber != null &&
                                            v.User.ContactNumber.Trim() == normalizedContact);

                if (visitor == null)
                {
                    return Json(new { success = false, message = "No visitor found with this contact number" });
                }

                // Find the most recent pending appointment
                var appointment = await _context.VisitLogs
                    .Where(v => v.VisitorUserId == visitor.UserId &&
                               v.VerificationStatus == null && // Only pending appointments
                               v.AppointmentDate >= DateOnly.FromDateTime(DateTime.Today))
                    .OrderByDescending(v => v.AppointmentDate)
                    .FirstOrDefaultAsync();

                if (appointment == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "No pending appointments found to cancel"
                    });
                }

                // Remove the appointment record
                _context.VisitLogs.Remove(appointment);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Appointment cancelled successfully"
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "An error occurred while cancelling the appointment"
                });
            }
        }

        public class CancelAppointmentRequest
        {
            [Required(ErrorMessage = "Contact number is required")]
            [RegularExpression(@"^09\d{9}$", ErrorMessage = "Invalid contact number format")]
            public string contactNumber { get; set; }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ClearRegisterAlert()
        {
            TempData.Remove("ShowRegisterAlert");
            TempData.Remove("RegisterMessage");
            return Json(new { success = true });
        }
    }
}