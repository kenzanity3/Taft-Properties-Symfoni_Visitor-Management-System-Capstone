// RequestVisitorApprovalController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using VisitorManagementSystem_Capstone.Data;
using VisitorManagementSystem_Capstone.Models;
using VisitorManagementSystem_Capstone.ViewModels;

namespace VisitorManagementSystem_Capstone.Controllers
{
    public class RequestVisitorApprovalController : Controller
    {
        private readonly VisitorManagementSystemDatabaseContext _context;
        private readonly LocalImageService _imageService;

        public RequestVisitorApprovalController(
            VisitorManagementSystemDatabaseContext context,
            LocalImageService imageService)
        {
            _context = context;
            _imageService = imageService;
        }

        // GET: RequestVisitorApproval
        public async Task<IActionResult> RequestVisitorApproval()
        {
            var viewModel = new ForgotContactKioskViewModel
            {
                VisitLog = await GetWalkInVisitorsAsync()
            };
            return View(viewModel);
        }

        // GET: Get walk-in visitors for today
        private async Task<List<VisitLog>> GetWalkInVisitorsAsync()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);

            return await _context.VisitLogs
                .Include(v => v.Visitor)
                    .ThenInclude(v => v.User)
                .Include(v => v.RoomOwner)
                    .ThenInclude(r => r.User)
                .Include(v => v.Room)
                .Include(v => v.CheckInOut)
                .Where(v => v.IssueDate == today && v.logStatus == true)
                .OrderByDescending(v => v.VisitLogId)
                .ToListAsync();
        }

        // POST: Create a new visitor approval request
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestApproval(VisitorRequestModel model)
        {
            if (!ModelState.IsValid)
            {
                // Get the name of the model type from the action method's parameter
                var modelTypeName = typeof(VisitorRequestModel).Name;
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return Json(new { success = false, message = $"Please fill all required fields. {modelTypeName}", errors = errors });
            }

            // Get current staff user ID
            var currentStaffUserId = GetCurrentStaffUserId();
            if (string.IsNullOrEmpty(currentStaffUserId))
            {
                return Json(new { success = false, message = "Staff not authenticated." });
            }

            var currentUser = await _context.Users.FindAsync(currentStaffUserId);
            var currentStaffName = currentUser != null ? $"{currentUser.FirstName} {currentUser.LastName}" : "Staff";

            // Create a new visitor or get existing one
            string visitorUserId = await GetOrCreateVisitorAsync(model);

            // Get room and owner details
            var room = await _context.Rooms
                .FirstOrDefaultAsync(r => r.RoomNumber == model.RoomNumber && r.Tower == model.Tower);

            if (room == null)
            {
                return Json(new { success = false, message = "Room not found." });
            }

            var roomOwnerUserId = await GetRoomOwnerUserIdAsync(model.RoomNumber, model.Tower);
            if (string.IsNullOrEmpty(roomOwnerUserId))
            {
                return Json(new { success = false, message = "Room owner not found for this room." });
            }

            // Check for existing active request for this room today
            var existingRequest = await _context.VisitLogs
                .FirstOrDefaultAsync(v => v.RoomId == room.RoomId
                    && v.IssueDate == DateOnly.FromDateTime(DateTime.Today)
                    && v.logStatus == true
                    && v.VerificationStatus == null
                    && v.VisitorUserId == visitorUserId);

            if (existingRequest != null)
            {
                return Json(new { success = false, message = "There's already an active request for this room today." });
            }

            // Create visit log for walk-in (no appointment date, created by staff)
            var visitLog = new VisitLog
            {
                VisitorUserId = visitorUserId,
                RoomId = room.RoomId,
                OwnerUserId = roomOwnerUserId,
                PurposeOfVisit = model.PurposeOfVisit,
                CreatedBy = "staff",
                AuthorizedUserId = currentStaffUserId,
                IssueDate = DateOnly.FromDateTime(DateTime.Today),
                AppointmentDate = null, // No appointment date for walk-ins
                VerificationStatus = null, // Pending approval
                logStatus = true
            };

            _context.VisitLogs.Add(visitLog);
            await _context.SaveChangesAsync();

            // Log the action
            var actionLog = new AccountActionLog
            {
                ActionDateTime = DateTime.UtcNow,
                ActionText = $"requested walk-in visitor for {model.VisitorFullName} to room {model.RoomNumber}",
                ActionType = "Request",
                UserId = currentStaffUserId,
                TargetUserId = visitorUserId
            };
            _context.accountActionLogs.Add(actionLog);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = $"Visitor request submitted successfully! {model.VisitorFullName} is now pending room owner approval.",
                visitLogId = visitLog.VisitLogId
            });
            //}
            //catch (Exception ex)
            //{
            //    return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            //}
        }

        // POST: Check out a visitor
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckOutVisitor(int visitLogId)
        {
            try
            {
                var visitLog = await _context.VisitLogs
                    .Include(v => v.CheckInOut)
                    .FirstOrDefaultAsync(v => v.VisitLogId == visitLogId);

                if (visitLog == null)
                {
                    return Json(new { success = false, message = "Visit log not found." });
                }

                if (visitLog.CheckInOut == null)
                {
                    return Json(new { success = false, message = "Visitor hasn't checked in yet." });
                }

                if (visitLog.CheckInOut.CheckOutDateTime != null)
                {
                    return Json(new { success = false, message = "Visitor already checked out." });
                }

                // Set checkout time and record who checked out
                visitLog.CheckInOut.CheckOutDateTime = DateTime.Now;
                visitLog.CheckInOut.CheckedOutBy = GetCurrentStaffUserId();

                _context.Update(visitLog);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Visitor checked out successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }

        // Add Check In functionality
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckInVisitor(int visitLogId)
        {
            try
            {
                var visitLog = await _context.VisitLogs
                    .Include(v => v.Visitor)
                    .ThenInclude(v => v.User)
                    .Include(v => v.Room)
                    .FirstOrDefaultAsync(v => v.VisitLogId == visitLogId);

                if (visitLog == null)
                {
                    return Json(new { success = false, message = "Visit log not found." });
                }

                if (visitLog.CheckInOut != null)
                {
                    return Json(new { success = false, message = "Visitor already checked in." });
                }

                if (visitLog.VerificationStatus != true)
                {
                    return Json(new { success = false, message = "Cannot check in a visitor that hasn't been approved." });
                }

                // Create check-in record
                visitLog.CheckInOut = new CheckInOut
                {
                    CheckInDateTime = DateTime.Now
                };

                _context.Update(visitLog);
                await _context.SaveChangesAsync();

                // Log the action
                var actionLog = new AccountActionLog
                {
                    ActionDateTime = DateTime.UtcNow,
                    ActionText = $"Checked in visitor: {visitLog.Visitor.User.FirstName} {visitLog.Visitor.User.LastName}",
                    ActionType = "ClockIn",
                    UserId = GetCurrentStaffUserId(),
                    TargetUserId = visitLog.VisitorUserId
                };
                _context.accountActionLogs.Add(actionLog);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"Visitor {visitLog.Visitor.User.FirstName} {visitLog.Visitor.User.LastName} checked in successfully!"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }

        // Helper method to get or create visitor
        private async Task<string> GetOrCreateVisitorAsync(VisitorRequestModel model)
        {
            var normalizedFullName = model.VisitorFullName.Trim().ToLower();
            // Check if visitor with this contact number exists
            User existingVisitor = new();
            if (model.ContactNumber != null)
            {
                existingVisitor = await _context.Users
                    .FirstOrDefaultAsync(u => u.ContactNumber == model.ContactNumber!);
            }
            else
            {
                existingVisitor = await _context.Users
                .FirstOrDefaultAsync(u => (u.FirstName.ToLower() + " " + u.LastName.ToLower())
                .Equals(model.VisitorFullName.ToLower().Trim())
                );
            }

            if (existingVisitor != null)
            {
                return existingVisitor.UserId;
            }

            // Create new visitor using thread-safe ID generation
            var postViewModel = new POSTViewModel();
            string newUserId = await postViewModel.GenerateNextVisitorIdAsync(_context, "VIS");

            // Save profile picture using LocalImageService
            string profilePicturePath = "/images/default.png";
            if (model.ProfilePicture != null && model.ProfilePicture.Length > 0)
            {
                profilePicturePath = await _imageService.UploadImageAsync(model.ProfilePicture);
            }

            var newUser = new User
            {
                UserId = newUserId,
                FirstName = model.VisitorFullName?.Split(' ').First(),
                LastName = model.VisitorFullName?.Split(' ').Last(),
                ContactNumber = model.ContactNumber ?? null,
                AccountStatus = true,
                DateCreated = DateOnly.FromDateTime(DateTime.Today)
            };

            var newVisitor = new Visitor
            {
                UserId = newUserId,
                ProfilePicture = profilePicturePath
            };

            _context.Users.Add(newUser);
            _context.Visitors.Add(newVisitor);
            await _context.SaveChangesAsync();

            return newUserId;
        }

        // Helper method to get room owner user ID
        private async Task<string> GetRoomOwnerUserIdAsync(string roomNumber, string tower)
        {
            var room = await _context.Rooms
                .FirstOrDefaultAsync(r => r.RoomNumber == roomNumber && r.Tower == tower);

            if (room == null) return null;

            var occupant = await _context.RoomOccupants
                .Include(o => o.RoomOwner)
                .FirstOrDefaultAsync(o => o.RoomId == room.RoomId && o.OccupationStatus);

            return occupant?.RoomOwner?.UserId;
        }

        // Implement these based on your authentication system
        private string GetCurrentStaffUserId()
        {
            var userId = HttpContext.Session.GetString("UserId");

            return userId;
        }

        //GET: Fetch visitor details by contact number
        [HttpGet]
        public async Task<JsonResult> fetchVisitorDetails(string contactnumber)
        {
            if (string.IsNullOrWhiteSpace(contactnumber))
            {
                return Json(new { success = false, message = "Contact number is required" });
            }
            var visitor = await _context.Visitors
                .Include(v => v.User)
                .FirstOrDefaultAsync(v => v.User.ContactNumber == contactnumber.Trim());
            if (visitor == null)
            {
                return Json(new { success = false, message = "No visitor found with this contact number" });
            }
            return Json(new
            {
                success = true,
                message = "Visitor found",
                visitorName = $"{visitor.User.FirstName} {visitor.User.LastName}",
                profilePicture = visitor.ProfilePicture ?? "/images/default.png"
            });
        }

        //GET: Get contactnumbers of existing visitors by full name
        [HttpGet]
        public async Task<JsonResult> CheckVisitorExistingName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
            {
                return Json(new { error = false, message = "Visitor Full Name is required" });
            }

            var visitors = await _context.Visitors
                .Include(u => u.User)
                .Where(u => (u.User.FirstName.ToLower() + " " + u.User.LastName.ToLower())
                .Equals(fullName.ToLower().Trim()))
                .ToListAsync();

            if (visitors.Count() <= 0)
            {
                return Json(new { error = false, message = "No existing visitor found" });
            }

            List<string> contactnumbers = new();
            foreach (var visitor in visitors)
            {
                contactnumbers.Add(visitor.User.ContactNumber);
            }

            return Json(new { success = true, message = "Existing visitor found", contactnumbers });
        }

        // GET: Get room owner details
        [HttpGet]
        public async Task<JsonResult> GetRoomOwner(string roomNumber, string tower)
        {
            if (string.IsNullOrWhiteSpace(roomNumber) || string.IsNullOrWhiteSpace(tower))
            {
                return Json(new { success = false, message = "Room number and tower are required" });
            }

            var room = await _context.Rooms
                .FirstOrDefaultAsync(r => r.RoomNumber == roomNumber && r.Tower == tower);

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
                    roomOwnerName = $"{roomOccupant.RoomOwner.User?.FirstName} {roomOccupant.RoomOwner.User?.LastName}"
                });
            }

            return Json(new { success = false, message = "Room owner not found" });
        }

        //Get Walk In Visitors for Today - returns HTML
        // Update the GetWalkInVisitors method to include check-in status
        [HttpGet]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> GetWalkInVisitors()
        {
            var visitors = await GetWalkInVisitorsAsync();

            if (!visitors.Any())
            {
                return Content(@"<tr>
            <td colspan='10' class='text-center text-muted py-4'>
                <i class='bi bi-people' style='font-size: 2rem;'></i>
                <p class='mt-2'>No walk-in visitors today</p>
            </td>
        </tr>");
            }

            var html = "";
            foreach (var visit in visitors)
            {
                var statusBadge = visit.VerificationStatus switch
                {
                    true => "<span class='badge bg-success'>Approved</span>",
                    false => "<span class='badge bg-danger'>Denied</span>",
                    _ => "<span class='badge bg-warning'>Pending</span>"
                };

                var checkInStatus = "";
                if (visit.CheckInOut != null)
                {
                    if (visit.CheckInOut.CheckOutDateTime == null)
                    {
                        checkInStatus = "<span class='badge bg-info'>Checked In</span>" +
                                       $"<small class='d-block'>{visit.CheckInOut.CheckInDateTime.ToString("hh:mm tt")}</small>";
                    }
                    else
                    {
                        checkInStatus = "<span class='badge bg-secondary'>Checked Out</span>" +
                                       $"<small class='d-block'>{visit.CheckInOut.CheckOutDateTime.Value.ToString("hh:mm tt")}</small>";
                    }
                }
                else
                {
                    checkInStatus = "<span class='badge bg-secondary'>Not Checked In</span>";
                }

                var towerBadge = visit.Room != null ?
                    $"<span class='badge {(visit.Room.Tower == "Basso" ? "bg-primary" : "bg-info")}'>{visit.Room.Tower}</span>" :
                    "<span class='badge bg-secondary'>N/A</span>";

                var contactInfo = visit.Visitor?.User?.ContactNumber ?? "N/A";
                if (contactInfo == "N/A")
                {
                    contactInfo = "<span class='text-danger'>No Contact</span>";
                }

                var actions = "";
                if (visit.CheckInOut != null && visit.CheckInOut.CheckOutDateTime == null)
                {
                    // Checked in but not checked out - show checkout button
                    actions = $@"<button class='btn btn-sm btn-outline-danger checkout-btn'
                        data-visitlogid='{visit.VisitLogId}'
                        data-visitorname='{visit.Visitor?.User?.FirstName} {visit.Visitor?.User?.LastName}'>
                    <i class='fa-solid fa-door-open'></i> Check Out
                </button>";
                }
                else if (visit.CheckInOut == null && visit.VerificationStatus == true)
                {
                    // Approved but not checked in - show checkin button
                    actions = $@"<button class='btn btn-sm btn-outline-success checkin-btn'
                        data-visitlogid='{visit.VisitLogId}'
                        data-visitorname='{visit.Visitor?.User?.FirstName} {visit.Visitor?.User?.LastName}'>
                    <i class='fa-solid fa-door-closed'></i> Check In
                </button>";
                }
                else
                {
                    actions = "<span class='text-muted'>No actions</span>";
                }

                html += $@"<tr class='visitor-row' data-visitlogid='{visit.VisitLogId}'>
            <td>
                <img src='{(visit.Visitor?.ProfilePicture ?? "/images/default.png")}'
                     class='visitor-avatar'
                     alt='Visitor'
                     onerror='this.src=""/images/default.png""'>
            </td>
            <td>{visit.Visitor?.User?.FirstName} {visit.Visitor?.User?.LastName}</td>
            <td>{contactInfo}</td>
            <td>{towerBadge}</td>
            <td>{visit.Room?.RoomNumber}</td>
            <td>{visit.RoomOwner?.User?.FirstName} {visit.RoomOwner?.User?.LastName}</td>
            <td>{statusBadge}</td>
            <td>{checkInStatus}</td>
            <td>{visit.PurposeOfVisit}</td>
            <td>{actions}</td>
        </tr>";
            }

            return Content(html);
        }
    }

    // ViewModel for visitor request
    public class VisitorRequestModel
    {
        [Required(ErrorMessage = "Visitor full name is required")]
        public string VisitorFullName { get; set; }

        [Required(ErrorMessage = "Room number is required")]
        public string RoomNumber { get; set; }

        [Required(ErrorMessage = "Tower is required")]
        public string Tower { get; set; }

        public string? ContactNumber { get; set; }

        [Required(ErrorMessage = "Purpose of visit is required")]
        public string PurposeOfVisit { get; set; }

        public IFormFile ProfilePicture { get; set; }
    }
}