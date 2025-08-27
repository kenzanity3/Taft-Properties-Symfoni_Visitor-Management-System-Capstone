// StaffController.cs - Fixed version
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem_Capstone.Data;
using VisitorManagementSystem_Capstone.Models;
using VisitorManagementSystem_Capstone.ViewModels;

namespace VisitorManagementSystem_Capstone.Controllers
{
    public class StaffController : Controller
    {
        private readonly VisitorManagementSystemDatabaseContext _context;
        private readonly LocalImageService _imageService;

        public StaffController(VisitorManagementSystemDatabaseContext context, LocalImageService imageService)
        {
            _context = context;
            _imageService = imageService;
        }

        public async Task<IActionResult> DashboardStaff()
        {
            ViewData["Title"] = "Staff Dashboard";

            var model = new ForgotContactKioskViewModel
            {
                VisitLog = await GetTodayWalkInVisitors()
            };

            return View(model);
        }

        private async Task<List<VisitLog>> GetTodayWalkInVisitors()
        {
            return await _context.VisitLogs
                .Include(v => v.Visitor)
                .ThenInclude(v => v.User)
                .Include(v => v.Room)
                .Include(v => v.RoomOwner)
                .ThenInclude(ro => ro.User)
                .Include(v => v.AuthorizedUser)  // Include AuthorizedUser instead of Staff
                .Include(v => v.CheckInOut)
                .Where(v => v.IssueDate == DateOnly.FromDateTime(DateTime.Today) &&
                           v.CreatedBy == "staff" &&
                           v.logStatus == true)
                .OrderByDescending(v => v.VisitLogId)
                .ToListAsync();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestApproval(ForgotContactKioskViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Please fill all required fields" });
            }

            try
            {
                // Find room by room number
                var room = await _context.Rooms
                    .FirstOrDefaultAsync(r => r.RoomNumber == model.RoomNumber);

                if (room == null)
                {
                    return Json(new { success = false, message = "Room not found" });
                }

                // Find active room occupant for this room
                var roomOccupant = await _context.RoomOccupants
                    .Include(ro => ro.RoomOwner)
                    .ThenInclude(ro => ro.User)
                    .FirstOrDefaultAsync(ro => ro.RoomId == room.RoomId &&
                                              ro.OccupationStatus &&
                                              ro.MoveOutDate == null);

                if (roomOccupant == null || roomOccupant.RoomOwner == null)
                {
                    return Json(new { success = false, message = "No active room owner found for this room" });
                }

                var roomOwner = roomOccupant.RoomOwner;

                // Generate visitor ID
                var visitorId = await GenerateVisitorId();

                // Save profile picture using LocalImageService
                string profilePicturePath = "/images/default.png";
                if (model.ProfilePicture != null && model.ProfilePicture.Length > 0)
                {
                    profilePicturePath = await _imageService.UploadImageAsync(model.ProfilePicture);
                }

                // Create user for visitor
                var user = new User
                {
                    UserId = visitorId,
                    FirstName = model.VisitorFullName?.Split(' ').FirstOrDefault(),
                    LastName = model.VisitorFullName?.Split(' ').LastOrDefault(),
                    ContactNumber = model.ContactNumber ?? "N/A",
                    AccountStatus = true,
                    DateCreated = DateOnly.FromDateTime(DateTime.UtcNow)
                };

                // Create visitor
                var visitor = new Visitor
                {
                    UserId = visitorId,
                    ProfilePicture = profilePicturePath
                };

                // Create visit log
                var visitLog = new VisitLog
                {
                    IssueDate = DateOnly.FromDateTime(DateTime.Today),
                    PurposeOfVisit = model.PurposeOfVisit,
                    VisitorUserId = visitorId,
                    OwnerUserId = roomOwner.UserId,
                    RoomId = room.RoomId,
                    CreatedBy = "staff",
                    AuthorizedUserId = HttpContext.Session.GetString("UserId"), // Use AuthorizedUserId
                    logStatus = true
                };

                // Save to database
                _context.Users.Add(user);
                _context.Visitors.Add(visitor);
                _context.VisitLogs.Add(visitLog);

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Visit request submitted successfully! Waiting for room owner approval."
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        public async Task<IActionResult> GetWalkInVisitors()
        {
            var visitors = await GetTodayWalkInVisitors();

            if (!visitors.Any())
            {
                return Content(@"<tr>
                    <td colspan='7' class='text-center text-muted py-4'>
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

                var checkInStatus = visit.CheckInOut != null ?
                    "<span class='badge bg-info'>Checked In</span>" :
                    "<span class='badge bg-secondary'>Not Checked In</span>";

                html += $@"
                <tr class='visitor-row' data-visitlogid='{visit.VisitLogId}'>
                    <td>
                        <img src='{visit.Visitor?.ProfilePicture}'
                             class='visitor-avatar'
                             alt='Visitor'
                             onerror='this.src=""/images/default.png""'>
                    </td>
                    <td>{visit.Visitor?.User?.FirstName} {visit.Visitor?.User?.LastName}</td>
                    <td>{visit.Room?.RoomNumber}</td>
                    <td>{visit.RoomOwner?.User?.FirstName} {visit.RoomOwner?.User?.LastName}</td>
                    <td>{statusBadge}</td>
                    <td>{checkInStatus}</td>
                    <td>{visit.PurposeOfVisit}</td>
                </tr>";
            }

            return Content(html);
        }

        [HttpGet]
        public async Task<JsonResult> GetRoomOwner(string roomNumber)
        {
            // Find room by room number
            var room = await _context.Rooms
                .FirstOrDefaultAsync(r => r.RoomNumber == roomNumber);

            if (room == null)
            {
                return Json(new { success = false, message = "Room not found" });
            }

            // Find active room occupant for this room
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

        private async Task<string> GenerateVisitorId()
        {
            var year = DateTime.Now.Year;
            var prefix = $"VIS-{year}-";

            var lastVisitor = await _context.Visitors
                .Where(v => v.UserId.StartsWith(prefix))
                .OrderByDescending(v => v.UserId)
                .FirstOrDefaultAsync();

            var nextNumber = 1;
            if (lastVisitor != null)
            {
                var numberPart = lastVisitor.UserId.Substring(prefix.Length);
                if (int.TryParse(numberPart, out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            return $"{prefix}{nextNumber:D5}";
        }
    }
}