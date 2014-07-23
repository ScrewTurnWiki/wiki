
using System;
using System.Collections.Generic;
using System.Text;
using ScrewTurn.Wiki.Plugins.SqlCommon;
using System.Data.Common;
using System.Data.SqlClient;

namespace ScrewTurn.Wiki.Plugins.SqlServer {

	/// <summary>
	/// Implements a command builder for SQL Server.
	/// </summary>
	public class SqlServerCommandBuilder : ICommandBuilder {

		/// <summary>
		/// Gets the table and column name prefix.
		/// </summary>
		public string ObjectNamePrefix {
			get { return "["; }
		}

		/// <summary>
		/// Gets the table and column name suffix.
		/// </summary>
		public string ObjectNameSuffix {
			get { return "]"; }
		}

		/// <summary>
		/// Gets the parameter name prefix.
		/// </summary>
		public string ParameterNamePrefix {
			get { return "@"; }
		}

		/// <summary>
		/// Gets the parameter name suffix.
		/// </summary>
		public string ParameterNameSuffix {
			get { return ""; }
		}

		/// <summary>
		/// Gets the parameter name placeholder.
		/// </summary>
		public string ParameterPlaceholder {
			get { throw new NotImplementedException(); }
		}

		/// <summary>
		/// Gets a value indicating whether to use named parameters. If <c>false</c>,
		/// parameter placeholders will be equal to <see cref="ParameterPlaceholder"/>.
		/// </summary>
		public bool UseNamedParameters {
			get { return true; }
		}

		/// <summary>
		/// Gets the string to use in order to separate queries in a batch.
		/// </summary>
		public string BatchQuerySeparator {
			get { return "; "; }
		}

		/// <summary>
		/// Gets a new database connection, open.
		/// </summary>
		/// <param name="connString">The connection string.</param>
		/// <returns>The connection.</returns>
		public DbConnection GetConnection(string connString) {
			DbConnection cn = new SqlConnection(connString);
			cn.Open();

			return cn;
		}

		/// <summary>
		/// Gets a properly built database command, with the underlying connection already open.
		/// </summary>
		/// <param name="connString">The connection string.</param>
		/// <param name="preparedQuery">The prepared query.</param>
		/// <param name="parameters">The parameters, if any.</param>
		/// <returns>The command.</returns>
		public DbCommand GetCommand(string connString, string preparedQuery, List<Parameter> parameters) {
			return GetCommand(GetConnection(connString), preparedQuery, parameters);
		}

		/// <summary>
		/// Gets a properly built database command, re-using an open connection.
		/// </summary>
		/// <param name="connection">The open connection to use.</param>
		/// <param name="preparedQuery">The prepared query.</param>
		/// <param name="parameters">The parameters, if any.</param>
		/// <returns>The command.</returns>
		public DbCommand GetCommand(DbConnection connection, string preparedQuery, List<Parameter> parameters) {
			DbCommand cmd = connection.CreateCommand();
			cmd.CommandText = preparedQuery;

			foreach(Parameter param in parameters) {
				cmd.Parameters.Add(new SqlParameter("@" + param.Name, param.Value));
			}

			return cmd;
		}

		/// <summary>
		/// Gets a properly built database command, re-using an open connection and a begun transaction.
		/// </summary>
		/// <param name="transaction">The transaction.</param>
		/// <param name="preparedQuery">The prepared query.</param>
		/// <param name="parameters">The parameters, if any.</param>
		/// <returns>The command.</returns>
		public DbCommand GetCommand(DbTransaction transaction, string preparedQuery, List<Parameter> parameters) {
			DbCommand cmd = transaction.Connection.CreateCommand();
			cmd.Transaction = transaction;
			cmd.CommandText = preparedQuery;

			foreach(Parameter param in parameters) {
				cmd.Parameters.Add(new SqlParameter("@" + param.Name, param.Value));
			}

			return cmd;
		}

	}

}
