using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem_Capstone.Data;
using VisitorManagementSystem_Capstone.Models;
using VisitorManagementSystem_Capstone.ViewModels;

namespace VisitorManagementSystem_Capstone.Controllers
{
    /// <summary>
    /// Controller for admin user management operations such as creating and listing Room Owners.
    /// </summary>
    public class VisitorManagementController : Controller
    {
        // The database context is injected via dependency injection.
        private readonly VisitorManagementSystemDatabaseContext _context;

        private readonly LocalImageService _imageService; // LocalImage

        /// <summary>
        /// Constructor that receives the database context via dependency injection.
        /// </summary>
        /// <param name="context">The VisitorManagementSystemDatabaseContext instance.</param>
        public VisitorManagementController(VisitorManagementSystemDatabaseContext context, LocalImageService imageService)
        {
            _context = context;
            _imageService = imageService;
        }

        /// <summary>
        /// Returns the main index view for admin visitor management.
        /// </summary>
        // ✅ VIEW: /VisitorManagement/VisitorManagement.cshtml
        public async Task<IActionResult> VisitorManagement()
        {
            ViewData["Title"] = "Visitor Management";

            var postViewModel = new POSTViewModel();
            var visitorBundles = await postViewModel.GetUserRoleList(_context, 1); // 1 = Visitor

            return View(visitorBundles);
        }

        // ✅ VIEW: /VisitorManagement/AppointmentList.cshtml
        public IActionResult AppointmentList()
        {
            ViewData["Title"] = "Appointment List";
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateVisitor(POSTViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Invalid input. Please check the form and try again.";
                return RedirectToAction("VisitorManagement");
            }

            // 1️⃣ Contact number validation
            var contactNumber = viewModel.User?.ContactNumber?.Trim();
            if (string.IsNullOrWhiteSpace(contactNumber))
            {
                TempData["ErrorMessage"] = "Contact number is required.";
                return RedirectToAction("VisitorManagement");
            }

            bool contactExists = _context.Users.Any(u => u.ContactNumber == contactNumber);
            if (contactExists)
            {
                TempData["ErrorMessage"] = "This contact number is already registered.";
                return RedirectToAction("VisitorManagement");
            }

            // 2️⃣ Default picture path
            string profilePicturePath = "/images/default.png";

            // 3️⃣ Handle file upload if provided
            if (viewModel.ProfilePictureFile != null && viewModel.ProfilePictureFile.Length > 0)
            {
                // Check file extension
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".jfif" };
                var fileExtension = Path.GetExtension(viewModel.ProfilePictureFile.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    TempData["ErrorMessage"] = "Invalid file type. Only JPG, JPEG, JFIF and PNG are only allowed.";
                    return RedirectToAction("VisitorManagement");
                }

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
                    TempData["ErrorMessage"] = $"Image upload failed: {ex.Message}";
                    return RedirectToAction("VisitorManagement");
                }
            }

            // Use POSTViewModel's thread-safe method to generate ID
            var postViewModel = new POSTViewModel();
            string newUserId = await postViewModel.GenerateNextVisitorIdAsync(_context, "VIS");

            // 4️⃣ Prepare User and Visitor objects
            var user = new User
            {
                UserId = newUserId,
                FirstName = viewModel.User?.FirstName,
                MiddleName = viewModel.User?.MiddleName,
                LastName = viewModel.User?.LastName,
                ContactNumber = contactNumber,
                DateCreated = DateOnly.FromDateTime(DateTime.Now),
                AccountStatus = true
            };

            var visitor = new Visitor
            {
                UserId = user.UserId,
                ProfilePicture = profilePicturePath
            };

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

            // 5️⃣ Save to DB
            var errors = await postViewModel.RegisterUserAsync(_context, 1);
            if (errors.Any())
            {
                TempData["ErrorMessage"] = string.Join(", ", errors);
                return RedirectToAction("VisitorManagement");
            }

            TempData["SuccessMessage"] = $"Visitor {user.FirstName} {user.LastName} created successfully!";
            return RedirectToAction("VisitorManagement");
        }

        // ✅ EDIT: VISITOR
        [HttpPost]
        public async Task<IActionResult> EditVisitor(POSTViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Invalid input. Please check the form and try again.";
                return RedirectToAction("VisitorManagement");
            }

            // 1️⃣ Contact number validation
            var contactNumber = viewModel.User?.ContactNumber?.Trim();
            if (string.IsNullOrWhiteSpace(contactNumber))
            {
                TempData["ErrorMessage"] = "Contact number is required.";
                return RedirectToAction("VisitorManagement");
            }

            // Check if contact number exists for other users
            bool contactExists = await _context.Users
                .AnyAsync(u => u.ContactNumber == contactNumber && u.UserId != viewModel.User.UserId);

            if (contactExists)
            {
                TempData["ErrorMessage"] = "This contact number is already registered by another user.";
                return RedirectToAction("VisitorManagement");
            }

            // 2️⃣ Handle file upload if provided
            string profilePicturePath = null;
            if (viewModel.ProfilePictureFile != null && viewModel.ProfilePictureFile.Length > 0)
            {
                // Check file extension
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".jfif" };
                var fileExtension = Path.GetExtension(viewModel.ProfilePictureFile.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    TempData["ErrorMessage"] = "Invalid file type. Only JPG, JPEG, JFIF and PNG are allowed.";
                    return RedirectToAction("VisitorManagement");
                }

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
                    TempData["ErrorMessage"] = $"Image upload failed: {ex.Message}";
                    return RedirectToAction("VisitorManagement");
                }
            }

            // 3️⃣ Prepare the view model for editing
            var postViewModel = new POSTViewModel
            {
                User = viewModel.User,
                Visitor = new Visitor
                {
                    UserId = viewModel.User.UserId,
                    ProfilePicture = profilePicturePath // Will be updated only if new image was uploaded
                },
                userbundle = new UserDataBundle
                {
                    User = viewModel.User,
                    Visitor = new Visitor { UserId = viewModel.User.UserId }
                }
            };

            // 4️⃣ Update the visitor
            var errors = await postViewModel.EditRoleUserAsync(_context, 1, viewModel.User.UserId); // 1 = Visitor
            if (errors.Any())
            {
                TempData["ErrorMessage"] = string.Join(", ", errors);
                return RedirectToAction("VisitorManagement");
            }

            // 5️⃣ Update profile picture if it was changed
            if (!string.IsNullOrEmpty(profilePicturePath))
            {
                var visitor = await _context.Visitors.FindAsync(viewModel.User.UserId);

                {
                    visitor.ProfilePicture = profilePicturePath;
                    _context.Visitors.Update(visitor);
                    await _context.SaveChangesAsync();
                }
            }

            TempData["SuccessMessage"] = $"Visitor {viewModel.User.FirstName} {viewModel.User.LastName} updated successfully!";
            return RedirectToAction("VisitorManagement");
        }

        //Contact Number Validation
        [HttpGet]
        public async Task<IActionResult> CheckContactNumberExists(string number, string currentUserId = null)
        {
            if (string.IsNullOrWhiteSpace(number))
            {
                return Json(new { exists = false });
            }

            // Check if the number exists and doesn't belong to the current user (for edit)
            var query = _context.Users.Where(u => u.ContactNumber == number);

            if (!string.IsNullOrEmpty(currentUserId))
            {
                query = query.Where(u => u.UserId != currentUserId);
            }

            var exists = await query.AnyAsync();
            return Json(new { exists });
        }

        public async Task<IActionResult> InfoVisitor(string id)
        {
            var postViewModel = new POSTViewModel();
            var bundle = await postViewModel.GetUserRoleAsync(_context, 1, id); // 1 = Visitor
            if (bundle == null) return NotFound();
            return View("InfoVisitor", bundle); // Create InfoVisitor.cshtml as needed
        }

        [HttpPost]
        public async Task<IActionResult> SoftDeleteVisitor(string id)
        {
            var postViewModel = new POSTViewModel();
            await postViewModel.DeleteUserAsync(_context, id, 2); // 2 = Soft delete
            return RedirectToAction("VisitorManagement");
        }
    }
}