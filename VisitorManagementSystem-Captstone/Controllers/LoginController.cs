using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem_Captstone.Data;
using VisitorManagementSystem_Captstone.Models;
using VisitorManagementSystem_Captstone.Services;
using Microsoft.AspNetCore.Identity;

namespace VisitorManagementSystem_Captstone.Controllers
{
    public class LoginController : Controller
    {
        private readonly VisitorManagementSystemDatabaseContext _context;    // Database context for accessing user data
        private readonly EmailService _emailService;                         // Email service for sending verification codes

        // Constructor with dependency injection
        public LoginController(VisitorManagementSystemDatabaseContext context, EmailService emailService)
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
                // Find active user with matching username
                var userRegistered = _context.Users
                    .Where(q => q.IsActive == true)
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
                        HttpContext.Session.SetInt32("UserId", userRegistered.UserId);

                        // Store user details in session
                        HttpContext.Session.SetString("accountLoggedIn", userRegistered.FirstName);
                        HttpContext.Session.SetString("accountType", userRegistered.AccountType);
                        TempData["SuccessMessage"] = "You have logged in successfully!";
                        // Redirect based on account type 
                        return userRegistered.AccountType switch
                        {
                            "Admin" => RedirectToAction("Index", "Dashboard"),
                            //"KIOSK" => RedirectToAction("TicketCheckIn", "Tickets"),
                            //"Staff" => RedirectToAction("Index", "Bookings"),
                            //"Visitor" => RedirectToAction("Index", "Visitor"),
                            _ => View("Index") // Default case
                        };
                    }
                }

                // If authentication fails
                ViewData["LoginErrorMessage"] = "Incorrect Username or Password!";
                return View("Index");
            }

            // If password is empty
            ViewData["LoginErrorMessage"] = "Incorrect Username or Password!";
            return View("Index");
        }
        // GET: Display the registration (Sign Up) page
        public IActionResult SignUp()
        {
            return View();
        }
        //Get SignUp Page
        [HttpPost]
        public async Task<IActionResult> SignUp(User user)
        {
            if (ModelState.IsValid)
            {
                // Check if username already exists
                if (_context.Users.Any(u => u.Username == user.Username))
                {
                    ModelState.AddModelError("Username", "Username already taken.");
                    return View(user);
                }

                // Hash the password
                var passwordHasher = new PasswordHasher<User>();
                user.Password = passwordHasher.HashPassword(user, user.Password);

                // Default account settings
                user.IsActive = true;
                user.AccountType = "Visitor"; // Or based on form input

                // Save user to DB
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Redirect to login after successful registration
                TempData["RegisterMessage"] = "Registration successful. Please log in.";
                return RedirectToAction("Index");
            }

            return View(user);
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
            string storedCode = HttpContext.Session.GetString("VerificationCode");
            string storedEmail = HttpContext.Session.GetString("VerificationEmail");

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
