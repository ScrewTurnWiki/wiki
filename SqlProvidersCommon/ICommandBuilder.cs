
using System;
using System.Collections.Generic;
using System.Text;
using System.Data.Common;

namespace ScrewTurn.Wiki.Plugins.SqlCommon {

	/// <summary>
	/// Defines an interface for a command builder component.
	/// </summary>
	/// <remarks>Classes implementing this interface should be <b>thread-safe</b>.</remarks>
	public interface ICommandBuilder {

		/// <summary>
		/// Gets the table and column name prefix.
		/// </summary>
		string ObjectNamePrefix { get; }

		/// <summary>
		/// Gets the table and column name suffix.
		/// </summary>
		string ObjectNameSuffix { get; }

		/// <summary>
		/// Gets the parameter name prefix.
		/// </summary>
		string ParameterNamePrefix { get; }

		/// <summary>
		/// Gets the parameter name suffix.
		/// </summary>
		string ParameterNameSuffix { get; }

		/// <summary>
		/// Gets the parameter name placeholder.
		/// </summary>
		string ParameterPlaceholder { get; }

		/// <summary>
		/// Gets a value indicating whether to use named parameters. If <c>false</c>,
		/// parameter placeholders will be equal to <see cref="ParameterPlaceholder" />.
		/// </summary>
		bool UseNamedParameters { get; }

		/// <summary>
		/// Gets the string to use in order to separate queries in a batch.
		/// </summary>
		string BatchQuerySeparator { get; }

		/// <summary>
		/// Gets a new database connection, open.
		/// </summary>
		/// <param name="connString">The connection string.</param>
		/// <returns>The connection.</returns>
		DbConnection GetConnection(string connString);

		/// <summary>
		/// Gets a properly built database command, with the underlying connection already open.
		/// </summary>
		/// <param name="connString">The connection string.</param>
		/// <param name="preparedQuery">The prepared query.</param>
		/// <param name="parameters">The parameters, if any.</param>
		/// <returns>The command.</returns>
		DbCommand GetCommand(string connString, string preparedQuery, List<Parameter> parameters);

		/// <summary>
		/// Gets a properly built database command, re-using an open connection.
		/// </summary>
		/// <param name="connection">The open connection to use.</param>
		/// <param name="preparedQuery">The prepared query.</param>
		/// <param name="parameters">The parameters, if any.</param>
		/// <returns>The command.</returns>
		DbCommand GetCommand(DbConnection connection, string preparedQuery, List<Parameter> parameters);

		/// <summary>
		/// Gets a properly built database command, re-using an open connection and a begun transaction.
		/// </summary>
		/// <param name="transaction">The transaction.</param>
		/// <param name="preparedQuery">The prepared query.</param>
		/// <param name="parameters">The parameters, if any.</param>
		/// <returns>The command.</returns>
		DbCommand GetCommand(DbTransaction transaction, string preparedQuery, List<Parameter> parameters);

	}

}
