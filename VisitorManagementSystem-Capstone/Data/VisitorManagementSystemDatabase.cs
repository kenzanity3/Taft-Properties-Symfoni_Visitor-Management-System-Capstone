using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem_Capstone.Models;
using VisitorManagementSystem_Capstone.ViewModels;

namespace VisitorManagementSystem_Capstone.Data
{
    //Setting The database SQL Server
    //Database context class for Visitor Management System
    public class VisitorManagementSystemDatabaseContext : DbContext
    {
        // Constructor that accepts DbContextOptions to configure the database context
        public VisitorManagementSystemDatabaseContext(DbContextOptions<VisitorManagementSystemDatabaseContext> options)
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

        //DbSet link user informations with their account
        public DbSet<Account> Accounts { get; set; }

        // DbSet for room records including tower, floor, and identifiers
        public DbSet<Room> Rooms { get; set; }

        // DbSet for facility resources (gyms, lounges, pools, etc.)
        public DbSet<Facility> Facilities { get; set; }

        // DbSet for room occupancy records (owner, tenant, temporary) with authority types
        public DbSet<RoomOccupant> RoomOccupants { get; set; }

        // DbSet for visit logs including check-in, check-out, and access details
        public DbSet<VisitLog> VisitLogs { get; set; }

        // DbSet for tracking system actions and generating notifications
        public DbSet<AccountActionLog> accountActionLogs { get; set; }

        // DbSet for facility access history, showing who used a facility and when
        public DbSet<FacilityLog> FacilityLogs { get; set; }

        // DbSet for predefined check-in/check-out options
        public DbSet<CheckInOut> CheckInOuts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ErrorViewModel>()
                .HasNoKey(); // no primary key for ErrorViewModel
            base.OnModelCreating(modelBuilder);
            foreach (var foreignKey in modelBuilder.Model.GetEntityTypes()
           .SelectMany(e => e.GetForeignKeys()))
            {
                foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
            }
            modelBuilder.Entity<Account>()
                .HasIndex(u => u.Username)
                .IsUnique(); // Ensure unique usernames

            modelBuilder.Entity<Account>()
                .HasIndex(u => u.Email)
                .IsUnique(); // Ensure unique emails

            modelBuilder.Entity<User>()
                .HasIndex(u => u.ContactNumber)
                .IsUnique(); // Ensure unique primary contact numbers

            //// Seed the default data into the database.
            //// This prepopulates essential entities with initial values for system functionality.
            //// Each call to HasData injects predefined records stored in the DefaultData class.
            modelBuilder.Entity<User>().HasData(DefaultData.DefaultUsers.ToArray());
            modelBuilder.Entity<Account>().HasData(DefaultData.DefaultAccounts.ToArray());
            modelBuilder.Entity<Staff>().HasData(DefaultData.DefaultStaff);
            modelBuilder.Entity<RoomOwner>().HasData(DefaultData.DefaultRoomOwner);
            modelBuilder.Entity<Visitor>().HasData(DefaultData.DefaultVisitors.ToArray());
            modelBuilder.Entity<Admin>().HasData(DefaultData.DefaultAdmin);
            modelBuilder.Entity<RoomOccupant>().HasData(DefaultData.DefaultRoomOccupant);
            modelBuilder.Entity<Facility>().HasData(DefaultData.FacilitiesData);
            modelBuilder.Entity<Room>().HasData(DefaultData.RoomData.ToArray());
            modelBuilder.Entity<VisitLog>().HasData(DefaultData.DefaultVisitLog);
            modelBuilder.Entity<CheckInOut>().HasData(DefaultData.CheckInOutData);
        }
    }
}