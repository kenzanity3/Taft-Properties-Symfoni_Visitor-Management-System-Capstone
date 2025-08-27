using Microsoft.AspNetCore.Mvc;
using VisitorManagementSystem_Capstone.Data;
using VisitorManagementSystem_Capstone.Models;
using VisitorManagementSystem_Capstone.ViewModels;

namespace VisitorManagementSystem_Capstone.Controllers
{
    /// <summary>
    /// Controller for managing facility-related operations such as creating, editing, and listing facilities.
    /// Uses dependency injection to obtain the database context.
    /// </summary>
    public class FacilityController : Controller
    {
        // The database context for accessing facility and related data.
        private readonly VisitorManagementSystemDatabaseContext _context;

        /// <summary>
        /// Constructor that receives the database context via dependency injection.
        /// </summary>
        /// <param name="context">The VisitorManagementSystemDatabaseContext instance.</param>
        public FacilityController(VisitorManagementSystemDatabaseContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Displays the Facility Management dashboard with a list of facilities.
        /// </summary>
        public async Task<IActionResult> FacilityManagement()
        {
            var postViewModel = new POSTViewModel();
            var facilities = await postViewModel.GetDataListAsync(_context, 8); // 8 = Facility
                                                                                // Cast to List<Facility> for the view
            return View(facilities.OfType<Facility>().ToList());
        }

        /// <summary>
        /// Displays the page to end a facility session.
        /// </summary>
        public IActionResult EndSession()
        {
            return View();
        }

        /// <summary>
        /// Displays the page to generate a QR code for facility pass.
        /// </summary>
        public IActionResult FacilityGenerateQRCode()
        {
            return View();
        }

        /// <summary>
        /// Displays the list or form for facility passes.
        /// </summary>
        public IActionResult FacilityPass()
        {
            return View();
        }

        /// <summary>
        /// Handles the POST request to create a facility from a modal form.
        /// </summary>
        /// <param name="facility">The facility model to create.</param>
        /// <returns>Redirects to Dashboard on success, otherwise returns the dashboard view with errors.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateFacility(Facility facility)
        {
            if (ModelState.IsValid)
            {
                _context.Facilities.Add(facility);
                _context.SaveChanges();

                TempData["SuccessMessage"] = "Facility successfully created!";
                return RedirectToAction(nameof(FacilityManagement));
            }

            // If invalid, re-display form with errors (optional)
            var facilities = _context.Facilities.ToList();
            return View("FacilityManagement", facilities);
        }

        /// <summary>
        /// Handles the POST request to update a facility.
        /// </summary>
        /// <param name="facility">The updated facility model.</param>
        /// <returns>Redirects to EditSuccessful on success, otherwise returns the edit view with errors.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Facility facility)
        {
            if (ModelState.IsValid)
            {
                _context.Facilities.Update(facility);
                _context.SaveChanges();
                return RedirectToAction(nameof(EditSuccessful));
            }
            return View(facility);
        }

        /// <summary>
        /// Displays a confirmation view after a successful edit.
        /// </summary>
        /// <returns>The edit successful view.</returns>
        public IActionResult EditSuccessful()
        {
            ViewBag.Message = "Facility updated successfully!";
            return View();
        }

        /// <summary>
        /// Handles the POST request to edit a facility from a modal form.
        /// </summary>
        /// <param name="facility">The updated facility model.</param>
        /// <returns>Redirects to Dashboard on success, otherwise returns the dashboard view with errors.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditFacility(Facility facility)
        {
            if (ModelState.IsValid)
            {
                _context.Facilities.Update(facility);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Facility successfully edited!";
                return RedirectToAction(nameof(FacilityManagement));
            }
            var facilities = _context.Facilities.ToList();
            return View("FacilityManagement", facilities);
        }
    }
}