
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using Rhino.Mocks;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki.Tests {

	[TestFixture]
	public abstract class UsersStorageProviderTestScaffolding {

		private MockRepository mocks = new MockRepository();
		private string testDir = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), Guid.NewGuid().ToString());

		protected IHostV30 MockHost() {
			if(!Directory.Exists(testDir)) Directory.CreateDirectory(testDir);

			IHostV30 host = mocks.DynamicMock<IHostV30>();
			Expect.Call(host.GetSettingValue(SettingName.PublicDirectory)).Return(testDir).Repeat.AtLeastOnce();

			mocks.Replay(host);

			return host;
		}

		[TearDown]
		public void TearDown() {
			try {
				Directory.Delete(testDir, true);
			}
			catch { }
		}

		public abstract IUsersStorageProviderV30 GetProvider();

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void Init_NullConfig() {
			IUsersStorageProviderV30 prov = GetProvider();
			prov.Init(MockHost(), null);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void Init_NullHost() {
			IUsersStorageProviderV30 prov = GetProvider();
			prov.Init(null, "");
		}

		[Test]
		public void AddUser_GetUsers() {
			IUsersStorageProviderV30 prov = GetProvider();

			UserInfo u1 = new UserInfo("user", "User", "email@server.com", true, DateTime.Now.AddDays(-1), prov);
			UserInfo u2 = new UserInfo("john", null, "john@john.com", false, DateTime.Now, prov);

			UserInfo u1Out = prov.AddUser(u1.Username, u1.DisplayName, "password", u1.Email, u1.Active, u1.DateTime);
			Assert.IsNotNull(u1Out, "AddUser should return something");
			AssertUserInfosAreEqual(u1, u1Out, true);

			UserInfo u2Out = prov.AddUser(u2.Username, u2.DisplayName, "password", u2.Email, u2.Active, u2.DateTime);
			Assert.IsNotNull(u2Out, "AddUser should return something");
			AssertUserInfosAreEqual(u2, u2Out, true);
			
			Assert.IsNull(prov.AddUser("user", null, "pwd999", "dummy@server.com", false, DateTime.Now), "AddUser should return false");

			UserInfo[] users = prov.GetUsers();
			Array.Sort(users, delegate(UserInfo x, UserInfo y) { return x.Username.CompareTo(y.Username); });

			Assert.AreEqual(2, users.Length, "Wrong user count");

			AssertUserInfosAreEqual(u2, users[0], true);
			AssertUserInfosAreEqual(u1, users[1], true);
		}

		private void AssertUserInfosAreEqual(UserInfo expected, UserInfo actual, bool checkProvider) {
			Assert.AreEqual(expected.Username, actual.Username, "Wrong username");
			Assert.AreEqual(expected.DisplayName, actual.DisplayName, "Wrong display name");
			Assert.AreEqual(expected.Email, actual.Email, "Wrong email");
			Assert.AreEqual(expected.Active, actual.Active, "Wrong activation status");
			Tools.AssertDateTimesAreEqual(expected.DateTime, actual.DateTime, true);
			if(checkProvider) Assert.AreSame(expected.Provider, actual.Provider, "Different provider instances");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void AddUser_InvalidUsername(string u) {
			IUsersStorageProviderV30 prov = GetProvider();

			prov.AddUser(u, null, "pass", "email@server.com", true, DateTime.Now);
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void AddUser_InvalidPassword(string p) {
			IUsersStorageProviderV30 prov = GetProvider();

			prov.AddUser("user", null, p, "email@server.com", true, DateTime.Now);
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void AddUser_InvalidEmail(string e) {
			IUsersStorageProviderV30 prov = GetProvider();

			prov.AddUser("user", null, "pass", e, true, DateTime.Now);
		}

		[Test]
		public void TestAccount() {
			IUsersStorageProviderV30 prov = GetProvider();

			UserInfo u1 = prov.AddUser("user1", null, "password", "email1@server.com", true, DateTime.Now);
			UserInfo u2 = prov.AddUser("user2", "User", "password", "email2@server.com", false, DateTime.Now);

			Assert.IsTrue(prov.TestAccount(u1, "password"), "TestAccount should return true");
			Assert.IsFalse(prov.TestAccount(new UserInfo(u1.Username.ToUpperInvariant(), null, "email1@server.com", true, DateTime.Now, prov), "password"), "TestAccount should return false");
			Assert.IsFalse(prov.TestAccount(u2, "password"), "TestAccount should return false because the account is disabled");
			Assert.IsFalse(prov.TestAccount(new UserInfo("blah", null, "email30@server.com", true, DateTime.Now, prov), "blah"), "TestAccount should return false");
			Assert.IsFalse(prov.TestAccount(u1, "password222"), "TestAccount should return false");
			Assert.IsFalse(prov.TestAccount(u1, ""), "TestAccount should return false");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void TestAccount_NullUser() {
			IUsersStorageProviderV30 prov = GetProvider();

			prov.TestAccount(null, "pass");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void TestAccount_NullPassword() {
			IUsersStorageProviderV30 prov = GetProvider();

			prov.TestAccount(new UserInfo("blah", null, "email30@server.com", true, DateTime.Now, prov), null);
		}

		[Test]
		public void ModifyUser() {
			IUsersStorageProviderV30 prov = GetProvider();

			UserInfo user = new UserInfo("username", null, "email@server.com", false, DateTime.Now, prov);
			prov.AddUser(user.Username, user.DisplayName, "password", user.Email, user.Active, user.DateTime);
			prov.AddUser("zzzz", null, "password2", "email@server2.com", false, DateTime.Now);

			// Set new password
			UserInfo expected = new UserInfo(user.Username, "New Display", "new@server.com", true, user.DateTime, prov);
			UserInfo result = prov.ModifyUser(user, "New Display", "newpass", "new@server.com", true);
			AssertUserInfosAreEqual(expected, result, true);

			UserInfo[] allUsers = prov.GetUsers();
			Assert.AreEqual(2, allUsers.Length, "Wrong user count");
			Array.Sort(allUsers, delegate(UserInfo x, UserInfo y) { return x.Username.CompareTo(y.Username); });
			AssertUserInfosAreEqual(expected, allUsers[0], true);

			Assert.IsTrue(prov.TestAccount(user, "newpass"), "TestAccount should return true");

			// Set null display name
			expected = new UserInfo(user.Username, null, "new@server.com", true, user.DateTime, prov);
			result = prov.ModifyUser(user, null, null, "new@server.com", true);
			AssertUserInfosAreEqual(expected, result, true);

			allUsers = prov.GetUsers();
			Assert.AreEqual(2, allUsers.Length, "Wrong user count");
			Array.Sort(allUsers, delegate(UserInfo x, UserInfo y) { return x.Username.CompareTo(y.Username); });
			AssertUserInfosAreEqual(expected, allUsers[0], true);

			Assert.IsTrue(prov.TestAccount(user, "newpass"), "TestAccount should return true");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void ModifyUser_NullUser() {
			IUsersStorageProviderV30 prov = GetProvider();

			prov.ModifyUser(null, "Display Name", null, "email@server.com", true);
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void ModifyUser_InvalidNewEmail(string e) {
			IUsersStorageProviderV30 prov = GetProvider();

			UserInfo user = new UserInfo("username", null, "email@server.com", true, DateTime.Now, prov);
			prov.AddUser(user.Username, user.DisplayName, "password", user.Email, user.Active, user.DateTime);

			prov.ModifyUser(user, "Display Name", null, e, false);
		}

		[Test]
		public void RemoveUser() {
			IUsersStorageProviderV30 prov = GetProvider();

			UserInfo user = prov.AddUser("user", null, "password", "email@server.com", false, DateTime.Now);

			Assert.IsFalse(prov.RemoveUser(new UserInfo("user1", "Joe", "email1@server.com", false, DateTime.Now, prov)), "RemoveUser should return false");

			Assert.IsTrue(prov.RemoveUser(user), "RemoveUser should return true");

			Assert.AreEqual(0, prov.GetUsers().Length, "Wrong user count");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RemoveUser_NullUser() {
			IUsersStorageProviderV30 prov = GetProvider();
			prov.RemoveUser(null);
		}

		private void AssertUserGroupsAreEqual(UserGroup expected, UserGroup actual, bool checkProvider) {
			Assert.AreEqual(expected.Name, actual.Name, "Wrong name");
			Assert.AreEqual(expected.Description, actual.Description, "Wrong description");
			Assert.AreEqual(expected.Users.Length, actual.Users.Length, "Wrong user count");
			for(int i = 0; i < expected.Users.Length; i++) {
				Assert.AreEqual(expected.Users[i], actual.Users[i], "Wrong user");
			}
			if(checkProvider) {
				Assert.AreSame(expected.Provider, actual.Provider, "Wrong provider");
			}
		}

		[Test]
		public void AddUserGroup_GetUserGroups() {
			IUsersStorageProviderV30 prov = GetProvider();

			UserGroup group1 = prov.AddUserGroup("Group1", "Test1");
			UserGroup expected1 = new UserGroup("Group1", "Test1", prov);
			UserGroup group2 = prov.AddUserGroup("Group2", "Test2");
			UserGroup expected2 = new UserGroup("Group2", "Test2", prov);

			Assert.IsNull(prov.AddUserGroup("Group1", "Test"), "AddUserGroup should return null");

			AssertUserGroupsAreEqual(expected1, group1, true);
			AssertUserGroupsAreEqual(expected2, group2, true);

			UserGroup[] allGroups = prov.GetUserGroups();
			Assert.AreEqual(2, allGroups.Length, "Wrong group count");
			Array.Sort(allGroups, delegate(UserGroup x, UserGroup y) { return x.Name.CompareTo(y.Name); });

			AssertUserGroupsAreEqual(expected1, allGroups[0], true);
			AssertUserGroupsAreEqual(expected2, allGroups[1], true);
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void AddUserGroup_InvalidName(string n) {
			IUsersStorageProviderV30 prov = GetProvider();
			prov.AddUserGroup(n, "Description");
		}
		
		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void AddUserGroup_NullDescription() {
			IUsersStorageProviderV30 prov = GetProvider();
			prov.AddUserGroup("Group", null);
		}

		[Test]
		public void ModifyUserGroup() {
			IUsersStorageProviderV30 prov = GetProvider();

			UserGroup group1 = prov.AddUserGroup("Group1", "Description1");
			UserGroup group2 = prov.AddUserGroup("Group2", "Description2");

			Assert.IsNull(prov.ModifyUserGroup(new UserGroup("Inexistent", "Descr", prov), "New"), "ModifyUserGroup should return null");

			prov.SetUserMembership(prov.AddUser("user", "user", "pass", "user@server.com", true, DateTime.Now), new string[] { "Group2" });

			UserGroup group2Out = prov.ModifyUserGroup(new UserGroup("Group2", "Description2", prov), "Mod");

			UserGroup expected = new UserGroup("Group2", "Mod", prov);
			expected.Users = new string[] { "user" };

			AssertUserGroupsAreEqual(expected, group2Out, true);

			UserGroup[] allGroups = prov.GetUserGroups();
			Assert.AreEqual(2, allGroups.Length, "Wrong group count");
			Array.Sort(allGroups, delegate(UserGroup x, UserGroup y) { return x.Name.CompareTo(y.Name); });

			AssertUserGroupsAreEqual(new UserGroup("Group1", "Description1", prov), allGroups[0], true);
			AssertUserGroupsAreEqual(expected, allGroups[1], true);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void ModifyUserGroup_NullGroup() {
			IUsersStorageProviderV30 prov = GetProvider();
			prov.ModifyUserGroup(null, "Description");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void ModifyUserGroup_NullDescription() {
			IUsersStorageProviderV30 prov = GetProvider();
			prov.ModifyUserGroup(prov.AddUserGroup("Group", "Description"), null);
		}

		[Test]
		public void RemoveUserGroup() {
			IUsersStorageProviderV30 prov = GetProvider();

			UserGroup group1 = prov.AddUserGroup("Group1", "Description1");
			UserGroup group2 = prov.AddUserGroup("Group2", "Description2");

			Assert.IsFalse(prov.RemoveUserGroup(new UserGroup("Inexistent", "Descr", prov)), "RemoveUserGroup should return false");

			Assert.IsTrue(prov.RemoveUserGroup(new UserGroup("Group1", "Desc", prov)), "RemoveUser should return true");

			UserGroup[] allGroups = prov.GetUserGroups();
			Assert.AreEqual(1, allGroups.Length, "Wrong group count");

			AssertUserGroupsAreEqual(group2, allGroups[0], true);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RemoveUserGroup_NullGroup() {
			IUsersStorageProviderV30 prov = GetProvider();
			prov.RemoveUserGroup(null);
		}

		[Test]
		public void SetUserMembership() {
			IUsersStorageProviderV30 prov = GetProvider();

			DateTime dt = DateTime.Now;

			UserInfo user = prov.AddUser("user", "user", "pass", "user@server.com", true, dt);
			UserGroup group1 = prov.AddUserGroup("Group1", "");
			UserGroup group2 = prov.AddUserGroup("Group2", "");

			Assert.IsNull(prov.SetUserMembership(new UserInfo("user222", "user222", "user222@server.com", true, DateTime.Now, prov), new string[0]),
				"SetUserMembership should return null");

			Assert.IsNull(prov.SetUserMembership(user, new string[] { "Group2", "Inexistent" }), "SetUserMembership should return null");

			UserInfo output = prov.SetUserMembership(user, new string[] { "Group2", "Group1" });
			AssertUserInfosAreEqual(new UserInfo("user", "user", "user@server.com", true, dt, prov), output, true);
			Assert.AreEqual(2, output.Groups.Length, "Wrong group count");
			Array.Sort(output.Groups);
			Assert.AreEqual("Group1", output.Groups[0], "Wrong group");
			Assert.AreEqual("Group2", output.Groups[1], "Wrong group");

			UserInfo[] allUsers = prov.GetUsers();
			Assert.AreEqual(2, allUsers[0].Groups.Length, "Wrong group count");
			Array.Sort(allUsers[0].Groups);
			Assert.AreEqual("Group1", allUsers[0].Groups[0], "Wrong group");
			Assert.AreEqual("Group2", allUsers[0].Groups[1], "Wrong group");

			// Also test ModifyUser
			UserInfo info = prov.ModifyUser(output, output.Username, "Pass", output.Email, output.Active);
			Array.Sort(allUsers[0].Groups);
			Assert.AreEqual("Group1", info.Groups[0], "Wrong group");
			Assert.AreEqual("Group2", info.Groups[1], "Wrong group");

			UserGroup[] allGroups = prov.GetUserGroups();

			Assert.AreEqual(2, allGroups.Length, "Wrong group count");

			UserGroup expected1 = new UserGroup("Group1", "", prov);
			expected1.Users = new string[] { "user" };
			UserGroup expected2 = new UserGroup("Group2", "", prov);
			expected2.Users = new string[] { "user" };

			Array.Sort(allGroups, delegate(UserGroup x, UserGroup y) { return x.Name.CompareTo(y.Name); });
			AssertUserGroupsAreEqual(expected1, allGroups[0], true);
			AssertUserGroupsAreEqual(expected2, allGroups[1], true);

			output = prov.SetUserMembership(user, new string[0]);
			AssertUserInfosAreEqual(new UserInfo("user", "user", "user@server.com", true, dt, prov), output, true);
			Assert.AreEqual(0, output.Groups.Length, "Wrong group count");

			allGroups = prov.GetUserGroups();

			Assert.AreEqual(2, allGroups.Length, "Wrong group count");

			expected1 = new UserGroup("Group1", "", prov);
			expected2 = new UserGroup("Group2", "", prov);

			Array.Sort(allGroups, delegate(UserGroup x, UserGroup y) { return x.Name.CompareTo(y.Name); });
			AssertUserGroupsAreEqual(expected1, allGroups[0], true);
			AssertUserGroupsAreEqual(expected2, allGroups[1], true);

			allUsers = prov.GetUsers();
			Assert.AreEqual(0, allUsers[0].Groups.Length, "Wrong group count");

			// Also test ModifyUser
			info = prov.ModifyUser(output, output.Username, "Pass", output.Email, output.Active);
			Assert.AreEqual(0, info.Groups.Length, "Wrong group count");

		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void SetUserMembership_NullUser() {
			IUsersStorageProviderV30 prov = GetProvider();
			prov.SetUserMembership(null, new string[0]);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void SetUserMembership_NullGroups() {
			IUsersStorageProviderV30 prov = GetProvider();
			prov.SetUserMembership(prov.AddUser("user", "user", "pass", "user@server.com", true, DateTime.Now), null);
		}

		[Test]
		public void TryManualLogin() {
			IUsersStorageProviderV30 prov = GetProvider();

			UserInfo user = prov.AddUser("user", null, "password", "email@server.com", true, DateTime.Now);
			prov.AddUser("user2", null, "password", "email2@server.com", false, DateTime.Now);

			UserInfo output = prov.TryManualLogin("inexistent", "password");
			Assert.IsNull(output, "TryManualLogin should return null");

			output = prov.TryManualLogin("inexistent", "");
			Assert.IsNull(output, "TryManualLogin should return null");

			output = prov.TryManualLogin("", "password");
			Assert.IsNull(output, "TryManualLogin should return null");

			output = prov.TryManualLogin("user2", "password");
			Assert.IsNull(output, "TryManualLogin should return null because the account is inactive");

			output = prov.TryManualLogin("user", "password");
			AssertUserInfosAreEqual(user, output, true);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void TryManualLogin_NullUsername() {
			IUsersStorageProviderV30 prov = GetProvider();

			prov.TryManualLogin(null, "password");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void TryManualLogin_NullPassword() {
			IUsersStorageProviderV30 prov = GetProvider();

			prov.AddUser("user", null, "password", "email@server.com", true, DateTime.Now);
			prov.TryManualLogin("user", null);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void TryAutoLogin_NullContext() {
			IUsersStorageProviderV30 prov = GetProvider();

			prov.TryAutoLogin(null);
		}

		[Test]
		public void GetUser() {
			IUsersStorageProviderV30 prov = GetProvider();

			UserInfo user = prov.AddUser("user", null, "password", "email@server.com", true, DateTime.Now);

			Assert.IsNull(prov.GetUser("inexistent"), "TryGetUser should return null");

			UserInfo output = prov.GetUser("user");

			AssertUserInfosAreEqual(user, output, true);
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void GetUser_InvalidUsername(string u) {
			IUsersStorageProviderV30 prov = GetProvider();

			prov.GetUser(u);
		}

		[Test]
		public void GetUserByEmail() {
			IUsersStorageProviderV30 prov = GetProvider();

			UserInfo user1 = prov.AddUser("user1", null, "password", "email1@server.com", true, DateTime.Now);
			UserInfo user2 = prov.AddUser("user2", null, "password", "email2@server.com", true, DateTime.Now);

			Assert.IsNull(prov.GetUserByEmail("inexistent@server.com"), "TryGetUserByEmail should return null");

			UserInfo output = prov.GetUserByEmail("email1@server.com");

			AssertUserInfosAreEqual(user1, output, true);
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void GetUserByEmail_InvalidEmail(string e) {
			IUsersStorageProviderV30 prov = GetProvider();

			prov.GetUserByEmail(e);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void NotifyCookieLogin_NullUser() {
			IUsersStorageProviderV30 prov = GetProvider();

			prov.NotifyCookieLogin(null);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void NotifyLogout_NullUser() {
			IUsersStorageProviderV30 prov = GetProvider();

			prov.NotifyLogout(null);
		}

		[Test]
		public void StoreUserData_RetrieveUserData() {
			IUsersStorageProviderV30 prov = GetProvider();

			UserInfo user = new UserInfo("User", "User", "user@users.com", true, DateTime.Now, prov);

			Assert.IsFalse(prov.StoreUserData(user, "Key", "Value"), "StoreUserData should return false");

			user = prov.AddUser("User", "User", "password", "user@users.com", true, DateTime.Now);

			Assert.IsTrue(prov.StoreUserData(user, "Key", "Value"), "StoreUserData should return true");
			Assert.IsTrue(prov.StoreUserData(user, "Key2", "Value2"), "StoreUserData should return true");
			string value = prov.RetrieveUserData(user, "Key");
			Assert.AreEqual("Value", value, "Wrong value");
			string value2 = prov.RetrieveUserData(user, "Key2");
			Assert.AreEqual("Value2", value2, "Wrong value");
		}

		[Test]
		public void StoreUserData_RetrieveUserData_Overwrite() {
			IUsersStorageProviderV30 prov = GetProvider();

			UserInfo user = prov.AddUser("User", "User", "password", "user@users.com", true, DateTime.Now);

			Assert.IsTrue(prov.StoreUserData(user, "Key", "Value1"), "StoreUserData should return true");
			Assert.IsTrue(prov.StoreUserData(user, "Key2", "Value2"), "StoreUserData should return true");
			Assert.IsTrue(prov.StoreUserData(user, "Key", "Value"), "StoreUserData should return true");
			string value = prov.RetrieveUserData(user, "Key");
			Assert.AreEqual("Value", value, "Wrong value");
			string value2 = prov.RetrieveUserData(user, "Key2");
			Assert.AreEqual("Value2", value2, "Wrong value");
		}

		[Test]
		public void StoreUserData_RetrieveUserData_NullValue() {
			IUsersStorageProviderV30 prov = GetProvider();

			UserInfo user = prov.AddUser("User", "User", "password", "user@users.com", true, DateTime.Now);

			Assert.IsTrue(prov.StoreUserData(user, "Key", "Value"), "StoreUserData should return true");
			Assert.IsTrue(prov.StoreUserData(user, "Key2", "Value2"), "StoreUserData should return true");
			Assert.IsTrue(prov.StoreUserData(user, "Key", null), "StoreUserData should return true");

			string value = prov.RetrieveUserData(user, "Key");
			Assert.IsNull(value, "Wrong value");
			string value2 = prov.RetrieveUserData(user, "Key2");
			Assert.AreEqual("Value2", value2, "Wrong value");
		}

		[Test]
		public void StoreUserData_RetrieveUserData_EmptyValue() {
			IUsersStorageProviderV30 prov = GetProvider();

			UserInfo user = prov.AddUser("User", "User", "password", "user@users.com", true, DateTime.Now);

			Assert.IsTrue(prov.StoreUserData(user, "Key", "Value"), "StoreUserData should return true");
			Assert.IsTrue(prov.StoreUserData(user, "Key2", "Value2"), "StoreUserData should return true");
			Assert.IsTrue(prov.StoreUserData(user, "Key", ""), "StoreUserData should return true");

			string value = prov.RetrieveUserData(user, "Key");
			Assert.AreEqual("", value, "Wrong value");
			string value2 = prov.RetrieveUserData(user, "Key2");
			Assert.AreEqual("Value2", value2, "Wrong value");
		}

		[Test]
		public void StoreUserData_RetrieveUserData_RemoveUser() {
			IUsersStorageProviderV30 prov = GetProvider();

			UserInfo user = prov.AddUser("User", "User", "password", "user@users.com", true, DateTime.Now);
			UserInfo user2 = prov.AddUser("User2", "User2", "password2", "user2@users.com", true, DateTime.Now);

			Assert.IsTrue(prov.StoreUserData(user, "Key", "Value"), "StoreUserData should return true");
			Assert.IsTrue(prov.StoreUserData(user2, "Key", "Value"), "StoreUserData should return true");
			prov.RemoveUser(user);

			string value = prov.RetrieveUserData(user, "Key");
			Assert.IsNull(value, "Wrong value");
			string value2 = prov.RetrieveUserData(user2, "Key");
			Assert.AreEqual(value2, "Value", "Wrong value");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void StoreUserData_NullUser() {
			IUsersStorageProviderV30 prov = GetProvider();

			prov.StoreUserData(null, "Key", "Value");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void StoreUserData_InvalidKey(string k) {
			IUsersStorageProviderV30 prov = GetProvider();

			UserInfo user = new UserInfo("User", "User", "user@users.com", true, DateTime.Now, prov);

			prov.StoreUserData(user, k, "Value");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RetrieveUserData_NullUser() {
			IUsersStorageProviderV30 prov = GetProvider();

			prov.RetrieveUserData(null, "Key");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void RetrieveUserData_InvalidKey(string k) {
			IUsersStorageProviderV30 prov = GetProvider();

			UserInfo user = prov.AddUser("User", "User", "password", "user@users.com", true, DateTime.Now);

			prov.RetrieveUserData(user, k);
		}

		[Test]
		public void RetrieveUserData_InexistentKey() {
			IUsersStorageProviderV30 prov = GetProvider();

			UserInfo user = prov.AddUser("User", "User", "password", "user@users.com", true, DateTime.Now);

			Assert.IsNull(prov.RetrieveUserData(user, "Inexistent"), "RetrieveUserData should return null");
		}

		[Test]
		public void GetUsersWithData() {
			IUsersStorageProviderV30 prov = GetProvider();

			UserInfo user1 = prov.AddUser("user1", "User1", "password", "user1@users.com", true, DateTime.Now);
			UserInfo user2 = prov.AddUser("user2", "User2", "password", "user2@users.com", true, DateTime.Now);
			UserInfo user3 = prov.AddUser("user3", "User3", "password", "user3@users.com", true, DateTime.Now);
			UserInfo user4 = prov.AddUser("user4", "User4", "password", "user4@users.com", true, DateTime.Now);

			Assert.AreEqual(0, prov.GetUsersWithData("Key").Count, "Wrong user count");

			prov.StoreUserData(user1, "Key", "Value");
			prov.StoreUserData(user2, "Key2", "Value");
			prov.StoreUserData(user4, "Key", "Value2");

			IDictionary<UserInfo, string> data = prov.GetUsersWithData("Key");

			Assert.AreEqual(2, data.Count, "Wrong user count");

			UserInfo[] users = new UserInfo[data.Count];
			data.Keys.CopyTo(users, 0);

			AssertUserInfosAreEqual(user1, users[0], true);
			AssertUserInfosAreEqual(user4, users[1], true);

			Assert.AreEqual("Value", data[users[0]], "Wrong data");
			Assert.AreEqual("Value2", data[users[1]], "Wrong data");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void GetUsersWithData_InvalidKey(string k) {
			IUsersStorageProviderV30 prov = GetProvider();

			prov.GetUsersWithData(k);
		}

		[Test]
		public void RetrieveAllUserData() {
			IUsersStorageProviderV30 prov = GetProvider();

			Assert.AreEqual(0, prov.RetrieveAllUserData(new UserInfo("Inexistent", "Inex", "inex@users.com", true, DateTime.Now, prov)).Count, "Wrong data count");

			UserInfo user1 = prov.AddUser("user1", "User1", "password", "user1@users.com", true, DateTime.Now);
			UserInfo user2 = prov.AddUser("user2", "User2", "password", "user2@users.com", true, DateTime.Now);

			Assert.AreEqual(0, prov.RetrieveAllUserData(user1).Count, "Wrong data count");

			prov.StoreUserData(user1, "Key", "Value");
			prov.StoreUserData(user1, "Key2", "Value2");
			prov.StoreUserData(user2, "Key", "Value3");

			IDictionary<string, string> data = prov.RetrieveAllUserData(user1);
			Assert.AreEqual(2, data.Count, "Wrong data count");
			Assert.AreEqual("Value", data["Key"], "Wrong data");
			Assert.AreEqual("Value2", data["Key2"], "Wrong data");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RetrieveAllUserData_NullUser() {
			IUsersStorageProviderV30 prov = GetProvider();

			prov.RetrieveAllUserData(null);
		}

	}

}
