
using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Configuration;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using ScrewTurn.Wiki.PluginFramework;
using System.Globalization;

namespace ScrewTurn.Wiki {

	public partial class UserProfile : BasePage {

		private UserInfo currentUser;
		private string[] currentGroups;
		
		protected void Page_Load(object sender, EventArgs e) {
			Page.Title = Properties.Messages.ProfileTitle + " - " + Settings.WikiTitle;

			if(SessionFacade.LoginKey == null) {
				UrlTools.Redirect(UrlTools.BuildUrl("Login.aspx?Redirect=Profile.aspx"));
			}

			currentUser = SessionFacade.GetCurrentUser();
			currentGroups = SessionFacade.GetCurrentGroupNames();

			if(currentUser.Username == "admin") {
				// Admin only has language preferences, stored in a cookie
				UrlTools.Redirect("Language.aspx");
                return;
            }

            if(!Page.IsPostBack) {
				bool usersDataSupported = !currentUser.Provider.UsersDataReadOnly;
				bool accountDetailsSupported = !currentUser.Provider.UserAccountsReadOnly;

				pnlUserData.Visible = usersDataSupported;
				pnlAccount.Visible = accountDetailsSupported;
				pnlNoChanges.Visible = !usersDataSupported && !accountDetailsSupported;

				languageSelector.LoadLanguages();

				string name = string.IsNullOrEmpty(currentUser.DisplayName) ? currentUser.Username : currentUser.DisplayName;
				lblUsername.Text = name;
				txtDisplayName.Text = currentUser.DisplayName;
				txtEmail1.Text = currentUser.Email;
				lblGroupsList.Text = string.Join(", ", Array.ConvertAll(SessionFacade.GetCurrentGroups(), delegate(UserGroup g) { return g.Name; }));

				LoadNotificationsStatus();
				LoadLanguageAndTimezoneSettings();

				rxvDisplayName.ValidationExpression = Settings.DisplayNameRegex;
				rxvEmail1.ValidationExpression = Settings.EmailRegex;
				rxvPassword1.ValidationExpression = Settings.PasswordRegex;
			}
		}

		/// <summary>
		/// Loads the page/discussion change notification data.
		/// </summary>
		private void LoadNotificationsStatus() {
			lstPageChanges.Items.Clear();
			lstDiscussionMessages.Items.Clear();

			bool pageChanges, discussionMessages;
			Users.GetEmailNotification(currentUser, null as ScrewTurn.Wiki.PluginFramework.NamespaceInfo,
				out pageChanges, out discussionMessages);

			lstPageChanges.Items.Add(new ListItem("&lt;root&gt;", ""));
			lstPageChanges.Items[0].Selected = pageChanges;
			lstDiscussionMessages.Items.Add(new ListItem("&lt;root&gt;", ""));
			lstDiscussionMessages.Items[0].Selected = discussionMessages;

			foreach(ScrewTurn.Wiki.PluginFramework.NamespaceInfo ns in Pages.GetNamespaces()) {
				Users.GetEmailNotification(currentUser, ns, out pageChanges, out discussionMessages);

				if(AuthChecker.CheckActionForNamespace(ns, Actions.ForNamespaces.ReadPages, currentUser.Username, currentGroups)) {
					lstPageChanges.Items.Add(new ListItem(ns.Name, ns.Name));
					lstPageChanges.Items[lstPageChanges.Items.Count - 1].Selected = pageChanges;
				}

				if(AuthChecker.CheckActionForNamespace(ns, Actions.ForNamespaces.ReadDiscussion, currentUser.Username, currentGroups)) {
					lstDiscussionMessages.Items.Add(new ListItem(ns.Name, ns.Name));
					lstDiscussionMessages.Items[lstPageChanges.Items.Count - 1].Selected = discussionMessages;
				}
			}
		}

		/// <summary>
		/// Loads language and time zone settings.
		/// </summary>
		private void LoadLanguageAndTimezoneSettings() {
			// Load hard-stored settings
			// If not available, look for cookie
			// If not available, load defaults

			string culture = Preferences.LoadLanguageFromUserData();
			if(culture == null) culture = Preferences.LoadLanguageFromCookie();
			if(culture == null) culture = Settings.DefaultLanguage;

			int? tempTimezone = Preferences.LoadTimezoneFromUserData();
			if(!tempTimezone.HasValue) tempTimezone = Preferences.LoadTimezoneFromCookie();
			if(!tempTimezone.HasValue) tempTimezone = Settings.DefaultTimezone;

			languageSelector.SelectedLanguage = culture;
			languageSelector.SelectedTimezone = tempTimezone.ToString();
		}

		protected void btnSaveNotifications_Click(object sender, EventArgs e) {
			// Assume lists have the same number of items
			for(int i = 0; i < lstPageChanges.Items.Count; i++) {
				bool pageChanges = lstPageChanges.Items[i].Selected;
				bool discussionMessages = lstDiscussionMessages.Items[i].Selected;

				Users.SetEmailNotification(currentUser,
					Pages.FindNamespace(lstPageChanges.Items[i].Value), pageChanges, discussionMessages);
			}

			lblNotificationsResult.CssClass = "resultok";
			lblNotificationsResult.Text = Properties.Messages.NotificationsStatusSaved;
		}

		protected void btnSaveLanguage_Click(object sender, EventArgs e) {
			// Hard store settings
			// Delete cookie
			if(Preferences.SavePreferencesInUserData(languageSelector.SelectedLanguage,
				int.Parse(languageSelector.SelectedTimezone, CultureInfo.InvariantCulture))) {

				Preferences.DeletePreferencesCookie();
			}
			else {
				Preferences.SavePreferencesInCookie(languageSelector.SelectedLanguage,
					int.Parse(languageSelector.SelectedTimezone, CultureInfo.InvariantCulture));
			}
			lblLanguageResult.CssClass = "resultok";
			lblLanguageResult.Text = Properties.Messages.PreferencesSaved;
		}

		protected void btnSaveDisplayName_Click(object sender, EventArgs e) {
			lblSaveDisplayNameResult.CssClass = "";
			lblSaveDisplayNameResult.Text = "";

			Page.Validate("vgDisplayName");
			if(!Page.IsValid) return;

			if(Users.ModifyUser(currentUser, txtDisplayName.Text, null,
				currentUser.Email, currentUser.Active)) {
				lblSaveDisplayNameResult.CssClass = "resultok";
				lblSaveDisplayNameResult.Text = Properties.Messages.DisplayNameSaved;
			}
			else {
				lblSaveDisplayNameResult.CssClass = "resulterror";
				lblSaveDisplayNameResult.Text = Properties.Messages.CouldNotSaveDisplayName;
			}

			currentUser = Users.FindUser(currentUser.Username);
		}

		protected void btnSaveEmail_Click(object sender, EventArgs e) {
			lblSaveEmailResult.CssClass = "";
			lblSaveEmailResult.Text = "";

			Page.Validate("vgEmail");
			if(!Page.IsValid) return; 

			Users.ChangeEmail(currentUser, txtEmail1.Text);
			lblSaveEmailResult.CssClass = "resultok";
			lblSaveEmailResult.Text = Properties.Messages.EmailSaved;
			txtEmail2.Text = "";

			currentUser = Users.FindUser(currentUser.Username);
		}

		protected void btnSavePassword_Click(object sender, EventArgs e) {
			Page.Validate("vgPassword");
			if(!Page.IsValid) return;

			Users.ChangePassword(currentUser, txtPassword1.Text);
			lblSavePasswordResult.CssClass = "resultok";
			lblSavePasswordResult.Text = Properties.Messages.PasswordSaved;
			txtOldPassword.Text = "";
			txtPassword1.Text = "";
			txtPassword2.Text = "";

			currentUser = Users.FindUser(currentUser.Username);
		}

		protected void btnDeleteAccount_Click(object sender, EventArgs e) {
			btnConfirm.Enabled = true;
			btnDeleteAccount.Enabled = false;
		}

		protected void btnConfirm_Click(object sender, EventArgs e) {
			Log.LogEntry("Account deletion requested", EntryType.General, currentUser.Username);
			UserInfo user = Users.FindUser(currentUser.Username);
			Users.RemoveUser(user);
			Session.Abandon();
			UrlTools.RedirectHome();
		}

		#region Custom Validators
		
		protected void cvEmail1_ServerValidate(object source, ServerValidateEventArgs args) {
			args.IsValid = (string.Compare(txtEmail1.Text, txtEmail2.Text, true) == 0);
		}
		
		protected void cvEmail2_ServerValidate(object source, ServerValidateEventArgs args) {
			args.IsValid = true;
		}
		
		protected void cvPassword1_ServerValidate(object source, ServerValidateEventArgs args) {
			args.IsValid = txtPassword1.Text.Equals(txtPassword2.Text);
		}
		
		protected void cvPassword2_ServerValidate(object source, ServerValidateEventArgs args) {
			args.IsValid = true;
		}
		
		protected void cvOldPassword_ServerValidate(object source, ServerValidateEventArgs args) {
			UserInfo user = SessionFacade.GetCurrentUser();
			args.IsValid = user.Provider.TestAccount(user, txtOldPassword.Text);
		}
		
		#endregion
	}

}
