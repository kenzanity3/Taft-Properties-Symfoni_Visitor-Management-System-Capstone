using Microsoft.AspNetCore.Mvc;
using VisitorManagementSystem_Capstone.Data;
using VisitorManagementSystem_Capstone.Models;
using VisitorManagementSystem_Capstone.Services;
using VisitorManagementSystem_Capstone.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace VisitorManagementSystem_Capstone.Controllers
{
    public class KioskController : Controller
    {
        private readonly VisitorManagementSystemDatabaseContext _context;
        private readonly LocalImageService _imageService;

        public KioskController(
            VisitorManagementSystemDatabaseContext context,
            LocalImageService imageService)
        {
            _context = context;
            _imageService = imageService;
        }

        // GET: Kiosk Home
        public IActionResult KioskHome()
        {
            var viewModel = new KioskHomeViewModel
            {
                CheckIns = new List<CheckInOutViewModel>(),
                CheckOuts = new List<CheckInOutViewModel>()
            };
            return View(viewModel);
        }

        // GET: Kiosk Check-In/Out
        // Update the KioskCheckInOut action to properly separate check-ins and check-outs
        public async Task<IActionResult> KioskCheckInOut()
        {
            try
            {
                // Get today's active check-ins (approved, checked-in, but not checked out)
                var checkIns = await _context.VisitLogs
                    .Include(v => v.Visitor)
                    .ThenInclude(v => v.User)
                    .Include(v => v.CheckInOut)
                    .Where(v => v.IssueDate == DateOnly.FromDateTime(DateTime.Today) &&
                               v.logStatus &&
                               v.VerificationStatus == true && // Only approved appointments
                               v.CheckInOut != null && // Has checked in
                               v.CheckInOut.CheckOutDateTime == null) // Has not checked out
                    .Select(v => new CheckInOutViewModel
                    {
                        VisitorName = v.Visitor.User.FirstName + " " + v.Visitor.User.LastName,
                        CheckInTime = v.CheckInOut.CheckInDateTime.ToString("hh:mm tt"),
                        CheckOutTime = "Still Checked In",
                        VisitLogId = v.VisitLogId
                    })
                    .ToListAsync();

                // Get today's check-outs (approved and checked out)
                var checkOuts = await _context.VisitLogs
                    .Include(v => v.Visitor)
                    .ThenInclude(v => v.User)
                    .Include(v => v.CheckInOut)
                    .Where(v => v.IssueDate == DateOnly.FromDateTime(DateTime.Today) &&
                               v.logStatus &&
                               v.VerificationStatus == true && // Only approved appointments
                               v.CheckInOut != null &&
                               v.CheckInOut.CheckOutDateTime.HasValue) // Has checked out
                    .Select(v => new CheckInOutViewModel
                    {
                        VisitorName = v.Visitor.User.FirstName + " " + v.Visitor.User.LastName,
                        CheckInTime = v.CheckInOut.CheckInDateTime.ToString("hh:mm tt"),
                        CheckOutTime = v.CheckInOut.CheckOutDateTime.Value.ToString("hh:mm tt"),
                        VisitLogId = v.VisitLogId
                    })
                    .ToListAsync();

                var viewModel = new KioskHomeViewModel
                {
                    CheckIns = checkIns,
                    CheckOuts = checkOuts
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                // Log error

                // Return empty view model on error
                var viewModel = new KioskHomeViewModel
                {
                    CheckIns = new List<CheckInOutViewModel>(),
                    CheckOuts = new List<CheckInOutViewModel>()
                };
                return View(viewModel);
            }
        }

        // GET: Kiosk Sign Up
        public IActionResult KioskSignUp()
        {
            return View(new POSTViewModel());
        }

        // POST: Kiosk Sign Up
        [HttpPost]
        public async Task<IActionResult> KioskSignUp(POSTViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Default picture
                    string profilePicturePath = "/images/default.png";

                    // If a file was uploaded, try saving it
                    if (viewModel.ProfilePictureFile != null && viewModel.ProfilePictureFile.Length > 0)
                    {
                        try
                        {
                            var uploadedPath = await _imageService.UploadImageAsync(viewModel.ProfilePictureFile);
                            if (!string.IsNullOrEmpty(uploadedPath))
                            {
                                profilePicturePath = uploadedPath;
                            }
                        }
                        catch (Exception ex)
                        {
                            ModelState.AddModelError("ProfilePictureFile", $"Image upload failed: {ex.Message}");
                            return View(viewModel);
                        }
                    }

                    // Use POSTViewModel's thread-safe method to generate ID
                    var postViewModel = new POSTViewModel();
                    string newUserId = await postViewModel.GenerateNextVisitorIdAsync(_context, "VIS");

                    // Add User
                    var user = new User
                    {
                        UserId = newUserId,
                        FirstName = viewModel.User.FirstName,
                        MiddleName = viewModel.User.MiddleName,
                        LastName = viewModel.User.LastName,
                        ContactNumber = viewModel.User.ContactNumber,
                        DateCreated = DateOnly.FromDateTime(DateTime.Now),
                        AccountStatus = true,
                    };

                    // Add Visitor
                    var visitor = new Visitor
                    {
                        UserId = user.UserId,
                        ProfilePicture = profilePicturePath
                    };

                    // Prepare the ViewModel and UserDataBundle
                    postViewModel = new POSTViewModel
                    {
                        User = user,
                        Visitor = visitor,
                        userbundle = new UserDataBundle
                        {
                            User = user,
                            Visitor = visitor
                        }
                    };

                    // Register the user as Visitor (mode = 1)
                    var errors = await postViewModel.RegisterUserAsync(_context, 1);

                    if (errors.Any())
                    {
                        foreach (var error in errors)
                            ModelState.AddModelError(string.Empty, error);
                        return View(viewModel);
                    }

                    // Return success response
                    return Json(new { success = true, message = $"Welcome {user.FirstName} {user.LastName}! Your registration was successful." });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = $"Registration failed: {ex.Message}" });
                }
            }

            // Return validation errors
            var errorList = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return Json(new { success = false, messages = errorList });
        }

        // Check if visitor exists by contact number
        [HttpGet]
        public async Task<IActionResult> CheckVisitorStatus(string contactNumber)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.ContactNumber == contactNumber);

                if (user == null)
                {
                    return Json(new { success = false, message = "Visitor not found" });
                }

                var visitor = await _context.Visitors
                    .Include(v => v.User)
                    .FirstOrDefaultAsync(v => v.UserId == user.UserId);

                if (visitor == null)
                {
                    return Json(new { success = false, message = "Visitor not found" });
                }

                // Check if visitor is currently checked in
                var isCheckedIn = await _context.VisitLogs
                    .AnyAsync(v => v.VisitorUserId == visitor.UserId &&
                                  v.logStatus &&
                                  v.CheckInOut != null &&
                                  v.CheckInOut.CheckOutDateTime == null);

                return Json(new
                {
                    success = true,
                    visitor = new
                    {
                        fullName = $"{visitor.User.FirstName} {visitor.User.LastName}",
                        profilePicture = visitor.ProfilePicture,
                        isCheckedIn = isCheckedIn
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error checking visitor status: {ex.Message}" });
            }
        }

        // Add these methods to your KioskController class

        // Check if visitor has approved appointment
        [HttpGet]
        public async Task<IActionResult> CheckAppointmentApproval(string contactNumber)
        {
            try
            {
                // Find user by contact number
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.ContactNumber == contactNumber);

                if (user == null)
                {
                    return Json(new { success = false, message = "Visitor not found" });
                }

                // Find approved appointment for today
                var approvedAppointment = await _context.VisitLogs
                    .Include(v => v.Room)
                    .Include(v => v.RoomOwner)
                    .ThenInclude(ro => ro.User)
                    .FirstOrDefaultAsync(v => v.VisitorUserId == user.UserId &&
                                            v.IssueDate == DateOnly.FromDateTime(DateTime.Today) &&
                                            v.VerificationStatus == true &&
                                            v.logStatus);

                if (approvedAppointment == null)
                {
                    return Json(new
                    {
                        success = false,
                        hasApprovedAppointment = false,
                        message = "No approved appointment found for today"
                    });
                }

                return Json(new
                {
                    success = true,
                    hasApprovedAppointment = true,
                    appointment = new
                    {
                        roomNumber = approvedAppointment.Room?.RoomNumber,
                        roomOwnerName = $"{approvedAppointment.RoomOwner?.User?.FirstName} {approvedAppointment.RoomOwner?.User?.LastName}",
                        purpose = approvedAppointment.PurposeOfVisit
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error checking appointment: {ex.Message}" });
            }
        }

        // Enhanced Process Check-In to handle both appointment and walk-in
        [HttpPost]
        public async Task<IActionResult> ProcessCheckIn([FromBody] CheckInRequest request)
        {
            try
            {
                // Find user by contact number
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.ContactNumber == request.ContactNumber);

                if (user == null)
                {
                    return Json(new { success = false, message = "Visitor not found" });
                }

                // Find visitor
                var visitor = await _context.Visitors
                    .FirstOrDefaultAsync(v => v.UserId == user.UserId);

                if (visitor == null)
                {
                    return Json(new { success = false, message = "Visitor not found" });
                }

                // Check if visitor is already checked in
                var isAlreadyCheckedIn = await _context.VisitLogs
                    .AnyAsync(v => v.VisitorUserId == visitor.UserId &&
                                  v.logStatus == true &&
                                  v.CheckInOut != null &&
                                  v.CheckInOut.CheckOutDateTime == null &&
                                  (v.AppointmentDate != null || request.IsAppointment && // Check appointment date only for appointments
                                  v.AppointmentDate == DateOnly.FromDateTime(DateTime.Today)) ||
                                  (v.IssueDate == DateOnly.FromDateTime(DateTime.Today))
                                  );

                if (isAlreadyCheckedIn)
                {
                    return Json(new { success = false, message = "Visitor is already checked in." });
                }

                VisitLog visitLog = null;

                // For appointments, find the approved visit log
                if (request.IsAppointment)
                {
                    visitLog = await _context.VisitLogs
                        .Include(v => v.Room)
                        .Include(v => v.RoomOwner)
                        .ThenInclude(ro => ro.User)
                        .FirstOrDefaultAsync(v => v.VisitorUserId == visitor.UserId &&
                                                v.AppointmentDate == DateOnly.FromDateTime(DateTime.Today) &&
                                                v.VerificationStatus == true &&
                                                v.CheckInOut == null &&
                                                v.CheckInOut.CheckInDateTime == null &&
                                                v.logStatus);

                    if (visitLog == null)
                    {
                        return Json(new { success = false, message = "No approved appointment found" });
                    }
                }
                else
                {
                    // For walk-ins, we need the room information
                    var viewmodel = new POSTViewModel();
                    var roomId = await GetRoomIdFromTowerAndUnit(request.Tower, request.UnitNumber);

                    // Find room occupant by room ID and host name
                    var checkoccupant = (await viewmodel.GetDataListAsync(_context, 10))
                                        .OfType<RoomOccupant>()
                                        .Where(ro => ro.RoomId == roomId &&
                                        (ro.RoomOwner.User.FirstName.ToLower() + " " + ro.RoomOwner.User.LastName.ToLower())
                                            .Equals(request.HostName.ToLower()))
                                        .FirstOrDefault();

                    if (checkoccupant == null)
                    {
                        return Json(new { success = false, message = "No room owner found with that name. Please check and try again" });
                    }

                    // Check if visit code is provided and validate it
                    bool? isApproved = null;
                    if (!string.IsNullOrEmpty(request.VisitCode))
                    {
                        if (string.IsNullOrEmpty(request.VisitCode) || request.VisitCode.Length != 6)
                        {
                            return Json(new { success = false, message = "Invalid visit code format" });
                        }

                        // Use OtpService to validate the visit code
                        bool isValidVisitCode = OtpService.ValidateRoomOwnerOtp(request.VisitCode, checkoccupant.RoomOwner.UserId);

                        if (!isValidVisitCode)
                        {
                            return Json(new { success = false, message = "Invalid or expired visit code" });
                        }

                        isApproved = true; // Auto-approve if valid visit code
                    }

                    // For walk-ins, create a new visit log
                    visitLog = new VisitLog
                    {
                        VisitorUserId = visitor.UserId,
                        OwnerUserId = checkoccupant.RoomOwner.UserId,
                        RoomId = roomId,
                        PurposeOfVisit = request.PurposeVisit,
                        IssueDate = DateOnly.FromDateTime(DateTime.Today),
                        AppointmentDate = DateOnly.FromDateTime(DateTime.Today),
                        CreatedBy = "kiosk",
                        logStatus = true,
                        VerificationStatus = isApproved // Auto-approve only if visit code is valid
                    };

                    _context.VisitLogs.Add(visitLog);
                    await _context.SaveChangesAsync();

                    // If no visit code was provided, generate an OTP for room owner approval
                    if (string.IsNullOrEmpty(request.VisitCode))
                    {
                        string otpCode = OtpService.GenerateRoomOwnerOtp(checkoccupant.RoomOwner.UserId, 1);
                        // In a real implementation, you might want to send this OTP to the room owner
                    }
                }
                if (visitLog.VerificationStatus == true)
                {
                    // Create check-in record
                    var checkInOut = new CheckInOut
                    {
                        CheckInDateTime = DateTime.Now
                    };

                    visitLog.CheckInOut = checkInOut;
                    await _context.SaveChangesAsync();
                }

                return Json(new
                {
                    success = true,
                    message = "Check-in successful",
                    isAppointment = request.IsAppointment,
                    needsApproval = string.IsNullOrEmpty(request.VisitCode) && !request.IsAppointment
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Check-in failed: {ex.Message}" });
            }
        }

        // Helper method to get room ID from tower and unit number
        private async Task<int> GetRoomIdFromTowerAndUnit(string tower, string unitNumber)
        {
            // Validate input parameters
            if (string.IsNullOrEmpty(unitNumber))
            {
                throw new ArgumentException("Unit number cannot be null or empty");
            }

            var room = await _context.Rooms
                .FirstOrDefaultAsync(r => r.RoomNumber == unitNumber);

            if (room == null)
            {
                // Validate that unitNumber has at least 1 character before calling Substring
                string floorLevel = !string.IsNullOrEmpty(unitNumber) && unitNumber.Length >= 1
                    ? unitNumber.Substring(0, 1)
                    : "1"; // Default to floor 1 if unit number is invalid

                // Create a new room for this tower/unit
                room = new Room
                {
                    RoomNumber = unitNumber,
                    FloorLevel = floorLevel,
                    Tower = tower ?? "Unknown" // Provide default if tower is null
                };

                _context.Rooms.Add(room);
                await _context.SaveChangesAsync();
            }

            return room.RoomId;
        }

        // In KioskController.cs
        [HttpGet]
        public async Task<JsonResult> GetRoomOwnerDetails(string unitNumber, string tower)
        {
            try
            {
                var room = await _context.Rooms
                    .FirstOrDefaultAsync(r => r.RoomNumber == unitNumber && r.Tower == tower);

                if (room == null)
                {
                    return Json(new { success = false, message = "Room not found" });
                }

                var roomOccupant = await _context.RoomOccupants
                    .Include(ro => ro.RoomOwner)
                    .ThenInclude(ro => ro.User)
                    .FirstOrDefaultAsync(ro => ro.RoomId == room.RoomId &&
                                             ro.OccupationStatus &&
                                             ro.MoveOutDate == null);

                if (roomOccupant != null && roomOccupant.RoomOwner != null)
                {
                    return Json(new
                    {
                        success = true,
                        roomOwnerName = $"{roomOccupant.RoomOwner.User.FirstName} {roomOccupant.RoomOwner.User.LastName}"
                    });
                }

                return Json(new { success = false, message = "Room owner not found" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // Process Check-Out
        [HttpPost]
        public async Task<IActionResult> ProcessCheckOut([FromBody] CheckOutRequest request)
        {
            try
            {
                // Find user by contact number
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.ContactNumber == request.ContactNumber);

                if (user == null)
                {
                    return Json(new { success = false, message = "Visitor not found" });
                }

                // Find active visit log with check-in but no check-out
                var visitLog = await _context.VisitLogs
                    .Include(v => v.CheckInOut)
                    .FirstOrDefaultAsync(v => v.VisitorUserId == user.UserId &&
                                            v.logStatus == true &&
                                            v.VerificationStatus == true &&
                                            (v.AppointmentDate != null || v.AppointmentDate == DateOnly.FromDateTime(DateTime.Today)) ||
                                            (v.IssueDate == DateOnly.FromDateTime(DateTime.Today) || v.CheckInOut!.CheckInDateTime.Date == DateTime.Today) &&
                                            v.CheckInOut != null &&
                                            v.CheckInOut.CheckOutDateTime == null);

                if (visitLog == null)
                {
                    return Json(new { success = false, message = "No active check-in found" });
                }

                // Update check-out time
                visitLog.CheckInOut.CheckOutDateTime = DateTime.Now;
                _context.VisitLogs.Update(visitLog);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Check-out successful" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Check-out failed: {ex.Message}" });
            }
        }

        // Get today's statistics
        [HttpGet]
        public async Task<IActionResult> GetTodayStats()
        {
            try
            {
                var today = DateOnly.FromDateTime(DateTime.Today);

                // Visitors today
                var visitorsToday = await _context.VisitLogs
                    .Where(v => v.IssueDate == today && v.logStatus)
                    .Select(v => v.VisitorUserId)
                    .Distinct()
                    .CountAsync();

                // Currently checked in
                var checkedIn = await _context.VisitLogs
                    .Where(v => v.IssueDate == today &&
                               v.logStatus &&
                               v.VerificationStatus == true &&
                               v.CheckInOut != null &&
                               v.CheckInOut.CheckOutDateTime == null)
                    .CountAsync();

                // Facilities in use (placeholder - you'll need to implement your facility logic)
                var facilitiesInUse = 0;

                return Json(new
                {
                    success = true,
                    visitorsToday = visitorsToday,
                    currentlyCheckedIn = checkedIn,
                    facilitiesInUse = facilitiesInUse
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    visitorsToday = 0,
                    currentlyCheckedIn = 0,
                    facilitiesInUse = 0
                });
            }
        }

        // Get visitor profile by contact number
        [HttpGet]
        public async Task<IActionResult> GetVisitorProfile(string contactNumber)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.ContactNumber == contactNumber);

                if (user == null)
                {
                    return Json(new { success = false, message = "Visitor not found" });
                }

                var visitor = await _context.Visitors
                    .FirstOrDefaultAsync(v => v.UserId == user.UserId);

                if (visitor == null)
                {
                    return Json(new { success = false, message = "Visitor not found" });
                }

                return Json(new
                {
                    success = true,
                    fullName = $"{user.FirstName} {user.LastName}",
                    profilePicture = visitor.ProfilePicture
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error fetching profile: {ex.Message}" });
            }
        }

        // NEW: Check appointment status by contact number (same as SymfoniHome functionality)
        [HttpGet]
        public async Task<IActionResult> CheckAppointmentStatus(string contactNumber)
        {
            try
            {
                // Validate contact number format
                if (string.IsNullOrEmpty(contactNumber) || !contactNumber.StartsWith("09") || contactNumber.Length != 11)
                {
                    return Json(new { success = false, message = "Please enter a valid 11-digit contact number starting with 09" });
                }

                // Find user by contact number
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.ContactNumber == contactNumber);

                if (user == null)
                {
                    return Json(new { success = false, message = "No visitor found with this contact number" });
                }

                // Find visitor
                var visitor = await _context.Visitors
                    .FirstOrDefaultAsync(v => v.UserId == user.UserId);

                if (visitor == null)
                {
                    return Json(new { success = false, message = "No visitor found with this contact number" });
                }

                // Get appointments for this visitor
                var appointments = await _context.VisitLogs
                    .Include(v => v.Room)
                    .Include(v => v.RoomOwner)
                    .ThenInclude(ro => ro.User)
                    .Where(v => v.VisitorUserId == visitor.UserId && v.logStatus)
                    .OrderByDescending(v => v.AppointmentDate)
                    .ToListAsync();

                if (!appointments.Any())
                {
                    return Json(new
                    {
                        success = true,
                        hasAppointments = false,
                        contactNumber = contactNumber,
                        message = "No appointments found for this visitor"
                    });
                }

                // Format appointment data
                var appointmentData = appointments.Select(a => new
                {
                    id = a.VisitLogId,
                    roomNumber = a.Room?.RoomNumber ?? "N/A",
                    appointmentDate = a.AppointmentDate?.ToString("yyyy-MM-dd") ?? "N/A",
                    status = GetAppointmentStatus(a.VerificationStatus),
                    purpose = a.PurposeOfVisit ?? "N/A",
                    roomOwnerName = a.RoomOwner != null ?
                        $"{a.RoomOwner.User?.FirstName} {a.RoomOwner.User?.LastName}" : "N/A"
                }).ToList();

                return Json(new
                {
                    success = true,
                    hasAppointments = true,
                    contactNumber = contactNumber,
                    appointments = appointmentData
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error checking appointment status: {ex.Message}" });
            }
        }

        // NEW: Cancel appointment (same as SymfoniHome functionality)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelAppointment([FromBody] CancelAppointmentRequest request)
        {
            try
            {
                // Validate contact number
                if (string.IsNullOrEmpty(request.ContactNumber) || !request.ContactNumber.StartsWith("09") || request.ContactNumber.Length != 11)
                {
                    return Json(new { success = false, message = "Invalid contact number" });
                }

                // Find the visit log
                var visitLog = await _context.VisitLogs
                    .FirstOrDefaultAsync(v => v.VisitLogId == request.AppointmentId && v.logStatus);

                if (visitLog == null)
                {
                    return Json(new { success = false, message = "Appointment not found" });
                }

                // Verify the contact number matches
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserId == visitLog.VisitorUserId && u.ContactNumber == request.ContactNumber);

                if (user == null)
                {
                    return Json(new { success = false, message = "Contact number does not match this appointment" });
                }

                // Cancel the appointment by setting logStatus to false
                visitLog.logStatus = false;
                _context.VisitLogs.Update(visitLog);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Appointment cancelled successfully"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error cancelling appointment: {ex.Message}" });
            }
        }

        // Helper method to get appointment status
        private string GetAppointmentStatus(bool? verificationStatus)
        {
            if (verificationStatus == null) return "pending";
            return verificationStatus.Value ? "approved" : "denied";
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

        // Helper method to mask contact number
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
    }

    // Request models
    public class CheckInRequest
    {
        public string ContactNumber { get; set; }
        public string HostName { get; set; }
        public string Tower { get; set; }
        public string UnitNumber { get; set; }
        public string PurposeVisit { get; set; }
        public bool IsAppointment { get; set; }
        public string VisitCode { get; set; }
    }

    public class CheckOutRequest
    {
        public string ContactNumber { get; set; }
    }

    // NEW: Cancel appointment request model
    public class CancelAppointmentRequest
    {
        public string ContactNumber { get; set; }
        public int AppointmentId { get; set; }
    }
}