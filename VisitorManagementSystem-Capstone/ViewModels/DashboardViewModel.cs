using VisitorManagementSystem_Capstone.Models;

namespace VisitorManagementSystem_Capstone.ViewModels
{
    public class DashboardViewModel
    {
        public List<VisitLog> PendingVisits { get; set; }
        public List<VisitLog> VerifiedVisits { get; set; }

        public List<VisitLog> StaffApprovalVisits { get; set; }
    }
}