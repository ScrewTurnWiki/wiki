
using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using ScrewTurn.Wiki.PluginFramework;
using System.Text;

namespace ScrewTurn.Wiki {

	public partial class AdminUsers : BasePage {

		/// <summary>
		/// The numer of items in a page.
		/// </summary>
		public int PageSize = 50;

		private IList<UserInfo> currentUsers = null;

		private int rangeBegin = 0;
		private int rangeEnd = 49;
		private int selectedPage = 0;

		protected void Page_Load(object sender, EventArgs e) {
			AdminMaster.RedirectToLoginIfNeeded();
			PageSize = Settings.ListSize;
			rangeEnd = PageSize - 1;

			if(!AdminMaster.CanManageUsers(SessionFacade.GetCurrentUsername(), SessionFacade.GetCurrentGroupNames())) UrlTools.Redirect("AccessDenied.aspx");
			aclActionsSelector.Visible = AdminMaster.CanManagePermissions(SessionFacade.GetCurrentUsername(), SessionFacade.GetCurrentGroupNames());

			revUsername.ValidationExpression = Settings.UsernameRegex;
			revDisplayName.ValidationExpression = Settings.DisplayNameRegex;
			revPassword1.ValidationExpression = Settings.PasswordRegex;
			revEmail.ValidationExpression = Settings.EmailRegex;

			if(!Page.IsPostBack) {
				ResetUserList();

				RefreshList();

				providerSelector.Reload();
				btnNewUser.Enabled = providerSelector.HasProviders;
			}

			if(Page.IsPostBack) {
				// Preserve password value (a bit insecure but much more usable)
				txtPassword1.Attributes.Add("value", txtPassword1.Text);
				txtPassword2.Attributes.Add("value", txtPassword2.Text);
			}
		}

		/// <summary>
		/// Resets the user list.
		/// </summary>
		private void ResetUserList() {
			currentUsers = GetUsers();
			pageSelector.ItemCount = currentUsers.Count;
			pageSelector.SelectPage(0);
		}

		protected void chkFilter_CheckedChanged(object sender, EventArgs e) {
			currentUsers = GetUsers();
			pageSelector.ItemCount = currentUsers.Count;
			pageSelector.SelectPage(0);

			RefreshList();
		}

		protected void btnFilter_Click(object sender, EventArgs e) {
			currentUsers = GetUsers();
			pageSelector.ItemCount = currentUsers.Count;
			pageSelector.SelectPage(0);

			RefreshList();
		}

		protected void pageSelector_SelectedPageChanged(object sender, SelectedPageChangedEventArgs e) {
			rangeBegin = e.SelectedPage * PageSize;
			rangeEnd = rangeBegin + e.ItemCount - 1;
			selectedPage = e.SelectedPage;

			RefreshList();
		}

		/// <summary>
		/// Gets the users.
		/// </summary>
		/// <returns>The users.</returns>
		private IList<UserInfo> GetUsers() {
			List<UserInfo> allUsers = Users.GetUsers();

			// Apply filter
			List<UserInfo> result = new List<UserInfo>(allUsers.Count);

			foreach(UserInfo user in allUsers) {
				if(user.Active && chkActive.Checked) {
					if(FilterUsername(user)) result.Add(user);
				}
				else if(!user.Active && chkInactive.Checked) {
					if(FilterUsername(user)) result.Add(user);
				}
			}

			result.Sort(new UsernameComparer());

			return result;
		}

		/// <summary>
		/// Refreshes the users list.
		/// </summary>
		private void RefreshList() {
			rangeBegin = pageSelector.SelectedPage * PageSize;
			rangeEnd = rangeBegin + pageSelector.SelectedPageSize - 1;
			selectedPage = pageSelector.SelectedPage;

			txtCurrentUsername.Value = "";
			ResetEditor();
			rptAccounts.DataBind();
		}

		private bool FilterUsername(UserInfo user) {
			if(txtFilter.Text.Length == 0) return true;
			else return user.Username.ToLower(System.Globalization.CultureInfo.CurrentCulture).Contains(txtFilter.Text.ToLower(System.Globalization.CultureInfo.CurrentCulture));
		}

		protected void rptAccounts_DataBinding(object sender, EventArgs e) {
			if(currentUsers == null) currentUsers = GetUsers();

			List<UserRow> selectedRows = new List<UserRow>(PageSize);

			for(int i = rangeBegin; i <= rangeEnd; i++) {
				selectedRows.Add(new UserRow(currentUsers[i], Users.GetUserGroupsForUser(currentUsers[i]),
					currentUsers[i].Username == txtCurrentUsername.Value));
			}

			rptAccounts.DataSource = selectedRows;
		}

		protected void rptAccounts_ItemCommand(object sender, RepeaterCommandEventArgs e) {
			if(e.CommandName == "Select") {
				txtCurrentUsername.Value = e.CommandArgument as string;
				//rptAccounts.DataBind(); Not needed because the list is hidden on select

				UserInfo user = Users.FindUser(txtCurrentUsername.Value);

				txtUsername.Text = user.Username;
				txtUsername.Enabled = false;
				txtDisplayName.Text = user.DisplayName;
				txtEmail.Text = user.Email;
				chkSetActive.Checked = user.Active;
				providerSelector.SelectedProvider = user.Provider.GetType().FullName;
				providerSelector.Enabled = false;
				btnCreate.Visible = false;
				btnSave.Visible = true;
				btnDelete.Visible = user.Username != SessionFacade.CurrentUsername;
				rfvPassword1.Enabled = false;
				cvUsername.Enabled = false;
				lblPasswordInfo.Visible = true;

				pnlEditAccount.Visible = true;
				pnlList.Visible = false;
				PopulateGroups();

				// Select user's groups
				List<UserGroup> groups = Users.GetUserGroupsForUser(user);
				foreach(ListItem item in lstGroups.Items) {
					if(groups.Find(delegate(UserGroup g) { return g.Name == item.Value; }) != null) {
						item.Selected = true;
					}
				}

				// Select user's global permissions
				aclActionsSelector.GrantedActions =
					AuthReader.RetrieveGrantsForGlobals(user);
				aclActionsSelector.DeniedActions =
					AuthReader.RetrieveDenialsForGlobals(user);

				// Enable/disable interface sections based on provider read-only settings
				lstGroups.Enabled = !user.Provider.GroupMembershipReadOnly;
				pnlAccountDetails.Enabled = !user.Provider.UserAccountsReadOnly;
				btnDelete.Enabled = !user.Provider.UserAccountsReadOnly;

				lblResult.CssClass = "";
				lblResult.Text = "";
			}
		}

		/// <summary>
		/// Resets the account editor.
		/// </summary>
		private void ResetEditor() {
			txtUsername.Text = "";
			txtUsername.Enabled = true;
			txtDisplayName.Text = "";
			txtEmail.Text = "";
			chkSetActive.Checked = true;
			providerSelector.Enabled = true;
			providerSelector.Reload();
			lstGroups.Enabled = true;
			pnlAccountDetails.Enabled = true;

			aclActionsSelector.GrantedActions = new string[0];
			aclActionsSelector.DeniedActions = new string[0];

			foreach(ListItem item in lstGroups.Items) {
				item.Selected = false;
			}

			btnCreate.Visible = true;
			btnSave.Visible = false;
			btnDelete.Visible = false;
			rfvPassword1.Enabled = true;
			cvUsername.Enabled = true;
			lblPasswordInfo.Visible = false;
			lblResult.Text = "";
		}

		protected void providerSelector_SelectedProviderChanged(object sender, EventArgs e) {
			PopulateGroups();
		}

		/// <summary>
		/// Populates the groups list according to the currently selected provider.
		/// </summary>
		private void PopulateGroups() {
			List<UserGroup> groups = Users.GetUserGroups(Collectors.UsersProviderCollector.GetProvider(providerSelector.SelectedProvider));

			lstGroups.Items.Clear();
			foreach(UserGroup group in groups) {
				ListItem item = new ListItem(group.Name, group.Name);
				lstGroups.Items.Add(item);
			}
		}

		protected void cvUsername_ServerValidate(object sender, ServerValidateEventArgs e) {
			e.IsValid = Users.FindUser(txtUsername.Text) == null;
		}

		protected void cvPassword2_ServerValidate(object sender, ServerValidateEventArgs e) {
			e.IsValid = txtPassword1.Text == txtPassword2.Text;
		}

		protected void btnBulkDelete_Click(object sender, EventArgs e) {
			Log.LogEntry("Bulk account deletion requested", EntryType.General, SessionFacade.CurrentUsername);

			DateTime now = DateTime.Now;
			List<UserInfo> allUsers = Users.GetUsers();
			int count = 0;

			for(int i = 0; i < allUsers.Count; i++) {
				if(!allUsers[i].Active && !allUsers[i].Provider.UserAccountsReadOnly && (now - allUsers[i].DateTime).TotalDays >= 31) {
					RemoveAllAclEntries(allUsers[i]);
					RemoveGroupMembership(allUsers[i]);
					Users.RemoveUser(allUsers[i]);
					count++;
				}
			}

			Log.LogEntry("Bulk account deletion completed - " + count.ToString() + " accounts deleted", EntryType.General, SessionFacade.CurrentUsername);

			ResetUserList();
			RefreshList();
			lblBulkDeleteResult.CssClass = "resultok";
			lblBulkDeleteResult.Text = Properties.Messages.NAccountsDeleted.Replace("$", count.ToString());
		}

		protected void btnCreate_Click(object sender, EventArgs e) {
			if(!Page.IsValid) return;

			txtUsername.Text = txtUsername.Text.Trim();

			lblResult.CssClass = "";
			lblResult.Text = "";

			Log.LogEntry("User creation requested for " + txtUsername.Text, EntryType.General, SessionFacade.CurrentUsername);

			// Add the new user, set its global permissions, set its membership
			bool done = Users.AddUser(txtUsername.Text, txtDisplayName.Text, txtPassword1.Text, txtEmail.Text,
				chkSetActive.Checked,
				Collectors.UsersProviderCollector.GetProvider(providerSelector.SelectedProvider));

			UserInfo currentUser = null;
			if(done) {
				currentUser = Users.FindUser(txtUsername.Text);

				// Wipe old data, if any
				RemoveAllAclEntries(currentUser);

				done = AddAclEntries(currentUser, aclActionsSelector.GrantedActions, aclActionsSelector.DeniedActions);

				if(done) {
					done = SetGroupMembership(currentUser, GetSelectedGroups());

					if(done) {
						ResetUserList();

						RefreshList();
						lblResult.CssClass = "resultok";
						lblResult.Text = Properties.Messages.UserCreated;
						ReturnToList();
					}
					else {
						lblResult.CssClass = "resulterror";
						lblResult.Text = Properties.Messages.UserCreatedCouldNotStoreGroupMembership;
					}
				}
				else {
					lblResult.CssClass = "resulterror";
					lblResult.Text = Properties.Messages.UserCreatedCouldNotStorePermissions;
				}
			}
			else {
				lblResult.CssClass = "resulterror";
				lblResult.Text = Properties.Messages.CouldNotCreateUser;
			}
		}

		protected void btnSave_Click(object sender, EventArgs e) {
			if(!Page.IsValid) return;

			// Perform proper actions based on provider read-only settings
			// 1. If possible, modify user
			// 2. Update ACLs
			// 3. If possible, update group membership

			lblResult.CssClass = "";
			lblResult.Text = "";

			Log.LogEntry("User update requested for " + txtCurrentUsername.Value, EntryType.General, SessionFacade.CurrentUsername);

			UserInfo currentUser = Users.FindUser(txtCurrentUsername.Value);

			bool done = true;

			if(!currentUser.Provider.UserAccountsReadOnly) {
				done = Users.ModifyUser(currentUser, txtDisplayName.Text, txtPassword1.Text, txtEmail.Text, chkSetActive.Checked);
			}

			if(!done) {
				lblResult.CssClass = "resulterror";
				lblResult.Text = Properties.Messages.CouldNotUpdateUser;
				return;
			}

			done = RemoveAllAclEntries(currentUser);
			if(done) {
				done = AddAclEntries(currentUser, aclActionsSelector.GrantedActions, aclActionsSelector.DeniedActions);

				if(!done) {
					lblResult.CssClass = "resulterror";
					lblResult.Text = Properties.Messages.UserUpdatedCouldNotStoreNewPermissions;
				}
			}
			else {
				lblResult.CssClass = "resulterror";
				lblResult.Text = Properties.Messages.UserUpdatedCouldNotDeleteOldPermissions;
				return;
			}

			if(!currentUser.Provider.GroupMembershipReadOnly) {
				// This overwrites old membership data
				done = SetGroupMembership(currentUser, GetSelectedGroups());

				if(done) {
					RefreshList();
					lblResult.CssClass = "resultok";
					lblResult.Text = Properties.Messages.UserUpdated;
					ReturnToList();
				}
				else {
					lblResult.CssClass = "resulterror";
					lblResult.Text = Properties.Messages.UserUpdatedCouldNotStoreGroupMembership;
				}
			}
		}

		protected void btnDelete_Click(object sender, EventArgs e) {
			lblResult.Text = "";
			lblResult.CssClass = "";

			Log.LogEntry("User deletion requested for " + txtCurrentUsername.Value, EntryType.General, SessionFacade.CurrentUsername);

			UserInfo currentUser = Users.FindUser(txtCurrentUsername.Value);

			if(currentUser.Provider.UserAccountsReadOnly) return;

			// Remove global permissions, remove group membership, remove user
			bool done = RemoveAllAclEntries(currentUser);
			if(done) {
				done = RemoveGroupMembership(currentUser);

				if(done) {
					done = Users.RemoveUser(currentUser);

					if(done) {
						ResetUserList();

						RefreshList();
						lblResult.Text = Properties.Messages.UserDeleted;
						lblResult.CssClass = "resultok";
						ReturnToList();
					}
					else {
						lblResult.CssClass = "resulterror";
						lblResult.Text = Properties.Messages.PermissionsAndGroupMembershipDeletedCouldNotDeleteUser;
					}
				}
				else {
					lblResult.CssClass = "resulterror";
					lblResult.Text = Properties.Messages.PermissionsDeletedCouldNotDeleteUser;
				}
			}
			else {
				lblResult.CssClass = "resulterror";
				lblResult.Text = Properties.Messages.CouldNotDeletePermissions;
			}
		}

		protected void btnCancel_Click(object sender, EventArgs e) {
			rangeBegin = pageSelector.SelectedPage * PageSize;
			rangeEnd = rangeBegin + pageSelector.SelectedPageSize - 1;
			selectedPage = pageSelector.SelectedPage;

			RefreshList();
			ReturnToList();
		}

		protected void btnNewUser_Click(object sender, EventArgs e) {
			pnlList.Visible = false;
			pnlEditAccount.Visible = true;
			PopulateGroups();

			lblResult.Text = "";
			lblResult.CssClass = "";
		}

		/// <summary>
		/// Gets the currently selected groups.
		/// </summary>
		/// <returns></returns>
		private string[] GetSelectedGroups() {
			List<string> selectedGroups = new List<string>(5);
			foreach(ListItem item in lstGroups.Items) {
				if(item.Selected) selectedGroups.Add(item.Value);
			}
			return selectedGroups.ToArray();
		}

		/// <summary>
		/// Removes all the ACL entries for a user.
		/// </summary>
		/// <param name="user">The user.</param>
		/// <returns><c>true</c> if the operation succeeded, <c>false</c> otherwise.</returns>
		private bool RemoveAllAclEntries(UserInfo user) {
			return AuthWriter.RemoveEntriesForGlobals(user);
		}

		/// <summary>
		/// Adds some ACL entries for a user.
		/// </summary>
		/// <param name="user">The user.</param>
		/// <param name="grants">The granted actions.</param>
		/// <param name="denials">The denied actions.</param>
		/// <returns><c>true</c> if the operation succeeded, <c>false</c> otherwise.</returns>
		private bool AddAclEntries(UserInfo user, string[] grants, string[] denials) {
			foreach(string action in grants) {
				bool done = AuthWriter.SetPermissionForGlobals(AuthStatus.Grant, action, user);
				if(!done) return false;
			}

			foreach(string action in denials) {
				bool done = AuthWriter.SetPermissionForGlobals(AuthStatus.Deny, action, user);
				if(!done) return false;
			}

			return true;
		}

		/// <summary>
		/// Removes all the group membership data for a user.
		/// </summary>
		/// <param name="user">The user.</param>
		/// <returns><c>true</c> if the operation succeeded, <c>false</c> otherwise.</returns>
		private bool RemoveGroupMembership(UserInfo user) {
			return Users.SetUserMembership(user, new string[0]);
		}

		/// <summary>
		/// Sets the group membership data for a user, overwriting previous membership.
		/// </summary>
		/// <param name="user">The user.</param>
		/// <param name="groups">The groups the user should be member of.</param>
		/// <returns><c>true</c> if the operation succeded, <c>false</c> otherwise.</returns>
		private bool SetGroupMembership(UserInfo user, string[] groups) {
			return Users.SetUserMembership(user, groups);
		}

		/// <summary>
		/// Returns to the accounts list.
		/// </summary>
		private void ReturnToList() {
			pnlEditAccount.Visible = false;
			pnlList.Visible = true;
		}

	}

	/// <summary>
	/// Represents a User for display purposes.
	/// </summary>
	public class UserRow {

		private string username, displayName, email, memberOf, regDateTime, provider, additionalClass;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:UserRow" /> class.
		/// </summary>
		/// <param name="user">The original user.</param>
		/// <param name="groups">The groups the user is member of.</param>
		/// <param name="selected">A value indicating whether the user is selected.</param>
		public UserRow(UserInfo user, List<UserGroup> groups, bool selected) {
			username = user.Username;
			displayName = Users.GetDisplayName(user);
			email = user.Email;

			StringBuilder sb = new StringBuilder(50);
			for(int i = 0; i < groups.Count; i++) {
				sb.Append(groups[i].Name);
				if(i != groups.Count - 1) sb.Append(", ");
			}
			memberOf = sb.ToString();

			regDateTime = user.DateTime.ToString(Settings.DateTimeFormat);
			provider = user.Provider.Information.Name;
			additionalClass = (selected ? " selected" : "") + (!user.Active ? " inactive" : "");
		}

		/// <summary>
		/// Gets the username.
		/// </summary>
		public string Username {
			get { return username; }
		}

		/// <summary>
		/// Gets the display name.
		/// </summary>
		public string DisplayName {
			get { return displayName; }
		}

		/// <summary>
		/// Gets the email.
		/// </summary>
		public string Email {
			get { return email; }
		}

		/// <summary>
		/// Gets the user membership.
		/// </summary>
		public string MemberOf {
			get { return memberOf; }
		}

		/// <summary>
		/// Gets the registration date/time, formatted.
		/// </summary>
		public string RegDateTime {
			get { return regDateTime; }
		}

		/// <summary>
		/// Gets the provider name.
		/// </summary>
		public string Provider {
			get { return provider; }
		}

		/// <summary>
		/// Gets the additional CSS classes to apply.
		/// </summary>
		public string AdditionalClass {
			get { return additionalClass; }
		}

	}

}
