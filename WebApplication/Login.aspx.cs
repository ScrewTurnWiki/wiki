
using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Collections.Generic;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki {

	public partial class Login : BasePage {

        protected void Page_Load(object sender, EventArgs e) {
			Page.Title = Properties.Messages.LoginTitle + " - " + Settings.WikiTitle;

			rxNewPassword1.ValidationExpression = Settings.PasswordRegex;
			rxNewPassword2.ValidationExpression = Settings.PasswordRegex;

			lblResult.Text = "";
			lblResult.CssClass = "";

			lblLostPassword.Text = Properties.Messages.LostPassword;

			PrintLoginNotice();

			if(Request["ForceLogout"] != null) {
				Logout();
				Session[LoginTools.Logout] = true;
				if(Request["Redirect"] != null) Response.Redirect(Request["Redirect"]);
				return;
			}

			// In case of provider supporting autologin, a user might not be able to logout
			// without applying a "filter" because the provider might keep logging her in.
			// When she clicks Logout and redirects to Login.aspx?Logout=1 a flag is set,
			// avoiding autologin for the current session - see LoginTools class
			if(Request["Logout"] != null) Session[LoginTools.Logout] = true;

			// All the following logic must be executed only on first page request
			if(Page.IsPostBack) return;

			if(SessionFacade.LoginKey != null) {
				mlvLogin.ActiveViewIndex = 0;
				lblLogout.Text = "<b>" + SessionFacade.CurrentUsername + "</b>, " + lblLogout.Text;
			}
			else {
				if(Request["PasswordReset"] != null) mlvLogin.ActiveViewIndex = 2;
				else if(Request["ResetCode"] != null && Request["Username"] != null) {
					if(LoadUserForPasswordReset() != null) {
						mlvLogin.ActiveViewIndex = 3;
					}
				}
				else mlvLogin.ActiveViewIndex = 1;
			}

            if(Request["Activate"] != null && Request["Username"] != null && !Page.IsPostBack) {
				UserInfo user = Users.FindUser(Request["Username"]);
                if(user!= null && Tools.ComputeSecurityHash(user.Username, user.Email, user.DateTime).Equals(Request["Activate"])) {
					Log.LogEntry("Account activation requested for " + user.Username, EntryType.General, Log.SystemUsername);
                    if(user.Active) {
						lblResult.CssClass = "resultok";
						lblResult.Text = Properties.Messages.AccountAlreadyActive;
                        return;
                    }
                    if(user.DateTime.AddHours(24).CompareTo(DateTime.Now) < 0) {
                        // Too late
						lblResult.CssClass = "resulterror";
						lblResult.Text = Properties.Messages.AccountNotFound;
                        // Delete user (is this correct?)
                        Users.RemoveUser(user);
                        return;
                    }
                    // Activate User
					Users.SetActivationStatus(user, true);
					lblResult.CssClass = "resultok";
					lblResult.Text = Properties.Messages.AccountActivated;
                    return;
                }
				lblResult.CssClass = "resulterror";
				lblResult.Text = Properties.Messages.AccountNotActivated;
                return;
            }
        }

		/// <summary>
		/// Loads the user for the password reset procedure.
		/// </summary>
		/// <returns>The user, or <c>null</c>.</returns>
		private UserInfo LoadUserForPasswordReset() {
			UserInfo user = Users.FindUser(Request["Username"]);
			if(user != null && Request["ResetCode"] == Tools.ComputeSecurityHash(user.Username, user.Email, user.DateTime)) {
				return user;
			}
			else return null;
		}

		/// <summary>
		/// Prints the login notice.
		/// </summary>
		public void PrintLoginNotice() {
			string n = Content.GetPseudoCacheValue("LoginNotice");
			if(n == null) {
				n = Settings.Provider.GetMetaDataItem(MetaDataItem.LoginNotice, null);
				if(!string.IsNullOrEmpty(n)) {
					n = FormattingPipeline.FormatWithPhase1And2(n, false, FormattingContext.Other, null);
					Content.SetPseudoCacheValue("LoginNotice", n);
				}
			}
			if(!string.IsNullOrEmpty(n)) lblDescription.Text = FormattingPipeline.FormatWithPhase3(n, FormattingContext.Other, null);
		}

        protected void btnLogin_Click(object sender, EventArgs e) {
			UserInfo user = Users.TryLogin(txtUsername.Text, txtPassword.Text);
			if(user != null) {
				string loginKey = Users.ComputeLoginKey(user.Username, user.Email, user.DateTime);
				if(chkRemember.Checked) {
					LoginTools.SetLoginCookie(user.Username, loginKey,
						DateTime.Now.AddYears(1));
				}
				LoginTools.SetupSession(user);
				Log.LogEntry("User " + user.Username + " logged in", EntryType.General, Log.SystemUsername);
				LoginTools.TryRedirect(true);
			}
			else {
				lblResult.CssClass = "resulterror";
				lblResult.Text = Properties.Messages.WrongUsernamePassword;
			}
        }

        protected void btnLogout_Click(object sender, EventArgs e) {
			Logout();
			UrlTools.Redirect(UrlTools.BuildUrl("Login.aspx?Logout=1"));
        }

		/// <summary>
		/// Performs the logout.
		/// </summary>
		private void Logout() {
			Users.NotifyLogout(SessionFacade.CurrentUsername);
			LoginTools.SetLoginCookie("", "", DateTime.Now.AddYears(-1));
			Log.LogEntry("User " + SessionFacade.CurrentUsername + " logged out", EntryType.General, Log.SystemUsername);
			Session.Abandon();
		}

		protected void btnResetPassword_Click(object sender, EventArgs e) {
			// Find the user
			txtUsernameReset.Text = txtUsernameReset.Text.Trim();
			txtEmailReset.Text = txtEmailReset.Text.Trim();

			UserInfo user = null;
			if(txtUsernameReset.Text.Length > 0) {
				user = Users.FindUser(txtUsernameReset.Text);
			}
			else if(txtEmailReset.Text.Length > 0) {
				user = Users.FindUserByEmail(txtEmailReset.Text);
			}

			if(user != null) {
				Log.LogEntry("Password reset message sent for " + user.Username, EntryType.General, Log.SystemUsername);

				Users.SendPasswordResetMessage(user.Username, user.Email, user.DateTime);

				lblResult.CssClass = "resultok";
				lblResult.Text = Properties.Messages.AMessageWasSentCheckInbox;
				txtUsernameReset.Text = "";
				txtEmailReset.Text = "";
			}
		}

		protected void cvNewPasswords_ServerValidate(object sender, ServerValidateEventArgs e) {
			e.IsValid = txtNewPassword1.Text == txtNewPassword2.Text;
		}

		protected void btnSaveNewPassword_Click(object sender, EventArgs e) {
			if(!Page.IsValid) return;

			UserInfo user = LoadUserForPasswordReset();
			if(user != null) {
				Users.ChangePassword(user, txtNewPassword1.Text);

				lblResult.CssClass = "resultok";
				lblResult.Text = Properties.Messages.NewPasswordSavedPleaseLogin;
			}
		}

    }

}
