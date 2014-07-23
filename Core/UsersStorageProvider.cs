
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Implements a Users Storage Provider.
	/// </summary>
	public class UsersStorageProvider : ProviderBase, IUsersStorageProviderV30 {

		private const string UsersFile = "Users.cs";
		private const string UsersDataFile = "UsersData.cs";
		private const string GroupsFile = "Groups.cs";

		private readonly ComponentInformation info = new ComponentInformation("Local Users Provider",
			"Threeplicate Srl", Settings.WikiVersion, "http://www.screwturn.eu", null);

		private IHostV30 host;

		private UserGroup[] groupsCache = null;
		private UserInfo[] usersCache = null;

		private string GetFullPath(string filename) {
			return Path.Combine(GetDataDirectory(host), filename);
		}

		/// <summary>
		/// Initializes the Provider.
		/// </summary>
		/// <param name="host">The Host of the Provider.</param>
		/// <param name="config">The Configuration data, if any.</param>
		/// <exception cref="ArgumentNullException">If <b>host</b> or <b>config</b> are <c>null</c>.</exception>
		/// <exception cref="InvalidConfigurationException">If <b>config</b> is not valid or is incorrect.</exception>
		public void Init(IHostV30 host, string config) {
			if(host == null) throw new ArgumentNullException("host");
			if(config == null) throw new ArgumentNullException("config");

			this.host = host;

			if(!LocalProvidersTools.CheckWritePermissions(GetDataDirectory(host))) {
				throw new InvalidConfigurationException("Cannot write into the public directory - check permissions");
			}

			if(!File.Exists(GetFullPath(UsersFile))) {
				File.Create(GetFullPath(UsersFile)).Close();
			}
			if(!File.Exists(GetFullPath(UsersDataFile))) {
				File.Create(GetFullPath(UsersDataFile)).Close();
			}
			if(!File.Exists(GetFullPath(GroupsFile))) {
				File.Create(GetFullPath(GroupsFile)).Close();
			}

			VerifyAndPerformUpgrade();
		}

		/// <summary>
		/// Verifies the need for a data upgrade, and performs it when needed.
		/// </summary>
		private void VerifyAndPerformUpgrade() {
			// Load file lines
			// Parse first line (if any) with old (v2) algorithm
			// If parsing is successful, then the file must be converted
			// Conversion consists in removing the 'ADMIN|USER' field, creating the proper default groups and setting user membership

			// Structure v2:
			// Username|PasswordHash|Email|Active-Inactive|DateTime|Admin-User

			//string[] lines = File.ReadAllLines(GetFullPath(UsersFile));
			// Use this method because version 2.0 file might have started with a blank line
			string[] lines = File.ReadAllText(GetFullPath(UsersFile)).Replace("\r", "").Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

			if(lines.Length > 0) {
				bool upgradeIsNeeded = false;
				LocalUserInfo[] users = new LocalUserInfo[lines.Length];
				bool[] oldStyleAdmin = new bool[lines.Length]; // Values are valid only if upgradeIsNeeded=true

				char[] splitter = new char[] { '|' };

				for(int i = 0; i < lines.Length; i++) {
					string line = lines[i];

					string[] fields = line.Split(splitter, StringSplitOptions.RemoveEmptyEntries);

					string displayName = null;

					if(fields.Length == 6) {
						if(fields[5] == "ADMIN" || fields[5] == "USER") {
							// Version 2.0
							upgradeIsNeeded = true;
							oldStyleAdmin[i] = fields[5] == "ADMIN";
						}
						else {
							// Version 3.0 with DisplayName specified
							oldStyleAdmin[i] = false;
							displayName = fields[5];
						}
					}
					else {
						// Can be a version 3.0 file, with empty DisplayName
						oldStyleAdmin[i] = false;
					}

					users[i] = new LocalUserInfo(fields[0], displayName, fields[2],
						fields[3].ToLowerInvariant() == "active", DateTime.Parse(fields[4]), this, fields[1]);
				}

				if(upgradeIsNeeded) {
					// Dump users
					// Create default groups
					// Set membership for old users
					// Tell the host to set the permissions for the default groups

					string backupFile = GetFullPath(Path.GetFileNameWithoutExtension(UsersFile) + "_v2" + Path.GetExtension(UsersFile));
					File.Copy(GetFullPath(UsersFile), backupFile);

					host.LogEntry("Upgrading users format from 2.0 to 3.0", LogEntryType.General, null, this);

					DumpUsers(users);
					UserGroup adminsGroup = AddUserGroup(host.GetSettingValue(SettingName.AdministratorsGroup), "Built-in Administrators");
					UserGroup usersGroup = AddUserGroup(host.GetSettingValue(SettingName.UsersGroup), "Built-in Users");

					for(int i = 0; i < users.Length; i++) {
						if(oldStyleAdmin[i]) {
							SetUserMembership(users[i], new string[] { adminsGroup.Name });
						}
						else {
							SetUserMembership(users[i], new string[] { usersGroup.Name });
						}
					}

					host.UpgradeSecurityFlagsToGroupsAcl(adminsGroup, usersGroup);
				}
			}
		}

		/// <summary>
		/// Method invoked on shutdown.
		/// </summary>
		/// <remarks>This method might not be invoked in some cases.</remarks>
		public void Shutdown() { }

		/// <summary>
		/// Gets the Information about the Provider.
		/// </summary>
		public ComponentInformation Information {
			get { return info; }
		}

		/// <summary>
		/// Gets a brief summary of the configuration string format, in HTML. Returns <c>null</c> if no configuration is needed.
		/// </summary>
		public string ConfigHelpHtml {
			get { return null; }
		}

		/// <summary>
		/// Gets a value indicating whether user accounts are read-only.
		/// </summary>
		public bool UserAccountsReadOnly {
			get { return false; }
		}

		/// <summary>
		/// Gets a value indicating whether user groups are read-only. If so, the provider 
		/// should support default user groups as defined in the wiki configuration.
		/// </summary>
		public bool UserGroupsReadOnly {
			get { return false; }
		}

		/// <summary>
		/// Gets a value indicating whether group membership is read-only (if <see cref="UserAccountsReadOnly" /> 
		/// is <c>false</c>, then this property must be <c>false</c>). If this property is <c>true</c>, the provider 
		/// should return membership data compatible with default user groups.
		/// </summary>
		public bool GroupMembershipReadOnly {
			get { return false; }
		}

		/// <summary>
		/// Gets a value indicating whether users' data is read-only.
		/// </summary>
		public bool UsersDataReadOnly {
			get { return false; }
		}

		/// <summary>
		/// Tests a Password for a User account.
		/// </summary>
		/// <param name="user">The User account.</param>
		/// <param name="password">The Password to test.</param>
		/// <returns>True if the Password is correct.</returns>
		/// <exception cref="ArgumentNullException">If <b>user</b> or <b>password</b> are <c>null</c>.</exception>
		public bool TestAccount(UserInfo user, string password) {
			if(user == null) throw new ArgumentNullException("user");
			if(password == null) throw new ArgumentNullException("password");

			return TryManualLogin(user.Username, password) != null;
		}

		/// <summary>
		/// Gets the complete list of Users.
		/// </summary>
		/// <returns>All the Users, sorted by username.</returns>
		public UserInfo[] GetUsers() {
			lock(this) {
				if(usersCache == null) {

					UserGroup[] groups = GetUserGroups();

					string[] lines = File.ReadAllLines(GetFullPath(UsersFile));

					UserInfo[] result = new UserInfo[lines.Length];

					char[] splitter = new char[] { '|' };

					string[] fields;
					for(int i = 0; i < lines.Length; i++) {
						fields = lines[i].Split(splitter, StringSplitOptions.RemoveEmptyEntries);

						// Structure (version 3.0 - file previously converted):
						// Username|PasswordHash|Email|Active-Inactive|DateTime[|DisplayName]

						string displayName = fields.Length == 6 ? fields[5] : null;

						result[i] = new LocalUserInfo(fields[0], displayName, fields[2], fields[3].ToLowerInvariant().Equals("active"),
							DateTime.Parse(fields[4]), this, fields[1]);

						result[i].Groups = GetGroupsForUser(result[i].Username, groups);
					}

					Array.Sort(result, new UsernameComparer());

					usersCache = result;
				}

				return usersCache;
			}
		}

		/// <summary>
		/// Gets the names of all the groups a user is member of.
		/// </summary>
		/// <param name="user">The username.</param>
		/// <param name="groups">The groups.</param>
		/// <returns>The names of the groups the user is member of.</returns>
		private string[] GetGroupsForUser(string user, UserGroup[] groups) {
			List<string> result = new List<string>(3);
			
			foreach(UserGroup group in groups) {
				if(Array.Find(group.Users, delegate(string u) { return u == user; }) != null) {
					result.Add(group.Name);
				}
			}

			return result.ToArray();
		}

		/// <summary>
		/// Loads a proper local instance of a user account.
		/// </summary>
		/// <param name="user">The user account.</param>
		/// <returns>The local instance, or <c>null</c>.</returns>
		private LocalUserInfo LoadLocalInstance(UserInfo user) {
			UserInfo[] users = GetUsers();
			UsernameComparer comp = new UsernameComparer();
			for(int i = 0; i < users.Length; i++) {
				if(comp.Compare(users[i], user) == 0) return users[i] as LocalUserInfo;
			}
			return null;
		}

		/// <summary>
		/// Searches for a User.
		/// </summary>
		/// <param name="user">The User to search for.</param>
		/// <returns>True if the User already exists.</returns>
		private bool UserExists(UserInfo user) {
			UserInfo[] users = GetUsers();
			UsernameComparer comp = new UsernameComparer();
			for(int i = 0; i < users.Length; i++) {
				if(comp.Compare(users[i], user) == 0) return true;
			}
			return false;
		}

		/// <summary>
		/// Adds a new User.
		/// </summary>
		/// <param name="username">The Username.</param>
		/// <param name="displayName">The display name (can be <c>null</c>).</param>
		/// <param name="password">The Password.</param>
		/// <param name="email">The Email address.</param>
		/// <param name="active">A value specifying whether or not the account is active.</param>
		/// <param name="dateTime">The Account creation Date/Time.</param>
		/// <returns>The correct <see cref="T:UserInfo"/> object or <c>null</c>.</returns>
		/// <exception cref="ArgumentNullException">If <b>username</b>, <b>password</b> or <b>email</b> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>username</b>, <b>password</b> or <b>email</b> are empty.</exception>
		public UserInfo AddUser(string username, string displayName, string password, string email, bool active, DateTime dateTime) {
			if(username == null) throw new ArgumentNullException("username");
			if(username.Length == 0) throw new ArgumentException("Username cannot be empty", "username");
			if(password == null) throw new ArgumentNullException("password");
			if(password.Length == 0) throw new ArgumentException("Password cannot be empty", "password");
			if(email == null) throw new ArgumentNullException("email");
			if(email.Length == 0) throw new ArgumentException("Email cannot be empty", "email");

			lock(this) {
				if(UserExists(new UserInfo(username, displayName, "", true, DateTime.Now, this))) return null;

				BackupUsersFile();

				StringBuilder sb = new StringBuilder();
				sb.Append(username);
				sb.Append("|");
				sb.Append(Hash.Compute(password));
				sb.Append("|");
				sb.Append(email);
				sb.Append("|");
				sb.Append(active ? "ACTIVE" : "INACTIVE");
				sb.Append("|");
				sb.Append(dateTime.ToString("yyyy'/'MM'/'dd' 'HH':'mm':'ss"));
				// ADMIN|USER no more used in version 3.0
				//sb.Append("|");
				//sb.Append(admin ? "ADMIN" : "USER");
				if(!string.IsNullOrEmpty(displayName)) {
					sb.Append("|");
					sb.Append(displayName);
				}
				sb.Append("\r\n");
				File.AppendAllText(GetFullPath(UsersFile), sb.ToString());

				usersCache = null;

				return new LocalUserInfo(username, displayName, email, active, dateTime, this, Hash.Compute(password));
			}
		}

		/// <summary>
		/// Modifies a User.
		/// </summary>
		/// <param name="user">The Username of the user to modify.</param>
		/// <param name="newDisplayName">The new display name (can be <c>null</c>).</param>
		/// <param name="newPassword">The new Password (<c>null</c> or blank to keep the current password).</param>
		/// <param name="newEmail">The new Email address.</param>
		/// <param name="newActive">A value indicating whether the account is active.</param>
		/// <returns>The correct <see cref="T:UserInfo"/> object or <c>null</c>.</returns>
		/// <exception cref="ArgumentNullException">If <b>user</b> or <b>newEmail</b> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>newEmail</b> is empty.</exception>
		public UserInfo ModifyUser(UserInfo user, string newDisplayName, string newPassword, string newEmail, bool newActive) {
			if(user == null) throw new ArgumentNullException("user");
			if(newEmail == null) throw new ArgumentNullException("newEmail");
			if(newEmail.Length == 0) throw new ArgumentException("New Email cannot be empty", "newEmail");

			lock(this) {
				LocalUserInfo local = LoadLocalInstance(user);
				if(local == null) return null;

				UserInfo[] allUsers = GetUsers();
				UsernameComparer comp = new UsernameComparer();

				usersCache = null;

				for(int i = 0; i < allUsers.Length; i++) {
					if(comp.Compare(allUsers[i], user) == 0) {
						LocalUserInfo result = new LocalUserInfo(user.Username, newDisplayName, newEmail,
							newActive, user.DateTime, this,
							string.IsNullOrEmpty(newPassword) ? local.PasswordHash : Hash.Compute(newPassword));
						result.Groups = allUsers[i].Groups;
						allUsers[i] = result;
						DumpUsers(allUsers);
						return result;
					}
				}
			}

			return null;
		}

		/// <summary>
		/// Removes a User.
		/// </summary>
		/// <param name="user">The User to remove.</param>
		/// <returns>True if the User has been removed successfully.</returns>
		/// <exception cref="ArgumentNullException">If <b>user</b> is <c>null</c>.</exception>
		public bool RemoveUser(UserInfo user) {
			if(user == null) throw new ArgumentNullException("user");

			lock(this) {
				UserInfo[] users = GetUsers();
				UsernameComparer comp = new UsernameComparer();
				int idx = -1;
				for(int i = 0; i < users.Length; i++) {
					if(comp.Compare(users[i], user) == 0) {
						idx = i;
						break;
					}
				}
				if(idx < 0) return false;

				// Remove user's data
				string lowercaseUsername = user.Username.ToLowerInvariant();
				string[] lines = File.ReadAllLines(GetFullPath(UsersDataFile));
				List<string> newLines = new List<string>(lines.Length);
				string[] fields;
				for(int i = 0; i < lines.Length; i++) {
					fields = lines[i].Split('|');
					if(fields[0].ToLowerInvariant() != lowercaseUsername) {
						newLines.Add(lines[i]);
					}
				}
				File.WriteAllLines(GetFullPath(UsersDataFile), newLines.ToArray());

				// Remove user
				List<UserInfo> tmp = new List<UserInfo>(users);
				tmp.Remove(tmp[idx]);
				DumpUsers(tmp.ToArray());

				usersCache = null;
			}
			return true;
		}

		private void BackupUsersFile() {
			lock(this) {
				File.Copy(GetFullPath(UsersFile),
					GetFullPath(Path.GetFileNameWithoutExtension(UsersFile) +
					".bak" + Path.GetExtension(UsersFile)), true);
			}
		}

		/// <summary>
		/// Writes on disk all the Users.
		/// </summary>
		/// <param name="users">The User list.</param>
		/// <remarks>This method does not lock resources, therefore a lock is need in the caller.</remarks>
		private void DumpUsers(UserInfo[] users) {
			lock(this) {
				BackupUsersFile();

				StringBuilder sb = new StringBuilder();
				for(int i = 0; i < users.Length; i++) {
					LocalUserInfo u = (LocalUserInfo)users[i];
					sb.Append(u.Username);
					sb.Append("|");
					sb.Append(u.PasswordHash);
					sb.Append("|");
					sb.Append(u.Email);
					sb.Append("|");
					sb.Append(u.Active ? "ACTIVE" : "INACTIVE");
					sb.Append("|");
					sb.Append(u.DateTime.ToString("yyyy'/'MM'/'dd' 'HH':'mm':'ss"));
					// ADMIN|USER no more used in version 3.0
					//sb.Append("|");
					//sb.Append(u.Admin ? "ADMIN" : "USER");
					if(!string.IsNullOrEmpty(u.DisplayName)) {
						sb.Append("|");
						sb.Append(u.DisplayName);
					}
					sb.Append("\r\n");
				}
				File.WriteAllText(GetFullPath(UsersFile), sb.ToString());
			}
		}

		/// <summary>
		/// Finds a user group.
		/// </summary>
		/// <param name="name">The name of the group to find.</param>
		/// <returns>The <see cref="T:UserGroup" /> or <c>null</c> if no data is found.</returns>
		private UserGroup FindGroup(string name) {
			lock(this) {
				UserGroup[] allUsers = GetUserGroups();
				UserGroupComparer comp = new UserGroupComparer();
				UserGroup target = new UserGroup(name, "", this);

				foreach(UserGroup g in allUsers) {
					if(comp.Compare(g, target) == 0) return g;
				}

				return null;
			}
		}

		/// <summary>
		/// Gets all the user groups.
		/// </summary>
		/// <returns>All the groups, sorted by name.</returns>
		public UserGroup[] GetUserGroups() {
			lock(this) {
				if(groupsCache == null) {

					string[] lines = File.ReadAllLines(GetFullPath(GroupsFile));

					UserGroup[] result = new UserGroup[lines.Length];

					string[] fields;
					string[] users;
					for(int count = 0; count < lines.Length; count++) {
						// Structure - description can be empty
						// Name|Description|User1|User2|...

						fields = lines[count].Split('|');
						users = new string[fields.Length - 2];

						for(int i = 0; i < fields.Length - 2; i++) {
							users[i] = fields[i + 2];
						}

						result[count] = new UserGroup(fields[0], fields[1], this);
						result[count].Users = users;
					}

					Array.Sort(result, new UserGroupComparer());

					groupsCache = result;
				}

				return groupsCache;
			}
		}

		/// <summary>
		/// Adds a new user group.
		/// </summary>
		/// <param name="name">The name of the group.</param>
		/// <param name="description">The description of the group.</param>
		/// <returns>The correct <see cref="T:UserGroup"/> object or <c>null</c>.</returns>
		/// <exception cref="ArgumentNullException">If <b>name</b> or <b>description</b> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>name</b> is empty.</exception>
		public UserGroup AddUserGroup(string name, string description) {
			if(name == null) throw new ArgumentNullException("name");
			if(name.Length == 0) throw new ArgumentException("Name cannot be empty", "name");
			if(description == null) throw new ArgumentNullException("description");

			lock(this) {
				if(FindGroup(name) != null) return null;

				BackupGroupsFile();

				groupsCache = null;

				// Structure - description can be empty
				// Name|Description|User1|User2|...

				File.AppendAllText(GetFullPath(GroupsFile),
					name + "|" + description + "\r\n");

				return new UserGroup(name, description, this);
			}
		}

		private void BackupGroupsFile() {
			lock(this) {
				File.Copy(GetFullPath(GroupsFile),
					GetFullPath(Path.GetFileNameWithoutExtension(GroupsFile) +
					".bak" + Path.GetExtension(GroupsFile)), true);
			}
		}

		/// <summary>
		/// Dumps user groups on disk.
		/// </summary>
		/// <param name="groups">The user groups to dump.</param>
		private void DumpUserGroups(UserGroup[] groups) {
			lock(this) {
				StringBuilder sb = new StringBuilder(1000);
				foreach(UserGroup group in groups) {
					// Structure - description can be empty
					// Name|Description|User1|User2|...

					sb.Append(group.Name);
					sb.Append("|");
					sb.Append(group.Description);
					if(group.Users.Length > 0) {
						foreach(string user in group.Users) {
							sb.Append("|");
							sb.Append(user);
						}
					}
					sb.Append("\r\n");
				}
				BackupGroupsFile();
				File.WriteAllText(GetFullPath(GroupsFile), sb.ToString());
			}
		}

		/// <summary>
		/// Modifies a user group.
		/// </summary>
		/// <param name="group">The group to modify.</param>
		/// <param name="description">The new description of the group.</param>
		/// <returns>The correct <see cref="T:UserGroup"/> object or <c>null</c>.</returns>
		/// <exception cref="ArgumentNullException">If <b>group</b> or <b>description</b> are <c>null</c>.</exception>
		public UserGroup ModifyUserGroup(UserGroup group, string description) {
			if(group == null) throw new ArgumentNullException("group");
			if(description == null) throw new ArgumentNullException("description");

			lock(this) {
				UserGroup[] allGroups = GetUserGroups();

				groupsCache = null;

				UserGroupComparer comp = new UserGroupComparer();
				for(int i = 0; i < allGroups.Length; i++) {
					if(comp.Compare(allGroups[i], group) == 0) {
						allGroups[i].Description = description;
						DumpUserGroups(allGroups);
						return allGroups[i];
					}
				}

				return null;
			}
		}

		/// <summary>
		/// Removes a user group.
		/// </summary>
		/// <param name="group">The group to remove.</param>
		/// <returns><c>true</c> if the group is removed, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <b>group</b> is <c>null</c>.</exception>
		public bool RemoveUserGroup(UserGroup group) {
			if(group == null) throw new ArgumentNullException("group");

			lock(this) {
				UserGroup[] allGroups = GetUserGroups();

				List<UserGroup> result = new List<UserGroup>(allGroups.Length);

				UserGroupComparer comp = new UserGroupComparer();

				foreach(UserGroup g in allGroups) {
					if(comp.Compare(g, group) != 0) {
						result.Add(g);
					}
				}

				DumpUserGroups(result.ToArray());

				groupsCache = null;
				usersCache = null;

				return result.Count == allGroups.Length - 1;
			}
		}

		/// <summary>
		/// Sets the group memberships of a user account.
		/// </summary>
		/// <param name="user">The user account.</param>
		/// <param name="groups">The groups the user account is member of.</param>
		/// <returns>The correct <see cref="T:UserGroup"/> object or <c>null</c>.</returns>
		/// <exception cref="ArgumentNullException">If <b>user</b> or <b>groups</b> are <c>null</c>.</exception>
		public UserInfo SetUserMembership(UserInfo user, string[] groups) {
			if(user == null) throw new ArgumentNullException("user");
			if(groups == null) throw new ArgumentNullException("groups");

			lock(this) {
				foreach(string g in groups) {
					if(FindGroup(g) == null) return null;
				}

				LocalUserInfo local = LoadLocalInstance(user);
				if(local == null) return null;

				UserGroup[] allGroups = GetUserGroups();

				List<string> users;
				for(int i = 0; i < allGroups.Length; i++) {

					users = new List<string>(allGroups[i].Users);

					if(IsSelected(allGroups[i], groups)) {
						// Current group is one of the selected, add user to it
						if(!users.Contains(user.Username)) users.Add(user.Username);
					}
					else {
						// Current group is not bound with the user, remove user
						users.Remove(user.Username);
					}

					allGroups[i].Users = users.ToArray();
				}

				groupsCache = null;
				usersCache = null;

				DumpUserGroups(allGroups);

				LocalUserInfo result = new LocalUserInfo(local.Username, local.DisplayName, local.Email, local.Active,
					local.DateTime, this, local.PasswordHash);
				result.Groups = groups;

				return result;
			}
		}

		/// <summary>
		/// Determines whether a user group is contained in an array of user group names.
		/// </summary>
		/// <param name="group">The user group to check.</param>
		/// <param name="groups">The user group names array.</param>
		/// <returns><c>true</c> if <b>users</b> contains <b>user.Name</b>, <c>false</c> otherwise.</returns>
		private static bool IsSelected(UserGroup group, string[] groups) {
			StringComparer comp = StringComparer.OrdinalIgnoreCase;
			return Array.Find(groups, delegate(string g) { return comp.Compare(g, group.Name) == 0; }) != null;
		}

		/// <summary>
		/// Tries to login a user directly through the provider.
		/// </summary>
		/// <param name="username">The username.</param>
		/// <param name="password">The password.</param>
		/// <returns>The correct UserInfo object, or <c>null</c>.</returns>
		/// <exception cref="ArgumentNullException">If <b>username</b> or <b>password</b> are <c>null</c>.</exception>
		public UserInfo TryManualLogin(string username, string password) {
			if(username == null) throw new ArgumentNullException("username");
			if(password == null) throw new ArgumentNullException("password");

			// Shortcut
			if(username.Length == 0) return null;
			if(password.Length == 0) return null;

			lock(this) {
				string hash = Hash.Compute(password);
				UserInfo[] all = GetUsers();
				foreach(UserInfo u in all) {
					if(u.Active &&
						//string.Compare(u.Username, username, false, System.Globalization.CultureInfo.InvariantCulture) == 0 &&
						//string.Compare(((LocalUserInfo)u).PasswordHash, hash, false, System.Globalization.CultureInfo.InvariantCulture) == 0) {
						string.CompareOrdinal(u.Username, username) == 0 &&
						string.CompareOrdinal(((LocalUserInfo)u).PasswordHash, hash) == 0) {
						return u;
					}
				}
			}
			return null;
		}

		/// <summary>
		/// Tries to login a user directly through the provider using
		/// the current HttpContext and without username/password.
		/// </summary>
		/// <param name="context">The current HttpContext.</param>
		/// <returns>The correct UserInfo object, or <c>null</c>.</returns>
		/// <exception cref="ArgumentNullException">If <b>context</b> is <c>null</c>.</exception>
		public UserInfo TryAutoLogin(System.Web.HttpContext context) {
			if(context == null) throw new ArgumentNullException("context");

			return null;
		}

		/// <summary>
		/// Tries to retrieve the information about a user account.
		/// </summary>
		/// <param name="username">The username.</param>
		/// <returns>The correct UserInfo object, or <c>null</c>.</returns>
		/// <exception cref="ArgumentNullException">If <b>username</b> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>username</b> is empty.</exception>
		public UserInfo GetUser(string username) {
			if(username == null) throw new ArgumentNullException("username");
			if(username.Length == 0) throw new ArgumentException("Username cannot be empty", "username");

			lock(this) {
				UserInfo[] all = GetUsers();
				foreach(UserInfo u in all) {
					if(string.Compare(u.Username, username, false, System.Globalization.CultureInfo.InvariantCulture) == 0) {
						return u;
					}
				}
			}
			return null;
		}

		/// <summary>
		/// Tries to retrieve the information about a user account.
		/// </summary>
		/// <param name="email">The email address.</param>
		/// <returns>The first user found with the specified email address, or <c>null</c>.</returns>
		/// <exception cref="ArgumentNullException">If <b>email</b> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>email</b> is empty.</exception>
		public UserInfo GetUserByEmail(string email) {
			if(email == null) throw new ArgumentNullException("email");
			if(email.Length == 0) throw new ArgumentException("Email cannot be empty", "email");

			lock(this) {
				foreach(UserInfo user in GetUsers()) {
					if(user.Email == email) return user;
				}
			}
			return null;
		}

		/// <summary>
		/// Notifies the provider that a user has logged in through the authentication cookie.
		/// </summary>
		/// <param name="user">The user who has logged in.</param>
		/// <exception cref="ArgumentNullException">If <b>user</b> is <c>null</c>.</exception>
		public void NotifyCookieLogin(UserInfo user) {
			if(user == null) throw new ArgumentNullException("user");
			// Nothing to do
		}

		/// <summary>
		/// Notifies the provider that a user has logged out.
		/// </summary>
		/// <param name="user">The user who has logged out.</param>
		/// <exception cref="ArgumentNullException">If <b>user</b> is <c>null</c>.</exception>
		public void NotifyLogout(UserInfo user) {
			if(user == null) throw new ArgumentNullException("user");
			// Nothing to do
		}

		/// <summary>
		/// Stores a user data element, overwriting the previous one if present.
		/// </summary>
		/// <param name="user">The user the data belongs to.</param>
		/// <param name="key">The key of the data element (case insensitive).</param>
		/// <param name="value">The value of the data element, <c>null</c> for deleting the data.</param>
		/// <returns><c>true</c> if the data element is stored, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <b>user</b> or <b>key</b> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>key</b> is empty.</exception>
		public bool StoreUserData(UserInfo user, string key, string value) {
			if(user == null) throw new ArgumentNullException("user");
			if(key == null) throw new ArgumentNullException("key");
			if(key.Length == 0) throw new ArgumentException("Key cannot be empty", "key");

			// Format
			// User|Key|Value

			lock(this) {
				if(GetUser(user.Username) == null) return false;

				// Find a previously existing key and replace it if found
				// If not found, add a new line

				string lowercaseUsername = user.Username.ToLowerInvariant();
				string lowercaseKey = key.ToLowerInvariant();

				string[] lines = File.ReadAllLines(GetFullPath(UsersDataFile));

				string[] fields;
				for(int i = 0; i < lines.Length; i++) {
					fields = lines[i].Split('|');
					if(fields[0].ToLowerInvariant() == lowercaseUsername && fields[1].ToLowerInvariant() == lowercaseKey) {
						if(value != null) {
							// Replace the value, then save file
							lines[i] = fields[0] + "|" + fields[1] + "|" + value;
						}
						else {
							// Remove the element
							string[] newLines = new string[lines.Length - 1];
							Array.Copy(lines, newLines, i);
							Array.Copy(lines, i + 1, newLines, i, lines.Length - i - 1);
							lines = newLines;
						}
						File.WriteAllLines(GetFullPath(UsersDataFile), lines);
						return true;
					}
				}

				// If the program gets here, the element was not present, append it
				File.AppendAllText(GetFullPath(UsersDataFile), user.Username + "|" + key + "|" + value + "\r\n");

				return true;
			}
		}

		/// <summary>
		/// Gets a user data element, if any.
		/// </summary>
		/// <param name="user">The user the data belongs to.</param>
		/// <param name="key">The key of the data element.</param>
		/// <returns>The value of the data element, or <c>null</c> if the element is not found.</returns>
		/// <exception cref="ArgumentNullException">If <b>user</b> or <b>key</b> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>key</b> is empty.</exception>
		public string RetrieveUserData(UserInfo user, string key) {
			if(user == null) throw new ArgumentNullException("user");
			if(key == null) throw new ArgumentNullException("key");
			if(key.Length == 0) throw new ArgumentException("Key cannot be empty", "key");

			lock(this) {
				string lowercaseUsername = user.Username.ToLowerInvariant();
				string lowercaseKey = key.ToLowerInvariant();

				string[] lines = File.ReadAllLines(GetFullPath(UsersDataFile));

				string[] fields;
				foreach(string line in lines) {
					fields = line.Split('|');

					if(fields[0].ToLowerInvariant() == lowercaseUsername && fields[1].ToLowerInvariant() == lowercaseKey) {
						return fields[2];
					}
				}
			}

			return null;
		}

		/// <summary>
		/// Retrieves all the user data elements for a user.
		/// </summary>
		/// <param name="user">The user.</param>
		/// <returns>The user data elements (key-&gt;value).</returns>
		/// <exception cref="ArgumentNullException">If <b>user</b> is <c>null</c>.</exception>
		public IDictionary<string, string> RetrieveAllUserData(UserInfo user) {
			if(user == null) throw new ArgumentNullException("user");

			lock(this) {
				string lowercaseUsername = user.Username.ToLowerInvariant();

				string[] lines = File.ReadAllLines(GetFullPath(UsersDataFile));

				Dictionary<string, string> result = new Dictionary<string, string>(10);

				string[] fields;
				foreach(string line in lines) {
					fields = line.Split('|');

					if(fields[0].ToLowerInvariant() == lowercaseUsername) {
						result.Add(fields[1], fields[2]);
					}
				}

				return result;
			}
		}

		/// <summary>
		/// Gets all the users that have the specified element in their data.
		/// </summary>
		/// <param name="key">The key of the data.</param>
		/// <returns>The users and the data.</returns>
		/// <exception cref="ArgumentNullException">If <b>key</b> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>key</b> is empty.</exception>
		public IDictionary<UserInfo, string> GetUsersWithData(string key) {
			if(key == null) throw new ArgumentNullException("key");
			if(key.Length == 0) throw new ArgumentException("Key cannot be empty", "key");

			lock(this) {
				UserInfo[] allUsers = GetUsers();

				string[] lines = File.ReadAllLines(GetFullPath(UsersDataFile));

				Dictionary<UserInfo, string> result = new Dictionary<UserInfo, string>(lines.Length / 4);

				string[] fields;
				foreach(string line in lines) {
					fields = line.Split('|');

					if(fields[1] == key) {
						UserInfo currentUser = Array.Find(allUsers, delegate(UserInfo user) {
							return user.Username == fields[0];
						});
						if(currentUser != null) result.Add(currentUser, fields[2]);
					}
				}

				return result;
			}
		}

	}

}
