using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;
using VisitorManagementSystem_Capstone.Data;
using VisitorManagementSystem_Capstone.Models;
using VisitorManagementSystem_Capstone.Services;
using VisitorManagementSystem_Capstone.ViewModels;
using Microsoft.AspNetCore.Http;

namespace VisitorManagementSystem_Capstone.Controllers
{
    public class RoomOwnerController : Controller
    {
        private readonly VisitorManagementSystemDatabaseContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public RoomOwnerController(VisitorManagementSystemDatabaseContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<IActionResult> DashboardUnitOwner()
        {
            // Check if user is authenticated
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            POSTViewModel viewmodel = new POSTViewModel();
            ViewData["Title"] = "Unit Owner Dashboard";

            var model = new DashboardViewModel
            {
                PendingVisits = await getVisitList(),
                VerifiedVisits = await getVerifiedVisits(),
                StaffApprovalVisits = await GetStaffApprovalVisits()
            };

            return View(model);
        }

        private async Task<List<VisitLog>> getVisitList()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return new List<VisitLog>();
            }

            List<VisitLog> visitLogs = await _context.VisitLogs
               .Include(v => v.Visitor)
               .ThenInclude(v => v.User)
               .Include(v => v.Room)
               .Include(v => v.AuthorizedUser)
               .Where(r => r.OwnerUserId == userId && r.VerificationStatus == null && r.logStatus == true)
               .ToListAsync();

            return visitLogs;
        }

        private async Task<List<VisitLog>> getVerifiedVisits()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return new List<VisitLog>();
            }

            return await _context.VisitLogs
                .Include(v => v.Visitor)
                .ThenInclude(v => v.User)
                .Include(v => v.Room)
                .Include(v => v.AuthorizedUser)
                .Where(r => r.OwnerUserId == userId ||
                ((r.VerificationStatus == true && r.VerifiedDateTime != null) ||
                (r.VerificationStatus == false && r.logStatus != false) ||
                (r.VerificationStatus == null && r.logStatus == false)))
                .OrderByDescending(v => v.IssueDate)
                .OrderByDescending(v => v.VerifiedDateTime)
                .ToListAsync();
        }

        private async Task<List<VisitLog>> GetStaffApprovalVisits()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return new List<VisitLog>();
            }

            return await _context.VisitLogs
                .Include(v => v.Visitor)
                .ThenInclude(v => v.User)
                .Include(v => v.Room)
                .Include(v => v.AuthorizedUser)
                .Where(r => r.OwnerUserId == userId
                    && r.VerificationStatus == null
                    && r.logStatus == true
                    && (r.Visitor.User.ContactNumber == "N/A" ||
                        string.IsNullOrEmpty(r.Visitor.User.ContactNumber)))
                .ToListAsync();
        }


        public async Task<IActionResult> GetPendingAppointments()
        {
            var pendingVisits = await getVisitList();

            if (pendingVisits == null || !pendingVisits.Any())
            {
                return Content(@"<tr>
            <td colspan='6' class='text-center text-muted py-4'>
                <i class='bi bi-calendar-x' style='font-size: 2rem;'></i>
                <p class='mt-2'>No pending appointments found.</p>
            </td>
        </tr>");
            }

            var html = "";
            foreach (var appt in pendingVisits)
            {
                html += $@"
        <tr data-visitlogid='{appt.VisitLogId}'>
            <td>{appt.Visitor?.User?.FirstName} {appt.Visitor?.User?.LastName}</td>
            <td>{appt.Room?.RoomNumber}</td>
            <td><span class='badge bg-warning text-dark'>Pending</span></td>
            <td>
                <span class='badge {GetCreatorBadgeClass(appt.CreatedBy)}'>
                    {GetCreatorDisplayName(appt.CreatedBy, appt.AuthorizedUser)}
                </span>
            </td>
            <td>{appt.AppointmentDate?.ToString("MMM dd, yyyy")}</td>
            <td class='text-center'>
                <div class='btn-group btn-group-sm'>
                    <button class='btn btn-success approve-btn' data-visitlogid='{appt.VisitLogId}'>
                        <i class='fa-solid fa-check'></i> Approve
                    </button>
                    <button class='btn btn-danger deny-btn' data-visitlogid='{appt.VisitLogId}'>
                        <i class='fa-solid fa-xmark'></i> Decline
                    </button>
                    <button class='btn btn-info info-visitor-btn'
                            data-bs-toggle='modal'
                            data-bs-target='#infoVisitorModal'
                            data-userid='{appt.Visitor?.UserId}'
                            data-fullname='{appt.Visitor?.User?.FirstName} {appt.Visitor?.User?.LastName}'
                            data-contact='{appt.Visitor?.User?.ContactNumber ?? "N/A"}'
                            data-profilepic='{appt.Visitor?.ProfilePicture}'
                            data-purposeofvisit='{appt.PurposeOfVisit}'>
                        <i class='fa-solid fa-circle-info'></i> Details
                    </button>
                </div>
            </td>
        </tr>";
            }

            return Content(html);
        }

        public async Task<IActionResult> GetStaffApprovalVisitsPartial()
        {
            var staffApprovalVisits = await GetStaffApprovalVisits();

            if (staffApprovalVisits == null || !staffApprovalVisits.Any())
            {
                return Content(@"<tr>
            <td colspan='7' class='text-center text-muted py-4'>
                <i class='bi bi-telephone-x' style='font-size: 2rem;'></i>
                <p class='mt-2'>No visits requiring staff approval</p>
            </td>
        </tr>");
            }

            var html = "";
            foreach (var visit in staffApprovalVisits)
            {
                html += $@"
        <tr data-visitlogid='{visit.VisitLogId}'>
            <td>{visit.Visitor?.User?.FirstName} {visit.Visitor?.User?.LastName}</td>
            <td>{visit.Room?.RoomNumber}</td>
            <td><span class='badge bg-danger'>No Contact</span></td>
            <td>
                <span class='badge {GetCreatorBadgeClass(visit.CreatedBy)}'>
                    {GetCreatorDisplayName(visit.CreatedBy, visit.AuthorizedUser)}
                </span>
            </td>
            <td>{visit.AppointmentDate?.ToString("MMM dd, yyyy")}</td>
            <td>{visit.PurposeOfVisit}</td>
            <td class='text-center'>
                <div class='btn-group btn-group-sm'>
                    <button class='btn btn-success approve-staff-btn' data-visitlogid='{visit.VisitLogId}'>
                        <i class='fa-solid fa-phone'></i> Call & Approve
                    </button>
                    <button class='btn btn-danger deny-staff-btn' data-visitlogid='{visit.VisitLogId}'>
                        <i class='fa-solid fa-xmark'></i> Decline
                    </button>
                    <button class='btn btn-info info-visitor-btn'
                            data-bs-toggle='modal'
                            data-bs-target='#infoVisitorModal'
                            data-userid='{visit.Visitor?.UserId}'
                            data-fullname='{visit.Visitor?.User?.FirstName} {visit.Visitor?.User?.LastName}'
                            data-contact='{visit.Visitor?.User?.ContactNumber ?? "N/A"}'
                            data-profilepic='{visit.Visitor?.ProfilePicture}'
                            data-purposeofvisit='{visit.PurposeOfVisit}'>
                        <i class='fa-solid fa-circle-info'></i> Details
                    </button>
                </div>
            </td>
        </tr>";
            }

            return Content(html);
        }

        private string GetCreatorDisplayName(string createdBy, User authorizedUser)
        {
            return createdBy?.ToLower() switch
            {
                "admin" => $"Admin: {authorizedUser?.FirstName} {authorizedUser?.LastName}",
                "staff" => $"Staff: {authorizedUser?.FirstName} {authorizedUser?.LastName}",
                "visitor" => "Visitor",
                _ => createdBy ?? "Unknown"
            };
        }

        private string GetCreatorBadgeClass(string createdBy)
        {
            return createdBy?.ToLower() switch
            {
                "admin" => "bg-danger",
                "staff" => "bg-info",
                "visitor" => "bg-secondary",
                _ => "bg-secondary"
            };
        }

        public async Task<IActionResult> GetVerifiedAppointments()
        {
            var verifiedVisits = await getVerifiedVisits();

            if (verifiedVisits == null || !verifiedVisits.Any())
            {
                return Content(@"<div class='text-center text-muted py-4'>
                    <i class='bi bi-check-circle' style='font-size: 2rem;'></i>
                    <p class='mt-2'>No verified appointments</p>
                </div>");
            }

            var html = @"<div class='list-group'>";
            foreach (var appt in verifiedVisits.Take(5))
            {
                var statusClass = appt.VerificationStatus == true ? "bg-success" : "bg-danger";
                var statusText = appt.VerificationStatus == true ? "Approved" : "Declined";

                html += $@"
                <div class='list-group-item list-group-item-action'>
                    <div class='d-flex justify-content-between'>
                        <div>
                            <strong>{appt.Visitor?.User?.FirstName} {appt.Visitor?.User?.LastName}</strong>
                            <button class='btn btn-sm btn-info ms-2 info-visitor-btn'
                                    data-bs-toggle='modal'
                                    data-bs-target='#infoVisitorModal'
                                    data-userid='{appt.Visitor?.UserId}'
                                    data-fullname='{appt.Visitor?.User?.FirstName} {appt.Visitor?.User?.LastName}'
                                    data-contact='{appt.Visitor?.User?.ContactNumber ?? "N/A"}'
                                    data-profilepic='{appt.Visitor?.ProfilePicture}'>
                                <i class='bi bi-info-circle'></i>
                            </button>
                        </div>
                        <span class='badge {statusClass}'>{statusText}</span>
                    </div>
                    <small class='text-muted'>{appt.AppointmentDate?.ToString("MMM dd")}</small>
                </div>";
            }
            html += "</div>";

            if (verifiedVisits.Count > 5)
            {
                html += $@"<div class='text-center mt-2'>
                    <small class='text-muted'>+ {verifiedVisits.Count - 5} more</small>
                </div>";
            }

            return Content(html);
        }

        // Update the ApproveStaffVisit method to auto check-in
        [HttpPost]
        public async Task<IActionResult> ApproveStaffVisit(int VisitLogId)
        {
            try
            {
                var userId = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var visitLog = await _context.VisitLogs
                    .Include(v => v.Visitor)
                    .ThenInclude(v => v.User)
                    .Include(v => v.Room)
                    .FirstOrDefaultAsync(v => v.VisitLogId == VisitLogId);

                if (visitLog == null)
                {
                    return Json(new { success = false, message = "Visit log not found." });
                }

                // Check if current user is the owner of this visit log
                if (visitLog.OwnerUserId != userId)
                {
                    return Json(new { success = false, message = "Unauthorized to approve this visit" });
                }

                // Update the visit log status
                visitLog.VerificationStatus = true;
                visitLog.VerifiedDateTime = DateTime.UtcNow;

                // Auto check-in the visitor
                visitLog.CheckInOut = new CheckInOut
                {
                    CheckInDateTime = DateTime.UtcNow
                };

                _context.VisitLogs.Update(visitLog);
                await _context.SaveChangesAsync();

                // Log the action
                var actionLog = new AccountActionLog
                {
                    ActionDateTime = DateTime.UtcNow,
                    ActionText = $"Approved and auto-checked in forgot-contact visitor: {visitLog.Visitor.User.FirstName} {visitLog.Visitor.User.LastName}",
                    ActionType = "StaffApproval",
                    UserId = userId,
                    TargetUserId = visitLog.VisitorUserId
                };

                _context.accountActionLogs.Add(actionLog);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Visit approved and auto-checked in successfully",
                    visitorName = $"{visitLog.Visitor.User.FirstName} {visitLog.Visitor.User.LastName}",
                    roomNumber = visitLog.Room?.RoomNumber
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public async Task<JsonResult> GetQuickStats()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { pendingCount = 0, approvedToday = 0 });
            }

            var pendingCount = await _context.VisitLogs
                .CountAsync(v => v.OwnerUserId == userId && v.VerificationStatus == null && v.logStatus == true);

            var approvedToday = await _context.VisitLogs
                .CountAsync(v => v.OwnerUserId == userId
                    && v.VerificationStatus == true
                    && v.VerifiedDateTime.HasValue
                    && v.VerifiedDateTime.Value.Date == DateTime.Today
                    && v.logStatus == true);

            return Json(new
            {
                pendingCount = pendingCount,
                approvedToday = approvedToday
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateVisitCode()
        {
            var userId = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "User not authenticated" });
            }

            var roomOwner = await _context.RoomOwners
                .Include(ro => ro.User)
                .FirstOrDefaultAsync(ro => ro.UserId == userId);

            if (roomOwner == null)
            {
                return Json(new { success = false, message = "Room owner not found" });
            }

            // Generate OTP with max usage of 3
            string otp = OtpService.GenerateRoomOwnerOtp(roomOwner.UserId, 3);
            var remainingTime = OtpService.GetOtpRemainingTime(otp);

            return Json(new
            {
                success = true,
                otp = otp,
                expiresIn = remainingTime?.TotalSeconds ?? 1500, // 25 minutes
                maxUsage = 3,
                remainingUses = 3, // Initial remaining uses
                message = "Visit code generated successfully. Share this with your visitor."
            });
        }

        [HttpGet]
        public async Task<IActionResult> CheckActiveOtp()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false });
            }

            // Check if there's an active OTP for this user
            var activeOtps = OtpService.GetAllActiveOtps();
            var userOtp = activeOtps.FirstOrDefault(o => o.Id == userId);

            if (userOtp != null)
            {
                var remainingTime = OtpService.GetOtpRemainingTime(userOtp.Code);
                if (remainingTime.HasValue && remainingTime.Value.TotalSeconds > 0)
                {
                    return Json(new
                    {
                        success = true,
                        otp = userOtp.Code,
                        expiresIn = (int)remainingTime.Value.TotalSeconds,
                        remainingUses = userOtp.MaxUsage - userOtp.userids.Count
                    });
                }
            }
            return Json(new { success = false });
        }

        //Approve Appointments
        [HttpPost]
        public async Task<IActionResult> ApproveAppointment(int VisitLogId)
        {
            try
            {
                var userId = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var appointment = await _context.VisitLogs
                    .Include(v => v.Visitor)
                    .ThenInclude(v => v.User)
                    .Include(v => v.Room)
                    .FirstOrDefaultAsync(v => v.VisitLogId == VisitLogId);

                if (appointment == null)
                {
                    return Json(new { success = false, message = "Appointment not found" });
                }

                // Check if current user is the owner of this appointment
                if (appointment.OwnerUserId != userId)
                {
                    return Json(new { success = false, message = "Unauthorized to approve this appointment" });
                }

                // Update the appointment status
                appointment.VerificationStatus = true;
                appointment.VerifiedDateTime = DateTime.UtcNow;

                // Save changes to database
                _context.VisitLogs.Update(appointment);
                await _context.SaveChangesAsync();

                // Get updated counts
                var todayVisits = await _context.VisitLogs
                    .CountAsync(v => v.VerifiedDateTime.HasValue
                        && v.VerifiedDateTime.Value.Date == DateTime.UtcNow.Date
                        && v.VerificationStatus == true
                        && v.OwnerUserId == userId
                        && v.logStatus == true);

                return Json(new
                {
                    success = true,
                    visitorName = $"{appointment.Visitor.User.FirstName} {appointment.Visitor.User.LastName}",
                    contactNumber = appointment.Visitor.User.ContactNumber,
                    roomNumber = appointment.Room?.RoomNumber,
                    appointmentDate = appointment.AppointmentDate?.ToString("MMMM dd, yyyy"),
                    purpose = appointment.PurposeOfVisit,
                    visitsToday = todayVisits
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        [HttpPost]
        public async Task<IActionResult> DenyAppointment(int VisitLogId)
        {
            try
            {
                var userId = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var appointment = await _context.VisitLogs
                    .Include(v => v.Visitor)
                    .ThenInclude(v => v.User)
                    .Include(v => v.Room)
                    .FirstOrDefaultAsync(v => v.VisitLogId == VisitLogId);

                if (appointment == null)
                {
                    return Json(new { success = false, message = "Appointment not found" });
                }

                appointment.VerificationStatus = false;
                appointment.VerifiedDateTime = DateTime.UtcNow;

                _context.VisitLogs.Update(appointment);
                await _context.SaveChangesAsync();

                // Get updated visits today count
                var todayVisits = await _context.VisitLogs
                    .CountAsync(v => v.VerifiedDateTime.HasValue
                        && v.VerifiedDateTime.Value.Date == DateTime.UtcNow.Date
                        && v.VerificationStatus == true
                        && v.OwnerUserId == userId
                        && v.logStatus == true);

                return Json(new
                {
                    success = true,
                    visitorName = $"{appointment.Visitor.User.FirstName} {appointment.Visitor.User.LastName}",
                    contactNumber = appointment.Visitor.User.ContactNumber,
                    roomNumber = appointment.Room?.RoomNumber,
                    appointmentDate = appointment.AppointmentDate?.ToString("MMMM dd, yyyy"),
                    purpose = appointment.PurposeOfVisit,
                    visitsToday = todayVisits
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}