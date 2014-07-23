
using System;
using System.Configuration;
using System.Net;
using System.Net.Mail;
using System.Security.Principal;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Implements email-related tools.
	/// </summary>
	public static class EmailTools {

		/// <summary>
		/// Sends an email.
		/// </summary>
		/// <param name="recipient">The recipient.</param>
		/// <param name="sender">The sender.</param>
		/// <param name="subject">The subject.</param>
		/// <param name="body">The message body.</param>
		/// <param name="html"><c>true</c> if the body is HTML.</param>
		public static void AsyncSendEmail(string recipient, string sender, string subject, string body, bool html) {
			System.Threading.ThreadPool.QueueUserWorkItem(delegate(object state) {
				using(((WindowsIdentity)state).Impersonate()) {
					MailMessage message = new MailMessage(sender, recipient, subject, body);
					message.IsBodyHtml = html;
					TrySendMessage(message);
				}
			}, WindowsIdentity.GetCurrent());
		}

		/// <summary>
		/// Tries to send a message, swallowing all exceptions.
		/// </summary>
		/// <param name="message">The message to send.</param>
		private static void TrySendMessage(MailMessage message) {
			try {
				GenerateSmtpClient().Send(message);
			}
			catch(Exception ex) {
				if(ex is SmtpException) {
					Log.LogEntry("Unable to send Email: " + ex.Message, EntryType.Error, Log.SystemUsername);
				}
				else Log.LogEntry(ex.ToString(), EntryType.Error, Log.SystemUsername);
			}
		}

		/// <summary>
		/// Generates a new SMTP client with the proper settings.
		/// </summary>
		/// <returns>The generates SMTP client.</returns>
		private static SmtpClient GenerateSmtpClient() {
			SmtpClient client = new SmtpClient(Settings.SmtpServer);
			if(Settings.SmtpUsername.Length > 0) {
				client.Credentials = new NetworkCredential(Settings.SmtpUsername, Settings.SmtpPassword);
			}
			client.EnableSsl = Settings.SmtpSsl;
			if(Settings.SmtpPort != -1) client.Port = Settings.SmtpPort;
			else if(Settings.SmtpSsl) client.Port = 465;
			return client;
		}

		/// <summary>
		/// Gets the email addresses of a set of users.
		/// </summary>
		/// <param name="users">The users.</param>
		/// <returns>The email addresses.</returns>
		public static string[] GetRecipients(UserInfo[] users) {
			if(users == null) return new string[0];

			string[] result = new string[users.Length];

			for(int i = 0; i < result.Length; i++) {
				result[i] = users[i].Email;
			}

			return result;
		}

		/// <summary>
		/// Asynchronously sends a mass email, using BCC.
		/// </summary>
		/// <param name="recipients">The recipents.</param>
		/// <param name="sender">The sender.</param>
		/// <param name="subject">The subject.</param>
		/// <param name="body">The body.</param>
		/// <param name="html"><c>true</c> if the body is HTML.</param>
		public static void AsyncSendMassEmail(string[] recipients, string sender, string subject, string body, bool html) {
			if(recipients.Length == 0) return;

			System.Threading.ThreadPool.QueueUserWorkItem(delegate(object state) {
				using(((WindowsIdentity)state).Impersonate()) {
					MailMessage message = new MailMessage(new MailAddress(sender), new MailAddress(sender));
					message.Subject = subject;
					message.Body = body;
					for(int i = 0; i < recipients.Length; i++) {
						message.Bcc.Add(new MailAddress(recipients[i]));
					}
					message.IsBodyHtml = html;
					TrySendMessage(message);
				}
			}, WindowsIdentity.GetCurrent());
		}

		/// <summary>
		/// Notifies an error to the email addresses set in the configuration, swallowing all exceptions.
		/// </summary>
		/// <param name="ex">The exception to notify.</param>
		/// <param name="url">The URL that caused the error, if any.</param>
		public static void NotifyError(Exception ex, string url) {
			try {
				string[] recipients = Settings.ErrorsEmails;

				if(recipients.Length > 0) {
					AsyncSendMassEmail(recipients, Settings.SenderEmail, "Error Notification", "An error occurred on " +
						DateTime.Now.ToString("yyyy'/'MM'/'dd' 'HH':'mm':'ss") + " (server time) in the wiki hosted at " +
						Settings.MainUrl + " - server stack trace follows.\r\n\r\n" +
						(!string.IsNullOrEmpty(url) ? url + "\r\n\r\n" : "") +
						ex.ToString(), false);
				}
			}
			catch { }
		}

	}

}
