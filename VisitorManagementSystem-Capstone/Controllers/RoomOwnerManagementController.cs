using Azure;
using Microsoft.AspNet.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VisitorManagementSystem_Capstone.Data;
using VisitorManagementSystem_Capstone.Models;
using VisitorManagementSystem_Capstone.Services;
using VisitorManagementSystem_Capstone.ViewModels;

namespace VisitorManagementSystem_Capstone.Controllers
{
    public class RoomOwnerManagementController : Controller
    {
        private readonly VisitorManagementSystemDatabaseContext _context;
        private readonly ILogger<RoomOwnerManagementController> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly EmailService _emailService;

        public RoomOwnerManagementController(
            VisitorManagementSystemDatabaseContext context,
            IWebHostEnvironment environment,
            ILogger<RoomOwnerManagementController> logger,
            EmailService emailService)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
            _emailService = emailService;
        }

        // GET: RoomOwner Management Page
        public async Task<IActionResult> RoomOwnerManagement(bool showDeleted = false)
        {
            try
            {
                var postViewModel = new POSTViewModel();
                var roomOwners = await postViewModel.GetUserRoleList(_context, 2); // Mode 2 = Room Owners

                // Filter based on showDeleted toggle
                if (!showDeleted)
                {
                    roomOwners = roomOwners.Where(ro => ro.User != null && ro.User.AccountStatus).ToList();
                }

                var viewModels = new List<POSTViewModel>();

                foreach (var owner in roomOwners)
                {
                    if (owner.User == null) continue;

                    // Get the room occupant record for this owner
                    var occupant = await _context.RoomOccupants
                        .Include(o => o.Room)
                        .FirstOrDefaultAsync(o => o.OwnerUserId == owner.User.UserId && o.OccupationStatus);

                    viewModels.Add(new POSTViewModel
                    {
                        User = owner.User,
                        Account = owner.Account,
                        RoomOwner = owner.RoomOwner,
                        Occupant = occupant
                    });
                }

                ViewBag.ShowDeleted = showDeleted;

                return View(viewModels);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading room owner management page");
                TempData["ErrorMessage"] = "An error occurred while loading room owner data.";
                return View(new List<POSTViewModel>());
            }
        }

        //Create RoomOwner Action
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm] POSTViewModel viewModel,
        [FromForm] string Tower,
        [FromForm] string FloorLevel,
        [FromForm] string RoomNumberSuffix,
        [FromForm] IFormFile UserProfilePicture)
        {
            var formData = Request.Form.ToDictionary(x => x.Key, x => x.Value.ToString());
            _logger.LogInformation("Form data: {@FormData}", formData);

            try
            {
                // 1. Validate required inputs
                if (string.IsNullOrEmpty(viewModel.User?.FirstName) ||
                    string.IsNullOrEmpty(viewModel.User?.LastName) ||
                    string.IsNullOrEmpty(viewModel.Account?.Email))
                {
                    return Json(new { success = false, message = "First name, last name and email are required" });
                }

                if (string.IsNullOrEmpty(viewModel.User.ContactNumber))
                {
                    return Json(new { success = false, message = "Contact number is required" });
                }

                if (string.IsNullOrEmpty(viewModel.RoomOwner?.EmergencyContactName) ||
                    string.IsNullOrEmpty(viewModel.RoomOwner?.EmergencyContactNumber))
                {
                    return Json(new { success = false, message = "Emergency contact information is required" });
                }

                if (string.IsNullOrEmpty(Tower) ||
                    string.IsNullOrEmpty(FloorLevel) ||
                    string.IsNullOrEmpty(RoomNumberSuffix))
                {
                    return Json(new { success = false, message = "All room information is required" });
                }

                if (!int.TryParse(FloorLevel, out int floorNumber) || floorNumber < 2 || floorNumber > 15)
                {
                    return Json(new { success = false, message = "Invalid floor level format" });
                }
                // Validate emergency contact person (letters only)
                if (!string.IsNullOrEmpty(viewModel.RoomOwner?.EmergencyContactName) &&
                    !Regex.IsMatch(viewModel.RoomOwner.EmergencyContactName, @"^[a-zA-Z\s]+$"))
                {
                    return Json(new { success = false, message = "Emergency contact person should contain letters only" });
                }
                // Validate emergency contact number is different from primary contact
                if (viewModel.User.ContactNumber == viewModel.RoomOwner?.EmergencyContactNumber)
                {
                    return Json(new { success = false, message = "Emergency contact number cannot be the same as your contact number" });
                }

                // 2. Generate room number
                string towerPrefix = Tower == "Bossa" ? "B" : "A";
                string roomNumber = RoomNumberSuffix;

                // 3. Check if room exists or create new one
                var room = await _context.Rooms.FirstOrDefaultAsync(r => r.RoomNumber == roomNumber);
                if (room == null)
                {
                    room = new Room
                    {
                        RoomNumber = roomNumber,
                        FloorLevel = FloorLevel, 
                        Tower = Tower
                    };
                    _context.Rooms.Add(room);
                    await _context.SaveChangesAsync();
                }

                // 4. Handle profile picture upload
                string profilePicturePath = "/images/default.png";
                if (UserProfilePicture != null && UserProfilePicture.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
                    Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(UserProfilePicture.FileName)}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await UserProfilePicture.CopyToAsync(fileStream);
                    }
                    profilePicturePath = $"/uploads/{uniqueFileName}";
                }

                // 5. Generate password: lastname (lowercase) + 3 random numbers (e.g., "smith123")
                string rawPassword = $"{viewModel.User.LastName.ToLower()}{new Random().Next(100, 999)}";

                // 6. Create user bundle
                var userBundle = new UserDataBundle
                {
                    User = new User
                    {
                        FirstName = viewModel.User.FirstName,
                        MiddleName = viewModel.User.MiddleName,
                        LastName = viewModel.User.LastName,
                        ContactNumber = viewModel.User.ContactNumber,
                        DateCreated = DateOnly.FromDateTime(DateTime.Now),
                        AccountStatus = true
                    },
                    Account = new Account
                    {
                        Email = viewModel.Account.Email,
                        Username = viewModel.Account.Email,
                        Password = HashPassword(rawPassword)
                    },
                    RoomOwner = new RoomOwner
                    {
                        RoomOwnerProfilePicture = profilePicturePath,
                        EmergencyContactName = viewModel.RoomOwner.EmergencyContactName,
                        EmergencyContactNumber = viewModel.RoomOwner.EmergencyContactNumber
                    }
                };

                // 7. Register the user (mode 2 for RoomOwner)
                var registrationErrors = await new POSTViewModel { userbundle = userBundle }
                    .RegisterUserAsync(_context, 2);

                if (registrationErrors.Any())
                {
                    return Json(new { success = false, message = string.Join("<br>", registrationErrors) });
                }

                // 8. Update account with final password
                _context.Accounts.Update(userBundle.Account);

                // 9. Handle room occupancy
                // Deactivate current occupant if exists
                var currentOccupant = await _context.RoomOccupants
                    .FirstOrDefaultAsync(o => o.RoomId == room.RoomId && o.OccupationStatus);

                if (currentOccupant != null)
                {
                    currentOccupant.OccupationStatus = false;
                    currentOccupant.MoveOutDate = DateOnly.FromDateTime(DateTime.Now);
                    _context.RoomOccupants.Update(currentOccupant);
                }

                // Add new occupant
                var newOccupant = new RoomOccupant
                {
                    OwnerUserId = userBundle.User.UserId,
                    RoomId = room.RoomId,
                    MoveInDate = DateOnly.FromDateTime(DateTime.Now),
                    DateAssigned = DateOnly.FromDateTime(DateTime.Now),
                    OccupationStatus = true,
                };

                _context.RoomOccupants.Add(newOccupant);
                await _context.SaveChangesAsync();
                //// After successful creation, send email
                //if (response.success)
                //{
                //    try
                //    {
                //        await SendAccountCreationEmail(
                //            viewModel.Account.Email,
                //            viewModel.User.FirstName,
                //            viewModel.Account.Username,
                //            rawPassword
                //        );
                //    }
                //    catch (Exception ex)
                //    {
                //        _logger.LogError(ex, "Error sending account creation email");
                //        // Don't fail the request if email fails, just log it
                //    }
                //}
                return Json(new
                {
                    success = true,
                    message = "Room owner created successfully!",
                    generatedPassword = rawPassword,
                    userId = userBundle.User.UserId
                });
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating room owner");
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }

        }

        private string HashPassword(string password)
        {
            var hasher = new PasswordHasher<Account>();
            return hasher.HashPassword(null, password);
        }

        // POST: Edit RoomOwner
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(POSTViewModel model, IFormFile UserProfilePicture)
        {
            try
            {
                // Handle profile picture upload
                if (UserProfilePicture != null && UserProfilePicture.Length > 0)
                {
                    var fileName = Guid.NewGuid() + Path.GetExtension(UserProfilePicture.FileName);
                    var filePath = Path.Combine(_environment.WebRootPath, "uploads", fileName);
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await UserProfilePicture.CopyToAsync(stream);
                    }
                    model.RoomOwner.RoomOwnerProfilePicture = "/uploads/" + fileName;
                }

                // Hash the password if it's changed
                if (!string.IsNullOrEmpty(model.Account?.Password))
                {
                    var hasher = new PasswordHasher<Account>();
                    model.Account.Password = hasher.HashPassword(null, model.Account.Password);
                }

                // Call EditRoleUserAsync (mode 2 = RoomOwner)
                var errors = await model.EditRoleUserAsync(_context, 2, model.User.UserId);

                if (errors.Any())
                {
                    TempData["ErrorMessage"] = string.Join("<br>", errors);
                    return RedirectToAction("RoomOwnerManagement");
                }

                TempData["SuccessMessage"] = "Room Owner updated successfully!";
                return RedirectToAction("RoomOwnerManagement");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating room owner");
                TempData["ErrorMessage"] = $"An error occurred while updating the room owner: {ex.Message}";
                return RedirectToAction("RoomOwnerManagement");
            }
        }

        // POST: Validate Unique Fields
        [HttpPost]
        public async Task<JsonResult> ValidateUnique(string email, string username, string contact, string roomNumber, string currentUserId)
        {
            try
            {
                // Email uniqueness (Account)
                var emailExists = await _context.Accounts
                    .AnyAsync(a => a.Email == email && a.AccountId.ToString() != currentUserId);

                // Username uniqueness (Account)
                var usernameExists = await _context.Accounts
                    .AnyAsync(a => a.Username == username && a.AccountId.ToString() != currentUserId);

                // Contact number uniqueness (User)
                var contactExists = await _context.Users
                    .AnyAsync(u => u.ContactNumber == contact && u.UserId != currentUserId);

                // Room number uniqueness (RoomOccupant)
                var roomNumberExists = await _context.RoomOccupants
                    .Include(o => o.Room)
                    .AnyAsync(r => r.Room.RoomNumber == roomNumber && r.OwnerUserId != currentUserId);

                return Json(new
                {
                    emailExists,
                    usernameExists,
                    contactExists,
                    roomNumberExists,
                    isValid = !(emailExists || usernameExists || contactExists || roomNumberExists)
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    error = ex.Message,
                    isValid = false
                });
            }
        }

        // POST: Soft Delete RoomOwner
        [HttpPost]
        public async Task<IActionResult> SoftDelete(string ownerId)
        {
            try
            {
                var postViewModel = new POSTViewModel();
                await postViewModel.DeleteUserAsync(_context, ownerId, 1); // Mode 1 = Soft delete

                return Json(new { success = true, message = "Room Owner hidden successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error soft deleting room owner");
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }

        // POST: Hard Delete RoomOwner
        [HttpPost]
        public async Task<IActionResult> HardDelete(string ownerId)
        {
            try
            {
                var postViewModel = new POSTViewModel();
                await postViewModel.DeleteUserAsync(_context, ownerId, 2); // Mode 2 = Hard delete

                return Json(new { success = true, message = "Room Owner permanently deleted." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error hard deleting room owner");
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }

        private async Task SendAccountCreationEmail(string email, string firstName, string username, string password)
        {
            var subject = "Your Room Owner Account Has Been Created - TAFT Visitor Management System";

            // Create HTML content for the email
            string html = $@"
<div style='max-width:480px;margin:30px auto;padding:24px;border-radius:12px;
    background:#f9f9f9;font-family:Segoe UI,Arial,sans-serif;box-shadow:0 2px 8px #0001;'>
    <div style='text-align:center;margin-bottom:18px;'>
        <h2 style='margin:0;color:#2a2a2a;font-weight:700;letter-spacing:1px;'>TAFT Visitor Management System</h2>
    </div>
    <div style='background:#fff;padding:20px 16px;border-radius:8px;box-shadow:0 1px 4px #0001;'>
        <p style='font-size:1.1em;margin-bottom:12px;'>
            <span style='font-size:1.3em;'>🏠</span>
            <strong>Room Owner Account Created</strong>
        </p>
        <p style='margin:0 0 10px 0;font-size:1em;color:#444;'>
            Welcome to our Visitor Management System, {firstName}!
        </p>
        <div style='background:#e3f0ff;padding:12px;border-radius:6px;margin-bottom:18px;'>
            <p style='margin:0 0 8px 0;font-weight:bold;'>Your account credentials:</p>
            <p style='margin:0 0 5px 0;'><strong>Username:</strong> {username}</p>
            <p style='margin:0;'><strong>Password:</strong> {password}</p>
        </div>
        <p style='margin:0 0 10px 0;font-size:1em;color:#444;'>
            Please keep this information secure and change your password after first login.
        </p>
        <p style='margin:0;font-size:0.95em;color:#888;'>
            If you did not expect this email, please contact the system administrator.
        </p>
    </div>
    <div style='text-align:center;margin-top:18px;font-size:0.9em;color:#aaa;'>
        &copy; {DateTime.Now.Year} TAFT Visitor Management System
    </div>
</div>
";

            // Create a plain text version
            string text = $"TAFT Visitor Management System\n\n" +
                          $"Your room owner account has been created.\n\n" +
                          $"Username: {username}\n" +
                          $"Password: {password}\n\n" +
                          $"Please keep this information secure and change your password after first login.\n\n" +
                          $"Thank you,\nManagement Team";

            // Create an IdentityMessage
            var message = new IdentityMessage
            {
                Destination = email,
                Subject = subject,
                Body = text // Use text as the body, HTML will be added as alternate view
            };

            // Send the email using your EmailService
            await _emailService.SendAsync(message);
        }
    }
}