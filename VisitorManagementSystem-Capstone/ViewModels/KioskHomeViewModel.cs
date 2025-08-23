namespace VisitorManagementSystem_Capstone.ViewModels
{
    public class CheckInOutViewModel
    {
        public string VisitorName { get; set; }

        public string? VisitCode { get; set; }
        public string? Tower { get; set; }

        public string CheckInTime { get; set; }
        public string CheckOutTime { get; set; }
    }

    public class KioskHomeViewModel
    {
        public List<CheckInOutViewModel> CheckIns { get; set; }
        public List<CheckInOutViewModel> CheckOuts { get; set; }
    }
}
