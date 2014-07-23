
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
	public class SqlServerFilesStorageProviderTests : FilesStorageProviderTestScaffolding {

		//private const string ConnString = "Data Source=(local)\\SQLExpress;User ID=sa;Password=password;";
		private const string ConnString = "Data Source=(local)\\SQLExpress;Integrated Security=SSPI;";
		private const string InitialCatalog = "Initial Catalog=ScrewTurnWikiTest;";

		public override IFilesStorageProviderV30 GetProvider() {
			SqlServerFilesStorageProvider prov = new SqlServerFilesStorageProvider();
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
			cmd.CommandText = "use [ScrewTurnWikiTest]; delete from [Attachment]; delete from [File]; delete from [Directory] where [FullPath] <> '/';";
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
		}

		[Test]
		public void Init() {
			IFilesStorageProviderV30 prov = GetProvider();
			prov.Init(MockHost(), ConnString + InitialCatalog);

			Assert.IsNotNull(prov.Information, "Information should not be null");
		}

		[TestCase("", ExpectedException = typeof(InvalidConfigurationException))]
		[TestCase("blah", ExpectedException = typeof(InvalidConfigurationException))]
		[TestCase("Data Source=(local)\\SQLExpress;User ID=inexistent;Password=password;InitialCatalog=Inexistent;", ExpectedException = typeof(InvalidConfigurationException))]
		public void Init_InvalidConnString(string c) {
			IFilesStorageProviderV30 prov = GetProvider();
			prov.Init(MockHost(), c);
		}

	}

}
