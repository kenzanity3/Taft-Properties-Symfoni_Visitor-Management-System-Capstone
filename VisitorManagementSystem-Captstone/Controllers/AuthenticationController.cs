using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem_Captstone.Data;
using VisitorManagementSystem_Captstone.NewModel;
using VisitorManagementSystem_Captstone.Services;
using Microsoft.AspNetCore.Identity;
using VisitorManagementSystem_Captstone.ViewModels;

namespace VisitorManagementSystem_Captstone.Controllers
{
    public class AuthenticationController : Controller
    {
        private readonly testContext _context;    // Database context for accessing user data
        private readonly EmailService _emailService;  // Email service for sending verification codes

        // Constructor with dependency injection
        public AuthenticationController(testContext context, EmailService emailService)
        {
            _context = context;           // Initialize database context
            _emailService = emailService; // Initialize email service
        }
    
        // GET: Display the login page
        public IActionResult Login()
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
            // Redirect to login page
            return RedirectToAction("Login");
        }
       
        // POST: Handle user login
        [HttpPost]
        public IActionResult Login(User user)
        {
            // Only proceed if password is provided
            if (!string.IsNullOrEmpty(user.Password))
            {
                // Find active user with matching Email
                var userRegistered = _context.Users
                    .Where(q => q.AccountStatus == true)
                    .AsEnumerable()
                    .SingleOrDefault(q => q.Username == user.Username);

                // If user exists
                if (userRegistered != null)
                {
                    // Verify password against stored hash
                    var passwordHasher = new PasswordHasher<User>();
                    var passwordVerificationResult = passwordHasher.VerifyHashedPassword(userRegistered,userRegistered.Password,user.Password);

                    // If password is correct
                    if (passwordVerificationResult == PasswordVerificationResult.Success)
                    {
                        // Store user ID in TempData and Session
                        TempData["UserId"] = userRegistered.UserId;
                        ViewData["LoginErrorMessage"] = null;
                        HttpContext.Session.SetString("UserId", userRegistered.UserId);

                        var fullName = string.IsNullOrWhiteSpace(userRegistered.MiddleName)
                                        ? $"{userRegistered.FirstName} {userRegistered.LastName}"
                                        : $"{userRegistered.FirstName} {userRegistered.MiddleName} {userRegistered.LastName}";

                        // Store user details in session
                        HttpContext.Session.SetString("accountLoggedIn", fullName);
                        string accountType = (true) switch
                        {
                            _ when _context.Admins.Any(a => a.UserId == userRegistered.UserId) => "Admin",
                            _ when _context.Staffs.Any(s => s.UserId == userRegistered.UserId) => "Staff",
                            _ when _context.RoomOwners.Any(r => r.UserId == userRegistered.UserId) => "RoomOwner",
                            _ when _context.Visitors.Any(v => v.UserId == userRegistered.UserId) => "Visitor",
                            _ => "Unknown"
                        };
                        HttpContext.Session.SetString("accountType", $"{accountType}");
                        TempData["SuccessMessage"] = "You have logged in successfully!";
                        // Redirect based on account type 
                        return HttpContext.Session.GetString("accountType") switch
                        {
                            "Admin" => RedirectToAction("Dashboard", "Admin"),                         
                            "Staff" => RedirectToAction("Dashboard", "Staff"),
                            "RoomOwner" => RedirectToAction("Dashboard", "RoomOwner"),
                            "Visitor" => RedirectToAction("Dashboard", "Visitor"),
                            _ => View("Login") // Default case
                        };
                    }
                }

                // If authentication fails
                ViewData["LoginErrorMessage"] = "Incorrect Username or Password!";
                return View("Login");
            }

            // If password is empty
            ViewData["LoginErrorMessage"] = "Incorrect Username or Password!";
            return RedirectToAction("Dashboard", "Admin");
        }
        // GET: Display the registration (Sign Up) page
        public IActionResult SignUp()
        {
            return View();
        }
        //Get SignUp Page
        [HttpPost]
        public async Task<IActionResult> SignUp(RegisterViewModel viewModel)
        {
 
            if (ModelState.IsValid)
            {
              
                var user = viewModel.User; // Get user from ViewModel
                var visitor = viewModel.Visitor; // Get visitor from ViewModel
                
                // Check if username already exists
                if (_context.Users.Any(u => u.Username == user.Username))
                {
                    ModelState.AddModelError("Username", "Username already taken.");
                    return View(viewModel);
                }

                // Hash the password
                var passwordHasher = new PasswordHasher<User>();
                viewModel.User.Password = passwordHasher.HashPassword(user, user.Password);

                // Default account settings
                user.AccountStatus = true;
                visitor!.UserId = user.UserId;
                var errors = await viewModel.RegisterAsync(_context);
                if (errors.Any())
                {
                    foreach (var error in errors)
                        ModelState.AddModelError(string.Empty, error);
                    return View(viewModel);
                }
             
                TempData["RegisterMessage"] = "Registration successful. Please log in.";
                return RedirectToAction("Login");
            }

            return View(viewModel);
        }

        // GET: Display forgot password page
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: Handle password reset
        [HttpPost]
        public async Task<IActionResult> ForgotPassword(User user, string verificationCode)
        {
            // Initialize error messages
            ViewData["ErrorVerification"] = "";
            TempData["ForgotPassMessage"] = "";

            // Retrieve stored verification data from session
            string? storedCode = HttpContext.Session.GetString("VerificationCode");
            string? storedEmail = HttpContext.Session.GetString("VerificationEmail");

            // Validate verification session
            if (storedCode == null || storedEmail == null || storedEmail != user.Email)
            {
                ViewData["ErrorVerification"] = "Verification failed. Please try again.";
                return View();
            }

            // Verify code matches
            if (storedCode != verificationCode)
            {
                ViewData["ErrorVerification"] = "Incorrect verification code.";
                return View();
            }

            // Find user by email
            var userAccount = await _context.Users.SingleOrDefaultAsync(q => q.Email == user.Email);

            //Condition to check if user account exists
            if (userAccount != null)
            {
                // Hash and save new password
                var passwordHasher = new PasswordHasher<User>();
                userAccount.Password = passwordHasher.HashPassword(userAccount, user.Password);
                await _context.SaveChangesAsync();

                TempData["ForgotPassMessage"] = "Your password has been successfully updated.";
                return RedirectToAction("Index");
            }

            ViewData["ErrorVerification"] = "User account details are incorrect. Please try again.";
            return View();
        }

        // POST: Send verification code for password reset
        [HttpPost]
        public async Task<IActionResult> SendVerificationCode(string email)
        {
            // Check if email exists in system
            var userAccount = await _context.Users.SingleOrDefaultAsync(q => q.Email == email);

            if (userAccount == null)
            {
                TempData["ForgotPassMessage"] = "No account associated with this email.";
                TempData["Success"] = "false";
            }
            else
            {
                // Generate 6-digit verification code
                var verificationCode = new Random().Next(100000, 999999).ToString();

                // Store code and email in session
                HttpContext.Session.SetString("VerificationCode", verificationCode);
                HttpContext.Session.SetString("VerificationEmail", email);

                // Send email with verification code
                await _emailService.SendVerificationEmail(email, verificationCode);

                TempData["ForgotPassMessage"] = "Verification code sent. Check your email.";
                TempData["Success"] = "true";
            }

            // Return JSON response for AJAX call
            return Json(new { success = TempData["Success"], message = TempData["ForgotPassMessage"] });
        }
    }
}
