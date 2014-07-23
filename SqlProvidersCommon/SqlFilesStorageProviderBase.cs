
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki.Plugins.SqlCommon {

	/// <summary>
	/// Implements a base class for a SQL files storage provider.
	/// </summary>
	public abstract class SqlFilesStorageProviderBase : SqlStorageProviderBase, IFilesStorageProviderV30 {

		private const int MaxFileSize = 52428800; // 50 MB

		#region IFilesStorageProvider Members

		/// <summary>
		/// Prepares the directory name.
		/// </summary>
		/// <param name="directory">The directory to prepare.</param>
		/// <returns>The prepared directory, for example "/" or "/my/directory/".</returns>
		private static string PrepareDirectory(string directory) {
			if(string.IsNullOrEmpty(directory)) return "/";
			else {
				return (!directory.StartsWith("/") ? "/" : "") +
					directory +
					(!directory.EndsWith("/") ? "/" : "");
			}
		}

		/// <summary>
		/// Determines whether a directory exists.
		/// </summary>
		/// <param name="connection">A database connection.</param>
		/// <param name="directory">The directory, for example "/my/directory".</param>
		/// <returns><c>true</c> if the directory exists, <c>false</c> otherwise.</returns>
		/// <remarks>The root directory always exists.</remarks>
		private bool DirectoryExists(DbConnection connection, string directory) {
			directory = PrepareDirectory(directory);

			ICommandBuilder builder = GetCommandBuilder();
			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.SelectCountFrom("Directory");
			query = queryBuilder.Where(query, "FullPath", WhereOperator.Equals, "FullPath");

			List<Parameter> parameters = new List<Parameter>(1);
			parameters.Add(new Parameter(ParameterType.String, "FullPath", directory));

			DbCommand command = builder.GetCommand(connection, query, parameters);

			int count = ExecuteScalar<int>(command, -1, false);

			return count == 1;
		}

		/// <summary>
		/// Determines whether a directory exists.
		/// </summary>
		/// <param name="transaction">A database transaction.</param>
		/// <param name="directory">The directory, for example "/my/directory".</param>
		/// <returns><c>true</c> if the directory exists, <c>false</c> otherwise.</returns>
		/// <remarks>The root directory always exists.</remarks>
		private bool DirectoryExists(DbTransaction transaction, string directory) {
			directory = PrepareDirectory(directory);

			ICommandBuilder builder = GetCommandBuilder();
			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.SelectCountFrom("Directory");
			query = queryBuilder.Where(query, "FullPath", WhereOperator.Equals, "FullPath");

			List<Parameter> parameters = new List<Parameter>(1);
			parameters.Add(new Parameter(ParameterType.String, "FullPath", directory));

			DbCommand command = builder.GetCommand(transaction, query, parameters);

			int count = ExecuteScalar<int>(command, -1, false);

			return count == 1;
		}

		/// <summary>
		/// Splits a file full name into the directory and file parts.
		/// </summary>
		/// <param name="fullName">The file full name, for example "/file.txt" or "/directory/file.txt".</param>
		/// <param name="directory">The resulting directory path, for example "/" or "/directory/".</param>
		/// <param name="file">The file name, for example "file.txt".</param>
		private static void SplitFileFullName(string fullName, out string directory, out string file) {
			directory = fullName.Substring(0, fullName.LastIndexOf("/") + 1);
			directory = PrepareDirectory(directory);
			file = fullName.Substring(fullName.LastIndexOf("/") + 1);
		}

		/// <summary>
		/// Determines whether a file exists.
		/// </summary>
		/// <param name="connection">A database connection.</param>
		/// <param name="fullName">The file full name, for example "/file.txt" or "/directory/file.txt".</param>
		/// <returns><c>true</c> if the file exists, <c>false</c> otherwise.</returns>
		private bool FileExists(DbConnection connection, string fullName) {
			string directory, file;
			SplitFileFullName(fullName, out directory, out file);

			ICommandBuilder builder = GetCommandBuilder();
			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.SelectCountFrom("File");
			query = queryBuilder.Where(query, "Name", WhereOperator.Equals, "Name");
			query = queryBuilder.AndWhere(query, "Directory", WhereOperator.Equals, "Directory");

			List<Parameter> parameters = new List<Parameter>(2);
			parameters.Add(new Parameter(ParameterType.String, "Name", file));
			parameters.Add(new Parameter(ParameterType.String, "Directory", directory));

			DbCommand command = builder.GetCommand(connection, query, parameters);

			int count = ExecuteScalar<int>(command, -1, false);

			return count == 1;
		}

		/// <summary>
		/// Determines whether a file exists.
		/// </summary>
		/// <param name="transaction">A database transaction.</param>
		/// <param name="fullName">The file full name, for example "/file.txt" or "/directory/file.txt".</param>
		/// <returns><c>true</c> if the file exists, <c>false</c> otherwise.</returns>
		private bool FileExists(DbTransaction transaction, string fullName) {
			string directory, file;
			SplitFileFullName(fullName, out directory, out file);

			ICommandBuilder builder = GetCommandBuilder();
			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.SelectCountFrom("File");
			query = queryBuilder.Where(query, "Name", WhereOperator.Equals, "Name");
			query = queryBuilder.AndWhere(query, "Directory", WhereOperator.Equals, "Directory");

			List<Parameter> parameters = new List<Parameter>(2);
			parameters.Add(new Parameter(ParameterType.String, "Name", file));
			parameters.Add(new Parameter(ParameterType.String, "Directory", directory));

			DbCommand command = builder.GetCommand(transaction, query, parameters);

			int count = ExecuteScalar<int>(command, -1, false);

			return count == 1;
		}

		/// <summary>
		/// Lists the Files in the specified Directory.
		/// </summary>
		/// <param name="directory">The full directory name, for example "/my/directory". Null, empty or "/" for the root directory.</param>
		/// <returns>The list of Files in the directory.</returns>
		/// <exception cref="ArgumentException">If <paramref name="directory"/> does not exist.</exception>
		public string[] ListFiles(string directory) {
			directory = PrepareDirectory(directory);

			ICommandBuilder builder = GetCommandBuilder();
			DbConnection connection = builder.GetConnection(connString);

			if(!DirectoryExists(connection, directory)) {
				CloseConnection(connection);
				throw new ArgumentException("Directory does not exist", "directory");
			}

			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.SelectFrom("File", new string[] { "Name" });
			query = queryBuilder.Where(query, "Directory", WhereOperator.Equals, "Directory");
			query = queryBuilder.OrderBy(query, new [] { "Name" }, new[] { Ordering.Asc });

			List<Parameter> parameters = new List<Parameter>(1);
			parameters.Add(new Parameter(ParameterType.String, "Directory", directory));

			DbCommand command = builder.GetCommand(connection, query, parameters);

			DbDataReader reader = ExecuteReader(command);

			if(reader != null) {
				List<string> result = new List<string>(20);

				while(reader.Read()) {
					result.Add(directory + reader["Name"] as string);
				}

				CloseReader(command, reader);

				return result.ToArray();
			}
			else return null;
		}

		/// <summary>
		/// Lists the Directories in the specified directory.
		/// </summary>
		/// <param name="connection">An open connection.</param>
		/// <param name="directory">The full directory name, for example "/my/directory". Null, empty or "/" for the root directory.</param>
		/// <returns>The list of Directories in the Directory.</returns>
		private string[] ListDirectories(DbConnection connection, string directory) {
			directory = PrepareDirectory(directory);

			ICommandBuilder builder = GetCommandBuilder();

			if(!DirectoryExists(connection, directory)) {
				CloseConnection(connection);
				throw new ArgumentException("Directory does not exist", "directory");
			}

			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.SelectFrom("Directory", new string[] { "FullPath" });
			query = queryBuilder.Where(query, "Parent", WhereOperator.Equals, "Parent");
			query = queryBuilder.OrderBy(query, new[] { "FullPath" }, new[] { Ordering.Asc });

			List<Parameter> parameters = new List<Parameter>(1);
			parameters.Add(new Parameter(ParameterType.String, "Parent", directory));

			DbCommand command = builder.GetCommand(connection, query, parameters);

			DbDataReader reader = ExecuteReader(command);

			if(reader != null) {
				List<string> result = new List<string>(20);

				while(reader.Read()) {
					result.Add(reader["FullPath"] as string);
				}

				CloseReader(reader);

				return result.ToArray();
			}
			else return null;
		}

		/// <summary>
		/// Lists the Directories in the specified directory.
		/// </summary>
		/// <param name="transaction">A database transaction.</param>
		/// <param name="directory">The full directory name, for example "/my/directory". Null, empty or "/" for the root directory.</param>
		/// <returns>The list of Directories in the Directory.</returns>
		private string[] ListDirectories(DbTransaction transaction, string directory) {
			directory = PrepareDirectory(directory);

			ICommandBuilder builder = GetCommandBuilder();

			if(!DirectoryExists(transaction, directory)) {
				RollbackTransaction(transaction);
				throw new ArgumentException("Directory does not exist", "directory");
			}

			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.SelectFrom("Directory", new string[] { "FullPath" });
			query = queryBuilder.Where(query, "Parent", WhereOperator.Equals, "Parent");

			List<Parameter> parameters = new List<Parameter>(1);
			parameters.Add(new Parameter(ParameterType.String, "Parent", directory));
			query = queryBuilder.OrderBy(query, new[] { "FullPath" }, new[] { Ordering.Asc });

			DbCommand command = builder.GetCommand(transaction, query, parameters);

			DbDataReader reader = ExecuteReader(command);

			if(reader != null) {
				List<string> result = new List<string>(20);

				while(reader.Read()) {
					result.Add(reader["FullPath"] as string);
				}

				CloseReader(reader);

				return result.ToArray();
			}
			else return null;
		}

		/// <summary>
		/// Lists the Directories in the specified directory.
		/// </summary>
		/// <param name="directory">The full directory name, for example "/my/directory". Null, empty or "/" for the root directory.</param>
		/// <returns>The list of Directories in the Directory.</returns>
		/// <exception cref="ArgumentException">If <paramref name="directory"/> does not exist.</exception>
		public string[] ListDirectories(string directory) {
			directory = PrepareDirectory(directory);

			ICommandBuilder builder = GetCommandBuilder();
			DbConnection connection = builder.GetConnection(connString);

			if(!DirectoryExists(connection, directory)) {
				CloseConnection(connection);
				throw new ArgumentException("Directory does not exist", "directory");
			}

			string[] result = ListDirectories(connection, directory);
			CloseConnection(connection);

			return result;
		}

		/// <summary>
		/// Stores a file.
		/// </summary>
		/// <param name="fullName">The full name of the file.</param>
		/// <param name="sourceStream">A Stream object used as <b>source</b> of a byte stream,
		/// i.e. the method reads from the Stream and stores the content properly.</param>
		/// <param name="overwrite"><c>true</c> to overwrite an existing file.</param>
		/// <returns><c>true</c> if the File is stored, <c>false</c> otherwise.</returns>
		/// <remarks>If <b>overwrite</b> is <c>false</c> and File already exists, the method returns <c>false</c>.</remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="fullName"/> or <paramref name="sourceStream"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="fullName"/> is empty or <paramref name="sourceStream"/> does not support reading.</exception>
		public bool StoreFile(string fullName, System.IO.Stream sourceStream, bool overwrite) {
			if(fullName == null) throw new ArgumentNullException("fullName");
			if(fullName.Length == 0) throw new ArgumentException("Full Name cannot be empty", "fullName");
			if(sourceStream == null) throw new ArgumentNullException("sourceStream");
			if(!sourceStream.CanRead) throw new ArgumentException("Cannot read from Source Stream", "sourceStream");

			string directory, filename;
			SplitFileFullName(fullName, out directory, out filename);

			ICommandBuilder builder = GetCommandBuilder();
			DbConnection connection = builder.GetConnection(connString);
			DbTransaction transaction = BeginTransaction(connection);

			QueryBuilder queryBuilder = new QueryBuilder(builder);

			bool fileExists = FileExists(transaction, fullName);

			if(fileExists && !overwrite) {
				RollbackTransaction(transaction);
				return false;
			}

			// To achieve decent performance, an UPDATE query is issued if the file exists,
			// otherwise an INSERT query is issued

			string query;
			List<Parameter> parameters;

			byte[] fileData = null;
			int size = Tools.ReadStream(sourceStream, ref fileData, MaxFileSize);
			if(size < 0) {
				RollbackTransaction(transaction);
				throw new ArgumentException("Source Stream contains too much data", "sourceStream");
			}

			if(fileExists) {
				query = queryBuilder.Update("File", new string[] { "Size", "LastModified", "Data" }, new string[] { "Size", "LastModified", "Data" });
				query = queryBuilder.Where(query, "Name", WhereOperator.Equals, "Name");
				query = queryBuilder.AndWhere(query, "Directory", WhereOperator.Equals, "Directory");

				parameters = new List<Parameter>(5);
				parameters.Add(new Parameter(ParameterType.Int64, "Size", (long)size));
				parameters.Add(new Parameter(ParameterType.DateTime, "LastModified", DateTime.Now));
				parameters.Add(new Parameter(ParameterType.ByteArray, "Data", fileData));
				parameters.Add(new Parameter(ParameterType.String, "Name", filename));
				parameters.Add(new Parameter(ParameterType.String, "Directory", directory));
			}
			else {
				query = queryBuilder.InsertInto("File", new string[] { "Name", "Directory", "Size", "Downloads", "LastModified", "Data" },
					new string[] { "Name", "Directory", "Size", "Downloads", "LastModified", "Data" });

				parameters = new List<Parameter>(6);
				parameters.Add(new Parameter(ParameterType.String, "Name", filename));
				parameters.Add(new Parameter(ParameterType.String, "Directory", directory));
				parameters.Add(new Parameter(ParameterType.Int64, "Size", (long)size));
				parameters.Add(new Parameter(ParameterType.Int32, "Downloads", 0));
				parameters.Add(new Parameter(ParameterType.DateTime, "LastModified", DateTime.Now));
				parameters.Add(new Parameter(ParameterType.ByteArray, "Data", fileData));
			}

			DbCommand command = builder.GetCommand(transaction, query, parameters);

			int rows = ExecuteNonQuery(command, false);
			if(rows == 1) CommitTransaction(transaction);
			else RollbackTransaction(transaction);

			return rows == 1;
		}

		/// <summary>
		/// Retrieves a File.
		/// </summary>
		/// <param name="fullName">The full name of the File.</param>
		/// <param name="destinationStream">A Stream object used as <b>destination</b> of a byte stream,
		/// i.e. the method writes to the Stream the file content.</param>
		/// <param name="countHit">A value indicating whether or not to count this retrieval in the statistics.</param>
		/// <returns><c>true</c> if the file is retrieved, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="fullName"/> or <paramref name="destinationStream"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="fullName"/> is empty or <paramref name="destinationStream"/> does not support writing.</exception>
		public bool RetrieveFile(string fullName, System.IO.Stream destinationStream, bool countHit) {
			if(fullName == null) throw new ArgumentNullException("fullName");
			if(fullName.Length == 0) throw new ArgumentException("Full Name cannot be empty", "fullName");
			if(destinationStream == null) throw new ArgumentNullException("destinationStream");
			if(!destinationStream.CanWrite) throw new ArgumentException("Cannot write into Destination Stream", "destinationStream");

			ICommandBuilder builder = GetCommandBuilder();
			DbConnection connection = builder.GetConnection(connString);
			DbTransaction transaction = BeginTransaction(connection);

			if(!FileExists(transaction, fullName)) {
				RollbackTransaction(transaction);
				CloseConnection(connection);
				throw new ArgumentException("File does not exist", "fullName");
			}

			string directory, filename;
			SplitFileFullName(fullName, out directory, out filename);

			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.SelectFrom("File", new string[] { "Size", "Data" });
			query = queryBuilder.Where(query, "Name", WhereOperator.Equals, "Name");
			query = queryBuilder.AndWhere(query, "Directory", WhereOperator.Equals, "Directory");

			List<Parameter> parameters = new List<Parameter>(2);
			parameters.Add(new Parameter(ParameterType.String, "Name", filename));
			parameters.Add(new Parameter(ParameterType.String, "Directory", directory));

			DbCommand command = builder.GetCommand(transaction, query, parameters);

			DbDataReader reader = ExecuteReader(command);

			if(reader != null) {
				bool done = false;

				if(reader.Read()) {
					int read = ReadBinaryColumn(reader, "Data", destinationStream);
					done = (long)read == (long)reader["Size"];
				}

				CloseReader(reader);

				if(!done) {
					RollbackTransaction(transaction);
					return false;
				}
			}
			else {
				RollbackTransaction(transaction);
				return false;
			}

			if(countHit) {
				// Update download count
				query = queryBuilder.UpdateIncrement("File", "Downloads", 1);
				query = queryBuilder.Where(query, "Name", WhereOperator.Equals, "Name");
				query = queryBuilder.AndWhere(query, "Directory", WhereOperator.Equals, "Directory");

				parameters = new List<Parameter>(2);
				parameters.Add(new Parameter(ParameterType.String, "Name", filename));
				parameters.Add(new Parameter(ParameterType.String, "Directory", directory));

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
		/// Sets the number of times a file was retrieved.
		/// </summary>
		/// <param name="fullName">The full name of the file.</param>
		/// <param name="count">The count to set.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="fullName"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="fullName"/> is empty.</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="count"/> is less than zero.</exception>
		public void SetFileRetrievalCount(string fullName, int count) {
			if(fullName == null) throw new ArgumentNullException("fullName");
			if(fullName.Length == 0) throw new ArgumentException("Full Name cannot be empty", "fullName");
			if(count < 0) throw new ArgumentOutOfRangeException("count", "Count must be greater than or equal to zero");

			ICommandBuilder builder = GetCommandBuilder();
			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string directory, filename;
			SplitFileFullName(fullName, out directory, out filename);

			string query = queryBuilder.Update("File", new string[] { "Downloads" }, new string[] { "Downloads" });
			query = queryBuilder.Where(query, "Name", WhereOperator.Equals, "Name");
			query = queryBuilder.AndWhere(query, "Directory", WhereOperator.Equals, "Directory");

			List<Parameter> parameters = new List<Parameter>(2);
			parameters.Add(new Parameter(ParameterType.String, "Name", filename));
			parameters.Add(new Parameter(ParameterType.String, "Directory", directory));
			parameters.Add(new Parameter(ParameterType.Int32, "Downloads", count));

			DbCommand command = builder.GetCommand(connString, query, parameters);

			ExecuteNonQuery(command);
		}

		/// <summary>
		/// Gets the details of a file.
		/// </summary>
		/// <param name="fullName">The full name of the file.</param>
		/// <returns>The details, or <c>null</c> if the file does not exist.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="fullName"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="fullName"/> is empty.</exception>
		public FileDetails GetFileDetails(string fullName) {
			if(fullName == null) throw new ArgumentNullException("fullName");
			if(fullName.Length == 0) throw new ArgumentException("Full Name cannot be empty", "fullName");

			ICommandBuilder builder = GetCommandBuilder();
			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string directory, filename;
			SplitFileFullName(fullName, out directory, out filename);

			string query = queryBuilder.SelectFrom("File", new string[] { "Size", "Downloads", "LastModified" });
			query = queryBuilder.Where(query, "Name", WhereOperator.Equals, "Name");
			query = queryBuilder.AndWhere(query, "Directory", WhereOperator.Equals, "Directory");

			List<Parameter> parameters = new List<Parameter>(2);
			parameters.Add(new Parameter(ParameterType.String, "Name", filename));
			parameters.Add(new Parameter(ParameterType.String, "Directory", directory));

			DbCommand command = builder.GetCommand(connString, query, parameters);

			DbDataReader reader = ExecuteReader(command);

			if(reader != null) {
				FileDetails details = null;

				if(reader.Read()) {
					details = new FileDetails((long)reader["Size"],
						(DateTime)reader["LastModified"], (int)reader["Downloads"]);
				}

				CloseReader(command, reader);

				return details;
			}
			else return null;
		}

		/// <summary>
		/// Deletes a File.
		/// </summary>
		/// <param name="fullName">The full name of the File.</param>
		/// <returns><c>true</c> if the File is deleted, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="fullName"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="fullName"/> is empty or it does not exist.</exception>
		public bool DeleteFile(string fullName) {
			if(fullName == null) throw new ArgumentNullException("fullName");
			if(fullName.Length == 0) throw new ArgumentException("Full Name cannot be empty", "fullName");

			ICommandBuilder builder = GetCommandBuilder();
			DbConnection connection = builder.GetConnection(connString);
			DbTransaction transaction = BeginTransaction(connection);

			if(!FileExists(transaction, fullName)) {
				RollbackTransaction(transaction);
				throw new ArgumentException("File does not exist", "fullName");
			}

			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string directory, filename;
			SplitFileFullName(fullName, out directory, out filename);

			string query = queryBuilder.DeleteFrom("File");
			query = queryBuilder.Where(query, "Name", WhereOperator.Equals, "Name");
			query = queryBuilder.AndWhere(query, "Directory", WhereOperator.Equals, "Directory");

			List<Parameter> parameters = new List<Parameter>(2);
			parameters.Add(new Parameter(ParameterType.String, "Name", filename));
			parameters.Add(new Parameter(ParameterType.String, "Directory", directory));

			DbCommand command = builder.GetCommand(transaction, query, parameters);

			int rows = ExecuteNonQuery(command, false);
			if(rows == 1) CommitTransaction(transaction);
			else RollbackTransaction(transaction);

			return rows == 1;
		}

		/// <summary>
		/// Renames or moves a File.
		/// </summary>
		/// <param name="oldFullName">The old full name of the File.</param>
		/// <param name="newFullName">The new full name of the File.</param>
		/// <returns><c>true</c> if the File is renamed, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="oldFullName"/> or <paramref name="newFullName"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="oldFullName"/> or <paramref name="newFullName"/> are empty, or if the old file does not exist, or if the new file already exist.</exception>
		public bool RenameFile(string oldFullName, string newFullName) {
			if(oldFullName == null) throw new ArgumentNullException("oldFullName");
			if(oldFullName.Length == 0) throw new ArgumentException("Old Full Name cannot be empty", "oldFullName");
			if(newFullName == null) throw new ArgumentNullException("newFullName");
			if(newFullName.Length == 0) throw new ArgumentException("New Full Name cannot be empty", "newFullName");

			ICommandBuilder builder = GetCommandBuilder();
			DbConnection connection = builder.GetConnection(connString);
			DbTransaction transaction = BeginTransaction(connection);

			QueryBuilder queryBuilder = new QueryBuilder(builder);

			if(!FileExists(transaction, oldFullName)) {
				RollbackTransaction(transaction);
				throw new ArgumentException("File does not exist", "oldFullName");
			}
			if(FileExists(transaction, newFullName)) {
				RollbackTransaction(transaction);
				throw new ArgumentException("File already exists", "newFullPath");
			}

			string oldDirectory, newDirectory, oldFilename, newFilename;
			SplitFileFullName(oldFullName, out oldDirectory, out oldFilename);
			SplitFileFullName(newFullName, out newDirectory, out newFilename);

			string query = queryBuilder.Update("File", new string[] { "Name" }, new string[] { "NewName" });
			query = queryBuilder.Where(query, "Name", WhereOperator.Equals, "OldName");
			query = queryBuilder.AndWhere(query, "Directory", WhereOperator.Equals, "OldDirectory");

			List<Parameter> parameters = new List<Parameter>(3);
			parameters.Add(new Parameter(ParameterType.String, "NewName", newFilename));
			parameters.Add(new Parameter(ParameterType.String, "OldName", oldFilename));
			parameters.Add(new Parameter(ParameterType.String, "OldDirectory", oldDirectory));

			DbCommand command = builder.GetCommand(transaction, query, parameters);

			int rows = ExecuteNonQuery(command, false);
			if(rows == 1) CommitTransaction(transaction);
			else RollbackTransaction(transaction);

			return rows == 1;
		}

		/// <summary>
		/// Creates a new Directory.
		/// </summary>
		/// <param name="path">The path to create the new Directory in.</param>
		/// <param name="name">The name of the new Directory.</param>
		/// <returns><c>true</c> if the Directory is created, <c>false</c> otherwise.</returns>
		/// <remarks>If <b>path</b> is "/my/directory" and <b>name</b> is "newdir", a new directory named "/my/directory/newdir" is created.</remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="path"/> or <paramref name="name"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="name"/> is empty or if the directory does not exist, or if the new directory already exists.</exception>
		public bool CreateDirectory(string path, string name) {
			if(path == null) throw new ArgumentNullException("path");
			if(name == null) throw new ArgumentNullException("name");
			if(name.Length == 0) throw new ArgumentException("Name cannot be empty", "name");

			path = PrepareDirectory(path);

			ICommandBuilder builder = GetCommandBuilder();
			DbConnection connection = builder.GetConnection(connString);
			DbTransaction transaction = BeginTransaction(connection);

			if(!DirectoryExists(transaction, path)) {
				RollbackTransaction(transaction);
				throw new ArgumentException("Directory does not exist", "path");
			}

			string newDirectoryFullPath = PrepareDirectory(path + name);

			if(DirectoryExists(transaction, newDirectoryFullPath)) {
				RollbackTransaction(transaction);
				throw new ArgumentException("Directory already exists", "name");
			}

			string query = QueryBuilder.NewQuery(builder).InsertInto("Directory", new string[] { "FullPath", "Parent" }, new string[] { "FullPath", "Parent" });

			List<Parameter> parameters = new List<Parameter>(2);
			parameters.Add(new Parameter(ParameterType.String, "FullPath", newDirectoryFullPath));
			parameters.Add(new Parameter(ParameterType.String, "Parent", path));

			DbCommand command = builder.GetCommand(transaction, query, parameters);

			int rows = ExecuteNonQuery(command, false);
			if(rows == 1) CommitTransaction(transaction);
			else RollbackTransaction(transaction);

			return rows == 1;
		}

		/// <summary>
		/// Deletes a directory and all its contents.
		/// </summary>
		/// <param name="transaction">The current transaction to use.</param>
		/// <param name="fullPath">The full path of the directory.</param>
		/// <returns><c>true</c> if the directory is deleted, <c>false</c> otherwise.</returns>
		private bool DeleteDirectory(DbTransaction transaction, string fullPath) {
			string[] dirs = ListDirectories(transaction, fullPath);
			foreach(string dir in dirs) {
				if(!DeleteDirectory(transaction, dir)) {
					return false;
				}
			}

			ICommandBuilder builder = GetCommandBuilder();
			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.DeleteFrom("Directory");
			query = queryBuilder.Where(query, "FullPath", WhereOperator.Equals, "FullPath");

			List<Parameter> parameters = new List<Parameter>(2);
			parameters.Add(new Parameter(ParameterType.String, "FullPath", fullPath));

			DbCommand command = builder.GetCommand(transaction, query, parameters);

			int rows = ExecuteNonQuery(command, false);

			return rows > 0;
		}

		/// <summary>
		/// Deletes a Directory and <b>all of its content</b>.
		/// </summary>
		/// <param name="fullPath">The full path of the Directory.</param>
		/// <returns><c>true</c> if the Directory is deleted, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="fullPath"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="fullPath"/> is empty or if it equals '/' or it does not exist.</exception>
		public bool DeleteDirectory(string fullPath) {
			if(fullPath == null) throw new ArgumentNullException("fullPath");
			if(fullPath.Length == 0) throw new ArgumentException("Full Path cannot be empty", "fullPath");
			if(fullPath == "/") throw new ArgumentException("Cannot delete the root directory", "fullPath");

			fullPath = PrepareDirectory(fullPath);

			// /dir/
			// /dir/sub/
			// /dir/sub/blah/
			// /dir/file.txt
			// /dir/sub/file.txt
			// etc.

			// 1. Delete sub-directories (recursively, depth first)
			// 2. Delete directory

			ICommandBuilder builder = GetCommandBuilder();
			DbConnection connection = builder.GetConnection(connString);
			DbTransaction transaction = BeginTransaction(connection);

			if(!DirectoryExists(transaction, fullPath)) {
				RollbackTransaction(transaction);
				throw new ArgumentException("Directory does not exist");
			}

			bool done = DeleteDirectory(transaction, fullPath);

			if(done) CommitTransaction(transaction);
			else RollbackTransaction(transaction);

			return done;
		}

		/// <summary>
		/// Renames or moves a Directory.
		/// </summary>
		/// <param name="transaction">The current transaction to use.</param>
		/// <param name="oldFullPath">The old full path of the Directory.</param>
		/// <param name="newFullPath">The new full path of the Directory.</param>
		/// <returns><c>true</c> if the Directory is renamed, <c>false</c> otherwise.</returns>
		private bool RenameDirectory(DbTransaction transaction, string oldFullPath, string newFullPath) {
			string[] directories = ListDirectories(transaction, oldFullPath);
			foreach(string dir in directories) {
				string trimmed = dir.Trim('/');
				string name = trimmed.Substring(trimmed.LastIndexOf("/") + 1);

				string newFullPathSub = PrepareDirectory(newFullPath + name);

				RenameDirectory(dir, newFullPathSub);
			}

			ICommandBuilder builder = GetCommandBuilder();
			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.Update("Directory", new string[] { "FullPath" }, new string[] { "NewDirectory1" });
			query = queryBuilder.Where(query, "FullPath", WhereOperator.Equals, "OldDirectory1");

			List<Parameter> parameters = new List<Parameter>(2);
			parameters.Add(new Parameter(ParameterType.String, "NewDirectory1", newFullPath));
			parameters.Add(new Parameter(ParameterType.String, "OldDirectory1", oldFullPath));

			DbCommand command = builder.GetCommand(transaction, query, parameters);

			int rows = ExecuteNonQuery(command, false);

			return rows > 0;
		}

		/// <summary>
		/// Renames or moves a Directory.
		/// </summary>
		/// <param name="oldFullPath">The old full path of the Directory.</param>
		/// <param name="newFullPath">The new full path of the Directory.</param>
		/// <returns><c>true</c> if the Directory is renamed, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="oldFullPath"/> or <paramref name="newFullPath"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="oldFullPath"/> or <paramref name="newFullPath"/> are empty or equal to '/', 
		/// or if the old directory does not exist or the new directory already exists.</exception>
		public bool RenameDirectory(string oldFullPath, string newFullPath) {
			if(oldFullPath == null) throw new ArgumentNullException("oldFullPath");
			if(oldFullPath.Length == 0) throw new ArgumentException("Old Full Path cannot be empty", "oldFullPath");
			if(oldFullPath == "/") throw new ArgumentException("Cannot rename the root directory", "oldFullPath");
			if(newFullPath == null) throw new ArgumentNullException("newFullPath");
			if(newFullPath.Length == 0) throw new ArgumentException("New Full Path cannot be empty", "newFullPath");
			if(newFullPath == "/") throw new ArgumentException("Cannot rename directory to the root directory", "newFullPath");

			oldFullPath = PrepareDirectory(oldFullPath);
			newFullPath = PrepareDirectory(newFullPath);

			ICommandBuilder builder = GetCommandBuilder();
			DbConnection connection = builder.GetConnection(connString);
			DbTransaction transaction = BeginTransaction(connection);

			if(!DirectoryExists(transaction, oldFullPath)) {
				RollbackTransaction(transaction);
				throw new ArgumentException("Directory does not exist", "oldFullPath");
			}
			if(DirectoryExists(transaction, newFullPath)) {
				RollbackTransaction(transaction);
				throw new ArgumentException("Directory already exists", "newFullPath");
			}

			// /dir/
			// /dir/sub/
			// /dir/sub/blah/
			// /dir/file.txt
			// /dir/sub/file.txt
			// etc.

			// 1. Rename sub-directories (recursively, depth first)
			// 2. Rename directory

			bool done = RenameDirectory(transaction, oldFullPath, newFullPath);

			if(done) CommitTransaction(transaction);
			else RollbackTransaction(transaction);

			return done;
		}

		/// <summary>
		/// The the names of the pages with attachments.
		/// </summary>
		/// <returns>The names of the pages with attachments.</returns>
		public string[] GetPagesWithAttachments() {
			ICommandBuilder builder = GetCommandBuilder();
			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.SelectFrom("Attachment", new string[] { "Page" });
			query = queryBuilder.GroupBy(query, new[] { "Page" });
			query = queryBuilder.OrderBy(query, new[] { "Page" }, new[] { Ordering.Asc });

			DbCommand command = builder.GetCommand(connString, query, new List<Parameter>());

			DbDataReader reader = ExecuteReader(command);

			if(reader != null) {
				List<string> result = new List<string>(100);

				while(reader.Read()) {
					result.Add(reader["Page"] as string);
				}

				CloseReader(command, reader);

				return result.ToArray();
			}
			else return null;
		}

		/// <summary>
		/// Returns the names of the Attachments of a Page.
		/// </summary>
		/// <param name="transaction">A database transaction.</param>
		/// <param name="pageInfo">The Page Info object that owns the Attachments.</param>
		/// <returns>The names, or an empty list.</returns>
		private string[] ListPageAttachments(DbTransaction transaction, PageInfo pageInfo) {
			ICommandBuilder builder = GetCommandBuilder();
			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.SelectFrom("Attachment", new string[] { "Name" });
			query = queryBuilder.Where(query, "Page", WhereOperator.Equals, "Page");
			query = queryBuilder.OrderBy(query, new[] { "Name" }, new[] { Ordering.Asc });

			List<Parameter> parameters = new List<Parameter>(1);
			parameters.Add(new Parameter(ParameterType.String, "Page", pageInfo.FullName));

			DbCommand command = builder.GetCommand(transaction, query, parameters);

			DbDataReader reader = ExecuteReader(command);

			if(reader != null) {
				List<string> result = new List<string>(10);

				while(reader.Read()) {
					result.Add(reader["Name"] as string);
				}

				CloseReader(reader);

				return result.ToArray();
			}
			else return null;
		}

		/// <summary>
		/// Returns the names of the Attachments of a Page.
		/// </summary>
		/// <param name="connection">A database connection.</param>
		/// <param name="pageInfo">The Page Info object that owns the Attachments.</param>
		/// <returns>The names, or an empty list.</returns>
		private string[] ListPageAttachments(DbConnection connection, PageInfo pageInfo) {
			ICommandBuilder builder = GetCommandBuilder();
			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.SelectFrom("Attachment", new string[] { "Name" });
			query = queryBuilder.Where(query, "Page", WhereOperator.Equals, "Page");

			List<Parameter> parameters = new List<Parameter>(1);
			parameters.Add(new Parameter(ParameterType.String, "Page", pageInfo.FullName));
			query = queryBuilder.OrderBy(query, new[] { "Name" }, new[] { Ordering.Asc });

			DbCommand command = builder.GetCommand(connection, query, parameters);

			DbDataReader reader = ExecuteReader(command);

			if(reader != null) {
				List<string> result = new List<string>(10);

				while(reader.Read()) {
					result.Add(reader["Name"] as string);
				}

				CloseReader(reader);

				return result.ToArray();
			}
			else return null;
		}

		/// <summary>
		/// Returns the names of the Attachments of a Page.
		/// </summary>
		/// <param name="pageInfo">The Page Info object that owns the Attachments.</param>
		/// <returns>The names, or an empty list.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="pageInfo"/> is <c>null</c>.</exception>
		public string[] ListPageAttachments(PageInfo pageInfo) {
			if(pageInfo == null) throw new ArgumentNullException("pageInfo");

			ICommandBuilder builder = GetCommandBuilder();
			DbConnection connection = builder.GetConnection(connString);

			string[] result = ListPageAttachments(connection, pageInfo);
			CloseConnection(connection);

			return result;
		}

		/// <summary>
		/// Determines whether a page attachment exists.
		/// </summary>
		/// <param name="connection">A database connection.</param>
		/// <param name="page">The page.</param>
		/// <param name="name">The attachment.</param>
		/// <returns><c>true</c> if the attachment exists, <c>false</c> otherwise.</returns>
		private bool AttachmentExists(DbConnection connection, PageInfo page, string name) {
			ICommandBuilder builder = GetCommandBuilder();
			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.SelectCountFrom("Attachment");
			query = queryBuilder.Where(query, "Name", WhereOperator.Equals, "Name");
			query = queryBuilder.AndWhere(query, "Page", WhereOperator.Equals, "Page");

			List<Parameter> parameters = new List<Parameter>(2);
			parameters.Add(new Parameter(ParameterType.String, "Name", name));
			parameters.Add(new Parameter(ParameterType.String, "Page", page.FullName));

			DbCommand command = builder.GetCommand(connection, query, parameters);

			int count = ExecuteScalar<int>(command, -1, false);

			return count == 1;
		}

		/// <summary>
		/// Determines whether a page attachment exists.
		/// </summary>
		/// <param name="transaction">A database transaction.</param>
		/// <param name="page">The page.</param>
		/// <param name="name">The attachment.</param>
		/// <returns><c>true</c> if the attachment exists, <c>false</c> otherwise.</returns>
		private bool AttachmentExists(DbTransaction transaction, PageInfo page, string name) {
			ICommandBuilder builder = GetCommandBuilder();
			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.SelectCountFrom("Attachment");
			query = queryBuilder.Where(query, "Name", WhereOperator.Equals, "Name");
			query = queryBuilder.AndWhere(query, "Page", WhereOperator.Equals, "Page");

			List<Parameter> parameters = new List<Parameter>(2);
			parameters.Add(new Parameter(ParameterType.String, "Name", name));
			parameters.Add(new Parameter(ParameterType.String, "Page", page.FullName));

			DbCommand command = builder.GetCommand(transaction, query, parameters);

			int count = ExecuteScalar<int>(command, -1, false);

			return count == 1;
		}

		/// <summary>
		/// Stores a Page Attachment.
		/// </summary>
		/// <param name="pageInfo">The Page Info that owns the Attachment.</param>
		/// <param name="name">The name of the Attachment, for example "myfile.jpg".</param>
		/// <param name="sourceStream">A Stream object used as <b>source</b> of a byte stream,
		/// i.e. the method reads from the Stream and stores the content properly.</param>
		/// <param name="overwrite"><c>true</c> to overwrite an existing Attachment.</param>
		/// <returns><c>true</c> if the Attachment is stored, <c>false</c> otherwise.</returns>
		/// <remarks>If <b>overwrite</b> is <c>false</c> and Attachment already exists, the method returns <c>false</c>.</remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="pageInfo"/>, <paramref name="name"/> or <paramref name="sourceStream"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="name"/> is empty or if <paramref name="sourceStream"/> does not support reading.</exception>
		public bool StorePageAttachment(PageInfo pageInfo, string name, System.IO.Stream sourceStream, bool overwrite) {
			if(pageInfo == null) throw new ArgumentNullException("pageInfo");
			if(name == null) throw new ArgumentNullException("name");
			if(name.Length == 0) throw new ArgumentException("Name cannot be empty", "name");
			if(sourceStream == null) throw new ArgumentNullException("sourceStream");
			if(!sourceStream.CanRead) throw new ArgumentException("Cannot read from Source Stream", "sourceStream");

			ICommandBuilder builder = GetCommandBuilder();
			DbConnection connection = builder.GetConnection(connString);
			DbTransaction transaction = BeginTransaction(connection);

			bool attachmentExists = AttachmentExists(transaction, pageInfo, name);

			if(attachmentExists && !overwrite) {
				RollbackTransaction(transaction);
				return false;
			}

			// To achieve decent performance, an UPDATE query is issued if the attachment exists,
			// otherwise an INSERT query is issued

			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query;
			List<Parameter> parameters;

			byte[] attachmentData = null;
			int size = Tools.ReadStream(sourceStream, ref attachmentData, MaxFileSize);
			if(size < 0) {
				RollbackTransaction(transaction);
				throw new ArgumentException("Source Stream contains too much data", "sourceStream");
			}

			if(attachmentExists) {
				query = queryBuilder.Update("Attachment", new string[] { "Size", "LastModified", "Data" }, new string[] { "Size", "LastModified", "Data" });
				query = queryBuilder.Where(query, "Name", WhereOperator.Equals, "Name");
				query = queryBuilder.AndWhere(query, "Page", WhereOperator.Equals, "Page");

				parameters = new List<Parameter>(5);
				parameters.Add(new Parameter(ParameterType.Int64, "Size", (long)size));
				parameters.Add(new Parameter(ParameterType.DateTime, "LastModified", DateTime.Now));
				parameters.Add(new Parameter(ParameterType.ByteArray, "Data", attachmentData));
				parameters.Add(new Parameter(ParameterType.String, "Name", name));
				parameters.Add(new Parameter(ParameterType.String, "Page", pageInfo.FullName));
			}
			else {
				query = queryBuilder.InsertInto("Attachment", new string[] { "Name", "Page", "Size", "Downloads", "LastModified", "Data" },
					new string[] { "Name", "Page", "Size", "Downloads", "LastModified", "Data" });

				parameters = new List<Parameter>(6);
				parameters.Add(new Parameter(ParameterType.String, "Name", name));
				parameters.Add(new Parameter(ParameterType.String, "Page", pageInfo.FullName));
				parameters.Add(new Parameter(ParameterType.Int64, "Size", (long)size));
				parameters.Add(new Parameter(ParameterType.Int32, "Downloads", 0));
				parameters.Add(new Parameter(ParameterType.DateTime, "LastModified", DateTime.Now));
				parameters.Add(new Parameter(ParameterType.ByteArray, "Data", attachmentData));
			}

			DbCommand command = builder.GetCommand(transaction, query, parameters);

			int rows = ExecuteNonQuery(command, false);
			if(rows == 1) CommitTransaction(transaction);
			else RollbackTransaction(transaction);

			return rows == 1;
		}

		/// <summary>
		/// Retrieves a Page Attachment.
		/// </summary>
		/// <param name="pageInfo">The Page Info that owns the Attachment.</param>
		/// <param name="name">The name of the Attachment, for example "myfile.jpg".</param>
		/// <param name="destinationStream">A Stream object used as <b>destination</b> of a byte stream,
		/// i.e. the method writes to the Stream the file content.</param>
		/// <param name="countHit">A value indicating whether or not to count this retrieval in the statistics.</param>
		/// <returns><c>true</c> if the Attachment is retrieved, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="pageInfo"/>, <paramref name="name"/> or <paramref name="destinationStream"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="name"/> is empty or if <paramref name="destinationStream"/> does not support writing,
		/// or if the page does not have attachments or if the attachment does not exist.</exception>
		public bool RetrievePageAttachment(PageInfo pageInfo, string name, System.IO.Stream destinationStream, bool countHit) {
			if(pageInfo == null) throw new ArgumentNullException("pageInfo");
			if(name == null) throw new ArgumentNullException("name");
			if(name.Length == 0) throw new ArgumentException("Name cannot be empty", "name");
			if(destinationStream == null) throw new ArgumentNullException("destinationStream");
			if(!destinationStream.CanWrite) throw new ArgumentException("Cannot write into Destination Stream", "destinationStream");

			ICommandBuilder builder = GetCommandBuilder();
			DbConnection connection = builder.GetConnection(connString);
			DbTransaction transaction = BeginTransaction(connection);

			if(!AttachmentExists(transaction, pageInfo, name)) {
				RollbackTransaction(transaction);
				throw new ArgumentException("Attachment does not exist", "name");
			}

			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.SelectFrom("Attachment", new string[] { "Size", "Data" });
			query = queryBuilder.Where(query, "Name", WhereOperator.Equals, "Name");
			query = queryBuilder.AndWhere(query, "Page", WhereOperator.Equals, "Page");

			List<Parameter> parameters = new List<Parameter>(2);
			parameters.Add(new Parameter(ParameterType.String, "Name", name));
			parameters.Add(new Parameter(ParameterType.String, "Page", pageInfo.FullName));

			DbCommand command = builder.GetCommand(transaction, query, parameters);

			DbDataReader reader = ExecuteReader(command);

			if(reader != null) {
				bool done = false;

				if(reader.Read()) {
					int read = ReadBinaryColumn(reader, "Data", destinationStream);
					done = (long)read == (long)reader["Size"];
				}

				CloseReader(reader);

				if(!done) {
					RollbackTransaction(transaction);
					return false;
				}
			}
			else {
				RollbackTransaction(transaction);
				return false;
			}

			if(countHit) {
				// Update download count
				query = queryBuilder.UpdateIncrement("Attachment", "Downloads", 1);
				query = queryBuilder.Where(query, "Name", WhereOperator.Equals, "Name");
				query = queryBuilder.AndWhere(query, "Page", WhereOperator.Equals, "Page");

				parameters = new List<Parameter>(2);
				parameters.Add(new Parameter(ParameterType.String, "Name", name));
				parameters.Add(new Parameter(ParameterType.String, "Page", pageInfo.FullName));

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
		/// Sets the number of times a page attachment was retrieved.
		/// </summary>
		/// <param name="pageInfo">The page.</param>
		/// <param name="name">The name of the attachment.</param>
		/// <param name="count">The count to set.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="pageInfo"/> or <paramref name="name"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="name"/> is empty.</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="count"/> is less than zero.</exception>
		public void SetPageAttachmentRetrievalCount(PageInfo pageInfo, string name, int count) {
			if(pageInfo == null) throw new ArgumentNullException("pageInfo");
			if(name == null) throw new ArgumentNullException("name");
			if(name.Length == 0) throw new ArgumentException("Name cannot be empty");
			if(count < 0) throw new ArgumentOutOfRangeException("Count must be greater than or equal to zero", "count");

			ICommandBuilder builder = GetCommandBuilder();
			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.Update("Attachment", new string[] { "Downloads" }, new string[] { "Downloads" });
			query = queryBuilder.Where(query, "Name", WhereOperator.Equals, "Name");
			query = queryBuilder.AndWhere(query, "Page", WhereOperator.Equals, "Page");

			List<Parameter> parameters = new List<Parameter>(2);
			parameters.Add(new Parameter(ParameterType.String, "Name", name));
			parameters.Add(new Parameter(ParameterType.String, "Page", pageInfo.FullName));
			parameters.Add(new Parameter(ParameterType.Int32, "Downloads", count));

			DbCommand command = builder.GetCommand(connString, query, parameters);

			ExecuteNonQuery(command);
		}

		/// <summary>
		/// Gets the details of a page attachment.
		/// </summary>
		/// <param name="pageInfo">The page that owns the attachment.</param>
		/// <param name="name">The name of the attachment, for example "myfile.jpg".</param>
		/// <returns>The details of the attachment, or <c>null</c> if the attachment does not exist.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="pageInfo"/> or <paramref name="name"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="name"/> is empty.</exception>
		public FileDetails GetPageAttachmentDetails(PageInfo pageInfo, string name) {
			if(pageInfo == null) throw new ArgumentNullException("pageInfo");
			if(name == null) throw new ArgumentNullException("name");
			if(name.Length == 0) throw new ArgumentException("Name cannot be empty");

			ICommandBuilder builder = GetCommandBuilder();
			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.SelectFrom("Attachment", new string[] { "Size", "Downloads", "LastModified" });
			query = queryBuilder.Where(query, "Name", WhereOperator.Equals, "Name");
			query = queryBuilder.AndWhere(query, "Page", WhereOperator.Equals, "Page");

			List<Parameter> parameters = new List<Parameter>(2);
			parameters.Add(new Parameter(ParameterType.String, "Name", name));
			parameters.Add(new Parameter(ParameterType.String, "Page", pageInfo.FullName));

			DbCommand command = builder.GetCommand(connString, query, parameters);

			DbDataReader reader = ExecuteReader(command);

			if(reader != null) {
				FileDetails details = null;

				if(reader.Read()) {
					details = new FileDetails((long)reader["Size"],
						(DateTime)reader["LastModified"], (int)reader["Downloads"]);
				}

				CloseReader(command, reader);

				return details;
			}
			else return null;
		}

		/// <summary>
		/// Deletes a Page Attachment.
		/// </summary>
		/// <param name="pageInfo">The Page Info that owns the Attachment.</param>
		/// <param name="name">The name of the Attachment, for example "myfile.jpg".</param>
		/// <returns><c>true</c> if the Attachment is deleted, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="pageInfo"/> or <paramref name="name"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="name"/> is empty or if the page or attachment do not exist.</exception>
		public bool DeletePageAttachment(PageInfo pageInfo, string name) {
			if(pageInfo == null) throw new ArgumentNullException("pageInfo");
			if(name == null) throw new ArgumentNullException("name");
			if(name.Length == 0) throw new ArgumentException("Name cannot be empty");

			ICommandBuilder builder = GetCommandBuilder();
			DbConnection connection = builder.GetConnection(connString);
			DbTransaction transaction = BeginTransaction(connection);

			if(!AttachmentExists(transaction, pageInfo, name)) {
				RollbackTransaction(transaction);
				throw new ArgumentException("Attachment does not exist", "name");
			}

			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.DeleteFrom("Attachment");
			query = queryBuilder.Where(query, "Name", WhereOperator.Equals, "Name");
			query = queryBuilder.AndWhere(query, "Page", WhereOperator.Equals, "Page");

			List<Parameter> parameters = new List<Parameter>(2);
			parameters.Add(new Parameter(ParameterType.String, "Name", name));
			parameters.Add(new Parameter(ParameterType.String, "Page", pageInfo.FullName));

			DbCommand command = builder.GetCommand(transaction, query, parameters);

			int rows = ExecuteNonQuery(command, false);
			if(rows == 1) CommitTransaction(transaction);
			else RollbackTransaction(transaction);

			return rows == 1;
		}

		/// <summary>
		/// Renames a Page Attachment.
		/// </summary>
		/// <param name="pageInfo">The Page Info that owns the Attachment.</param>
		/// <param name="oldName">The old name of the Attachment.</param>
		/// <param name="newName">The new name of the Attachment.</param>
		/// <returns><c>true</c> if the Attachment is renamed, false otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="pageInfo"/>, <paramref name="oldName"/> or <paramref name="newName"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="pageInfo"/>, <paramref name="oldName"/> or <paramref name="newName"/> are empty,
		/// or if the page or old attachment do not exist, or the new attachment name already exists.</exception>
		public bool RenamePageAttachment(PageInfo pageInfo, string oldName, string newName) {
			if(pageInfo == null) throw new ArgumentNullException("pageInfo");
			if(oldName == null) throw new ArgumentNullException("oldName");
			if(oldName.Length == 0) throw new ArgumentException("Old Name cannot be empty", "oldName");
			if(newName == null) throw new ArgumentNullException("newName");
			if(newName.Length == 0) throw new ArgumentException("New Name cannot be empty", "newName");

			ICommandBuilder builder = GetCommandBuilder();
			DbConnection connection = builder.GetConnection(connString);
			DbTransaction transaction = BeginTransaction(connection);

			if(!AttachmentExists(transaction, pageInfo, oldName)) {
				RollbackTransaction(transaction);
				throw new ArgumentException("Attachment does not exist", "name");
			}
			if(AttachmentExists(transaction, pageInfo, newName)) {
				RollbackTransaction(transaction);
				throw new ArgumentException("Attachment already exists", "name");
			}

			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.Update("Attachment", new string[] { "Name" }, new string[] { "NewName" });
			query = queryBuilder.Where(query, "Name", WhereOperator.Equals, "OldName");
			query = queryBuilder.AndWhere(query, "Page", WhereOperator.Equals, "Page");

			List<Parameter> parameters = new List<Parameter>(3);
			parameters.Add(new Parameter(ParameterType.String, "NewName", newName));
			parameters.Add(new Parameter(ParameterType.String, "OldName", oldName));
			parameters.Add(new Parameter(ParameterType.String, "Page", pageInfo.FullName));

			DbCommand command = builder.GetCommand(transaction, query, parameters);

			int rows = ExecuteNonQuery(command, false);
			if(rows == 1) CommitTransaction(transaction);
			else RollbackTransaction(transaction);

			return rows == 1;
		}

		/// <summary>
		/// Notifies the Provider that a Page has been renamed.
		/// </summary>
		/// <param name="oldPage">The old Page Info object.</param>
		/// <param name="newPage">The new Page Info object.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="oldPage"/> or <paramref name="newPage"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If the new page is already in use.</exception>
		public void NotifyPageRenaming(PageInfo oldPage, PageInfo newPage) {
			if(oldPage == null) throw new ArgumentNullException("oldPage");
			if(newPage == null) throw new ArgumentNullException("newPage");

			ICommandBuilder builder = GetCommandBuilder();
			DbConnection connection = builder.GetConnection(connString);
			DbTransaction transaction = BeginTransaction(connection);

			if(ListPageAttachments(transaction, newPage).Length > 0) {
				RollbackTransaction(transaction);
				throw new ArgumentException("New Page already exists", "newPage");
			}

			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.Update("Attachment", new string[] { "Page" }, new string[] { "NewPage" });
			query = queryBuilder.Where(query, "Page", WhereOperator.Equals, "OldPage");

			List<Parameter> parameters = new List<Parameter>(2);
			parameters.Add(new Parameter(ParameterType.String, "NewPage", newPage.FullName));
			parameters.Add(new Parameter(ParameterType.String, "OldPage", oldPage.FullName));

			DbCommand command = builder.GetCommand(transaction, query, parameters);

			int rows = ExecuteNonQuery(command, false);

			if(rows != -1) CommitTransaction(transaction);
			else RollbackTransaction(transaction);
		}

		#endregion

		#region IStorageProvider Members

		/// <summary>
		/// Gets a value specifying whether the provider is read-only, i.e. it can only provide data and not store it.
		/// </summary>
		public bool ReadOnly {
			get { return false; }
		}

		#endregion

	}

}
