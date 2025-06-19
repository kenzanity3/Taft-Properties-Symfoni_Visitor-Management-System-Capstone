using Microsoft.CodeAnalysis.Classification;
using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem_Captstone.Data;
using VisitorManagementSystem_Captstone.NewModel;

namespace VisitorManagementSystem_Captstone.ViewModels
{
    public class RegisterViewModel
    {    
        public required User User { get; set; } 
        public Visitor? Visitor { get; set; }
        public Staff? Staff { get; set; }
        public Admin? Admin { get; set; }
        public RoomOwner? RoomOwner { get; set; }
        public User_ContactNumber? user_ContactNumber { get; set; }

        public async Task<List<string>> RegisterAsync(testContext Context)
        {
            var errors = new List<string>();
            var user = Context.Users.FirstOrDefault(q => q.Email == User.Email);

            //if register from visitor registration form
            if (user != null && Visitor != null)
                errors.Add("Email already exists.");

            if (user_ContactNumber == null && Visitor != null)
                errors.Add("Contact number is empty.");

            if (user_ContactNumber != null && await Context.User_ContactNumbers.AnyAsync(q => q.ContactNumber == user_ContactNumber.ContactNumber))
                errors.Add("Contact number already exists.");
                 
            if (errors.Any()) return errors;
            
            Context.Users.Add(User);
            if(user_ContactNumber != null)
            {
                user_ContactNumber.UserId = User.UserId;
                Context.User_ContactNumbers.Add(user_ContactNumber);
            }

            if (Visitor != null)
            {
                Visitor.VisitorId = await GenerateNextIdAsync("VIS", Context.Visitors.Select(v => v.VisitorId));
                Visitor.UserId = User.UserId;
                Context.Visitors.Add(Visitor);
            }

            if (Staff != null)
            {
                Staff.StaffId = await GenerateNextIdAsync("STF", Context.Staffs.Select(s => s.StaffId));
                Staff.UserId = User.UserId;
                Context.Staffs.Add(Staff);
            }

            if (Admin != null)
            {
                Admin.AdminId = await GenerateNextIdAsync("ADM", Context.Admins.Select(a => a.AdminId));
                Admin.UserId = User.UserId;
                Context.Admins.Add(Admin);
            }

            if (RoomOwner != null)
            {
                RoomOwner.OwnerId = await GenerateNextIdAsync("RMW", Context.RoomOwners.Select(r => r.OwnerId));
                RoomOwner.UserId = User.UserId;
                Context.RoomOwners.Add(RoomOwner);
            }
            Console.WriteLine(Visitor!.VisitorId+ "------------------");
            await Context.SaveChangesAsync();
            return errors;
        }

        private async Task<string> GenerateNextIdAsync(string prefix, IQueryable<string> existingIds)
        {
            string yearPrefix = $"{prefix}-{DateTime.Now.Year}-";

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

