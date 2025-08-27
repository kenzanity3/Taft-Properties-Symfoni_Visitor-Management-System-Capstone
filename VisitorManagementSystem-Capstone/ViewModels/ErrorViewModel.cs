namespace VisitorManagementSystem_Capstone.ViewModels
{
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }
        public string ErrorMessage { get; set; }  // Add this property
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}