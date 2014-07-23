
using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Rhino.Mocks;

namespace ScrewTurn.Wiki.Plugins.SqlCommon.Tests {

	[TestFixture]
	public class QueryBuilderTests {

		private MockRepository mocks = new MockRepository();

		/// <summary>
		/// Gets a new command builder for testing purposes.
		/// </summary>
		/// <returns>The command builder.</returns>
		private ICommandBuilder MockCommandBuilderWithNamedParameters() {
			ICommandBuilder builder = mocks.StrictMock<ICommandBuilder>();
			Expect.Call(builder.ObjectNamePrefix).Return("[").Repeat.Any();
			Expect.Call(builder.ObjectNameSuffix).Return("]").Repeat.Any();
			Expect.Call(builder.ParameterNamePrefix).Return("@").Repeat.Any();
			Expect.Call(builder.ParameterNameSuffix).Return("").Repeat.Any();
			Expect.Call(builder.UseNamedParameters).Return(true).Repeat.Any();
			Expect.Call(builder.BatchQuerySeparator).Return("; ").Repeat.Any();

			mocks.Replay(builder);

			return builder;
		}

		/// <summary>
		/// Gets a new command builder for testing purposes.
		/// </summary>
		/// <returns>The command builder.</returns>
		private ICommandBuilder MockCommandBuilderWithoutNamedParameters() {
			ICommandBuilder builder = mocks.StrictMock<ICommandBuilder>();
			Expect.Call(builder.ObjectNamePrefix).Return("[").Repeat.Any();
			Expect.Call(builder.ObjectNameSuffix).Return("]").Repeat.Any();
			Expect.Call(builder.ParameterPlaceholder).Return("?").Repeat.Any();
			Expect.Call(builder.UseNamedParameters).Return(false).Repeat.Any();
			Expect.Call(builder.BatchQuerySeparator).Return("; ").Repeat.Any();

			mocks.Replay(builder);

			return builder;
		}

		[Test]
		public void SelectFrom_Columns() {
			string query = QueryBuilder.NewQuery(MockCommandBuilderWithNamedParameters()).SelectFrom("Table", new string[] { "Col1", "Col2" });

			Assert.AreEqual("select [Col1], [Col2] from [Table]", query, "Wrong query");
		}

		[Test]
		public void SelectFrom_All() {
			string query = QueryBuilder.NewQuery(MockCommandBuilderWithNamedParameters()).SelectFrom("Table");

			Assert.AreEqual("select * from [Table]", query, "Wrong query");
		}

		[Test]
		public void SelectFrom_ColumnsJoin() {
			string query = QueryBuilder.NewQuery(MockCommandBuilderWithNamedParameters()).SelectFrom(
				"Table", "Joined", "Col1", "Col2", Join.InnerJoin,
				new string[] { "Col1", "Col2" }, new string[] { "Col5", "Col9" });

			Assert.AreEqual("select [Table].[Col1] as [Table_Col1], [Table].[Col2] as [Table_Col2], [Joined].[Col5] as [Joined_Col5], [Joined].[Col9] as [Joined_Col9] from [Table] inner join [Joined] on [Table].[Col1] = [Joined].[Col2]", query, "Wrong query");
		}

		[Test]
		public void SelectFrom_ColumnsJoinMultiple() {
			string query = QueryBuilder.NewQuery(MockCommandBuilderWithNamedParameters()).SelectFrom(
				"Table", "Joined", new string[] { "Col1", "Col2" }, new string[] { "Col3", "Col4" }, Join.RightJoin,
				new string[] { "Col6", "Col7" }, new string[] { "Col8", "Col9" });

			Assert.AreEqual("select [Table].[Col6] as [Table_Col6], [Table].[Col7] as [Table_Col7], [Joined].[Col8] as [Joined_Col8], [Joined].[Col9] as [Joined_Col9] from [Table] right join [Joined] on [Table].[Col1] = [Joined].[Col3] and [Table].[Col2] = [Joined].[Col4]", query, "Wrong query");
		}

		[Test]
		public void SelectFrom_AllJoin() {
			string query = QueryBuilder.NewQuery(MockCommandBuilderWithNamedParameters()).SelectFrom(
				"Table", "Joined", "Col1", "Col2", Join.LeftJoin);

			Assert.AreEqual("select * from [Table] left join [Joined] on [Table].[Col1] = [Joined].[Col2]", query, "Wrong query");
		}

		[Test]
		public void SelectFrom_ColumnsJoin2() {
			string query = QueryBuilder.NewQuery(MockCommandBuilderWithNamedParameters()).SelectFrom(
				"Table1", "Table2", "Col1", "Col2", Join.Join,
				new string[] { "Col1", "Col2" }, new string[] { "Col4", "Col6" },
				"Table3", "Col5", Join.LeftJoin, new string[] { "Col34" });

			Assert.AreEqual("select [Table1].[Col1] as [Table1_Col1], [Table1].[Col2] as [Table1_Col2], [Table2].[Col4] as [Table2_Col4], [Table2].[Col6] as [Table2_Col6], [Table3].[Col34] as [Table3_Col34] from [Table1] join [Table2] on [Table1].[Col1] = [Table2].[Col2] left join [Table3] on [Table1].[Col1] = [Table3].[Col5]", query, "Wrong query");
		}

		[Test]
		public void SelectCountFrom() {
			string query = QueryBuilder.NewQuery(MockCommandBuilderWithNamedParameters()).SelectCountFrom("Table");

			Assert.AreEqual("select count(*) from [Table]", query, "Wrong query");
		}

		[Test]
		public void Where_AndWhere_NamedParameter() {
			string query = QueryBuilder.NewQuery(MockCommandBuilderWithNamedParameters()).SelectFrom("Table");

			query = QueryBuilder.NewQuery(MockCommandBuilderWithNamedParameters()).Where(query, "Col1", WhereOperator.LessThanOrEqualTo, "Param1");
			query = QueryBuilder.NewQuery(MockCommandBuilderWithNamedParameters()).AndWhere(query, "Col2", WhereOperator.Equals, "Param2");

			Assert.AreEqual("select * from [Table] where [Col1] <= @Param1 and [Col2] = @Param2", query, "Wrong query");

			query = QueryBuilder.NewQuery(MockCommandBuilderWithNamedParameters()).SelectFrom("Table");

			query = QueryBuilder.NewQuery(MockCommandBuilderWithNamedParameters()).Where(query, "Col1", WhereOperator.LessThanOrEqualTo, "Param1", true, false);
			query = QueryBuilder.NewQuery(MockCommandBuilderWithNamedParameters()).AndWhere(query, "Col2", WhereOperator.Equals, "Param2", true, false);
			query = QueryBuilder.NewQuery(MockCommandBuilderWithNamedParameters()).AndWhere(query, "Col3", WhereOperator.Equals, "Param3", false, true) + ")";

			Assert.AreEqual("select * from [Table] where ([Col1] <= @Param1 and ([Col2] = @Param2 and [Col3] = @Param3))", query, "Wrong query");
		}

		[Test]
		public void Where_AndWhere_UnnamedParameter() {
			string query = QueryBuilder.NewQuery(MockCommandBuilderWithoutNamedParameters()).SelectFrom("Table");

			query = QueryBuilder.NewQuery(MockCommandBuilderWithoutNamedParameters()).Where(query, "Col1", WhereOperator.LessThanOrEqualTo, "Param1");
			query = QueryBuilder.NewQuery(MockCommandBuilderWithoutNamedParameters()).AndWhere(query, "Col2", WhereOperator.Equals, "Param2");

			Assert.AreEqual("select * from [Table] where [Col1] <= ? and [Col2] = ?", query, "Wrong query");

			query = QueryBuilder.NewQuery(MockCommandBuilderWithoutNamedParameters()).SelectFrom("Table");

			query = QueryBuilder.NewQuery(MockCommandBuilderWithoutNamedParameters()).Where(query, "Col1", WhereOperator.LessThanOrEqualTo, "Param1", true, false);
			query = QueryBuilder.NewQuery(MockCommandBuilderWithoutNamedParameters()).AndWhere(query, "Col2", WhereOperator.Equals, "Param2", true, false);
			query = QueryBuilder.NewQuery(MockCommandBuilderWithoutNamedParameters()).AndWhere(query, "Col3", WhereOperator.Equals, "Param3", false, true) + ")";

			Assert.AreEqual("select * from [Table] where ([Col1] <= ? and ([Col2] = ? and [Col3] = ?))", query, "Wrong query");
		}

		[Test]
		public void Where_OrWhere_NamedParameter() {
			string query = QueryBuilder.NewQuery(MockCommandBuilderWithNamedParameters()).SelectFrom("Table");

			query = QueryBuilder.NewQuery(MockCommandBuilderWithNamedParameters()).Where(query, "Col1", WhereOperator.LessThanOrEqualTo, "Param1");
			query = QueryBuilder.NewQuery(MockCommandBuilderWithNamedParameters()).OrWhere(query, "Col2", WhereOperator.Equals, "Param2");

			Assert.AreEqual("select * from [Table] where [Col1] <= @Param1 or [Col2] = @Param2", query, "Wrong query");

			query = QueryBuilder.NewQuery(MockCommandBuilderWithNamedParameters()).SelectFrom("Table");

			query = QueryBuilder.NewQuery(MockCommandBuilderWithNamedParameters()).Where(query, "Col1", WhereOperator.LessThanOrEqualTo, "Param1", true, false);
			query = QueryBuilder.NewQuery(MockCommandBuilderWithNamedParameters()).OrWhere(query, "Col2", WhereOperator.Equals, "Param2", true, false);
			query = QueryBuilder.NewQuery(MockCommandBuilderWithNamedParameters()).OrWhere(query, "Col3", WhereOperator.Equals, "Param3", false, true) + ")";

			Assert.AreEqual("select * from [Table] where ([Col1] <= @Param1 or ([Col2] = @Param2 or [Col3] = @Param3))", query, "Wrong query");
		}

		[Test]
		public void Where_OrWhere_UnnamedParameter() {
			string query = QueryBuilder.NewQuery(MockCommandBuilderWithoutNamedParameters()).SelectFrom("Table");

			query = QueryBuilder.NewQuery(MockCommandBuilderWithoutNamedParameters()).Where(query, "Col1", WhereOperator.LessThanOrEqualTo, "Param1");
			query = QueryBuilder.NewQuery(MockCommandBuilderWithoutNamedParameters()).OrWhere(query, "Col2", WhereOperator.Equals, "Param2");

			Assert.AreEqual("select * from [Table] where [Col1] <= ? or [Col2] = ?", query, "Wrong query");

			query = QueryBuilder.NewQuery(MockCommandBuilderWithoutNamedParameters()).SelectFrom("Table");

			query = QueryBuilder.NewQuery(MockCommandBuilderWithoutNamedParameters()).Where(query, "Col1", WhereOperator.LessThanOrEqualTo, "Param1", true, false);
			query = QueryBuilder.NewQuery(MockCommandBuilderWithoutNamedParameters()).OrWhere(query, "Col2", WhereOperator.Equals, "Param2", true, false);
			query = QueryBuilder.NewQuery(MockCommandBuilderWithoutNamedParameters()).OrWhere(query, "Col3", WhereOperator.Equals, "Param3", false, true) + ")";

			Assert.AreEqual("select * from [Table] where ([Col1] <= ? or ([Col2] = ? or [Col3] = ?))", query, "Wrong query");
		}

		[Test]
		public void Where_Table_AndWhere_Table_NamedParameter() {
			string query = QueryBuilder.NewQuery(MockCommandBuilderWithNamedParameters()).SelectFrom("Table");

			query = QueryBuilder.NewQuery(MockCommandBuilderWithNamedParameters()).Where(query, "Table", "Col1", WhereOperator.LessThanOrEqualTo, "Param1");
			query = QueryBuilder.NewQuery(MockCommandBuilderWithNamedParameters()).AndWhere(query, "Table", "Col2", WhereOperator.Equals, "Param2");

			Assert.AreEqual("select * from [Table] where [Table].[Col1] <= @Param1 and [Table].[Col2] = @Param2", query, "Wrong query");

			query = QueryBuilder.NewQuery(MockCommandBuilderWithNamedParameters()).SelectFrom("Table");

			query = QueryBuilder.NewQuery(MockCommandBuilderWithNamedParameters()).Where(query, "Table", "Col1", WhereOperator.LessThanOrEqualTo, "Param1", true, false);
			query = QueryBuilder.NewQuery(MockCommandBuilderWithNamedParameters()).AndWhere(query, "Table", "Col2", WhereOperator.Equals, "Param2", true, false);
			query = QueryBuilder.NewQuery(MockCommandBuilderWithNamedParameters()).AndWhere(query, "Table", "Col3", WhereOperator.Equals, "Param3", false, true) + ")";

			Assert.AreEqual("select * from [Table] where ([Table].[Col1] <= @Param1 and ([Table].[Col2] = @Param2 and [Table].[Col3] = @Param3))", query, "Wrong query");
		}

		[Test]
		public void Where_Table_AndWhere_Table_UnnamedParameter() {
			string query = QueryBuilder.NewQuery(MockCommandBuilderWithNamedParameters()).SelectFrom("Table");

			query = QueryBuilder.NewQuery(MockCommandBuilderWithoutNamedParameters()).Where(query, "Table", "Col1", WhereOperator.LessThanOrEqualTo, "Param1");
			query = QueryBuilder.NewQuery(MockCommandBuilderWithoutNamedParameters()).AndWhere(query, "Table", "Col2", WhereOperator.Equals, "Param2");

			Assert.AreEqual("select * from [Table] where [Table].[Col1] <= ? and [Table].[Col2] = ?", query, "Wrong query");

			query = QueryBuilder.NewQuery(MockCommandBuilderWithNamedParameters()).SelectFrom("Table");

			query = QueryBuilder.NewQuery(MockCommandBuilderWithoutNamedParameters()).Where(query, "Table", "Col1", WhereOperator.LessThanOrEqualTo, "Param1", true, false);
			query = QueryBuilder.NewQuery(MockCommandBuilderWithoutNamedParameters()).AndWhere(query, "Table", "Col2", WhereOperator.Equals, "Param2", true, false);
			query = QueryBuilder.NewQuery(MockCommandBuilderWithoutNamedParameters()).AndWhere(query, "Table", "Col3", WhereOperator.Equals, "Param3", false, true) + ")";

			Assert.AreEqual("select * from [Table] where ([Table].[Col1] <= ? and ([Table].[Col2] = ? and [Table].[Col3] = ?))", query, "Wrong query");
		}

		[Test]
		public void Where_Table_OrWhere_Table_NamedParameter() {
			string query = QueryBuilder.NewQuery(MockCommandBuilderWithNamedParameters()).SelectFrom("Table");

			query = QueryBuilder.NewQuery(MockCommandBuilderWithNamedParameters()).Where(query, "Table", "Col1", WhereOperator.LessThanOrEqualTo, "Param1");
			query = QueryBuilder.NewQuery(MockCommandBuilderWithNamedParameters()).OrWhere(query, "Table", "Col2", WhereOperator.Equals, "Param2");

			Assert.AreEqual("select * from [Table] where [Table].[Col1] <= @Param1 or [Table].[Col2] = @Param2", query, "Wrong query");

			query = QueryBuilder.NewQuery(MockCommandBuilderWithNamedParameters()).SelectFrom("Table");

			query = QueryBuilder.NewQuery(MockCommandBuilderWithNamedParameters()).Where(query, "Table", "Col1", WhereOperator.LessThanOrEqualTo, "Param1", true, false);
			query = QueryBuilder.NewQuery(MockCommandBuilderWithNamedParameters()).OrWhere(query, "Table", "Col2", WhereOperator.Equals, "Param2", true, false);
			query = QueryBuilder.NewQuery(MockCommandBuilderWithNamedParameters()).OrWhere(query, "Table", "Col3", WhereOperator.Equals, "Param3", false, true) + ")";

			Assert.AreEqual("select * from [Table] where ([Table].[Col1] <= @Param1 or ([Table].[Col2] = @Param2 or [Table].[Col3] = @Param3))", query, "Wrong query");
		}

		[Test]
		public void Where_Table_OrWhere_Table_UnnamedParameter() {
			string query = QueryBuilder.NewQuery(MockCommandBuilderWithNamedParameters()).SelectFrom("Table");

			query = QueryBuilder.NewQuery(MockCommandBuilderWithoutNamedParameters()).Where(query, "Table", "Col1", WhereOperator.LessThanOrEqualTo, "Param1");
			query = QueryBuilder.NewQuery(MockCommandBuilderWithoutNamedParameters()).OrWhere(query, "Table", "Col2", WhereOperator.Equals, "Param2");

			Assert.AreEqual("select * from [Table] where [Table].[Col1] <= ? or [Table].[Col2] = ?", query, "Wrong query");

			query = QueryBuilder.NewQuery(MockCommandBuilderWithNamedParameters()).SelectFrom("Table");

			query = QueryBuilder.NewQuery(MockCommandBuilderWithoutNamedParameters()).Where(query, "Table", "Col1", WhereOperator.LessThanOrEqualTo, "Param1", true, false);
			query = QueryBuilder.NewQuery(MockCommandBuilderWithoutNamedParameters()).OrWhere(query, "Table", "Col2", WhereOperator.Equals, "Param2", true, false);
			query = QueryBuilder.NewQuery(MockCommandBuilderWithoutNamedParameters()).OrWhere(query, "Table", "Col3", WhereOperator.Equals, "Param3", false, true) + ")";

			Assert.AreEqual("select * from [Table] where ([Table].[Col1] <= ? or ([Table].[Col2] = ? or [Table].[Col3] = ?))", query, "Wrong query");
		}

		[Test]
		public void WhereIn_NamedParameters() {
			string query = QueryBuilder.NewQuery(MockCommandBuilderWithNamedParameters()).SelectFrom("Table");

			query = QueryBuilder.NewQuery(MockCommandBuilderWithNamedParameters()).WhereIn(query, "Col1", new string[] { "Param1", "Param2" });

			Assert.AreEqual("select * from [Table] where [Col1] in (@Param1, @Param2)", query, "Wrong query");
		}

		[Test]
		public void WhereIn_UnnamedParameters() {
			string query = QueryBuilder.NewQuery(MockCommandBuilderWithoutNamedParameters()).SelectFrom("Table");

			query = QueryBuilder.NewQuery(MockCommandBuilderWithoutNamedParameters()).WhereIn(query, "Col1", new string[] { "Param1", "Param2" });

			Assert.AreEqual("select * from [Table] where [Col1] in (?, ?)", query, "Wrong query");
		}

		[Test]
		public void WhereIn_Table_NamedParameters() {
			string query = QueryBuilder.NewQuery(MockCommandBuilderWithNamedParameters()).SelectFrom("Table");

			query = QueryBuilder.NewQuery(MockCommandBuilderWithNamedParameters()).WhereIn(query, "Table", "Col1", new string[] { "Param1", "Param2" });

			Assert.AreEqual("select * from [Table] where [Table].[Col1] in (@Param1, @Param2)", query, "Wrong query");
		}

		[Test]
		public void WhereIn_Table_UnnamedParameters() {
			string query = QueryBuilder.NewQuery(MockCommandBuilderWithoutNamedParameters()).SelectFrom("Table");

			query = QueryBuilder.NewQuery(MockCommandBuilderWithoutNamedParameters()).WhereIn(query, "Table", "Col1", new string[] { "Param1", "Param2" });

			Assert.AreEqual("select * from [Table] where [Table].[Col1] in (?, ?)", query, "Wrong query");
		}

		[Test]
		public void WhereNotIn_NamedParameters() {
			string query = QueryBuilder.NewQuery(MockCommandBuilderWithNamedParameters()).DeleteFrom("Table");

			query = QueryBuilder.NewQuery(MockCommandBuilderWithNamedParameters()).WhereNotInSubquery(query, "Table", "Col1", "fake_sub_query");

			Assert.AreEqual("delete from [Table] where [Table].[Col1] not in (fake_sub_query)", query, "Wrong query");
		}

		[Test]
		public void WhereNotIn_UnnamedParameters() {
			string query = QueryBuilder.NewQuery(MockCommandBuilderWithoutNamedParameters()).DeleteFrom("Table");

			query = QueryBuilder.NewQuery(MockCommandBuilderWithoutNamedParameters()).WhereNotInSubquery(query, "Table", "Col1", "fake_sub_query");

			Assert.AreEqual("delete from [Table] where [Table].[Col1] not in (fake_sub_query)", query, "Wrong query");
		}

		[Test]
		public void OrderBy() {
			string query = QueryBuilder.NewQuery(MockCommandBuilderWithNamedParameters()).SelectFrom("Table");

			query = QueryBuilder.NewQuery(MockCommandBuilderWithNamedParameters()).OrderBy(query, new string[] { "Col1", "Col2", "Col3" }, new Ordering[] { Ordering.Asc, Ordering.Desc, Ordering.Asc });

			Assert.AreEqual("select * from [Table] order by [Col1] asc, [Col2] desc, [Col3] asc", query, "Wrong query");
		}

		[Test]
		public void GroupBy() {
			string query = QueryBuilder.NewQuery(MockCommandBuilderWithNamedParameters()).SelectFrom("Table", new string[] { "Col1", "Col2" });

			query = QueryBuilder.NewQuery(MockCommandBuilderWithNamedParameters()).GroupBy(query, new string[] { "Col2", "Col1" });

			Assert.AreEqual("select [Col1], [Col2] from [Table] group by [Col2], [Col1]", query, "Wrong query");
		}

		[Test]
		public void InsertInto_NamedParameters() {
			string query = QueryBuilder.NewQuery(MockCommandBuilderWithNamedParameters()).InsertInto("Table",
				new string[] { "Col1", "Col2" }, new string[] { "Param1", "Param2" });

			Assert.AreEqual("insert into [Table] ([Col1], [Col2]) values (@Param1, @Param2)", query, "Wrong query");
		}

		[Test]
		public void InsertInto_UnnamedParameters() {
			string query = QueryBuilder.NewQuery(MockCommandBuilderWithoutNamedParameters()).InsertInto("Table",
				new string[] { "Col1", "Col2" }, new string[] { "Param1", "Param2" });

			Assert.AreEqual("insert into [Table] ([Col1], [Col2]) values (?, ?)", query, "Wrong query");
		}

		[Test]
		public void Update_NamedParameters() {
			string query = QueryBuilder.NewQuery(MockCommandBuilderWithNamedParameters()).Update("Table",
				new string[] { "Col1", "Col2" }, new string[] { "Param1", "Param2" });

			Assert.AreEqual("update [Table] set [Col1] = @Param1, [Col2] = @Param2", query, "Wrong query");
		}

		[Test]
		public void Update_UnnamedParameters() {
			string query = QueryBuilder.NewQuery(MockCommandBuilderWithoutNamedParameters()).Update("Table",
				new string[] { "Col1", "Col2" }, new string[] { "Param1", "Param2" });

			Assert.AreEqual("update [Table] set [Col1] = ?, [Col2] = ?", query, "Wrong query");
		}

		[Test]
		public void UpdateIncrement_Positive() {
			string query = QueryBuilder.NewQuery(MockCommandBuilderWithNamedParameters()).UpdateIncrement("Table", "Col1", 3);

			Assert.AreEqual("update [Table] set [Col1] = [Col1] + 3", query, "Wrong query");
		}

		[Test]
		public void UpdateIncrement_Negative() {
			string query = QueryBuilder.NewQuery(MockCommandBuilderWithNamedParameters()).UpdateIncrement("Table", "Col1", -8);

			Assert.AreEqual("update [Table] set [Col1] = [Col1] - 8", query, "Wrong query");
		}

		[Test]
		public void DeleteFrom() {
			string query = QueryBuilder.NewQuery(MockCommandBuilderWithNamedParameters()).DeleteFrom("Table");

			Assert.AreEqual("delete from [Table]", query, "Wrong query");
		}

		[Test]
		public void AppendForBatch() {
			string query1 = QueryBuilder.NewQuery(MockCommandBuilderWithNamedParameters()).DeleteFrom("Table");
			string query2 = QueryBuilder.NewQuery(MockCommandBuilderWithNamedParameters()).DeleteFrom("Table2");

			string query = QueryBuilder.NewQuery(MockCommandBuilderWithNamedParameters()).AppendForBatch(query1, query2);

			Assert.AreEqual("delete from [Table]; delete from [Table2]", query, "Wrong query");
		}
		
	}

}
