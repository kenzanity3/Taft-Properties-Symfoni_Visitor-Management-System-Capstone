namespace VisitorManagementSystem_Capstone.ViewModels
{
    public class VisitorReportItem
    {
            public string Date { get; set; }
            public string VisitorName { get; set; }
            public string ContactNumber { get; set; }
            public string RoomNumber { get; set; }
            public string RoomOwnerName { get; set; }
            public string Purpose { get; set; }
            public string Status { get; set; }
            public string CheckInTime { get; set; }
        }

        public class VisitorTrendItem
        {
            public string Period { get; set; }
            public int TotalVisitors { get; set; }
            public int Approved { get; set; }
            public int Pending { get; set; }
            public int Declined { get; set; }
        }
    }
