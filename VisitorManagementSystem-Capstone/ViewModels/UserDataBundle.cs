using VisitorManagementSystem_Capstone.Models;

namespace VisitorManagementSystem_Capstone.ViewModels
{
    public class UserDataBundle
    {
        /// <summary>
        /// For admin to view all user data.
        /// </summary>
        public User? User { get; set; }

        /// <summary>
        /// For admin to view visitor-specific data.
        /// </summary>
        public Visitor? Visitor { get; set; }


        // Room information properties
        /// <summary>
        /// For admin to view room owner-specific data.
        /// </summary>
        public RoomOwner? RoomOwner { get; set; }
        

        /// <summary>
        /// For admin to view room specific data.
        /// </summary>
        public Room? Room{ get; set; }

        /// <summary>
        /// For admin to view staff-specific data.
        /// </summary>
        public Staff? Staff { get; set; }

        /// <summary>
        /// For admin to view admin-specific data.
        /// </summary>
        public Admin? Admin { get; set; }

        /// <summary>
        /// For admin to view account-related data.
        /// </summary>
        public Account? Account { get; set; }


       
    }
}

