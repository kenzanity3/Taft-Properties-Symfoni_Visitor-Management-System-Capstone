using Microsoft.AspNet.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem_Captstone.Models;

namespace VisitorManagementSystem_Captstone.Data
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
        // DbSet for User model to interact with the Users table in the database
        public DbSet<User> Users { get; set; }








        // DbSet for ErrorViewModel to handle error-related data
        //public DbSet<ErrorViewModel> ErrorViewModels { get; set; }
        // Configures the model and relationships using Fluent API
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Initialize PasswordHasher
            var passwordHasher = new PasswordHasher<User>();

            // Create a new user instance for the admin user
            var adminUser = new User
            {
                // Set properties for the admin user
                UserId = 1,
                FirstName = "Main Administrator",
                MiddleName = "-",
                LastName = "-",
                Username = "admin",
                AccountType = "Admin",
                Email = "act.lusamdelictor@gmail.com",
                IsActive = true,
                Password = "AQAAAAIAAYagAAAAEDav1nOozAS/YpYepAYxmfg8arUJmWPtdJOskBHiZYFoSyk4G3f+1TU0+oTuyivZIg=="// Hashed encrypted password
            };

            // Hash the password, maka error siya dapat ari sa adminuser
            //adminUser.Password = passwordHasher.HashPassword(adminUser, "admin123");


            // Seed the admin user with hashed password
            modelBuilder.Entity<User>().HasData(adminUser);
        }
    }
}


