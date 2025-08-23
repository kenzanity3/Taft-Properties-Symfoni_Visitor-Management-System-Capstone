using Microsoft.AspNet.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using VisitorManagementSystem_Capstone.Data;
using VisitorManagementSystem_Capstone.Models;
using VisitorManagementSystem_Capstone.Services;

namespace VisitorManagementSystem_Capstone.ViewModels
{
    public class POSTViewModel
    {
        /// <summary>
        /// ViewModel used for handling user registration, room/facility management,
        /// visit/facility logging, and account operations within the Visitor Management System.
        /// </summary>

        // Current User Modes (used to identify and manage logged-in role)
        private Admin? adminmode;
        private Staff? staffmode;
        private RoomOwner? ownermode;

        public UserDataBundle? userbundle { get; set; } = new();

        // Primary User Entities
        public User? User { get; set; }
        public Visitor? Visitor { get; set; }                      // For single visitor binding (e.g., create/edit forms)
        public List<Visitor>? Visitors { get; set; } = new();      // For listing multiple visitors
        public Staff? Staff { get; set; }
        public Admin? Admin { get; set; }

        public Account? Account { get; set; }

        //Profile Picture Handling
        public IFormFile? ProfilePictureFile { get; set; }


        // Room & Facility Structure
        public Room? Room { get; set; }
        public RoomOwner? RoomOwner { get; set; }
        public Facility? Facility { get; set; }


        // Logging & Occupancy
        public VisitLog? VisitLog { get; set; }
        public FacilityLog? FacilityLog { get; set; }
        public RoomOccupant? Occupant { get; set; }


        /// <summary>
        /// Generates a full name string from first, middle, and last names.
        /// If either first or last name is missing or whitespace, returns null.
        /// </summary>
        public static Func<string, string, string?, string?> fullname = (firstName, lastName, middleName) =>
        {
            // Return null if either first or last name is missing
            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
                return null;

            // Return full name with middle name if provided
            if (!string.IsNullOrWhiteSpace(middleName))
                return $"{firstName} {middleName} {lastName}";

            // Return full name without middle name
            return $"{firstName} {lastName}";
        };

		/// <summary>
		/// Infers the action type based on keywords found in the list of change descriptions.
		/// </summary>
		/// <param name="changes">List of change descriptions (e.g., "user clocked in", "user verified")</param>
		/// <returns>String representing the action type</returns>
		private string InferActionType(List<string> changes)
		{
			if (changes.Any(c => c.Contains("clocked in"))) return "ClockIn";
			if (changes.Any(c => c.Contains("clocked out"))) return "ClockOut";
			if (changes.Any(c => c.Contains("verified"))) return "Verified";
			if (changes.Any(c => c.Contains("denied"))) return "Denied";
			if (changes.Any(c => c.Contains("requested"))) return "Request";
			if (changes.Any(c => c.Contains("added"))) return "Added";
			if (changes.Any(c => c.Contains("updated"))) return "Updated";
			if (changes.Any(c => c.Contains("deleted"))) return "Deleted";
			if (changes.Any(c => c.Contains("created"))) return "Registered";

			return "Updated"; // Default
		}

		/// <summary>
		/// Default constructor for general use.
		/// </summary>
		public POSTViewModel() { }

        /// <summary>
        /// Constructor for admin mode operations.
        /// Initializes the ViewModel with an Admin and associated User.
        /// </summary>
        public POSTViewModel(Admin admin)
        {
            adminmode = admin;
        }

        /// <summary>
        /// Constructor for staff mode operations.
        /// Initializes the ViewModel with a Staff and associated User.
        /// </summary>
        public POSTViewModel(Staff staff)
        {
            staffmode = staff;
        }

        /// <summary>
        /// Constructor for room owner mode operations.
        /// Initializes the ViewModel with a RoomOwner and associated User.
        /// </summary>
        public POSTViewModel(RoomOwner roomOwner)
        {
            ownermode = roomOwner;
        }

        /// <summary>
        /// Creates a log entry for user or admin actions.
        /// Used for auditing purposes.
        /// </summary>
        private AccountActionLog CreateActionLog(string text, string type)
        {
            return new AccountActionLog
            {
                ActionDateTime = DateTime.UtcNow,
                ActionText = adminmode == null
                    ? staffmode == null
                        ? $"User: {User?.UserId} {text}" 
                        : $"Staff: {staffmode.UserId} {text} to User: {User?.UserId}"
                    : $"Admin: {adminmode.UserId} {text} to User: {User?.UserId}",
                ActionType = type,
                UserId = adminmode != null ? adminmode.UserId : staffmode != null ? staffmode.UserId : User?.UserId,
                TargetUserId = adminmode != null ? staffmode != null ? User?.UserId : null: null
            };
        }

        /// <summary>
        /// Adds new reference or structure data to the database.
        /// Prevents duplicates and logs successful additions through AccountActionLog.
        /// Supports Room, Facility, Floor, RoomType, Tower, IdType, Position, OwnerType, VisitorType, Citizenship, City, and Country.
        /// Does not include logging or occupancy-related classes.
        /// </summary>
        public async Task<List<string>> AddAsync(VisitorManagementSystemDatabaseContext ctx)
        {
            var errors = new List<string>();
            var changes = new List<string>();
            
            if (Room != null)
            {
                if (await ctx.Rooms.FindAsync(Room.RoomNumber) != null)
                    errors.Add($"Room {Room.RoomNumber} already exists.");
                else
                {
                    ctx.Rooms.Add(Room);
                    changes.Add($"Room {Room.RoomNumber}");
                }
            }

            if (Facility != null)
            {
                if (await ctx.Facilities.AnyAsync(f => f.Name == Facility.Name))
                    errors.Add($"Facility {Facility.Name} already exists.");
                else
                {
                    ctx.Facilities.Add(Facility);
                    changes.Add($"Facility {Facility.Name}");
                }
            }


            if (changes.Any())
                ctx.accountActionLogs.Add(CreateActionLog($"added {string.Join(", ", changes)}", InferActionType(changes)));

            await ctx.SaveChangesAsync();
            return errors;
        }


        /// <summary>
        /// Creates a visit or facility log entry and records the action for auditing.
        /// If the visit request is not approved within 12 hours, it will automatically be denied.
        /// For Visit Logs, VerificationStatus will be set to false on auto-denial.
        /// </summary>
        public async Task<List<string>> CreateLogAsync(VisitorManagementSystemDatabaseContext ctx, string userid, int mode)
        {
            var errors = new List<string>();
            var changes = new List<string>();

            // Validate that the user exists
            var user = (await GetDataListAsync(ctx, 1))
                .OfType<User>()
                .FirstOrDefault(u => u.UserId == userid);

            if (user == null)
            {
                errors.Add("User not found.");
                return errors;
            }

            string cacheKey = string.Empty;

            // --- MODE 1: Visit Log ---
            if (mode == 1)
            {
                if (VisitLog == null)
                {
                    errors.Add("Visit Log data is null.");
                    return errors;
                }

                var visitorExists = await ctx.Visitors.AnyAsync(v => v.UserId == VisitLog.VisitorUserId);
                if (!visitorExists)
                {
                    errors.Add($"Visitor ID {VisitLog.VisitorUserId} not found.");
                    return errors;
                }

                var roomExists = await ctx.Rooms.AnyAsync(r => r.RoomNumber == VisitLog.Room!.RoomNumber);
                if (!roomExists)
                {
                    errors.Add($"Room {VisitLog.Room!.RoomNumber} not found.");
                    return errors;
                }

                var visitorRequestCheck = await ctx.VisitLogs.AnyAsync(u =>
                    u.IssueDate == DateOnly.FromDateTime(DateTime.UtcNow) &&
                    u.logStatus == true &&
                    u.RoomId == VisitLog.RoomId);

                if (!visitorRequestCheck)
                {
                    ctx.VisitLogs.Add(VisitLog);
                    changes.Add($"requested visit log for Room {VisitLog.Room!.RoomNumber}");
                }
                else
                {
                    errors.Add("The request for this room number is already active.");
                    return errors;
                }
            }
            // --- MODE 2: Facility Log ---
            else if (mode == 2)
            {
                if (FacilityLog == null)
                {
                    errors.Add("Facility Log data is null.");
                    return errors;
                }

                var userExists = await ctx.Users.AnyAsync(v => v.UserId == FacilityLog.UserId);
                if (!userExists)
                {
                    errors.Add($"User ID {FacilityLog.UserId} not found.");
                    return errors;
                }

                var facilityExists = await ctx.Facilities.AnyAsync(f => f.FacilityId == FacilityLog.FacilityId);
                if (!facilityExists)
                {
                    errors.Add($"Facility ID {FacilityLog.FacilityId} not found.");
                    return errors;
                }

                var facilityRequestCheck = await ctx.FacilityLogs.AnyAsync(u =>
                    u.IssueDate == DateOnly.FromDateTime(DateTime.UtcNow) &&
                    u.logStatus == true &&
                    u.FacilityId != FacilityLog.FacilityId);

                if (!facilityRequestCheck)
                {
                    ctx.FacilityLogs.Add(FacilityLog);
                    changes.Add($"requested facility log for Facility {FacilityLog.Facility?.Name}");
                }
                else
                {
                    errors.Add($"Your request for this facility is already active. Facility ID: {FacilityLog.FacilityId}");
                    return errors;
                }
            }
            else
            {
                errors.Add("The Mode is invalid.");
            }

            // Save initial creation
            if (changes.Any())
            {
                string actionText = string.Join(", ", changes);
                string actionType = InferActionType(changes);
                ctx.accountActionLogs.Add(CreateActionLog(actionText, actionType));
                await ctx.SaveChangesAsync();

                if (mode == 1)
                {
                    if (VisitLog.AppointmentDate != null)
                    {
                        cacheKey = $"{VisitLog.VisitLogId}";
                        IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());

                        // --- Auto-deny after 12 hours if not approved ---
                        var cacheEntryOptions = new MemoryCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12)
                        };

                        cacheEntryOptions.RegisterPostEvictionCallback(async (key, value, reason, state) =>
                        {
                            if (reason == EvictionReason.Expired && key is string expiredKey)
                            {
                                // Load config from appsettings.json
                                var config = new ConfigurationBuilder()
                                    .SetBasePath(Directory.GetCurrentDirectory())
                                    .AddJsonFile("appsettings.json")
                                    .Build();

                                var connectionString = config.GetConnectionString("DefaultConnection");

                                var optionsBuilder = new DbContextOptionsBuilder<VisitorManagementSystemDatabaseContext>();
                                optionsBuilder.UseSqlServer(connectionString);

                                // Create the context
                                using var context = new VisitorManagementSystemDatabaseContext(optionsBuilder.Options);
                                var newCtx = context;

                                var id = int.Parse(expiredKey);
                                var log = await newCtx.VisitLogs.FirstOrDefaultAsync(v => v.VisitLogId == id && v.logStatus == true);

                                // Only deny if VerificationStatus isn't already true
                                if (log != null && log.VerificationStatus != true)
                                {
                                    log.VerificationStatus = false; // Auto-deny
                                    log.logStatus = false;
                                    await newCtx.SaveChangesAsync();
                                }

                            }
                        });

                        _cache.Set(cacheKey, true, cacheEntryOptions);
                    }
                }
                if (mode == 2)
                {
                    OtpService.GenerateFacilityOtp(Convert.ToString(FacilityLog!.FacilityLogId), FacilityLog!.UserId);
                }
            }
            return errors;
        }
        /// <summary>
        /// Edits either a Visit Log or a Facility Log in the system based on the given mode.
        /// Tracks changes for auditing purposes using the changes list, which is then logged to AccountActionLogs.
        /// NOTE: For Visit Log edits, ensure you use OwnerMode or AdminMode. 
        /// For Facility Log edits, use AdminMode only.
        /// </summary>
        /// <param name="ctx">Database context for the Visitor Management System.</param>
        /// <param name="logid">The ID of the log to be edited.</param>
        /// <param name="mode">
        /// 1 = Visit Log
        /// 2 = Facility Log
        /// </param>
        /// <returns>List of validation errors encountered during the process. If empty, the operation was successful.</returns>
        public async Task<List<string>> EditLogAsync(VisitorManagementSystemDatabaseContext ctx, int logid, int mode)
        {
            var errors = new List<string>();    // Holds validation errors
            var changes = new List<string>();   // Tracks changes for auditing

            // ------------------------------
            // MODE 1: Visit Log Editing
            // ------------------------------
            if (mode == 1)
            {
                var existingvisitlog = (await GetDataListAsync(ctx, 8))
                    .OfType<VisitLog>()
                    .FirstOrDefault(v => v.VisitLogId == logid);

                if (existingvisitlog == null)
                {
                    errors.Add($"Visit Log with ID {logid} not found.");
                    return errors;
                }

                if (VisitLog == null)
                {
                    errors.Add("Visit Log data is null.");
                    return errors;
                }

                // Validate and update Appointment Date
                if (existingvisitlog.AppointmentDate != null && existingvisitlog.AppointmentDate != VisitLog.AppointmentDate)
                {
                    if (existingvisitlog.IssueDate < VisitLog.AppointmentDate)
                    {
                        errors.Add("Appointment date must be after the request date.");
                        return errors;
                    }
                    if (VisitLog.AppointmentDate < DateOnly.FromDateTime(DateTime.UtcNow) && adminmode == null)
                    {
                        existingvisitlog.AppointmentDate = VisitLog.AppointmentDate;
                        changes.Add($"updated appointment date for Visit Log ID {existingvisitlog.VisitLogId} to {VisitLog.AppointmentDate}");
                    }
                    else if (adminmode != null)
                    {
                        existingvisitlog.AppointmentDate = VisitLog.AppointmentDate;
                        changes.Add($"updated appointment date for Visit Log ID {existingvisitlog.VisitLogId} to {VisitLog.AppointmentDate}");
                    }
                    else
                    {
                        errors.Add("Appointment date must be in the future for visitors.");
                        return errors;
                    }
                }

                //RoomOwner-only field updates
                if (ownermode != null)
                {
                    // Verification Status
                    if (existingvisitlog.VerificationStatus != VisitLog.VerificationStatus)
                    {
                        existingvisitlog.VerificationStatus = VisitLog.VerificationStatus;
                        if (existingvisitlog.VerificationStatus == true)
                            existingvisitlog.VerifiedDateTime = DateTime.UtcNow;
                        changes.Add(existingvisitlog.VerificationStatus == true
                            ? $"verified Visit Log ID {existingvisitlog.VisitLogId}"
                            : $"denied Visit Log ID {existingvisitlog.VisitLogId}");
                    }
                }

                // Admin-only field updates
                if (adminmode != null)
                {
                    // Visitor ID
                    if (existingvisitlog.VisitorUserId != VisitLog.VisitorUserId)
                    {
                        existingvisitlog.VisitorUserId = VisitLog.VisitorUserId;
                        var visitor = await ctx.Visitors.FirstOrDefaultAsync(v => v.UserId == existingvisitlog.VisitorUserId);
                        if (visitor != null)
                            changes.Add($"updated visitor for Visit Log ID {existingvisitlog.VisitLogId} to User ID {visitor.UserId}");
                    }

                    // Owner ID
                    if (existingvisitlog.OwnerUserId != VisitLog.OwnerUserId)
                    {
                        existingvisitlog.OwnerUserId = VisitLog.OwnerUserId;
                        var owner = await ctx.RoomOwners.FirstOrDefaultAsync(o => o.UserId == existingvisitlog.OwnerUserId);
                        if (owner != null)
                            changes.Add($"updated owner for Visit Log ID {existingvisitlog.VisitLogId} to User ID {owner.UserId}");
                    }

                    // Room Number
                    if (existingvisitlog.Room!.RoomNumber != VisitLog.Room!.RoomNumber)
                    {
                        existingvisitlog.Room!.RoomNumber = VisitLog.Room!.RoomNumber;
                        changes.Add($"updated room number for Visit Log ID {existingvisitlog.VisitLogId} to {existingvisitlog.Room!.RoomNumber}");
                    }

                    // Issue Date
                    if (existingvisitlog.IssueDate != VisitLog.IssueDate)
                    {
                        existingvisitlog.IssueDate = VisitLog.IssueDate;
                        changes.Add($"updated request date for Visit Log ID {existingvisitlog.VisitLogId} to {VisitLog.IssueDate}");
                    }

                    // Purpose of Visit
                    if (existingvisitlog.PurposeOfVisit != VisitLog.PurposeOfVisit)
                    {
                        existingvisitlog.PurposeOfVisit = VisitLog.PurposeOfVisit;
                        changes.Add($"updated purpose of visit for Visit Log ID {existingvisitlog.VisitLogId} to '{existingvisitlog.PurposeOfVisit}'");
                    }

                    // Verification Status
                    if (existingvisitlog.VerificationStatus != VisitLog.VerificationStatus)
                    {
                        existingvisitlog.VerificationStatus = VisitLog.VerificationStatus;
                        if (existingvisitlog.VerificationStatus == true)
                            existingvisitlog.VerifiedDateTime = DateTime.UtcNow;
                        changes.Add(existingvisitlog.VerificationStatus == true
                            ? $"verified Visit Log ID {existingvisitlog.VisitLogId}"
                            : $"denied Visit Log ID {existingvisitlog.VisitLogId}");
                    }

                    // Check-In/Out Update
                    if (existingvisitlog.CheckInOut != null && VisitLog.CheckInOut != null)
                    {
                        if (existingvisitlog.CheckInOut.CheckInDateTime != VisitLog.CheckInOut.CheckInDateTime)
                        {
                            existingvisitlog.CheckInOut.CheckInDateTime = VisitLog.CheckInOut.CheckInDateTime;
                            changes.Add($"clocked in for Visit Log ID {existingvisitlog.VisitLogId} at {existingvisitlog.CheckInOut.CheckInDateTime}");
                        }

                        if (existingvisitlog.CheckInOut.CheckOutDateTime != VisitLog.CheckInOut.CheckOutDateTime)
                        {
                            existingvisitlog.CheckInOut.CheckOutDateTime = VisitLog.CheckInOut.CheckOutDateTime;
                            changes.Add($"clocked out for Visit Log ID {existingvisitlog.VisitLogId} at {existingvisitlog.CheckInOut.CheckOutDateTime}");
                        }

                        ctx.CheckInOuts.UpdateRange(existingvisitlog.CheckInOut);
                    }
                }
            }

            // ------------------------------
            // MODE 2: Facility Log Editing
            // ------------------------------
            else if (mode == 2)
            {
                var existingfacilitylog = (await GetDataListAsync(ctx, 9))
                    .OfType<FacilityLog>()
                    .FirstOrDefault(f => f.FacilityLogId == logid);

                if (existingfacilitylog == null)
                {
                    errors.Add($"Facility Log with ID {logid} not found.");
                    return errors;
                }
                if (FacilityLog == null)
                {
                    errors.Add("Facility Log data is null.");
                    return errors;
                }

                if (adminmode != null)
                {
                    // Facility ID
                    if (existingfacilitylog.FacilityId != FacilityLog.FacilityId)
                    {
                        existingfacilitylog.FacilityId = FacilityLog.FacilityId;
                        var facility = await ctx.Facilities.FirstOrDefaultAsync(f => f.FacilityId == existingfacilitylog.FacilityId);
                        if (facility != null)
                            changes.Add($"updated facility for Facility Log ID {existingfacilitylog.FacilityLogId} to Facility ID {facility.FacilityId}");
                    }

                    // User ID
                    if (existingfacilitylog.UserId != FacilityLog.UserId)
                    {
                        existingfacilitylog.UserId = FacilityLog.UserId;
                        var visitor = await ctx.Visitors.FirstOrDefaultAsync(v => v.UserId == existingfacilitylog.UserId);
                        if (visitor != null)
                            changes.Add($"updated user for Facility Log ID {existingfacilitylog.FacilityLogId} to User ID {visitor.UserId}");
                    }

                    // Issue Date
                    if (existingfacilitylog.IssueDate != FacilityLog.IssueDate)
                    {
                        existingfacilitylog.IssueDate = FacilityLog.IssueDate;
                        changes.Add($"updated request date for Facility Log ID {existingfacilitylog.FacilityLogId} to {existingfacilitylog.IssueDate}");
                    }

                    // Check-In/Out Update
                    if (existingfacilitylog.CheckInOut != null && FacilityLog.CheckInOut != null)
                    {
                        if (existingfacilitylog.CheckInOut.CheckInDateTime != FacilityLog.CheckInOut.CheckInDateTime)
                        {
                            existingfacilitylog.CheckInOut.CheckInDateTime = FacilityLog.CheckInOut.CheckInDateTime;
                            changes.Add($"clocked in for Facility Log ID {existingfacilitylog.FacilityLogId} at {existingfacilitylog.CheckInOut.CheckInDateTime}");
                        }

                        if (existingfacilitylog.CheckInOut.CheckOutDateTime != FacilityLog.CheckInOut.CheckOutDateTime)
                        {
                            existingfacilitylog.CheckInOut.CheckOutDateTime = FacilityLog.CheckInOut.CheckOutDateTime;
                            changes.Add($"clocked out for Facility Log ID {existingfacilitylog.FacilityLogId} at {existingfacilitylog.CheckInOut.CheckOutDateTime}");
                        }

                        ctx.CheckInOuts.UpdateRange(existingfacilitylog.CheckInOut);
                    }

                    ctx.FacilityLogs.UpdateRange(existingfacilitylog);
                }
            }
            else
            {
                errors.Add("The Mode is Invalid.");
            }

            // ------------------------------
            // Log Changes for Auditing
            // ------------------------------
            if (changes.Any())
            {
                string actionText = string.Join(", ", changes);
                string actionType = InferActionType(changes);
                ctx.accountActionLogs.Add(CreateActionLog(actionText, actionType));
                await ctx.SaveChangesAsync();
            }

            return errors;
        }
        /// <summary>
        /// Deletes a Visit or Facility log by marking its logStatus as false, with audit tracking.
        /// </summary>
        public async Task<List<string>> DeleteLogAsync(VisitorManagementSystemDatabaseContext ctx, int logid, int mode, int deletemode)
        {
            var errors = new List<string>();
            var changes = new List<string>();

            // Mode 1: Visit Log
            if (mode == 1)
            {
                var visitlogexists = await ctx.VisitLogs.FirstOrDefaultAsync(u => u.VisitLogId == logid);
                if (visitlogexists == null)
                {
                    errors.Add($"Visit Log with ID {logid} does not exist.");
                    return errors;
                }

                visitlogexists.logStatus = false;
                ctx.VisitLogs.Update(visitlogexists);

                // Audit change
                changes.Add($"deleted Visit Log {visitlogexists.VisitLogId} for Room {visitlogexists.Room?.RoomNumber}");
            }
            // Mode 2: Facility Log
            else if (mode == 2)
            {
                var facilitylogexists = await ctx.FacilityLogs.FirstOrDefaultAsync(u => u.FacilityLogId == logid);
                if (facilitylogexists == null)
                {
                    errors.Add($"Facility Log with ID {logid} does not exist.");
                    return errors;
                }

                facilitylogexists.logStatus = false;
                ctx.FacilityLogs.Update(facilitylogexists);

                // Audit change
                changes.Add($"deleted Facility Log {facilitylogexists.FacilityLogId} for Facility {facilitylogexists.Facility?.Name}");
            }
            else
            {
                errors.Add("The Mode is Invalid.");
                return errors;
            }

            // Save to account action logs if changes were made
            if (changes.Any())
            {
                string actionText = string.Join(", ", changes);
                string actionType = InferActionType(changes);
                ctx.accountActionLogs.Add(CreateActionLog(actionText, actionType));
                await ctx.SaveChangesAsync();
            }

            return errors;
        }
        /// <summary>
        /// Handles visitor or facility check-in and check-out with audit logging.
        /// </summary>
        public async Task<List<string>> CheckInOutAsync(VisitorManagementSystemDatabaseContext ctx, string contactnumber, int mode)
        {
            var errors = new List<string>();
            var changes = new List<string>();

            // Validate user by contact number
            var user = (await GetDataListAsync(ctx, 1))
                .OfType<User>()
                .FirstOrDefault(u => u.ContactNumber == contactnumber);

            if (user == null)
            {
                errors.Add("User not found.");
                return errors;
            }

            // Mode 1: Visit Log Check-in/out
            if (mode == 1)
            {
                var visit = (await GetDataListAsync(ctx, 8))
                    .OfType<VisitLog>()
                    .FirstOrDefault(u => u.VisitorUserId == user.UserId && (u.CheckInOutId == null || u.CheckInOut.CheckOutDateTime == null));

                if (visit == null)
                {
                    errors.Add("No active Visit Request found for check-in/out.");
                }
                else if (visit.CheckInOut == null)
                {
                    // New check-in
                    visit.CheckInOut = new CheckInOut { CheckInDateTime = DateTime.UtcNow };
                    ctx.CheckInOuts.Add(visit.CheckInOut);
                    changes.Add($"clocked in to Room {visit.Room?.RoomNumber}");
                }
                else
                {
                    // Check-out
                    visit.CheckInOut.CheckOutDateTime = DateTime.UtcNow;
                    ctx.CheckInOuts.Update(visit.CheckInOut);
                    changes.Add($"clocked out from Room {visit.Room?.RoomNumber}");
                }
            }
            // Mode 2: Facility Log Check-in/out
            else if (mode == 2)
            {
                var facility = (await GetDataListAsync(ctx, 9))
                    .OfType<FacilityLog>()
                    .FirstOrDefault(f => f.UserId == user.UserId && (f.CheckInOutId == null || f.CheckInOut.CheckOutDateTime == null));

                if (facility == null)
                {
                    errors.Add("No active Facility Request found for check-in/out.");
                }
                else if (facility.CheckInOut == null)
                {
                    // New check-in
                    facility.CheckInOut = new CheckInOut { CheckInDateTime = DateTime.UtcNow };
                    ctx.CheckInOuts.Add(facility.CheckInOut);
                    changes.Add($"clocked in to Facility {facility.Facility?.Name}");
                }
                else
                {
                    // Check-out
                    facility.CheckInOut.CheckOutDateTime = DateTime.UtcNow;
                    ctx.CheckInOuts.Update(facility.CheckInOut);
                    changes.Add($"clocked out from Facility {facility.Facility?.Name}");
                }
            }
            else
            {
                errors.Add("The Mode is Invalid.");
                return errors;
            }

            // Save audit log
            if (changes.Any())
            {
                string actionText = string.Join(", ", changes);
                string actionType = InferActionType(changes);
                ctx.accountActionLogs.Add(CreateActionLog(actionText, actionType));
                await ctx.SaveChangesAsync();
            }

            return errors;
        }
       

        /// <summary>
        /// Performs check-in or check-out for visitors or facility usage and logs the action for auditing.
        /// </summary>
        /// <param name="ctx">Database context for accessing logs, users, and audit entries.</param>
        /// <param name="contactnumber">Contact number of the user or visitor.</param>
        /// <param name="mode">
        /// Operation mode:
        /// 1 – Visitor Visit Log check-in/out
        /// 2 – Facility Log check-in/out (requires <paramref name="facilitylogotp"/>)
        /// </param>
        /// <param name="facilitylogotp">OTP required for facility check-in/out (mode 2). Ignored for mode 1.</param>
        /// <returns>List of error messages; empty if operation succeeded.</returns>
        /// <remarks>
        /// Visitor logs: Creates CheckInOut if not present, otherwise sets CheckOutDateTime.  
        /// Facility logs: Validates OTP, retrieves facility log, then creates or updates CheckInOut.  
        /// All successful actions are recorded in the audit log.
        /// </remarks>

        public async Task<List<string>> CheckInOutAsync(VisitorManagementSystemDatabaseContext ctx, string contactnumber, int mode, string? facilitylogotp)
        {
            var errors = new List<string>();
            var changes = new List<string>();

            // Mode 1: Visit Log Check-in/out
            if (mode == 1)
            {
                var visit = (await GetDataListAsync(ctx, 8))
                    .OfType<VisitLog>()
                    .FirstOrDefault(u => u.Visitor.User.ContactNumber == contactnumber &&
                                         (u.CheckInOutId == null || u.CheckInOut.CheckOutDateTime == null));

                if (visit == null)
                {
                    errors.Add("No active Visit Request found for check-in/out.");
                    return errors;
                }

                if (visit.CheckInOut == null)
                {
                    // New check-in
                    visit.CheckInOut = new CheckInOut { CheckInDateTime = DateTime.UtcNow };
                    ctx.CheckInOuts.Add(visit.CheckInOut);
                    changes.Add($"clocked in to Room {visit.Room?.RoomNumber}");
                }
                else
                {
                    // Check-out
                    visit.CheckInOut.CheckOutDateTime = DateTime.UtcNow;
                    ctx.CheckInOuts.Update(visit.CheckInOut);
                    changes.Add($"clocked out from Room {visit.Room?.RoomNumber}");
                }
            }
            // Mode 2: Facility Log Check-in/out
            else if (mode == 2)
            {
                var user = await ctx.Users.FirstOrDefaultAsync(u => u.ContactNumber == contactnumber);
                if (user == null)
                {
                    errors.Add("User not found.");
                    return errors;
                }

                if (string.IsNullOrEmpty(facilitylogotp))
                {
                    errors.Add("Facility Log OTP is required for check-in/out.");
                    return errors;
                }

                if (!OtpService.ValidateFacilityOtp(facilitylogotp, user.UserId))
                {
                    errors.Add("Invalid Facility Log OTP.");
                    return errors;
                }

                var otpEntry = OtpService.GetOtpEntry(facilitylogotp);
                if (otpEntry == null)
                {
                    errors.Add("OTP entry not found.");
                    return errors;
                }

                var facility = (await GetDataListAsync(ctx, 9))
                    .OfType<FacilityLog>()
                    .FirstOrDefault(f => Convert.ToString(f.FacilityLogId) == otpEntry.Id &&
                                         (f.CheckInOutId == null || f.CheckInOut!.CheckOutDateTime == null));

                if (facility == null)
                {
                    errors.Add("No active Facility Request found for check-in/out.");
                    return errors;
                }

                if (facility.CheckInOut == null)
                {
                    // New check-in
                    facility.CheckInOut = new CheckInOut { CheckInDateTime = DateTime.UtcNow };
                    ctx.CheckInOuts.Add(facility.CheckInOut);
                    changes.Add($"clocked in to Facility {facility.Facility?.Name}");
                }
                else
                {
                    // Check-out
                    facility.CheckInOut.CheckOutDateTime = DateTime.UtcNow;
                    ctx.CheckInOuts.Update(facility.CheckInOut);
                    changes.Add($"clocked out from Facility {facility.Facility?.Name}");
                    await OtpService.ForceRemoveOTP(facilitylogotp);
                }
            }
            else
            {
                errors.Add("The Mode is Invalid.");
                return errors;
            }

            // Save audit log
            if (changes.Any())
            {
                string actionText = string.Join(", ", changes);
                string actionType = InferActionType(changes);
                ctx.accountActionLogs.Add(CreateActionLog(actionText, actionType));
                await ctx.SaveChangesAsync();
            }

            return errors;
        }

        /// <summary>
        /// Handles the registration of a new user based on the selected role.
        /// This method receives pre-populated data for User, Account, and role-specific information (Visitor, Staff, Admin, RoomOwner).
        /// It validates duplicate emails and contact numbers, generates a formatted UserId, and saves all associated records to the database.
        /// If the user is a Room Owner, it also links an occupant if provided.
        /// Returns a list of error messages if validation fails.
        /// </summary>
        /// <param name="context">The database context used to perform all entity operations.</param>
        /// <param name="mode">The selected role type: 1 - Visitor, 2 - Staff, 3 - Admin, 4 - Room Owner.</param>
        /// <returns>A list of validation error messages. If empty, the registration is successful.</returns>

        public async Task<List<string>> RegisterUserAsync(VisitorManagementSystemDatabaseContext ctx, int mode)
        {
            var errors = new List<string>();

            // Validate required fields
            if (userbundle?.User == null)
            {
                errors.Add("User information is required");
                return errors;
            }

            // Validate contact number
            if (string.IsNullOrWhiteSpace(userbundle.User.ContactNumber))
            {
                errors.Add("Contact number is required");
            }
            else if (ctx.Users.Any(u => u.ContactNumber == userbundle.User.ContactNumber))
            {
                errors.Add("Contact number is already registered");
            }

            // For Room Owners
            if (mode == 2 && userbundle.RoomOwner != null)
            {
                if (string.IsNullOrWhiteSpace(userbundle.RoomOwner.EmergencyContactNumber))
                {
                    errors.Add("Emergency contact number is required");
                }
                else if (userbundle.RoomOwner.EmergencyContactNumber == userbundle.User.ContactNumber)
                {
                    errors.Add("Emergency contact cannot be the same as primary contact");
                }
            }

            if (errors.Any()) return errors;

            try
            {
                // Generate UserId based on role
                userbundle.User.UserId = mode switch
                {
                    1 => await GenerateNextIdAsync("VIS", ctx.Visitors.Select(v => v.UserId)),
                    2 => await GenerateNextIdAsync("RMW", ctx.RoomOwners.Select(r => r.UserId)),
                    3 => await GenerateNextIdAsync("STF", ctx.Staffs.Select(s => s.UserId)),
                    4 => await GenerateNextIdAsync("ADM", ctx.Admins.Select(a => a.UserId)),
                    _ => throw new ArgumentException("Invalid user mode")
                };


                // Set default profile picture for Visitor
                if (mode == 1 && userbundle.Visitor != null)
                {
                    if (string.IsNullOrWhiteSpace(userbundle.Visitor.ProfilePicture))
                    {
                        userbundle.Visitor.ProfilePicture = "/images/default.png";
                    }
                }
                // Set default profile picture for Room Owner
                if (mode == 2 && userbundle.RoomOwner != null)
                {
                    if (string.IsNullOrWhiteSpace(userbundle.RoomOwner.RoomOwnerProfilePicture))
                    {
                        userbundle.RoomOwner.RoomOwnerProfilePicture = "/images/default.png";
                    }
                }
                // Add User entity
                ctx.Users.Add(userbundle.User);

                // Add role-specific entity
                switch (mode)
                {
                    case 1 when userbundle.Visitor != null:
                        userbundle.Visitor.UserId = userbundle.User.UserId;
                        ctx.Visitors.Add(userbundle.Visitor);
                        break;

                    case 2 when userbundle.RoomOwner != null:
                        userbundle.RoomOwner.UserId = userbundle.User.UserId;
                        if (userbundle.Account != null)
                        {
                            ctx.Accounts.Add(userbundle.Account);
                            await ctx.SaveChangesAsync(); // Save to get AccountId
                            userbundle.RoomOwner.AccountId = userbundle.Account.AccountId;
                        }
                        ctx.RoomOwners.Add(userbundle.RoomOwner);
                        break;

                    case 3 when userbundle.Staff != null:
                        userbundle.Staff.UserId = userbundle.User.UserId;
                        if (userbundle.Account != null)
                        {
                            ctx.Accounts.Add(userbundle.Account);
                            await ctx.SaveChangesAsync(); // Save to get AccountId
                            userbundle.Staff.AccountId = userbundle.Account.AccountId;
                        }
                        ctx.Staffs.Add(userbundle.Staff);
                        break;

                    case 4 when userbundle.Admin != null:
                        userbundle.Admin.UserId = userbundle.User.UserId;
                        if (userbundle.Account != null)
                        {
                            ctx.Accounts.Add(userbundle.Account);
                            await ctx.SaveChangesAsync(); // Save to get AccountId
                            userbundle.Admin.AccountId = userbundle.Account.AccountId;
                        }
                        ctx.Admins.Add(userbundle.Admin);
                        break;
                }

                await ctx.SaveChangesAsync();
                return errors;
            }
            catch (Exception ex)
            {
                errors.Add($"An error occurred during registration: {ex.Message}");
                return errors;
            }
        }


        /// <summary>
        /// mode: 1 = Visitor, 2 = Room Owner, 3 = Staff, 4 = Admin
        /// </summary>
        /// <param name="context"></param>
        /// <param name="mode"></param>
        /// <param name="targetUserId"></param>
        /// <returns></returns>
        public async Task<List<string>> EditRoleUserAsync(VisitorManagementSystemDatabaseContext context, int mode, string? targetUserId = null)
        {
            var errors = new List<string>();
            var changes = new List<string>();
            UserDataBundle existingUser;

            // ─── 0. Validate current user role ─────────────────────────────────────
            if (mode < 1 || mode > 4)
            {
                errors.Add("Invalid mode specified. Must be between 1 and 4.");
                return errors;
            }
            // ─── 1. Fetch user ──────────────────────────────────────
            if (targetUserId != null)
                existingUser = await GetUserRoleAsync(context, mode + 1, targetUserId);             
            else
                existingUser = await GetUserRoleAsync(context, mode + 1, User!.UserId);

            if (existingUser == null)
            {
                errors.Add("User not found.");
                return errors;
            }

            // ─── 2. Handle role-specific updates ───────────────────
            if (mode != null && mode < 5 && mode > 0)
            {
                if (mode == 1) // Visitor
                {
                    if (existingUser.Visitor != null && Visitor != null && (adminmode != null || staffmode != null))
                    {
                        // Update profile picture for visitor
                        if (existingUser.Visitor.ProfilePicture != Visitor.ProfilePicture)
                        {
                            changes.Add($"updated Profile Picture: {existingUser.Visitor.ProfilePicture} to {Visitor.ProfilePicture}");
                            existingUser.Visitor.ProfilePicture = Visitor.ProfilePicture;
                        }

                        context.Visitors.Update(existingUser.Visitor);
                    }
                    else
                    {
                        errors.Add("Visitor not found.");
                        return errors;
                    }
                }
                else if (mode == 2) // Room Owner
                {
                    if (existingUser.RoomOwner == null || RoomOwner == null)
                    {
                        errors.Add("Room Owner is Empty.");
                        return errors;
                    }

                    // Emergency contact updates
                    if (existingUser.RoomOwner.EmergencyContactNumber != RoomOwner.EmergencyContactNumber)
                    {
                        if (RoomOwner.EmergencyContactNumber == existingUser.User.ContactNumber)
                        {
                            errors.Add("Emergency contact number cannot be the same as contact number.");
                            return errors;
                        }
                        changes.Add($"updated Emergency Contact Number: {existingUser.RoomOwner.EmergencyContactNumber} to {RoomOwner.EmergencyContactNumber}");
                        existingUser.RoomOwner.EmergencyContactNumber = RoomOwner.EmergencyContactNumber;
                    }

                    if (existingUser.RoomOwner.EmergencyContactName != RoomOwner.EmergencyContactName)
                    {
                        changes.Add($"updated Emergency Contact Name: {existingUser.RoomOwner.EmergencyContactName} to {RoomOwner.EmergencyContactName}");
                        existingUser.RoomOwner.EmergencyContactName = RoomOwner.EmergencyContactName;
                    }

                    // Update profile picture for Room Owner
                    if (existingUser.RoomOwner.RoomOwnerProfilePicture != RoomOwner.RoomOwnerProfilePicture)
                    {
                        changes.Add($"updated Profile Picture: {existingUser.RoomOwner.RoomOwnerProfilePicture} to {RoomOwner.RoomOwnerProfilePicture}");
                        existingUser.RoomOwner.RoomOwnerProfilePicture = RoomOwner.RoomOwnerProfilePicture;
                    }

                    context.RoomOwners.Update(existingUser.RoomOwner);
                }
                else if (mode == 3 && adminmode != null) // Staff
                {
                    if (existingUser.Staff != null && Staff != null)
                    {
                        if (existingUser.Staff.DateHired != Staff?.DateHired)
                        {
                            changes.Add($"updated Date Hired: {existingUser.Staff.DateHired} to {Staff?.DateHired}");
                            existingUser.Staff.DateHired = Staff.DateHired;
                        }
                        if (existingUser.Staff.Position != Staff.Position)
                        {
                            changes.Add($"updated Position: {existingUser.Staff.Position} to {Staff.Position}");
                            existingUser.Staff.Position = Staff.Position;
                        }
                        context.Staffs.UpdateRange(existingUser.Staff);
                    }
                }
                else if (mode == 4) // Admin
                {
                    if (existingUser.Admin != null && Admin != null && existingUser.Admin.DateAssigned != Admin!.DateAssigned)
                    {
                        changes.Add($"updated Date Assigned: {existingUser.Admin.DateAssigned} to {Admin.DateAssigned}");
                        existingUser.Admin.DateAssigned = Admin.DateAssigned;
                        context.Admins.UpdateRange(existingUser.Admin);
                    }
                }

                if(existingUser.Account != null)
                {
                    // ─── 3. Update account details ─────────────────────────────
                    if (existingUser.Account.Username != Account?.Username)
                    {
                        var existingUsername = await context.Accounts
                            .FirstOrDefaultAsync(u => u.Username == Account.Username && User.UserId != existingUser.User.UserId);
                        if (existingUsername != null)
                        {
                            errors.Add($"Username '{Account.Username}' is already taken.");
                        }
                        else
                        {
                            changes.Add($"updated Username: {existingUser.Account.Username} to {Account.Username}");
                            existingUser.Account.Username = Account.Username;
                        }
                    }
                    if (existingUser.Account.Password != Account?.Password)
                    {
                        changes.Add($"updated Password for User: {existingUser.User.UserId}");
                        existingUser.Account.Password = Account.Password;
                    }
                    context.Accounts.UpdateRange(existingUser.Account);
                }
            }

            // ─── 4. Update core user fields ─────────────────────────
            void TryUpdate<T>(string label, T oldVal, T newVal, Action update) where T : IEquatable<T>
            {
                if (!Equals(oldVal, newVal))
                {
                    changes.Add($"{label} changed from \"{oldVal}\" to \"{newVal}\"");
                    update();
                }
            }

            TryUpdate("updated First Name", existingUser.User.FirstName, User?.FirstName, () => existingUser.User.FirstName = User?.FirstName);
            TryUpdate("updated Middle Name", existingUser.User.MiddleName ?? "", User?.MiddleName ?? "", () => existingUser.User.MiddleName = User?.MiddleName);
            TryUpdate("updated Last Name", existingUser.User.LastName, User?.LastName, () => existingUser.User.LastName = User?.LastName);
            TryUpdate("updated Account Status", existingUser.User.AccountStatus, User.AccountStatus, () => existingUser.User.AccountStatus = User.AccountStatus);
            TryUpdate("updated Date Created", existingUser.User.DateCreated, User.DateCreated, () => existingUser.User.DateCreated = User.DateCreated);
            TryUpdate("updated Contact Number", existingUser.User.ContactNumber, User.ContactNumber, () => existingUser.User.ContactNumber = User.ContactNumber);

            context.Users.Update(existingUser.User);

            // ─── 5. Final logging ────────────────────────────────────
            if (changes.Any())
            {
                context.accountActionLogs.AddRange(CreateActionLog($"{string.Join(", ", changes)}", InferActionType(changes)));
            }
            await context.SaveChangesAsync();
            return errors;
        }


        /// <summary>
        /// Mode: 1 = Soft delete (deactivation), 2 = Hard delete  Deletes a user and related records.
        /// </summary>
        public async Task DeleteUserAsync(VisitorManagementSystemDatabaseContext context, string userId, int mode)
        {
            var user = await context.Users.FindAsync(userId);
            var visitor = await context.Visitors.FindAsync(userId);
            var staff = await context.Staffs.FindAsync(userId);
            var admin = await context.Admins.FindAsync(userId);
            var roomOwner = await context.RoomOwners.FindAsync(userId);

            if (user == null  && ( visitor == null && staff == null && admin == null && roomOwner == null ))
                return;

            if( mode < 1 || mode > 2)
                throw new ArgumentException("Invalid mode. Use 1 for soft delete or 2 for hard delete.");

            if (mode == 1 && user != null)
            {
                // Soft delete
                user.AccountStatus = false;
                context.Users.Update(user);
            }
            else if (mode == 2)
            {
                // Delete related visitor logs
                var visitLogs = await context.VisitLogs
                    .Where(v => v.VisitorUserId == userId ||
                                v.OwnerUserId == userId)
                    .ToListAsync();
                context.VisitLogs.RemoveRange(visitLogs);

                // Delete related facility logs
                var facilityLogs = await context.FacilityLogs
                    .Where(f => f.UserId == userId)
                    .ToListAsync();
                context.FacilityLogs.RemoveRange(facilityLogs);

                // Delete room occupant record if room owner
                var occupant = await context.RoomOccupants
                    .FirstOrDefaultAsync(o => o.OwnerUserId == userId);
                if (occupant != null)
                    context.RoomOccupants.RemoveRange(occupant);

                // Hard delete
                if (visitor != null) context.Visitors.RemoveRange(visitor);
                if (staff != null) context.Staffs.RemoveRange(staff);
                if (admin != null) context.Admins.RemoveRange(admin);
                if (roomOwner != null) context.RoomOwners.RemoveRange(roomOwner);
                if (user != null) context.Users.RemoveRange(user);
            }
            
            string deletiontype = mode == 1 ? "soft" : "hard";  

            // Log the deletion
            context.accountActionLogs.Add(
                CreateActionLog(
                    $"{deletiontype} deleted user: {userId}",
                    InferActionType(new List<string> { "deleted" })
                )
            );

            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Retrieves a list of data objects based on the specified mode.
        /// </summary>
        /// <param name="context">The database context to query from.</param>
        /// <param name="mode">
        /// Mode selector:
        /// 1 - User
        /// 2 - Visitor
        /// 3 - Room Owner
        /// 4 - Staff
        /// 5 - Admin
        /// 6 - Room
        /// 7 - Facility
        /// 8 - Visit Log
        /// 9 - Facility Log
        /// 10 - Room Occupant
        /// 11 - Account Action Log
        /// 12 - CheckInOut
        /// 13 - Account
        /// </param>
        /// <returns>List of data objects corresponding to the selected mode.</returns>
        public async Task<List<object?>> GetDataListAsync(VisitorManagementSystemDatabaseContext context, int mode)
        {
            List<object?> result = new();

            switch (mode)
            {
                case 1: // User
                    result = (await context.Users.ToListAsync()).Cast<object?>().ToList();
                    break;
                case 2: // Visitor
                    result = (await context.Visitors.Include(x => x.User)
                                                    .ToListAsync()).Cast<object?>().ToList();
                    break;
                case 3: // Room Owner
                    result = (await context.RoomOwners.Include(x => x.User)
                                                      .Include(x => x.Account)
                                                      .ToListAsync()).Cast<object?>().ToList();
                    break;
                case 4: // Staff
                    result = (await context.Staffs.Include(x => x.User)
                                                  .Include(x => x.Position)
                                                  .Include(x => x.Account)
                                                  .ToListAsync()).Cast<object?>().ToList();
                    break;
                case 5: // Admin
                    result = (await context.Admins.Include(x => x.User)
                                                  .Include(x => x.Account)
                                                  .ToListAsync()).Cast<object?>().ToList();
                    break;
                case 6: // Room
                    result = (await context.Rooms.ToListAsync()).Cast<object?>().ToList();
                    break;
                case 7: // Facility
                    result = (await context.Facilities.ToListAsync()).Cast<object?>().ToList();
                    break;
                case 8: // Visit Log
                    result = (await context.VisitLogs.Include(x => x.Visitor)
                                                     .Include(x => x.CheckInOut)
                                                     .Include(x => x.Room)
                                                     .Include(x => x.RoomOwner)
                                                     .Include(x => x.Visitor.User)
                                                     .Include(x => x.RoomOwner.User)
                                                     .ToListAsync()).Cast<object?>().ToList();
                    break;
                case 9: // Facility Log
                    result = (await context.FacilityLogs.Include(x => x.User)
                                                       .Include(x => x.Facility)
                                                       .Include(x => x.IssueDate)
                                                       .Include(x => x.CheckInOut)
                                                       .ToListAsync()).Cast<object?>().ToList();
                    break;
                case 10: // Room Occupant
                    result = (await context.RoomOccupants.Include(x => x.Room)
                                                         .Include(x => x.RoomOwner)
                                                         .ThenInclude(x => x.User)
                                                         .ToListAsync()).Cast<object?>().ToList();
                    break;
                case 11: // Account Action Log
                    result = (await context.accountActionLogs.Include(x => x.TargetUser)
                                                             .Include(x => x.User)
                                                             .ToListAsync()).Cast<object?>().ToList();
                    break;
                case 12: // CheckInOut
                    result = (await context.CheckInOuts.ToListAsync()).Cast<object?>().ToList();
                    break;

                case 13: // Account
                    result = (await context.Accounts.ToListAsync()).Cast<object?>().ToList();
                    break;
                default:
                    throw new ArgumentException("Invalid mode.");
            }

            return result;
        }
      

        /// <summary>
        /// Retrieves user information and contact numbers based on the role type and ID.
        /// </summary>
        /// <param name="context">Database context instance</param>
        /// <param name="mode">
        /// 1 - User  
        /// 2 - Visitor  
        /// 3 - Room Owner  
        /// 4 - Staff  
        /// 5 - Admin  
        /// </param>
        /// <param name="roleid">The UserId associated with the role</param>
        /// <returns>User data bundle with role object and contact numbers</returns>
        public async Task<UserDataBundle> GetUserRoleAsync(VisitorManagementSystemDatabaseContext context, int mode, string roleid)
        {
            if (mode < 1 || mode > 5)
            {
                throw new ArgumentException("Invalid mode.");
            }

            var bundle = new UserDataBundle();

            switch (mode)
            {
                case 1:
                    var user = await context.Users
                        .FirstOrDefaultAsync(u => u.UserId == roleid)
                        ?? throw new InvalidOperationException("User not found.");
                    bundle.User = user;
                    break;

                case 2:
                    var visitor = await context.Visitors
                        .Include(v => v.User) // User navigation property
                        .FirstOrDefaultAsync(x => x.UserId == roleid)
                        ?? throw new InvalidOperationException("Visitor not found.");
                    bundle.Visitor = visitor;
                    bundle.User = visitor.User; // Include User data in the bundle
                    break;

                case 3:
                    var roomOwner = await context.RoomOwners
                        .Include(ro => ro.User) // User navigation property
                        .Include(ro => ro.Account) // Account navigation property
                        .FirstOrDefaultAsync(x => x.UserId == roleid)
                        ?? throw new InvalidOperationException("Room Owner not found.");
                    bundle.RoomOwner = roomOwner;
                    bundle.User = roomOwner.User; // Include User data in the bundle
                    bundle.Account = roomOwner.Account; // Include Account data in the bundle
                    break;

                case 4:
                    var staff = await context.Staffs
                        .Include(s => s.User) // User navigation property
                        .Include(ro => ro.Account) // Account navigation property
                        .FirstOrDefaultAsync(x => x.UserId == roleid)
                        ?? throw new InvalidOperationException("Staff not found.");
                    bundle.Staff = staff;
                    bundle.User = staff.User; // Include User data in the bundle
                    bundle.Account = staff.Account; // Include Account data in the bundle
                    break;

                case 5:
                    var admin = await context.Admins
                        .Include(a => a.User) // User navigation property
                        .Include(ro => ro.Account) // Account navigation property
                        .FirstOrDefaultAsync(x => x.UserId == roleid)
                        ?? throw new InvalidOperationException("Admin not found.");
                    bundle.Admin = admin;
                    bundle.User = admin.User; // Include User data in the bundle
                    bundle.Account = admin.Account; // Include Account data in the bundle
                    break;
            }

            return bundle;
        }

        /// <summary>
        /// Retrieve a list of users along with their role-specific data.
        /// mode: 1 = Visitor, 2 = Room Owner, 3 = Staff, 4 = Admin, 5 = All Users
        /// </summary>
        /// <param name="context">The database context.</param>
        /// <param name="mode">The mode for filtering which user role to fetch.</param>
        /// <returns>A list of UserDataBundle containing user and role information.</returns>
        public async Task<List<UserDataBundle>> GetUserRoleList(VisitorManagementSystemDatabaseContext context, int mode)
        {
            var userList = new List<UserDataBundle>();
            var errors = new List<string>();

            try
            {
                dynamic? roleList = mode switch
                {
                    1 => (await GetDataListAsync(context,2)).OfType<Visitor>().ToList(),
                    2 => (await GetDataListAsync(context,3)).OfType<RoomOwner>().ToList(),
                    3 => (await GetDataListAsync(context, 4)).OfType<Staff>().ToList(),
                    4 => (await GetDataListAsync(context, 5)).OfType<Admin>().ToList(),
                    5 => (await GetDataListAsync(context, 1)).OfType<User>().ToList(),
                    _ => null
                };

                if (roleList == null)
                {
                    errors.Add("Invalid role mode.");
                    return userList;
                }

                foreach (var role in roleList)
                {
                    if (role == null)
                        continue;

                    UserDataBundle userData;

                    if (mode == 5)
                    {
                        var user = role as User;
                        if (user == null)
                            continue;

                        var userId = user.UserId;

                        userData = new UserDataBundle
                        {
                            User = user,
                            Visitor = await context.Visitors.FirstOrDefaultAsync(v => v.UserId == userId),
                            RoomOwner = await context.RoomOwners
                                .Include(ro => ro.User)
                                .Include(ro => ro.Account)
                                .FirstOrDefaultAsync(ro => ro.UserId == userId),
                            Staff = await context.Staffs.FirstOrDefaultAsync(s => s.UserId == userId),
                            Admin = await context.Admins.FirstOrDefaultAsync(a => a.UserId == userId)
                        };
                    }
                    else
                    {
                        var userId = mode switch
                        {
                            1 => (role as Visitor)?.UserId,
                            2 => (role as RoomOwner)?.UserId,
                            3 => (role as Staff)?.UserId,
                            4 => (role as Admin)?.UserId,
                            _ => null
                        };

                        if (string.IsNullOrEmpty(userId))
                            continue;

                        userData = new UserDataBundle
                        {
                            User = await context.Users.FirstOrDefaultAsync(u => u.UserId == userId),
                            Visitor = mode == 1 ? role as Visitor : null,
                            RoomOwner = mode == 2
                                ? await context.RoomOwners
                                    .Include(ro => ro.User)
                                    .Include(ro => ro.Account)
                                    .FirstOrDefaultAsync(ro => ro.UserId == userId)
                                : null,
                            Staff = mode == 3 ? role as Staff : null,
                            Admin = mode == 4 ? role as Admin : null
                        };
                    }
                    if (userData.RoomOwner != null)
                    {
                        userData.Account = userData.RoomOwner.Account;
                    }
                    else if (userData.Staff != null)
                    {
                        userData.Account = userData.Staff.Account;
                    }
                    else if (userData.Admin != null)
                    {
                        userData.Account = userData.Admin.Account;
                    }

                    if (userData.User != null)
                        userList.Add(userData);
                }              
            }
            catch (Exception ex)
            {
                errors.Add("An error occurred while retrieving user roles: " + ex.Message);
            }

            return userList;
        }


        /// <summary>
        /// Retrieves a list of room occupants with optional filtering and sorting.
        /// </summary>
        /// <param name="context">Database context</param>
        /// <param name="mode">Mode (not used in logic here)</param>
        /// <param name="sort">
        /// Optional sort mode:
        /// 1 - Ascending OccupantId, 2 - Descending OccupantId,
        /// 3 - Ascending MoveInDate, 4 - Descending MoveInDate,
        /// 5 - Ascending MoveOutDate, 6 - Descending MoveOutDate,
        /// 7 - Ascending DateAssigned, 8 - Descending DateAssigned
        /// </param>
        /// <param name="occupantstatus">"Active", "Inactive", or "MovedOut"</param>
        public async Task<List<RoomOccupant>> GetOccupantList(VisitorManagementSystemDatabaseContext context, int mode, int? sort = null, string? occupantstatus = null)
        {
            var query = (await GetDataListAsync(context, 10)).OfType<RoomOccupant>()
                .AsQueryable();

            // Apply filtering based on status
            if (occupantstatus is not null)
            {
                query = occupantstatus switch
                {
                    "Active" => query.Where(x => x.OccupationStatus == true),
                    "Inactive" => query.Where(x => x.OccupationStatus == false),
                    "MovedOut" => query.Where(x => x.MoveOutDate != null),
                    _ => query
                };
            }

            // Apply sorting using switch expression
            query = sort switch
            {
                1 => query.OrderBy(x => x.OccupantId),
                2 => query.OrderByDescending(x => x.OccupantId),
                3 => query.OrderBy(x => x.MoveInDate),
                4 => query.OrderByDescending(x => x.MoveInDate),
                5 => query.OrderBy(x => x.MoveOutDate),
                6 => query.OrderByDescending(x => x.MoveOutDate),
                7 => query.OrderBy(x => x.DateAssigned),
                8 => query.OrderByDescending(x => x.DateAssigned),
                null => query,
                _ => throw new ArgumentException("Invalid sort option.")
            };

            return await query.ToListAsync();
        }

        /// <summary>
        /// Retrieves a list of visit logs with related visitor, owner, room, and check-in/out information,
        /// with optional sorting and filtering.
        /// </summary>
        /// <param name="context">Database context</param>
        /// <param name="sort">
        /// Optional sort mode:
        /// 1 - Ascending VisitLogId, 2 - Descending VisitLogId,
        /// 3 - Ascending TimeIn, 4 - Descending TimeIn,
        /// 5 - Ascending TimeOut, 6 - Descending TimeOut,
        /// 7 - Ascending DateCreated, 8 - Descending DateCreated,
        /// 9 - Ascending VerifiedDateTime, 10 - Descending VerifiedDateTime
        /// </param>
        /// <param name="filter">"Approved", "Denied", "Pending", "Completed"</param>
        /// <param name="filteruser">Filter by Visitor or Owner UserId</param>
        public async Task<List<VisitLog>> GetVisitLogListAsync(VisitorManagementSystemDatabaseContext context, int? sort = null, string? filter = null, string? filteruser = null)
        {
            var query = context.VisitLogs
                .Include(v => v.Visitor)
                    .ThenInclude(v => v.User)
                .Include(v => v.RoomOwner)
                    .ThenInclude(r => r.User)
                .Include(v => v.Room)
                .Include(v => v.CheckInOut)
                .AsQueryable();

            // Optional filter by user ID
            if (!string.IsNullOrWhiteSpace(filteruser))
            {
                query = query.Where(v =>
                    v.VisitorUserId == filteruser ||
                    v.OwnerUserId == filteruser);
            }

            // Optional status filtering
            if (filter is not null)
            {
                query = filter switch
                {
                    "Approved" => query.Where(v => v.VerificationStatus == true),
                    "Denied" => query.Where(v => v.VerificationStatus == false),
                    "Pending" => query.Where(v => v.VerificationStatus == null),
                    "Completed" => query.Where(v => v.CheckInOut != null && v.CheckInOut.CheckOutDateTime != null),
                    _ => query
                };
            }

            // Optional sorting
            query = sort switch
            {
                1 => query.OrderBy(v => v.VisitLogId),
                2 => query.OrderByDescending(v => v.VisitLogId),
                3 => query.OrderBy(v => v.CheckInOut!.CheckInDateTime),
                4 => query.OrderByDescending(v => v.CheckInOut!.CheckInDateTime),
                5 => query.OrderBy(v => v.CheckInOut!.CheckOutDateTime),
                6 => query.OrderByDescending(v => v.CheckInOut!.CheckOutDateTime),
                7 => query.OrderBy(v => v.IssueDate),
                8 => query.OrderByDescending(v => v.IssueDate),
                9 => query.OrderBy(v => v.VerifiedDateTime),
                10 => query.OrderByDescending(v => v.VerifiedDateTime),
                null => query,
                _ => throw new ArgumentException("Invalid sort option.")
            };
                
            return await query.ToListAsync();
        }

        /// <summary>
        /// Retrieves a list of facility logs with related facility, user, pass, date, and check-in/out information,
        /// with optional sorting and filtering.
        /// </summary>
        /// <param name="context">Database context</param>
        /// <param name="sort">
        /// Optional sort mode:
        /// 1 - Ascending FacilityLogId, 2 - Descending FacilityLogId,
        /// 3 - Ascending CheckInDateTime, 4 - Descending CheckInDateTime,
        /// 5 - Ascending CheckOutDateTime, 6 - Descending CheckOutDateTime,
        /// 7 - Ascending IssueDate, 8 - Descending IssueDate
        /// </param>
        /// <param name="filteruser">Filter by UserId</param>
        public async Task<List<FacilityLog>> GetFacilityLogListAsync(VisitorManagementSystemDatabaseContext context, int? sort = null, string? filteruser = null)
        {
            var query = (await GetDataListAsync(context,9))
                .OfType<FacilityLog>()
                .AsQueryable();

            // Optional filter by user ID
            if (!string.IsNullOrWhiteSpace(filteruser))
            {
                query = query.Where(f => f.UserId == filteruser);
            }

            // Optional sorting
            query = sort switch
            {
                1 => query.OrderBy(f => f.FacilityLogId),
                2 => query.OrderByDescending(f => f.FacilityLogId),
                3 => query.OrderBy(f => f.CheckInOut!.CheckInDateTime),
                4 => query.OrderByDescending(f => f.CheckInOut!.CheckInDateTime),
                5 => query.OrderBy(f => f.CheckInOut!.CheckOutDateTime),
                6 => query.OrderByDescending(f => f.CheckInOut!.CheckOutDateTime),
                7 => query.OrderBy(f => f.IssueDate),
                8 => query.OrderByDescending(f => f.IssueDate),
                null => query,
                _ => throw new ArgumentException("Invalid sort option.")
            };

            return await query.ToListAsync();
        }

        /// <summary>
        /// Retrieves a list of account action logs with related action type and user information,
        /// with optional sorting and filtering.
        /// </summary>
        /// <param name="context">The database context to query from.</param>
        /// <param name="sort">
        /// Optional sort mode:
        /// 1 - Ascending LogId, 
        /// 2 - Descending LogId, 
        /// 3 - Ascending ActionDateTime, 
        /// 4 - Descending ActionDateTime
        /// </param>
        /// <param name="filter">
        /// Optional filter for action keywords within ActionText
        /// (e.g., "Registered", "Verified", "Denied", "Updated", "Assigned", "Added", "Request", "ClockIn", "ClockOut")
        /// </param>
        /// <param name="filteruser">Optional filter by UserId or TargetUserId</param>
        /// <param name="filterusermode">Optional filter mode:
        /// 1 - Filter by UserId,
        /// 2 - Filter by TargetUserId</param>
        /// <returns>List of filtered and sorted AccountActionLog entries</returns>
        public async Task<List<AccountActionLog>> GetAccountActionLogListAsync(VisitorManagementSystemDatabaseContext context, int? sort = null, string? filter = null, int? filterusermode = null, string? filteruser = null)
        {
            List<AccountActionLog> ActionLogList = (await GetDataListAsync(context, 11)).OfType<AccountActionLog>().ToList();

            // Apply filtering by user, if provided
            if (filteruser != null)
            {
                if (filterusermode == 1)
                {
                    // Filter by UserId
                    ActionLogList = ActionLogList.Where(x => x.UserId == filteruser).ToList();
                }
                else
                {
                    // Filter by TargetUserId
                    ActionLogList = ActionLogList.Where(x => x.TargetUserId == filteruser).ToList();
                }
            }
            // Apply sorting
            ActionLogList = sort switch
            {
                1 => ActionLogList.OrderBy(x => x.LogId).ToList(),
                2 => ActionLogList.OrderByDescending(x => x.LogId).ToList(),
                3 => ActionLogList.OrderBy(x => x.ActionDateTime).ToList(),
                4 => ActionLogList.OrderByDescending(x => x.ActionDateTime).ToList(),
                null => ActionLogList,
                _ => throw new ArgumentException("Invalid sort option.")
            };
            // Apply filter
            if (filter is "Registered" or "Verified" or "Denied" or "Updated" or "Assigned" or "Added" or "Request" or "ClockOut" or "ClockIn")
            {
                ActionLogList = ActionLogList
                    .Where(v => v.ActionType == filter)
                    .ToList();
            }
            return ActionLogList;
        }

        /// <summary>
        /// Generates the next sequential ID with a given prefix and year.
        /// Ensures uniqueness by querying existing IDs.
        /// </summary>

        private async Task<string> GenerateNextIdAsync(string prefix, IQueryable<string> existingIds)
        {
            string yearPrefix = $"{prefix}-{DateTime.UtcNow.Year}-";

            var lastId = await existingIds
                .Where(id => id.StartsWith(yearPrefix))
                .OrderByDescending(id => id)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (lastId != null)
            {
                string numberPart = lastId.Substring(yearPrefix.Length);
                if (int.TryParse(numberPart, out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            return $"{yearPrefix}{nextNumber:D5}";
        }
    }
}
