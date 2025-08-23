// RequestVisitorApprovalController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using VisitorManagementSystem_Capstone.Data;
using VisitorManagementSystem_Capstone.Models;
using VisitorManagementSystem_Capstone.Services;
using VisitorManagementSystem_Capstone.ViewModels;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
        [HttpGet]
        public async Task<List<VisitLog>> GetWalkInVisitorsAsync()
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

            Console.WriteLine("\n\n 0 here \n\n");
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors)
                                 .Select(e => e.ErrorMessage)
                                 .ToList();
                Console.WriteLine(string.Join(", ", errors)); // Log errors
                return Json(new { success = false, message = string.Join(", ", errors) });
                //return Json(new { success = false, message = "Please fill all required fields." });
            }
            Console.WriteLine("\n\n 123 here \n\n");
            try
            {
                // Create a new POSTViewModel instance
                var postViewModel = new POSTViewModel();

                // Map the form data to VisitLog
                postViewModel.VisitLog = new VisitLog
                {
                    VisitorUserId = await GetOrCreateVisitorAsync(model),
                    RoomId = await GetRoomIdAsync(model.RoomNumber, model.Tower),
                    OwnerUserId = await GetRoomOwnerUserIdAsync(model.RoomNumber,model.Tower,model.RoomOwnerName),
                    PurposeOfVisit = model.PurposeOfVisit,
                    CreatedBy = "staff",
                    AuthorizedUserId = GetCurrentStaffUserId(),
                    IssueDate = DateOnly.FromDateTime(DateTime.Today)
                };

                // Save the visit log
                var errors = await postViewModel.CreateLogAsync(_context, GetCurrentStaffUserId(), 1);

                Console.WriteLine("\n\n 321 here \n\n");
                if (errors.Any())
                {
                    return Json(new { success = false, message = string.Join(", ", errors) });
                }

                return Json(new { success = true, message = "Visitor request submitted successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
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

        // Helper method to get or create visitor
        private async Task<string> GetOrCreateVisitorAsync(VisitorRequestModel model)
        {
            // Check if visitor with this contact number exists
            var existingVisitor = await _context.Users
                .FirstOrDefaultAsync(u => u.ContactNumber == model.ContactNumber);

            if (existingVisitor != null)
            {
                return existingVisitor.UserId;
            }

            // Create new visitor
            var newUserId = await GenerateNextVisitorIdAsync();

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
                ContactNumber = model.ContactNumber ?? "N/A", // Use "N/A" if no contact number
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

        // Helper method to generate next visitor ID
        private async Task<string> GenerateNextVisitorIdAsync()
        {
            var lastVisitor = await _context.Visitors
                .OrderByDescending(v => v.UserId)
                .FirstOrDefaultAsync();

            if (lastVisitor == null)
            {
                return "VIS-00001";
            }

            var lastId = lastVisitor.UserId;
            var numberPart = int.Parse(lastId.Split('-')[1]);
            return $"VIS-{(numberPart + 1).ToString("D5")}";
        }

        // Helper method to get room ID
        private async Task<int> GetRoomIdAsync(string roomNumber, string tower)
        {
            var room = await _context.Rooms
                .FirstOrDefaultAsync(r => r.RoomNumber == roomNumber && r.Tower == tower);

            return room?.RoomId ?? 0;
        }

        // Helper method to get room owner user ID
        private async Task<string> GetRoomOwnerUserIdAsync(string roomNumber, string tower, string name)
        {
            var room = await _context.Rooms
                .FirstOrDefaultAsync(r => r.RoomNumber == roomNumber && r.Tower == tower);

            if (room == null) return null;

            var occupant = await _context.RoomOccupants
                .Include(o => o.RoomOwner)
                .ThenInclude(ro => ro.User)
                .FirstOrDefaultAsync(o => o.RoomId == room.RoomId && o.OccupationStatus && (o.RoomOwner.User.FirstName + " " + o.RoomOwner.User.LastName) == name);

            return occupant?.RoomOwner?.UserId;
        }

        // Implement these based on your authentication system
        private string GetCurrentStaffUserId()
        {
            // This should return the currently logged-in staff user ID
            return "STF-001"; // Example value
        }

        [HttpGet]
        public async Task<JsonResult> GetRoomOwner(string roomNumber, string tower)
        {
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

        [HttpGet]
        public async Task<IActionResult> GetWalkInVisitors()
        {
            var visitors = await GetWalkInVisitorsAsync();

            if (!visitors.Any())
            {
                return Content(@"<tr>
            <td colspan='8' class='text-center text-muted py-4'>
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

                var checkInStatus = visit.CheckInOut != null
                    ? (visit.CheckInOut.CheckOutDateTime == null
                        ? $"<span class='badge bg-info'>Checked In</span><small class='d-block'>{visit.CheckInOut.CheckInDateTime:hh:mm tt}</small>"
                        : $"<span class='badge bg-secondary'>Checked Out</span><small class='d-block'>{visit.CheckInOut.CheckOutDateTime.Value:hh:mm tt}</small>")
                    : "<span class='badge bg-secondary'>Not Checked In</span>";

                var actions = visit.CheckInOut != null && visit.CheckInOut.CheckOutDateTime == null
                    ? $@"<button class='btn btn-sm btn-outline-danger checkout-btn' 
                data-visitlogid='{visit.VisitLogId}'
                data-visitorname='{visit.Visitor?.User?.FirstName} {visit.Visitor?.User?.LastName}'>
                    <i class='fa-solid fa-door-open'></i> Check Out
                </button>"
                    : visit.CheckInOut == null && visit.VerificationStatus == true
                    ? $@"<button class='btn btn-sm btn-outline-success checkin-btn' 
                data-visitlogid='{visit.VisitLogId}'>
                    <i class='fa-solid fa-door-closed'></i> Check In
                </button>"
                    : "<span class='text-muted'>No actions</span>";

                html += $@"<tr class='visitor-row' data-visitlogid='{visit.VisitLogId}'>
            <td>
                <img src='{(visit.Visitor?.ProfilePicture ?? "/images/default.png")}'
                     class='visitor-avatar'
                     alt='Visitor'
                     onerror='this.src=&quot;/images/default.png&quot;'>
            </td>
            <td>{visit.Visitor?.User?.FirstName} {visit.Visitor?.User?.LastName}</td>
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

        [Required(ErrorMessage = "Tower is required")]
        public string Tower { get; set; }

        [Required(ErrorMessage = "Room number is required")]
        public string RoomNumber { get; set; }

        [Required(ErrorMessage = "RoomOwnerName is required")]
        public string RoomOwnerName { get; set; }

        public string ContactNumber { get; set; }

        [Required(ErrorMessage = "Purpose of visit is required")]
        public string PurposeOfVisit { get; set; }

        public IFormFile ProfilePicture { get; set; }
    }
}