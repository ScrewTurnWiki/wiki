
using System;
using System.Collections.Generic;
using System.Text;

namespace ScrewTurn.Wiki.Plugins.SqlCommon {

	/// <summary>
	/// A tool for building queries.
	/// </summary>
	public class QueryBuilder {

		private ICommandBuilder builder;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:QueryBuilder" /> class.
		/// </summary>
		/// <param name="builder">The command builder.</param>
		public QueryBuilder(ICommandBuilder builder) {
			this.builder = builder;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:QueryBuilder" /> class.
		/// </summary>
		/// <param name="builder">The command builder.</param>
		/// <returns>The new instance.</returns>
		public static QueryBuilder NewQuery(ICommandBuilder builder) {
			return new QueryBuilder(builder);
		}

		/// <summary>
		/// Builds a SELECT query.
		/// </summary>
		/// <param name="table">The table.</param>
		/// <param name="columns">The columns to select.</param>
		/// <returns>The SELECT query.</returns>
		public string SelectFrom(string table, string[] columns) {
			StringBuilder sb = new StringBuilder(100);

			sb.Append("select ");
			for(int i = 0; i < columns.Length; i++) {
				sb.Append(builder.ObjectNamePrefix);
				sb.Append(columns[i]);
				sb.Append(builder.ObjectNameSuffix);
				if(i != columns.Length - 1) sb.Append(",");
				sb.Append(" ");
			}

			sb.Append("from ");
			sb.Append(builder.ObjectNamePrefix);
			sb.Append(table);
			sb.Append(builder.ObjectNameSuffix);

			return sb.ToString();
		}

		/// <summary>
		/// Builds a SELECT query.
		/// </summary>
		/// <param name="table">The table.</param>
		/// <returns>The SELECT query.</returns>
		public string SelectFrom(string table) {
			StringBuilder sb = new StringBuilder(100);

			sb.Append("select * from ");
			sb.Append(builder.ObjectNamePrefix);
			sb.Append(table);
			sb.Append(builder.ObjectNameSuffix);

			return sb.ToString();
		}

		/// <summary>
		/// Builds a SELECT query with a JOIN clause.
		/// </summary>
		/// <param name="table">The min table.</param>
		/// <param name="joinedTable">The joined table.</param>
		/// <param name="tableColumn">The main table column to join.</param>
		/// <param name="joinedTableColumn">The joined table column to join.</param>
		/// <param name="join">The JOIN type.</param>
		/// <param name="tableColumns">The main table columns to select.</param>
		/// <param name="joinedTableColumns">The joined table columns to select.</param>
		/// <returns>The SELECT query (returned columns are named like <b>[Table_Column]</b>).</returns>
		public string SelectFrom(string table, string joinedTable, string tableColumn, string joinedTableColumn, Join join,
			string[] tableColumns, string[] joinedTableColumns) {

			return SelectFrom(table, joinedTable, new string[] { tableColumn }, new string[] { joinedTableColumn }, join,
				tableColumns, joinedTableColumns);
		}

		/// <summary>
		/// Builds a SELECT query with a JOIN clause.
		/// </summary>
		/// <param name="table">The min table.</param>
		/// <param name="joinedTable">The joined table.</param>
		/// <param name="joinTableColumns">The main table columns to join.</param>
		/// <param name="joinJoinedTableColumns">The joined table columns to join.</param>
		/// <param name="join">The JOIN type.</param>
		/// <param name="tableColumns">The main table columns to select.</param>
		/// <param name="joinedTableColumns">The joined table columns to select.</param>
		/// <returns>The SELECT query (returned columns are named like <b>[Table_Column]</b>).</returns>
		public string SelectFrom(string table, string joinedTable, string[] joinTableColumns, string[] joinJoinedTableColumns, Join join,
			string[] tableColumns, string[] joinedTableColumns) {

			StringBuilder sb = new StringBuilder(200);
			sb.Append("select ");

			for(int i = 0; i < tableColumns.Length; i++) {
				sb.Append(builder.ObjectNamePrefix);
				sb.Append(table);
				sb.Append(builder.ObjectNameSuffix);
				sb.Append(".");
				sb.Append(builder.ObjectNamePrefix);
				sb.Append(tableColumns[i]);
				sb.Append(builder.ObjectNameSuffix);

				sb.Append(" as ");
				sb.Append(builder.ObjectNamePrefix);
				sb.Append(table);
				sb.Append("_");
				sb.Append(tableColumns[i]);
				sb.Append(builder.ObjectNameSuffix);

				sb.Append(", ");
			}

			for(int i = 0; i < joinedTableColumns.Length; i++) {
				sb.Append(builder.ObjectNamePrefix);
				sb.Append(joinedTable);
				sb.Append(builder.ObjectNameSuffix);
				sb.Append(".");
				sb.Append(builder.ObjectNamePrefix);
				sb.Append(joinedTableColumns[i]);
				sb.Append(builder.ObjectNameSuffix);

				sb.Append(" as ");
				sb.Append(builder.ObjectNamePrefix);
				sb.Append(joinedTable);
				sb.Append("_");
				sb.Append(joinedTableColumns[i]);
				sb.Append(builder.ObjectNameSuffix);

				if(i != joinedTableColumns.Length - 1) sb.Append(", ");
			}

			sb.Append(" from ");

			sb.Append(builder.ObjectNamePrefix);
			sb.Append(table);
			sb.Append(builder.ObjectNameSuffix);

			sb.Append(" ");
			sb.Append(JoinToString(join));
			sb.Append(" ");

			sb.Append(builder.ObjectNamePrefix);
			sb.Append(joinedTable);
			sb.Append(builder.ObjectNameSuffix);

			sb.Append(" on ");

			for(int i = 0; i < joinTableColumns.Length; i++) {
				sb.Append(builder.ObjectNamePrefix);
				sb.Append(table);
				sb.Append(builder.ObjectNameSuffix);
				sb.Append(".");
				sb.Append(builder.ObjectNamePrefix);
				sb.Append(joinTableColumns[i]);
				sb.Append(builder.ObjectNameSuffix);

				sb.Append(" = ");

				sb.Append(builder.ObjectNamePrefix);
				sb.Append(joinedTable);
				sb.Append(builder.ObjectNameSuffix);
				sb.Append(".");
				sb.Append(builder.ObjectNamePrefix);
				sb.Append(joinJoinedTableColumns[i]);
				sb.Append(builder.ObjectNameSuffix);

				if(i != joinTableColumns.Length - 1) sb.Append(" and ");
			}

			return sb.ToString();
		}

		/// <summary>
		/// Builds a SELECT query with a JOIN clause.
		/// </summary>
		/// <param name="table">The main table.</param>
		/// <param name="joinedTable">The joined table.</param>
		/// <param name="tableColumn">The main table column to join.</param>
		/// <param name="joinedTableColumn">The joined table column to join.</param>
		/// <param name="join">The JOIN type.</param>
		/// <returns>The SELECT query.</returns>
		public string SelectFrom(string table, string joinedTable, string tableColumn, string joinedTableColumn, Join join) {
			StringBuilder sb = new StringBuilder(100);
			sb.Append("select * from ");

			sb.Append(builder.ObjectNamePrefix);
			sb.Append(table);
			sb.Append(builder.ObjectNameSuffix);

			sb.Append(" ");
			sb.Append(JoinToString(join));
			sb.Append(" ");

			sb.Append(builder.ObjectNamePrefix);
			sb.Append(joinedTable);
			sb.Append(builder.ObjectNameSuffix);

			sb.Append(" on ");

			sb.Append(builder.ObjectNamePrefix);
			sb.Append(table);
			sb.Append(builder.ObjectNameSuffix);
			sb.Append(".");
			sb.Append(builder.ObjectNamePrefix);
			sb.Append(tableColumn);
			sb.Append(builder.ObjectNameSuffix);

			sb.Append(" = ");

			sb.Append(builder.ObjectNamePrefix);
			sb.Append(joinedTable);
			sb.Append(builder.ObjectNameSuffix);
			sb.Append(".");
			sb.Append(builder.ObjectNamePrefix);
			sb.Append(joinedTableColumn);
			sb.Append(builder.ObjectNameSuffix);

			return sb.ToString();
		}

		/// <summary>
		/// Builds a SELECT query with two JOIN clauses (both on the main table).
		/// </summary>
		/// <param name="table">The main table.</param>
		/// <param name="joinedTable">The joined table.</param>
		/// <param name="tableColumn">The main table column to join.</param>
		/// <param name="joinedTableColumn">The joined table column to join.</param>
		/// <param name="join">The join.</param>
		/// <param name="tableColumns">The main table columns to select.</param>
		/// <param name="joinedTableColumns">The joined table columns to select.</param>
		/// <param name="otherJoinedTable">The other joined table.</param>
		/// <param name="otherJoinedTableColumn">The other joined table column to join.</param>
		/// <param name="otherJoin">The join.</param>
		/// <param name="otherJoinedTableColumns">The other joined table columns to select.</param>
		/// <returns>The SELECT query (returned columns are named like <b>[Table_Column]</b>).</returns>
		public string SelectFrom(string table, string joinedTable, string tableColumn, string joinedTableColumn, Join join,
			string[] tableColumns, string[] joinedTableColumns,
			string otherJoinedTable, string otherJoinedTableColumn, Join otherJoin, string[] otherJoinedTableColumns) {

			StringBuilder sb = new StringBuilder(200);
			sb.Append("select ");

			for(int i = 0; i < tableColumns.Length; i++) {
				sb.Append(builder.ObjectNamePrefix);
				sb.Append(table);
				sb.Append(builder.ObjectNameSuffix);
				sb.Append(".");
				sb.Append(builder.ObjectNamePrefix);
				sb.Append(tableColumns[i]);
				sb.Append(builder.ObjectNameSuffix);

				sb.Append(" as ");
				sb.Append(builder.ObjectNamePrefix);
				sb.Append(table);
				sb.Append("_");
				sb.Append(tableColumns[i]);
				sb.Append(builder.ObjectNameSuffix);

				sb.Append(", ");
			}

			for(int i = 0; i < joinedTableColumns.Length; i++) {
				sb.Append(builder.ObjectNamePrefix);
				sb.Append(joinedTable);
				sb.Append(builder.ObjectNameSuffix);
				sb.Append(".");
				sb.Append(builder.ObjectNamePrefix);
				sb.Append(joinedTableColumns[i]);
				sb.Append(builder.ObjectNameSuffix);

				sb.Append(" as ");
				sb.Append(builder.ObjectNamePrefix);
				sb.Append(joinedTable);
				sb.Append("_");
				sb.Append(joinedTableColumns[i]);
				sb.Append(builder.ObjectNameSuffix);

				sb.Append(", ");
			}

			for(int i = 0; i < otherJoinedTableColumns.Length; i++) {
				sb.Append(builder.ObjectNamePrefix);
				sb.Append(otherJoinedTable);
				sb.Append(builder.ObjectNameSuffix);
				sb.Append(".");
				sb.Append(builder.ObjectNamePrefix);
				sb.Append(otherJoinedTableColumns[i]);
				sb.Append(builder.ObjectNameSuffix);

				sb.Append(" as ");
				sb.Append(builder.ObjectNamePrefix);
				sb.Append(otherJoinedTable);
				sb.Append("_");
				sb.Append(otherJoinedTableColumns[i]);
				sb.Append(builder.ObjectNameSuffix);

				if(i != otherJoinedTableColumns.Length - 1) sb.Append(", ");
			}

			sb.Append(" from ");

			sb.Append(builder.ObjectNamePrefix);
			sb.Append(table);
			sb.Append(builder.ObjectNameSuffix);

			sb.Append(" ");
			sb.Append(JoinToString(join));
			sb.Append(" ");

			sb.Append(builder.ObjectNamePrefix);
			sb.Append(joinedTable);
			sb.Append(builder.ObjectNameSuffix);

			sb.Append(" on ");

			sb.Append(builder.ObjectNamePrefix);
			sb.Append(table);
			sb.Append(builder.ObjectNameSuffix);
			sb.Append(".");
			sb.Append(builder.ObjectNamePrefix);
			sb.Append(tableColumn);
			sb.Append(builder.ObjectNameSuffix);

			sb.Append(" = ");

			sb.Append(builder.ObjectNamePrefix);
			sb.Append(joinedTable);
			sb.Append(builder.ObjectNameSuffix);
			sb.Append(".");
			sb.Append(builder.ObjectNamePrefix);
			sb.Append(joinedTableColumn);
			sb.Append(builder.ObjectNameSuffix);

			sb.Append(" ");
			sb.Append(JoinToString(otherJoin));
			sb.Append(" ");

			sb.Append(builder.ObjectNamePrefix);
			sb.Append(otherJoinedTable);
			sb.Append(builder.ObjectNameSuffix);

			sb.Append(" on ");

			sb.Append(builder.ObjectNamePrefix);
			sb.Append(table);
			sb.Append(builder.ObjectNameSuffix);
			sb.Append(".");
			sb.Append(builder.ObjectNamePrefix);
			sb.Append(tableColumn);
			sb.Append(builder.ObjectNameSuffix);

			sb.Append(" = ");

			sb.Append(builder.ObjectNamePrefix);
			sb.Append(otherJoinedTable);
			sb.Append(builder.ObjectNameSuffix);
			sb.Append(".");
			sb.Append(builder.ObjectNamePrefix);
			sb.Append(otherJoinedTableColumn);
			sb.Append(builder.ObjectNameSuffix);

			return sb.ToString();
		}

		/// <summary>
		/// Converts a Join type to its string representation.
		/// </summary>
		/// <param name="join">The join type.</param>
		/// <returns>The string representation.</returns>
		private static string JoinToString(Join join) {
			switch(join) {
				case Join.Join:
					return "join";
				case Join.InnerJoin:
					return "inner join";
				case Join.LeftJoin:
					return "left join";
				case Join.RightJoin:
					return "right join";
				default:
					throw new NotSupportedException();
			}
		}

		/// <summary>
		/// Builds a SELECT COUNT(*) query.
		/// </summary>
		/// <param name="table">The table.</param>
		/// <returns>The SELECT query.</returns>
		public string SelectCountFrom(string table) {
			StringBuilder sb = new StringBuilder(100);

			sb.Append("select count(*) from ");
			sb.Append(builder.ObjectNamePrefix);
			sb.Append(table);
			sb.Append(builder.ObjectNameSuffix);

			return sb.ToString();
		}

		/// <summary>
		/// Builds a WHERE clause condition.
		/// </summary>
		/// <param name="table">The table the column belongs to, or <c>null</c>.</param>
		/// <param name="column">The column.</param>
		/// <param name="op">The operator.</param>
		/// <param name="parameter">The parameter name.</param>
		/// <returns>The condition.</returns>
		private string BuildWhereClause(string table, string column, WhereOperator op, string parameter) {
			StringBuilder sb = new StringBuilder(80);

			if(table != null) {
				sb.Append(builder.ObjectNamePrefix);
				sb.Append(table);
				sb.Append(builder.ObjectNameSuffix);
				sb.Append(".");
			}

			sb.Append(builder.ObjectNamePrefix);
			sb.Append(column);
			sb.Append(builder.ObjectNameSuffix);

			sb.Append(" ");
			sb.Append(WhereOperatorToString(op));

			if(op != WhereOperator.IsNull && op != WhereOperator.IsNotNull) {
				sb.Append(" ");

				if(builder.UseNamedParameters) {
					sb.Append(builder.ParameterNamePrefix);
					sb.Append(parameter);
					sb.Append(builder.ParameterNameSuffix);
				}
				else sb.Append(builder.ParameterPlaceholder);
			}

			return sb.ToString();
		}

		/// <summary>
		/// Applies a WHERE clause to a query.
		/// </summary>
		/// <param name="query">The query.</param>
		/// <param name="column">The column subject of the WHERE clause.</param>
		/// <param name="op">The operator.</param>
		/// <param name="parameter">The name of the parameter for the WHERE clause.</param>
		/// <returns>The resulting query.</returns>
		public string Where(string query, string column, WhereOperator op, string parameter) {
			return Where(query, null, column, op, parameter, false, false);
		}

		/// <summary>
		/// Applies a WHERE clause to a query.
		/// </summary>
		/// <param name="query">The query.</param>
		/// <param name="column">The column subject of the WHERE clause.</param>
		/// <param name="op">The operator.</param>
		/// <param name="parameter">The name of the parameter for the WHERE clause.</param>
		/// <param name="openBracket">A value indicating whether to open a bracket after the WHERE.</param>
		/// <param name="closeBracket">A value indicating whether to close a bracket after the clause.</param>
		/// <returns>The resulting query.</returns>
		public string Where(string query, string column, WhereOperator op, string parameter, bool openBracket, bool closeBracket) {
			return Where(query, null, column, op, parameter, openBracket, closeBracket);
		}

		/// <summary>
		/// Applies a WHERE clause to a query.
		/// </summary>
		/// <param name="query">The query.</param>
		/// <param name="table">The table the column belongs to.</param>
		/// <param name="column">The column subject of the WHERE clause.</param>
		/// <param name="op">The operator.</param>
		/// <param name="parameter">The name of the parameter for the WHERE clause.</param>
		/// <returns>The resulting query.</returns>
		public string Where(string query, string table, string column, WhereOperator op, string parameter) {
			return Where(query, table, column, op, parameter, false, false);
		}

		/// <summary>
		/// Applies a WHERE clause to a query.
		/// </summary>
		/// <param name="query">The query.</param>
		/// <param name="table">The table the column belongs to.</param>
		/// <param name="column">The column subject of the WHERE clause.</param>
		/// <param name="op">The operator.</param>
		/// <param name="parameter">The name of the parameter for the WHERE clause.</param>
		/// <param name="openBracket">A value indicating whether to open a bracket after the WHERE.</param>
		/// <param name="closeBracket">A value indicating whether to close a bracket after the clause.</param>
		/// <returns>The resulting query.</returns>
		public string Where(string query, string table, string column, WhereOperator op, string parameter, bool openBracket, bool closeBracket) {
			StringBuilder sb = new StringBuilder(150);
			sb.Append(query);
			sb.Append(" where ");
			if(openBracket) sb.Append("(");

			sb.Append(BuildWhereClause(table, column, op, parameter));

			if(closeBracket) sb.Append(")");

			return sb.ToString();
		}

		/// <summary>
		/// Builds a WHERE clause with IN operator.
		/// </summary>
		/// <param name="table">The table the column belongs to, or <c>null</c>.</param>
		/// <param name="column">The column subject of the WHERE clause.</param>
		/// <param name="parameters">The names of the parameters in the IN set.</param>
		/// <returns>The resulting clause.</returns>
		private string BuildWhereInClause(string table, string column, string[] parameters) {
			StringBuilder sb = new StringBuilder(100);

			if(!string.IsNullOrEmpty(table)) {
				sb.Append(builder.ObjectNamePrefix);
				sb.Append(table);
				sb.Append(builder.ObjectNameSuffix);
				sb.Append(".");
			}

			sb.Append(builder.ObjectNamePrefix);
			sb.Append(column);
			sb.Append(builder.ObjectNameSuffix);

			sb.Append(" in (");

			for(int i = 0; i < parameters.Length; i++) {
				if(builder.UseNamedParameters) {
					sb.Append(builder.ParameterNamePrefix);
					sb.Append(parameters[i]);
					sb.Append(builder.ParameterNameSuffix);
				}
				else {
					sb.Append(builder.ParameterPlaceholder);
				}
				if(i != parameters.Length - 1) sb.Append(", ");
			}

			sb.Append(")");

			return sb.ToString();
		}

		/// <summary>
		/// Applies a WHERE clause with IN operator to a query.
		/// </summary>
		/// <param name="query">The query.</param>
		/// <param name="column">The column subject of the WHERE clause.</param>
		/// <param name="parameters">The names of the parameters in the IN set.</param>
		/// <returns>The resulting query.</returns>
		public string WhereIn(string query, string column, string[] parameters) {
			return WhereIn(query, null, column, parameters);
		}

		/// <summary>
		/// Applies a WHERE clause with IN operator to a query.
		/// </summary>
		/// <param name="query">The query.</param>
		/// <param name="table">The table the column belongs to.</param>
		/// <param name="column">The column subject of the WHERE clause.</param>
		/// <param name="parameters">The names of the parameters in the IN set.</param>
		/// <returns>The resulting query.</returns>
		public string WhereIn(string query, string table, string column, string[] parameters) {
			StringBuilder sb = new StringBuilder(200);
			sb.Append(query);
			sb.Append(" where ");

			sb.Append(BuildWhereInClause(table, column, parameters));

			return sb.ToString();
		}

		/// <summary>
		/// Applies a WHERE NOT IN (subQuery) clause to a query.
		/// </summary>
		/// <param name="query">The query.</param>
		/// <param name="table">The table the column belongs to.</param>
		/// <param name="column">The column.</param>
		/// <param name="subQuery">The subQuery.</param>
		/// <returns>The resulting query.</returns>
		public string WhereNotInSubquery(string query, string table, string column, string subQuery) {
			StringBuilder sb = new StringBuilder(200);
			sb.Append(query);
			sb.Append(" where ");

			if(table != null) {
				sb.Append(builder.ObjectNamePrefix);
				sb.Append(table);
				sb.Append(builder.ObjectNameSuffix);
				sb.Append(".");
			}

			sb.Append(builder.ObjectNamePrefix);
			sb.Append(column);
			sb.Append(builder.ObjectNameSuffix);

			sb.Append(" not in (");
			sb.Append(subQuery);
			sb.Append(")");

			return sb.ToString();
		}

		/// <summary>
		/// Adds another WHERE clause to a query.
		/// </summary>
		/// <param name="query">The query.</param>
		/// <param name="column">The column subject of the WHERE clause.</param>
		/// <param name="op">The operator.</param>
		/// <param name="parameter">The name of the parameter for the WHERE clause.</param>
		/// <returns>The resulting query.</returns>
		public string AndWhere(string query, string column, WhereOperator op, string parameter) {
			return AndWhere(query, column, op, parameter, false, false);
		}

		/// <summary>
		/// Adds another WHERE clause to a query.
		/// </summary>
		/// <param name="query">The query.</param>
		/// <param name="column">The column subject of the WHERE clause.</param>
		/// <param name="op">The operator.</param>
		/// <param name="parameter">The name of the parameter for the WHERE clause.</param>
		/// <param name="openBracket">A value indicating whether to open a bracket after the AND.</param>
		/// <param name="closeBracket">A value indicating whether to close a bracket after the clause.</param>
		/// <returns>The resulting query.</returns>
		public string AndWhere(string query, string column, WhereOperator op, string parameter, bool openBracket, bool closeBracket) {
			return AndWhere(query, null, column, op, parameter, openBracket, closeBracket);
		}

		/// <summary>
		/// Adds another WHERE clause to a query.
		/// </summary>
		/// <param name="query">The query.</param>
		/// <param name="table">The table the column belongs to.</param>
		/// <param name="column">The column subject of the WHERE clause.</param>
		/// <param name="op">The operator.</param>
		/// <param name="parameter">The name of the parameter for the WHERE clause.</param>
		/// <returns>The resulting query.</returns>
		public string AndWhere(string query, string table, string column, WhereOperator op, string parameter) {
			return AndWhere(query, table, column, op, parameter, false, false);
		}

		/// <summary>
		/// Adds another WHERE clause to a query.
		/// </summary>
		/// <param name="query">The query.</param>
		/// <param name="table">The table the column belongs to.</param>
		/// <param name="column">The column subject of the WHERE clause.</param>
		/// <param name="op">The operator.</param>
		/// <param name="parameter">The name of the parameter for the WHERE clause.</param>
		/// <param name="openBracket">A value indicating whether to open a bracket after the AND.</param>
		/// <param name="closeBracket">A value indicating whether to close a bracket after the clause.</param>
		/// <returns>The resulting query.</returns>
		public string AndWhere(string query, string table, string column, WhereOperator op, string parameter, bool openBracket, bool closeBracket) {
			StringBuilder sb = new StringBuilder(200);
			sb.Append(query);
			sb.Append(" and ");
			if(openBracket) sb.Append("(");

			sb.Append(BuildWhereClause(table, column, op, parameter));

			if(closeBracket) sb.Append(")");

			return sb.ToString();
		}

		/// <summary>
		/// Adds another WHERE clause to a query.
		/// </summary>
		/// <param name="query">The query.</param>
		/// <param name="column">The column subject of the WHERE clause.</param>
		/// <param name="op">The operator.</param>
		/// <param name="parameter">The name of the parameter for the WHERE clause.</param>
		/// <returns>The resulting query.</returns>
		public string OrWhere(string query, string column, WhereOperator op, string parameter) {
			return OrWhere(query, column, op, parameter, false, false);
		}

		/// <summary>
		/// Adds another WHERE clause to a query.
		/// </summary>
		/// <param name="query">The query.</param>
		/// <param name="column">The column subject of the WHERE clause.</param>
		/// <param name="op">The operator.</param>
		/// <param name="parameter">The name of the parameter for the WHERE clause.</param>
		/// <param name="openBracket">A value indicating whether to open a bracket after the OR.</param>
		/// <param name="closeBracket">A value indicating whether to close a bracket after the clause.</param>
		/// <returns>The resulting query.</returns>
		public string OrWhere(string query, string column, WhereOperator op, string parameter, bool openBracket, bool closeBracket) {
			return OrWhere(query, null, column, op, parameter, openBracket, closeBracket);
		}

		/// <summary>
		/// Adds another WHERE clause to a query.
		/// </summary>
		/// <param name="query">The query.</param>
		/// <param name="table">The table the column belongs to.</param>
		/// <param name="column">The column subject of the WHERE clause.</param>
		/// <param name="op">The operator.</param>
		/// <param name="parameter">The name of the parameter for the WHERE clause.</param>
		/// <returns>The resulting query.</returns>
		public string OrWhere(string query, string table, string column, WhereOperator op, string parameter) {
			return OrWhere(query, table, column, op, parameter, false, false);
		}

		/// <summary>
		/// Adds another WHERE clause to a query.
		/// </summary>
		/// <param name="query">The query.</param>
		/// <param name="table">The table the column belongs to.</param>
		/// <param name="column">The column subject of the WHERE clause.</param>
		/// <param name="op">The operator.</param>
		/// <param name="parameter">The name of the parameter for the WHERE clause.</param>
		/// <param name="openBracket">A value indicating whether to open a bracket after the AND.</param>
		/// <param name="closeBracket">A value indicating whether to close a bracket after the clause.</param>
		/// <returns>The resulting query.</returns>
		public string OrWhere(string query, string table, string column, WhereOperator op, string parameter, bool openBracket, bool closeBracket) {
			StringBuilder sb = new StringBuilder(200);
			sb.Append(query);
			sb.Append(" or ");
			if(openBracket) sb.Append("(");

			sb.Append(BuildWhereClause(table, column, op, parameter));

			if(closeBracket) sb.Append(")");

			return sb.ToString();
		}

		/// <summary>
		/// Converts a WHERE operator to its corresponding string.
		/// </summary>
		/// <param name="op">The operator.</param>
		/// <returns>The string.</returns>
		private static string WhereOperatorToString(WhereOperator op) {
			switch(op) {
				case WhereOperator.Like:
					return "like";
				case WhereOperator.Equals:
					return "=";
				case WhereOperator.NotEquals:
					return "<>";
				case WhereOperator.GreaterThan:
					return ">";
				case WhereOperator.LessThan:
					return "<";
				case WhereOperator.GreaterThanOrEqualTo:
					return ">=";
				case WhereOperator.LessThanOrEqualTo:
					return "<=";
				case WhereOperator.IsNull:
					return "is null";
				case WhereOperator.IsNotNull:
					return "is not null";
				default:
					throw new NotSupportedException();
			}
		}

		/// <summary>
		/// Applies an ORDER BY clause to a query.
		/// </summary>
		/// <param name="query">The query.</param>
		/// <param name="columns">The columns to order by.</param>
		/// <param name="orderings">The ordering directions for each column.</param>
		/// <returns>The resulting query.</returns>
		public string OrderBy(string query, string[] columns, Ordering[] orderings) {
			StringBuilder sb = new StringBuilder(200);
			sb.Append(query);

			sb.Append(" order by ");

			for(int i = 0; i < columns.Length; i++) {
				sb.Append(builder.ObjectNamePrefix);
				sb.Append(columns[i]);
				sb.Append(builder.ObjectNameSuffix);
				sb.Append(" ");

				sb.Append(OrderingToString(orderings[i]));

				if(i != columns.Length - 1) sb.Append(", ");
			}

			return sb.ToString();
		}

		/// <summary>
		/// Converts an ordering to string.
		/// </summary>
		/// <param name="ordering">The ordering.</param>
		/// <returns>The string.</returns>
		private static string OrderingToString(Ordering ordering) {
			switch(ordering) {
				case Ordering.Asc:
					return "asc";
				case Ordering.Desc:
					return "desc";
				default:
					throw new NotSupportedException();
			}
		}

		/// <summary>
		/// Applies a GROUP BY clause to a query.
		/// </summary>
		/// <param name="query">The query.</param>
		/// <param name="columns">The columns to group by.</param>
		/// <returns>The resulting query.</returns>
		public string GroupBy(string query, string[] columns) {
			StringBuilder sb = new StringBuilder(200);
			sb.Append(query);
			sb.Append(" group by ");

			for(int i = 0; i < columns.Length; i++) {
				sb.Append(builder.ObjectNamePrefix);
				sb.Append(columns[i]);
				sb.Append(builder.ObjectNameSuffix);

				if(i != columns.Length - 1) sb.Append(", ");
			}

			return sb.ToString();
		}

		/// <summary>
		/// Builds an INSERT INTO query.
		/// </summary>
		/// <param name="table">The destination table.</param>
		/// <param name="columns">The columns names.</param>
		/// <param name="parameters">The parameters names.</param>
		/// <returns>The INSERT INTO query.</returns>
		public string InsertInto(string table, string[] columns, string[] parameters) {
			StringBuilder sb = new StringBuilder(200);
			sb.Append("insert into ");

			sb.Append(builder.ObjectNamePrefix);
			sb.Append(table);
			sb.Append(builder.ObjectNameSuffix);

			sb.Append(" (");
			for(int i = 0; i < columns.Length; i++) {
				sb.Append(builder.ObjectNamePrefix);
				sb.Append(columns[i]);
				sb.Append(builder.ObjectNameSuffix);
				if(i != columns.Length - 1) sb.Append(", ");
			}
			sb.Append(") values (");
			for(int i = 0; i < parameters.Length; i++) {
				if(builder.UseNamedParameters) {
					sb.Append(builder.ParameterNamePrefix);
					sb.Append(parameters[i]);
					sb.Append(builder.ParameterNameSuffix);
				}
				else {
					sb.Append(builder.ParameterPlaceholder);
				}

				if(i != parameters.Length - 1) sb.Append(", ");
			}
			sb.Append(")");

			return sb.ToString();
		}

		/// <summary>
		/// Builds an UPDATE query.
		/// </summary>
		/// <param name="table">The table.</param>
		/// <param name="columns">The columns to update.</param>
		/// <param name="parameters">The parameters.</param>
		/// <returns>The UPDATE query, without any WHERE clause.</returns>
		public string Update(string table, string[] columns, string[] parameters) {
			StringBuilder sb = new StringBuilder(100);
			sb.Append("update ");

			sb.Append(builder.ObjectNamePrefix);
			sb.Append(table);
			sb.Append(builder.ObjectNameSuffix);

			sb.Append(" set ");
			for(int i = 0; i < columns.Length; i++) {
				sb.Append(builder.ObjectNamePrefix);
				sb.Append(columns[i]);
				sb.Append(builder.ObjectNameSuffix);
				sb.Append(" = ");

				if(builder.UseNamedParameters) {
					sb.Append(builder.ParameterNamePrefix);
					sb.Append(parameters[i]);
					sb.Append(builder.ParameterNameSuffix);
				}
				else {
					sb.Append(builder.ParameterPlaceholder);
				}

				if(i != columns.Length - 1) sb.Append(", ");
			}

			return sb.ToString();
		}

		/// <summary>
		/// Builds an UPDATE query that increments the numerical value of a column by one.
		/// </summary>
		/// <param name="table">The table.</param>
		/// <param name="column">The column to update.</param>
		/// <param name="increment">The increment or decrement value.</param>
		/// <returns>The UPDATE query, without any WHERE clause.</returns>
		public string UpdateIncrement(string table, string column, int increment) {
			StringBuilder sb = new StringBuilder(100);
			sb.Append("update ");

			sb.Append(builder.ObjectNamePrefix);
			sb.Append(table);
			sb.Append(builder.ObjectNameSuffix);

			sb.Append(" set ");

			sb.Append(builder.ObjectNamePrefix);
			sb.Append(column);
			sb.Append(builder.ObjectNameSuffix);

			sb.Append(" = ");

			sb.Append(builder.ObjectNamePrefix);
			sb.Append(column);
			sb.Append(builder.ObjectNameSuffix);

			if(increment > 0) sb.Append(" + ");
			else sb.Append(" - ");
			sb.Append(Math.Abs(increment).ToString());

			return sb.ToString();
		}

		/// <summary>
		/// Builds a DELETE FROM query.
		/// </summary>
		/// <param name="table">The table.</param>
		/// <returns>The DELETE FROM query, without any WHERE clause.</returns>
		public string DeleteFrom(string table) {
			StringBuilder sb = new StringBuilder(100);
			sb.Append("delete from ");

			sb.Append(builder.ObjectNamePrefix);
			sb.Append(table);
			sb.Append(builder.ObjectNameSuffix);

			return sb.ToString();
		}

		/// <summary>
		/// Appends a query to an existing query for batch execution.
		/// </summary>
		/// <param name="query">The query.</param>
		/// <param name="secondQuery">The second query.</param>
		/// <returns>The resulting query.</returns>
		public string AppendForBatch(string query, string secondQuery) {
			return query + builder.BatchQuerySeparator + secondQuery;
		}

	}

	/// <summary>
	/// Lists WHERE operators.
	/// </summary>
	public enum WhereOperator {
		/// <summary>
		/// LIKE.
		/// </summary>
		Like,
		/// <summary>
		/// =.
		/// </summary>
		Equals,
		/// <summary>
		/// &lt;&gt;.
		/// </summary>
		NotEquals,
		/// <summary>
		/// &gt;.
		/// </summary>
		GreaterThan,
		/// <summary>
		/// &lt;
		/// </summary>
		LessThan,
		/// <summary>
		/// &gt;=.
		/// </summary>
		GreaterThanOrEqualTo,
		/// <summary>
		/// &lt;=.
		/// </summary>
		LessThanOrEqualTo,
		/// <summary>
		/// IS NULL.
		/// </summary>
		IsNull,
		/// <summary>
		/// IS NOT NULL.
		/// </summary>
		IsNotNull
	}

	/// <summary>
	/// List JOIN types.
	/// </summary>
	public enum Join {
		/// <summary>
		/// JOIN.
		/// </summary>
		Join,
		/// <summary>
		/// INNER JOIN.
		/// </summary>
		InnerJoin,
		/// <summary>
		/// LEFT JOIN.
		/// </summary>
		LeftJoin,
		/// <summary>
		/// RIGHT JOIN.
		/// </summary>
		RightJoin
	}

	/// <summary>
	/// Lists ordering directions.
	/// </summary>
	public enum Ordering {
		/// <summary>
		/// Ascending.
		/// </summary>
		Asc,
		/// <summary>
		/// Descending.
		/// </summary>
		Desc
	}

}
