
using System;
using System.Collections.Generic;
using System.Text;
using ScrewTurn.Wiki.PluginFramework;
using ScrewTurn.Wiki.Plugins.SqlCommon;
using System.Data.SqlClient;

namespace ScrewTurn.Wiki.Plugins.SqlServer {

	/// <summary>
	/// Implements a SQL Server-based users storage provider.
	/// </summary>
	public class SqlServerPagesStorageProvider : SqlPagesStorageProviderBase, IPagesStorageProviderV30 {

		private readonly ComponentInformation info = new ComponentInformation("SQL Server Pages Storage Provider", "Threeplicate Srl", "3.0.4.566", "http://www.screwturn.eu", "http://www.screwturn.eu/Version/SQLServerProv/Pages.txt");

		private readonly SqlServerCommandBuilder commandBuilder = new SqlServerCommandBuilder();

		private const int CurrentSchemaVersion = 3001;

		/// <summary>
		/// Gets a new command with an open connection.
		/// </summary>
		/// <param name="connString">The connection string.</param>
		/// <returns>The command.</returns>
		private SqlCommand GetCommand(string connString) {
			return commandBuilder.GetCommand(connString, "select current_user", new List<Parameter>()) as SqlCommand;
		}

		/// <summary>
		/// Gets a new command builder object.
		/// </summary>
		/// <returns>The command builder.</returns>
		protected override ICommandBuilder GetCommandBuilder() {
			return commandBuilder;
		}

		/// <summary>
		/// Validates a connection string.
		/// </summary>
		/// <param name="connString">The connection string to validate.</param>
		/// <remarks>If the connection string is invalid, the method throws <see cref="T:InvalidConfigurationException"/>.</remarks>
		protected override void ValidateConnectionString(string connString) {
			SqlCommand cmd = null;
			try {
				cmd = GetCommand(connString);
			}
			catch(SqlException ex) {
				throw new InvalidConfigurationException("Provided connection string is not valid", ex);
			}
			catch(InvalidOperationException ex) {
				throw new InvalidConfigurationException("Provided connection string is not valid", ex);
			}
			catch(ArgumentException ex) {
				throw new InvalidConfigurationException("Provided connection string is not valid", ex);
			}
			finally {
				try {
					cmd.Connection.Close();
				}
				catch { }
			}
		}

		/// <summary>
		/// Detects whether the database schema exists.
		/// </summary>
		/// <returns><c>true</c> if the schema exists, <c>false</c> otherwise.</returns>
		private bool SchemaExists() {
			SqlCommand cmd = GetCommand(connString);
			cmd.CommandText = "select [Version] from [Version] where [Component] = 'Pages'";

			bool exists = false;

			try {
				int version = ExecuteScalar<int>(cmd, -1);
				if(version > CurrentSchemaVersion) throw new InvalidConfigurationException("The version of the database schema is greater than the supported version");
				exists = version != -1;
			}
			catch(SqlException) {
				exists = false;
			}
			finally {
				try {
					cmd.Connection.Close();
				}
				catch { }
			}

			return exists;
		}

		/// <summary>
		/// Detects whether the database schema needs to be updated.
		/// </summary>
		/// <returns><c>true</c> if an update is needed, <c>false</c> otherwise.</returns>
		private bool SchemaNeedsUpdate() {
			SqlCommand cmd = GetCommand(connString);
			cmd.CommandText = "select [Version] from [Version] where [Component] = 'Pages'";

			bool exists = false;

			try {
				int version = ExecuteScalar<int>(cmd, -1);
				exists = version < CurrentSchemaVersion;
			}
			catch(SqlException) {
				exists = false;
			}
			finally {
				try {
					cmd.Connection.Close();
				}
				catch { }
			}

			return exists;
		}

		/// <summary>
		/// Creates the standard database schema.
		/// </summary>
		private void CreateStandardSchema() {
			SqlCommand cmd = GetCommand(connString);
			cmd.CommandText = Properties.Resources.PagesDatabase;

			cmd.ExecuteNonQuery();

			cmd.Connection.Close();
		}

		/// <summary>
		/// Creates or updates the database schema if necessary.
		/// </summary>
		protected override void CreateOrUpdateDatabaseIfNecessary() {
			if(!SchemaExists()) {
				// Verify if an upgrade from version 2.0 is possible
				if(SchemaAllowsUpgradeFrom20()) {
					UpgradeFrom20();
				}
				else {
					// If not, create the standard schema
					CreateStandardSchema();
				}
			}
			if(SchemaNeedsUpdate()) {
				// Run minor update batches...
				Update3000to3001();
				// Other update batches
			}
		}

		/// <summary>
		/// Updates the database schema from version 3000 to version 3001.
		/// </summary>
		private void Update3000to3001() {
			SqlCommand cmd = GetCommand(connString);
			cmd.CommandText = Properties.Resources.PagesDatabase_3000to3001;

			cmd.ExecuteNonQuery();

			cmd.Connection.Close();
		}

		/// <summary>
		/// Detects whether an upgrade is possible from version 2.0.
		/// </summary>
		/// <returns><c>true</c> if the upgrade is possible, <c>false</c> otherwise.</returns>
		private bool SchemaAllowsUpgradeFrom20() {
			// Look for 'PagesProviderVersion' table
			SqlCommand cmd = GetCommand(connString);
			cmd.CommandText = "select count(*) from sys.tables where [name] = 'PagesProviderVersion'";

			int count = ExecuteScalar<int>(cmd, -1);

			return count == 1;
		}

		/// <summary>
		/// Upgrades the database schema and data from version 2.0.
		/// </summary>
		private void UpgradeFrom20() {
			// Procedure
			// 1. Rename old tables (_v2) and create new schema
			// 2. Copy snippets
			// 3. Copy pages
			// 4. Copy messages
			// 5. Copy categories
			// 6. Copy category bindings
			// 7. Copy nav paths
			// 8. Rename offending pages and categories (names with dots) leveraging cascaded FKs
			// 9. Update security to use ACL

			SqlCommand cmd = GetCommand(connString);
			cmd.CommandText =
@"exec sp_rename 'Page', 'Page_v2';
exec sp_rename 'PageContent', 'PageContent_v2';
exec sp_rename 'Category', 'Category_v2';
exec sp_rename 'CategoryBinding', 'CategoryBinding_v2';
exec sp_rename 'Message', 'Message_v2';
exec sp_rename 'Snippet', 'Snippet_v2';
exec sp_rename 'NavigationPath', 'NavigationPath_v2';
exec sp_rename 'NavigationPathBinding', 'NavigationPathBinding_v2';
exec sp_rename 'PagesProviderVersion', 'PagesProviderVersion_v2';";
			cmd.ExecuteNonQuery();
			cmd.Connection.Close();

			CreateStandardSchema();

			cmd = GetCommand(connString);
			cmd.CommandText = "insert into [Snippet] select [Name], [Content] from [Snippet_v2]";
			cmd.ExecuteNonQuery();

			Dictionary<string, char> pageStatus = new Dictionary<string, char>(500);

			cmd.CommandText = "select * from [Page_v2]";
			using(SqlDataReader reader = cmd.ExecuteReader()) {
				SqlCommand insertCmd = GetCommand(connString);
				insertCmd.CommandText = "insert into [Page] ([Name], [Namespace], [CreationDateTime]) values (@Name, '', @CreationDateTime)";

				while(reader.Read()) {
					insertCmd.Parameters.Clear();
					insertCmd.Parameters.Add(new SqlParameter("@Name", reader["Name"] as string));
					insertCmd.Parameters.Add(new SqlParameter("@CreationDateTime", (DateTime)reader["CreationDateTime"]));
					pageStatus.Add(reader["Name"] as string, (reader["Status"] as string).ToUpperInvariant()[0]);

					insertCmd.ExecuteNonQuery();
				}

				insertCmd.Connection.Close();
			}

			cmd.CommandText = "select * from [PageContent_v2]";
			using(SqlDataReader reader = cmd.ExecuteReader()) {
				SqlCommand insertCmd = GetCommand(connString);
				insertCmd.CommandText = "insert into [PageContent] ([Page], [Namespace], [Revision], [Title], [User], [LastModified], [Comment], [Content], [Description]) values (@Page, '', @Revision, @Title, @User, @LastModified, @Comment, @Content, NULL)";

				while(reader.Read()) {
					insertCmd.Parameters.Clear();
					insertCmd.Parameters.Add(new SqlParameter("@Page", reader["Page"] as string));
					insertCmd.Parameters.Add(new SqlParameter("@Revision", (short)(int)reader["Revision"]));
					insertCmd.Parameters.Add(new SqlParameter("@Title", reader["Title"] as string));
					insertCmd.Parameters.Add(new SqlParameter("@User", reader["Username"] as string));
					insertCmd.Parameters.Add(new SqlParameter("@LastModified", (DateTime)reader["DateTime"]));
					insertCmd.Parameters.Add(new SqlParameter("@Comment", reader["Comment"] as string)); // Cannot be null in v2
					insertCmd.Parameters.Add(new SqlParameter("@Content", reader["Content"] as string));

					insertCmd.ExecuteNonQuery();
				}

				insertCmd.Connection.Close();
			}

			cmd.CommandText = "select * from [Message_v2]";
			using(SqlDataReader reader = cmd.ExecuteReader()) {
				SqlCommand insertCmd = GetCommand(connString);
				insertCmd.CommandText = "insert into [Message] ([Page], [Namespace], [Id], [Parent], [Username], [Subject], [DateTime], [Body]) values (@Page, '', @Id, @Parent, @Username, @Subject, @DateTime, @Body)";

				while(reader.Read()) {
					insertCmd.Parameters.Clear();
					insertCmd.Parameters.Add(new SqlParameter("@Page", reader["Page"] as string));
					insertCmd.Parameters.Add(new SqlParameter("@Id", (short)(int)reader["ID"]));
					int parent = (int)reader["Parent"];
					if(parent == -1) insertCmd.Parameters.Add(new SqlParameter("@Parent", DBNull.Value));
					else insertCmd.Parameters.Add(new SqlParameter("@Parent", (short)parent));
					insertCmd.Parameters.Add(new SqlParameter("@Username", reader["Username"] as string));
					insertCmd.Parameters.Add(new SqlParameter("@Subject", reader["Subject"] as string));
					insertCmd.Parameters.Add(new SqlParameter("@DateTime", (DateTime)reader["DateTime"]));
					insertCmd.Parameters.Add(new SqlParameter("@Body", reader["Body"] as string));

					insertCmd.ExecuteNonQuery();
				}

				insertCmd.Connection.Close();
			}

			cmd.CommandText = "select * from [Category_v2]";
			using(SqlDataReader reader = cmd.ExecuteReader()) {
				SqlCommand insertCmd = GetCommand(connString);
				insertCmd.CommandText = "insert into [Category] ([Name], [Namespace]) values (@Name, '')";

				while(reader.Read()) {
					insertCmd.Parameters.Clear();
					insertCmd.Parameters.Add(new SqlParameter("@Name", reader["Name"] as string));

					insertCmd.ExecuteNonQuery();
				}

				insertCmd.Connection.Close();
			}

			cmd.CommandText = "select * from [CategoryBinding_v2]";
			using(SqlDataReader reader = cmd.ExecuteReader()) {
				SqlCommand insertCmd = GetCommand(connString);
				insertCmd.CommandText = "insert into [CategoryBinding] ([Namespace], [Category], [Page]) values ('', @Category, @Page)";

				while(reader.Read()) {
					insertCmd.Parameters.Clear();
					insertCmd.Parameters.Add(new SqlParameter("@Category", reader["Category"] as string));
					insertCmd.Parameters.Add(new SqlParameter("@Page", reader["Page"] as string));

					insertCmd.ExecuteNonQuery();
				}

				insertCmd.Connection.Close();
			}

			cmd.CommandText = "select * from [NavigationPathBinding_v2]";
			using(SqlDataReader reader = cmd.ExecuteReader()) {
				SqlCommand insertCmd = GetCommand(connString);
				insertCmd.CommandText = "insert into [NavigationPath] ([Name], [Namespace], [Page], [Number]) values (@Name, '', @Page, @Number)";

				while(reader.Read()) {
					insertCmd.Parameters.Clear();
					insertCmd.Parameters.Add(new SqlParameter("@Name", reader["NavigationPath"] as string));
					insertCmd.Parameters.Add(new SqlParameter("@Page", reader["Page"] as string));
					insertCmd.Parameters.Add(new SqlParameter("@Number", (short)(int)reader["Number"]));

					insertCmd.ExecuteNonQuery();
				}

				insertCmd.Connection.Close();
			}

			// Rename pages and categories
			List<string> allPages = new List<string>(500);
			List<string> allCategories = new List<string>(50);

			cmd.CommandText = "select [Name] from [Page]";
			using(SqlDataReader reader = cmd.ExecuteReader()) {
				while(reader.Read()) {
					allPages.Add(reader["Name"] as string);
				}
			}

			cmd.CommandText = "select [Name] from [Category]";
			using(SqlDataReader reader = cmd.ExecuteReader()) {
				while(reader.Read()) {
					allCategories.Add(reader["Name"] as string);
				}
			}

			cmd.CommandText = "alter table [CategoryBinding] nocheck constraint all";
			cmd.ExecuteNonQuery();

			cmd.CommandText = "update [Page] set [Name] = @NewName where [Name] = @OldName; update [CategoryBinding] set [Page] = @NewName2 where [Page] = @OldName2";
			foreach(string page in allPages) {
				if(page.Contains(".")) {
					cmd.Parameters.Clear();
					cmd.Parameters.Add(new SqlParameter("@NewName", page.Replace(".", "_")));
					cmd.Parameters.Add(new SqlParameter("@OldName", page));
					cmd.Parameters.Add(new SqlParameter("@NewName2", page.Replace(".", "_")));
					cmd.Parameters.Add(new SqlParameter("@OldName2", page));

					cmd.ExecuteNonQuery();
				}
			}

			cmd.CommandText = "alter table [CategoryBinding] with check check constraint all";
			cmd.ExecuteNonQuery();

			cmd.CommandText = "update [Category] set [Name] = @NewName where [Name] = @OldName";
			foreach(string category in allCategories) {
				if(category.Contains(".")) {
					cmd.Parameters.Clear();
					cmd.Parameters.Add(new SqlParameter("@NewName", category.Replace(".", "_")));
					cmd.Parameters.Add(new SqlParameter("@OldName", category));

					cmd.ExecuteNonQuery();
				}
			}

			cmd.Connection.Close();

			string[] keys = new string[pageStatus.Count];
			pageStatus.Keys.CopyTo(keys, 0);
			foreach(string key in keys) {
				if(key.Contains(".")) {
					char status = pageStatus[key];
					pageStatus.Remove(key);
					pageStatus.Add(key.Replace(".", "_"), status);
				}
			}

			// Setup permissions for single pages
			foreach(KeyValuePair<string, char> pair in pageStatus) {
				if(pair.Value != 'N') {
					// Need to set permissions emulating old-style behavior
					host.UpgradePageStatusToAcl(new PageInfo(pair.Key, this, DateTime.MinValue), pair.Value);
				}
			}
		}

		/// <summary>
		/// Tries to load the configuration from a corresponding v2 provider.
		/// </summary>
		/// <returns>The configuration, or an empty string.</returns>
		protected override string TryLoadV2Configuration() {
			return host.GetProviderConfiguration("ScrewTurn.Wiki.PluginPack.SqlServerPagesStorageProvider");
		}

		/// <summary>
		/// Tries to load the configuration of the corresponding settings storage provider.
		/// </summary>
		/// <returns>The configuration, or an empty string.</returns>
		protected override string TryLoadSettingsStorageProviderConfiguration() {
			return host.GetProviderConfiguration(typeof(SqlServerSettingsStorageProvider).FullName);
		}

		/// <summary>
		/// Gets the Information about the Provider.
		/// </summary>
		public override ComponentInformation Information {
			get { return info; }
		}

		/// <summary>
		/// Gets a brief summary of the configuration string format, in HTML. Returns <c>null</c> if no configuration is needed.
		/// </summary>
		public override string ConfigHelpHtml {
			get { return "Connection string format:<br /><code>Data Source=<i>Database Address and Instance</i>;Initial Catalog=<i>Database name</i>;User ID=<i>login</i>;Password=<i>password</i>;</code>"; }
		}
	
	}

}
