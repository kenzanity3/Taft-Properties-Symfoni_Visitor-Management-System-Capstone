using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem_Capstone.Data;
using VisitorManagementSystem_Capstone.Models;
using VisitorManagementSystem_Capstone.Services;
using VisitorManagementSystem_Capstone.ViewModels;

namespace VisitorManagementSystem_Capstone.Controllers
{
    public class VisitorAppointmentController : Controller
    {
        private readonly VisitorManagementSystemDatabaseContext _context;

        public VisitorAppointmentController(VisitorManagementSystemDatabaseContext context)
        {
            _context = context;
        }

        public IActionResult AppointmentSet()
        {
            ViewData["Title"] = "Set Appointment";
            return View();
        }

        // VisitorAppointmentController.cs
        [HttpPost]
        public async Task<IActionResult> SubmitAppointment(AppointmentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("AppointmentSet", model);
            }

            // Validate purpose of visit
            if (string.IsNullOrWhiteSpace(model.PurposeOfVisit))
            {
                TempData["ErrorMessage"] = "Purpose of visit is required.";
                ModelState.AddModelError("PurposeOfVisit", "Purpose of visit is required");
                return View("AppointmentSet", model);
            }
            // Trim the purpose to remove any accidental whitespace
            model.PurposeOfVisit = model.PurposeOfVisit.Trim();
            // Check if visitor has active appointment
            bool hasActiveAppointment = await HasActiveAppointment(model.ContactNumber);
            if (hasActiveAppointment)
            {
                TempData["ErrorMessage"] = "You already have an active appointment. Please wait until your current appointment is completed, canceled, or past due before creating a new one.";
                return View("AppointmentSet", model);
            }

            POSTViewModel viewModel = new POSTViewModel();

            // Check visitor exists
            var visitor = (await viewModel.GetDataListAsync(_context, 2))
                .OfType<Visitor>()
                .FirstOrDefault(v => v.User.ContactNumber == model.ContactNumber);

            if (visitor == null)
            {
                TempData["ErrorMessage"] = "Visitor not found with this contact number. Please register first.";
                return View("AppointmentSet", model);
            }

            // Find room
            var room = (await viewModel.GetDataListAsync(_context, 6)).OfType<Room>()
                .FirstOrDefault(r => r.RoomNumber == model.RoomNumber);

            if (room == null)
            {
                TempData["ErrorMessage"] = "Invalid room number. Please check and try again.";
                return View("AppointmentSet", model);
            }

            var checkoccupant = (await viewModel.GetDataListAsync(_context, 10))
                                   .OfType<RoomOccupant>()
                                   .Where(ro => ro.RoomId == room.RoomId &&
                                   (ro.RoomOwner.User.FirstName.ToLower() + " " + ro.RoomOwner.User.LastName.ToLower()).Equals(model.RoomOwnerFullName.ToLower()))
                                   .FirstOrDefault();

            if (checkoccupant == null)
            {
                TempData["ErrorMessage"] = "No room owner found with that name. Please check and try again";
                return View("AppointmentSet", model);
            }

            // Check for existing appointment using the new method
            // Check for existing appointment
            bool hasExistingAppointment = await HasExistingAppointment(visitor.UserId, room.RoomId, model.AppointmentDate.Value);

            if (hasExistingAppointment)
            {
                TempData["ErrorMessage"] = "You already have an active appointment for this room on the selected date. Please wait until your current appointment is completed or canceled.";
                return View("AppointmentSet", model);
            }

            // Handle visit code validation if provided
            bool isVerified = false;
            if (!string.IsNullOrEmpty(model.VisitCode))
            {
                if (!OtpService.ValidateRoomOwnerOtp(model.VisitCode, visitor.UserId))
                {
                    TempData["ErrorMessage"] = "Invalid or expired visit code. Please check and try again.";
                    return View("AppointmentSet", model);
                }
                isVerified = true;
            }

            // Create visit log
            var visitLog = new VisitLog
            {
                VisitorUserId = visitor.UserId,
                OwnerUserId = checkoccupant.RoomOwner.UserId,
                RoomId = room.RoomId,
                AppointmentDate = model.AppointmentDate,
                PurposeOfVisit = model.PurposeOfVisit.Trim(), // Ensure trimmed
                VerificationStatus = isVerified ? true : null,
                VerifiedDateTime = isVerified ? DateTime.Now : null,
                IssueDate = DateOnly.FromDateTime(DateTime.Now)
            };

            _context.VisitLogs.Add(visitLog);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = isVerified
                ? $"Appointment successfully set for {visitor.User.FirstName} {visitor.User.LastName}"
                : $"Appointment request submitted for {visitor.User.FirstName} {visitor.User.LastName}. Waiting for room owner approval.";

            return RedirectToAction("AppointmentSet");
        }

        // VisitorAppointmentController.cs - Add this method
        [HttpGet]
        public async Task<IActionResult> CheckActiveAppointment(string contactNumber)
        {
            if (string.IsNullOrWhiteSpace(contactNumber))
            {
                return Json(new { success = false, hasActiveAppointment = false });
            }

            try
            {
                bool hasActiveAppointment = await HasActiveAppointment(contactNumber);
                return Json(new { success = true, hasActiveAppointment });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, hasActiveAppointment = false });
            }
        }

        // VisitorAppointmentController.cs - Update this method
        private async Task<bool> HasExistingAppointment(string visitorUserId, int roomId, DateOnly appointmentDate)
        {
            return await _context.VisitLogs
                .Include(vl => vl.CheckInOut)
                .AnyAsync(vl =>
                    vl.VisitorUserId == visitorUserId &&
                    vl.RoomId == roomId &&
                    vl.AppointmentDate == appointmentDate &&
                    vl.logStatus && // Only active logs
                    (vl.VerificationStatus == null || vl.VerificationStatus == true) && // Pending or approved
                    (vl.CheckInOut == null || // No check-in record OR
                     (vl.CheckInOut.CheckOutDateTime == null && // Checked in but not checked out
                      vl.AppointmentDate >= DateOnly.FromDateTime(DateTime.Now))) // And appointment is not past
                );
        }

        // Check if the visitor has an existing appointment
        private async Task<bool> HasActiveAppointment(string contactNumber)
        {
            var visitor = await _context.Visitors
                .Include(v => v.User)
                .FirstOrDefaultAsync(v => v.User != null &&
                                        v.User.ContactNumber != null &&
                                        v.User.ContactNumber.Trim() == contactNumber.Trim());

            if (visitor == null) return false;

            return await _context.VisitLogs
                .Include(vl => vl.CheckInOut)
                .AnyAsync(vl =>
                    vl.VisitorUserId == visitor.UserId &&
                    vl.logStatus && // Active logs only
                    (vl.VerificationStatus == null || vl.VerificationStatus == true) && // Pending or approved
                    (vl.CheckInOut == null || // No check-in record OR
                     (vl.CheckInOut.CheckOutDateTime == null && // Checked in but not checked out
                      vl.AppointmentDate >= DateOnly.FromDateTime(DateTime.Now))) // And appointment is not past
                );
        }

        //Fetch Room Owner Details
        [HttpGet]
        public async Task<IActionResult> GetRoomOwnerByVisitCode(string visitCode)
        {
            if (string.IsNullOrWhiteSpace(visitCode))
                return Json(new { success = false, message = "Visit code is required." });

            var otpEntry = OtpService.GetOtpEntry(visitCode);
            if (otpEntry == null)
                return Json(new { success = false, message = "Invalid or expired visit code." });

            // Get the room owner associated with this OTP
            var roomOwner = await _context.RoomOwners
                .Include(ro => ro.User)
                .FirstOrDefaultAsync(ro => ro.UserId == otpEntry.Id);

            if (roomOwner == null || roomOwner.User == null)
                return Json(new { success = false, message = "Room owner not found." });

            // Get the room assigned to this owner
            var roomOccupancy = await _context.RoomOccupants
                .Include(ro => ro.Room)
                .FirstOrDefaultAsync(ro => ro.OwnerUserId == roomOwner.UserId &&
                                         ro.OccupationStatus == true);

            if (roomOccupancy?.Room == null)
                return Json(new { success = false, message = "No active room assignment found for this owner." });

            return Json(new
            {
                success = true,
                fullName = $"{roomOwner.User.FirstName} {roomOwner.User.LastName}",
                tower = roomOccupancy.Room.Tower,
                roomNumber = roomOccupancy.Room.RoomNumber,
                floorLevel = roomOccupancy.Room.FloorLevel
            });
        }

        //Profile Pic Preview
        [HttpGet]
        public async Task<IActionResult> GetVisitorProfile(string contactNumber)
        {
            if (string.IsNullOrWhiteSpace(contactNumber))
            {
                return Json(new { success = false, message = "Contact number is required" });
            }
            try
            {
                var normalizedContact = contactNumber.Trim();
                var visitor = await _context.Visitors
                    .Include(v => v.User)
                    .FirstOrDefaultAsync(v => v.User != null &&
                                             v.User.ContactNumber != null &&
                                             v.User.ContactNumber.Trim() == normalizedContact);

                if (visitor == null || visitor.User == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Visitor not found with this contact number"
                    });
                }

                return Json(new
                {
                    success = true,
                    profilePicture = string.IsNullOrEmpty(visitor.ProfilePicture)
                                 ? "/images/default.png"
                                 : visitor.ProfilePicture,
                    fullName = $"{visitor.User.FirstName ?? ""} {visitor.User.LastName ?? ""}"
                });
            }
            catch (Exception ex)
            {
                // Log the exception here if you have logging configured
                return Json(new
                {
                    success = false,
                    message = "An error occurred while fetching visitor profile"
                });
            }
        }
    }
}