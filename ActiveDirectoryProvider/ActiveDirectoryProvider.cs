using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using System.Web;
using ScrewTurn.Wiki.PluginFramework;
using System.Text;
using System.DirectoryServices.ActiveDirectory;

namespace ScrewTurn.Wiki.Plugins.ActiveDirectory {
	/// <summary>
	/// Implements a Users Storage Provider for Active Directory.
	/// </summary>
	public class ActiveDirectoryProvider : IUsersStorageProviderV30 {
		private const string HELP_HELP =
			"<b>Configuration Settings:</b><br/>" +
			"Map one or more domain groups to wiki groups (Users, Administrators, etc.):" +
			"<ul>" +
			"<li>GroupMap=somedomaingroup:somewikigroup[,anotherwikigroup[,...]]</li>" +
			"</ul>" +
			"Give all users membership in common wiki groups (Users, etc.):" +
			"<ul>" +
			"<li>CommonGroups=somewikigroup[,anotherwikigroup[,...]]</li>" +
			"</ul>" +
			"Give users with no wiki group membership default wiki groups (Users, etc.):" +
			"<ul>" +
			"<li>DefaultGroups=somewikigroup[,anotherwikigroup[,...]]</li>" +
			"</ul>" +
			"Authenticate against a domain the web server is not joined to (optional, choose one):" +
			"<ul>" +
			"<li>Domain=some.domain</li>" +
			"<li>Server=somedomaincontroller.some.domain</li>" +
			"</ul>" +
			"Query active directory as a specific user on the domain (optional):" +
			"<ul>" +
			"<li>Username=someusername</li>" +
			"<li>Password=somepassword</li>" +
			"</ul>" +
			"Automatic login without a login form (optional):" +
			"<ul>" +
			"<li>Set Authentication mode to Windows in Web.config.</li>" +
			"<li>Turn on Windows authentication on the web server.</li>" +
			"</ul>" +
			"In case the user doesn't have an email in his ActiveDirectory profile, sets the email to a predefined value in the form name.surname@example.com (optional):" +
			"<ul>" +
			"<li>AutomaticMail=example.com</li>" +
			"</ul>" +
			"Case insensitive login (optional):" +
			"<ul>" +
			"<li>CaseInsensitive</li>" +
			"</ul>" +
			"Comments start with a semicolon \";\".";

		private IHostV30 m_Host;
		private IUsersStorageProviderV30 m_StorageProvider;
		private Random m_Random;
		private Config m_Config;

		private class Config {
			public string ServerName;
			public string DomainName;
			public string Username;
			public string Password;
			public string AutomaticMail;
			public bool CaseInsensitive;

			private Dictionary<string, string[]> GroupMap = new Dictionary<string, string[]>(StringComparer.InvariantCultureIgnoreCase);

			/// <summary>
			/// Gets the domain specified by the config or else the computer domain.
			/// </summary>
			/// <returns>The Domain object.</returns>
			public Domain GetDomain() {
				return GetDomain(Username, Password);
			}

			/// <summary>
			/// Gets the domain specified by the config or else the computer domain using the given credentials (if any).
			/// </summary>
			/// <param name="username">The username.</param>
			/// <param name="password">The password.</param>
			/// <returns>The Domain object.</returns>
			public Domain GetDomain(string username, string password) {
				DirectoryContext context = null;

				if(ServerName != null) {
					context = new DirectoryContext(DirectoryContextType.DirectoryServer, ServerName, username, password);
				}
				else {
					context = new DirectoryContext(DirectoryContextType.Domain, DomainName, username, password);
				}

				return Domain.GetDomain(context);
			}

			/// <summary>
			/// Determine if the given user credentials are valid.
			/// </summary>
			/// <param name="username">The username.</param>
			/// <param name="password">The password.</param>
			/// <returns><c>true</c> if credentials are valid otherwise <c>false</c>.</returns>
			public bool ValidateCredentials(string username, string password) {
				try {
					using(Domain domain = GetDomain(username, password)) {
						return true;
					}
				}

				catch {
					return false;
				}
			}

			/// <summary>
			/// Get the wiki groups for the given domain groups, if any.
			/// </summary>
			/// <param name="domainGroups">The domain groups.</param>
			/// <returns>The list of wiki groups.</returns>
			public List<string> GetWikiGroups(List<string> domainGroups) {
				// find all the wiki groups from the given domain groups
				var wikiGroups = domainGroups.SelectMany(t => GetGroupMap(t)).ToList();

				// add groups common to all users
				wikiGroups.AddRange(CommonGroups);

				// remove duplicates
				wikiGroups = wikiGroups.Distinct(StringComparer.InvariantCultureIgnoreCase).ToList();

				// return the groups, if any
				if(wikiGroups.Count > 0)
					return wikiGroups;

				// return the default groups, if any
				return DefaultGroups.ToList();
			}

			/// <summary>
			/// Get the wiki groups for the given domain group, if any.
			/// </summary>
			/// <param name="domainGroup">The domain group.</param>
			/// <returns>The array of string containing the wiki groups.</returns>
			private string[] GetGroupMap(string domainGroup) {
				string[] wikiGroups;

				if(GroupMap.TryGetValue(domainGroup, out wikiGroups))
					return wikiGroups;

				return new string[0];
			}

			/// <summary>
			/// Adds the given group map.
			/// </summary>
			/// <param name="domainGroup">The domain group.</param>
			/// <param name="wikiGroups">The wiki groups.</param>
			public void AddGroupMap(string domainGroup, string[] wikiGroups) {
				if(wikiGroups == null || wikiGroups.Length == 0)
					GroupMap.Remove(domainGroup);
				else
					GroupMap[domainGroup] = wikiGroups;
			}

			/// <summary>
			/// Returns true if the group map contains at least one entry.
			/// </summary>
			/// <value>
			/// 	<c>true</c> if this instance is group map set; otherwise, <c>false</c>.
			/// </value>
			public bool IsGroupMapSet { get { return GroupMap.Count > 0; } }

			/// <summary>
			/// Groups that are common to all users, regardless of their domain group membership.
			/// </summary>
			/// <value>The common groups.</value>
			private static readonly string CommonKey = String.Empty;
			public string[] CommonGroups {
				get { return GetGroupMap(CommonKey); }
				set { AddGroupMap(CommonKey, value); }
			}

			/// <summary>
			/// Groups that are used when a user has no other group membership.
			/// </summary>
			/// <value>The default groups.</value>
			private static readonly string DefaultKey = ".DEFAULT!";
			public string[] DefaultGroups {
				get { return GetGroupMap(DefaultKey); }
				set { AddGroupMap(DefaultKey, value); }
			}
		}

		/// <summary>
		/// Tests a Password for a User account.
		/// </summary>
		/// <param name="user">The User account.</param>
		/// <param name="password">The Password to test.</param>
		/// <returns>True if the Password is correct.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="user"/> or <paramref name="password"/> are <c>null</c>.</exception>
		public bool TestAccount(UserInfo user, string password) {
			return false;
		}

		/// <summary>
		/// Gets the complete list of Users.
		/// </summary>
		/// <returns>All the Users, sorted by username.</returns>
		public UserInfo[] GetUsers() {
			return new UserInfo[] { };
		}

		/// <summary>
		/// Adds a new User.
		/// </summary>
		/// <param name="username">The Username.</param>
		/// <param name="displayName">The display name (can be <c>null</c>).</param>
		/// <param name="password">The Password.</param>
		/// <param name="email">The Email address.</param>
		/// <param name="active">A value indicating whether the account is active.</param>
		/// <param name="dateTime">The Account creation Date/Time.</param>
		/// <returns>
		/// The correct <see cref="T:UserInfo"/> object or <c>null</c>.
		/// </returns>
		/// <exception cref="ArgumentNullException">If <paramref name="username"/>, <paramref name="password"/> or <paramref name="email"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="username"/>, <paramref name="password"/> or <paramref name="email"/> are empty.</exception>
		public UserInfo AddUser(string username, string displayName, string password, string email, bool active, DateTime dateTime) {
			throw new NotImplementedException();
		}

		/// <summary>
		/// Modifies a User.
		/// </summary>
		/// <param name="user">The Username of the user to modify.</param>
		/// <param name="newDisplayName">The new display name (can be <c>null</c>).</param>
		/// <param name="newPassword">The new Password (<c>null</c> or blank to keep the current password).</param>
		/// <param name="newEmail">The new Email address.</param>
		/// <param name="newActive">A value indicating whether the account is active.</param>
		/// <returns>
		/// The correct <see cref="T:UserInfo"/> object or <c>null</c>.
		/// </returns>
		/// <exception cref="ArgumentNullException">If <paramref name="user"/> or <paramref name="newEmail"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="newEmail"/> is empty.</exception>
		public UserInfo ModifyUser(UserInfo user, string newDisplayName, string newPassword, string newEmail, bool newActive) {
			throw new NotImplementedException();
		}

		/// <summary>
		/// Removes a User.
		/// </summary>
		/// <param name="user">The User to remove.</param>
		/// <returns>
		/// True if the User has been removed successfully.
		/// </returns>
		/// <exception cref="ArgumentNullException">If <paramref name="user"/> is <c>null</c>.</exception>
		public bool RemoveUser(UserInfo user) {
			throw new NotImplementedException();
		}

		/// <summary>
		/// Gets all the user groups.
		/// </summary>
		/// <returns>All the groups, sorted by name.</returns>
		public UserGroup[] GetUserGroups() {
			return new UserGroup[] { };
		}

		/// <summary>
		/// Adds a new user group.
		/// </summary>
		/// <param name="name">The name of the group.</param>
		/// <param name="description">The description of the group.</param>
		/// <returns>
		/// The correct <see cref="T:UserGroup"/> object or <c>null</c>.
		/// </returns>
		/// <exception cref="ArgumentNullException">If <paramref name="name"/> or <paramref name="description"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="name"/> is empty.</exception>
		public UserGroup AddUserGroup(string name, string description) {
			throw new NotImplementedException();
		}

		/// <summary>
		/// Modifies a user group.
		/// </summary>
		/// <param name="group">The group to modify.</param>
		/// <param name="description">The new description of the group.</param>
		/// <returns>
		/// The correct <see cref="T:UserGroup"/> object or <c>null</c>.
		/// </returns>
		/// <exception cref="ArgumentNullException">If <paramref name="group"/> or <paramref name="description"/> are <c>null</c>.</exception>
		public UserGroup ModifyUserGroup(UserGroup group, string description) {
			throw new NotImplementedException();
		}

		/// <summary>
		/// Removes a user group.
		/// </summary>
		/// <param name="group">The group to remove.</param>
		/// <returns>
		/// 	<c>true</c> if the group is removed, <c>false</c> otherwise.
		/// </returns>
		/// <exception cref="ArgumentNullException">If <paramref name="group"/> is <c>null</c>.</exception>
		public bool RemoveUserGroup(UserGroup group) {
			throw new NotImplementedException();
		}

		/// <summary>
		/// Sets the group memberships of a user account.
		/// </summary>
		/// <param name="user">The user account.</param>
		/// <param name="groups">The groups the user account is member of.</param>
		/// <returns>
		/// The correct <see cref="T:UserGroup"/> object or <c>null</c>.
		/// </returns>
		/// <exception cref="ArgumentNullException">If <paramref name="user"/> or <paramref name="groups"/> are <c>null</c>.</exception>
		public UserInfo SetUserMembership(UserInfo user, string[] groups) {
			throw new NotImplementedException();
		}

		/// <summary>
		/// Tries to login a user directly through the provider.
		/// </summary>
		/// <param name="username">The username.</param>
		/// <param name="password">The password.</param>
		/// <returns>
		/// The correct UserInfo object, or <c>null</c>.
		/// </returns>
		/// <exception cref="ArgumentNullException">If <paramref name="username"/> or <paramref name="password"/> are <c>null</c>.</exception>
		public UserInfo TryManualLogin(string username, string password) {
			var info = StorageProvider.TryManualLogin(username, password);

			if(info != null)
				return info;

			if(m_Config.ValidateCredentials(username, password))
				return GetUser(username);

			return null;
		}

		/// <summary>
		/// Tries to login a user directly through the provider using
		/// the current HttpContext and without username/password.
		/// </summary>
		/// <param name="context">The current HttpContext.</param>
		/// <returns>
		/// The correct UserInfo object, or <c>null</c>.
		/// </returns>
		/// <exception cref="ArgumentNullException">If <paramref name="context"/> is <c>null</c>.</exception>
		public UserInfo TryAutoLogin(HttpContext context) {
			try {
				if(!context.User.Identity.IsAuthenticated)
					return null;

				var username = context.User.Identity.Name.Substring(context.User.Identity.Name.IndexOf(@"\") + 1);

				return GetUser(username);
			}

			catch {
				return null;
			}
		}

		/// <summary>
		/// Creates the primary group SID.
		/// http://dunnry.com/blog/DeterminingYourPrimaryGroupInActiveDirectoryUsingNET.aspx
		/// </summary>
		/// <param name="userSid">The user sid.</param>
		/// <param name="primaryGroupID">The primary group ID.</param>
		private void CreatePrimaryGroupSID(byte[] userSid, int primaryGroupID) {
			// convert the int into a byte array
			byte[] rid = BitConverter.GetBytes(primaryGroupID);

			// place the bytes into the user's SID byte array
			// overwriting them as necessary
			for(int i = 0; i < rid.Length; i++) {
				userSid.SetValue(rid[i], new long[] { userSid.Length - (rid.Length - i) });
			}
		}

		/// <summary>
		/// Builds the octet string.
		/// http://dunnry.com/blog/DeterminingYourPrimaryGroupInActiveDirectoryUsingNET.aspx
		/// </summary>
		/// <param name="bytes">The bytes.</param>
		/// <returns></returns>
		private string BuildOctetString(byte[] bytes) {
			StringBuilder sb = new StringBuilder();

			for(int i = 0; i < bytes.Length; i++) {
				sb.Append(bytes[i].ToString("X2"));
			}

			return sb.ToString();
		}

		private class UserProperties {
			public byte[] ObjectSid;
			public string Mail;
			public string DisplayName;
			public List<string> MemberOf;
			public int PrimaryGroupID;
		}

		/// <summary>
		/// Gets the user info object for the currently logged in user.
		/// </summary>
		/// <param name="username">The username.</param>
		/// <returns>
		/// The <see cref="T:UserInfo"/>, or <c>null</c>.
		/// </returns>
		/// <exception cref="ArgumentNullException">If <paramref name="username"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="username"/> is empty.</exception>
		public UserInfo GetUser(string username) {
			try {
				var user = StorageProvider.GetUser(username);

				if(user != null)
					return user;


				if(m_Config.CaseInsensitive) {
					UserInfo[] all = StorageProvider.GetUsers();

					int userIndex = Array.BinarySearch(all, new UserInfo(username, null, null, true, DateTime.MinValue, null), new UsernameComparer());

					if(userIndex >= 0)
						return all[userIndex];
				}


				// Active Directory Attributes
				//
				// http://msdn.microsoft.com/en-us/library/ms683980(VS.85).aspx
				//
				// Object-Sid       -> objectSid      (required, single-value)
				// Object-Class     -> objectClass    (required, multi-value)
				// Object-Category  -> objectCategory (required, single-value)
				// SAM-Account-Name -> sAMAccountName (required, single-value)
				// E-mail-Addresses -> mail           (optional, single-value)
				// Display-Name     -> displayName    (optional, single-value)
				// Is-Member-Of-DL  -> memberOf       (optional, multi-value)

				using(Domain domain = m_Config.GetDomain()) {
					SearchResult result;

					try {
						using(DirectoryEntry searchRoot = domain.GetDirectoryEntry()) {
							using(var searcher = new DirectorySearcher(searchRoot)) {
								searcher.Filter = String.Format("(&(objectClass=user)(objectCategory=person)(sAMAccountName={0}))", username);

								searcher.PropertiesToLoad.Add("objectSid");
								searcher.PropertiesToLoad.Add("mail");
								searcher.PropertiesToLoad.Add("displayName");
								searcher.PropertiesToLoad.Add("memberOf");
								searcher.PropertiesToLoad.Add("primaryGroupID");

								result = searcher.FindOne();
							}
						}

						if(result == null)
							return null;
					}

					catch(Exception ex) {
						LogEntry(LogEntryType.Error, "Unable to complete search for user \"{0}\": {1}", username, ex);
						return null;
					}

					UserProperties userProperties;

					try {
						userProperties = new UserProperties {
							ObjectSid = result.Properties["objectSid"].Cast<byte[]>().Single(),
							Mail = result.Properties["mail"].Cast<string>().SingleOrDefault(),
							DisplayName = result.Properties["displayName"].Cast<string>().SingleOrDefault(),
							MemberOf = result.Properties["memberOf"].Cast<string>().ToList(),
							PrimaryGroupID = result.Properties["primaryGroupID"].Cast<int>().Single(),
						};
					}

					catch(Exception ex) {
						LogEntry(LogEntryType.Error, "Unable to access properties for user \"{0}\": {1}", username, ex);
						return null;
					}

					if(userProperties.Mail == null) {
						if(m_Config.AutomaticMail != null && !string.IsNullOrEmpty(userProperties.DisplayName)) {
							userProperties.Mail = userProperties.DisplayName.Replace(" ", ".") + "@" + m_Config.AutomaticMail;
						}
						else {
							LogEntry(LogEntryType.Error, "Cannot login user \"{0}\" because they have no email address.", username);
							return null;
						}
					}

					CreatePrimaryGroupSID(userProperties.ObjectSid, userProperties.PrimaryGroupID);

					string primaryGroupPath = String.Format("<SID={0}>", BuildOctetString(userProperties.ObjectSid));

					userProperties.MemberOf.Add(primaryGroupPath);

					var domainGroups = new List<string>();

					foreach(string memberOfPath in userProperties.MemberOf) {
						try {
							using(var memberOfEntry = domain.GetDirectoryEntry()) {
								string basePath = memberOfEntry.Path.Remove(memberOfEntry.Path.LastIndexOf("/"));

								memberOfEntry.Path = String.Format("{0}/{1}", basePath, memberOfPath);

								var samAccountName = memberOfEntry.Properties["sAMAccountName"].Cast<string>().Single();

								domainGroups.Add(samAccountName);
							}
						}

						catch(Exception ex) {
							LogEntry(LogEntryType.Error, "Skipping group \"{0}\" due to lookup error: {1}", memberOfPath, ex);
							continue;
						}
					}

					var wikiGroups = m_Config.GetWikiGroups(domainGroups);

					if(wikiGroups.Count == 0) {
						LogEntry(LogEntryType.Error, "Refusing to create user \"{0}\" without any groups, please check the GroupMap configuration.", username);
						return null;
					}

					user = StorageProvider.AddUser(username, userProperties.DisplayName, GeneratePassword(), userProperties.Mail, true, DateTime.Now);

					if(user == null) {
						LogEntry(LogEntryType.Error, "Failed to create user \"{0}\" using provider \"{1}\", but no error was given by the provider.", username, StorageProvider.GetType());
						return null;
					}

					LogEntry(LogEntryType.General, "Created user \"{0}\" using provider \"{1}\", but no group membership has been set yet.", username, StorageProvider.GetType());

					user = StorageProvider.SetUserMembership(user, wikiGroups.ToArray());

					if(user == null) {
						LogEntry(LogEntryType.Error, "Failed to set user membership for user \"{0}\" using provider \"{1}\", but no error was given by the provider.", username, StorageProvider.GetType());
						return null;
					}

					LogEntry(LogEntryType.General, "Set user membership for user \"{0}\" using provider \"{1}\", user is ready for use.", username, StorageProvider.GetType());

					return user;
				}
			}

			catch(Exception ex) {
				LogEntry(LogEntryType.Error, "Error looking up user: {0}", ex);
				return null;
			}
		}


		/// <summary>
		/// Generate a random password of garbage since a non-zero length password is required
		/// </summary>
		/// <returns>The random password.</returns>
		private string GeneratePassword() {
			byte[] bytes = new byte[100];
			m_Random.NextBytes(bytes);
			return new String(bytes.Select(t => Convert.ToChar(t)).ToArray());
		}


		/// <summary>
		/// Gets a user account.
		/// Not Implemented - Passed Directly to the IUsersStorageProviderV30
		/// </summary>
		/// <param name="email">The email address.</param>
		/// <returns>
		/// The first user found with the specified email address, or <c>null</c>.
		/// </returns>
		/// <exception cref="ArgumentNullException">If <paramref name="email"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="email"/> is empty.</exception>
		public UserInfo GetUserByEmail(string email) {
			return StorageProvider.GetUserByEmail(email);

		}


		/// <summary>
		/// Notifies the provider that a user has logged in through the authentication cookie.
		/// Not Implemented - Passed Directly to the IUsersStorageProviderV30
		/// </summary>
		/// <param name="user">The user who has logged in.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="user"/> is <c>null</c>.</exception>
		public void NotifyCookieLogin(UserInfo user) {
			StorageProvider.NotifyCookieLogin(user);
		}


		/// <summary>
		/// Notifies the provider that a user has logged out.
		/// Not Implemented - Passed Directly to the IUsersStorageProviderV30
		/// </summary>
		/// <param name="user">The user who has logged out.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="user"/> is <c>null</c>.</exception>
		public void NotifyLogout(UserInfo user) {
			StorageProvider.NotifyLogout(user);
		}


		/// <summary>
		/// Stores a user data element, overwriting the previous one if present.
		/// Not Implemented - Passed Directly to the IUsersStorageProviderV30
		/// </summary>
		/// <param name="user">The user the data belongs to.</param>
		/// <param name="key">The key of the data element (case insensitive).</param>
		/// <param name="value">The value of the data element, <c>null</c> for deleting the data.</param>
		/// <returns>
		/// 	<c>true</c> if the data element is stored, <c>false</c> otherwise.
		/// </returns>
		/// <exception cref="ArgumentNullException">If <paramref name="user"/> or <paramref name="key"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="key"/> is empty.</exception>
		public bool StoreUserData(UserInfo user, string key, string value) {
			return StorageProvider.StoreUserData(user, key, value);
		}


		/// <summary>
		/// Gets a user data element, if any.
		/// Not Implemented - Passed Directly to the IUsersStorageProviderV30
		/// </summary>
		/// <param name="user">The user the data belongs to.</param>
		/// <param name="key">The key of the data element.</param>
		/// <returns>
		/// The value of the data element, or <c>null</c> if the element is not found.
		/// </returns>
		/// <exception cref="ArgumentNullException">If <paramref name="user"/> or <paramref name="key"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="key"/> is empty.</exception>
		public string RetrieveUserData(UserInfo user, string key) {
			return StorageProvider.RetrieveUserData(user, key);
		}


		/// <summary>
		/// Retrieves all the user data elements for a user.
		/// Not Implemented - Passed Directly to the IUsersStorageProviderV30
		/// </summary>
		/// <param name="user">The user.</param>
		/// <returns>The user data elements (key-&gt;value).</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="user"/> is <c>null</c>.</exception>
		public IDictionary<string, string> RetrieveAllUserData(UserInfo user) {
			return StorageProvider.RetrieveAllUserData(user);
		}


		/// <summary>
		/// Gets all the users that have the specified element in their data.
		/// Not Implemented - Passed Directly to the IUsersStorageProviderV30
		/// </summary>
		/// <param name="key">The key of the data.</param>
		/// <returns>The users and the data.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="key"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="key"/> is empty.</exception>
		public IDictionary<UserInfo, string> GetUsersWithData(string key) {
			return StorageProvider.GetUsersWithData(key);
		}


		/// <summary>
		/// Gets a value indicating whether user accounts are read-only.
		/// </summary>
		/// <value>True, always, we can't write back to AD</value>
		public bool UserAccountsReadOnly {
			get { return true; }
		}


		/// <summary>
		/// Gets a value indicating whether user groups are read-only. If so, the provider
		/// should support default user groups as defined in the wiki configuration.
		/// </summary>
		/// <value>True, always, we can't write back to AD</value>
		public bool UserGroupsReadOnly {
			get { return true; }
		}

		/// <summary>
		/// Gets a value indicating whether group membership is read-only (if <see cref="UserAccountsReadOnly"/>
		/// is <c>false</c>, then this property must be <c>false</c>). If this property is <c>true</c>, the provider
		/// should return membership data compatible with default user groups.
		/// </summary>
		/// <value>True, always, we can't write back to AD</value>
		public bool GroupMembershipReadOnly {
			get { return true; }
		}


		/// <summary>
		/// Gets a value indicating whether users' data is read-only.
		/// </summary>
		/// <value>True, always, we can't write back to AD</value>
		public bool UsersDataReadOnly {
			get { return true; }
		}

		/// <summary>
		/// Initializes the Storage Provider.
		/// </summary>
		/// <param name="host">The Host of the Component.</param>
		/// <param name="config">The Configuration data, if any.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="host"/> or <paramref name="config"/> are <c>null</c>.</exception>
		/// <exception cref="InvalidConfigurationException">If <paramref name="config"/> is not valid or is incorrect.</exception>
		public void Init(IHostV30 host, string config) {
			m_Host = host;
			m_Random = new Random();

			InitConfig(config);
		}


		/// <summary>
		/// Returns the storage provider.
		/// The storage provider is identified the first time it is needed, rather than at init.
		/// This avoids a dependency on the storage provider being loaded first, which is not guaranteed
		/// </summary>
		/// <value>The storage provider.</value>
		private IUsersStorageProviderV30 StorageProvider {
			get {
				if(m_StorageProvider == null) {
					lock(m_Host) {
						if(m_StorageProvider == null) {
							m_StorageProvider = (from a in m_Host.GetUsersStorageProviders(true)
												 where a.Information.Name != this.Information.Name
												 select a).FirstOrDefault();

							if(m_StorageProvider == null) {
								LogEntry(LogEntryType.Error, "This provider requires an additional active storage provider for storing of active directory user information.");
								throw new InvalidConfigurationException("This provider requires an additional active storage provider for storing of active directory user information.");
							}
						}
					}
				}

				return m_StorageProvider;
			}
		}


		/// <summary>
		/// Method invoked on shutdown.
		/// Ignored
		/// </summary>
		/// <remarks>This method might not be invoked in some cases.</remarks>
		public void Shutdown() {
			StorageProvider.Shutdown();
		}


		/// <summary>
		/// Gets the Information about the Provider.
		/// </summary>
		/// <value>The information</value>
		public ComponentInformation Information {
			get { return new ComponentInformation("Active Directory Provider", "Threeplicate Srl", "3.0.2.534", "http://www.screwturn.eu", "http://www.screwturn.eu/Version/ADProv/ADProv.txt"); }
		}


		/// <summary>
		/// Gets a brief summary of the configuration string format, in HTML. Returns <c>null</c> if no configuration is needed.
		/// </summary>
		/// <value></value>
		public string ConfigHelpHtml {
			get { return HELP_HELP; }
		}


		/// <summary>
		/// Logs a message from this plugin.
		/// </summary>
		/// <param name="entryType">Type of the entry.</param>
		/// <param name="message">The message.</param>
		/// <param name="args">The args.</param>
		private void LogEntry(LogEntryType entryType, string message, params object[] args) {
			string entry = String.Format(message, args);
			m_Host.LogEntry(entry, entryType, null, this);
		}


		/// <summary>
		/// Configures the plugin based on the configuration settings.
		/// </summary>
		/// <param name="config">The config.</param>
		private void InitConfig(string config) {
			Config newConfig = ParseConfig(config);

			if(!newConfig.IsGroupMapSet) {
				LogEntry(LogEntryType.Error, "No GroupMap entries found. Please make sure at least one valid GroupMap configuration entry exists.");
				throw new InvalidConfigurationException("No GroupMap entries found. Please make sure at least one valid GroupMap configuration entry exists.");
			}

			if(newConfig.CommonGroups.Length > 0 && newConfig.DefaultGroups.Length > 0) {
				LogEntry(LogEntryType.Warning, "DefaultGroups will be ignored because CommonGroups have been configured.");
				newConfig.DefaultGroups = null;
			}

			if(newConfig.ServerName != null) {
				if(newConfig.DomainName != null) {
					LogEntry(LogEntryType.Warning,
						"Domain and Server config keys are mutually exclusive, but both were given. " +
						"The Domain entry will be ignored.");

					newConfig.DomainName = null;
				}

				LogEntry(LogEntryType.General,
					"Configured to use domain controller \"{0}\".",
					newConfig.ServerName);
			}
			else {
				if(newConfig.DomainName == null) {
					try {
						newConfig.DomainName = Domain.GetComputerDomain().Name;
					}

					catch(Exception ex) {
						LogEntry(LogEntryType.Error, "Unable to auto-detect the computer domain: {0}", ex);
						throw new InvalidConfigurationException("Unable to auto-detect the computer domain.", ex);
					}

					LogEntry(LogEntryType.General,
						"Domain \"{0}\" was determined through auto-detection.",
						newConfig.DomainName);
				}
				else {
					LogEntry(LogEntryType.General,
						"Configured to use domain \"{0}\".",
						newConfig.DomainName);
				}
			}

			try {
				using(Domain domain = newConfig.GetDomain()) {
				}
			}

			catch(Exception ex) {
				LogEntry(LogEntryType.Error, "Unable to connect to active directory with configured username and password (if any): {0}", ex);
				throw new InvalidConfigurationException("Unable to connect to active directory with configured username and password (if any).", ex);
			}

			m_Config = newConfig;
		}


		/// <summary>
		/// Parses the plugin configuration string.
		/// </summary>
		/// <param name="config">The config.</param>
		/// <returns>A Config object representig the configuration string.</returns>
		private Config ParseConfig(string config) {
			Config newConfig = new Config();

			try {
				string[] configLines = config.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

				foreach(string configLine in configLines) {
					string remainingConfigLine = configLine.Split(new[] { ';' }).First();

					if(remainingConfigLine.Length == 0)
						continue;

					string[] configEntry = remainingConfigLine.Split(new[] { '=' }, 2);

					if(configEntry.Length == 1 && configEntry[0].ToLowerInvariant() == "caseinsensitive") {
						newConfig.CaseInsensitive = true;
						continue;
					}

					if(configEntry.Length != 2) {
						LogEntry(LogEntryType.Error,
							"Config lines must be in the format \"Key=Value\". " +
							"The config line \"{0}\" will be ignored.",
							configLine);
						continue;
					}

					string key = configEntry[0].Trim().ToLowerInvariant();
					string value = configEntry[1].Trim();

					switch(key) {
						case "server":
							newConfig.ServerName = value;
							break;

						case "username":
							newConfig.Username = value;
							break;

						case "password":
							newConfig.Password = value;
							break;

						case "domain":
							newConfig.DomainName = value;
							break;

						case "groupmap":
							string[] groupMap = value.Split(new[] { ':' }, 2);

							if(groupMap.Length != 2) {
								LogEntry(LogEntryType.Error,
									"GroupMap entries must be in the format \"GroupMap=DomainGroup:WikiGroup[,...]\". " +
									"The config line \"{0}\" will be ignored.",
									configLine);
								break;
							}

							string fromDomainGroup = groupMap[0];
							string[] toWikiGroups = groupMap[1].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Distinct(StringComparer.InvariantCultureIgnoreCase).ToArray();

							newConfig.AddGroupMap(fromDomainGroup, toWikiGroups);

							break;

						case "commongroups":
							string[] commonGroups = value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Distinct(StringComparer.InvariantCultureIgnoreCase).ToArray();
							newConfig.CommonGroups = commonGroups;
							break;

						case "defaultgroups":
							string[] defaultGroups = value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Distinct(StringComparer.InvariantCultureIgnoreCase).ToArray();
							newConfig.DefaultGroups = defaultGroups;
							break;

						case "automaticmail":
							newConfig.AutomaticMail = value;
							break;

						default:
							LogEntry(LogEntryType.Error,
								"Invalid config key, see help for valid options. " +
								"The config line \"{0}\" will be ignored.",
								configLine);
							break;
					}
				}

				return newConfig;
			}

			catch(Exception ex) {
				LogEntry(LogEntryType.Error, "Error parsing the configuration: {0}", ex);
				throw new InvalidConfigurationException("Error parsing the configuration.", ex);
			}
		}
	}
}
