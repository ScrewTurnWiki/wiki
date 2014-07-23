
using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using ScrewTurn.Wiki.PluginFramework;
using System.Text;
using System.Security.Cryptography;

namespace ScrewTurn.Wiki {

	public partial class User : BasePage {

		private string currentUsername = null;
		private UserInfo currentUser = null;

		protected void Page_Load(object sender, EventArgs e) {
			Page.Title = Properties.Messages.UserTitle + " - " + Settings.WikiTitle;

			currentUsername = Request["User"];
			if(string.IsNullOrEmpty(currentUsername)) currentUsername = Request["Username"];
			if(string.IsNullOrEmpty(currentUsername)) UrlTools.Redirect("Default.aspx");

			if(currentUsername == "admin") {
				currentUser = Users.GetAdministratorAccount();
			}
			else currentUser = Users.FindUser(currentUsername);

			if(currentUser == null) UrlTools.Redirect("Default.aspx");

			if(!Page.IsPostBack) {
				lblTitle.Text = lblTitle.Text.Replace("##NAME##", Users.GetDisplayName(currentUser));

				txtSubject.Text = Request["Subject"];
				if(txtSubject.Text != "" && SessionFacade.LoginKey == null) UrlTools.Redirect("Login.aspx?Redirect=" + Tools.UrlEncode(Tools.GetCurrentUrlFixed()));
			}

			if(SessionFacade.LoginKey == null) pnlMessage.Visible = false;
			else pnlMessage.Visible = true;

			DisplayGravatar();

			DisplayRecentActivity();
		}

		/// <summary>
		/// Displays the gravatar of the user.
		/// </summary>
		private void DisplayGravatar() {
			if(Settings.DisplayGravatars) {
				lblGravatar.Text = string.Format(@"<img src=""http://www.gravatar.com/avatar/{0}?d=identicon"" alt=""Gravatar"" />",
					GetGravatarHash(currentUser.Email));
			}
		}

		/// <summary>
		/// Gets the gravatar hash of an email.
		/// </summary>
		/// <param name="email">The email.</param>
		/// <returns>The hash.</returns>
		private static string GetGravatarHash(string email) {
			MD5 md5 = MD5.Create();
			byte[] bytes = md5.ComputeHash(Encoding.ASCII.GetBytes(email.ToLowerInvariant()));

			StringBuilder sb = new StringBuilder(100);
			foreach(byte b in bytes) {
				sb.AppendFormat("{0:x2}", b);
			}

			return sb.ToString();
		}

		/// <summary>
		/// Displays the recent activity.
		/// </summary>
		private void DisplayRecentActivity() {
			RecentChange[] changes = RecentChanges.GetAllChanges();

			List<RecentChange> result = new List<RecentChange>(Settings.MaxRecentChangesToDisplay);

			foreach(RecentChange c in changes) {
				if(c.User == currentUser.Username) {
					result.Add(c);
				}
			}

			// Sort by date/time descending
			result.Reverse();

			lblNoActivity.Visible = result.Count == 0;

			lblRecentActivity.Text = Formatter.BuildRecentChangesTable(result, FormattingContext.Other, null);
		}

		protected void btnSend_Click(object sender, EventArgs e) {
			lblSendResult.Text = "";
			lblSendResult.CssClass = "";

			Page.Validate();
			if(!Page.IsValid) return;

			UserInfo loggedUser = SessionFacade.GetCurrentUser();

			Log.LogEntry("Sending Email to " + currentUser.Username, EntryType.General, loggedUser.Username);
			EmailTools.AsyncSendEmail(currentUser.Email,
				"\"" + Users.GetDisplayName(loggedUser) + "\" <" + Settings.SenderEmail + ">",
				txtSubject.Text,
				Users.GetDisplayName(loggedUser) + " sent you this message from " + Settings.WikiTitle + ". To reply, please go to " + Settings.MainUrl + "User.aspx?Username=" + Tools.UrlEncode(loggedUser.Username) + "&Subject=" + Tools.UrlEncode("Re: " + txtSubject.Text) + "\nPlease do not reply to this Email.\n\n------------\n\n" + txtBody.Text,
				false);
			lblSendResult.Text = Properties.Messages.MessageSent;
			lblSendResult.CssClass = "resultok";

			txtSubject.Text = "";
			txtBody.Text = "";
		}

	}

}
