using Microsoft.AspNetCore.Mvc;

namespace VisitorManagementSystem_Capstone.Controllers
{
    /// <summary>
    /// Controller to handle report generation and display for different types of reports.
    /// </summary>
    public class ReportsController : Controller
    {
        /// <summary>
        /// Displays a report of facility usage.
        /// </summary>
        /// <returns>FacilityUsageReport.cshtml view</returns>
        public IActionResult FacilityUsageReport()
        {
            ViewData["Title"] = "Facility Usage Report";
            return View();
        }

        /// <summary>
        /// Displays a report of visitor history or activities.
        /// </summary>
        /// <returns>VisitorReport.cshtml view</returns>
        public IActionResult VisitorReport()
        {
            ViewData["Title"] = "Visitor Reports";
            return View();
        }

        /// <summary>
        /// Displays a report of staff-related activities.
        /// </summary>
        /// <returns>StaffActivityReport.cshtml view</returns>
        public IActionResult StaffActivityReport()
        {
            ViewData["Title"] = "Staff Activities";
            return View();
        }
    }
}