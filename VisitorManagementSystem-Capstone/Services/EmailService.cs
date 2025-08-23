using System.Net.Mail; //provides classes for sending emails via SMTP
using System.Net; //provides core networking protocols and utilities
using System.Net.Mime; //provides types for MIME (Multipurpose Internet Mail Extensions) handling
using System.Web;// provides utilities for web-related operations like HTML encoding
using Microsoft.AspNet.Identity;// provides ASP.NET Identity core interfaces and classes

namespace VisitorManagementSystem_Capstone.Services
{
    /// <summary>
    /// Service for sending emails, particularly for identity-related messages
    /// </summary>
    public class EmailService
    {
        /// <summary>
        /// Asynchronously sends an identity message via email
        /// </summary>
        /// <param name="message">The identity message containing destination, subject and body</param>
        /// <returns>A Task representing the asynchronous operation</returns>
        /// <remarks>
        /// Wraps the synchronous sendMail method in a Task to provide async functionality
        /// </remarks>
        public Task SendAsync(IdentityMessage message)
        {
            return Task.Factory.StartNew(() =>
            {
                sendMail(message);
            });
        }

        /// <summary>
        /// Core method that constructs and sends the email message
        /// </summary>
        /// <param name="message">The identity message to send</param>
        /// <remarks>
        /// Creates both text and HTML versions of the email
        /// Configures SMTP client with hardcoded credentials (security concern)
        /// Uses Gmail's SMTP server on port 587 with SSL
        /// </remarks>
        void sendMail(IdentityMessage message)
        {
            #region formatter
            // Plain text version
            string text = $"TAFT Visitor Management System\n\n" +
                          $"Your verification code: {message.Body}\n\n" +
                          $"Copy this code and paste it in the verification box to reset your password.";

            // HTML version with logo, bold code, and styling
            string logoUrl = "https://yourdomain.com/taft-logo.png"; // <-- Update this to your actual logo URL

            string html = $@"
<div style='max-width:480px;margin:30px auto;padding:24px;border-radius:12px;
    background:#f9f9f9;font-family:Segoe UI,Arial,sans-serif;box-shadow:0 2px 8px #0001;'>
    <div style='text-align:center;margin-bottom:18px;'>
        <img src='{logoUrl}' alt='TAFT Logo' style='height:64px;margin-bottom:8px;'/>
        <h2 style='margin:0;color:#2a2a2a;font-weight:700;letter-spacing:1px;'>TAFT Visitor Management System</h2>
    </div>
    <div style='background:#fff;padding:20px 16px;border-radius:8px;box-shadow:0 1px 4px #0001;'>
        <p style='font-size:1.1em;margin-bottom:12px;'>
            <span style='font-size:1.3em;'>🔒</span>
            <strong>Verification Code</strong>
        </p>
        <div style='font-size:2em;font-weight:800;letter-spacing:6px;color:#1a73e8;
            background:#e3f0ff;padding:12px 0;border-radius:6px;margin-bottom:18px;text-align:center;'>
            {HttpUtility.HtmlEncode(message.Body)}
        </div>
        <p style='margin:0 0 10px 0;font-size:1em;color:#444;'>
            Please copy the code above and paste it in the verification box to reset your password.
        </p>
        <p style='margin:0;font-size:0.95em;color:#888;'>
            If you did not request this, you can safely ignore this email.
        </p>
    </div>
    <div style='text-align:center;margin-top:18px;font-size:0.9em;color:#aaa;'>
        &copy; {DateTime.Now.Year} TAFT Visitor Management System
    </div>
</div>
";
            #endregion

            // Create the mail message object
            MailMessage msg = new MailMessage();
            // Set sender address 
            msg.From = new MailAddress("taftverify001@gmail.com");
            // Add recipient from the identity message
            msg.To.Add(new MailAddress(message.Destination));
            // Set email subject
            msg.Subject = message.Subject;
            // Add both text and HTML versions as alternate views
            msg.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(text, null, MediaTypeNames.Text.Plain));
            msg.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(html, null, MediaTypeNames.Text.Html));

            // Configure SMTP client for Gmail DO NOT EDIT THIS its the workhorse that actually delivers your verification emails to Gmail's servers.
            SmtpClient smtpClient = new SmtpClient("smtp.gmail.com", Convert.ToInt32(587));
            // Set credentials (hardcoded - security concern)
            System.Net.NetworkCredential credentials = new System.Net.NetworkCredential("taftverify001@gmail.com", "gbue arla ctnu vmpv");
            smtpClient.Credentials = credentials;
            // Enable SSL for secure transmission
            smtpClient.EnableSsl = true;
            // Send the email
            smtpClient.Send(msg);
        }

        /// <summary>
        /// Sends a verification code email to the specified address
        /// </summary>
        /// <param name="email">The recipient's email address</param>
        /// <param name="verificationCode">The verification code to send</param>
        /// <returns>A Task representing the asynchronous operation</returns>
        /// <remarks>
        /// Convenience method specifically for sending verification codes
        /// Creates an IdentityMessage and sends it asynchronously
        /// </remarks>
        public async Task SendVerificationEmail(string email, string verificationCode)
        {
            var message = new IdentityMessage
            {
                Destination = email,
                Subject = "TAFT Visitor Management System Verification Code",
                Body = verificationCode
            };
            await SendAsync(message);
        }
    }
}