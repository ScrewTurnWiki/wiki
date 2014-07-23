
using System;
using System.Collections.Generic;
using System.Text;
using ScrewTurn.Wiki.PluginFramework;
using System.Web;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Manages all the User Accounts data.
	/// </summary>
	public static class Users {

		private static UserInfo adminAccount = null;
		private static UserInfo anonAccount = null;

		/// <summary>
		/// Gets the built-in administrator account.
		/// </summary>
		/// <returns>The account.</returns>
		public static UserInfo GetAdministratorAccount() {
			if(adminAccount == null) {
				adminAccount = new UserInfo("admin", "Administrator", Settings.ContactEmail, true, DateTime.MinValue, null);
				adminAccount.Groups = new[] { Settings.AdministratorsGroup };
			}

			return adminAccount;
		}

		/// <summary>
		/// Gets the fake anonymous account.
		/// </summary>
		/// <returns>The account.</returns>
		public static UserInfo GetAnonymousAccount() {
			if(anonAccount == null) {
				anonAccount = new UserInfo(SessionFacade.AnonymousUsername, null, null, false, DateTime.MinValue, null);
				anonAccount.Groups = new[] { Settings.AnonymousGroup };
			}

			return anonAccount;
		}

		/// <summary>
		/// The user data key pointing to page changes notification entries.
		/// </summary>
		private const string PageChangesKey = "PageChanges";

		/// <summary>
		/// The user data key pointing to discussion messages notification entries.
		/// </summary>
		private const string DiscussionMessagesKey = "DiscussionMessages";

		/// <summary>
		/// The user data key pointing to page changes notification entries for whole namespace.
		/// </summary>
		private const string NamespacePageChangesKey = "NamespacePageChanges";

		/// <summary>
		/// The user data key pointing to discussion messages notification entries for whole namespaces.
		/// </summary>
		private const string NamespaceDiscussionMessagesKey = "NamespaceDiscussionMessages";

		/// <summary>
		/// Gets all the Users that the providers declare to manage.
		/// </summary>
		/// <returns>The users, sorted by username.</returns>
		public static List<UserInfo> GetUsers() {
			List<UserInfo> allUsers = new List<UserInfo>(1000);

			// Retrieve all the users from the Users Providers
			int count = 0;
			foreach(IUsersStorageProviderV30 provider in Collectors.UsersProviderCollector.AllProviders) {
				count++;
				allUsers.AddRange(provider.GetUsers());
			}

			if(count > 1) {
				allUsers.Sort(new UsernameComparer());
			}

			return allUsers;
		}

		/// <summary>
		/// Finds a user.
		/// </summary>
		/// <param name="username">The username.</param>
		/// <returns>The user, or <c>null</c>.</returns>
		public static UserInfo FindUser(string username) {
			if(string.IsNullOrEmpty(username)) return null;

			if(username == "admin") return GetAdministratorAccount();

			// Try default provider first
			IUsersStorageProviderV30 defaultProvider = Collectors.UsersProviderCollector.GetProvider(Settings.DefaultUsersProvider);
			UserInfo temp = defaultProvider.GetUser(username);
			if(temp != null) return temp;

			// The try other providers
			temp = null;
			IUsersStorageProviderV30[] providers = Collectors.UsersProviderCollector.AllProviders;
			foreach(IUsersStorageProviderV30 p in providers) {
				IUsersStorageProviderV30 extProv = p as IUsersStorageProviderV30;
				if(extProv != null && extProv != defaultProvider) {
					temp = extProv.GetUser(username);
					if(temp != null) return temp;
				}
			}
			return null;
		}

		/// <summary>
		/// Finds a user by email.
		/// </summary>
		/// <param name="email">The email address.</param>
		/// <returns>The user, or <c>null</c>.</returns>
		public static UserInfo FindUserByEmail(string email) {
			// Try default provider first
			IUsersStorageProviderV30 defaultProvider = Collectors.UsersProviderCollector.GetProvider(Settings.DefaultUsersProvider);
			UserInfo temp = defaultProvider.GetUserByEmail(email);
			if(temp != null) return temp;

			// The try other providers
			temp = null;
			IUsersStorageProviderV30[] providers = Collectors.UsersProviderCollector.AllProviders;
			foreach(IUsersStorageProviderV30 p in providers) {
				IUsersStorageProviderV30 extProv = p as IUsersStorageProviderV30;
				if(extProv != null && extProv != defaultProvider) {
					temp = extProv.GetUserByEmail(email);
					if(temp != null) return temp;
				}
			}
			return null;
		}

		/// <summary>
		/// Gets user data.
		/// </summary>
		/// <param name="user">The user.</param>
		/// <param name="key">The data key.</param>
		/// <returns>The data, or <c>null</c> if either the user or the key is not found.</returns>
		public static string GetUserData(UserInfo user, string key) {
			if(user == null) return null;
			if(string.IsNullOrEmpty(key)) return null;
			if(user.Username == "admin") return null;

			return user.Provider.RetrieveUserData(user, key);
		}

		/// <summary>
		/// Sets user data.
		/// </summary>
		/// <param name="user">The user.</param>
		/// <param name="key">The data key.</param>
		/// <param name="data">The data value.</param>
		/// <returns><c>true</c> if the data is stored, <c>false</c> otherwise.</returns>
		public static bool SetUserData(UserInfo user, string key, string data) {
			if(user == null) return false;
			if(string.IsNullOrEmpty(key)) return false;
			if(user.Username == "admin") return false;
			if(user.Provider.UsersDataReadOnly) return false;

			bool done = user.Provider.StoreUserData(user, key, data);

			if(done) Log.LogEntry("User data stored for " + user.Username, EntryType.General, Log.SystemUsername);
			else Log.LogEntry("Could not store user data for " + user.Username, EntryType.Error, Log.SystemUsername);

			return done;
		}

		/// <summary>
		/// Adds a new User.
		/// </summary>
		/// <param name="username">The Username.</param>
		/// <param name="displayName">The display name.</param>
		/// <param name="password">The Password (plain text).</param>
		/// <param name="email">The Email address.</param>
		/// <param name="active">A value specifying whether or not the account is active.</param>
		/// <param name="provider">The Provider. If null, the default provider is used.</param>
		/// <returns>True if the User has been created successfully.</returns>
		public static bool AddUser(string username, string displayName, string password, string email, bool active, IUsersStorageProviderV30 provider) {
			if(FindUser(username) != null) return false;
			if(provider == null) provider = Collectors.UsersProviderCollector.GetProvider(Settings.DefaultUsersProvider);

			if(provider.UserAccountsReadOnly) return false;

			UserInfo u = provider.AddUser(username, displayName, password, email, active, DateTime.Now);
			if(u == null) {
				Log.LogEntry("User creation failed for " + username, EntryType.Error, Log.SystemUsername);
				return false;
			}
			else {
				Log.LogEntry("User " + username + " created", EntryType.General, Log.SystemUsername);
				Host.Instance.OnUserAccountActivity(u, UserAccountActivity.AccountAdded);
				return true;
			}
		}

		/// <summary>
		/// Updates a new User.
		/// </summary>
		/// <param name="user">The user to modify.</param>
		/// <param name="displayName">The display name.</param>
		/// <param name="password">The Password (plain text, <c>null</c> or empty for no change).</param>
		/// <param name="email">The Email address.</param>
		/// <param name="active">A value specifying whether or not the account is active.</param>
		/// <returns>True if the User has been created successfully.</returns>
		public static bool ModifyUser(UserInfo user, string displayName, string password, string email, bool active) {
			if(user.Provider.UserAccountsReadOnly) return false;

			UserInfo newUser = user.Provider.ModifyUser(user, displayName, password, email, active);

			if(newUser != null) {
				Log.LogEntry("User " + user.Username + " updated", EntryType.General, Log.SystemUsername);
				Host.Instance.OnUserAccountActivity(newUser, UserAccountActivity.AccountModified);
				if(user.Active != newUser.Active) {
					Host.Instance.OnUserAccountActivity(newUser, newUser.Active ? UserAccountActivity.AccountActivated : UserAccountActivity.AccountDeactivated);
				}
				return true;
			}
			else {
				Log.LogEntry("User update failed for " + user.Username, EntryType.Error, Log.SystemUsername);
				return false;
			}
		}

		/// <summary>
		/// Removes a User.
		/// </summary>
		/// <param name="user">The User to remove.</param>
		/// <returns>True if the User has been removed successfully.</returns>
		public static bool RemoveUser(UserInfo user) {
			if(user.Provider.UserAccountsReadOnly) return false;

			RemovePermissions(user);
			
			bool done = user.Provider.RemoveUser(user);
			if(done) {
				Log.LogEntry("User " + user.Username + " removed", EntryType.General, Log.SystemUsername);
				Host.Instance.OnUserAccountActivity(user, UserAccountActivity.AccountRemoved);
				return true;
			}
			else {
				Log.LogEntry("User deletion failed for " + user.Username, EntryType.Error, Log.SystemUsername);
				return false;
			}
		}

		/// <summary>
		/// Lists all directories in a provider, including the root.
		/// </summary>
		/// <param name="provider">The provider.</param>
		/// <returns>The directories.</returns>
		private static List<string> ListDirectories(IFilesStorageProviderV30 provider) {
			List<string> directories = new List<string>(50);
			directories.Add("/");

			ListDirectoriesRecursive(provider, "/", directories);

			return directories;
		}

		private static void ListDirectoriesRecursive(IFilesStorageProviderV30 provider, string current, List<string> output) {
			foreach(string dir in provider.ListDirectories(current)) {
				output.Add(dir);
				ListDirectoriesRecursive(provider, dir, output);
			}
		}

		/// <summary>
		/// Removes all permissions for a user.
		/// </summary>
		/// <param name="user">The user.</param>
		private static void RemovePermissions(UserInfo user) {
			foreach(IFilesStorageProviderV30 prov in Collectors.FilesProviderCollector.AllProviders) {
				foreach(string dir in ListDirectories(prov)) {
					AuthWriter.RemoveEntriesForDirectory(user, prov, dir);
				}
			}

			AuthWriter.RemoveEntriesForGlobals(user);

			AuthWriter.RemoveEntriesForNamespace(user, null);
			foreach(IPagesStorageProviderV30 prov in Collectors.PagesProviderCollector.AllProviders) {
				foreach(PageInfo page in prov.GetPages(null)) {
					AuthWriter.RemoveEntriesForPage(user, page);
				}

				foreach(NamespaceInfo nspace in prov.GetNamespaces()) {
					AuthWriter.RemoveEntriesForNamespace(user, nspace);

					foreach(PageInfo page in prov.GetPages(nspace)) {
						AuthWriter.RemoveEntriesForPage(user, page);
					}
				}
			}
		}

		/// <summary>
		/// Removes all permissions for a group.
		/// </summary>
		/// <param name="group">The group.</param>
		private static void RemovePermissions(UserGroup group) {
			foreach(IFilesStorageProviderV30 prov in Collectors.FilesProviderCollector.AllProviders) {
				foreach(string dir in ListDirectories(prov)) {
					AuthWriter.RemoveEntriesForDirectory(group, prov, dir);
				}
			}

			AuthWriter.RemoveEntriesForGlobals(group);

			AuthWriter.RemoveEntriesForNamespace(group, null);
			foreach(IPagesStorageProviderV30 prov in Collectors.PagesProviderCollector.AllProviders) {
				foreach(PageInfo page in prov.GetPages(null)) {
					AuthWriter.RemoveEntriesForPage(group, page);
				}

				foreach(NamespaceInfo nspace in prov.GetNamespaces()) {
					AuthWriter.RemoveEntriesForNamespace(group, nspace);

					foreach(PageInfo page in prov.GetPages(nspace)) {
						AuthWriter.RemoveEntriesForPage(group, page);
					}
				}
			}
		}

		/// <summary>
		/// Changes the Password of a User.
		/// </summary>
		/// <param name="user">The User to change the password of.</param>
		/// <param name="newPassword">The new Password (plain text).</param>
		/// <returns><c>true</c> if the Password has been changed successfully, <c>false</c> otherwise.</returns>
		public static bool ChangePassword(UserInfo user, string newPassword) {
			return ModifyUser(user, user.DisplayName, newPassword, user.Email, user.Active);
		}

		/// <summary>
		/// Sends the password reset message to a user.
		/// </summary>
		/// <param name="username">The username.</param>
		/// <param name="email">The email.</param>
		/// <param name="dateTime">The user registration date/time.</param>
		public static void SendPasswordResetMessage(string username, string email, DateTime dateTime) {
			string mainLink = Settings.MainUrl + "Login.aspx?ResetCode=" + Tools.ComputeSecurityHash(username, email, dateTime) + "&Username=" + Tools.UrlEncode(username);
			string body = Settings.Provider.GetMetaDataItem(MetaDataItem.PasswordResetProcedureMessage, null).Replace("##USERNAME##",
				username).Replace("##LINK##", mainLink).Replace("##WIKITITLE##",
				Settings.WikiTitle).Replace("##EMAILADDRESS##", Settings.ContactEmail);

			EmailTools.AsyncSendEmail(email, Settings.SenderEmail,
				Settings.WikiTitle + " - " + Exchanger.ResourceExchanger.GetResource("ResetPassword"), body, false);
		}

		/// <summary>
		/// Changes the Email address of a User.
		/// </summary>
		/// <param name="user">The User to change the Email address of.</param>
		/// <param name="newEmail">The new Email address.</param>
		/// <returns><c>true</c> if the Email address has been changed successfully, <c>false</c> otherwise.</returns>
		public static bool ChangeEmail(UserInfo user, string newEmail) {
			return ModifyUser(user, user.DisplayName, null, newEmail, user.Active);
		}

		/// <summary>
		/// Sets the Active/Inactive status of a User.
		/// </summary>
		/// <param name="user">The User.</param>
		/// <param name="active">The status.</param>
		/// <returns>True if the User's status has been changed successfully.</returns>
		public static bool SetActivationStatus(UserInfo user, bool active) {
			return ModifyUser(user, user.DisplayName, null, user.Email, active);
		}

		/// <summary>
		/// Gets all the user groups.
		/// </summary>
		/// <returns>All the user groups, sorted by name.</returns>
		public static List<UserGroup> GetUserGroups() {
			List<UserGroup> result = new List<UserGroup>(50);

			int count = 0;
			foreach(IUsersStorageProviderV30 prov in Collectors.UsersProviderCollector.AllProviders) {
				count++;
				result.AddRange(prov.GetUserGroups());
			}

			if(count > 1) {
				result.Sort(new UserGroupComparer());
			}

			return result;
		}

		/// <summary>
		/// Gets all the user groups in a provider.
		/// </summary>
		/// <param name="provider">The provider.</param>
		/// <returns>The user groups, sorted by name.</returns>
		public static List<UserGroup> GetUserGroups(IUsersStorageProviderV30 provider) {
			return new List<UserGroup>(provider.GetUserGroups());
		}

		/// <summary>
		/// Gets all the user groups a user is member of.
		/// </summary>
		/// <param name="user">The user.</param>
		/// <returns>All the user groups the user is member of, sorted by name.</returns>
		public static List<UserGroup> GetUserGroupsForUser(UserInfo user) {
			UserGroup[] allGroups = user.Provider.GetUserGroups();

			List<UserGroup> result = new List<UserGroup>(allGroups.Length);

			StringComparer comp = StringComparer.OrdinalIgnoreCase;

			foreach(UserGroup group in allGroups) {
				if(Array.Find(user.Groups, delegate(string g) {
					return comp.Compare(g, group.Name) == 0;
				}) != null) {
					result.Add(group);
				}
			}

			return result;
		}

		/// <summary>
		/// Finds a user group.
		/// </summary>
		/// <param name="name">The name of the user group to find.</param>
		/// <returns>The <see cref="T:UserGroup" /> object or <c>null</c> if no data is found.</returns>
		public static UserGroup FindUserGroup(string name) {
			List<UserGroup> allGroups = GetUserGroups();
			int index = allGroups.BinarySearch(new UserGroup(name, "", null), new UserGroupComparer());

			if(index < 0) return null;
			else return allGroups[index];
		}

		/// <summary>
		/// Adds a new user group to a specific provider.
		/// </summary>
		/// <param name="name">The name of the group.</param>
		/// <param name="description">The description of the group.</param>
		/// <param name="provider">The target provider.</param>
		/// <returns><c>true</c> if the groups is added, <c>false</c> otherwise.</returns>
		public static bool AddUserGroup(string name, string description, IUsersStorageProviderV30 provider) {
			if(provider == null) provider = Collectors.UsersProviderCollector.GetProvider(Settings.DefaultUsersProvider);

			if(provider.UserGroupsReadOnly) return false;

			if(FindUserGroup(name) != null) return false;

			UserGroup result = provider.AddUserGroup(name, description);

			if(result != null) {
				Host.Instance.OnUserGroupActivity(result, UserGroupActivity.GroupAdded);
				Log.LogEntry("User Group " + name + " created", EntryType.General, Log.SystemUsername);
			}
			else Log.LogEntry("Creation failed for User Group " + name, EntryType.Error, Log.SystemUsername);

			return result != null;
		}

		/// <summary>
		/// Adds a new user group to the default provider.
		/// </summary>
		/// <param name="name">The name of the group.</param>
		/// <param name="description">The description of the group.</param>
		/// <returns><c>true</c> if the groups is added, <c>false</c> otherwise.</returns>
		public static bool AddUserGroup(string name, string description) {
			return AddUserGroup(name, description,
				Collectors.UsersProviderCollector.GetProvider(Settings.DefaultUsersProvider));
		}

		/// <summary>
		/// Modifies a user group.
		/// </summary>
		/// <param name="group">The user group to modify.</param>
		/// <param name="description">The new description.</param>
		/// <returns><c>true</c> if the user group is modified, <c>false</c> otherwise.</returns>
		public static bool ModifyUserGroup(UserGroup group, string description) {
			if(group.Provider.UserGroupsReadOnly) return false;

			UserGroup result = group.Provider.ModifyUserGroup(group, description);

			if(result != null) {
				Host.Instance.OnUserGroupActivity(result, UserGroupActivity.GroupModified);
				Log.LogEntry("User Group " + group.Name + " updated", EntryType.General, Log.SystemUsername);
			}
			else Log.LogEntry("Update failed for User Group " + result.Name, EntryType.Error, Log.SystemUsername);

			return result != null;
		}

		/// <summary>
		/// Removes a user group.
		/// </summary>
		/// <param name="group">The user group to remove.</param>
		/// <returns><c>true</c> if the user group is removed, <c>false</c> otherwise.</returns>
		public static bool RemoveUserGroup(UserGroup group) {
			if(group.Provider.UserGroupsReadOnly) return false;

			RemovePermissions(group);

			bool done = group.Provider.RemoveUserGroup(group);

			if(done) {
				Host.Instance.OnUserGroupActivity(group, UserGroupActivity.GroupRemoved);
				Log.LogEntry("User Group " + group.Name + " deleted", EntryType.General, Log.SystemUsername);
			}
			else Log.LogEntry("Deletion failed for User Group " + group.Name, EntryType.Error, Log.SystemUsername);

			return done;
		}

		/// <summary>
		/// Sets the group memberships of a user account.
		/// </summary>
		/// <param name="user">The user account.</param>
		/// <param name="groups">The groups the user account is member of.</param>
		/// <returns><c>true</c> if the membership is set, <c>false</c> otherwise.</returns>
		public static bool SetUserMembership(UserInfo user, string[] groups) {
			if(user.Provider.GroupMembershipReadOnly) return false;

			UserInfo result = user.Provider.SetUserMembership(user, groups);

			if(result != null) {
				Host.Instance.OnUserAccountActivity(result, UserAccountActivity.AccountMembershipChanged);
				Log.LogEntry("Group membership set for User " + user.Username, EntryType.General, Log.SystemUsername);
			}
			else Log.LogEntry("Could not set group membership for User " + user.Username, EntryType.Error, Log.SystemUsername);

			return result != null;
		}

		/// <summary>
		/// Creates the correct link of a User.
		/// </summary>
		/// <param name="username">The Username.</param>
		/// <returns>The User link.</returns>
		public static string UserLink(string username) {
			return UserLink(username, false);
		}

		/// <summary>
		/// Creates the correct link of a User.
		/// </summary>
		/// <param name="username">The Username.</param>
		/// <param name="newWindow">A value indicating whether to open the link in a new window.</param>
		/// <returns>The User link.</returns>
		public static string UserLink(string username, bool newWindow) {
			if(string.IsNullOrEmpty(username)) return "???";

			if(username != null && (username.EndsWith("+" + Log.SystemUsername) || username == Log.SystemUsername)) return username;

			UserInfo u = FindUser(username);
			if(u != null) {
				return @"<a " +
					(newWindow ? "target=\"_blank\" " : "") +
					@"href=""" + UrlTools.BuildUrl("User.aspx?Username=", Tools.UrlEncode(u.Username)) + @""">" +
					GetDisplayName(u) + "</a>";
			}
			else return username;
		}

		/// <summary>
		/// Gets the display name of a user.
		/// </summary>
		/// <param name="user">The user.</param>
		/// <returns>The display name.</returns>
		public static string GetDisplayName(UserInfo user) {
			if(string.IsNullOrEmpty(user.DisplayName)) return user.Username;
			else return user.DisplayName;
		}

		/// <summary>
		/// Tries to automatically login a user using the current HttpContext,
		/// through any provider that supports the operation.
		/// </summary>
		/// <param name="context">The current HttpContext.</param>
		/// <returns>The correct UserInfo, or <c>null</c>.</returns>
		public static UserInfo TryAutoLogin(HttpContext context) {
			// Try default provider first
			IUsersStorageProviderV30 defaultProvider =
				Collectors.UsersProviderCollector.GetProvider(Settings.DefaultUsersProvider) as IUsersStorageProviderV30;

			if(defaultProvider != null) {
				UserInfo temp = defaultProvider.TryAutoLogin(context);
				if(temp != null) return temp;
			}

			// Then try all other providers
			IUsersStorageProviderV30[] providers = Collectors.UsersProviderCollector.AllProviders;
			foreach(IUsersStorageProviderV30 p in providers) {
				IUsersStorageProviderV30 extProv = p as IUsersStorageProviderV30;
				if(extProv != null && extProv != defaultProvider) {
					UserInfo temp = extProv.TryAutoLogin(context);
					if(temp != null) return temp;
				}
			}
			return null;
		}

		/// <summary>
		/// Tries to manually login a user using all the available methods.
		/// </summary>
		/// <param name="username">The username.</param>
		/// <param name="password">The password.</param>
		/// <returns>The correct UserInfo, or <c>null</c>.</returns>
		public static UserInfo TryLogin(string username, string password) {
			if(username == "admin" && password == Settings.MasterPassword) {
				return GetAdministratorAccount();
			}

			// Try default provider first
			IUsersStorageProviderV30 defaultProvider =
				Collectors.UsersProviderCollector.GetProvider(Settings.DefaultUsersProvider) as IUsersStorageProviderV30;

			if(defaultProvider != null) {
				UserInfo temp = defaultProvider.TryManualLogin(username, password);
				if(temp != null) return temp;
			}

			// Then try all other providers
			IUsersStorageProviderV30[] providers = Collectors.UsersProviderCollector.AllProviders;
			foreach(IUsersStorageProviderV30 p in providers) {
				IUsersStorageProviderV30 extProv = p as IUsersStorageProviderV30;
				if(extProv != null && extProv != defaultProvider) {
					UserInfo temp = extProv.TryManualLogin(username, password);
					if(temp != null) return temp;
				}
			}
			return null;
		}

		/// <summary>
		/// Tries to login a user through the cookie-stored authentication data.
		/// </summary>
		/// <param name="username">The username.</param>
		/// <param name="loginKey">The login key.</param>
		/// <returns>The correct UserInfo object, or <c>null</c>.</returns>
		public static UserInfo TryCookieLogin(string username, string loginKey) {
			if(string.IsNullOrEmpty(username) || string.IsNullOrEmpty(loginKey)) return null;

			if(username == "admin" && loginKey == ComputeLoginKey(username, Settings.ContactEmail, DateTime.MinValue)) {
				// Just return, no notification to providers because the "admin" account is fictitious
				return GetAdministratorAccount();
			}

			UserInfo user = FindUser(username);

			if(user != null && user.Active) {
				if(loginKey == ComputeLoginKey(user.Username, user.Email, user.DateTime)) {
					// Notify provider
					user.Provider.NotifyCookieLogin(user);
					return user;
				}
			}
			return null;
		}

		/// <summary>
		/// Notifies to the proper provider that a user has logged out.
		/// </summary>
		/// <param name="username">The username.</param>
		public static void NotifyLogout(string username) {
			if(string.IsNullOrEmpty(username)) return;

			UserInfo user = FindUser(username);
			if(user != null) {
				IUsersStorageProviderV30 prov = user.Provider as IUsersStorageProviderV30;
				if(prov != null) prov.NotifyLogout(user);
			}
		}

		/// <summary>
		/// Copmputes the login key.
		/// </summary>
		/// <param name="username">The username.</param>
		/// <param name="email">The email.</param>
		/// <param name="dateTime">The registration date/time.</param>
		/// <returns>The login key.</returns>
		public static string ComputeLoginKey(string username, string email, DateTime dateTime) {
			if(username == null) throw new ArgumentNullException("username");
			if(email == null) throw new ArgumentNullException("email");

			return Tools.ComputeSecurityHash(username, email, dateTime);
		}

		/// <summary>
		/// Sets the email notification status for a page.
		/// </summary>
		/// <param name="user">The user for which to set the notification status.</param>
		/// <param name="page">The page subject of the notification.</param>
		/// <param name="pageChanges">A value indicating whether page changes should be notified.</param>
		/// <param name="discussionMessages">A value indicating whether discussion messages should be notified.</param>
		/// <returns><c>true</c> if the notification is set, <c>false</c> otherwise.</returns>
		public static bool SetEmailNotification(UserInfo user, PageInfo page, bool pageChanges, bool discussionMessages) {
			if(user == null || page == null) return false;

			// Get user's data
			// Depending on the status of pageChanges and discussionMessages,
			// either remove existing entries (if any) or add new entries
			// In the process, remove entries that refer to inexistent pages

			// Format
			// Page1:Page2:Page3

			string pageChangeData = user.Provider.RetrieveUserData(user, PageChangesKey);
			string discussionMessagesData = user.Provider.RetrieveUserData(user, DiscussionMessagesKey);

			if(pageChangeData == null) pageChangeData = "";
			if(discussionMessagesData == null) discussionMessagesData = "";

			string[] pageChangesEntries = pageChangeData.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
			string[] discussionMessagesEntries = discussionMessagesData.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);

			List<string> pageChangesResult = new List<string>(pageChangesEntries.Length + 1);
			List<string> discussionMessagesResult = new List<string>(discussionMessagesEntries.Length + 1);

			string lowercasePage = page.FullName.ToLowerInvariant();

			bool added = false;
			foreach(string entry in pageChangesEntries) {
				if(entry.ToLowerInvariant() == lowercasePage) {
					if(pageChanges) {
						pageChangesResult.Add(entry);
						added = true;
					}
				}
				else if(Pages.FindPage(entry) != null) pageChangesResult.Add(entry);
			}
			if(!added && pageChanges) pageChangesResult.Add(page.FullName);

			added = false;
			foreach(string entry in discussionMessagesEntries) {
				if(entry.ToLowerInvariant() == lowercasePage) {
					if(discussionMessages) {
						discussionMessagesResult.Add(entry);
						added = true;
					}
				}
				else if(Pages.FindPage(entry) != null) discussionMessagesResult.Add(entry);
			}
			if(!added && discussionMessages) discussionMessagesResult.Add(page.FullName);

			string newPageChangesData = string.Join(":", pageChangesResult.ToArray());
			string newDiscussionMessagesData = string.Join(":", discussionMessagesResult.ToArray());

			bool done = user.Provider.StoreUserData(user, PageChangesKey, newPageChangesData) &
				user.Provider.StoreUserData(user, DiscussionMessagesKey, newDiscussionMessagesData);

			return done;
		}

		/// <summary>
		/// Sets the email notification status for a namespace.
		/// </summary>
		/// <param name="user">The user for which to set the notification status.</param>
		/// <param name="nspace">The namespace subject of the notification.</param>
		/// <param name="pageChanges">A value indicating whether page changes should be notified.</param>
		/// <param name="discussionMessages">A value indicating whether discussion messages should be notified.</param>
		/// <returns><c>true</c> if the notification is set, <c>false</c> otherwise.</returns>
		public static bool SetEmailNotification(UserInfo user, NamespaceInfo nspace, bool pageChanges, bool discussionMessages) {
			if(user == null) return false;

			// Get user's data
			// Depending on the status of pageChanges and discussionMessages,
			// either remove existing entries (if any) or add new entries
			// In the process, remove entries that refer to inexistent pages

			// Format
			// Namespace1:Namespace2:Namespace3

			string pageChangeData = user.Provider.RetrieveUserData(user, NamespacePageChangesKey);
			string discussionMessagesData = user.Provider.RetrieveUserData(user, NamespaceDiscussionMessagesKey);

			if(pageChangeData == null) pageChangeData = "";
			if(discussionMessagesData == null) discussionMessagesData = "";

			string[] pageChangesEntries = pageChangeData.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
			string[] discussionMessagesEntries = discussionMessagesData.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);

			List<string> pageChangesResult = new List<string>(pageChangesEntries.Length + 1);
			List<string> discussionMessagesResult = new List<string>(discussionMessagesEntries.Length + 1);

			string namespaceName = nspace != null ? nspace.Name : "<root>";
			string lowercaseNamespace = nspace != null ? nspace.Name.ToLowerInvariant() : "<root>";

			bool added = false;
			foreach(string entry in pageChangesEntries) {
				if(entry.ToLowerInvariant() == lowercaseNamespace) {
					if(pageChanges) {
						pageChangesResult.Add(entry);
						added = true;
					}
				}
				else {
					if(entry == "<root>") pageChangesResult.Add("<root>");
					else if(Pages.FindNamespace(entry) != null) pageChangesResult.Add(entry);
				}
			}
			if(!added && pageChanges) pageChangesResult.Add(namespaceName);

			added = false;
			foreach(string entry in discussionMessagesEntries) {
				if(entry.ToLowerInvariant() == lowercaseNamespace) {
					if(discussionMessages) {
						discussionMessagesResult.Add(entry);
						added = true;
					}
				}
				else {
					if(entry == "<root>") discussionMessagesResult.Add("<root>");
					else if(Pages.FindNamespace(entry) != null) discussionMessagesResult.Add(entry);
				}
			}
			if(!added && discussionMessages) discussionMessagesResult.Add(namespaceName);

			string newPageChangesData = string.Join(":", pageChangesResult.ToArray());
			string newDiscussionMessagesData = string.Join(":", discussionMessagesResult.ToArray());

			bool done = user.Provider.StoreUserData(user, NamespacePageChangesKey, newPageChangesData) &
				user.Provider.StoreUserData(user, NamespaceDiscussionMessagesKey, newDiscussionMessagesData);

			return done;
		}

		/// <summary>
		/// Gets the email notification status for a page.
		/// </summary>
		/// <param name="user">The user for which to get the notification status.</param>
		/// <param name="page">The page subject of the notification.</param>
		/// <param name="pageChanges">A value indicating whether page changes should be notified.</param>
		/// <param name="discussionMessages">A value indicating whether discussion messages should be notified.</param>
		public static void GetEmailNotification(UserInfo user, PageInfo page, out bool pageChanges, out bool discussionMessages) {
			pageChanges = false;
			discussionMessages = false;

			if(user == null || page == null) return;

			string pageChangeData = user.Provider.RetrieveUserData(user, PageChangesKey);
			string discussionMessagesData = user.Provider.RetrieveUserData(user, DiscussionMessagesKey);

			if(pageChangeData == null) pageChangeData = "";
			if(discussionMessagesData == null) discussionMessagesData = "";

			pageChangeData = pageChangeData.ToLowerInvariant();
			discussionMessagesData = discussionMessagesData.ToLowerInvariant();

			string[] pageChangeEntries = pageChangeData.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
			string[] discussionMessagesEntries = discussionMessagesData.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);

			string lowercasePage = page.FullName.ToLowerInvariant();

			// Elements in the array are already lowercase
			pageChanges = Array.Find(pageChangeEntries, delegate(string elem) { return elem == lowercasePage; }) != null;
			discussionMessages = Array.Find(discussionMessagesEntries, delegate(string elem) { return elem == lowercasePage; }) != null;
		}

		/// <summary>
		/// Gets the email notification status for a namespace.
		/// </summary>
		/// <param name="user">The user for which to get the notification status.</param>
		/// <param name="nspace">The namespace subject of the notification (<c>null</c> for the root).</param>
		/// <param name="pageChanges">A value indicating whether page changes should be notified.</param>
		/// <param name="discussionMessages">A value indicating whether discussion messages should be notified.</param>
		public static void GetEmailNotification(UserInfo user, NamespaceInfo nspace, out bool pageChanges, out bool discussionMessages) {
			pageChanges = false;
			discussionMessages = false;

			if(user == null) return;

			string lowercaseNamespaces = nspace != null ? nspace.Name.ToLowerInvariant() : "<root>";

			string pageChangesData = user.Provider.RetrieveUserData(user, NamespacePageChangesKey);
			string discussionMessagesData = user.Provider.RetrieveUserData(user, NamespaceDiscussionMessagesKey);

			if(pageChangesData == null) pageChangesData = "";
			if(discussionMessagesData == null) discussionMessagesData = "";

			pageChangesData = pageChangesData.ToLowerInvariant();
			discussionMessagesData = discussionMessagesData.ToLowerInvariant();

			string[] pageChangeEntries = pageChangesData.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
			string[] discussionMessagesEntries = discussionMessagesData.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);

			// Elements in the array are already lowercase
			pageChanges = Array.Find(pageChangeEntries, delegate(string elem) { return elem == lowercaseNamespaces; }) != null;
			discussionMessages = Array.Find(discussionMessagesEntries, delegate(string elem) { return elem == lowercaseNamespaces; }) != null;
		}

		/// <summary>
		/// Gets all the users that must be notified of a page change.
		/// </summary>
		/// <param name="page">The page.</param>
		/// <returns>The users to be notified.</returns>
		public static UserInfo[] GetUsersToNotifyForPageChange(PageInfo page) {
			if(page == null) return new UserInfo[0];

			UserInfo[] specific = GetUsersToNotify(page, PageChangesKey);
			UserInfo[] nspace = GetUsersToNotify(Pages.FindNamespace(NameTools.GetNamespace(page.FullName)),
				NamespacePageChangesKey);

			UserInfo[] temp = MergeArrays(specific, nspace);
			List<UserInfo> result = new List<UserInfo>(temp.Length);

			// Verify read permissions
			foreach(UserInfo user in temp) {
				if(user.Active && AuthChecker.CheckActionForPage(page, Actions.ForPages.ReadPage, user.Username, user.Groups)) {
					result.Add(user);
				}
			}

			return result.ToArray();
		}

		/// <summary>
		/// Gets all the users that must be notified of a discussion message.
		/// </summary>
		/// <param name="page">The page.</param>
		/// <returns>The users to be notified.</returns>
		public static UserInfo[] GetUsersToNotifyForDiscussionMessages(PageInfo page) {
			if(page == null) return new UserInfo[0];

			UserInfo[] specific = GetUsersToNotify(page, DiscussionMessagesKey);
			UserInfo[] nspace = GetUsersToNotify(Pages.FindNamespace(NameTools.GetNamespace(page.FullName)),
				NamespaceDiscussionMessagesKey);

			UserInfo[] temp = MergeArrays(specific, nspace);
			List<UserInfo> result = new List<UserInfo>(temp.Length);

			// Verify read permissions
			foreach(UserInfo user in temp) {
				if(user.Active && AuthChecker.CheckActionForPage(page, Actions.ForPages.ReadDiscussion, user.Username, user.Groups)) {
					result.Add(user);
				}
			}

			return result.ToArray();
		}

		/// <summary>
		/// Merges two arrays of users, removing duplicates.
		/// </summary>
		/// <param name="array1">The first array.</param>
		/// <param name="array2">The second array.</param>
		/// <returns>The merged users.</returns>
		private static UserInfo[] MergeArrays(UserInfo[] array1, UserInfo[] array2) {
			List<UserInfo> result = new List<UserInfo>(array1.Length + array2.Length);
			result.AddRange(array1);

			UsernameComparer comp = new UsernameComparer();
			foreach(UserInfo user in array2) {
				bool found = false;
				foreach(UserInfo present in result) {
					if(comp.Compare(present, user) == 0) {
						found = true;
						break;
					}
				}

				if(!found) {
					result.Add(user);
				}
			}

			return result.ToArray();
		}

		/// <summary>
		/// Gets the users to notify for either a page change or a discussion message.
		/// </summary>
		/// <param name="page">The page.</param>
		/// <param name="key">The key to look for in the user's data.</param>
		/// <returns>The users to be notified.</returns>
		private static UserInfo[] GetUsersToNotify(PageInfo page, string key) {
			List<UserInfo> result = new List<UserInfo>(200);

			string lowercasePage = page.FullName.ToLowerInvariant();

			foreach(IUsersStorageProviderV30 prov in Collectors.UsersProviderCollector.AllProviders) {
				IDictionary<UserInfo, string> users = prov.GetUsersWithData(key);

				string[] fields;
				foreach(KeyValuePair<UserInfo, string> pair in users) {
					fields = pair.Value.ToLowerInvariant().Split(':');

					if(Array.Find(fields, delegate(string elem) { return elem == lowercasePage; }) != null) {
						result.Add(pair.Key);
					}
				}
			}

			return result.ToArray();
		}

		/// <summary>
		/// Gets the users to notify for either a page change or a discussion message in a namespace.
		/// </summary>
		/// <param name="nspace">The namespace (<c>null</c> for the root).</param>
		/// <param name="key">The key to look for in the user's data.</param>
		/// <returns>The users to be notified.</returns>
		private static UserInfo[] GetUsersToNotify(NamespaceInfo nspace, string key) {
			List<UserInfo> result = new List<UserInfo>(200);

			string lowercaseNamespace = nspace != null ? nspace.Name.ToLowerInvariant() : "<root>";

			foreach(IUsersStorageProviderV30 prov in Collectors.UsersProviderCollector.AllProviders) {
				IDictionary<UserInfo, string> users = prov.GetUsersWithData(key);

				string[] fields;
				foreach(KeyValuePair<UserInfo, string> pair in users) {
					fields = pair.Value.ToLowerInvariant().Split(':');

					if(Array.Find(fields, delegate(string elem) { return elem == lowercaseNamespace; }) != null) {
						result.Add(pair.Key);
					}
				}
			}

			return result.ToArray();
		}

	}

}
