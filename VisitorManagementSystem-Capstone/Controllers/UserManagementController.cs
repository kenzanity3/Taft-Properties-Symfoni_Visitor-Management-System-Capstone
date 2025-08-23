using Microsoft.AspNetCore.Mvc;
using VisitorManagementSystem_Capstone.Data;
using VisitorManagementSystem_Capstone.ViewModels;
using VisitorManagementSystem_Capstone.Models;
using Microsoft.EntityFrameworkCore;

namespace VisitorManagementSystem_Capstone.Controllers
{
    /// <summary>
    /// Controller for admin user management operations such as creating and listing Room Owners.
    /// </summary>
    public class UserManagementController : Controller
    {
        // The database context is injected via dependency injection.
        private readonly VisitorManagementSystemDatabaseContext _context;

        /// <summary>
        /// Constructor that receives the database context via dependency injection.
        /// </summary>
        /// <param name="context">The VisitorManagementSystemDatabaseContext instance.</param>
        public UserManagementController(VisitorManagementSystemDatabaseContext context)
        {
            _context = context;
        }
        /// <summary>
        /// Returns the main index view for admin user management.
        /// </summary>
        public IActionResult UserManagement()
        {
            var viewModel = new POSTViewModel
            {
                // Optionally initialize properties like lists here if needed
            };

            ViewData["Title"] = "User Management";
            return View(viewModel);
        }

        /// <summary>
        /// Returns the Assign Admin view.
        /// </summary>
        public IActionResult AssignAdmin()
        {
            ViewData["Title"] = "Assign Admin";
            return View();
        }
    }
}
