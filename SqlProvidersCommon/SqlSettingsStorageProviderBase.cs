
using System;
using System.Collections.Generic;
using System.Text;
using ScrewTurn.Wiki.PluginFramework;
using ScrewTurn.Wiki.AclEngine;
using System.Data.Common;

namespace ScrewTurn.Wiki.Plugins.SqlCommon {

	/// <summary>
	/// Implements a base class for a SQL settings storage provider.
	/// </summary>
	public abstract class SqlSettingsStorageProviderBase : SqlStorageProviderBase, ISettingsStorageProviderV30 {

		private const int EstimatedLogEntrySize = 100; // bytes
		private const int MaxAssemblySize = 5242880; // 5 MB
		private const int MaxParametersInQuery = 50;

		private IAclManager aclManager;

		/// <summary>
		/// Holds a value indicating whether the application was started for the first time.
		/// </summary>
		protected bool isFirstStart = false;

		/// <summary>
		/// Initializes the Storage Provider.
		/// </summary>
		/// <param name="host">The Host of the Component.</param>
		/// <param name="config">The Configuration data, if any.</param>
		/// <remarks>If the configuration string is not valid, the methoud should throw a <see cref="InvalidConfigurationException"/>.</remarks>
		public new void Init(IHostV30 host, string config) {
			base.Init(host, config);

			aclManager = new SqlAclManager(StoreEntry, DeleteEntries, RenameAclResource, RetrieveAllAclEntries, RetrieveAclEntriesForResource, RetrieveAclEntriesForSubject);
		}

		/// <summary>
		/// Gets the default users storage provider, when no value is stored in the database.
		/// </summary>
		protected abstract string DefaultUsersStorageProvider { get; }

		/// <summary>
		/// Gets the default pages storage provider, when no value is stored in the database.
		/// </summary>
		protected abstract string DefaultPagesStorageProvider { get; }

		/// <summary>
		/// Gets the default files storage provider, when no value is stored in the database.
		/// </summary>
		protected abstract string DefaultFilesStorageProvider { get; }

		#region ISettingsStorageProvider Members

		/// <summary>
		/// Retrieves the value of a Setting.
		/// </summary>
		/// <param name="name">The name of the Setting.</param>
		/// <returns>The value of the Setting, or null.</returns>
		/// <exception cref="ArgumentNullException">If <b>name</b> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>name</b> is empty.</exception>
		public string GetSetting(string name) {
			if(name == null) throw new ArgumentNullException("name");
			if(name.Length == 0) throw new ArgumentException("Name cannot be empty", "name");

			ICommandBuilder builder = GetCommandBuilder();
			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.SelectFrom("Setting");
			query = queryBuilder.Where(query, "Name", WhereOperator.Equals, "Name");

			List<Parameter> parameters = new List<Parameter>(1);
			parameters.Add(new Parameter(ParameterType.String, "Name", name));

			DbCommand command = builder.GetCommand(connString, query, parameters);

			DbDataReader reader = ExecuteReader(command);

			if(reader != null) {
				string result = null;

				if(reader.Read()) {
					result = reader["Value"] as string;
				}

				CloseReader(command, reader);

				// HACK: this allows to correctly initialize a fully SQL-based wiki instance without any user intervention
				if(string.IsNullOrEmpty(result)) {
					if(name == "DefaultUsersProvider") result = DefaultUsersStorageProvider;
					if(name == "DefaultPagesProvider") result = DefaultPagesStorageProvider;
					if(name == "DefaultFilesProvider") result = DefaultFilesStorageProvider;
				}

				return result;
			}
			else return null;
		}

		/// <summary>
		/// Stores the value of a Setting.
		/// </summary>
		/// <param name="name">The name of the Setting.</param>
		/// <param name="value">The value of the Setting. Value cannot contain CR and LF characters, which will be removed.</param>
		/// <returns>True if the Setting is stored, false otherwise.</returns>
		/// <remarks>This method stores the Value immediately.</remarks>
		public bool SetSetting(string name, string value) {
			if(name == null) throw new ArgumentNullException("name");
			if(name.Length == 0) throw new ArgumentException("Name cannot be empty", "name");

			// 1. Delete old value, if any
			// 2. Store new value

			// Nulls are converted to empty strings
			if(value == null) value = "";

			ICommandBuilder builder = GetCommandBuilder();
			DbConnection connection = builder.GetConnection(connString);
			DbTransaction transaction = BeginTransaction(connection);

			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.DeleteFrom("Setting");
			query = queryBuilder.Where(query, "Name", WhereOperator.Equals, "Name");

			List<Parameter> parameters = new List<Parameter>(1);
			parameters.Add(new Parameter(ParameterType.String, "Name", name));

			DbCommand command = builder.GetCommand(transaction, query, parameters);

			int rows = ExecuteNonQuery(command, false);

			if(rows == -1) {
				RollbackTransaction(transaction);
				return false; // Deletion command failed (0-1 are OK)
			}

			query = queryBuilder.InsertInto("Setting",
				new string[] { "Name", "Value" }, new string[] { "Name", "Value" });
			parameters = new List<Parameter>(2);
			parameters.Add(new Parameter(ParameterType.String, "Name", name));
			parameters.Add(new Parameter(ParameterType.String, "Value", value));

			command = builder.GetCommand(transaction, query, parameters);

			rows = ExecuteNonQuery(command, false);

			if(rows == 1) CommitTransaction(transaction);
			else RollbackTransaction(transaction);

			return rows == 1;
		}

		/// <summary>
		/// Gets the all the setting values.
		/// </summary>
		/// <returns>All the settings.</returns>
		public IDictionary<string, string> GetAllSettings() {
			ICommandBuilder builder = GetCommandBuilder();

			// Sorting order is not relevant
			string query = QueryBuilder.NewQuery(builder).SelectFrom("Setting");

			DbCommand command = builder.GetCommand(connString, query, new List<Parameter>());

			DbDataReader reader = ExecuteReader(command);

			if(reader != null) {
				Dictionary<string, string> result = new Dictionary<string, string>(50);

				while(reader.Read()) {
					result.Add(reader["Name"] as string, reader["Value"] as string);
				}

				CloseReader(command, reader);

				return result;
			}
			else return null;
		}

		/// <summary>
		/// Starts a Bulk update of the Settings so that a bulk of settings can be set before storing them.
		/// </summary>
		public void BeginBulkUpdate() {
			// Do nothing - currently not supported
		}

		/// <summary>
		/// Ends a Bulk update of the Settings and stores the settings.
		/// </summary>
		public void EndBulkUpdate() {
			// Do nothing - currently not supported
		}

		/// <summary>
		/// Converts an <see cref="T:EntryType" /> to its character representation.
		/// </summary>
		/// <param name="type">The <see cref="T:EntryType" />.</param>
		/// <returns>Th haracter representation.</returns>
		private static char EntryTypeToChar(EntryType type) {
			switch(type) {
				case EntryType.Error:
					return 'E';
				case EntryType.Warning:
					return 'W';
				case EntryType.General:
					return 'G';
				default:
					throw new NotSupportedException();
			}
		}

		/// <summary>
		/// Converts the character representation of an <see cref="T:EntryType" /> back to the enumeration value.
		/// </summary>
		/// <param name="c">The character representation.</param>
		/// <returns>The<see cref="T:EntryType" />.</returns>
		private static EntryType EntryTypeFromChar(char c) {
			switch(char.ToUpperInvariant(c)) {
				case 'E':
					return EntryType.Error;
				case 'W':
					return EntryType.Warning;
				case 'G':
					return EntryType.General;
				default:
					throw new NotSupportedException();
			}
		}

		/// <summary>
		/// Sanitizes a stiring from all unfriendly characters.
		/// </summary>
		/// <param name="input">The input string.</param>
		/// <returns>The sanitized result.</returns>
		private static string Sanitize(string input) {
			StringBuilder sb = new StringBuilder(input);
			sb.Replace("<", "&lt;");
			sb.Replace(">", "&gt;");
			return sb.ToString();
		}

		/// <summary>
		/// Records a message to the System Log.
		/// </summary>
		/// <param name="message">The Log Message.</param>
		/// <param name="entryType">The Type of the Entry.</param>
		/// <param name="user">The User.</param>
		/// <remarks>This method <b>should not</b> write messages to the Log using the method IHost.LogEntry.
		/// This method should also never throw exceptions (except for parameter validation).</remarks>
		/// <exception cref="ArgumentNullException">If <b>message</b> or <b>user</b> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>message</b> or <b>user</b> are empty.</exception>
		public void LogEntry(string message, EntryType entryType, string user) {
			if(message == null) throw new ArgumentNullException("message");
			if(message.Length == 0) throw new ArgumentException("Message cannot be empty", "message");
			if(user == null) throw new ArgumentNullException("user");
			if(user.Length == 0) throw new ArgumentException("User cannot be empty", "user");

			ICommandBuilder builder = GetCommandBuilder();
			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.InsertInto("Log",
				new string[] { "DateTime", "EntryType", "User", "Message" }, new string[] { "DateTime", "EntryType", "User", "Message" });

			List<Parameter> parameters = new List<Parameter>(4);
			parameters.Add(new Parameter(ParameterType.DateTime, "DateTime", DateTime.Now));
			parameters.Add(new Parameter(ParameterType.Char, "EntryType", EntryTypeToChar(entryType)));
			parameters.Add(new Parameter(ParameterType.String, "User", Sanitize(user)));
			parameters.Add(new Parameter(ParameterType.String, "Message", Sanitize(message)));

			try {
				DbCommand command = builder.GetCommand(connString, query, parameters);

				ExecuteNonQuery(command, true);

				// No transaction - accurate log sizing is not really a concern

				int logSize = LogSize;
				if(logSize > int.Parse(host.GetSettingValue(SettingName.MaxLogSize))) {
					CutLog((int)(logSize * 0.75));
				}
			}
			catch { }
		}

		/// <summary>
		/// Reduces the size of the Log to the specified size (or less).
		/// </summary>
		/// <param name="size">The size to shrink the log to (in bytes).</param>
		private void CutLog(int size) {
			ICommandBuilder builder = GetCommandBuilder();
			DbConnection connection = builder.GetConnection(connString);
			DbTransaction transaction = BeginTransaction(connection);

			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.SelectCountFrom("Log");

			DbCommand command = builder.GetCommand(transaction, query, new List<Parameter>());

			int rows = ExecuteScalar<int>(command, -1, false);

			if(rows == -1) {
				RollbackTransaction(transaction);
				return;
			}

			int estimatedSize = rows * EstimatedLogEntrySize;

			if(size < estimatedSize) {

				int difference = estimatedSize - size;
				int entriesToDelete = difference / EstimatedLogEntrySize;
				// Add 10% to avoid 1-by-1 deletion when adding new entries
				entriesToDelete += entriesToDelete / 10;

				if(entriesToDelete > 0) {
					// This code is not optimized, but it surely works in most DBMS
					query = queryBuilder.SelectFrom("Log", new string[] { "Id" });
					query = queryBuilder.OrderBy(query, new string[] { "Id" }, new Ordering[] { Ordering.Asc });

					command = builder.GetCommand(transaction, query, new List<Parameter>());

					DbDataReader reader = ExecuteReader(command);

					List<int> ids = new List<int>(entriesToDelete);

					if(reader != null) {
						while(reader.Read() && ids.Count < entriesToDelete) {
							ids.Add((int)reader["Id"]);
						}

						CloseReader(reader);
					}

					if(ids.Count > 0) {
						// Given that the IDs to delete can be many, the query is split in many chunks, each one deleting 50 items
						// This works-around the problem of too many parameters in a RPC call of SQL Server
						// See also CutRecentChangesIfNecessary

						for(int chunk = 0; chunk <= ids.Count / MaxParametersInQuery; chunk++) {
							query = queryBuilder.DeleteFrom("Log");
							List<string> parms = new List<string>(MaxParametersInQuery);
							List<Parameter> parameters = new List<Parameter>(MaxParametersInQuery);

							for(int i = chunk * MaxParametersInQuery; i < Math.Min(ids.Count, (chunk + 1) * MaxParametersInQuery); i++) {
								parms.Add("P" + i.ToString());
								parameters.Add(new Parameter(ParameterType.Int32, parms[parms.Count - 1], ids[i]));
							}

							query = queryBuilder.WhereIn(query, "Id", parms.ToArray());

							command = builder.GetCommand(transaction, query, parameters);

							if(ExecuteNonQuery(command, false) < 0) {
								RollbackTransaction(transaction);
								return;
							}
						}
					}

					CommitTransaction(transaction);
				}
			}
		}

		/// <summary>
		/// Gets all the Log Entries, sorted by date/time (oldest to newest).
		/// </summary>
		/// <returns>The Log Entries.</returns>
		public LogEntry[] GetLogEntries() {
			ICommandBuilder builder = GetCommandBuilder();
			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.SelectFrom("Log", new string[] { "DateTime", "EntryType", "User", "Message" });
			query = queryBuilder.OrderBy(query, new string[] { "DateTime" }, new Ordering[] { Ordering.Asc });

			DbCommand command = builder.GetCommand(connString, query, new List<Parameter>());

			DbDataReader reader = ExecuteReader(command);

			if(reader != null) {
				List<LogEntry> result = new List<LogEntry>(100);

				while(reader.Read()) {
					result.Add(new LogEntry(EntryTypeFromChar((reader["EntryType"] as string)[0]),
						(DateTime)reader["DateTime"], reader["Message"] as string, reader["User"] as string));
				}

				CloseReader(command, reader);

				return result.ToArray();
			}
			else return null;
		}

		/// <summary>
		/// Clear the Log.
		/// </summary>
		public void ClearLog() {
			ICommandBuilder builder = GetCommandBuilder();

			string query = QueryBuilder.NewQuery(builder).DeleteFrom("Log");

			DbCommand command = builder.GetCommand(connString, query, new List<Parameter>());

			ExecuteNonQuery(command);
		}

		/// <summary>
		/// Gets the current size of the Log, in KB.
		/// </summary>
		public int LogSize {
			get {
				ICommandBuilder builder = GetCommandBuilder();
				QueryBuilder queryBuilder = new QueryBuilder(builder);

				string query = queryBuilder.SelectCountFrom("Log");

				DbCommand command = builder.GetCommand(connString, query, new List<Parameter>());

				int rows = ExecuteScalar<int>(command, -1);

				if(rows == -1) return 0;

				int estimatedSize = rows * EstimatedLogEntrySize;

				return estimatedSize / 1024;
			}
		}

		/// <summary>
		/// Gets a meta-data item's content.
		/// </summary>
		/// <param name="item">The item.</param>
		/// <param name="tag">The tag that specifies the context (usually the namespace).</param>
		/// <returns>The content.</returns>
		public string GetMetaDataItem(MetaDataItem item, string tag) {
			if(tag == null) tag = "";

			ICommandBuilder builder = GetCommandBuilder();
			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.SelectFrom("MetaDataItem", new string[] { "Data" });
			query = queryBuilder.Where(query, "Name", WhereOperator.Equals, "Name");
			query = queryBuilder.AndWhere(query, "Tag", WhereOperator.Equals, "Tag");

			List<Parameter> parameters = new List<Parameter>(2);
			parameters.Add(new Parameter(ParameterType.String, "Name", item.ToString()));
			parameters.Add(new Parameter(ParameterType.String, "Tag", tag));

			DbCommand command = builder.GetCommand(connString, query, parameters);

			string value = ExecuteScalar<string>(command, "");

			return value;
		}

		/// <summary>
		/// Sets a meta-data items' content.
		/// </summary>
		/// <param name="item">The item.</param>
		/// <param name="tag">The tag that specifies the context (usually the namespace).</param>
		/// <param name="content">The content.</param>
		/// <returns><c>true</c> if the content is set, <c>false</c> otherwise.</returns>
		public bool SetMetaDataItem(MetaDataItem item, string tag, string content) {
			if(tag == null) tag = "";
			if(content == null) content = "";

			// 1. Delete old value, if any
			// 2. Store new value

			ICommandBuilder builder = GetCommandBuilder();
			DbConnection connection = builder.GetConnection(connString);
			DbTransaction transaction = BeginTransaction(connection);

			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.DeleteFrom("MetaDataItem");
			query = queryBuilder.Where(query, "Name", WhereOperator.Equals, "Name");
			query = queryBuilder.AndWhere(query, "Tag", WhereOperator.Equals, "Tag");

			List<Parameter> parameters = new List<Parameter>(2);
			parameters.Add(new Parameter(ParameterType.String, "Name", item.ToString()));
			parameters.Add(new Parameter(ParameterType.String, "Tag", tag));

			DbCommand command = builder.GetCommand(transaction, query, parameters);

			int rows = ExecuteNonQuery(command, false);

			if(rows == -1) {
				RollbackTransaction(transaction);
				return false;
			}

			query = queryBuilder.InsertInto("MetaDataItem", new string[] { "Name", "Tag", "Data" }, new string[] { "Name", "Tag", "Content" });

			parameters = new List<Parameter>(3);
			parameters.Add(new Parameter(ParameterType.String, "Name", item.ToString()));
			parameters.Add(new Parameter(ParameterType.String, "Tag", tag));
			parameters.Add(new Parameter(ParameterType.String, "Content", content));

			command = builder.GetCommand(transaction, query, parameters);

			rows = ExecuteNonQuery(command, false);

			if(rows == 1) CommitTransaction(transaction);
			else RollbackTransaction(transaction);

			return rows == 1;
		}

		/// <summary>
		/// Converts a <see cref="T:ScrewTurn.Wiki.PluginFramework.Change" /> to its character representation.
		/// </summary>
		/// <param name="change">The <see cref="T:ScrewTurn.Wiki.PluginFramework.Change" />.</param>
		/// <returns>The character representation.</returns>
		private static char RecentChangeToChar(ScrewTurn.Wiki.PluginFramework.Change change) {
			switch(change) {
				case ScrewTurn.Wiki.PluginFramework.Change.MessageDeleted:
					return 'M';
				case ScrewTurn.Wiki.PluginFramework.Change.MessageEdited:
					return 'E';
				case ScrewTurn.Wiki.PluginFramework.Change.MessagePosted:
					return 'P';
				case ScrewTurn.Wiki.PluginFramework.Change.PageDeleted:
					return 'D';
				case ScrewTurn.Wiki.PluginFramework.Change.PageRenamed:
					return 'N';
				case ScrewTurn.Wiki.PluginFramework.Change.PageRolledBack:
					return 'R';
				case ScrewTurn.Wiki.PluginFramework.Change.PageUpdated:
					return 'U';
				default:
					throw new NotSupportedException();
			}
		}

		/// <summary>
		/// Converts a character representation of a <see cref="T:ScrewTurn.Wiki.PluginFramework.Change" /> back to the enum value.
		/// </summary>
		/// <param name="c">The character representation.</param>
		/// <returns>The <see cref="T:ScrewTurn.Wiki.PluginFramework.Change" />.</returns>
		private static ScrewTurn.Wiki.PluginFramework.Change RecentChangeFromChar(char c) {
			switch(char.ToUpperInvariant(c)) {
				case 'M':
					return ScrewTurn.Wiki.PluginFramework.Change.MessageDeleted;
				case 'E':
					return ScrewTurn.Wiki.PluginFramework.Change.MessageEdited;
				case 'P':
					return ScrewTurn.Wiki.PluginFramework.Change.MessagePosted;
				case 'D':
					return ScrewTurn.Wiki.PluginFramework.Change.PageDeleted;
				case 'N':
					return ScrewTurn.Wiki.PluginFramework.Change.PageRenamed;
				case 'R':
					return ScrewTurn.Wiki.PluginFramework.Change.PageRolledBack;
				case 'U':
					return ScrewTurn.Wiki.PluginFramework.Change.PageUpdated;
				default:
					throw new NotSupportedException();
			}
		}

		/// <summary>
		/// Gets the recent changes of the Wiki.
		/// </summary>
		/// <returns>The recent Changes, oldest to newest.</returns>
		public RecentChange[] GetRecentChanges() {
			ICommandBuilder builder = GetCommandBuilder();
			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.SelectFrom("RecentChange", new string[] { "Page", "Title", "MessageSubject", "DateTime", "User", "Change", "Description" });
			query = queryBuilder.OrderBy(query, new string[] { "DateTime" }, new Ordering[] { Ordering.Asc });

			DbCommand command = builder.GetCommand(connString, query, new List<Parameter>());

			DbDataReader reader = ExecuteReader(command);

			if(reader != null) {
				List<RecentChange> result = new List<RecentChange>(100);

				while(reader.Read()) {
					result.Add(new RecentChange(reader["Page"] as string, reader["Title"] as string,
						GetNullableColumn<string>(reader, "MessageSubject", ""),
						(DateTime)reader["DateTime"], reader["User"] as string, RecentChangeFromChar(((string)reader["Change"])[0]),
						GetNullableColumn<string>(reader, "Description", "")));
				}

				CloseReader(command, reader);

				return result.ToArray();
			}
			else return null;
		}

		/// <summary>
		/// Adds a new change.
		/// </summary>
		/// <param name="page">The page name.</param>
		/// <param name="title">The page title.</param>
		/// <param name="messageSubject">The message subject (or <c>null</c>).</param>
		/// <param name="dateTime">The date/time.</param>
		/// <param name="user">The user.</param>
		/// <param name="change">The change.</param>
		/// <param name="descr">The description (optional).</param>
		/// <returns><c>true</c> if the change is saved, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <b>page</b>, <b>title</b> or <b>user</b> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>page</b>, <b>title</b> or <b>user</b> are empty.</exception>
		public bool AddRecentChange(string page, string title, string messageSubject, DateTime dateTime, string user, ScrewTurn.Wiki.PluginFramework.Change change, string descr) {
			if(page == null) throw new ArgumentNullException("page");
			if(page.Length == 0) throw new ArgumentException("Page cannot be empty", "page");
			if(title == null) throw new ArgumentNullException("title");
			if(title.Length == 0) throw new ArgumentException("Title cannot be empty", "title");
			if(user == null) throw new ArgumentNullException("user");
			if(user.Length == 0) throw new ArgumentException("User cannot be empty", "user");

			ICommandBuilder builder = GetCommandBuilder();

			string query = QueryBuilder.NewQuery(builder).InsertInto("RecentChange", new string[] { "Page", "Title", "MessageSubject", "DateTime", "User", "Change", "Description" },
				new string[] { "Page", "Title", "MessageSubject", "DateTime", "User", "Change", "Description" });

			List<Parameter> parameters = new List<Parameter>(7);
			parameters.Add(new Parameter(ParameterType.String, "Page", page));
			parameters.Add(new Parameter(ParameterType.String, "Title", title));
			if(!string.IsNullOrEmpty(messageSubject)) parameters.Add(new Parameter(ParameterType.String, "MessageSubject", messageSubject));
			else parameters.Add(new Parameter(ParameterType.String, "MessageSubject", DBNull.Value));
			parameters.Add(new Parameter(ParameterType.DateTime, "DateTime", dateTime));
			parameters.Add(new Parameter(ParameterType.String, "User", user));
			parameters.Add(new Parameter(ParameterType.Char, "Change", RecentChangeToChar(change)));
			if(!string.IsNullOrEmpty(descr)) parameters.Add(new Parameter(ParameterType.String, "Description", descr));
			else parameters.Add(new Parameter(ParameterType.String, "Description", DBNull.Value));

			DbCommand command = builder.GetCommand(connString, query, parameters);

			int rows = ExecuteNonQuery(command);

			CutRecentChangesIfNecessary();

			return rows == 1;
		}

		/// <summary>
		/// Cuts the recent changes if necessary.
		/// </summary>
		private void CutRecentChangesIfNecessary() {
			ICommandBuilder builder = GetCommandBuilder();
			DbConnection connection = builder.GetConnection(connString);
			DbTransaction transaction = BeginTransaction(connection);

			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.SelectCountFrom("RecentChange");

			DbCommand command = builder.GetCommand(transaction, query, new List<Parameter>());

			int rows = ExecuteScalar<int>(command, -1, false);

			int maxChanges = int.Parse(host.GetSettingValue(SettingName.MaxRecentChanges));

			if(rows > maxChanges) {
				// Remove 10% of old changes to avoid 1-by-1 deletion every time a change is made
				int entriesToDelete = maxChanges / 10;
				if(entriesToDelete > rows) entriesToDelete = rows;
				//entriesToDelete += entriesToDelete / 10;

				// This code is not optimized, but it surely works in most DBMS
				query = queryBuilder.SelectFrom("RecentChange", new string[] { "Id" });
				query = queryBuilder.OrderBy(query, new string[] { "Id" }, new Ordering[] { Ordering.Asc });

				command = builder.GetCommand(transaction, query, new List<Parameter>());

				DbDataReader reader = ExecuteReader(command);

				List<int> ids = new List<int>(entriesToDelete);

				if(reader != null) {
					while(reader.Read() && ids.Count < entriesToDelete) {
						ids.Add((int)reader["Id"]);
					}

					CloseReader(reader);
				}

				if(ids.Count > 0) {
					// Given that the IDs to delete can be many, the query is split in many chunks, each one deleting 50 items
					// This works-around the problem of too many parameters in a RPC call of SQL Server
					// See also CutLog

					for(int chunk = 0; chunk <= ids.Count / MaxParametersInQuery; chunk++) {
						query = queryBuilder.DeleteFrom("RecentChange");
						List<string> parms = new List<string>(MaxParametersInQuery);
						List<Parameter> parameters = new List<Parameter>(MaxParametersInQuery);

						for(int i = chunk * MaxParametersInQuery; i < Math.Min(ids.Count, (chunk + 1) * MaxParametersInQuery); i++) {
							parms.Add("P" + i.ToString());
							parameters.Add(new Parameter(ParameterType.Int32, parms[parms.Count - 1], ids[i]));
						}

						query = queryBuilder.WhereIn(query, "Id", parms.ToArray());

						command = builder.GetCommand(transaction, query, parameters);

						if(ExecuteNonQuery(command, false) < 0) {
							RollbackTransaction(transaction);
							return;
						}
					}
				}
			}

			CommitTransaction(transaction);
		}

		/// <summary>
		/// Lists the stored plugin assemblies.
		/// </summary>
		public string[] ListPluginAssemblies() {
			ICommandBuilder builder = GetCommandBuilder();

			// Sort order is not relevant
			string query = QueryBuilder.NewQuery(builder).SelectFrom("PluginAssembly", new string[] { "Name" });

			DbCommand command = builder.GetCommand(connString, query, new List<Parameter>());

			DbDataReader reader = ExecuteReader(command);

			if(reader != null) {
				List<string> result = new List<string>(10);

				while(reader.Read()) {
					result.Add(reader["Name"] as string);
				}

				CloseReader(command, reader);

				return result.ToArray();
			}
			else return null;
		}

		/// <summary>
		/// Stores a plugin's assembly, overwriting existing ones if present.
		/// </summary>
		/// <param name="filename">The file name of the assembly, such as "Assembly.dll".</param>
		/// <param name="assembly">The assembly content.</param>
		/// <returns><c>true</c> if the assembly is stored, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <b>filename</b> or <b>assembly</b> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>filename</b> or <b>assembly</b> are empty.</exception>
		public bool StorePluginAssembly(string filename, byte[] assembly) {
			if(filename == null) throw new ArgumentNullException("filename");
			if(filename.Length == 0) throw new ArgumentException("Filename cannot be empty", "filename");
			if(assembly == null) throw new ArgumentNullException("assembly");
			if(assembly.Length == 0) throw new ArgumentException("Assembly cannot be empty", "assembly");
			if(assembly.Length > MaxAssemblySize) throw new ArgumentException("Assembly is too big", "assembly");

			// 1. Delete old plugin assembly, if any
			// 2. Store new assembly

			ICommandBuilder builder = GetCommandBuilder();
			DbConnection connection = builder.GetConnection(connString);
			DbTransaction transaction = BeginTransaction(connection);

			DeletePluginAssembly(transaction, filename);

			string query = QueryBuilder.NewQuery(builder).InsertInto("PluginAssembly", new string[] { "Name", "Assembly" }, new string[] { "Name", "Assembly" });

			List<Parameter> parameters = new List<Parameter>(2);
			parameters.Add(new Parameter(ParameterType.String, "Name", filename));
			parameters.Add(new Parameter(ParameterType.ByteArray, "Assembly", assembly));

			DbCommand command = builder.GetCommand(transaction, query, parameters);

			int rows = ExecuteNonQuery(command, false);

			if(rows == 1) CommitTransaction(transaction);
			else RollbackTransaction(transaction);

			return rows == 1;
		}

		/// <summary>
		/// Retrieves a plugin's assembly.
		/// </summary>
		/// <param name="filename">The file name of the assembly.</param>
		/// <returns>The assembly content, or <c>null</c>.</returns>
		/// <exception cref="ArgumentNullException">If <b>filename</b> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>filename</b> is empty.</exception>
		public byte[] RetrievePluginAssembly(string filename) {
			if(filename == null) throw new ArgumentNullException("filename");
			if(filename.Length == 0) throw new ArgumentException("Filename cannot be empty", "filename");

			ICommandBuilder builder = GetCommandBuilder();
			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.SelectFrom("PluginAssembly", new string[] { "Assembly" });
			query = queryBuilder.Where(query, "Name", WhereOperator.Equals, "Name");

			List<Parameter> parameters = new List<Parameter>(1);
			parameters.Add(new Parameter(ParameterType.String, "Name", filename));

			DbCommand command = builder.GetCommand(connString, query, parameters);

			DbDataReader reader = ExecuteReader(command);

			if(reader != null) {
				byte[] result = null;

				if(reader.Read()) {
					result = GetBinaryColumn(reader, "Assembly", MaxAssemblySize);
				}

				CloseReader(command, reader);

				return result;
			}
			else return null;
		}

		/// <summary>
		/// Removes a plugin's assembly.
		/// </summary>
		/// <param name="transaction">A database transaction.</param>
		/// <param name="filename">The file name of the assembly to remove, such as "Assembly.dll".</param>
		/// <returns><c>true</c> if the assembly is removed, <c>false</c> otherwise.</returns>
		private bool DeletePluginAssembly(DbTransaction transaction, string filename) {
			ICommandBuilder builder = GetCommandBuilder();
			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.DeleteFrom("PluginAssembly");
			query = queryBuilder.Where(query, "Name", WhereOperator.Equals, "Name");

			List<Parameter> parameters = new List<Parameter>(1);
			parameters.Add(new Parameter(ParameterType.String, "Name", filename));

			DbCommand command = builder.GetCommand(transaction, query, parameters);

			int rows = ExecuteNonQuery(command, false);

			return rows == 1;
		}

		/// <summary>
		/// Removes a plugin's assembly.
		/// </summary>
		/// <param name="connection">A database connection.</param>
		/// <param name="filename">The file name of the assembly to remove, such as "Assembly.dll".</param>
		/// <returns><c>true</c> if the assembly is removed, <c>false</c> otherwise.</returns>
		private bool DeletePluginAssembly(DbConnection connection, string filename) {
			ICommandBuilder builder = GetCommandBuilder();
			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.DeleteFrom("PluginAssembly");
			query = queryBuilder.Where(query, "Name", WhereOperator.Equals, "Name");

			List<Parameter> parameters = new List<Parameter>(1);
			parameters.Add(new Parameter(ParameterType.String, "Name", filename));

			DbCommand command = builder.GetCommand(connection, query, parameters);

			int rows = ExecuteNonQuery(command, false);

			return rows == 1;
		}

		/// <summary>
		/// Removes a plugin's assembly.
		/// </summary>
		/// <param name="filename">The file name of the assembly to remove, such as "Assembly.dll".</param>
		/// <returns><c>true</c> if the assembly is removed, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <b>filename</b> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>filename</b> is empty.</exception>
		public bool DeletePluginAssembly(string filename) {
			if(filename == null) throw new ArgumentNullException(filename);
			if(filename.Length == 0) throw new ArgumentException("Filename cannot be empty", "filename");

			ICommandBuilder builder = GetCommandBuilder();
			DbConnection connection = builder.GetConnection(connString);

			bool deleted = DeletePluginAssembly(connection, filename);
			CloseConnection(connection);

			return deleted;
		}

		/// <summary>
		/// Prepares the plugin status row, if necessary.
		/// </summary>
		/// <param name="transaction">A database transaction.</param>
		/// <param name="typeName">The Type name of the plugin.</param>
		private void PreparePluginStatusRow(DbTransaction transaction, string typeName) {
			ICommandBuilder builder = GetCommandBuilder();
			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.SelectCountFrom("PluginStatus");
			query = queryBuilder.Where(query, "Name", WhereOperator.Equals, "Name");

			List<Parameter> parameters = new List<Parameter>(1);
			parameters.Add(new Parameter(ParameterType.String, "Name", typeName));

			DbCommand command = builder.GetCommand(transaction, query, parameters);

			int rows = ExecuteScalar<int>(command, -1, false);

			if(rows == -1) return;

			if(rows == 0) {
				// Insert a neutral row (enabled, empty config)

				query = queryBuilder.InsertInto("PluginStatus", new string[] { "Name", "Enabled", "Configuration" }, new string[] { "Name", "Enabled", "Configuration" });

				parameters = new List<Parameter>(3);
				parameters.Add(new Parameter(ParameterType.String, "Name", typeName));
				parameters.Add(new Parameter(ParameterType.Boolean, "Enabled", true));
				parameters.Add(new Parameter(ParameterType.String, "Configuration", ""));

				command = builder.GetCommand(transaction, query, parameters);

				ExecuteNonQuery(command, false);
			}
		}

		/// <summary>
		/// Sets the status of a plugin.
		/// </summary>
		/// <param name="typeName">The Type name of the plugin.</param>
		/// <param name="enabled">The plugin status.</param>
		/// <returns><c>true</c> if the status is stored, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <b>typeName</b> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>typeName</b> is empty.</exception>
		public bool SetPluginStatus(string typeName, bool enabled) {
			if(typeName == null) throw new ArgumentNullException("typeName");
			if(typeName.Length == 0) throw new ArgumentException("Type Name cannot be empty", "typeName");

			ICommandBuilder builder = GetCommandBuilder();
			DbConnection connection = builder.GetConnection(connString);
			DbTransaction transaction = BeginTransaction(connection);

			PreparePluginStatusRow(transaction, typeName);

			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.Update("PluginStatus", new string[] { "Enabled" }, new string[] { "Enabled" });
			query = queryBuilder.Where(query, "Name", WhereOperator.Equals, "Name");

			List<Parameter> parameters = new List<Parameter>(2);
			parameters.Add(new Parameter(ParameterType.Boolean, "Enabled", enabled));
			parameters.Add(new Parameter(ParameterType.String, "Name", typeName));

			DbCommand command = builder.GetCommand(transaction, query, parameters);

			int rows = ExecuteNonQuery(command, false);

			if(rows == 1) CommitTransaction(transaction);
			else RollbackTransaction(transaction);

			return rows == 1;
		}

		/// <summary>
		/// Gets the status of a plugin.
		/// </summary>
		/// <param name="typeName">The Type name of the plugin.</param>
		/// <returns>The status (<c>false</c> for disabled, <c>true</c> for enabled), or <c>true</c> if no status is found.</returns>
		/// <exception cref="ArgumentNullException">If <b>typeName</b> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>typeName</b> is empty.</exception>
		public bool GetPluginStatus(string typeName) {
			if(typeName == null) throw new ArgumentNullException("typeName");
			if(typeName.Length == 0) throw new ArgumentException("Type Name cannot be empty", "typeName");

			ICommandBuilder builder = GetCommandBuilder();
			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.SelectFrom("PluginStatus", new string[] { "Enabled" });
			query = queryBuilder.Where(query, "Name", WhereOperator.Equals, "Name");

			List<Parameter> parameters = new List<Parameter>(1);
			parameters.Add(new Parameter(ParameterType.String, "Name", typeName));

			DbCommand command = builder.GetCommand(connString, query, parameters);

			DbDataReader reader = ExecuteReader(command);

			bool? enabled = null;

			if(reader != null && reader.Read()) {
				if(!IsDBNull(reader, "Enabled")) enabled = (bool)reader["Enabled"];
			}
			CloseReader(command, reader);

			if(enabled.HasValue) return enabled.Value;
			else {
				if(typeName == "ScrewTurn.Wiki.UsersStorageProvider" ||
					typeName == "ScrewTurn.Wiki.PagesStorageProvider" ||
					typeName == "ScrewTurn.Wiki.FilesStorageProvider") return false;
				else return true;
			}
		}

		/// <summary>
		/// Sets the configuration of a plugin.
		/// </summary>
		/// <param name="typeName">The Type name of the plugin.</param>
		/// <param name="config">The configuration.</param>
		/// <returns><c>true</c> if the configuration is stored, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <b>typeName</b> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>typeName</b> is empty.</exception>
		public bool SetPluginConfiguration(string typeName, string config) {
			if(typeName == null) throw new ArgumentNullException("typeName");
			if(typeName.Length == 0) throw new ArgumentException("Type Name cannot be empty", "typeName");

			if(config == null) config = "";

			ICommandBuilder builder = GetCommandBuilder();
			DbConnection connection = builder.GetConnection(connString);
			DbTransaction transaction = BeginTransaction(connection);

			PreparePluginStatusRow(transaction, typeName);

			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.Update("PluginStatus", new string[] { "Configuration" }, new string[] { "Configuration" });
			query = queryBuilder.Where(query, "Name", WhereOperator.Equals, "Name");

			List<Parameter> parameters = new List<Parameter>(2);
			parameters.Add(new Parameter(ParameterType.String, "Configuration", config));
			parameters.Add(new Parameter(ParameterType.String, "Name", typeName));

			DbCommand command = builder.GetCommand(transaction, query, parameters);

			int rows = ExecuteNonQuery(command, false);

			if(rows == 1) CommitTransaction(transaction);
			else RollbackTransaction(transaction);

			return rows == 1;
		}

		/// <summary>
		/// Gets the configuration of a plugin.
		/// </summary>
		/// <param name="typeName">The Type name of the plugin.</param>
		/// <returns>The plugin configuration, or <b>String.Empty</b>.</returns>
		/// <exception cref="ArgumentNullException">If <b>typeName</b> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>typeName</b> is empty.</exception>
		public string GetPluginConfiguration(string typeName) {
			if(typeName == null) throw new ArgumentNullException("typeName");
			if(typeName.Length == 0) throw new ArgumentException("Type Name cannot be empty", "typeName");

			ICommandBuilder builder = GetCommandBuilder();
			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.SelectFrom("PluginStatus", new string[] { "Configuration" });
			query = queryBuilder.Where(query, "Name", WhereOperator.Equals, "Name");

			List<Parameter> parameters = new List<Parameter>(1);
			parameters.Add(new Parameter(ParameterType.String, "Name", typeName));

			DbCommand command = builder.GetCommand(connString, query, parameters);

			string result = ExecuteScalar<string>(command, "");

			return result;
		}

		/// <summary>
		/// Gets the ACL Manager instance.
		/// </summary>
		public IAclManager AclManager {
			get { return aclManager; }
		}

		/// <summary>
		/// Stores the outgoing links of a page, overwriting existing data.
		/// </summary>
		/// <param name="page">The full name of the page.</param>
		/// <param name="outgoingLinks">The full names of the pages that <b>page</b> links to.</param>
		/// <returns><c>true</c> if the outgoing links are stored, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <b>page</b> or <b>outgoingLinks</b> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>page</b> or <b>outgoingLinks</b> are empty.</exception>
		public bool StoreOutgoingLinks(string page, string[] outgoingLinks) {
			if(page == null) throw new ArgumentNullException("page");
			if(page.Length == 0) throw new ArgumentException("Page cannot be empty", "page");
			if(outgoingLinks == null) throw new ArgumentNullException("outgoingLinks");

			foreach(string link in outgoingLinks) {
				if(link == null) throw new ArgumentNullException("outgoingLinks");
				if(link.Length == 0) throw new ArgumentException("Link cannot be empty", "outgoingLinks");
			}

			// 1. Delete old values, if any
			// 2. Store new values

			ICommandBuilder builder = GetCommandBuilder();
			DbConnection connection = builder.GetConnection(connString);
			DbTransaction transaction = BeginTransaction(connection);

			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.DeleteFrom("OutgoingLink");
			query = queryBuilder.Where(query, "Source", WhereOperator.Equals, "Source");

			List<Parameter> parameters = new List<Parameter>(1);
			parameters.Add(new Parameter(ParameterType.String, "Source", page));

			DbCommand command = builder.GetCommand(transaction, query, parameters);

			if(ExecuteNonQuery(command, false) < 0) {
				RollbackTransaction(transaction);
				return false;
			}

			foreach(string link in outgoingLinks) {
				query = queryBuilder.InsertInto("OutgoingLink", new string[] { "Source", "Destination" }, new string[] { "Source", "Destination" });

				parameters = new List<Parameter>(2);
				parameters.Add(new Parameter(ParameterType.String, "Source", page));
				parameters.Add(new Parameter(ParameterType.String, "Destination", link));

				command = builder.GetCommand(transaction, query, parameters);

				int rows = ExecuteNonQuery(command, false);

				if(rows != 1) {
					RollbackTransaction(transaction);
					return false;
				}
			}

			CommitTransaction(transaction);
			return true;
		}

		/// <summary>
		/// Gets the outgoing links of a page.
		/// </summary>
		/// <param name="page">The full name of the page.</param>
		/// <returns>The outgoing links.</returns>
		/// <exception cref="ArgumentNullException">If <b>page</b> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>page</b> is empty.</exception>
		public string[] GetOutgoingLinks(string page) {
			if(page == null) throw new ArgumentNullException("page");
			if(page.Length == 0) throw new ArgumentException("Page cannot be empty", "page");

			ICommandBuilder builder = GetCommandBuilder();
			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.SelectFrom("OutgoingLink", new string[] { "Destination" });
			query = queryBuilder.Where(query, "Source", WhereOperator.Equals, "Source");
			query = queryBuilder.OrderBy(query, new[] { "Destination" }, new[] { Ordering.Asc });

			List<Parameter> parameters = new List<Parameter>(1);
			parameters.Add(new Parameter(ParameterType.String, "Source", page));

			DbCommand command = builder.GetCommand(connString, query, parameters);

			DbDataReader reader = ExecuteReader(command);

			if(reader != null) {
				List<string> result = new List<string>(20);

				while(reader.Read()) {
					result.Add(reader["Destination"] as string);
				}

				CloseReader(command, reader);

				return result.ToArray();
			}
			else return null;
		}

		/// <summary>
		/// Gets all the outgoing links stored.
		/// </summary>
		/// <returns>The outgoing links, in a dictionary in the form page-&gt;outgoing_links.</returns>
		public IDictionary<string, string[]> GetAllOutgoingLinks() {
			ICommandBuilder builder = GetCommandBuilder();
			QueryBuilder queryBuilder = new QueryBuilder(builder);

			// Explicit columns in order to allow usage of GROUP BY
			string query = queryBuilder.SelectFrom("OutgoingLink", new string[] { "Source", "Destination" });
			query = queryBuilder.GroupBy(query, new string[] { "Source", "Destination" });

			DbCommand command = builder.GetCommand(connString, query, new List<Parameter>());

			DbDataReader reader = ExecuteReader(command);

			if(reader != null) {
				Dictionary<string, string[]> result = new Dictionary<string, string[]>(100);

				string prevSource = "|||";
				string source = null;
				List<string> destinations = new List<string>(20);

				while(reader.Read()) {
					source = reader["Source"] as string;

					if(source != prevSource) {
						if(prevSource != "|||") {
							result.Add(prevSource, destinations.ToArray());
							destinations.Clear();
						}
					}

					prevSource = source;
					destinations.Add(reader["Destination"] as string);
				}

				result.Add(prevSource, destinations.ToArray());
				
				CloseReader(command, reader);

				return result;
			}
			else return null;
		}

		/// <summary>
		/// Deletes the outgoing links of a page and all the target links that include the page.
		/// </summary>
		/// <param name="page">The full name of the page.</param>
		/// <returns><c>true</c> if the links are deleted, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <b>page</b> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>page</b> is empty.</exception>
		public bool DeleteOutgoingLinks(string page) {
			if(page == null) throw new ArgumentNullException("page");
			if(page.Length == 0) throw new ArgumentException("Page cannot be empty", "page");

			ICommandBuilder builder = GetCommandBuilder();
			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.DeleteFrom("OutgoingLink");
			query = queryBuilder.Where(query, "Source", WhereOperator.Equals, "Source");
			query = queryBuilder.OrWhere(query, "Destination", WhereOperator.Equals, "Destination");

			List<Parameter> parameters = new List<Parameter>(2);
			parameters.Add(new Parameter(ParameterType.String, "Source", page));
			parameters.Add(new Parameter(ParameterType.String, "Destination", page));

			DbCommand command = builder.GetCommand(connString, query, parameters);

			int rows = ExecuteNonQuery(command);

			return rows > 0;
		}

		/// <summary>
		/// Updates all outgoing links data for a page rename.
		/// </summary>
		/// <param name="oldName">The old page name.</param>
		/// <param name="newName">The new page name.</param>
		/// <returns><c>true</c> if the data is updated, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <b>oldName</b> or <b>newName</b> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>oldName</b> or <b>newName</b> are empty.</exception>
		public bool UpdateOutgoingLinksForRename(string oldName, string newName) {
			if(oldName == null) throw new ArgumentNullException("oldName");
			if(oldName.Length == 0) throw new ArgumentException("Old Name cannot be empty", "oldName");
			if(newName == null) throw new ArgumentNullException("newName");
			if(newName.Length == 0) throw new ArgumentException("New Name cannot be empty", "newName");

			// 1. Rename sources
			// 2. Rename destinations

			ICommandBuilder builder = GetCommandBuilder();
			DbConnection connection = builder.GetConnection(connString);
			DbTransaction transaction = BeginTransaction(connection);

			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.Update("OutgoingLink", new string[] { "Source" }, new string[] { "NewSource" });
			query = queryBuilder.Where(query, "Source", WhereOperator.Equals, "OldSource");

			List<Parameter> parameters = new List<Parameter>(2);
			parameters.Add(new Parameter(ParameterType.String, "NewSource", newName));
			parameters.Add(new Parameter(ParameterType.String, "OldSource", oldName));

			DbCommand command = builder.GetCommand(transaction, query, parameters);

			int rows = ExecuteNonQuery(command, false);

			if(rows == -1) {
				RollbackTransaction(transaction);
				return false;
			}

			bool somethingUpdated = rows > 0;

			query = queryBuilder.Update("OutgoingLink", new string[] { "Destination" }, new string[] { "NewDestination" });
			query = queryBuilder.Where(query, "Destination", WhereOperator.Equals, "OldDestination");

			parameters = new List<Parameter>(2);
			parameters.Add(new Parameter(ParameterType.String, "NewDestination", newName));
			parameters.Add(new Parameter(ParameterType.String, "OldDestination", oldName));

			command = builder.GetCommand(transaction, query, parameters);

			rows = ExecuteNonQuery(command, false);

			if(rows >= 0) CommitTransaction(transaction);
			else RollbackTransaction(transaction);

			return somethingUpdated || rows > 0;
		}

		/// <summary>
		/// Determines whether the application was started for the first time.
		/// </summary>
		/// <returns><c>true</c> if the application was started for the first time, <c>false</c> otherwise.</returns>
		public bool IsFirstApplicationStart() {
			return isFirstStart;
		}

		#endregion

		#region AclManager backend methods

		/// <summary>
		/// Converts a <see cref="T:Value" /> to its corresponding character representation.
		/// </summary>
		/// <param name="value">The <see cref="T:Value" />.</param>
		/// <returns>The character representation.</returns>
		private static char AclEntryValueToChar(Value value) {
			switch(value) {
				case Value.Grant:
					return 'G';
				case Value.Deny:
					return 'D';
				default:
					throw new NotSupportedException();
			}
		}

		/// <summary>
		/// Converts a character representation of a <see cref="T:Value" /> back to the enum value.
		/// </summary>
		/// <param name="c">The character representation.</param>
		/// <returns>The <see cref="T:Value" />.</returns>
		private static Value AclEntryValueFromChar(char c) {
			switch(char.ToUpperInvariant(c)) {
				case 'G':
					return Value.Grant;
				case 'D':
					return Value.Deny;
				default:
					throw new NotSupportedException();
			}
		}

		/// <summary>
		/// Retrieves all ACL entries.
		/// </summary>
		/// <returns>The ACL entries.</returns>
		private AclEntry[] RetrieveAllAclEntries() {
			ICommandBuilder builder = GetCommandBuilder();

			// Sort order is not relevant
			string query = QueryBuilder.NewQuery(builder).SelectFrom("AclEntry");

			DbCommand command = builder.GetCommand(connString, query, new List<Parameter>());

			DbDataReader reader = ExecuteReader(command);

			if(reader != null) {
				List<AclEntry> result = new List<AclEntry>(50);

				while(reader.Read()) {
					result.Add(new AclEntry(reader["Resource"] as string, reader["Action"] as string, reader["Subject"] as string,
						AclEntryValueFromChar(((string)reader["Value"])[0])));
				}

				CloseReader(command, reader);

				return result.ToArray();
			}
			else return null;
		}

		/// <summary>
		/// Retrieves all ACL entries for a resource.
		/// </summary>
		/// <param name="resource">The resource.</param>
		/// <returns>The ACL entries for the resource.</returns>
		private AclEntry[] RetrieveAclEntriesForResource(string resource) {
			ICommandBuilder builder = GetCommandBuilder();

			QueryBuilder queryBuilder = new QueryBuilder(builder);

			// Sort order is not relevant
			string query = queryBuilder.SelectFrom("AclEntry");
			query = queryBuilder.Where(query, "Resource", WhereOperator.Equals, "Resource");

			List<Parameter> parameters = new List<Parameter>(1);
			parameters.Add(new Parameter(ParameterType.String, "Resource", resource));

			DbCommand command = builder.GetCommand(connString, query, parameters);

			DbDataReader reader = ExecuteReader(command);

			if(reader != null) {
				List<AclEntry> result = new List<AclEntry>(50);

				while(reader.Read()) {
					result.Add(new AclEntry(reader["Resource"] as string, reader["Action"] as string, reader["Subject"] as string,
						AclEntryValueFromChar(((string)reader["Value"])[0])));
				}

				CloseReader(command, reader);

				return result.ToArray();
			}
			else return null;
		}

		/// <summary>
		/// Retrieves all ACL entries for a subject.
		/// </summary>
		/// <param name="subject">The subject.</param>
		/// <returns>The ACL entries for the subject.</returns>
		private AclEntry[] RetrieveAclEntriesForSubject(string subject) {
			ICommandBuilder builder = GetCommandBuilder();

			QueryBuilder queryBuilder = new QueryBuilder(builder);

			// Sort order is not relevant
			string query = queryBuilder.SelectFrom("AclEntry");
			query = queryBuilder.Where(query, "Subject", WhereOperator.Equals, "Subject");

			List<Parameter> parameters = new List<Parameter>(1);
			parameters.Add(new Parameter(ParameterType.String, "Subject", subject));

			DbCommand command = builder.GetCommand(connString, query, parameters);

			DbDataReader reader = ExecuteReader(command);

			if(reader != null) {
				List<AclEntry> result = new List<AclEntry>(50);

				while(reader.Read()) {
					result.Add(new AclEntry(reader["Resource"] as string, reader["Action"] as string, reader["Subject"] as string,
						AclEntryValueFromChar(((string)reader["Value"])[0])));
				}

				CloseReader(command, reader);

				return result.ToArray();
			}
			else return null;
		}

		/// <summary>
		/// Deletes some ACL entries.
		/// </summary>
		/// <param name="entries">The entries to delete.</param>
		/// <returns><c>true</c> if one or more entries were deleted, <c>false</c> otherwise.</returns>
		private bool DeleteEntries(AclEntry[] entries) {
			ICommandBuilder builder = GetCommandBuilder();
			DbConnection connection = builder.GetConnection(connString);
			DbTransaction transaction = BeginTransaction(connection);

			QueryBuilder queryBuilder = new QueryBuilder(builder);

			foreach(AclEntry entry in entries) {
				string query = queryBuilder.DeleteFrom("AclEntry");
				query = queryBuilder.Where(query, "Resource", WhereOperator.Equals, "Resource");
				query = queryBuilder.AndWhere(query, "Action", WhereOperator.Equals, "Action");
				query = queryBuilder.AndWhere(query, "Subject", WhereOperator.Equals, "Subject");

				List<Parameter> parameters = new List<Parameter>(3);
				parameters.Add(new Parameter(ParameterType.String, "Resource", entry.Resource));
				parameters.Add(new Parameter(ParameterType.String, "Action", entry.Action));
				parameters.Add(new Parameter(ParameterType.String, "Subject", entry.Subject));

				DbCommand command = builder.GetCommand(transaction, query, parameters);

				if(ExecuteNonQuery(command, false) <= 0) {
					RollbackTransaction(transaction);
					return false;
				}
			}

			CommitTransaction(transaction);

			return true;
		}

		/// <summary>
		/// Stores a ACL entry.
		/// </summary>
		/// <param name="entry">The entry to store.</param>
		/// <returns><c>true</c> if the entry was stored, <c>false</c> otherwise.</returns>
		private bool StoreEntry(AclEntry entry) {
			ICommandBuilder builder = GetCommandBuilder();
			DbConnection connection = builder.GetConnection(connString);
			DbTransaction transaction = BeginTransaction(connection);

			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.InsertInto("AclEntry", new string[] { "Resource", "Action", "Subject", "Value" }, new string[] { "Resource", "Action", "Subject", "Value" });

			List<Parameter> parameters = new List<Parameter>(3);
			parameters.Add(new Parameter(ParameterType.String, "Resource", entry.Resource));
			parameters.Add(new Parameter(ParameterType.String, "Action", entry.Action));
			parameters.Add(new Parameter(ParameterType.String, "Subject", entry.Subject));
			parameters.Add(new Parameter(ParameterType.Char, "Value", AclEntryValueToChar(entry.Value)));

			DbCommand command = builder.GetCommand(transaction, query, parameters);

			if(ExecuteNonQuery(command, false) != 1) {
				RollbackTransaction(transaction);
				return false;
			}

			CommitTransaction(transaction);

			return true;
		}

		/// <summary>
		/// Renames a ACL resource.
		/// </summary>
		/// <param name="resource">The resource to rename.</param>
		/// <param name="newName">The new name of the resource.</param>
		/// <returns><c>true</c> if one or more entries weere updated, <c>false</c> otherwise.</returns>
		private bool RenameAclResource(string resource, string newName) {
			ICommandBuilder builder = GetCommandBuilder();
			DbConnection connection = builder.GetConnection(connString);
			DbTransaction transaction = BeginTransaction(connection);

			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.Update("AclEntry", new[] { "Resource" }, new[] { "ResourceNew" });
			query = queryBuilder.Where(query, "Resource", WhereOperator.Equals, "ResourceOld");

			List<Parameter> parameters = new List<Parameter>(2);
			parameters.Add(new Parameter(ParameterType.String, "ResourceNew", newName));
			parameters.Add(new Parameter(ParameterType.String, "ResourceOld", resource));

			DbCommand command = builder.GetCommand(transaction, query, parameters);

			if(ExecuteNonQuery(command, false) <= 0) {
				RollbackTransaction(transaction);
				return false;
			}

			CommitTransaction(transaction);

			return true;
		}

		#endregion

	}

}
