using Microsoft.AspNetCore.Mvc;

namespace VisitorManagementSystem_Captstone.Controllers
{
    public class NavigationController : Controller
    {
        public IActionResult AdminDashboard()
        {
            return RedirectToAction("Dashboard", "Admin");
        }

        public IActionResult StaffDashboard()
        {
            return RedirectToAction("Dashboard", "Staff");
        }

        public IActionResult RoomOwnerDashboard()
        {
            return RedirectToAction("Dashboard", "RoomOwner");
        }

        public IActionResult VisitorDashboard()
        {
            return RedirectToAction("Dashboard", "Visitor");
        }

        public IActionResult LoginView()
        {
            return RedirectToAction("Login", "Login");
        }

        public IActionResult RegisterView()
        {
            return RedirectToAction("SignUp", "SignUp");
        }
    }
}
