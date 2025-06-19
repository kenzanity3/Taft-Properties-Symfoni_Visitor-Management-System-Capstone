using Microsoft.AspNetCore.Mvc;

namespace VisitorManagementSystem_Captstone.Controllers
{
    public class VisitorController : Controller
    {
        public IActionResult Dashboard()
        {
            return View();
        }
    }
}
