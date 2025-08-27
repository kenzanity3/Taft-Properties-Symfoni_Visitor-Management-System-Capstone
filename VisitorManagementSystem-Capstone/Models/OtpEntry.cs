namespace VisitorManagementSystem_Capstone.Models
{
    public class OtpEntry
    {
        /// <summary>
        /// The generated OTP code used for validation (6-character alphanumeric).
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// The maximum number of times the OTP can be used before it expires.
        /// </summary>
        public int MaxUsage { get; set; } = 1;

        /// <summary>
        /// The Id associated with the OTP, used to track the originator.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// it track on how many expirationtiime has been left
        /// </summary>
        public DateTime ExpirationTime { get; set; }

        /// <summary>
        /// The Ids that used this to ensure it will not be repeatedly used.
        /// </summary>
        public List<string> userids { get; set; } = new List<string>();
    }
}