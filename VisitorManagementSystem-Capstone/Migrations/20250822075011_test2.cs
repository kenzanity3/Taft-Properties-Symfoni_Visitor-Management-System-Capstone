using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace VisitorManagementSystem_Capstone.Migrations
{
    /// <inheritdoc />
    public partial class test2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    AccountId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.AccountId);
                });

            migrationBuilder.CreateTable(
                name: "CheckInOuts",
                columns: table => new
                {
                    CheckInOutId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CheckInDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CheckOutDateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CheckedOutBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CheckInOuts", x => x.CheckInOutId);
                });

            migrationBuilder.CreateTable(
                name: "ErrorViewModels",
                columns: table => new
                {
                    RequestId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "Facilities",
                columns: table => new
                {
                    FacilityId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OpenTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    ClosingTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    FacilityStatus = table.Column<bool>(type: "bit", nullable: false),
                    FloorLevel = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Facilities", x => x.FacilityId);
                });

            migrationBuilder.CreateTable(
                name: "Rooms",
                columns: table => new
                {
                    RoomId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoomNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FloorLevel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Tower = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rooms", x => x.RoomId);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MiddleName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContactNumber = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    AccountStatus = table.Column<bool>(type: "bit", nullable: false),
                    DateCreated = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "accountActionLogs",
                columns: table => new
                {
                    LogId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActionType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActionText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActionDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TargetUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_accountActionLogs", x => x.LogId);
                    table.ForeignKey(
                        name: "FK_accountActionLogs_Users_TargetUserId",
                        column: x => x.TargetUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_accountActionLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Admins",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DateAssigned = table.Column<DateOnly>(type: "date", nullable: false),
                    AccountId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Admins", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_Admins_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "AccountId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Admins_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FacilityLogs",
                columns: table => new
                {
                    FacilityLogId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IssueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    FacilityId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CheckInOutId = table.Column<int>(type: "int", nullable: false),
                    logStatus = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacilityLogs", x => x.FacilityLogId);
                    table.ForeignKey(
                        name: "FK_FacilityLogs_CheckInOuts_CheckInOutId",
                        column: x => x.CheckInOutId,
                        principalTable: "CheckInOuts",
                        principalColumn: "CheckInOutId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FacilityLogs_Facilities_FacilityId",
                        column: x => x.FacilityId,
                        principalTable: "Facilities",
                        principalColumn: "FacilityId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FacilityLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RoomOwners",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoomOwnerProfilePicture = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EmergencyContactNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmergencyContactName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AccountId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomOwners", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_RoomOwners_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "AccountId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoomOwners_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Staffs",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DateHired = table.Column<DateOnly>(type: "date", nullable: false),
                    Position = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AccountId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Staffs", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_Staffs_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "AccountId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Staffs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Visitors",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProfilePicture = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Visitors", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_Visitors_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RoomOccupants",
                columns: table => new
                {
                    OccupantId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MoveInDate = table.Column<DateOnly>(type: "date", nullable: false),
                    MoveOutDate = table.Column<DateOnly>(type: "date", nullable: true),
                    DateAssigned = table.Column<DateOnly>(type: "date", nullable: false),
                    OccupationStatus = table.Column<bool>(type: "bit", nullable: false),
                    OwnerType = table.Column<int>(type: "int", nullable: false),
                    OwnerUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoomId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomOccupants", x => x.OccupantId);
                    table.ForeignKey(
                        name: "FK_RoomOccupants_RoomOwners_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "RoomOwners",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoomOccupants_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "RoomId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VisitLogs",
                columns: table => new
                {
                    VisitLogId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IssueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    AppointmentDate = table.Column<DateOnly>(type: "date", nullable: true),
                    VerificationStatus = table.Column<bool>(type: "bit", nullable: true),
                    VerifiedDateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PurposeOfVisit = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CheckInOutId = table.Column<int>(type: "int", nullable: true),
                    VisitorUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    OwnerUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoomId = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AuthorizedUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    logStatus = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisitLogs", x => x.VisitLogId);
                    table.ForeignKey(
                        name: "FK_VisitLogs_CheckInOuts_CheckInOutId",
                        column: x => x.CheckInOutId,
                        principalTable: "CheckInOuts",
                        principalColumn: "CheckInOutId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VisitLogs_RoomOwners_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "RoomOwners",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VisitLogs_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "RoomId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VisitLogs_Users_AuthorizedUserId",
                        column: x => x.AuthorizedUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VisitLogs_Visitors_VisitorUserId",
                        column: x => x.VisitorUserId,
                        principalTable: "Visitors",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Accounts",
                columns: new[] { "AccountId", "Email", "Password", "Username" },
                values: new object[,]
                {
                    { 1, "default.owner@gmail.com", "AQAAAAIAAYagAAAAEGworE4xLkk9afv0B3spP11ZoGiFKo6hjHOL2F+dN7Ih8o9ZDf9Hcri84YzErMqK1Q==", "roomowner" },
                    { 2, "default.staff@gmail.com", "AQAAAAIAAYagAAAAECMgnaa0nlsEFNYKyY/0wLFVaxFPjhnW9KGGEw4JHnXnhQ5/qofNYvnXgJfJPcj7AQ==", "staff" },
                    { 3, "act.lusamdelictor@gmail.com", "AQAAAAIAAYagAAAAEDav1nOozAS/YpYepAYxmfg8arUJmWPtdJOskBHiZYFoSyk4G3f+1TU0+oTuyivZIg==", "admin" }
                });

            migrationBuilder.InsertData(
                table: "Facilities",
                columns: new[] { "FacilityId", "Capacity", "ClosingTime", "Description", "FacilityStatus", "FloorLevel", "Name", "OpenTime" },
                values: new object[,]
                {
                    { "Gym001", 5, new TimeOnly(20, 0, 0), "A fully equipped fitness center featuring cardio machines, free weights, and strength training areas.", true, "Second Floor", "Gymnastic", new TimeOnly(10, 0, 0) },
                    { "Pool001", 5, new TimeOnly(20, 0, 0), "An indoor heated swimming pool suitable for recreational and lap swimming, with locker and shower facilities.", true, "Second Floor", "Swimming Pool", new TimeOnly(10, 0, 0) },
                    { "Study001", 5, new TimeOnly(20, 0, 0), "A quiet, well-lit space with individual desks and power outlets, ideal for reading, studying, and group discussions.", true, "Second Floor", "Study Room", new TimeOnly(10, 0, 0) }
                });

            migrationBuilder.InsertData(
                table: "Rooms",
                columns: new[] { "RoomId", "FloorLevel", "RoomNumber", "Tower" },
                values: new object[,]
                {
                    { 1, "Second Floor", "201", "Bossa" },
                    { 2, "Second Floor", "202", "Bossa" },
                    { 3, "Second Floor", "203", "Bossa" },
                    { 4, "Second Floor", "204", "Bossa" },
                    { 5, "Second Floor", "205", "Bossa" },
                    { 6, "Third Floor", "301", "Bossa" },
                    { 7, "Third Floor", "302", "Bossa" },
                    { 8, "Third Floor", "303", "Bossa" },
                    { 9, "Third Floor", "304", "Bossa" },
                    { 10, "Third Floor", "305", "Bossa" },
                    { 11, "Fourth Floor", "401", "Bossa" },
                    { 12, "Fourth Floor", "402", "Bossa" },
                    { 13, "Fourth Floor", "403", "Bossa" },
                    { 14, "Fourth Floor", "404", "Bossa" },
                    { 15, "Fourth Floor", "405", "Bossa" },
                    { 16, "Fifth Floor", "501", "Bossa" },
                    { 17, "Fifth Floor", "502", "Bossa" },
                    { 18, "Fifth Floor", "503", "Bossa" },
                    { 19, "Fifth Floor", "504", "Bossa" },
                    { 20, "Fifth Floor", "505", "Bossa" },
                    { 21, "Sixth Floor", "601", "Bossa" },
                    { 22, "Sixth Floor", "602", "Bossa" },
                    { 23, "Sixth Floor", "603", "Bossa" },
                    { 24, "Sixth Floor", "604", "Bossa" },
                    { 25, "Sixth Floor", "605", "Bossa" },
                    { 26, "Seventh Floor", "701", "Bossa" },
                    { 27, "Seventh Floor", "702", "Bossa" },
                    { 28, "Seventh Floor", "703", "Bossa" },
                    { 29, "Seventh Floor", "704", "Bossa" },
                    { 30, "Seventh Floor", "705", "Bossa" },
                    { 31, "Eighth Floor", "801", "Bossa" },
                    { 32, "Eighth Floor", "802", "Bossa" },
                    { 33, "Eighth Floor", "803", "Bossa" },
                    { 34, "Eighth Floor", "804", "Bossa" },
                    { 35, "Eighth Floor", "805", "Bossa" },
                    { 36, "Ninth Floor", "901", "Bossa" },
                    { 37, "Ninth Floor", "902", "Bossa" },
                    { 38, "Ninth Floor", "903", "Bossa" },
                    { 39, "Ninth Floor", "904", "Bossa" },
                    { 40, "Ninth Floor", "905", "Bossa" },
                    { 41, "Tenth Floor", "1001", "Bossa" },
                    { 42, "Tenth Floor", "1002", "Bossa" },
                    { 43, "Tenth Floor", "1003", "Bossa" },
                    { 44, "Tenth Floor", "1004", "Bossa" },
                    { 45, "Tenth Floor", "1005", "Bossa" },
                    { 46, "Eleventh Floor", "1101", "Bossa" },
                    { 47, "Eleventh Floor", "1102", "Bossa" },
                    { 48, "Eleventh Floor", "1103", "Bossa" },
                    { 49, "Eleventh Floor", "1104", "Bossa" },
                    { 50, "Eleventh Floor", "1105", "Bossa" },
                    { 51, "Twelfth Floor", "1201", "Bossa" },
                    { 52, "Twelfth Floor", "1202", "Bossa" },
                    { 53, "Twelfth Floor", "1203", "Bossa" },
                    { 54, "Twelfth Floor", "1204", "Bossa" },
                    { 55, "Twelfth Floor", "1205", "Bossa" },
                    { 56, "Thirteenth Floor", "1301", "Bossa" },
                    { 57, "Thirteenth Floor", "1302", "Bossa" },
                    { 58, "Thirteenth Floor", "1303", "Bossa" },
                    { 59, "Thirteenth Floor", "1304", "Bossa" },
                    { 60, "Thirteenth Floor", "1305", "Bossa" },
                    { 61, "Fourteenth Floor", "1401", "Bossa" },
                    { 62, "Fourteenth Floor", "1402", "Bossa" },
                    { 63, "Fourteenth Floor", "1403", "Bossa" },
                    { 64, "Fourteenth Floor", "1404", "Bossa" },
                    { 65, "Fourteenth Floor", "1405", "Bossa" },
                    { 66, "Fifteenth Floor", "1501", "Bossa" },
                    { 67, "Fifteenth Floor", "1502", "Bossa" },
                    { 68, "Fifteenth Floor", "1503", "Bossa" },
                    { 69, "Fifteenth Floor", "1504", "Bossa" },
                    { 70, "Fifteenth Floor", "1505", "Bossa" },
                    { 71, "Second Floor", "201", "Alto" },
                    { 72, "Second Floor", "202", "Alto" },
                    { 73, "Second Floor", "203", "Alto" },
                    { 74, "Second Floor", "204", "Alto" },
                    { 75, "Second Floor", "205", "Alto" },
                    { 76, "Third Floor", "301", "Alto" },
                    { 77, "Third Floor", "302", "Alto" },
                    { 78, "Third Floor", "303", "Alto" },
                    { 79, "Third Floor", "304", "Alto" },
                    { 80, "Third Floor", "305", "Alto" },
                    { 81, "Fourth Floor", "401", "Alto" },
                    { 82, "Fourth Floor", "402", "Alto" },
                    { 83, "Fourth Floor", "403", "Alto" },
                    { 84, "Fourth Floor", "404", "Alto" },
                    { 85, "Fourth Floor", "405", "Alto" },
                    { 86, "Fifth Floor", "501", "Alto" },
                    { 87, "Fifth Floor", "502", "Alto" },
                    { 88, "Fifth Floor", "503", "Alto" },
                    { 89, "Fifth Floor", "504", "Alto" },
                    { 90, "Fifth Floor", "505", "Alto" },
                    { 91, "Sixth Floor", "601", "Alto" },
                    { 92, "Sixth Floor", "602", "Alto" },
                    { 93, "Sixth Floor", "603", "Alto" },
                    { 94, "Sixth Floor", "604", "Alto" },
                    { 95, "Sixth Floor", "605", "Alto" },
                    { 96, "Seventh Floor", "701", "Alto" },
                    { 97, "Seventh Floor", "702", "Alto" },
                    { 98, "Seventh Floor", "703", "Alto" },
                    { 99, "Seventh Floor", "704", "Alto" },
                    { 100, "Seventh Floor", "705", "Alto" },
                    { 101, "Eighth Floor", "801", "Alto" },
                    { 102, "Eighth Floor", "802", "Alto" },
                    { 103, "Eighth Floor", "803", "Alto" },
                    { 104, "Eighth Floor", "804", "Alto" },
                    { 105, "Eighth Floor", "805", "Alto" },
                    { 106, "Ninth Floor", "901", "Alto" },
                    { 107, "Ninth Floor", "902", "Alto" },
                    { 108, "Ninth Floor", "903", "Alto" },
                    { 109, "Ninth Floor", "904", "Alto" },
                    { 110, "Ninth Floor", "905", "Alto" },
                    { 111, "Tenth Floor", "1001", "Alto" },
                    { 112, "Tenth Floor", "1002", "Alto" },
                    { 113, "Tenth Floor", "1003", "Alto" },
                    { 114, "Tenth Floor", "1004", "Alto" },
                    { 115, "Tenth Floor", "1005", "Alto" }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "UserId", "AccountStatus", "ContactNumber", "DateCreated", "FirstName", "LastName", "MiddleName" },
                values: new object[,]
                {
                    { "ADM-2025-00001", true, "09123456789", new DateOnly(2025, 8, 22), "Main", "Administrator", "-" },
                    { "OWR-2025-00001", true, "09123456782", new DateOnly(2025, 8, 22), "Default", "Owner", "-" },
                    { "STF-2025-00001", true, "09123456784", new DateOnly(2025, 8, 22), "Default", "Staff", "-" },
                    { "VIS-2025-00001", true, "09123456780", new DateOnly(2025, 8, 22), "Default", "Visitor", "-" }
                });

            migrationBuilder.InsertData(
                table: "Admins",
                columns: new[] { "UserId", "AccountId", "DateAssigned" },
                values: new object[] { "ADM-2025-00001", 3, new DateOnly(2025, 1, 1) });

            migrationBuilder.InsertData(
                table: "RoomOwners",
                columns: new[] { "UserId", "AccountId", "EmergencyContactName", "EmergencyContactNumber", "RoomOwnerProfilePicture" },
                values: new object[] { "OWR-2025-00001", 1, null, null, "/images/default.png" });

            migrationBuilder.InsertData(
                table: "Staffs",
                columns: new[] { "UserId", "AccountId", "DateHired", "Position" },
                values: new object[] { "STF-2025-00001", 2, new DateOnly(2025, 1, 1), "Receptionist" });

            migrationBuilder.InsertData(
                table: "Visitors",
                columns: new[] { "UserId", "ProfilePicture" },
                values: new object[] { "VIS-2025-00001", "/images/default.png" });

            migrationBuilder.InsertData(
                table: "RoomOccupants",
                columns: new[] { "OccupantId", "DateAssigned", "MoveInDate", "MoveOutDate", "OccupationStatus", "OwnerType", "OwnerUserId", "RoomId" },
                values: new object[] { 1, new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 1), null, true, 0, "OWR-2025-00001", 1 });

            migrationBuilder.CreateIndex(
                name: "IX_accountActionLogs_TargetUserId",
                table: "accountActionLogs",
                column: "TargetUserId");

            migrationBuilder.CreateIndex(
                name: "IX_accountActionLogs_UserId",
                table: "accountActionLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Email",
                table: "Accounts",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Username",
                table: "Accounts",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Admins_AccountId",
                table: "Admins",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_FacilityLogs_CheckInOutId",
                table: "FacilityLogs",
                column: "CheckInOutId");

            migrationBuilder.CreateIndex(
                name: "IX_FacilityLogs_FacilityId",
                table: "FacilityLogs",
                column: "FacilityId");

            migrationBuilder.CreateIndex(
                name: "IX_FacilityLogs_UserId",
                table: "FacilityLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomOccupants_OwnerUserId",
                table: "RoomOccupants",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomOccupants_RoomId",
                table: "RoomOccupants",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomOwners_AccountId",
                table: "RoomOwners",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Staffs_AccountId",
                table: "Staffs",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_ContactNumber",
                table: "Users",
                column: "ContactNumber",
                unique: true,
                filter: "[ContactNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_VisitLogs_AuthorizedUserId",
                table: "VisitLogs",
                column: "AuthorizedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_VisitLogs_CheckInOutId",
                table: "VisitLogs",
                column: "CheckInOutId");

            migrationBuilder.CreateIndex(
                name: "IX_VisitLogs_OwnerUserId",
                table: "VisitLogs",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_VisitLogs_RoomId",
                table: "VisitLogs",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_VisitLogs_VisitorUserId",
                table: "VisitLogs",
                column: "VisitorUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "accountActionLogs");

            migrationBuilder.DropTable(
                name: "Admins");

            migrationBuilder.DropTable(
                name: "ErrorViewModels");

            migrationBuilder.DropTable(
                name: "FacilityLogs");

            migrationBuilder.DropTable(
                name: "RoomOccupants");

            migrationBuilder.DropTable(
                name: "Staffs");

            migrationBuilder.DropTable(
                name: "VisitLogs");

            migrationBuilder.DropTable(
                name: "Facilities");

            migrationBuilder.DropTable(
                name: "CheckInOuts");

            migrationBuilder.DropTable(
                name: "RoomOwners");

            migrationBuilder.DropTable(
                name: "Rooms");

            migrationBuilder.DropTable(
                name: "Visitors");

            migrationBuilder.DropTable(
                name: "Accounts");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
