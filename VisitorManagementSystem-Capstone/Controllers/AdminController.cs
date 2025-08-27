using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using VisitorManagementSystem_Capstone.Data;
using VisitorManagementSystem_Capstone.ViewModels;

namespace VisitorManagementSystem_Capstone.Controllers
{
    public class AdminController : Controller
    {
        private readonly VisitorManagementSystemDatabaseContext _context;
        private readonly LocalImageService _imageService;

        public AdminController(VisitorManagementSystemDatabaseContext context, LocalImageService imageService)
        {
            _context = context;
            _imageService = imageService;
        }

        public IActionResult DashboardAdmin()
        {
            ViewData["Title"] = "Dashboard Admin";
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}