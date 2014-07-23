
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;
using NUnit.Framework;
using Rhino.Mocks;
using ScrewTurn.Wiki.Tests;
using ScrewTurn.Wiki.PluginFramework;
using ScrewTurn.Wiki.Plugins.SqlCommon;
using ScrewTurn.Wiki.SearchEngine;

namespace ScrewTurn.Wiki.Plugins.SqlServer.Tests {

	[TestFixture]
	public class SqlServerPagesStorageProviderTests : PagesStorageProviderTestScaffolding {

		//private const string ConnString = "Data Source=(local)\\SQLExpress;User ID=sa;Password=password;";
		private const string ConnString = "Data Source=(local)\\SQLExpress;Integrated Security=SSPI;";
		private const string InitialCatalog = "Initial Catalog=ScrewTurnWikiTest;";

		public override IPagesStorageProviderV30 GetProvider() {
			SqlServerPagesStorageProvider prov = new SqlServerPagesStorageProvider();
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
			cmd.CommandText = "use [ScrewTurnWikiTest]; delete from [IndexWordMapping]; delete from [IndexWord]; delete from [IndexDocument]; delete from [ContentTemplate]; delete from [Snippet]; delete from [NavigationPath]; delete from [Message]; delete from [PageKeyword]; delete from [PageContent]; delete from [CategoryBinding]; delete from [Page]; delete from [Category]; delete from [Namespace] where [Name] <> '';";
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
			IPagesStorageProviderV30 prov = GetProvider();
			prov.Init(MockHost(), ConnString + InitialCatalog);

			Assert.IsNotNull(prov.Information, "Information should not be null");
		}

		[TestCase("", ExpectedException = typeof(InvalidConfigurationException))]
		[TestCase("blah", ExpectedException = typeof(InvalidConfigurationException))]
		[TestCase("Data Source=(local)\\SQLExpress;User ID=inexistent;Password=password;InitialCatalog=Inexistent;", ExpectedException = typeof(InvalidConfigurationException))]
		public void Init_InvalidConnString(string c) {
			IPagesStorageProviderV30 prov = GetProvider();
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
@"CREATE TABLE [PagesProviderVersion] (
	[Version] varchar(12) PRIMARY KEY
);
INSERT INTO [PagesProviderVersion] ([Version]) VALUES ('Irrelevant');
create table [Page] (
	[Name] nvarchar(128) primary key,
	[Status] char not null default ('N'), -- (P)ublic, N(ormal), (L)ocked
	[CreationDateTime] datetime not null
);

create table [PageContent] (
	[Page] nvarchar(128) references [Page]([Name]) on update cascade on delete cascade,
	[Revision] int not null default ((-1)), -- -1 for Current Revision
	[Title] nvarchar(256) not null,
	[DateTime] datetime not null,
	[Username] nvarchar(64) not null,
	[Content] ntext not null,
	[Comment] nvarchar(128) not null,
	primary key ([Page], [Revision])
);

create table [Category] (
	[Name] nvarchar(128) primary key
);

create table [CategoryBinding] (
	[Category] nvarchar(128) references [Category]([Name]) on update cascade on delete cascade,
	[Page] nvarchar(128) references [Page]([Name]) on update cascade on delete cascade,
	primary key ([Category], [Page])
);

create table [Message] (
	[ID] int primary key identity,
	[Page] nvarchar(128) references [Page]([Name]) on update cascade on delete cascade,
	[Parent] int not null, -- -1 for no parent
	[Username] nvarchar(64) not null,
	[DateTime] datetime not null,
	[Subject] nvarchar(128) not null,
	[Body] ntext not null
);

create table [Snippet] (
	[Name] nvarchar(128) primary key,
	[Content] ntext not null
);

create table [NavigationPath] (
	[Name] nvarchar(128) not null primary key
);

create table [NavigationPathBinding] (
	[NavigationPath] nvarchar(128) not null references [NavigationPath]([Name]) on delete cascade,
	[Page] nvarchar(128) not null references [Page]([Name]) on update cascade on delete cascade,
	[Number] int not null,
	primary key ([NavigationPath], [Page], [Number])
);

insert into [Page] ([Name], [Status], [CreationDateTime]) values ('Page1', 'N', '2008/12/31 12:12:12');
insert into [Page] ([Name], [Status], [CreationDateTime]) values ('Page2', 'L', '2008/12/31 12:12:12');
insert into [Page] ([Name], [Status], [CreationDateTime]) values ('Page.WithDot', 'P', '2008/12/31 12:12:12');

insert into [PageContent] ([Page], [Revision], [Title], [DateTime], [Username], [Content], [Comment]) values ('Page1', -1, 'Page1 Title', '2008/12/31 14:14:14', 'SYSTEM', 'Test Content 1', 'Comment 1');
insert into [PageContent] ([Page], [Revision], [Title], [DateTime], [Username], [Content], [Comment]) values ('Page1', 0, 'Page1 Title 0', '2008/12/31 12:12:12', 'SYSTEM', 'Test Content 0', '');
insert into [PageContent] ([Page], [Revision], [Title], [DateTime], [Username], [Content], [Comment]) values ('Page2', -1, 'Page2 Title', '2008/12/31 14:14:14', 'SYSTEM', 'Test Content 2', 'Comment 2');
insert into [PageContent] ([Page], [Revision], [Title], [DateTime], [Username], [Content], [Comment]) values ('Page.WithDot', -1, 'Page.WithDot Title', '2008/12/31 14:14:14', 'SYSTEM', 'Test Content 3', 'Comment 3');

insert into [Category] ([Name]) values ('Cat1');
insert into [Category] ([Name]) values ('Cat2');
insert into [Category] ([Name]) values ('Cat.WithDot');

insert into [CategoryBinding] ([Category], [Page]) values ('Cat1', 'Page1');
insert into [CategoryBinding] ([Category], [Page]) values ('Cat2', 'Page1');
insert into [CategoryBinding] ([Category], [Page]) values ('Cat1', 'Page2');
insert into [CategoryBinding] ([Category], [Page]) values ('Cat2', 'Page.WithDot');
insert into [CategoryBinding] ([Category], [Page]) values ('Cat.WithDot', 'Page.WithDot');

insert into [Message] ([Page], [Parent], [Username], [DateTime], [Subject], [Body]) values ('Page1', -1, 'SYSTEM', '2008/12/31 16:16:16', 'Test 1', 'Body 1');
insert into [Message] ([Page], [Parent], [Username], [DateTime], [Subject], [Body]) values ('Page1', 0, 'SYSTEM', '2008/12/31 16:16:16', 'Test 1.1', 'Body 1.1');
insert into [Message] ([Page], [Parent], [Username], [DateTime], [Subject], [Body]) values ('Page.WithDot', -1, 'SYSTEM', '2008/12/31 16:16:16', 'Test dot', 'Body dot');

insert into [Snippet] ([Name], [Content]) values ('Snip', 'Content');

insert into [NavigationPath] ([Name]) values ('Path');

insert into [NavigationPathBinding] ([NavigationPath], [Page], [Number]) values ('Path', 'Page1', 1);
insert into [NavigationPathBinding] ([NavigationPath], [Page], [Number]) values ('Path', 'Page2', 2);
insert into [NavigationPathBinding] ([NavigationPath], [Page], [Number]) values ('Path', 'Page.WithDot', 3);";

			bool done = false;
			try {
				cmd.ExecuteNonQuery();
				done = true;
			}
			catch(SqlException sqlex) {
				Console.WriteLine(sqlex);
			}
			finally {
				cn.Close();
			}

			if(!done) throw new Exception("Could not generate v2 test database");

			MockRepository mocks = new MockRepository();
			IHostV30 host = mocks.DynamicMock<IHostV30>();
			Expect.Call(host.UpgradePageStatusToAcl(null, 'L')).IgnoreArguments().Repeat.Twice().Return(true);

			mocks.Replay(host);

			SqlServerPagesStorageProvider prov = new SqlServerPagesStorageProvider();
			prov.Init(host, ConnString + InitialCatalog);

			Snippet[] snippets = prov.GetSnippets();
			Assert.AreEqual(1, snippets.Length, "Wrong snippet count");
			Assert.AreEqual("Snip", snippets[0].Name, "Wrong snippet name");
			Assert.AreEqual("Content", snippets[0].Content, "Wrong snippet content");

			PageInfo[] pages = prov.GetPages(null);
			Assert.AreEqual(3, pages.Length, "Wrong page count");
			Assert.AreEqual("Page_WithDot", pages[0].FullName, "Wrong page name");
			Assert.AreEqual("Page1", pages[1].FullName, "Wrong page name");
			Assert.AreEqual("Page2", pages[2].FullName, "Wrong page name");

			Assert.AreEqual("Test Content 3", prov.GetContent(pages[0]).Content, "Wrong content");
			Assert.AreEqual("Test Content 1", prov.GetContent(pages[1]).Content, "Wrong content");
			Assert.AreEqual("Test Content 0", prov.GetBackupContent(pages[1], 0).Content, "Wrong backup content");
			Assert.AreEqual("Test Content 2", prov.GetContent(pages[2]).Content, "Wrong content");

			Message[] messages = prov.GetMessages(pages[0]);
			Assert.AreEqual(1, messages.Length, "Wrong message count");
			Assert.AreEqual("Test dot", messages[0].Subject, "Wrong message subject");

			CategoryInfo[] categories = prov.GetCategories(null);
			Assert.AreEqual(3, categories.Length, "Wrong category count");
			Assert.AreEqual("Cat_WithDot", categories[0].FullName, "Wrong category name");
			Assert.AreEqual(1, categories[0].Pages.Length, "Wrong page count");
			Assert.AreEqual("Page_WithDot", categories[0].Pages[0], "Wrong page");
			Assert.AreEqual("Cat1", categories[1].FullName, "Wrong category name");
			Assert.AreEqual("Page1", categories[1].Pages[0], "Wrong page");
			Assert.AreEqual("Page2", categories[1].Pages[1], "Wrong page");
			Assert.AreEqual("Cat2", categories[2].FullName, "Wrong category name");
			Assert.AreEqual("Page_WithDot", categories[2].Pages[0], "Wrong page");
			Assert.AreEqual("Page1", categories[2].Pages[1], "Wrong page");

			NavigationPath[] paths = prov.GetNavigationPaths(null);
			Assert.AreEqual(1, paths.Length, "Wrong navigation path count");
			Assert.AreEqual("Path", paths[0].FullName, "Wrong navigation path name");
			Assert.AreEqual("Page1", paths[0].Pages[0], "Wrong page");
			Assert.AreEqual("Page2", paths[0].Pages[1], "Wrong page");
			Assert.AreEqual("Page_WithDot", paths[0].Pages[2], "Wrong page");

			mocks.Verify(host);
		}

	}

}
