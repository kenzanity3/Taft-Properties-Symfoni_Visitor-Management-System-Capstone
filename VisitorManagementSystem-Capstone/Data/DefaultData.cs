using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using VisitorManagementSystem_Capstone.Models;

namespace VisitorManagementSystem_Capstone.Data
{
    public static class DefaultData
    {
        public static List<Facility> FacilitiesData { get; } = new List<Facility>
        {
            new Facility { FacilityId = "Gym001", Name = "Gymnastic", Description = "A fully equipped fitness center featuring cardio machines, free weights, and strength training areas.", Capacity = 5, OpenTime = new TimeOnly(10,0), ClosingTime = new TimeOnly(20,0), FacilityStatus = true },
            new Facility { FacilityId = "Pool001",  Name = "Swimming Pool", Description = "An indoor heated swimming pool suitable for recreational and lap swimming, with locker and shower facilities.", Capacity = 5, OpenTime = new TimeOnly(10,0), ClosingTime = new TimeOnly(20,0) , FacilityStatus = true},
            new Facility { FacilityId = "Study001", Name = "Study Room", Description = "A quiet, well-lit space with individual desks and power outlets, ideal for reading, studying, and group discussions.", Capacity = 5, OpenTime = new TimeOnly(10,0), ClosingTime = new TimeOnly(20,0), FacilityStatus = true}
        };

        public static List<string> VisitorTypesData { get; } = new List<string>
        {
            "Guest",
            "Delivery",
            "Service"
        };

        public static List<string> VerificationStatusData { get; } = new List<string>
        {
            "Pending",
            "Inactive",
            "Active",
            "Completed"
        };

        public static List<string> FloorData { get; } = new List<string>
        {
            "Upper Ground", "Second Floor", "Third Floor", "Fourth Floor", "Fifth Floor",
            "Sixth Floor", "Seventh Floor", "Eighth Floor", "Ninth Floor", "Tenth Floor",
            "Eleventh Floor", "Twelfth Floor", "Thirteenth Floor", "Fourteenth Floor", "Fifteenth Floor"
        };

        public static List<string> OwnerTypeData { get; } = new List<string>
        {
            "RoomOwner", "Tenant", "Temporary"
        };

        public static List<string> IdTypeData { get; } = new List<string>
        {
            "Passport", "Driver's License", "National ID", "Voter's ID", "Social Security Card",
            "Birth Certificate", "Company ID", "Student ID", "PhilHealth ID", "Senior Citizen ID",
            "Postal ID", "PRC ID", "Barangay Clearance", "Alien Certificate of Registration",
            "OFW ID", "Police Clearance", "Firearms License", "GSIS ID", "SSS ID", "TIN ID",
            "UMID", "Health Insurance Card", "Refugee Card", "Other Government-Issued ID", "Others"
        };

        public static List<string> TowerData { get; } = new List<string>
        {
            "Alto",
            "Bossa"
        };

        // Update room generation to show simple numbers
        public static List<Room> RoomData { get; } = GenerateAllRooms();

        private static List<Room> GenerateAllRooms()
        {
            var rooms = new List<Room>();
            int roomIdCounter = 1;

            rooms.AddRange(GenerateTowerRooms("Bossa", 2, 15, ref roomIdCounter));
            rooms.AddRange(GenerateTowerRooms("Alto", 2, 10, ref roomIdCounter));

            return rooms;
        }

        private static List<Room> GenerateTowerRooms(string tower, int startFloor, int endFloor, ref int roomIdCounter)
        {
            var rooms = new List<Room>();

            for (int floor = startFloor; floor <= endFloor; floor++)
            {
                string floorName = GetFloorName(floor);
                for (int i = 1; i <= 5; i++)
                {
                    rooms.Add(new Room
                    {
                        RoomId = roomIdCounter++,
                        Tower = tower,
                        FloorLevel = floorName,
                        RoomNumber = $"{floor}0{i}" // Simple room number (1-5)
                    });
                }
            }

            return rooms;
        }

        private static string GetFloorName(int floor)
        {
            return floor switch
            {
                2 => "Second Floor",
                3 => "Third Floor",
                4 => "Fourth Floor",
                5 => "Fifth Floor",
                6 => "Sixth Floor",
                7 => "Seventh Floor",
                8 => "Eighth Floor",
                9 => "Ninth Floor",
                10 => "Tenth Floor",
                11 => "Eleventh Floor",
                12 => "Twelfth Floor",
                13 => "Thirteenth Floor",
                14 => "Fourteenth Floor",
                15 => "Fifteenth Floor",
                _ => $"{floor}th Floor"
            };
        }

        public static List<string> PositionData { get; } = new List<string>
        {
            "Receptionist",
            "Manager",
            "Security"
        };

        public static RoomOccupant DefaultRoomOccupant => new RoomOccupant
        {
            OccupantId = 1,
            MoveInDate = new DateOnly(2025, 1, 1),
            MoveOutDate = null,
            DateAssigned = new DateOnly(2025, 1, 1),
            OccupationStatus = true,
            OwnerUserId = DefaultRoomOwner.UserId,
            RoomId = 1
        };
        public static List<VisitLog> DefaultVisitLog => new List<VisitLog>
        {
            new VisitLog
            {
                VisitLogId = 1,
                IssueDate = DateOnly.FromDateTime(new DateTime(2025, 8, 19)),  // August 19, 2025
                AppointmentDate = null,
                VerificationStatus = null,
                VerifiedDateTime = null,
                PurposeOfVisit = "Guest",
                CheckInOutId = null,
                VisitorUserId = DefaultVisitor.UserId,
                OwnerUserId = DefaultRoomOccupant.OwnerUserId,
                RoomId = DefaultRoomOccupant.RoomId,
                CreatedBy = "Visitor",
                AuthorizedUserId = null,
                logStatus = false
            },

            new VisitLog
            {
                VisitLogId = 2,
                IssueDate = DateOnly.FromDateTime(new DateTime(2025, 8, 20)),  // August 20, 2025
                AppointmentDate = DateOnly.FromDateTime(new DateTime(2025,8,21)),
                VerificationStatus = true,
                VerifiedDateTime = new DateTime(2025, 8, 21, 12, 0, 0),
                PurposeOfVisit = "Guest",
                CheckInOutId = 1,
                VisitorUserId = DefaultVisitor.UserId,
                OwnerUserId = DefaultRoomOccupant.OwnerUserId,
                RoomId = DefaultRoomOccupant.RoomId,
                CreatedBy = "Visitor",
                AuthorizedUserId = null,
                logStatus = true
            },

            new VisitLog
            {
                VisitLogId = 3,
                IssueDate = DateOnly.FromDateTime(new DateTime(2025, 8, 21)),  // August 21, 2025
                AppointmentDate = null,
                VerificationStatus = false,
                VerifiedDateTime = null,
                PurposeOfVisit = "Guest",
                CheckInOutId = null,
                VisitorUserId = DefaultVisitor.UserId,
                OwnerUserId = DefaultRoomOccupant.OwnerUserId,
                RoomId = DefaultRoomOccupant.RoomId,
                CreatedBy = "Visitor",
                AuthorizedUserId = null,
                logStatus = true
            },
        };

        public static CheckInOut CheckInOutData => new CheckInOut
        {
            CheckInOutId = 1,
            CheckInDateTime = new DateTime(2025, 8, 21, 14, 0, 0), // August 21, 2025 at 2:00 PM
            CheckOutDateTime = new DateTime(2025, 8, 21, 20, 0, 0),
        };

        public static List<User> DefaultUsers { get; } = new List<User>
        {
            new User
            {
                UserId = $"VIS-{DateTime.Now.Year}-00001",
                FirstName = "Default",
                MiddleName = "-",
                LastName = "Visitor",
                ContactNumber = "09123456780"
            },
            new User
            {
                UserId = $"OWR-{DateTime.Now.Year}-00001",
                FirstName = "Default",
                MiddleName = "-",
                LastName = "Owner",
                ContactNumber = "09123456782"
            },
            new User
            {
                UserId = $"STF-{DateTime.Now.Year}-00001",
                FirstName = "Default",
                MiddleName = "-",
                LastName = "Staff",
                ContactNumber = "09123456784"
            },
            new User
            {
                UserId = $"ADM-{DateTime.Now.Year}-00001",
                FirstName = "Main",
                MiddleName = "-",
                LastName = "Administrator",
                ContactNumber = "09123456789",
            }
        };

        public static List<Account> DefaultAccounts { get; } = new List<Account>
        {
            new Account
            {
                AccountId = 1,
                Username = "roomowner",
                // hashed password roomowner123
                Password = "AQAAAAIAAYagAAAAEGworE4xLkk9afv0B3spP11ZoGiFKo6hjHOL2F+dN7Ih8o9ZDf9Hcri84YzErMqK1Q==",
                Email = "default.owner@gmail.com"
            },
            new Account
            {
                AccountId = 2,
                Username = "staff", 
                // hashed password staff123
                Password = "AQAAAAIAAYagAAAAECMgnaa0nlsEFNYKyY/0wLFVaxFPjhnW9KGGEw4JHnXnhQ5/qofNYvnXgJfJPcj7AQ==",
                Email = "default.staff@gmail.com"
            },
            new Account
            {
                AccountId = 3,
                Username = "admin", 
                 // hashed password admin123
                Password = "AQAAAAIAAYagAAAAEDav1nOozAS/YpYepAYxmfg8arUJmWPtdJOskBHiZYFoSyk4G3f+1TU0+oTuyivZIg==", // hashed admin123
                Email = "act.lusamdelictor@gmail.com"
            }
        };

        public static Visitor DefaultVisitor { get; } = new Visitor
        {
            UserId = $"VIS-{DateTime.Now.Year}-00001",
            ProfilePicture = "/images/default.png"
        };

        public static RoomOwner DefaultRoomOwner { get; } = new RoomOwner
        {
            UserId = $"OWR-{DateTime.Now.Year}-00001",
            RoomOwnerProfilePicture = "/images/default.png",
            AccountId = 1
        };

        public static Staff DefaultStaff { get; } = new Staff
        {
            UserId = $"STF-{DateTime.Now.Year}-00001",
            Position = "Receptionist",
            DateHired = new DateOnly(2025, 1, 1),
            AccountId = 2
        };

        public static Admin DefaultAdmin { get; } = new Admin
        {
            UserId = $"ADM-{DateTime.Now.Year}-00001",
            DateAssigned = new DateOnly(2025, 1, 1),
            AccountId = 3
        };
    }
}