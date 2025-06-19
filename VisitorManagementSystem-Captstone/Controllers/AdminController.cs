using Microsoft.AspNetCore.Mvc;

namespace VisitorManagementSystem_Captstone.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Dashboard()
        {
            return View();
        }
        public IActionResult Create()
        {
            return View();
        }
        public IActionResult Report()
        {
            return View();
        }
        public IActionResult Facility()
        {
            return View();
        }
    }
}
