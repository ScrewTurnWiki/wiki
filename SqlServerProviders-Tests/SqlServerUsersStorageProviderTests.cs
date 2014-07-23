
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;
using NUnit.Framework;
using Rhino.Mocks;
using ScrewTurn.Wiki.Tests;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki.Plugins.SqlServer.Tests {

	[TestFixture]
	public class SqlServerUsersStorageProviderTests : UsersStorageProviderTestScaffolding {

		//private const string ConnString = "Data Source=(local)\\SQLExpress;User ID=sa;Password=password;";
		private const string ConnString = "Data Source=(local)\\SQLExpress;Integrated Security=SSPI;";
		private const string InitialCatalog = "Initial Catalog=ScrewTurnWikiTest;";

		public override IUsersStorageProviderV30 GetProvider() {
			SqlServerUsersStorageProvider prov = new SqlServerUsersStorageProvider();
			prov.Init(MockHost(), ConnString + InitialCatalog);
			return prov;
		}

		[TestFixtureSetUp]
		public void FixtureSetUp() {
			// Create database with no tables
			SqlConnection cn = new SqlConnection(ConnString);
			cn.Open();

			SqlCommand cmd = cn.CreateCommand();
			cmd.CommandText = "if (select count(*) from sys.databases where [Name] = 'ScrewTurnWikiTest') = 0 begin create database [ScrewTurnWikiTest] end";
			cmd.ExecuteNonQuery();

			cn.Close();
		}

		[TearDown]
		public new void TearDown() {
			base.TearDown();

			// Clear all tables
			SqlConnection cn = new SqlConnection(ConnString);
			cn.Open();

			SqlCommand cmd = cn.CreateCommand();
			cmd.CommandText = "use [ScrewTurnWikiTest]; delete from [UserData]; delete from [UserGroupMembership]; delete from [UserGroup]; delete from [User];";
			try {
				cmd.ExecuteNonQuery();
			}
			catch(SqlException sqlex) {
				Console.WriteLine(sqlex.ToString());
			}

			cn.Close();
		}

		[TestFixtureTearDown]
		public void FixtureTearDown() {
			// Delete database
			SqlConnection cn = new SqlConnection(ConnString);
			cn.Open();

			SqlCommand cmd = cn.CreateCommand();
			cmd.CommandText = "alter database [ScrewTurnWikiTest] set single_user with rollback immediate";
			try {
				cmd.ExecuteNonQuery();
			}
			catch(SqlException sqlex) {
				Console.WriteLine(sqlex.ToString());
			}

			cmd = cn.CreateCommand();
			cmd.CommandText = "drop database [ScrewTurnWikiTest]";
			try {
				cmd.ExecuteNonQuery();
			}
			catch(SqlException sqlex) {
				Console.WriteLine(sqlex.ToString());
			}

			cn.Close();

			// This is neede because the pooled connection are using a session
			// that is now invalid due to the commands executed above
			SqlConnection.ClearAllPools();
		}

		[Test]
		public void Init() {
			IUsersStorageProviderV30 prov = GetProvider();
			prov.Init(MockHost(), ConnString + InitialCatalog);

			Assert.IsNotNull(prov.Information, "Information should not be null");
		}

		[TestCase("", ExpectedException = typeof(InvalidConfigurationException))]
		[TestCase("blah", ExpectedException = typeof(InvalidConfigurationException))]
		[TestCase("Data Source=(local)\\SQLExpress;User ID=inexistent;Password=password;InitialCatalog=Inexistent;", ExpectedException = typeof(InvalidConfigurationException))]
		public void Init_InvalidConnString(string c) {
			IUsersStorageProviderV30 prov = GetProvider();
			prov.Init(MockHost(), c);
		}

		[Test]
		public void Init_Upgrade() {
			FixtureTearDown();

			SqlConnection cn = new SqlConnection(ConnString);
			cn.Open();

			SqlCommand cmd = cn.CreateCommand();
			cmd.CommandText = "create database [ScrewTurnWikiTest];";
			cmd.ExecuteNonQuery();
			cn.Close();

			cn = new SqlConnection(ConnString + InitialCatalog);
			cn.Open();

			cmd = cn.CreateCommand();
			cmd.CommandText =
@"CREATE TABLE [UsersProviderVersion] (
	[Version] varchar(12) PRIMARY KEY
);
INSERT INTO [UsersProviderVersion] ([Version]) VALUES ('Irrelevant');

CREATE TABLE [User] (
	[Username] nvarchar(128) PRIMARY KEY,
	[PasswordHash] varchar(128) NOT NULL,
	[Email] varchar(128) NOT NULL,
	[DateTime] datetime NOT NULL,
	[Active] bit NOT NULL DEFAULT ((0)),
	[Admin] bit NOT NULL DEFAULT ((0))
);

INSERT INTO [User] ([Username], [PasswordHash], [Email], [DateTime], [Active], [Admin]) values ('user', 'hash', 'email@users.com', '2008/12/27 12:12:12', 'true', 'false');
INSERT INTO [User] ([Username], [PasswordHash], [Email], [DateTime], [Active], [Admin]) values ('user2', 'hash2', 'email2@users.com', '2008/12/27 12:12:13', 'false', 'true');";

			bool done = false;
			try {
				cmd.ExecuteNonQuery();
				done = true;
			}
			catch(SqlException sqlex) {
				Console.WriteLine(sqlex.ToString());
			}
			finally {
				cn.Close();
			}

			if(!done) throw new Exception("Could not create v2 test database");

			MockRepository mocks = new MockRepository();
			IHostV30 host = mocks.DynamicMock<IHostV30>();
			Expect.Call(host.GetSettingValue(SettingName.AdministratorsGroup)).Return("Administrators").Repeat.Once();
			Expect.Call(host.GetSettingValue(SettingName.UsersGroup)).Return("Users").Repeat.Once();

			Expect.Call(host.UpgradeSecurityFlagsToGroupsAcl(null, null)).IgnoreArguments().Repeat.Times(1).Return(true);

			mocks.Replay(host);

			IUsersStorageProviderV30 prov = new SqlServerUsersStorageProvider();
			prov.Init(host, ConnString + InitialCatalog);

			mocks.Verify(host);

			UserInfo[] users = prov.GetUsers();

			Assert.AreEqual(2, users.Length, "Wrong user count");

			Assert.AreEqual("user", users[0].Username, "Wrong username");
			Assert.IsNull(users[0].DisplayName, "Display name should be null");
			Assert.AreEqual("email@users.com", users[0].Email, "Wrong email");
			Assert.AreEqual("2008/12/27 12:12:12", users[0].DateTime.ToString("yyyy'/'MM'/'dd' 'HH':'mm':'ss"), "Wrong date/time");
			Assert.IsTrue(users[0].Active, "User should be active");
			Assert.AreEqual(1, users[0].Groups.Length, "Wrong group count");
			Assert.AreEqual("Users", users[0].Groups[0], "Wrong group");

			Assert.AreEqual("user2", users[1].Username, "Wrong username");
			Assert.IsNull(users[1].DisplayName, "Display name should be null");
			Assert.AreEqual("email2@users.com", users[1].Email, "Wrong email");
			Assert.AreEqual("2008/12/27 12:12:13", users[1].DateTime.ToString("yyyy'/'MM'/'dd' 'HH':'mm':'ss"), "Wrong date/time");
			Assert.IsFalse(users[1].Active, "User should be inactive");
			Assert.AreEqual(1, users[1].Groups.Length, "Wrong group count");
			Assert.AreEqual("Administrators", users[1].Groups[0], "Wrong group");
		}

	}

}
