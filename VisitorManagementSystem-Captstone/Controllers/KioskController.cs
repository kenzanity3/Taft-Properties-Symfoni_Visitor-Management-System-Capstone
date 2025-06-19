using Microsoft.AspNetCore.Mvc;

namespace VisitorManagementSystem_Captstone.Controllers
{
    public class KioskController : Controller
    {
        public IActionResult CheckInCheckOutManagement()
        {
            return View();
        }
        public IActionResult FacilityManagement()
        {
            return View();
        }
        public IActionResult WalkInManagement()
        {
            return View();
        }
        public IActionResult CreateVisitorAccount()
        {
            return View();
        }
    }
}
