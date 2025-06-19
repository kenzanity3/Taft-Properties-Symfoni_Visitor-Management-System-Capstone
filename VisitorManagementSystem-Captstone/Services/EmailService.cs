using System.Net.Mail; //provides classes for sending emails via SMTP
using System.Net; //provides core networking protocols and utilities
using System.Net.Mime; //provides types for MIME (Multipurpose Internet Mail Extensions) handling
using System.Web;// provides utilities for web-related operations like HTML encoding
using Microsoft.AspNet.Identity;// provides ASP.NET Identity core interfaces and classes

namespace VisitorManagementSystem_Captstone.Services
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
            // Create plain text version of the email
            string text = $"TAFT: This is your verification {message.Body}";
            // Create HTML version of the email with proper encoding
            string html = $"<strong>Your verification code:</strong> {HttpUtility.HtmlEncode(message.Body)}<br/>";
            // Add additional instructions to HTML version
            html += HttpUtility.HtmlEncode($"Copy this code and paste it in the verification box to reset your password.");
            #endregion

            // Create the mail message object
            MailMessage msg = new MailMessage();
            // Set sender address 
            msg.From = new MailAddress("tafftverificationcode@gmail.com");
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
            System.Net.NetworkCredential credentials = new System.Net.NetworkCredential("tafftverificationcode@gmail.com", "qcyg ovhx riwu hucn");
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
