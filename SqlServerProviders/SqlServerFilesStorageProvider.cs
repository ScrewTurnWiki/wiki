
using System;
using System.Collections.Generic;
using System.Text;
using ScrewTurn.Wiki.Plugins.SqlCommon;
using ScrewTurn.Wiki.PluginFramework;
using System.Data.SqlClient;

namespace ScrewTurn.Wiki.Plugins.SqlServer {

	/// <summary>
	/// Implements a SQL Server-based files storage provider.
	/// </summary>
	public class SqlServerFilesStorageProvider : SqlFilesStorageProviderBase {

		private readonly ComponentInformation info = new ComponentInformation("SQL Server Files Storage Provider", "Threeplicate Srl", "3.0.1.471", "http://www.screwturn.eu", "http://www.screwturn.eu/Version/SQLServerProv/Files.txt");

		private readonly SqlServerCommandBuilder commandBuilder = new SqlServerCommandBuilder();

		private const int CurrentSchemaVersion = 3000;

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
			cmd.CommandText = "select [Version] from [Version] where [Component] = 'Files'";

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
			cmd.CommandText = "select [Version] from [Version] where [Component] = 'Files'";

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
			cmd.CommandText = Properties.Resources.FilesDatabase;

			cmd.ExecuteNonQuery();

			cmd.Connection.Close();
		}

		/// <summary>
		/// Creates or updates the database schema if necessary.
		/// </summary>
		protected override void CreateOrUpdateDatabaseIfNecessary() {
			if(!SchemaExists()) {
				CreateStandardSchema();
			}
			if(SchemaNeedsUpdate()) {
				// Run minor update batches...
			}
		}

		/// <summary>
		/// Tries to load the configuration from a corresponding v2 provider.
		/// </summary>
		/// <returns>The configuration, or an empty string.</returns>
		protected override string TryLoadV2Configuration() {
			return "";
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
