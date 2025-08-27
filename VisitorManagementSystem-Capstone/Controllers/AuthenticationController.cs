using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using VisitorManagementSystem_Capstone.Data;
using VisitorManagementSystem_Capstone.Models;
using VisitorManagementSystem_Capstone.Services;
using VisitorManagementSystem_Capstone.ViewModels;

namespace VisitorManagementSystem_Capstone.Controllers
{
    public class AuthenticationController : Controller
    {
        private readonly VisitorManagementSystemDatabaseContext _context;
        private readonly EmailService _emailService;
        private readonly LocalImageService _imageService;

        // Update constructor to include LocalImageService
        public AuthenticationController(
            VisitorManagementSystemDatabaseContext context,
            EmailService emailService,
            LocalImageService imageService)
        {
            _context = context;
            _emailService = emailService;
            _imageService = imageService; // Initialize the service
        }

        // GET: Display the login page
        public IActionResult Login()
        {
            return View();
        }

        // GET: Display the registration (Sign Up) page
        public IActionResult SignUp()
        {
            return View();
        }

        // GET: Display all users (likely for debugging/admin purposes)
        [HttpGet]
        public async Task<IActionResult> AllUsers() // Or LoginView()
        {
            return View(await _context.Users.ToListAsync());
        }

        // POST: Handle user logout
        public IActionResult Logout()
        {
            // Clear all temporary data
            TempData.Clear();
            // Clear session data
            HttpContext.Session.Clear();
            // Set SweetAlert message for logout
            TempData["LogoutMessage"] = "Logout successful!";
            // Redirect to login page
            return RedirectToAction("Login");
        }

        // POST: Handle user login
        // In the Login method, optimize database queries
        [HttpPost]
        public async Task<IActionResult> Login(Account user)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(user.Username) || string.IsNullOrWhiteSpace(user.Password))
                {
                    TempData["LoginErrorMessage"] = "Please enter both your username and password.";
                    return View("Login");
                }

                // Find account with related user data in a single query
                var account = await _context.Accounts
                    .Include(a => a.RoomOwners)
                    .Include(a => a.Staffs)
                    .Include(a => a.Admins)
                    .FirstOrDefaultAsync(a => a.Username.ToLower() == user.Username.ToLower());

                if (account == null)
                {
                    TempData["LoginErrorMessage"] = "Incorrect Username or Password!";
                    return View("Login");
                }

                // Verify password first to avoid unnecessary queries if password is wrong
                var passwordHasher = new PasswordHasher<Account>();
                var passwordVerificationResult = passwordHasher.VerifyHashedPassword(account, account.Password, user.Password);

                if (passwordVerificationResult != PasswordVerificationResult.Success)
                {
                    TempData["LoginErrorMessage"] = "Incorrect Username or Password!";
                    return View("Login");
                }

                // Find user based on account type with a more efficient query
                User userRegistered = null;
                string accountType = "Unknown";

                if (account.RoomOwners.Any())
                {
                    var roomOwner = account.RoomOwners.First();
                    userRegistered = await _context.Users
                        .FirstOrDefaultAsync(u => u.UserId == roomOwner.UserId && u.AccountStatus == true);
                    accountType = "RoomOwner";

                    // Check occupancy status if RoomOwner
                    var occupant = await _context.RoomOccupants
                        .FirstOrDefaultAsync(o => o.OwnerUserId == roomOwner.UserId);
                    if (occupant != null && occupant.OccupationStatus != true)
                    {
                        TempData["LoginErrorMessage"] = "Your account is not active. Please contact admin.";
                        return View("Login");
                    }
                }
                else if (account.Staffs.Any())
                {
                    var staff = account.Staffs.First();
                    userRegistered = await _context.Users
                        .FirstOrDefaultAsync(u => u.UserId == staff.UserId && u.AccountStatus == true);
                    accountType = "Staff";
                }
                else if (account.Admins.Any())
                {
                    var admin = account.Admins.First();
                    userRegistered = await _context.Users
                        .FirstOrDefaultAsync(u => u.UserId == admin.UserId && u.AccountStatus == true);
                    accountType = "Admin";
                }

                if (userRegistered == null)
                {
                    TempData["LoginErrorMessage"] = "Account not active or not found!";
                    return View("Login");
                }

                // Store user session data
                HttpContext.Session.SetString("UserId", userRegistered.UserId);

                var fullName = string.IsNullOrWhiteSpace(userRegistered.MiddleName)
                    ? $"{userRegistered.FirstName} {userRegistered.LastName}"
                    : $"{userRegistered.FirstName} {userRegistered.MiddleName} {userRegistered.LastName}";

                HttpContext.Session.SetString("accountLoggedIn", fullName);
                HttpContext.Session.SetString("accountType", accountType);

                // Set success message
                TempData["LoginSuccess"] = "true";
                TempData["LoginSuccessMessage"] = $"Welcome back, {fullName}! Redirecting to your dashboard...";
                TempData["AccountType"] = accountType;

                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                // Log the exception here
                TempData["LoginErrorMessage"] = "Login Error. Please try again.";
                return View("Login");
            }
        }

        // POST: Handle user registration
        [HttpPost]
        public async Task<IActionResult> SignUp(POSTViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                // Check for duplicate contact number
                bool contactExists = await _context.Users.AnyAsync(u => u.ContactNumber == viewModel.User.ContactNumber);
                if (contactExists)
                {
                    ModelState.AddModelError("User.ContactNumber", "This contact number is already registered.");
                    return View(viewModel);
                }

                // Default picture
                string profilePicturePath = "/images/default.png";

                // If a file was uploaded, try saving it
                if (viewModel.ProfilePictureFile != null && viewModel.ProfilePictureFile.Length > 0)
                {
                    try
                    {
                        // Validate file type
                        var validTypes = new[] { "image/jpeg", "image/png" };
                        if (!validTypes.Contains(viewModel.ProfilePictureFile.ContentType.ToLower()))
                        {
                            ModelState.AddModelError("ProfilePictureFile", "Only JPG and PNG images are allowed.");
                            return View(viewModel);
                        }

                        // Validate file size (2MB)
                        if (viewModel.ProfilePictureFile.Length > 2 * 1024 * 1024)
                        {
                            ModelState.AddModelError("ProfilePictureFile", "Image must be less than 2MB.");
                            return View(viewModel);
                        }

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

                // Use the POSTViewModel's method to generate ID (better approach)
                var postViewModel = new POSTViewModel();
                string newUserId = await postViewModel.GenerateNextVisitorIdAsync(_context, "VIS");

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

                var visitor = new Visitor
                {
                    UserId = user.UserId,
                    ProfilePicture = profilePicturePath
                };

                _context.Users.Add(user);
                _context.Visitors.Add(visitor);
                await _context.SaveChangesAsync();

                // Prepare success response
                TempData["RegistrationSuccess"] = "true";
                TempData["RegisterMessage"] = $"Welcome {user.FirstName} {user.LastName}! Your registration was successful.";

                return RedirectToAction("SignUp");
            }

            return View(viewModel);
        }

        //Handle Contact Number Validation
        [HttpGet]
        public IActionResult CheckContactNumber(string contact)
        {
            bool exists = _context.Users.Any(u => u.ContactNumber == contact);
            return Json(new { isUnique = !exists });
        }

        // GET: Display forgot password page
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: Handle password reset
        [HttpPost]
        public async Task<IActionResult> ForgotPassword(Account account, string verificationCode)
        {
            ViewData["ErrorVerification"] = "";
            TempData["ForgotPassMessage"] = "";

            // Password length check
            if (string.IsNullOrEmpty(account.Password) || account.Password.Length < 8)
            {
                ViewData["ErrorVerification"] = "Password must be at least 8 characters long.";
                ViewData["KeepVerificationCode"] = verificationCode;
                return View(account);
            }

            // Retrieve stored verification data from session
            string? storedCode = HttpContext.Session.GetString("VerificationCode");
            string? storedEmail = HttpContext.Session.GetString("VerificationEmail");

            // Validate verification session
            if (storedCode == null || storedEmail == null || storedEmail != account.Email)
            {
                ViewData["ErrorVerification"] = "Verification failed. Please try again.";
                return View(account);
            }

            // Verify code matches
            if (storedCode != verificationCode)
            {
                ViewData["ErrorVerification"] = "Incorrect verification code.";
                return View(account);
            }

            // Find account by email
            var accountEntity = await _context.Accounts.SingleOrDefaultAsync(a => a.Email == account.Email);

            if (accountEntity != null)
            {
                // Check if the new password is the same as the old one
                var passwordHasher = new PasswordHasher<Account>();
                var passwordVerificationResult = passwordHasher.VerifyHashedPassword(accountEntity, accountEntity.Password, account.Password);
                if (passwordVerificationResult == PasswordVerificationResult.Success)
                {
                    ViewData["ErrorVerification"] = "New password cannot be the same as the previous password.";
                    ViewData["KeepVerificationCode"] = verificationCode;
                    return View(account);
                }

                // Hash and save new password
                accountEntity.Password = passwordHasher.HashPassword(accountEntity, account.Password);
                await _context.SaveChangesAsync();

                TempData["ForgotPassMessage"] = $"Password successfully updated for {accountEntity.Username}";
                TempData["ShowSuccess"] = "true";
                return RedirectToAction("ForgotPassword");
            }

            ViewData["ErrorVerification"] = "Account details are incorrect. Please try again.";
            return View(account);
        }

        // POST: Send verification code for password reset
        [HttpPost]
        public async Task<IActionResult> SendVerificationCode(string email)
        {
            var accountEntity = await _context.Accounts.SingleOrDefaultAsync(a => a.Email == email);

            if (accountEntity == null)
            {
                TempData["ForgotPassMessage"] = "No account associated with this email.";
                TempData["Success"] = "false";
            }
            else
            {
                var verificationCode = new Random().Next(100000, 999999).ToString();
                HttpContext.Session.SetString("VerificationCode", verificationCode);
                HttpContext.Session.SetString("VerificationEmail", email);

                await _emailService.SendVerificationEmail(email, verificationCode);

                TempData["ForgotPassMessage"] = "Verification code sent. Check your email.";
                TempData["Success"] = "true";
            }

            return Json(new { success = TempData["Success"], message = TempData["ForgotPassMessage"] });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}