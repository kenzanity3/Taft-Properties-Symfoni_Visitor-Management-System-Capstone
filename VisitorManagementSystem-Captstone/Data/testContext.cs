using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem_Captstone.NewModel;

namespace VisitorManagementSystem_Captstone.Data
{

    //Setting The database SQL Server
    //Database context class for Visitor Management System
    public class testContext : DbContext
    {
        // Constructor that accepts DbContextOptions to configure the database context
        public testContext(DbContextOptions<testContext> options)
            : base(options)
        {
        }
        // DbSet for managing user accounts (shared by admin, staff, room owners, etc.)
        public DbSet<User> Users { get; set; }

        // DbSet for visitor profiles and identification details
        public DbSet<Visitor> Visitors { get; set; }

        // DbSet for facility staff members and their assignments
        public DbSet<Staff> Staffs { get; set; }

        // DbSet for administrator accounts and administrative actions
        public DbSet<Admin> Admins { get; set; }

        // DbSet for unit or room owners with linked user accounts
        public DbSet<RoomOwner> RoomOwners { get; set; }

        // DbSet for room records including tower, floor, and identifiers
        public DbSet<Room> Rooms { get; set; }

        // DbSet for facility resources (gyms, lounges, pools, etc.)
        public DbSet<Facility> Facilities { get; set; }

        // DbSet for room occupancy records (owner, tenant, temporary) with authority types
        public DbSet<RoomOccupant> RoomOccupants { get; set; }

        // DbSet for storing one-to-many user contact numbers
        public DbSet<User_ContactNumber> User_ContactNumbers { get; set; }

        // DbSet for visit logs including check-in, check-out, and access details
        public DbSet<VisitLog> VisitLogs { get; set; }

        // DbSet for tracking system actions and generating notifications
        public DbSet<AccountActionLog> accountActionLogs { get; set; }

        // DbSet for facility access history, showing who used a facility and when
        public DbSet<FacilityLog> FacilityLogs { get; set; }

        // DbSet for ErrorViewModel to handle error-related data
        //public DbSet<ErrorViewModel> ErrorViewModels { get; set; }
        // Configures the model and relationships using Fluent API
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            foreach (var foreignKey in modelBuilder.Model.GetEntityTypes()
           .SelectMany(e => e.GetForeignKeys()))
            {
                foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
            }
            // Initialize PasswordHasher
            var passwordHasher = new PasswordHasher<User>();
                   
            // Create a new user instance for the admin user
            var adminUser = new User
            {
                // Set properties for the admin user
                UserId = $"USER-0001",
                FirstName = "Main Administrator",
                MiddleName = "-",
                LastName = "-",
                DateOfBirth = new DateOnly(2002, 9, 14), // September 14, 2002
                Username = "admin",
                Password = "AQAAAAIAAYagAAAAEDav1nOozAS/YpYepAYxmfg8arUJmWPtdJOskBHiZYFoSyk4G3f+1TU0+oTuyivZIg==",// Hashed encrypted password                
                Email = "act.lusamdelictor@gmail.com",
                StreetAddress = "default street",
                City = "default city",
                Country = "default country"
            };

            // Seed the admin user with hashed password
            modelBuilder.Entity<User>().HasData(adminUser);
            modelBuilder.Entity<Admin>().HasData(new Admin
            {
                AdminId = $"ADM-{DateTime.Now.Year}-0001",
                DateAssigned = DateOnly.FromDateTime(DateTime.Now),
                UserId = adminUser.UserId // admin user has UserId
            });
            modelBuilder.Entity<User_ContactNumber>().HasData(new User_ContactNumber
            {
                UserId = adminUser.UserId,
                ContactNumber = "09123456789"
            });
            
        }
    }
}


