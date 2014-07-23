
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using Rhino.Mocks;
using ScrewTurn.Wiki.SearchEngine;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki.Tests {

	[TestFixture]
	public abstract class PagesStorageProviderTestScaffolding {

		private MockRepository mocks = new MockRepository();
		private string testDir = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), Guid.NewGuid().ToString());

		private delegate string ToStringDelegate(PageInfo p, string input);

		protected IHostV30 MockHost() {
			if(!Directory.Exists(testDir)) Directory.CreateDirectory(testDir);

			IHostV30 host = mocks.DynamicMock<IHostV30>();
			Expect.Call(host.GetSettingValue(SettingName.PublicDirectory)).Return(testDir).Repeat.AtLeastOnce();
			Expect.Call(host.PrepareContentForIndexing(null, null)).IgnoreArguments().Do((ToStringDelegate)delegate(PageInfo p, string input) { return input; }).Repeat.Any();
			Expect.Call(host.PrepareTitleForIndexing(null, null)).IgnoreArguments().Do((ToStringDelegate)delegate(PageInfo p, string input) { return input; }).Repeat.Any();

			mocks.Replay(host);

			return host;
		}

		public abstract IPagesStorageProviderV30 GetProvider();

		[TearDown]
		public void TearDown() {
			try {
				Directory.Delete(testDir, true);
			}
			catch { }
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void Init_NullHost() {
			IPagesStorageProviderV30 prov = GetProvider();
			prov.Init(null, "");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void Init_NullConfig() {
			IPagesStorageProviderV30 prov = GetProvider();
			prov.Init(MockHost(), null);
		}

		private void AssertNamespaceInfosAreEqual(NamespaceInfo expected, NamespaceInfo actual, bool checkProvider) {
			Assert.AreEqual(expected.Name, actual.Name, "Wrong name");
			if(expected.DefaultPage == null) Assert.IsNull(actual.DefaultPage, "DefaultPage should be null");
			else AssertPageInfosAreEqual(expected.DefaultPage, actual.DefaultPage, true);
			if(checkProvider) Assert.AreSame(expected.Provider, actual.Provider);
		}

		[Test]
		public void AddNamespace_GetNamespaces() {
			IPagesStorageProviderV30 prov = GetProvider();

			Assert.AreEqual(0, prov.GetNamespaces().Length, "Wrong initial namespace count");

			NamespaceInfo ns1 = prov.AddNamespace("Sub1");
			NamespaceInfo ns2 = prov.AddNamespace("Sub2");
			NamespaceInfo ns3 = prov.AddNamespace("Spaced Namespace");
			Assert.IsNull(prov.AddNamespace("Sub1"), "AddNamespace should return null");

			AssertNamespaceInfosAreEqual(new NamespaceInfo("Sub1", prov, null), ns1, true);
			AssertNamespaceInfosAreEqual(new NamespaceInfo("Sub2", prov, null), ns2, true);
			AssertNamespaceInfosAreEqual(new NamespaceInfo("Spaced Namespace", prov, null), ns3, true);

			NamespaceInfo[] allNS = prov.GetNamespaces();
			Assert.AreEqual(3, allNS.Length, "Wrong namespace count");

			Array.Sort(allNS, delegate(NamespaceInfo x, NamespaceInfo y) { return x.Name.CompareTo(y.Name); });
			AssertNamespaceInfosAreEqual(ns3, allNS[0], true);
			AssertNamespaceInfosAreEqual(ns1, allNS[1], true);
			AssertNamespaceInfosAreEqual(ns2, allNS[2], true);
		}

		[Test]
		public void AddNamespace_GetNamespaces_WithDefaultPages() {
			IPagesStorageProviderV30 prov = GetProvider();

			Assert.AreEqual(0, prov.GetNamespaces().Length, "Wrong initial namespace count");

			NamespaceInfo ns1 = prov.AddNamespace("Sub1");
			NamespaceInfo ns2 = prov.AddNamespace("Sub2");
			Assert.IsNull(prov.AddNamespace("Sub1"), "AddNamespace should return null");

			PageInfo dp1 = prov.AddPage(ns1.Name, "MainPage", DateTime.Now);
			ns1 = prov.SetNamespaceDefaultPage(ns1, dp1);

			PageInfo dp2 = prov.AddPage(ns2.Name, "MainPage", DateTime.Now);
			ns2 = prov.SetNamespaceDefaultPage(ns2, dp2);

			AssertNamespaceInfosAreEqual(new NamespaceInfo("Sub1", prov, dp1), ns1, true);
			AssertNamespaceInfosAreEqual(new NamespaceInfo("Sub2", prov, dp2), ns2, true);

			NamespaceInfo[] allNS = prov.GetNamespaces();
			Assert.AreEqual(2, allNS.Length, "Wrong namespace count");

			Array.Sort(allNS, delegate(NamespaceInfo x, NamespaceInfo y) { return x.Name.CompareTo(y.Name); });
			AssertNamespaceInfosAreEqual(ns1, allNS[0], true);
			AssertNamespaceInfosAreEqual(ns2, allNS[1], true);
		}

		[Test]
		public void AddNamespace_GetNamespace() {
			IPagesStorageProviderV30 prov = GetProvider();

			Assert.IsNull(prov.GetNamespace("Sub1"), "GetNamespace should return null");

			NamespaceInfo ns1 = prov.AddNamespace("Sub1");
			NamespaceInfo ns2 = prov.AddNamespace("Sub2");

			Assert.IsNull(prov.GetNamespace("Sub3"), "GetNamespace should return null");

			NamespaceInfo ns1Out = prov.GetNamespace("Sub1");
			NamespaceInfo ns2Out = prov.GetNamespace("Sub2");

			AssertNamespaceInfosAreEqual(ns1, ns1Out, true);
			AssertNamespaceInfosAreEqual(ns2, ns2Out, true);
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void GetNamespace_InvalidName(string n) {
			IPagesStorageProviderV30 prov = GetProvider();

			prov.GetNamespace(n);
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void AddNamespace_InvalidName(string n) {
			IPagesStorageProviderV30 prov = GetProvider();
			prov.AddNamespace(n);
		}

		[Test]
		public void RenameNamespace() {
			IPagesStorageProviderV30 prov = GetProvider();

			NamespaceInfo sub = prov.AddNamespace("Sub");
			prov.AddNamespace("Sub2");

			CategoryInfo cat = prov.AddCategory(sub.Name, "Cat");

			PageInfo page = prov.AddPage("Sub", "Page", DateTime.Now);
			prov.AddMessage(page, "NUnit", "Test", DateTime.Now, "Body", -1);

			prov.SetNamespaceDefaultPage(sub, page);

			prov.RebindPage(page, new string[] { cat.FullName });

			Assert.IsNull(prov.RenameNamespace(new NamespaceInfo("Inexistent", prov, null), "NewName"), "RenameNamespace should return null");

			NamespaceInfo ns = prov.RenameNamespace(new NamespaceInfo("Sub", prov, null), "Sub1");

			AssertNamespaceInfosAreEqual(new NamespaceInfo("Sub1", prov, new PageInfo("Sub1.Page", prov, page.CreationDateTime)), ns, true);

			NamespaceInfo[] allNS = prov.GetNamespaces();
			Assert.AreEqual(2, allNS.Length, "Wrong namespace count");

			Array.Sort(allNS, delegate(NamespaceInfo x, NamespaceInfo y) { return x.Name.CompareTo(y.Name); });
			AssertNamespaceInfosAreEqual(new NamespaceInfo("Sub1", prov, new PageInfo("Sub1.Page", prov, page.CreationDateTime)), allNS[0], true);
			AssertNamespaceInfosAreEqual(new NamespaceInfo("Sub2", prov, null), allNS[1], true);

			Assert.AreEqual(1, prov.GetMessages(new PageInfo(NameTools.GetFullName("Sub1", "Page"), prov, page.CreationDateTime)).Length, "Wrong message count");

			CategoryInfo[] categories = prov.GetCategories(ns);
			Assert.AreEqual(1, categories.Length, "Wrong category count");
			Assert.AreEqual(NameTools.GetFullName(ns.Name, NameTools.GetLocalName(cat.FullName)), categories[0].FullName, "Wrong category name");
			Assert.AreEqual(1, categories[0].Pages.Length, "Wrong page count");
			Assert.AreEqual(NameTools.GetFullName(ns.Name, NameTools.GetLocalName(page.FullName)), categories[0].Pages[0], "Wrong page");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RenameNamespace_NullNamespace() {
			IPagesStorageProviderV30 prov = GetProvider();
			prov.RenameNamespace(null, "NewName");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void RenameNamespace_InvalidNewName(string n) {
			IPagesStorageProviderV30 prov = GetProvider();

			NamespaceInfo ns = prov.AddNamespace("Sub");

			prov.RenameNamespace(ns, n);
		}

		[Test]
		public void RenameNamespace_PerformSearch() {
			IPagesStorageProviderV30 prov = GetProvider();

			NamespaceInfo ns = prov.AddNamespace("NS");

			PageInfo page1 = prov.AddPage(ns.Name, "Page1", DateTime.Now);
			prov.ModifyPage(page1, "Title1", "NUnit", DateTime.Now, "Comment1", "Content1", new string[0], "Descr1", SaveMode.Normal);

			PageInfo page2 = prov.AddPage(ns.Name, "Page2", DateTime.Now);
			prov.ModifyPage(page2, "Title2", "NUnit", DateTime.Now, "Comment2", "Content2", new string[0], "Descr2", SaveMode.Normal);

			prov.AddMessage(page1, "NUnit", "Test1", DateTime.Now, "Body1", -1);
			prov.AddMessage(page1, "NUnit", "Test2", DateTime.Now, "Body2", prov.GetMessages(page1)[0].ID);

			NamespaceInfo renamedNamespace = prov.RenameNamespace(ns, "NS_Ren");

			SearchResultCollection result = prov.PerformSearch(new SearchParameters("content1 content2"));
			Assert.AreEqual(2, result.Count, "Wrong result count");
			Assert.AreEqual(renamedNamespace.Name, NameTools.GetNamespace(PageDocument.GetPageName(result[0].Document.Name)), "Wrong document name");
			Assert.AreEqual(renamedNamespace.Name, NameTools.GetNamespace(PageDocument.GetPageName(result[1].Document.Name)), "Wrong document name");

			result = prov.PerformSearch(new SearchParameters("test1 test2"));
			Assert.AreEqual(2, result.Count, "Wrong result count");
			Assert.AreEqual(1, result[0].Matches.Count, "Wrong match count");
			Assert.AreEqual(1, result[1].Matches.Count, "Wrong match count");

			string page;
			int id;

			MessageDocument.GetMessageDetails(result[0].Document.Name, out page, out id);
			Assert.AreEqual(renamedNamespace.Name, NameTools.GetNamespace(page), "Wrong document name");

			MessageDocument.GetMessageDetails(result[1].Document.Name, out page, out id);
			Assert.AreEqual(renamedNamespace.Name, NameTools.GetNamespace(page), "Wrong document name");
		}

		[Test]
		public void SetNamespaceDefaultPage() {
			IPagesStorageProviderV30 prov = GetProvider();

			NamespaceInfo ns = prov.AddNamespace("NS");

			PageInfo page = prov.AddPage(ns.Name, "Page", DateTime.Now);

			Assert.IsNull(prov.SetNamespaceDefaultPage(new NamespaceInfo("Inexistent", prov, null),
				new PageInfo(NameTools.GetFullName("Inexistent", "Page"), prov, DateTime.Now)),
				"SetNamespaceDefaultPage should return null when the namespace does not exist");

			Assert.IsNull(prov.SetNamespaceDefaultPage(ns, new PageInfo(NameTools.GetFullName(ns.Name, "Inexistent"), prov, DateTime.Now)),
				"SetNamespaceDefaultPage should return null when the page does not exist");
			
			NamespaceInfo result = prov.SetNamespaceDefaultPage(ns, page);

			AssertNamespaceInfosAreEqual(new NamespaceInfo(ns.Name, prov, page), result, true);

			result = prov.SetNamespaceDefaultPage(ns, null);

			AssertNamespaceInfosAreEqual(new NamespaceInfo(ns.Name, prov, null), result, true);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void SetNamespaceDefaultPage_NullNamespace() {
			IPagesStorageProviderV30 prov = GetProvider();

			PageInfo page = prov.AddPage(null, "Page", DateTime.Now);

			prov.SetNamespaceDefaultPage(null, page);
		}

		[Test]
		public void RemoveNamespace() {
			IPagesStorageProviderV30 prov = GetProvider();

			Assert.IsFalse(prov.RemoveNamespace(new NamespaceInfo("Inexistent", prov, null)), "RemoveNamespace should return alse");

			prov.AddNamespace("Sub");
			prov.AddNamespace("Sub2");

			Assert.IsTrue(prov.RemoveNamespace(new NamespaceInfo("Sub2", prov, null)), "RemoveNamespace should return true");

			NamespaceInfo[] allNS = prov.GetNamespaces();
			Assert.AreEqual(1, allNS.Length, "Wrong namespace count");

			AssertNamespaceInfosAreEqual(new NamespaceInfo("Sub", prov, null), allNS[0], true);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RemoveNamespace_NullNamespace() {
			IPagesStorageProviderV30 prov = GetProvider();
			prov.RemoveNamespace(null);
		}

		[Test]
		public void RemoveNamespace_PerformSearch() {
			IPagesStorageProviderV30 prov = GetProvider();

			NamespaceInfo ns = prov.AddNamespace("NS");

			PageInfo page1 = prov.AddPage(ns.Name, "Page1", DateTime.Now);
			prov.ModifyPage(page1, "Title1", "NUnit", DateTime.Now, "Comment1", "Content1", new string[0], "Descr1", SaveMode.Normal);

			prov.AddMessage(page1, "NUnit", "Test1", DateTime.Now, "Body1", -1);
			prov.AddMessage(page1, "NUnit", "Test2", DateTime.Now, "Body2", prov.GetMessages(page1)[0].ID);

			PageInfo page2 = prov.AddPage(ns.Name, "Page2", DateTime.Now);
			prov.ModifyPage(page2, "Title2", "NUnit", DateTime.Now, "Comment2", "Content2", new string[0], "Descr2", SaveMode.Normal);

			prov.RemoveNamespace(ns);

			Assert.AreEqual(0, prov.PerformSearch(new SearchParameters("content1 content2")).Count, "Wrong result count");

			Assert.AreEqual(0, prov.PerformSearch(new SearchParameters("test1 test2 comment1 comment2")).Count, "Wrong result count");
		}

		[Test]
		public void MovePage_Root2Sub_NoCategories() {
			IPagesStorageProviderV30 prov = GetProvider();

			NamespaceInfo ns = prov.AddNamespace("Namespace");
			CategoryInfo cat1 = prov.AddCategory(null, "Category1");
			CategoryInfo cat2 = prov.AddCategory(null, "Category2");
			CategoryInfo cat3 = prov.AddCategory(ns.Name, "Category3");

			PageInfo page = prov.AddPage(null, "Page", DateTime.Now);
			prov.ModifyPage(page, "Title", "NUnit", DateTime.Now, "Comment", "Content", null, null, SaveMode.Normal);
			prov.ModifyPage(page, "Title0", "NUnit0", DateTime.Now, "Comment0", "Content0", null, null, SaveMode.Backup);
			prov.ModifyPage(page, "Title1", "NUnit1", DateTime.Now, "Comment1", "Content1", null, null, SaveMode.Backup);
			prov.RebindPage(page, new string[] { cat1.FullName });
			prov.AddMessage(page, "NUnit", "Test", DateTime.Now, "Body", -1);

			PageInfo moved = prov.MovePage(page, ns, false);

			PageInfo expected = new PageInfo(NameTools.GetFullName(ns.Name, NameTools.GetLocalName(page.FullName)), prov, page.CreationDateTime);

			AssertPageInfosAreEqual(expected, moved, true);

			Assert.AreEqual(0, prov.GetPages(null).Length, "Wrong page count");

			PageInfo[] allPages = prov.GetPages(ns);
			Assert.AreEqual(1, allPages.Length, "Wrong page count");
			AssertPageInfosAreEqual(expected, allPages[0], true);
			Assert.AreEqual(1, prov.GetMessages(expected).Length, "Wrong message count");

			Assert.AreEqual(2, prov.GetCategories(null).Length, "Wrong category count");
			Assert.AreEqual(1, prov.GetCategories(ns).Length, "Wrong category count");

			Assert.AreEqual(2, prov.GetBackups(expected).Length, "Wrong backup count");
			Assert.AreEqual("Content1", prov.GetContent(expected).Content, "Wrong content");
		}

		[Test]
		public void MovePage_Root2Sub_WithCategories() {
			IPagesStorageProviderV30 prov = GetProvider();

			NamespaceInfo ns = prov.AddNamespace("Namespace");
			CategoryInfo cat1 = prov.AddCategory(null, "Category1");
			CategoryInfo cat2 = prov.AddCategory(null, "Category2");
			CategoryInfo cat1ns = prov.AddCategory(ns.Name, "Category1");
			CategoryInfo cat3 = prov.AddCategory(ns.Name, "Category3");

			PageInfo page = prov.AddPage(null, "Page", DateTime.Now);
			prov.ModifyPage(page, "Title", "NUnit", DateTime.Now, "Comment", "Content", null, null, SaveMode.Normal);
			prov.ModifyPage(page, "Title0", "NUnit0", DateTime.Now, "Comment0", "Content0", null, null, SaveMode.Backup);
			prov.ModifyPage(page, "Title1", "NUnit1", DateTime.Now, "Comment1", "Content1", null, null, SaveMode.Backup);
			prov.RebindPage(page, new string[] { cat1.FullName });
			prov.AddMessage(page, "NUnit", "Test", DateTime.Now, "Body", -1);

			PageInfo moved = prov.MovePage(page, ns, true);

			PageInfo expected = new PageInfo(NameTools.GetFullName(ns.Name, NameTools.GetLocalName(page.FullName)), prov, page.CreationDateTime);

			AssertPageInfosAreEqual(expected, moved, true);

			Assert.AreEqual(0, prov.GetPages(null).Length, "Wrong page count");

			PageInfo[] allPages = prov.GetPages(ns);
			Assert.AreEqual(1, allPages.Length, "Wrong page count");
			AssertPageInfosAreEqual(expected, allPages[0], true);
			Assert.AreEqual(1, prov.GetMessages(expected).Length, "Wrong message count");

			Assert.AreEqual(2, prov.GetCategories(null).Length, "Wrong category count");
			Assert.AreEqual(2, prov.GetCategories(ns).Length, "Wrong category count");

			CategoryInfo[] expectedCategories = new CategoryInfo[] {
				new CategoryInfo(NameTools.GetFullName(ns.Name, NameTools.GetLocalName(cat1.FullName)), prov),
				new CategoryInfo(NameTools.GetFullName(ns.Name, NameTools.GetLocalName(cat3.FullName)), prov) };
			expectedCategories[0].Pages = new string[] { NameTools.GetFullName(ns.Name, "Page") };

			CategoryInfo[] actualCategories = prov.GetCategories(ns);
			Assert.AreEqual(expectedCategories.Length, actualCategories.Length, "Wrong category count");
			Array.Sort(actualCategories, delegate(CategoryInfo x, CategoryInfo y) { return x.FullName.CompareTo(y.FullName); });
			AssertCategoryInfosAreEqual(expectedCategories[0], actualCategories[0], true);
			AssertCategoryInfosAreEqual(expectedCategories[1], actualCategories[1], true);

			Assert.AreEqual(2, prov.GetBackups(expected).Length, "Wrong backup count");
			Assert.AreEqual("Content1", prov.GetContent(expected).Content, "Wrong content");
		}

		[Test]
		public void MovePage_Sub2Root_NoCategories() {
			IPagesStorageProviderV30 prov = GetProvider();

			NamespaceInfo ns = prov.AddNamespace("Namespace");
			CategoryInfo cat1 = prov.AddCategory(null, "Category1");
			CategoryInfo cat2 = prov.AddCategory(ns.Name, "Category2");
			CategoryInfo cat3 = prov.AddCategory(ns.Name, "Category3");

			PageInfo page = prov.AddPage(ns.Name, "Page", DateTime.Now);
			prov.ModifyPage(page, "Title", "NUnit", DateTime.Now, "Comment", "Content", null, null, SaveMode.Normal);
			prov.ModifyPage(page, "Title0", "NUnit0", DateTime.Now, "Comment0", "Content0", null, null, SaveMode.Backup);
			prov.ModifyPage(page, "Title1", "NUnit1", DateTime.Now, "Comment1", "Content1", null, null, SaveMode.Backup);
			prov.RebindPage(page, new string[] { cat2.FullName });
			prov.AddMessage(page, "NUnit", "Test", DateTime.Now, "Body", -1);

			PageInfo moved = prov.MovePage(page, null, false);

			PageInfo expected = new PageInfo(NameTools.GetLocalName(page.FullName), prov, page.CreationDateTime);

			AssertPageInfosAreEqual(expected, moved, true);

			Assert.AreEqual(0, prov.GetPages(ns).Length, "Wrong page count");

			PageInfo[] allPages = prov.GetPages(null);
			Assert.AreEqual(1, allPages.Length, "Wrong page count");
			AssertPageInfosAreEqual(expected, allPages[0], true);
			Assert.AreEqual(1, prov.GetMessages(expected).Length, "Wrong message count");

			Assert.AreEqual(2, prov.GetCategories(ns).Length, "Wrong category count");
			Assert.AreEqual(1, prov.GetCategories(null).Length, "Wrong category count");

			Assert.AreEqual(2, prov.GetBackups(expected).Length, "Wrong backup count");
			Assert.AreEqual("Content1", prov.GetContent(expected).Content, "Wrong content");
		}

		[Test]
		public void MovePage_Sub2Root_WithCategories() {
			IPagesStorageProviderV30 prov = GetProvider();

			NamespaceInfo ns = prov.AddNamespace("Namespace");
			CategoryInfo cat1 = prov.AddCategory(null, "Category1");
			CategoryInfo cat2 = prov.AddCategory(null, "Category2");
			CategoryInfo cat2ns = prov.AddCategory(ns.Name, "Category2");
			CategoryInfo cat3 = prov.AddCategory(ns.Name, "Category3");

			PageInfo page = prov.AddPage(ns.Name, "Page", DateTime.Now);
			prov.ModifyPage(page, "Title", "NUnit", DateTime.Now, "Comment", "Content", null, null, SaveMode.Normal);
			prov.ModifyPage(page, "Title0", "NUnit0", DateTime.Now, "Comment0", "Content0", null, null, SaveMode.Backup);
			prov.ModifyPage(page, "Title1", "NUnit1", DateTime.Now, "Comment1", "Content1", null, null, SaveMode.Backup);
			prov.RebindPage(page, new string[] { cat2ns.FullName });
			prov.AddMessage(page, "NUnit", "Test", DateTime.Now, "Body", -1);

			PageInfo moved = prov.MovePage(page, null, true);

			PageInfo expected = new PageInfo(NameTools.GetLocalName(page.FullName), prov, page.CreationDateTime);

			AssertPageInfosAreEqual(expected, moved, true);

			Assert.AreEqual(0, prov.GetPages(ns).Length, "Wrong page count");

			PageInfo[] allPages = prov.GetPages(null);
			Assert.AreEqual(1, allPages.Length, "Wrong page count");
			AssertPageInfosAreEqual(expected, allPages[0], true);
			Assert.AreEqual(1, prov.GetMessages(expected).Length, "Wrong message count");

			Assert.AreEqual(2, prov.GetCategories(null).Length, "Wrong category count");
			Assert.AreEqual(2, prov.GetCategories(ns).Length, "Wrong category count");

			CategoryInfo[] expectedCategories = new CategoryInfo[] {
				new CategoryInfo(NameTools.GetLocalName(cat1.FullName), prov),
				new CategoryInfo(NameTools.GetLocalName(cat2.FullName), prov) };
			expectedCategories[1].Pages = new string[] { "Page" };

			CategoryInfo[] actualCategories = prov.GetCategories(null);
			Assert.AreEqual(expectedCategories.Length, actualCategories.Length, "Wrong category count");
			Array.Sort(actualCategories, delegate(CategoryInfo x, CategoryInfo y) { return x.FullName.CompareTo(y.FullName); });
			AssertCategoryInfosAreEqual(expectedCategories[0], actualCategories[0], true);
			AssertCategoryInfosAreEqual(expectedCategories[1], actualCategories[1], true);

			Assert.AreEqual(2, prov.GetBackups(expected).Length, "Wrong backup count");
			Assert.AreEqual("Content1", prov.GetContent(expected).Content, "Wrong content");
		}

		[Test]
		public void MovePage_Sub2Sub_NoCategories() {
			IPagesStorageProviderV30 prov = GetProvider();

			NamespaceInfo ns1 = prov.AddNamespace("Namespace1");
			NamespaceInfo ns2 = prov.AddNamespace("Namespace2");
			CategoryInfo cat1 = prov.AddCategory(ns1.Name, "Category1");
			CategoryInfo cat2 = prov.AddCategory(ns1.Name, "Category2");
			CategoryInfo cat3 = prov.AddCategory(ns2.Name, "Category3");

			PageInfo page = prov.AddPage(ns1.Name, "Page", DateTime.Now);
			prov.ModifyPage(page, "Title", "NUnit", DateTime.Now, "Comment", "Content", null, null, SaveMode.Normal);
			prov.ModifyPage(page, "Title0", "NUnit0", DateTime.Now, "Comment0", "Content0", null, null, SaveMode.Backup);
			prov.ModifyPage(page, "Title1", "NUnit1", DateTime.Now, "Comment1", "Content1", null, null, SaveMode.Backup);
			prov.RebindPage(page, new string[] { cat2.FullName });
			prov.AddMessage(page, "NUnit", "Test", DateTime.Now, "Body", -1);

			PageInfo moved = prov.MovePage(page, ns2, false);

			PageInfo expected = new PageInfo(NameTools.GetFullName(ns2.Name, NameTools.GetLocalName(page.FullName)), prov, page.CreationDateTime);

			AssertPageInfosAreEqual(expected, moved, true);

			Assert.AreEqual(0, prov.GetPages(ns1).Length, "Wrong page count");

			PageInfo[] allPages = prov.GetPages(ns2);
			Assert.AreEqual(1, allPages.Length, "Wrong page count");
			AssertPageInfosAreEqual(expected, allPages[0], true);
			Assert.AreEqual(1, prov.GetMessages(expected).Length, "Wrong message count");

			Assert.AreEqual(2, prov.GetCategories(ns1).Length, "Wrong category count");
			Assert.AreEqual(1, prov.GetCategories(ns2).Length, "Wrong category count");

			Assert.AreEqual(2, prov.GetBackups(expected).Length, "Wrong backup count");
			Assert.AreEqual("Content1", prov.GetContent(expected).Content, "Wrong content");
		}

		[Test]
		public void MovePage_Sub2Sub_WithCategories() {
			IPagesStorageProviderV30 prov = GetProvider();

			NamespaceInfo ns1 = prov.AddNamespace("Namespace1");
			NamespaceInfo ns2 = prov.AddNamespace("Namespace2");
			CategoryInfo cat1 = prov.AddCategory(ns1.Name, "Category1");
			CategoryInfo cat2 = prov.AddCategory(ns1.Name, "Category2");
			CategoryInfo cat2ns2 = prov.AddCategory(ns2.Name, "Category2");
			CategoryInfo cat3 = prov.AddCategory(ns2.Name, "Category3");

			PageInfo page = prov.AddPage(ns1.Name, "Page", DateTime.Now);
			prov.ModifyPage(page, "Title", "NUnit", DateTime.Now, "Comment", "Content", null, null, SaveMode.Normal);
			prov.ModifyPage(page, "Title0", "NUnit0", DateTime.Now, "Comment0", "Content0", null, null, SaveMode.Backup);
			prov.ModifyPage(page, "Title1", "NUnit1", DateTime.Now, "Comment1", "Content1", null, null, SaveMode.Backup);
			prov.RebindPage(page, new string[] { cat2.FullName });
			prov.AddMessage(page, "NUnit", "Test", DateTime.Now, "Body", -1);

			PageInfo moved = prov.MovePage(page, ns2, true);

			PageInfo expected = new PageInfo(NameTools.GetFullName(ns2.Name, NameTools.GetLocalName(page.FullName)), prov, page.CreationDateTime);

			AssertPageInfosAreEqual(expected, moved, true);

			Assert.AreEqual(0, prov.GetPages(ns1).Length, "Wrong page count");

			PageInfo[] allPages = prov.GetPages(ns2);
			Assert.AreEqual(1, allPages.Length, "Wrong page count");
			AssertPageInfosAreEqual(expected, allPages[0], true);
			Assert.AreEqual(1, prov.GetMessages(expected).Length, "Wrong message count");

			Assert.AreEqual(2, prov.GetCategories(ns2).Length, "Wrong category count");
			Assert.AreEqual(2, prov.GetCategories(ns1).Length, "Wrong category count");

			CategoryInfo[] expectedCategories = new CategoryInfo[] {
				new CategoryInfo(NameTools.GetFullName(ns2.Name, NameTools.GetLocalName(cat2.FullName)), prov),
				new CategoryInfo(NameTools.GetFullName(ns2.Name, NameTools.GetLocalName(cat3.FullName)), prov) };
			expectedCategories[0].Pages = new string[] { moved.FullName };

			CategoryInfo[] actualCategories = prov.GetCategories(ns2);
			Assert.AreEqual(expectedCategories.Length, actualCategories.Length, "Wrong category count");
			Array.Sort(actualCategories, delegate(CategoryInfo x, CategoryInfo y) { return x.FullName.CompareTo(y.FullName); });
			AssertCategoryInfosAreEqual(expectedCategories[0], actualCategories[0], true);
			AssertCategoryInfosAreEqual(expectedCategories[1], actualCategories[1], true);

			Assert.AreEqual(2, prov.GetBackups(expected).Length, "Wrong backup count");
			Assert.AreEqual("Content1", prov.GetContent(expected).Content, "Wrong content");
		}

		[Test]
		public void MovePage_SameNamespace_Root2Root() {
			IPagesStorageProviderV30 prov = GetProvider();

			PageInfo page = prov.AddPage(null, "Page", DateTime.Now);

			Assert.IsNull(prov.MovePage(page, null, false), "MovePage should return null");
		}

		[Test]
		public void MovePage_SameNamespace_Sub2Sub() {
			IPagesStorageProviderV30 prov = GetProvider();

			NamespaceInfo ns = prov.AddNamespace("Namespace");

			PageInfo page = prov.AddPage("Namespace", "Page", DateTime.Now);

			Assert.IsNull(prov.MovePage(page, ns, false), "MovePage should return null");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void MovePage_NullPage() {
			IPagesStorageProviderV30 prov = GetProvider();

			prov.MovePage(null, prov.AddNamespace("ns"), false);
		}

		[Test]
		public void MovePage_InexistentPage() {
			IPagesStorageProviderV30 prov = GetProvider();

			NamespaceInfo ns = prov.AddNamespace("Namespace");

			Assert.IsNull(prov.MovePage(new PageInfo("Page", prov, DateTime.Now),
				ns, false), "MovePage should return null");

			Assert.IsNull(prov.MovePage(new PageInfo(NameTools.GetFullName(ns.Name, "Page"), prov, DateTime.Now),
				null, false), "MovePage should return null");
		}

		[Test]
		public void MovePage_InexistentNamespace() {
			IPagesStorageProviderV30 prov = GetProvider();

			NamespaceInfo ns = prov.AddNamespace("Namespace");
			PageInfo page = prov.AddPage(ns.Name, "Page", DateTime.Now);

			Assert.IsNull(prov.MovePage(page, new NamespaceInfo("Inexistent", prov, null), false), "MovePage should return null");
		}

		[Test]
		public void MovePage_ExistentPage_Root2Sub() {
			IPagesStorageProviderV30 prov = GetProvider();

			NamespaceInfo ns = prov.AddNamespace("Namespace");

			PageInfo page = prov.AddPage(null, "Page", DateTime.Now);
			PageInfo existing = prov.AddPage(ns.Name, "Page", DateTime.Now);

			Assert.IsNull(prov.MovePage(page, ns, false), "MovePage should return null");
		}

		[Test]
		public void MovePage_ExistentPage_Sub2Root() {
			IPagesStorageProviderV30 prov = GetProvider();

			NamespaceInfo ns = prov.AddNamespace("Namespace");

			PageInfo page = prov.AddPage(ns.Name, "Page", DateTime.Now);
			PageInfo existing = prov.AddPage(null, "Page", DateTime.Now);

			Assert.IsNull(prov.MovePage(page, null, false), "MovePage should return null");
		}

		[Test]
		public void MovePage_DefaultPage() {
			IPagesStorageProviderV30 prov = GetProvider();

			NamespaceInfo ns = prov.AddNamespace("NS");

			PageInfo page = prov.AddPage("NS", "MainPage", DateTime.Now);

			prov.SetNamespaceDefaultPage(ns, page);

			Assert.IsNull(prov.MovePage(page, null, false), "Cannot move the default page");
		}

		[Test]
		public void MovePage_Root2Sub_PerformSearch() {
			IPagesStorageProviderV30 prov = GetProvider();

			NamespaceInfo ns = prov.AddNamespace("NS");

			PageInfo page = prov.AddPage(null, "Page", DateTime.Now);
			prov.ModifyPage(page, "Title", "NUnit", DateTime.Now, "Comment", "Content", new string[0], "Descr", SaveMode.Backup);

			prov.AddMessage(page, "NUnit", "Test1", DateTime.Now, "Body1", -1);
			prov.AddMessage(page, "NUnit", "Test2", DateTime.Now, "Body2", prov.GetMessages(page)[0].ID);

			PageInfo movedPage = prov.MovePage(page, ns, false);

			SearchResultCollection result = prov.PerformSearch(new SearchParameters("content"));
			Assert.AreEqual(1, result.Count, "Wrong result count");
			Assert.AreEqual(movedPage.FullName, PageDocument.GetPageName(result[0].Document.Name), "Wrong document name");

			result = prov.PerformSearch(new SearchParameters("test1 test2 body1 body2"));
			Assert.AreEqual(2, result.Count, "Wrong result count");

			string pageName;
			int id;

			MessageDocument.GetMessageDetails(result[0].Document.Name, out pageName, out id);
			Assert.AreEqual(ns.Name, NameTools.GetNamespace(pageName), "Wrong document name");

			MessageDocument.GetMessageDetails(result[1].Document.Name, out pageName, out id);
			Assert.AreEqual(ns.Name, NameTools.GetNamespace(pageName), "Wrong document name");
		}

		[Test]
		public void MovePage_Sub2Root_PerformSearch() {
			IPagesStorageProviderV30 prov = GetProvider();

			NamespaceInfo ns = prov.AddNamespace("NS");

			PageInfo page = prov.AddPage(ns.Name, "Page", DateTime.Now);
			prov.ModifyPage(page, "Title", "NUnit", DateTime.Now, "Comment", "Content", new string[0], "Descr", SaveMode.Backup);

			prov.AddMessage(page, "NUnit", "Test1", DateTime.Now, "Body1", -1);
			prov.AddMessage(page, "NUnit", "Test2", DateTime.Now, "Body2", prov.GetMessages(page)[0].ID);

			PageInfo movedPage = prov.MovePage(page, null, false);

			SearchResultCollection result = prov.PerformSearch(new SearchParameters("content"));
			Assert.AreEqual(1, result.Count, "Wrong result count");
			Assert.AreEqual(movedPage.FullName, PageDocument.GetPageName(result[0].Document.Name), "Wrong document name");

			result = prov.PerformSearch(new SearchParameters("test1 test2 body1 body2"));
			Assert.AreEqual(2, result.Count, "Wrong result count");

			string pageName;
			int id;

			MessageDocument.GetMessageDetails(result[0].Document.Name, out pageName, out id);
			Assert.AreEqual(null, NameTools.GetNamespace(pageName), "Wrong document name");

			MessageDocument.GetMessageDetails(result[1].Document.Name, out pageName, out id);
			Assert.AreEqual(null, NameTools.GetNamespace(pageName), "Wrong document name");
		}

		[Test]
		public void MovePage_Sub2Sub_PerformSearch() {
			IPagesStorageProviderV30 prov = GetProvider();

			NamespaceInfo ns1 = prov.AddNamespace("NS1");
			NamespaceInfo ns2 = prov.AddNamespace("NS2");

			PageInfo page = prov.AddPage(ns1.Name, "Page", DateTime.Now);
			prov.ModifyPage(page, "Title", "NUnit", DateTime.Now, "Comment", "Content", new string[0], "Descr", SaveMode.Backup);

			prov.AddMessage(page, "NUnit", "Test1", DateTime.Now, "Body1", -1);
			prov.AddMessage(page, "NUnit", "Test2", DateTime.Now, "Body2", prov.GetMessages(page)[0].ID);

			PageInfo movedPage = prov.MovePage(page, ns2, false);

			SearchResultCollection result = prov.PerformSearch(new SearchParameters("content"));
			Assert.AreEqual(1, result.Count, "Wrong result count");
			Assert.AreEqual(movedPage.FullName, PageDocument.GetPageName(result[0].Document.Name), "Wrong document name");

			result = prov.PerformSearch(new SearchParameters("test1 test2 body1 body2"));
			Assert.AreEqual(2, result.Count, "Wrong result count");

			string pageName;
			int id;

			MessageDocument.GetMessageDetails(result[0].Document.Name, out pageName, out id);
			Assert.AreEqual(ns2.Name, NameTools.GetNamespace(pageName), "Wrong document name");

			MessageDocument.GetMessageDetails(result[1].Document.Name, out pageName, out id);
			Assert.AreEqual(ns2.Name, NameTools.GetNamespace(pageName), "Wrong document name");
		}

		private void AssertCategoryInfosAreEqual(CategoryInfo expected, CategoryInfo actual, bool checkProvider) {
			Assert.AreEqual(expected.FullName, actual.FullName, "Wrong full name");
			Assert.AreEqual(expected.Pages.Length, actual.Pages.Length, "Wrong page count");
			for(int i = 0; i < expected.Pages.Length; i++) {
				Assert.AreEqual(expected.Pages[i], actual.Pages[i], "Wrong page at position " + i.ToString());
			}
			if(checkProvider) Assert.AreSame(expected.Provider, actual.Provider, "Different provider instances");
		}

		[Test]
		public void AddCategory_GetCategories_Root() {
			IPagesStorageProviderV30 prov = GetProvider();

			Assert.AreEqual(0, prov.GetCategories(null).Length, "Wrong initial category count");

			CategoryInfo c1 = prov.AddCategory(null, "Category1");
			CategoryInfo c2 = prov.AddCategory(null, "Category2");

			Assert.IsNull(prov.AddCategory(null, "Category1"), "AddCategory should return null");

			AssertCategoryInfosAreEqual(new CategoryInfo("Category1", prov), c1, true);
			AssertCategoryInfosAreEqual(new CategoryInfo("Category2", prov), c2, true);

			CategoryInfo[] categories = prov.GetCategories(null);
			Array.Sort(categories, delegate(CategoryInfo x, CategoryInfo y) { return x.FullName.CompareTo(y.FullName); });

			AssertCategoryInfosAreEqual(new CategoryInfo("Category1", prov), categories[0], true);
			AssertCategoryInfosAreEqual(new CategoryInfo("Category2", prov), categories[1], true);
		}

		[Test]
		public void AddCategory_GetCategories_Sub() {
			IPagesStorageProviderV30 prov = GetProvider();

			NamespaceInfo ns = prov.AddNamespace("Namespace");

			Assert.AreEqual(0, prov.GetCategories(ns).Length, "Wrong initial category count");

			CategoryInfo c1 = prov.AddCategory(ns.Name, "Category1");
			CategoryInfo c2 = prov.AddCategory(ns.Name, "Category2");

			Assert.IsNull(prov.AddCategory(ns.Name, "Category1"), "AddCategory should return null");

			AssertCategoryInfosAreEqual(new CategoryInfo(NameTools.GetFullName(ns.Name, "Category1"), prov), c1, true);
			AssertCategoryInfosAreEqual(new CategoryInfo(NameTools.GetFullName(ns.Name, "Category2"), prov), c2, true);

			CategoryInfo[] categories = prov.GetCategories(ns);
			Array.Sort(categories, delegate(CategoryInfo x, CategoryInfo y) { return x.FullName.CompareTo(y.FullName); });

			AssertCategoryInfosAreEqual(new CategoryInfo(NameTools.GetFullName(ns.Name, "Category1"), prov), categories[0], true);
			AssertCategoryInfosAreEqual(new CategoryInfo(NameTools.GetFullName(ns.Name, "Category2"), prov), categories[1], true);
		}

		[Test]
		public void GetCategoriesForPage_Root() {
			IPagesStorageProviderV30 prov = GetProvider();

			CategoryInfo c1 = prov.AddCategory(null, "Category1");
			CategoryInfo c2 = prov.AddCategory(null, "Category2");
			CategoryInfo c3 = prov.AddCategory(null, "Category3");

			PageInfo page = prov.AddPage(null, "Page", DateTime.Now);
			PageInfo page2 = prov.AddPage(null, "Page2", DateTime.Now);

			prov.RebindPage(page, new string[] { c1.FullName, c3.FullName });

			Assert.AreEqual(0, prov.GetCategoriesForPage(page2).Length, "Wrong category count");
			CategoryInfo[] categories = prov.GetCategoriesForPage(page);
			Assert.AreEqual(2, categories.Length, "Wrong category count");

			Array.Sort(categories, delegate(CategoryInfo x, CategoryInfo y) { return x.FullName.CompareTo(y.FullName); });
			CategoryInfo cat1 = new CategoryInfo("Category1", prov);
			CategoryInfo cat3 = new CategoryInfo("Category3", prov);
			cat1.Pages = new string[] { page.FullName };
			cat3.Pages = new string[] { page.FullName };
			AssertCategoryInfosAreEqual(cat1, categories[0], true);
			AssertCategoryInfosAreEqual(cat3, categories[1], true);
		}

		[Test]
		public void GetCategoriesForPage_Sub() {
			IPagesStorageProviderV30 prov = GetProvider();

			NamespaceInfo ns = prov.AddNamespace("Namespace");

			CategoryInfo c1 = prov.AddCategory(ns.Name, "Category1");
			CategoryInfo c2 = prov.AddCategory(ns.Name, "Category2");
			CategoryInfo c3 = prov.AddCategory(ns.Name, "Category3");

			PageInfo page = prov.AddPage(ns.Name, "Page", DateTime.Now);
			PageInfo page2 = prov.AddPage(ns.Name, "Page2", DateTime.Now);

			prov.RebindPage(page, new string[] { c1.FullName, c3.FullName });

			Assert.AreEqual(0, prov.GetCategoriesForPage(page2).Length, "Wrong category count");
			CategoryInfo[] categories = prov.GetCategoriesForPage(page);
			Assert.AreEqual(2, categories.Length, "Wrong category count");

			Array.Sort(categories, delegate(CategoryInfo x, CategoryInfo y) { return x.FullName.CompareTo(y.FullName); });
			CategoryInfo cat1 = new CategoryInfo(NameTools.GetFullName(ns.Name, "Category1"), prov);
			CategoryInfo cat3 = new CategoryInfo(NameTools.GetFullName(ns.Name, "Category3"), prov);
			cat1.Pages = new string[] { page.FullName };
			cat3.Pages = new string[] { page.FullName };
			AssertCategoryInfosAreEqual(cat1, categories[0], true);
			AssertCategoryInfosAreEqual(cat3, categories[1], true);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void GetCategoriesForPage_NullPage() {
			IPagesStorageProviderV30 prov = GetProvider();

			prov.GetCategoriesForPage(null);
		}

		[Test]
		public void GetUncategorizedPages_Root() {
			IPagesStorageProviderV30 prov = GetProvider();

			CategoryInfo c1 = prov.AddCategory(null, "Category1");
			CategoryInfo c2 = prov.AddCategory(null, "Category2");
			CategoryInfo c3 = prov.AddCategory(null, "Category3");

			PageInfo page = prov.AddPage(null, "Page", DateTime.Now);
			PageInfo page2 = prov.AddPage(null, "Page2", DateTime.Now);
			PageInfo page3 = prov.AddPage(null, "Page3", DateTime.Now);

			prov.RebindPage(page, new string[] { c1.FullName, c3.FullName });

			PageInfo[] pages = prov.GetUncategorizedPages(null);
			Assert.AreEqual(2, pages.Length, "Wrong page count");

			AssertPageInfosAreEqual(page2, pages[0], true);
			AssertPageInfosAreEqual(page3, pages[1], true);
		}

		[Test]
		public void GetUncategorizedPages_Sub() {
			IPagesStorageProviderV30 prov = GetProvider();

			NamespaceInfo ns = prov.AddNamespace("Namespace");

			CategoryInfo c1 = prov.AddCategory(ns.Name, "Category1");
			CategoryInfo c2 = prov.AddCategory(ns.Name, "Category2");
			CategoryInfo c3 = prov.AddCategory(ns.Name, "Category3");

			PageInfo page = prov.AddPage(ns.Name, "Page", DateTime.Now);
			PageInfo page2 = prov.AddPage(ns.Name, "Page2", DateTime.Now);
			PageInfo page3 = prov.AddPage(ns.Name, "Page3", DateTime.Now);

			prov.RebindPage(page, new string[] { c1.FullName, c3.FullName });

			PageInfo[] pages = prov.GetUncategorizedPages(ns);
			Assert.AreEqual(2, pages.Length, "Wrong page count");

			AssertPageInfosAreEqual(page2, pages[0], true);
			AssertPageInfosAreEqual(page3, pages[1], true);
		}

		[Test]
		public void AddCategory_GetCategory_Root() {
			IPagesStorageProviderV30 prov = GetProvider();

			Assert.IsNull(prov.GetCategory("Category1"), "GetCategory should return null");

			CategoryInfo c1 = prov.AddCategory(null, "Category1");
			CategoryInfo c2 = prov.AddCategory(null, "Category2");

			Assert.IsNull(prov.GetCategory("Category3"), "GetCategory should return null");

			CategoryInfo c1Out = prov.GetCategory("Category1");
			CategoryInfo c2Out = prov.GetCategory("Category2");

			AssertCategoryInfosAreEqual(c1, c1Out, true);
			AssertCategoryInfosAreEqual(c2, c2Out, true);
		}

		[Test]
		public void AddCategory_GetCategory_Sub() {
			IPagesStorageProviderV30 prov = GetProvider();

			prov.AddNamespace("NS");

			Assert.IsNull(prov.GetCategory("NS.Category1"), "GetCategory should return null");

			CategoryInfo c1 = prov.AddCategory("NS", "Category1");
			CategoryInfo c2 = prov.AddCategory("NS", "Category2");

			Assert.IsNull(prov.GetCategory("NS.Category3"), "GetCategory should return null");

			CategoryInfo c1Out = prov.GetCategory("NS.Category1");
			CategoryInfo c2Out = prov.GetCategory("NS.Category2");

			AssertCategoryInfosAreEqual(c1, c1Out, true);
			AssertCategoryInfosAreEqual(c2, c2Out, true);
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void GetCategory_InvalidName(string n) {
			IPagesStorageProviderV30 prov = GetProvider();
			prov.GetCategory(n);
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void AddCategory_InvalidCategory(string c) {
			IPagesStorageProviderV30 prov = GetProvider();
			prov.AddCategory(null, c);
		}

		[Test]
		public void RenameCategory_Root() {
			IPagesStorageProviderV30 prov = GetProvider();

			CategoryInfo c1 = prov.AddCategory(null, "Category1");
			CategoryInfo c2 = prov.AddCategory(null, "Category2");

			PageInfo page = prov.AddPage(null, "Page", DateTime.Now);
			prov.RebindPage(page, new string[] { c2.FullName });

			Assert.IsNull(prov.RenameCategory(new CategoryInfo("Inexistent", prov), "NewName"), "RenameCategory should return null");
			Assert.IsNull(prov.RenameCategory(c2, "Category1"), "RenameCategory should return null");

			CategoryInfo c3 = new CategoryInfo("Category3", prov);
			c3.Pages = new string[] { page.FullName };
			AssertCategoryInfosAreEqual(c3, prov.RenameCategory(c2, "Category3"), true);

			CategoryInfo[] categories = prov.GetCategories(null);
			Assert.AreEqual(2, categories.Length, "Wrong category count");

			Array.Sort(categories, delegate(CategoryInfo x, CategoryInfo y) { return x.FullName.CompareTo(y.FullName); });
			AssertCategoryInfosAreEqual(new CategoryInfo("Category1", prov), categories[0], true);
			AssertCategoryInfosAreEqual(c3, categories[1], true);
		}

		[Test]
		public void RenameCategory_Sub() {
			IPagesStorageProviderV30 prov = GetProvider();

			NamespaceInfo ns = prov.AddNamespace("Namespace");

			CategoryInfo c1 = prov.AddCategory(ns.Name, "Category1");
			CategoryInfo c2 = prov.AddCategory(ns.Name, "Category2");

			PageInfo page = prov.AddPage(ns.Name, "Page", DateTime.Now);
			prov.RebindPage(page, new string[] { c2.FullName });

			Assert.IsNull(prov.RenameCategory(new CategoryInfo(NameTools.GetFullName(ns.Name, "Inexistent"), prov), "NewName"), "RenameCategory should return null");
			Assert.IsNull(prov.RenameCategory(c2, "Category1"), "RenameCategory should return null");

			CategoryInfo c3 = new CategoryInfo(NameTools.GetFullName(ns.Name, "Category3"), prov);
			c3.Pages = new string[] { page.FullName };
			AssertCategoryInfosAreEqual(c3, prov.RenameCategory(c2, "Category3"), true);

			CategoryInfo[] categories = prov.GetCategories(ns);
			Assert.AreEqual(2, categories.Length, "Wrong category count");

			Array.Sort(categories, delegate(CategoryInfo x, CategoryInfo y) { return x.FullName.CompareTo(y.FullName); });
			AssertCategoryInfosAreEqual(new CategoryInfo(NameTools.GetFullName(ns.Name, "Category1"), prov), categories[0], true);
			AssertCategoryInfosAreEqual(c3, categories[1], true);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RenameCategory_NullCategory() {
			IPagesStorageProviderV30 prov = GetProvider();

			prov.RenameCategory(null, "Name");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void RenameCategory_InvalidNewName(string n) {
			IPagesStorageProviderV30 prov = GetProvider();
			CategoryInfo c1 = prov.AddCategory(null, "Category1");
			prov.RenameCategory(c1, n);
		}

		[Test]
		public void RemoveCategory_Root() {
			IPagesStorageProviderV30 prov = GetProvider();

			CategoryInfo c1 = prov.AddCategory(null, "Category1");
			CategoryInfo c2 = prov.AddCategory(null, "Category2");

			Assert.IsFalse(prov.RemoveCategory(new CategoryInfo("Inexistent", prov)), "RemoveCategory should return false");

			Assert.IsTrue(prov.RemoveCategory(c1), "RemoveCategory should return true");

			CategoryInfo[] categories = prov.GetCategories(null);
			Assert.AreEqual(1, categories.Length, "Wrong category count");
			AssertCategoryInfosAreEqual(new CategoryInfo("Category2", prov), categories[0], true);
		}

		[Test]
		public void RemoveCategory_Sub() {
			IPagesStorageProviderV30 prov = GetProvider();

			NamespaceInfo ns = prov.AddNamespace("Namespace");

			CategoryInfo c1 = prov.AddCategory(ns.Name, "Category1");
			CategoryInfo c2 = prov.AddCategory(ns.Name, "Category2");

			Assert.IsFalse(prov.RemoveCategory(new CategoryInfo(NameTools.GetFullName(ns.Name, "Inexistent"), prov)), "RemoveCategory should return false");

			Assert.IsTrue(prov.RemoveCategory(c1), "RemoveCategory should return true");

			CategoryInfo[] categories = prov.GetCategories(ns);
			Assert.AreEqual(1, categories.Length, "Wrong category count");
			AssertCategoryInfosAreEqual(new CategoryInfo(NameTools.GetFullName(ns.Name, "Category2"), prov), categories[0], true);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RemoveCategory_NullCategory() {
			IPagesStorageProviderV30 prov = GetProvider();

			prov.RemoveCategory(null);
		}

		[Test]
		public void MergeCategories_Root() {
			IPagesStorageProviderV30 prov = GetProvider();

			CategoryInfo cat1 = prov.AddCategory(null, "Cat1");
			CategoryInfo cat2 = prov.AddCategory(null, "Cat2");
			CategoryInfo cat3 = prov.AddCategory(null, "Cat3");

			PageInfo page1 = prov.AddPage(null, "Page1", DateTime.Now);
			PageInfo page2 = prov.AddPage(null, "Page2", DateTime.Now);

			prov.RebindPage(page1, new string[] { "Cat1", "Cat2" });
			prov.RebindPage(page2, new string[] { "Cat3" });

			Assert.IsNull(prov.MergeCategories(new CategoryInfo("Inexistent", prov), cat1), "MergeCategories should return null");
			Assert.IsNull(prov.MergeCategories(cat1, new CategoryInfo("Inexistent", prov)), "MergeCategories should return null");

			CategoryInfo merged = prov.MergeCategories(cat1, cat3);
			Assert.IsNotNull(merged, "MergeCategories should return something");
			Assert.AreEqual("Cat3", merged.FullName, "Wrong name");

			CategoryInfo[] categories = prov.GetCategories(null);

			Assert.AreEqual(2, categories.Length, "Wrong category count");

			Array.Sort(categories, delegate(CategoryInfo x, CategoryInfo y) { return x.FullName.CompareTo(y.FullName); });

			Assert.AreEqual(1, categories[0].Pages.Length, "Wrong page count");
			Assert.AreEqual("Page1", categories[0].Pages[0], "Wrong page at position 0");

			Assert.AreEqual(2, categories[1].Pages.Length, "Wrong page count");
			Array.Sort(categories[1].Pages);
			Assert.AreEqual("Page1", categories[1].Pages[0], "Wrong page at position 0");
			Assert.AreEqual("Page2", categories[1].Pages[1], "Wrong page at position 1");
		}

		[Test]
		public void MergeCategories_Sub() {
			IPagesStorageProviderV30 prov = GetProvider();

			NamespaceInfo ns = prov.AddNamespace("Namespace");

			CategoryInfo cat1 = prov.AddCategory(ns.Name, "Cat1");
			CategoryInfo cat2 = prov.AddCategory(ns.Name, "Cat2");
			CategoryInfo cat3 = prov.AddCategory(ns.Name, "Cat3");

			PageInfo page1 = prov.AddPage(ns.Name, "Page1", DateTime.Now);
			PageInfo page2 = prov.AddPage(ns.Name, "Page2", DateTime.Now);

			prov.RebindPage(page1, new string[] { NameTools.GetFullName(ns.Name, "Cat1"), NameTools.GetFullName(ns.Name, "Cat2") });
			prov.RebindPage(page2, new string[] { NameTools.GetFullName(ns.Name, "Cat3") });

			Assert.IsNull(prov.MergeCategories(new CategoryInfo(NameTools.GetFullName(ns.Name, "Inexistent"), prov), cat1), "MergeCategories should return null");
			Assert.IsNull(prov.MergeCategories(cat1, new CategoryInfo(NameTools.GetFullName(ns.Name, "Inexistent"), prov)), "MergeCategories should return null");

			CategoryInfo merged = prov.MergeCategories(cat1, cat3);
			Assert.IsNotNull(merged, "MergeCategories should return something");
			Assert.AreEqual(NameTools.GetFullName(ns.Name, "Cat3"), merged.FullName, "Wrong name");

			CategoryInfo[] categories = prov.GetCategories(ns);

			Assert.AreEqual(2, categories.Length, "Wrong category count");

			Array.Sort(categories, delegate(CategoryInfo x, CategoryInfo y) { return x.FullName.CompareTo(y.FullName); });

			Assert.AreEqual(1, categories[0].Pages.Length, "Wrong page count");
			Assert.AreEqual(NameTools.GetFullName(ns.Name, "Page1"), categories[0].Pages[0], "Wrong page at position 0");

			Assert.AreEqual(2, categories[1].Pages.Length, "Wrong page count");
			Array.Sort(categories[1].Pages);
			Assert.AreEqual(NameTools.GetFullName(ns.Name, "Page1"), categories[1].Pages[0], "Wrong page at position 0");
			Assert.AreEqual(NameTools.GetFullName(ns.Name, "Page2"), categories[1].Pages[1], "Wrong page at position 1");
		}

		[Test]
		public void MergeCategories_DifferentNamespaces() {
			IPagesStorageProviderV30 prov = GetProvider();

			NamespaceInfo ns1 = prov.AddNamespace("Namespace1");
			NamespaceInfo ns2 = prov.AddNamespace("Namespace2");

			CategoryInfo cat1 = prov.AddCategory(ns1.Name, "Cat1");
			CategoryInfo cat2 = prov.AddCategory(ns2.Name, "Cat2");
			CategoryInfo cat3 = prov.AddCategory(null, "Cat3");

			// Sub 2 Sub
			Assert.IsNull(prov.MergeCategories(cat1, cat2), "MergeCategories should return null");
			// Sub to Root
			Assert.IsNull(prov.MergeCategories(cat2, cat3), "MergeCategories should return null");
			// Root to Sub
			Assert.IsNull(prov.MergeCategories(cat3, cat1), "MergeCategories should return null");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void MergeCategories_NullSource() {
			IPagesStorageProviderV30 prov = GetProvider();

			CategoryInfo cat1 = prov.AddCategory(null, "Cat1");

			prov.MergeCategories(null, cat1);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void MergeCategories_NullDestination() {
			IPagesStorageProviderV30 prov = GetProvider();

			CategoryInfo cat1 = prov.AddCategory(null, "Cat1");

			prov.MergeCategories(cat1, null);
		}

		[Test]
		public void PerformSearch() {
			IPagesStorageProviderV30 prov = GetProvider();

			PageInfo page = prov.AddPage(null, "Page", DateTime.Now);
			prov.ModifyPage(page, "Title", "NUnit", DateTime.Now, "Comment", "Content", null, null, SaveMode.Backup);

			Assert.AreEqual(1, prov.PerformSearch(new SearchParameters("content")).Count, "Wrong result count");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void PerformSearch_NullParameters() {
			IPagesStorageProviderV30 prov = GetProvider();

			prov.PerformSearch(null);
		}

		private void AssertPageInfosAreEqual(PageInfo expected, PageInfo actual, bool checkProvider) {
			Assert.AreEqual(expected.FullName, actual.FullName, "Wrong name");
			Assert.AreEqual(expected.NonCached, actual.NonCached, "Wrong non-cached flag");
			Tools.AssertDateTimesAreEqual(expected.CreationDateTime, actual.CreationDateTime, true);
			if(checkProvider) Assert.AreSame(expected.Provider, actual.Provider, "Different provider instances");
		}

		[Test]
		public void AddPage_GetPages_Root() {
			IPagesStorageProviderV30 prov = GetProvider();

			Assert.AreEqual(0, prov.GetPages(null).Length, "Wrong initial page count");

			PageInfo p1 = prov.AddPage(null, "Page1", DateTime.Now);
			PageInfo p2 = prov.AddPage(null, "Page2", DateTime.Now);

			Assert.IsNull(prov.AddPage(null, "Page1", DateTime.Now.AddDays(-1)), "AddPage should return null");

			AssertPageInfosAreEqual(new PageInfo("Page1", prov, p1.CreationDateTime), p1, true);
			AssertPageInfosAreEqual(new PageInfo("Page2", prov, p2.CreationDateTime), p2, true);

			PageInfo[] pages = prov.GetPages(null);

			Assert.AreEqual(2, pages.Length, "Wrong page count");
			Array.Sort(pages, delegate(PageInfo x, PageInfo y) { return x.FullName.CompareTo(y.FullName); });
			AssertPageInfosAreEqual(new PageInfo("Page1", prov, p1.CreationDateTime), pages[0], true);
			AssertPageInfosAreEqual(new PageInfo("Page2", prov, p2.CreationDateTime), pages[1], true);
		}

		[Test]
		public void AddPage_GetPages_Sub() {
			IPagesStorageProviderV30 prov = GetProvider();

			NamespaceInfo ns = prov.AddNamespace("Namespace");

			Assert.AreEqual(0, prov.GetPages(ns).Length, "Wrong initial page count");

			PageInfo p1 = prov.AddPage(ns.Name, "Page1", DateTime.Now);
			PageInfo p2 = prov.AddPage(ns.Name, "Page2", DateTime.Now);

			Assert.IsNull(prov.AddPage(ns.Name, "Page1", DateTime.Now.AddDays(-1)), "AddPage should return null");

			AssertPageInfosAreEqual(new PageInfo(NameTools.GetFullName(ns.Name, "Page1"), prov, p1.CreationDateTime), p1, true);
			AssertPageInfosAreEqual(new PageInfo(NameTools.GetFullName(ns.Name, "Page2"), prov, p2.CreationDateTime), p2, true);

			PageInfo[] pages = prov.GetPages(ns);

			Assert.AreEqual(2, pages.Length, "Wrong page count");
			Array.Sort(pages, delegate(PageInfo x, PageInfo y) { return x.FullName.CompareTo(y.FullName); });
			AssertPageInfosAreEqual(new PageInfo(NameTools.GetFullName(ns.Name, "Page1"), prov, p1.CreationDateTime), pages[0], true);
			AssertPageInfosAreEqual(new PageInfo(NameTools.GetFullName(ns.Name, "Page2"), prov, p2.CreationDateTime), pages[1], true);
		}

		[Test]
		public void AddPage_GetPage_Root() {
			IPagesStorageProviderV30 prov = GetProvider();

			Assert.IsNull(prov.GetPage("Page1"), "GetPage should return null");

			PageInfo p1 = prov.AddPage(null, "Page1", DateTime.Now);
			PageInfo p2 = prov.AddPage(null, "Page2", DateTime.Now);

			Assert.IsNull(prov.GetPage("Page3"), "GetPage should return null");

			PageInfo p1Out = prov.GetPage("Page1");
			PageInfo p2Out = prov.GetPage("Page2");

			AssertPageInfosAreEqual(p1, p1Out, true);
			AssertPageInfosAreEqual(p2, p2Out, true);
		}

		[Test]
		public void AddPage_GetPage_Sub() {
			IPagesStorageProviderV30 prov = GetProvider();

			prov.AddNamespace("NS");

			Assert.IsNull(prov.GetPage("NS.Page1"), "GetPage should return null");

			PageInfo p1 = prov.AddPage("NS", "Page1", DateTime.Now);
			PageInfo p2 = prov.AddPage("NS", "Page2", DateTime.Now);

			Assert.IsNull(prov.GetPage("NS.Page3"), "GetPage should return null");

			PageInfo p1Out = prov.GetPage("NS.Page1");
			PageInfo p2Out = prov.GetPage("NS.Page2");

			AssertPageInfosAreEqual(p1, p1Out, true);
			AssertPageInfosAreEqual(p2, p2Out, true);
		}

		[Test]
		public void AddPage_GetPage_SubSpaced() {
			IPagesStorageProviderV30 prov = GetProvider();

			prov.AddNamespace("Spaced Namespace");

			Assert.IsNull(prov.GetPage("Spaced Namespace.Page1"), "GetPage should return null");

			PageInfo p1 = prov.AddPage("Spaced Namespace", "Page1", DateTime.Now);
			PageInfo p2 = prov.AddPage("Spaced Namespace", "Page2", DateTime.Now);

			Assert.IsNull(prov.GetPage("Spaced Namespace.Page3"), "GetPage should return null");

			PageInfo p1Out = prov.GetPage("Spaced Namespace.Page1");
			PageInfo p2Out = prov.GetPage("Spaced Namespace.Page2");

			AssertPageInfosAreEqual(p1, p1Out, true);
			AssertPageInfosAreEqual(p2, p2Out, true);
		}


		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void GetPage_InvalidName(string n) {
			IPagesStorageProviderV30 prov = GetProvider();

			prov.GetPage(n);
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void AddPage_InvalidName(string n) {
			IPagesStorageProviderV30 prov = GetProvider();

			prov.AddPage(null, n, DateTime.Now);
		}

		private void AssertPageContentsAreEqual(PageContent expected, PageContent actual) {
			AssertPageInfosAreEqual(expected.PageInfo, actual.PageInfo, true);
			Assert.AreEqual(expected.Title, actual.Title, "Wrong title");
			Assert.AreEqual(expected.User, actual.User, "Wrong user");
			Tools.AssertDateTimesAreEqual(expected.LastModified, actual.LastModified, true);
			Assert.AreEqual(expected.Comment, actual.Comment, "Wrong comment");
			Assert.AreEqual(expected.Content, actual.Content, "Wrong content");

			if(expected.LinkedPages != null) {
				Assert.IsNotNull(actual.LinkedPages, "LinkedPages is null");
				Assert.AreEqual(expected.LinkedPages.Length, actual.LinkedPages.Length, "Wrong linked page count");
				for(int i = 0; i < expected.LinkedPages.Length; i++) {
					Assert.AreEqual(expected.LinkedPages[i], actual.LinkedPages[i], "Wrong linked page");
				}
			}

			if(expected.Keywords != null) {
				Assert.IsNotNull(actual.Keywords);
				Assert.AreEqual(expected.Keywords.Length, actual.Keywords.Length, "Wrong keyword count");
				for(int i = 0; i < expected.Keywords.Length; i++) {
					Assert.AreEqual(expected.Keywords[i], actual.Keywords[i], "Wrong keyword");
				}
			}

			Assert.AreEqual(expected.Description, actual.Description, "Wrong description");
		}

		[Test]
		public void ModifyPage_GetContent_GetBackups_GetBackupContent_Root() {
			IPagesStorageProviderV30 prov = GetProvider();

			PageInfo p = prov.AddPage(null, "Page", DateTime.Now);

			DateTime dt = DateTime.Now;

			Assert.IsFalse(prov.ModifyPage(new PageInfo("Inexistent", prov, DateTime.Now),
				"Title", "NUnit", dt, "Comment", "Content", null, null, SaveMode.Normal), "ModifyPage should return false");

			Assert.IsTrue(prov.ModifyPage(p, "Title", "NUnit", dt, null, "Content",
				new string[] { "keyword1", "keyword2" }, "Description", SaveMode.Normal), "ModifyPage should return true");

			// Test null/inexistent page
			Assert.IsNull(prov.GetContent(null), "GetContent should return null");
			Assert.IsNull(prov.GetContent(new PageInfo("PPP", prov, DateTime.Now)), "GetContent should return null");

			PageContent c = prov.GetContent(p);
			Assert.IsNotNull(c, "GetContent should return something");

			AssertPageContentsAreEqual(new PageContent(p, "Title", "NUnit", dt, "", "Content",
				new string[] { "keyword1", "keyword2" }, "Description"), c);

			Assert.IsTrue(prov.ModifyPage(p, "Title1", "NUnit1", dt.AddDays(1), "Comment1", "Content1", null, null, SaveMode.Backup), "ModifyPage should return true");

			int[] baks = prov.GetBackups(p);
			Assert.AreEqual(1, baks.Length, "Wrong backup content");

			PageContent backup = prov.GetBackupContent(p, baks[0]);
			Assert.IsNotNull(backup, "GetBackupContent should return something");
			AssertPageContentsAreEqual(c, backup);
		}

		[Test]
		public void ModifyPage_GetContent_GetBackups_GetBackupContent_Sub() {
			IPagesStorageProviderV30 prov = GetProvider();

			NamespaceInfo ns = prov.AddNamespace("Namespace");

			PageInfo p = prov.AddPage(ns.Name, "Page", DateTime.Now);

			DateTime dt = DateTime.Now;

			Assert.IsFalse(prov.ModifyPage(new PageInfo(NameTools.GetFullName(ns.Name, "Inexistent"), prov, DateTime.Now),
				"Title", "NUnit", dt, "Comment", "Content", null, null, SaveMode.Normal), "ModifyPage should return false");

			Assert.IsTrue(prov.ModifyPage(p, "Title", "NUnit", dt, null, "Content",
				new string[] { "keyword1", "keyword2" }, "Description", SaveMode.Normal), "ModifyPage should return true");

			// Test null/inexistent page
			Assert.IsNull(prov.GetContent(null), "GetContent should return null");
			Assert.IsNull(prov.GetContent(new PageInfo(NameTools.GetFullName(ns.Name, "PPP"), prov, DateTime.Now)), "GetContent should return null");

			PageContent c = prov.GetContent(p);
			Assert.IsNotNull(c, "GetContent should return something");

			AssertPageContentsAreEqual(new PageContent(p, "Title", "NUnit", dt, "", "Content",
				new string[] { "keyword1", "keyword2" }, "Description"), c);

			Assert.IsTrue(prov.ModifyPage(p, "Title1", "NUnit1", dt.AddDays(1), "Comment1", "Content1", null, null, SaveMode.Backup), "ModifyPage should return true");

			int[] baks = prov.GetBackups(p);
			Assert.AreEqual(1, baks.Length, "Wrong backup content");

			PageContent backup = prov.GetBackupContent(p, baks[0]);
			Assert.IsNotNull(backup, "GetBackupContent should return something");
			AssertPageContentsAreEqual(c, backup);
		}

		[Test]
		public void ModifyPage_PerformSearch() {
			IPagesStorageProviderV30 prov = GetProvider();

			// Added to check that pages inserted in reverse alphabetical order work with the search engine
			PageInfo p0 = prov.AddPage(null, "PagZ", DateTime.Now);
			prov.ModifyPage(p0, "ZZZ", "NUnit", DateTime.Now, "", "", null, "", SaveMode.Normal);

			PageInfo p = prov.AddPage(null, "Page", DateTime.Now);

			DateTime dt = DateTime.Now;

			Assert.IsTrue(prov.ModifyPage(p, "TitleOld", "NUnitOld", dt, "CommentOld", "ContentOld",
				new string[] { "keyword3", "keyword4" }, null, SaveMode.Normal), "ModifyPage should return true");
			Assert.IsTrue(prov.ModifyPage(p, "Title", "NUnit", dt, "Comment", "Content",
				new string[] { "keyword1", "keyword2" }, null, SaveMode.Normal), "ModifyPage should return true");

			SearchResultCollection result = prov.PerformSearch(new SearchParameters("content"));
			Assert.AreEqual(1, result.Count, "Wrong search result count");
			Assert.AreEqual(PageDocument.StandardTypeTag, result[0].Document.TypeTag, "Wrong type tag");
			Assert.AreEqual(1, result[0].Matches.Count, "Wrong match count");
			Assert.AreEqual(WordLocation.Content, result[0].Matches[0].Location, "Wrong word location");

			result = prov.PerformSearch(new SearchParameters("title"));
			Assert.AreEqual(1, result.Count, "Wrong search result count");
			Assert.AreEqual(PageDocument.StandardTypeTag, result[0].Document.TypeTag, "Wrong type tag");
			Assert.AreEqual(1, result[0].Matches.Count, "Wrong match count");
			Assert.AreEqual(WordLocation.Title, result[0].Matches[0].Location, "Wrong word location");

			result = prov.PerformSearch(new SearchParameters("keyword1"));
			Assert.AreEqual(1, result.Count, "Wrong search count");
			Assert.AreEqual(PageDocument.StandardTypeTag, result[0].Document.TypeTag, "Wrong type tag");
			Assert.AreEqual(1, result[0].Matches.Count, "Wrong match count");
			Assert.AreEqual(WordLocation.Keywords, result[0].Matches[0].Location, "Wrong word location");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void ModifyPage_NullPage() {
			IPagesStorageProviderV30 prov = GetProvider();
			prov.ModifyPage(null, "Title", "NUnit", DateTime.Now, "Comment", "Content", null, null, SaveMode.Backup);
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void ModifyPage_InvalidTitle(string t) {
			IPagesStorageProviderV30 prov = GetProvider();
			PageInfo p = prov.AddPage(null, "Page", DateTime.Now);
			prov.ModifyPage(p, t, "NUnit", DateTime.Now, "Comment", "Content", null, null, SaveMode.Backup);
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void ModifyPage_InvalidUsername(string u) {
			IPagesStorageProviderV30 prov = GetProvider();
			PageInfo p = prov.AddPage(null, "Page", DateTime.Now);
			prov.ModifyPage(p, "Title", u, DateTime.Now, "Comment", "Content", null, null, SaveMode.Backup);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void ModifyPage_NullContent() {
			IPagesStorageProviderV30 prov = GetProvider();
			PageInfo p = prov.AddPage(null, "Page", DateTime.Now);
			prov.ModifyPage(p, "Title", "NUnit", DateTime.Now, "", null, null, null, SaveMode.Backup);
		}

		[Test]
		public void ModifyPage_OddCombinationsOfKeywordsAndDescription() {
			IPagesStorageProviderV30 prov = GetProvider();

			PageInfo page = prov.AddPage(null, "Page", DateTime.Now);

			DateTime dt = DateTime.Now;

			Assert.IsTrue(prov.ModifyPage(page, "Title", "NUnit", dt, "", "Content",
				null, null, SaveMode.Normal), "ModifyPage should return true");
			PageContent content = prov.GetContent(page);
			AssertPageContentsAreEqual(new PageContent(page, "Title", "NUnit", dt, "", "Content",
				new string[0], null), content);

			Assert.IsTrue(prov.ModifyPage(page, "Title", "NUnit", dt, "", "Content",
				new string[0], null, SaveMode.Normal), "ModifyPage should return true");
			content = prov.GetContent(page);
			AssertPageContentsAreEqual(new PageContent(page, "Title", "NUnit", dt, "", "Content",
				new string[0], null), content);

			Assert.IsTrue(prov.ModifyPage(page, "Title", "NUnit", dt, "", "Content",
				new string[] { "Blah" }, null, SaveMode.Normal), "ModifyPage should return true");
			content = prov.GetContent(page);
			AssertPageContentsAreEqual(new PageContent(page, "Title", "NUnit", dt, "", "Content",
				new string[] { "Blah" }, null), content);

			Assert.IsTrue(prov.ModifyPage(page, "Title", "NUnit", dt, "", "Content",
				null, "", SaveMode.Normal), "ModifyPage should return true");
			content = prov.GetContent(page);
			AssertPageContentsAreEqual(new PageContent(page, "Title", "NUnit", dt, "", "Content",
				new string[0], null), content);

			Assert.IsTrue(prov.ModifyPage(page, "Title", "NUnit", dt, "", "Content",
				null, "Descr", SaveMode.Normal), "ModifyPage should return true");
			content = prov.GetContent(page);
			AssertPageContentsAreEqual(new PageContent(page, "Title", "NUnit", dt, "", "Content",
				new string[0], "Descr"), content);

			Assert.IsTrue(prov.ModifyPage(page, "Title", "NUnit", dt, "", "Content",
				new string[] { "Blah" }, "Descr", SaveMode.Normal), "ModifyPage should return true");
			content = prov.GetContent(page);
			AssertPageContentsAreEqual(new PageContent(page, "Title", "NUnit", dt, "", "Content",
				new string[] { "Blah" }, "Descr"), content);
		}

		[Test]
		public void ModifyPage_Draft_GetDraft_DeleteDraft() {
			IPagesStorageProviderV30 prov = GetProvider();

			DateTime dt = DateTime.Now;
			PageInfo page = prov.AddPage(null, "Page", DateTime.Now);
			prov.ModifyPage(page, "TitleOld", "NUnitOld", DateTime.Now, "CommentOld", "ContentOld", new string[0], "Blah", SaveMode.Normal);
			Assert.IsTrue(prov.ModifyPage(page, "Title", "NUnit", DateTime.Now, "Comment", "Content", new string[0], "", SaveMode.Draft), "ModifyPage should return true");
			Assert.IsTrue(prov.ModifyPage(page, "Title2", "NUnit", dt, "", "Content2", new string[0], "Descr", SaveMode.Draft), "ModifyPage should return true");

			PageContent content = prov.GetDraft(page);
			AssertPageContentsAreEqual(new PageContent(page, "Title2", "NUnit", dt, "", "Content2", new string[0], "Descr"), content);

			Assert.IsTrue(prov.DeleteDraft(page), "DeleteDraft should return true");

			Assert.IsNull(prov.GetDraft(page), "GetDraft should return null");
		}

		[Test]
		public void ModifyPage_Draft_RemovePage() {
			IPagesStorageProviderV30 prov = GetProvider();

			DateTime dt = DateTime.Now;
			PageInfo page = prov.AddPage(null, "Page", DateTime.Now);
			prov.ModifyPage(page, "Title", "NUnit", dt, "", "", new string[0], "", SaveMode.Normal);
			prov.ModifyPage(page, "Title", "NUnit", dt, "Comment", "Content", new string[0], "", SaveMode.Draft);

			prov.RemovePage(page);

			Assert.IsNull(prov.GetDraft(page), "GetDraft should return null");
		}

		[Test]
		public void ModifyPage_Draft_RenamePage_Root() {
			IPagesStorageProviderV30 prov = GetProvider();

			DateTime dt = DateTime.Now;
			PageInfo page = prov.AddPage(null, "Page", DateTime.Now);
			prov.ModifyPage(page, "Title", "NUnit", dt, "", "", new string[0], "", SaveMode.Normal);
			prov.ModifyPage(page, "Title", "NUnit", dt, "Comment", "Content", new string[0], "", SaveMode.Draft);

			PageInfo newPage = prov.RenamePage(page, "NewName");

			PageContent content = prov.GetDraft(newPage);

			AssertPageContentsAreEqual(new PageContent(newPage, "Title", "NUnit", dt, "Comment", "Content", new string[0], null), content);
		}

		[Test]
		public void ModifyPage_Draft_RenamePage_Sub() {
			IPagesStorageProviderV30 prov = GetProvider();

			DateTime dt = DateTime.Now;
			prov.AddNamespace("NS");
			PageInfo page = prov.AddPage("NS", "Page", DateTime.Now);
			prov.ModifyPage(page, "Title", "NUnit", dt, "", "", new string[0], "", SaveMode.Normal);
			prov.ModifyPage(page, "Title", "NUnit", dt, "Comment", "Content", new string[0], "", SaveMode.Draft);

			PageInfo newPage = prov.RenamePage(page, "NewName");

			PageContent content = prov.GetDraft(newPage);

			AssertPageContentsAreEqual(new PageContent(newPage, "Title", "NUnit", dt, "Comment", "Content", new string[0], null), content);
		}

		[Test]
		public void ModifyPage_Draft_RenameNamespace() {
			IPagesStorageProviderV30 prov = GetProvider();

			DateTime dt = DateTime.Now;
			NamespaceInfo ns = prov.AddNamespace("NS");
			PageInfo page = prov.AddPage("NS", "Page", dt);
			prov.ModifyPage(page, "Title", "NUnit", dt, "", "", new string[0], "", SaveMode.Normal);
			prov.ModifyPage(page, "Title", "NUnit", dt, "Comment", "Content", new string[0], "", SaveMode.Draft);

			NamespaceInfo ns2 = prov.RenameNamespace(ns, "NS2");

			PageInfo page2 = new PageInfo(NameTools.GetFullName(ns2.Name, "Page"), prov, dt);

			Assert.IsNull(prov.GetDraft(page), "GetDraft should return null");
			PageContent content = prov.GetDraft(page2);

			AssertPageContentsAreEqual(new PageContent(page2, "Title", "NUnit", dt, "Comment", "Content", new string[0], null), content);
		}

		[Test]
		public void ModifyPage_Draft_RemoveNamespace() {
			IPagesStorageProviderV30 prov = GetProvider();

			DateTime dt = DateTime.Now;
			NamespaceInfo ns = prov.AddNamespace("NS");
			PageInfo page = prov.AddPage("NS", "Page", dt);
			prov.ModifyPage(page, "Title", "NUnit", dt, "", "", new string[0], "", SaveMode.Normal);
			prov.ModifyPage(page, "Title", "NUnit", dt, "Comment", "Content", new string[0], "", SaveMode.Draft);

			prov.RemoveNamespace(ns);

			Assert.IsNull(prov.GetDraft(page), "GetDraft should return null");
		}

		[Test]
		public void ModifyPage_Draft_MovePage_Root2Sub() {
			IPagesStorageProviderV30 prov = GetProvider();

			DateTime dt = DateTime.Now;
			PageInfo page = prov.AddPage(null, "Page", dt);
			prov.ModifyPage(page, "Title", "NUnit", dt, "", "", new string[0], "", SaveMode.Normal);
			prov.ModifyPage(page, "Title", "NUnit", dt, "Comment", "Content", new string[0], "", SaveMode.Draft);

			NamespaceInfo ns2 = prov.AddNamespace("NS2");

			PageInfo page2 = prov.MovePage(page, ns2, false);

			Assert.IsNull(prov.GetDraft(page), "GetDraft should return null");
			PageContent content = prov.GetDraft(page2);

			AssertPageContentsAreEqual(new PageContent(page2, "Title", "NUnit", dt, "Comment", "Content", new string[0], null), content);
		}

		[Test]
		public void ModifyPage_Draft_MovePage_Sub2Root() {
			IPagesStorageProviderV30 prov = GetProvider();

			DateTime dt = DateTime.Now;
			NamespaceInfo ns = prov.AddNamespace("NS");
			PageInfo page = prov.AddPage("NS", "Page", dt);
			prov.ModifyPage(page, "Title", "NUnit", dt, "", "", new string[0], "", SaveMode.Normal);
			prov.ModifyPage(page, "Title", "NUnit", dt, "Comment", "Content", new string[0], "", SaveMode.Draft);

			PageInfo page2 = prov.MovePage(page, null, false);

			Assert.IsNull(prov.GetDraft(page), "GetDraft should return null");
			PageContent content = prov.GetDraft(page2);

			AssertPageContentsAreEqual(new PageContent(page2, "Title", "NUnit", dt, "Comment", "Content", new string[0], null), content);
		}

		[Test]
		public void ModifyPage_Draft_MovePage_Sub2Sub() {
			IPagesStorageProviderV30 prov = GetProvider();

			DateTime dt = DateTime.Now;
			NamespaceInfo ns = prov.AddNamespace("NS");
			PageInfo page = prov.AddPage("NS", "Page", dt);
			prov.ModifyPage(page, "Title", "NUnit", dt, "", "", new string[0], "", SaveMode.Normal);
			prov.ModifyPage(page, "Title", "NUnit", dt, "Comment", "Content", new string[0], "", SaveMode.Draft);

			NamespaceInfo ns2 = prov.AddNamespace("NS2");
			PageInfo page2 = prov.MovePage(page, ns2, false);

			Assert.IsNull(prov.GetDraft(page), "GetDraft should return null");
			PageContent content = prov.GetDraft(page2);

			AssertPageContentsAreEqual(new PageContent(page2, "Title", "NUnit", dt, "Comment", "Content", new string[0], null), content);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void GetDraft_NullPage() {
			IPagesStorageProviderV30 prov = GetProvider();

			prov.GetDraft(null);
		}

		[Test]
		public void GetDraft_Inexistent() {
			IPagesStorageProviderV30 prov = GetProvider();

			Assert.IsNull(prov.GetDraft(new PageInfo("Page", null, DateTime.Now)), "GetDraft should return null");

			PageInfo page = prov.AddPage(null, "Page", DateTime.Now);
			Assert.IsNull(prov.GetDraft(page), "GetDraft should return null");

			prov.ModifyPage(page, "Title", "NUnit", DateTime.Now, "", "Content", new string[0], "", SaveMode.Normal);
			Assert.IsNull(prov.GetDraft(page), "GetDraft should return null");

			prov.ModifyPage(page, "Title", "NUnit", DateTime.Now, "", "Content", new string[0], "", SaveMode.Backup);
			Assert.IsNull(prov.GetDraft(page), "GetDraft should return null");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void DeleteDraft_NullPage() {
			IPagesStorageProviderV30 prov = GetProvider();

			prov.DeleteDraft(null);
		}

		[Test]
		public void DeleteDraft_Inexistent() {
			IPagesStorageProviderV30 prov = GetProvider();

			Assert.IsFalse(prov.DeleteDraft(new PageInfo("Page", null, DateTime.Now)), "DeleteDraft should return false");

			PageInfo page = prov.AddPage(null, "Page", DateTime.Now);
			Assert.IsFalse(prov.DeleteDraft(page), "DeleteDraft should return false");

			prov.ModifyPage(page, "Title", "NUnit", DateTime.Now, "", "Content", new string[0], "", SaveMode.Normal);
			Assert.IsFalse(prov.DeleteDraft(page), "DeleteDraft should return false");

			DateTime dt = DateTime.Now;
			prov.ModifyPage(page, "Title2", "NUnit2", dt, "", "Content2", new string[0], "", SaveMode.Backup);
			Assert.IsFalse(prov.DeleteDraft(page), "DeleteDraft should return false");

			AssertPageContentsAreEqual(new PageContent(page, "Title2", "NUnit2", dt, "", "Content2", new string[0], null),
				prov.GetContent(page));
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void GetBackups_NullPage() {
			IPagesStorageProviderV30 prov = GetProvider();
			prov.GetBackups(null);
		}

		[Test]
		public void GetBackups_InexistentPage_Root() {
			IPagesStorageProviderV30 prov = GetProvider();
			Assert.IsNull(prov.GetBackups(new PageInfo("PPP", prov, DateTime.Now)), "GetBackups should return null");
		}

		[Test]
		public void GetBackups_InexistentPage_Sub() {
			IPagesStorageProviderV30 prov = GetProvider();
			NamespaceInfo ns = prov.AddNamespace("Namespace");
			Assert.IsNull(prov.GetBackups(new PageInfo(NameTools.GetFullName(ns.Name, "PPP"), prov, DateTime.Now)), "GetBackups should return null");
		}

		[Test]
		public void SetBackupContent_GetBackupContent_Root() {
			IPagesStorageProviderV30 prov = GetProvider();

			PageInfo p = prov.AddPage(null, "Page", DateTime.Now);
			prov.ModifyPage(p, "Title", "NUnit", DateTime.Now, "Comment", "Content", null, null, SaveMode.Normal);
			prov.ModifyPage(p, "Title1", "NUnit1", DateTime.Now, "Comment1", "Content1", null, null, SaveMode.Backup);

			PageContent content = new PageContent(p, "Title100", "NUnit100", DateTime.Now.AddDays(-1), "Comment100", "Content100", null, null);
			PageContent contentInexistent = new PageContent(new PageInfo("PPP", prov, DateTime.Now),
				"Title100", "NUnit100", DateTime.Now.AddDays(-1), "Comment100", "Content100", null, null);

			Assert.IsFalse(prov.SetBackupContent(contentInexistent, 0), "SetBackupContent should return ");

			Assert.IsTrue(prov.SetBackupContent(content, 0), "SetBackupContent should return true");

			PageContent contentOutput = prov.GetBackupContent(p, 0);

			AssertPageContentsAreEqual(content, contentOutput);
		}

		[Test]
		public void SetBackupContent_GetBackupContent_Sub() {
			IPagesStorageProviderV30 prov = GetProvider();

			NamespaceInfo ns = prov.AddNamespace("Namespace");

			PageInfo p = prov.AddPage(ns.Name, "Page", DateTime.Now);
			prov.ModifyPage(p, "Title", "NUnit", DateTime.Now, "Comment", "Content", null, null, SaveMode.Normal);
			prov.ModifyPage(p, "Title1", "NUnit1", DateTime.Now, "Comment1", "Content1", null, null, SaveMode.Backup);

			PageContent content = new PageContent(p, "Title100", "NUnit100", DateTime.Now.AddDays(-1), "Comment100", "Content100", null, null);
			PageContent contentInexistent = new PageContent(new PageInfo(NameTools.GetFullName(ns.Name, "PPP"), prov, DateTime.Now),
				"Title100", "NUnit100", DateTime.Now.AddDays(-1), "Comment100", "Content100", null, null);

			Assert.IsFalse(prov.SetBackupContent(contentInexistent, 0), "SetBackupContent should return ");

			Assert.IsTrue(prov.SetBackupContent(content, 0), "SetBackupContent should return true");

			PageContent contentOutput = prov.GetBackupContent(p, 0);

			AssertPageContentsAreEqual(content, contentOutput);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void GetBackupContent_NullPage() {
			IPagesStorageProviderV30 prov = GetProvider();
			prov.GetBackupContent(null, 0);
		}

		[Test]
		public void GetBackupContent_InexistentPage_Root() {
			IPagesStorageProviderV30 prov = GetProvider();
			Assert.IsNull(prov.GetBackupContent(new PageInfo("P", prov, DateTime.Now), 0), "GetBackupContent should return null");
		}

		[Test]
		public void GetBackupContent_InexistentPage_Sub() {
			IPagesStorageProviderV30 prov = GetProvider();
			NamespaceInfo ns = prov.AddNamespace("Namespace");
			Assert.IsNull(prov.GetBackupContent(new PageInfo(NameTools.GetFullName(ns.Name, "P"), prov, DateTime.Now), 0),
				"GetBackupContent should return null");
		}

		[Test]
		public void GetBackupContent_InexistentRevision_Root() {
			IPagesStorageProviderV30 prov = GetProvider();

			PageInfo p = prov.AddPage(null, "Page", DateTime.Now);
			prov.ModifyPage(p, "Title", "NUnit", DateTime.Now, "Comment", "Content", null, null, SaveMode.Normal);
			prov.ModifyPage(p, "Title1", "NUnit1", DateTime.Now, "Comment1", "Content1", null, null, SaveMode.Backup);

			Assert.IsNull(prov.GetBackupContent(p, 1), "GetBackupContent should return null");
		}

		[Test]
		public void GetBackupContent_InexistentRevision_Sub() {
			IPagesStorageProviderV30 prov = GetProvider();

			NamespaceInfo ns = prov.AddNamespace("NS");

			PageInfo p = prov.AddPage(ns.Name, "Page", DateTime.Now);
			prov.ModifyPage(p, "Title", "NUnit", DateTime.Now, "Comment", "Content", null, null, SaveMode.Normal);
			prov.ModifyPage(p, "Title1", "NUnit1", DateTime.Now, "Comment1", "Content1", null, null, SaveMode.Backup);

			Assert.IsNull(prov.GetBackupContent(p, 1), "GetBackupContent should return null");
		}

		[Test]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void GetBackupContent_InvalidRevision() {
			IPagesStorageProviderV30 prov = GetProvider();

			PageInfo p = prov.AddPage(null, "Page", DateTime.Now);
			prov.ModifyPage(p, "Title", "NUnit", DateTime.Now, "Comment", "Content", null, null, SaveMode.Normal);
			prov.ModifyPage(p, "Title1", "NUnit1", DateTime.Now, "Comment1", "Content1", null, null, SaveMode.Backup);

			prov.GetBackupContent(p, -1);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void SetBackupContent_NullContent() {
			IPagesStorageProviderV30 prov = GetProvider();
			prov.SetBackupContent(null, 0);
		}
		
		[Test]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void SetBackupContent_InvalidRevision() {
			IPagesStorageProviderV30 prov = GetProvider();

			PageInfo p = prov.AddPage(null, "Page", DateTime.Now);
			prov.ModifyPage(p, "Title", "NUnit", DateTime.Now, "Comment", "Content", null, null, SaveMode.Normal);
			prov.ModifyPage(p, "Title1", "NUnit1", DateTime.Now, "Comment1", "Content1", null, null, SaveMode.Backup);

			prov.SetBackupContent(new PageContent(p, "Title100", "NUnit100", DateTime.Now, "Comment100", "Content100", null, null), -1);
		}

		[Test]
		public void SetBackupContent_InexistentRevision_Root() {
			IPagesStorageProviderV30 prov = GetProvider();

			PageInfo p = prov.AddPage(null, "Page", DateTime.Now);
			prov.ModifyPage(p, "Title", "NUnit", DateTime.Now, "Comment", "Content", null, null, SaveMode.Normal);
			prov.ModifyPage(p, "Title1", "NUnit1", DateTime.Now, "Comment1", "Content1", null, null, SaveMode.Backup);

			PageContent testContent = new PageContent(p, "Title100", "NUnit100", DateTime.Now, "Comment100", "Content100", null, null);

			prov.SetBackupContent(testContent, 5);

			PageContent backup = prov.GetBackupContent(p, 5);

			AssertPageContentsAreEqual(testContent, backup);

			int[] baks = prov.GetBackups(p);
			Assert.AreEqual(2, baks.Length, "Wrong backup count");
			Assert.AreEqual(0, baks[0], "Wrong backup number");
			Assert.AreEqual(5, baks[1], "Wrong backup number");
		}

		[Test]
		public void SetBackupContent_InexistentRevision_Sub() {
			IPagesStorageProviderV30 prov = GetProvider();

			NamespaceInfo ns = prov.AddNamespace("NS");

			PageInfo p = prov.AddPage(ns.Name, "Page", DateTime.Now);
			prov.ModifyPage(p, "Title", "NUnit", DateTime.Now, "Comment", "Content", null, null, SaveMode.Normal);
			prov.ModifyPage(p, "Title1", "NUnit1", DateTime.Now, "Comment1", "Content1", null, null, SaveMode.Backup);

			PageContent testContent = new PageContent(p, "Title100", "NUnit100", DateTime.Now, "Comment100", "Content100", null, null);

			prov.SetBackupContent(testContent, 5);

			PageContent backup = prov.GetBackupContent(p, 5);

			AssertPageContentsAreEqual(testContent, backup);

			int[] baks = prov.GetBackups(p);
			Assert.AreEqual(2, baks.Length, "Wrong backup count");
			Assert.AreEqual(0, baks[0], "Wrong backup number");
			Assert.AreEqual(5, baks[1], "Wrong backup number");
		}

		[Test]
		public void RenamePage_Root() {
			IPagesStorageProviderV30 prov = GetProvider();

			PageInfo p = prov.AddPage(null, "Page", DateTime.Now);
			prov.AddPage(null, "Page2", DateTime.Now);
			prov.ModifyPage(p, "Title", "NUnit", DateTime.Now, "Comment", "Content", null, null, SaveMode.Normal);
			prov.ModifyPage(p, "Title1", "NUnit1", DateTime.Now, "Comment1", "Content1", null, null, SaveMode.Backup);
			CategoryInfo cat1 = prov.AddCategory(null, "Cat1");
			CategoryInfo cat2 = prov.AddCategory(null, "Cat2");
			CategoryInfo cat3 = prov.AddCategory(null, "Cat3");
			prov.RebindPage(p, new string[] { cat1.FullName, cat3.FullName });
			PageContent content = prov.GetContent(p);

			Assert.IsTrue(prov.AddMessage(p, "NUnit", "Test", DateTime.Now, "Test message.", -1), "AddMessage should return true");

			Assert.IsNull(prov.RenamePage(new PageInfo("Inexistent", prov, DateTime.Now), "RenamedPage"), "RenamePage should return null");
			Assert.IsNull(prov.RenamePage(p, "Page2"), "RenamePage should return null");

			PageInfo renamed = prov.RenamePage(p, "Renamed");
			Assert.IsNotNull(renamed, "RenamePage should return something");
			AssertPageInfosAreEqual(new PageInfo("Renamed", prov, p.CreationDateTime), renamed, true);

			Assert.IsNull(prov.GetContent(p), "GetContent should return null");

			AssertPageContentsAreEqual(new PageContent(renamed, content.Title, content.User, content.LastModified,
				content.Comment, content.Content, null, null),
				prov.GetContent(renamed));

			Assert.IsNull(prov.GetBackups(p), "GetBackups should return null");
			Assert.AreEqual(1, prov.GetBackups(renamed).Length, "Wrong backup count");

			CategoryInfo[] categories = prov.GetCategories(null);
			Assert.AreEqual(3, categories.Length, "Wrong category count");
			Array.Sort(categories, delegate(CategoryInfo x, CategoryInfo y) { return x.FullName.CompareTo(y.FullName); });

			Assert.AreEqual(1, categories[0].Pages.Length, "Wrong page count");
			Assert.AreEqual(renamed.FullName, categories[0].Pages[0], "Wrong page");
			Assert.AreEqual(0, categories[1].Pages.Length, "Wrong page count");
			Assert.AreEqual(1, categories[2].Pages.Length, "Wrong page count");
			Assert.AreEqual(renamed.FullName, categories[2].Pages[0], "Wrong page");

			Assert.IsNull(prov.GetMessages(p), "GetMessages should return null");
			Message[] messages = prov.GetMessages(renamed);
			Assert.AreEqual(1, messages.Length, "Wrong message count");
			Assert.AreEqual("Test", messages[0].Subject, "Wrong message subject");

		}

		[Test]
		public void RenamePage_Sub() {
			IPagesStorageProviderV30 prov = GetProvider();

			NamespaceInfo ns = prov.AddNamespace("NS");

			PageInfo p = prov.AddPage(ns.Name, "Page", DateTime.Now);
			prov.AddPage(ns.Name, "Page2", DateTime.Now);
			prov.ModifyPage(p, "Title", "NUnit", DateTime.Now, "Comment", "Content", null, null, SaveMode.Normal);
			prov.ModifyPage(p, "Title1", "NUnit1", DateTime.Now, "Comment1", "Content1", null, null, SaveMode.Backup);
			CategoryInfo cat1 = prov.AddCategory(ns.Name, "Cat1");
			CategoryInfo cat2 = prov.AddCategory(ns.Name, "Cat2");
			CategoryInfo cat3 = prov.AddCategory(ns.Name, "Cat3");
			prov.RebindPage(p, new string[] { cat1.FullName, cat3.FullName });
			PageContent content = prov.GetContent(p);

			Assert.IsTrue(prov.AddMessage(p, "NUnit", "Test", DateTime.Now, "Test message.", -1), "AddMessage should return true");

			Assert.IsNull(prov.RenamePage(new PageInfo(NameTools.GetFullName(ns.Name, "Inexistent"), prov, DateTime.Now), "RenamedPage"), "RenamePage should return null");
			Assert.IsNull(prov.RenamePage(p, "Page2"), "RenamePage should return null");

			PageInfo renamed = prov.RenamePage(p, "Renamed");
			Assert.IsNotNull(renamed, "RenamePage should return something");
			AssertPageInfosAreEqual(new PageInfo(NameTools.GetFullName(ns.Name, "Renamed"), prov, p.CreationDateTime), renamed, true);

			Assert.IsNull(prov.GetContent(p), "GetContent should return null");

			AssertPageContentsAreEqual(new PageContent(renamed, content.Title, content.User, content.LastModified, content.Comment, content.Content, null, null),
				prov.GetContent(renamed));

			Assert.IsNull(prov.GetBackups(p), "GetBackups should return null");
			Assert.AreEqual(1, prov.GetBackups(renamed).Length, "Wrong backup count");

			CategoryInfo[] categories = prov.GetCategories(ns);
			Assert.AreEqual(3, categories.Length, "Wrong category count");
			Array.Sort(categories, delegate(CategoryInfo x, CategoryInfo y) { return x.FullName.CompareTo(y.FullName); });

			Assert.AreEqual(1, categories[0].Pages.Length, "Wrong page count");
			Assert.AreEqual(renamed.FullName, categories[0].Pages[0], "Wrong page");
			Assert.AreEqual(0, categories[1].Pages.Length, "Wrong page count");
			Assert.AreEqual(1, categories[2].Pages.Length, "Wrong page count");
			Assert.AreEqual(renamed.FullName, categories[2].Pages[0], "Wrong page");

			Assert.IsNull(prov.GetMessages(p), "GetMessages should return null");
			Message[] messages = prov.GetMessages(renamed);
			Assert.AreEqual(1, messages.Length, "Wrong message count");
			Assert.AreEqual("Test", messages[0].Subject, "Wrong message subject");
		}

		[Test]
		public void RenamePage_DefaultPage() {
			IPagesStorageProviderV30 prov = GetProvider();

			NamespaceInfo ns = prov.AddNamespace("NS");

			PageInfo page = prov.AddPage("NS", "MainPage", DateTime.Now);

			prov.SetNamespaceDefaultPage(ns, page);

			Assert.IsNull(prov.RenamePage(page, "NewName"), "Cannot rename the default page");
		}

		[Test]
		public void RenamePage_PerformSearch() {
			IPagesStorageProviderV30 prov = GetProvider();

			PageInfo p = prov.AddPage(null, "Page", DateTime.Now);
			prov.AddMessage(p, "NUnit", "Message1", DateTime.Now, "Body1", -1);
			prov.AddMessage(p, "NUnit", "Message2", DateTime.Now, "Body2", prov.GetMessages(p)[0].ID);

			DateTime dt = DateTime.Now;

			Assert.IsTrue(prov.ModifyPage(p, "TitleOld", "NUnitOld", dt, "CommentOld", "ContentOld", null, null, SaveMode.Normal),
				"ModifyPage should return true");
			Assert.IsTrue(prov.ModifyPage(p, "Title", "NUnit", dt, "Comment", "Content", null, null, SaveMode.Backup),
				"ModifyPage should return true");
			prov.RenamePage(p, "Page2");

			SearchResultCollection results = prov.PerformSearch(new SearchParameters("content"));

			Assert.AreEqual(1, results.Count, "Wrong search result count");
			Assert.AreEqual("Page2", PageDocument.GetPageName(results[0].Document.Name), "Wrong document name");

			results = prov.PerformSearch(new SearchParameters("title"));

			Assert.AreEqual(1, results.Count, "Wrong search result count");
			Assert.AreEqual("Page2", PageDocument.GetPageName(results[0].Document.Name), "Wrong document name");

			results = prov.PerformSearch(new SearchParameters("message1 body1 message2 body2"));
			Assert.AreEqual(2, results.Count, "Wrong result count");
			Assert.AreEqual(2, results[0].Matches.Count, "Wrong match count");
			Assert.AreEqual(2, results[1].Matches.Count, "Wrong match count");

			string page;
			int id;

			MessageDocument.GetMessageDetails(results[0].Document.Name, out page, out id);
			Assert.AreEqual("Page2", page, "Wrong document name");

			MessageDocument.GetMessageDetails(results[1].Document.Name, out page, out id);
			Assert.AreEqual("Page2", page, "Wrong document name");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RenamePage_NullPage() {
			IPagesStorageProviderV30 prov = GetProvider();
			prov.RenamePage(null, "New Name");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void RenamePage_InvalidName(string n) {
			IPagesStorageProviderV30 prov = GetProvider();

			PageInfo p = prov.AddPage(null, "Page", DateTime.Now);
			prov.ModifyPage(p, "Title", "NUnit", DateTime.Now, "Comment", "Content", null, null, SaveMode.Normal);

			prov.RenamePage(p, n);
		}

		[Test]
		public void RollbackPage_Root() {
			IPagesStorageProviderV30 prov = GetProvider();

			PageInfo p = prov.AddPage(null, "Page", DateTime.Now);
			prov.ModifyPage(p, "Title", "NUnit", DateTime.Now, "Comment", "Content", new string[] { "kold", "k2old" }, "DescrOld", SaveMode.Normal);
			prov.ModifyPage(p, "Title1", "NUnit1", DateTime.Now, "Comment1", "Content1", new string[] { "k1", "k2" }, "Descr", SaveMode.Backup);

			PageContent content = prov.GetContent(p);

			prov.ModifyPage(p, "Title2", "NUnit2", DateTime.Now, "Comment2", "Content2", new string[] { "k4", "k5" }, "DescrNew", SaveMode.Backup);

			Assert.AreEqual(2, prov.GetBackups(p).Length, "Wrong backup count");

			Assert.IsFalse(prov.RollbackPage(new PageInfo("Inexistent", prov, DateTime.Now), 0), "RollbackPage should return false");
			Assert.IsFalse(prov.RollbackPage(p, 5), "RollbackPage should return false");

			Assert.IsTrue(prov.RollbackPage(p, 1), "RollbackPage should return true");

			Assert.AreEqual(3, prov.GetBackups(p).Length, "Wrong backup count");

			AssertPageContentsAreEqual(content, prov.GetContent(p));
		}

		[Test]
		public void RollbackPage_Sub() {
			IPagesStorageProviderV30 prov = GetProvider();

			NamespaceInfo ns = prov.AddNamespace("NS");

			PageInfo p = prov.AddPage(ns.Name, "Page", DateTime.Now);
			prov.ModifyPage(p, "Title", "NUnit", DateTime.Now, "Comment", "Content", new string[] { "kold", "k2old" }, "DescrOld", SaveMode.Normal);
			prov.ModifyPage(p, "Title1", "NUnit1", DateTime.Now, "Comment1", "Content1", new string[] { "k1", "k2" }, "Descr", SaveMode.Backup);

			PageContent content = prov.GetContent(p);

			prov.ModifyPage(p, "Title2", "NUnit2", DateTime.Now, "Comment2", "Content2", new string[] { "k4", "k5" }, "DescrNew", SaveMode.Backup);

			Assert.AreEqual(2, prov.GetBackups(p).Length, "Wrong backup count");

			Assert.IsFalse(prov.RollbackPage(new PageInfo(NameTools.GetFullName(ns.Name, "Inexistent"), prov, DateTime.Now), 0),
				"RollbackPage should return false");

			Assert.IsFalse(prov.RollbackPage(p, 5), "RollbackPage should return false");

			Assert.IsTrue(prov.RollbackPage(p, 1), "RollbackPage should return true");

			Assert.AreEqual(3, prov.GetBackups(p).Length, "Wrong backup count");

			AssertPageContentsAreEqual(content, prov.GetContent(p));
		}

		[Test]
		public void RollbackPage_PerformSearch() {
			IPagesStorageProviderV30 prov = GetProvider();

			PageInfo p = prov.AddPage(null, "Page", DateTime.Now);

			DateTime dt = DateTime.Now;

			Assert.IsTrue(prov.ModifyPage(p, "Title", "NUnit", dt, "Comment", "Content", new string[] { "k1" }, "descr", SaveMode.Backup), "ModifyPage should return true");
			Assert.IsTrue(prov.ModifyPage(p, "TitleMod", "NUnit", dt, "Comment", "ContentMod", new string[] { "k2" }, "descr2", SaveMode.Backup), "ModifyPage should return true");
			// Depending on the implementation, providers might start backups numbers from 0 or 1, or even don't perform a backup if the page has no content (as in this case)
			int[] baks = prov.GetBackups(p);
			prov.RollbackPage(p, baks[baks.Length - 1]);

			Assert.AreEqual(0, prov.PerformSearch(new SearchParameters("contentmod")).Count, "Wrong search result count");
			Assert.AreEqual(1, prov.PerformSearch(new SearchParameters("content")).Count, "Wrong search result count");

			Assert.AreEqual(0, prov.PerformSearch(new SearchParameters("k2")).Count, "Wrong search result count");
			Assert.AreEqual(1, prov.PerformSearch(new SearchParameters("k1")).Count, "Wrong search result count");

			Assert.AreEqual(0, prov.PerformSearch(new SearchParameters("titlemod")).Count, "Wrong search result count");
			Assert.AreEqual(1, prov.PerformSearch(new SearchParameters("title")).Count, "Wrong search result count");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RollbackPage_NullPage() {
			IPagesStorageProviderV30 prov = GetProvider();

			prov.RollbackPage(null, 0);
		}

		[Test]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void RollbackPage_InvalidRevision() {
			IPagesStorageProviderV30 prov = GetProvider();

			PageInfo p = prov.AddPage(null, "Page", DateTime.Now);
			prov.ModifyPage(p, "Title", "NUnit", DateTime.Now, "Comment", "Content", null, null, SaveMode.Normal);
			prov.ModifyPage(p, "Title1", "NUnit1", DateTime.Now, "Comment1", "Content1", null, null, SaveMode.Backup);

			prov.RollbackPage(p, -1);
		}

		[Test]
		public void DeleteBackups_Root() {
			IPagesStorageProviderV30 prov = GetProvider();

			PageInfo p = prov.AddPage(null, "Page", DateTime.Now);
			prov.ModifyPage(p, "Title", "NUnit", DateTime.Now, "Comment", "Content", null, null, SaveMode.Normal);
			prov.ModifyPage(p, "Title1", "NUnit1", DateTime.Now, "Comment1", "Content1", null, null, SaveMode.Backup);
			prov.ModifyPage(p, "Title2", "NUnit2", DateTime.Now, "Comment2", "Content2", new string[] { "k1", "k2" }, "Descr", SaveMode.Backup);

			PageContent content = prov.GetContent(p);

			Assert.IsFalse(prov.DeleteBackups(new PageInfo("Inexistent", prov, DateTime.Now), -1), "DeleteBackups should return false");

			Assert.IsTrue(prov.DeleteBackups(p, 5), "DeleteBackups should return true");
			Assert.AreEqual(2, prov.GetBackups(p).Length, "Wrong backup count");
			AssertPageContentsAreEqual(content, prov.GetContent(p));

			Assert.IsTrue(prov.DeleteBackups(p, 0), "DeleteBackups should return true");
			Assert.AreEqual(1, prov.GetBackups(p).Length, "Wrong backup count");
			AssertPageContentsAreEqual(content, prov.GetContent(p));

			Assert.IsTrue(prov.DeleteBackups(p, -1), "DeleteBackups should return true");
			Assert.AreEqual(0, prov.GetBackups(p).Length, "Wrong backup count");
			AssertPageContentsAreEqual(content, prov.GetContent(p));
		}

		[Test]
		public void DeleteBackups_Sub() {
			IPagesStorageProviderV30 prov = GetProvider();

			NamespaceInfo ns = prov.AddNamespace("NS");

			PageInfo p = prov.AddPage(ns.Name, "Page", DateTime.Now);
			prov.ModifyPage(p, "Title", "NUnit", DateTime.Now, "Comment", "Content", null, null, SaveMode.Normal);
			prov.ModifyPage(p, "Title1", "NUnit1", DateTime.Now, "Comment1", "Content1", null, null, SaveMode.Backup);
			prov.ModifyPage(p, "Title2", "NUnit2", DateTime.Now, "Comment2", "Content2", new string[] { "k1", "k2" }, "Descr", SaveMode.Backup);

			PageContent content = prov.GetContent(p);

			Assert.IsFalse(prov.DeleteBackups(new PageInfo(NameTools.GetFullName(ns.Name, "Inexistent"),
				prov, DateTime.Now), -1), "DeleteBackups should return false");

			Assert.IsTrue(prov.DeleteBackups(p, 5), "DeleteBackups should return true");
			Assert.AreEqual(2, prov.GetBackups(p).Length, "Wrong backup count");
			AssertPageContentsAreEqual(content, prov.GetContent(p));

			Assert.IsTrue(prov.DeleteBackups(p, 0), "DeleteBackups should return true");
			Assert.AreEqual(1, prov.GetBackups(p).Length, "Wrong backup count");
			AssertPageContentsAreEqual(content, prov.GetContent(p));

			Assert.IsTrue(prov.DeleteBackups(p, -1), "DeleteBackups should return true");
			Assert.AreEqual(0, prov.GetBackups(p).Length, "Wrong backup count");
			AssertPageContentsAreEqual(content, prov.GetContent(p));
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void DeleteBackups_NullPage() {
			IPagesStorageProviderV30 prov = GetProvider();

			prov.DeleteBackups(null, -1);
		}

		[Test]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void DeleteBackups_InvalidRevision() {
			IPagesStorageProviderV30 prov = GetProvider();

			PageInfo p = prov.AddPage(null, "Page", DateTime.Now);
			prov.ModifyPage(p, "Title", "NUnit", DateTime.Now, "Comment", "Content", null, null, SaveMode.Normal);
			prov.ModifyPage(p, "Title1", "NUnit1", DateTime.Now, "Comment1", "Content1", null, null, SaveMode.Backup);
			prov.ModifyPage(p, "Title2", "NUnit2", DateTime.Now, "Comment2", "Content2", null, null, SaveMode.Backup);

			prov.DeleteBackups(p, -2);
		}

		[Test]
		public void RemovePage_Root() {
			IPagesStorageProviderV30 prov = GetProvider();

			PageInfo p = prov.AddPage(null, "Page", DateTime.Now);
			prov.ModifyPage(p, "Title", "NUnit", DateTime.Now, "Comment", "Content", null, null, SaveMode.Normal);

			Assert.IsFalse(prov.RemovePage(new PageInfo("Inexistent", prov, DateTime.Now)),
				"RemovePage should return false");

			Assert.IsTrue(prov.RemovePage(p), "RemovePage should return true");

			Assert.AreEqual(0, prov.GetPages(null).Length, "Wrong page count");
			Assert.IsNull(prov.GetContent(p), "GetContent should return null");
			Assert.IsNull(prov.GetBackups(p), "GetBackups should return null");
		}

		[Test]
		public void RemovePage_Sub() {
			IPagesStorageProviderV30 prov = GetProvider();

			NamespaceInfo ns = prov.AddNamespace("NS");

			PageInfo p = prov.AddPage(ns.Name, "Page", DateTime.Now);
			prov.ModifyPage(p, "Title", "NUnit", DateTime.Now, "Comment", "Content", null, null, SaveMode.Normal);

			Assert.IsFalse(prov.RemovePage(new PageInfo(NameTools.GetFullName(ns.Name, "Inexistent"),
				prov, DateTime.Now)), "RemovePage should return false");

			Assert.IsTrue(prov.RemovePage(p), "RemovePage should return true");

			Assert.AreEqual(0, prov.GetPages(ns).Length, "Wrong page count");
			Assert.IsNull(prov.GetContent(p), "GetContent should return null");
			Assert.IsNull(prov.GetBackups(p), "GetBackups should return null");
		}

		[Test]
		public void RemovePage_DefaultPage() {
			IPagesStorageProviderV30 prov = GetProvider();

			NamespaceInfo ns = prov.AddNamespace("NS");

			PageInfo page = prov.AddPage("NS", "MainPage", DateTime.Now);

			prov.SetNamespaceDefaultPage(ns, page);

			Assert.IsFalse(prov.RemovePage(page), "Cannot remove default page");
		}

		[Test]
		public void RemovePage_PerformSearch() {
			IPagesStorageProviderV30 prov = GetProvider();

			PageInfo p = prov.AddPage(null, "Page", DateTime.Now);

			prov.AddMessage(p, "NUnit", "Test1", DateTime.Now, "Body1", -1);
			prov.AddMessage(p, "NUnit", "Test2", DateTime.Now, "Body2", prov.GetMessages(p)[0].ID);

			DateTime dt = DateTime.Now;

			Assert.IsTrue(prov.ModifyPage(p, "Title", "NUnit", dt, "Comment", "Content", null, null, SaveMode.Normal),
				"ModifyPage should return true");

			prov.RemovePage(p);

			Assert.AreEqual(0, prov.PerformSearch(new SearchParameters("content")).Count, "Wrong search result count");
			Assert.AreEqual(0, prov.PerformSearch(new SearchParameters("title")).Count, "Wrong search result count");

			Assert.AreEqual(0, prov.PerformSearch(new SearchParameters("test1 test2 body1 body2")).Count, "Wrong result count");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RemovePage_NullPage() {
			IPagesStorageProviderV30 prov = GetProvider();

			prov.RemovePage(null);
		}

		[Test]
		public void RebindPage_Root() {
			IPagesStorageProviderV30 prov = GetProvider();

			CategoryInfo cat1 = prov.AddCategory(null, "Cat1");
			CategoryInfo cat2 = prov.AddCategory(null, "Cat2");

			PageInfo page = prov.AddPage(null, "Page", DateTime.Now);

			Assert.IsFalse(prov.RebindPage(new PageInfo("Inexistent", prov, DateTime.Now), new string[] { "Cat1" }), "Rebind should return false");

			Assert.IsTrue(prov.RebindPage(page, new string[] { "Cat1" }), "Rebind should return true");

			CategoryInfo[] categories = prov.GetCategories(null);
			Array.Sort(categories, delegate(CategoryInfo x, CategoryInfo y) { return x.FullName.CompareTo(y.FullName); });
			Assert.AreEqual(1, categories[0].Pages.Length, "Wrong page count");
			Assert.AreEqual("Page", categories[0].Pages[0], "Wrong page name");

			Assert.IsTrue(prov.RebindPage(page, new string[0]), "Rebind should return true");

			categories = prov.GetCategories(null);
			Array.Sort(categories, delegate(CategoryInfo x, CategoryInfo y) { return x.FullName.CompareTo(y.FullName); });
			Assert.AreEqual(0, categories[0].Pages.Length, "Wrong page count");
		}

		[Test]
		public void RebindPage_Sub() {
			IPagesStorageProviderV30 prov = GetProvider();

			NamespaceInfo ns = prov.AddNamespace("NS");

			CategoryInfo cat1 = prov.AddCategory(ns.Name, "Cat1");
			CategoryInfo cat2 = prov.AddCategory(ns.Name, "Cat2");

			PageInfo page = prov.AddPage(ns.Name, "Page", DateTime.Now);

			Assert.IsFalse(prov.RebindPage(new PageInfo(NameTools.GetFullName(ns.Name, "Inexistent"), prov, DateTime.Now), new string[] { "Cat1" }), "Rebind should return false");

			Assert.IsTrue(prov.RebindPage(page, new string[] { cat1.FullName }), "Rebind should return true");

			CategoryInfo[] categories = prov.GetCategories(ns);
			Array.Sort(categories, delegate(CategoryInfo x, CategoryInfo y) { return x.FullName.CompareTo(y.FullName); });
			Assert.AreEqual(1, categories[0].Pages.Length, "Wrong page count");
			Assert.AreEqual(page.FullName, categories[0].Pages[0], "Wrong page name");

			Assert.IsTrue(prov.RebindPage(page, new string[0]), "Rebind should return true");

			categories = prov.GetCategories(ns);
			Array.Sort(categories, delegate(CategoryInfo x, CategoryInfo y) { return x.FullName.CompareTo(y.FullName); });
			Assert.AreEqual(0, categories[0].Pages.Length, "Wrong page count");
		}

		[Test]
		public void RebindPage_SameNames() {
			IPagesStorageProviderV30 prov = GetProvider();

			NamespaceInfo ns = prov.AddNamespace("NS");

			CategoryInfo cat1 = prov.AddCategory(null, "Category");
			CategoryInfo cat2 = prov.AddCategory(ns.Name, "Category");

			PageInfo page1 = prov.AddPage(null, "Page", DateTime.Now);
			PageInfo page2 = prov.AddPage(ns.Name, "Page", DateTime.Now);

			bool done1 = prov.RebindPage(page1, new string[] { cat1.FullName });
			bool done2 = prov.RebindPage(page2, new string[] { cat2.FullName });

			CategoryInfo[] categories1 = prov.GetCategories(null);
			Assert.AreEqual(1, categories1.Length, "Wrong category count");
			Assert.AreEqual(cat1.FullName, categories1[0].FullName, "Wrong category");
			Assert.AreEqual(1, categories1[0].Pages.Length, "Wrong page count");
			Assert.AreEqual(page1.FullName, categories1[0].Pages[0], "Wrong page");

			CategoryInfo[] categories2 = prov.GetCategories(ns);
			Assert.AreEqual(1, categories2.Length, "Wrong length");
			Assert.AreEqual(cat2.FullName, categories2[0].FullName, "Wrong category");
			Assert.AreEqual(1, categories2[0].Pages.Length, "Wrong page count");
			Assert.AreEqual(page2.FullName, categories2[0].Pages[0], "Wrong page");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RebindPage_NullPage() {
			IPagesStorageProviderV30 prov = GetProvider();

			prov.RebindPage(null, new string[0]);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RebindPage_NullCategories() {
			IPagesStorageProviderV30 prov = GetProvider();

			PageInfo page = prov.AddPage(null, "Page", DateTime.Now);

			prov.RebindPage(page, null);
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void RebindPage_InvalidCategoryElement(string e) {
			IPagesStorageProviderV30 prov = GetProvider();

			CategoryInfo cat1 = prov.AddCategory(null, "Cat1");
			CategoryInfo cat2 = prov.AddCategory(null, "Cat2");

			PageInfo page = prov.AddPage(null, "Page", DateTime.Now);

			prov.RebindPage(page, new string[] { "Cat1", e });
		}

		[Test]
		public void RebindPage_InexistentCategoryElement_Root() {
			IPagesStorageProviderV30 prov = GetProvider();

			CategoryInfo cat1 = prov.AddCategory(null, "Cat1");
			CategoryInfo cat2 = prov.AddCategory(null, "Cat2");

			PageInfo page = prov.AddPage(null, "Page", DateTime.Now);

			Assert.IsFalse(prov.RebindPage(page, new string[] { "Cat1", "Cat222" }), "Rebind should return false");
		}

		[Test]
		public void RebindPage_InexistentCategoryElement_Sub() {
			IPagesStorageProviderV30 prov = GetProvider();

			NamespaceInfo ns = prov.AddNamespace("NS");

			CategoryInfo cat1 = prov.AddCategory(ns.Name, "Cat1");
			CategoryInfo cat2 = prov.AddCategory(ns.Name, "Cat2");

			PageInfo page = prov.AddPage(ns.Name, "Page", DateTime.Now);

			Assert.IsFalse(prov.RebindPage(page, new string[] { "Cat1", "Cat222" }), "Rebind should return false");
		}

		[Test]
		public void BulkStoreMessages_Root() {
			IPagesStorageProviderV30 prov = GetProvider();

			Assert.IsFalse(prov.BulkStoreMessages(new PageInfo("Inexistent", prov, DateTime.Now), new Message[0]), "BulkStoreMessages should return false");

			PageInfo page = prov.AddPage(null, "Page", DateTime.Now);

			prov.AddMessage(page, "NUnit", "Test", DateTime.Now, "Message", -1);
			prov.AddMessage(page, "NUnit", "Test-1", DateTime.Now, "Message-1", prov.GetMessages(page)[0].ID);
			prov.AddMessage(page, "NUnit", "Test400", DateTime.Now, "Message400", -1);
			prov.AddMessage(page, "NUnit", "Test500", DateTime.Now, "Message500", -1);

			DateTime dt = DateTime.Now;

			List<Message> newMessages = new List<Message>();
			newMessages.Add(new Message(1, "NUnit", "New1", dt, "Body1"));
			newMessages[0].Replies = new Message[] { new Message(2, "NUnit", "New11", dt.AddDays(2), "Body11") };
			newMessages.Add(new Message(3, "NUnit", "New2", dt.AddDays(1), "Body2"));

			Assert.IsTrue(prov.BulkStoreMessages(page, newMessages.ToArray()), "BulkStoreMessages should return true");

			List<Message> result = new List<Message>(prov.GetMessages(page));

			Assert.AreEqual(2, result.Count, "Wrong root message count");
			Assert.AreEqual(1, result[0].Replies.Length, "Wrong reply count");

			Assert.AreEqual(1, result[0].ID, "Wrong ID");
			Assert.AreEqual("NUnit", result[0].Username, "Wrong username");
			Assert.AreEqual("New1", result[0].Subject, "Wrong subject");
			Tools.AssertDateTimesAreEqual(dt, result[0].DateTime);
			Assert.AreEqual("Body1", result[0].Body, "Wrong body");

			Assert.AreEqual(2, result[0].Replies[0].ID, "Wrong ID");
			Assert.AreEqual("NUnit", result[0].Replies[0].Username, "Wrong username");
			Assert.AreEqual("New11", result[0].Replies[0].Subject, "Wrong subject");
			Tools.AssertDateTimesAreEqual(dt.AddDays(2), result[0].Replies[0].DateTime);
			Assert.AreEqual("Body11", result[0].Replies[0].Body, "Wrong body");

			Assert.AreEqual(3, result[1].ID, "Wrong ID");
			Assert.AreEqual("NUnit", result[1].Username, "Wrong username");
			Assert.AreEqual("New2", result[1].Subject, "Wrong subject");
			Tools.AssertDateTimesAreEqual(dt.AddDays(1), result[1].DateTime);
			Assert.AreEqual("Body2", result[1].Body, "Wrong body");
		}

		[Test]
		public void BulkStoreMessages_Sub() {
			IPagesStorageProviderV30 prov = GetProvider();

			NamespaceInfo ns = prov.AddNamespace("NS");

			Assert.IsFalse(prov.BulkStoreMessages(new PageInfo(NameTools.GetFullName(ns.Name, "Inexistent"),
				prov, DateTime.Now), new Message[0]), "BulkStoreMessages should return false");

			PageInfo page = prov.AddPage(ns.Name, "Page", DateTime.Now);

			prov.AddMessage(page, "NUnit", "Test", DateTime.Now, "Message", -1);
			prov.AddMessage(page, "NUnit", "Test-1", DateTime.Now, "Message-1", prov.GetMessages(page)[0].ID);
			prov.AddMessage(page, "NUnit", "Test400", DateTime.Now, "Message400", -1);
			prov.AddMessage(page, "NUnit", "Test500", DateTime.Now, "Message500", -1);

			DateTime dt = DateTime.Now;

			List<Message> newMessages = new List<Message>();
			newMessages.Add(new Message(1, "NUnit", "New1", dt, "Body1"));
			newMessages[0].Replies = new Message[] { new Message(2, "NUnit", "New11", dt.AddDays(2), "Body11") };
			newMessages.Add(new Message(3, "NUnit", "New2", dt.AddDays(1), "Body2"));

			Assert.IsTrue(prov.BulkStoreMessages(page, newMessages.ToArray()), "BulkStoreMessages should return true");

			List<Message> result = new List<Message>(prov.GetMessages(page));

			Assert.AreEqual(2, result.Count, "Wrong root message count");
			Assert.AreEqual(1, result[0].Replies.Length, "Wrong reply count");

			Assert.AreEqual(1, result[0].ID, "Wrong ID");
			Assert.AreEqual("NUnit", result[0].Username, "Wrong username");
			Assert.AreEqual("New1", result[0].Subject, "Wrong subject");
			Tools.AssertDateTimesAreEqual(dt, result[0].DateTime);
			Assert.AreEqual("Body1", result[0].Body, "Wrong body");

			Assert.AreEqual(2, result[0].Replies[0].ID, "Wrong ID");
			Assert.AreEqual("NUnit", result[0].Replies[0].Username, "Wrong username");
			Assert.AreEqual("New11", result[0].Replies[0].Subject, "Wrong subject");
			Tools.AssertDateTimesAreEqual(dt.AddDays(2), result[0].Replies[0].DateTime);
			Assert.AreEqual("Body11", result[0].Replies[0].Body, "Wrong body");

			Assert.AreEqual(3, result[1].ID, "Wrong ID");
			Assert.AreEqual("NUnit", result[1].Username, "Wrong username");
			Assert.AreEqual("New2", result[1].Subject, "Wrong subject");
			Tools.AssertDateTimesAreEqual(dt.AddDays(1), result[1].DateTime);
			Assert.AreEqual("Body2", result[1].Body, "Wrong body");
		}

		[Test]
		public void BulkStoreMessages_DuplicateID() {
			IPagesStorageProviderV30 prov = GetProvider();

			PageInfo page = prov.AddPage(null, "Page", DateTime.Now);

			DateTime dt = DateTime.Now;

			List<Message> newMessages = new List<Message>();
			newMessages.Add(new Message(1, "NUnit", "New1", dt, "Body1"));
			newMessages[0].Replies = new Message[] { new Message(1, "NUnit", "New11", dt.AddDays(2), "Body11") };
			newMessages.Add(new Message(3, "NUnit", "New2", dt.AddDays(1), "Body2"));

			Assert.IsFalse(prov.BulkStoreMessages(page, newMessages.ToArray()), "BulkStoreMessages should return false");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void BulkStoreMessages_NullPage() {
			IPagesStorageProviderV30 prov = GetProvider();
			prov.BulkStoreMessages(null, new Message[0]);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void BulkStoreMessages_NullMessages() {
			IPagesStorageProviderV30 prov = GetProvider();
			prov.BulkStoreMessages(new PageInfo("Page", prov, DateTime.Now), null);
		}

		[Test]
		public void BulkStoreMessages_PerformSearch() {
			IPagesStorageProviderV30 prov = GetProvider();

			PageInfo page = prov.AddPage(null, "Page", DateTime.Now);

			prov.AddMessage(page, "NUnit", "Blah", DateTime.Now, "Blah", -1);

			List<Message> newMessages = new List<Message>();
			newMessages.Add(new Message(1, "NUnit", "New1", DateTime.Now, "Body1"));
			newMessages[0].Replies = new Message[] { new Message(2, "NUnit", "New11", DateTime.Now, "Body11") };
			newMessages.Add(new Message(3, "NUnit", "New2", DateTime.Now, "Body2"));

			prov.BulkStoreMessages(page, newMessages.ToArray());

			SearchResultCollection result = prov.PerformSearch(new SearchParameters("new1 new11 new2 blah"));
			Assert.AreEqual(3, result.Count, "Wrong result count");
			foreach(SearchResult res in result) {
				foreach(WordInfo info in res.Matches) {
					Assert.AreNotEqual("blah", info.Text, "Invalid search macth");
				}
			}
		}

		[Test]
		public void AddMessage_GetMessages_Root() {
			IPagesStorageProviderV30 prov = GetProvider();

			PageInfo page = prov.AddPage(null, "Page", DateTime.Now);

			Assert.AreEqual(-1, prov.GetMessageCount(new PageInfo("Inexistent", prov, DateTime.Now)), "GetMessageCount should return -1");
			Assert.IsNull(prov.GetMessages(new PageInfo("Inexistent", prov, DateTime.Now)), "GetMessages should return null");

			Assert.AreEqual(0, prov.GetMessageCount(page), "Wrong initial message count");
			Assert.AreEqual(0, prov.GetMessages(page).Length, "Wrong initial message count");

			Assert.IsFalse(prov.AddMessage(new PageInfo("Inexistent", prov, DateTime.Now), "NUnit", "Subject", DateTime.Now, "Body", -1), "AddMessage should return false");

			DateTime dt = DateTime.Now;

			Assert.IsTrue(prov.AddMessage(page, "NUnit", "Subject", dt, "Body", -1), "AddMessage should return true");
			Assert.AreEqual(1, prov.GetMessageCount(page), "Wrong message count");
			Assert.IsTrue(prov.AddMessage(page, "NUnit1", "Subject1", dt.AddDays(1), "Body1", prov.GetMessages(page)[0].ID), "AddMessage should return true");
			Assert.AreEqual(2, prov.GetMessageCount(page), "Wrong message count");

			Message[] messages = prov.GetMessages(page);

			Assert.AreEqual(1, messages.Length, "Wrong message count");
			Assert.AreEqual("NUnit", messages[0].Username, "Wrong username");
			Assert.AreEqual("Subject", messages[0].Subject, "Wrong subject");
			Tools.AssertDateTimesAreEqual(dt, messages[0].DateTime);
			Assert.AreEqual("Body", messages[0].Body, "Wrong body");

			messages = messages[0].Replies;
			Assert.AreEqual(1, messages.Length, "Wrong reply count");
			Assert.AreEqual(0, messages[0].Replies.Length, "Wrong reply count");

			Assert.AreEqual("NUnit1", messages[0].Username, "Wrong username");
			Assert.AreEqual("Subject1", messages[0].Subject, "Wrong subject");
			Tools.AssertDateTimesAreEqual(dt.AddDays(1), messages[0].DateTime);
			Assert.AreEqual("Body1", messages[0].Body, "Wrong body");
		}

		[Test]
		public void AddMessage_GetMessages_Sub() {
			IPagesStorageProviderV30 prov = GetProvider();

			NamespaceInfo ns = prov.AddNamespace("NS");

			PageInfo page = prov.AddPage(ns.Name, "Page", DateTime.Now);

			Assert.AreEqual(-1, prov.GetMessageCount(new PageInfo(NameTools.GetFullName(ns.Name, "Inexistent"),
				prov, DateTime.Now)), "GetMessageCount should return -1");

			Assert.IsNull(prov.GetMessages(new PageInfo(NameTools.GetFullName(ns.Name, "Inexistent"),
				prov, DateTime.Now)), "GetMessages should return null");

			Assert.AreEqual(0, prov.GetMessageCount(page), "Wrong initial message count");
			Assert.AreEqual(0, prov.GetMessages(page).Length, "Wrong initial message count");

			Assert.IsFalse(prov.AddMessage(new PageInfo(NameTools.GetFullName(ns.Name, "Inexistent"),
				prov, DateTime.Now), "NUnit", "Subject", DateTime.Now, "Body", -1), "AddMessage should return false");

			DateTime dt = DateTime.Now;

			Assert.IsTrue(prov.AddMessage(page, "NUnit", "Subject", dt, "Body", -1), "AddMessage should return true");
			Assert.AreEqual(1, prov.GetMessageCount(page), "Wrong message count");
			Assert.IsTrue(prov.AddMessage(page, "NUnit1", "Subject1", dt.AddDays(1), "Body1", prov.GetMessages(page)[0].ID), "AddMessage should return true");
			Assert.AreEqual(2, prov.GetMessageCount(page), "Wrong message count");

			Message[] messages = prov.GetMessages(page);

			Assert.AreEqual(1, messages.Length, "Wrong message count");
			Assert.AreEqual("NUnit", messages[0].Username, "Wrong username");
			Assert.AreEqual("Subject", messages[0].Subject, "Wrong subject");
			Tools.AssertDateTimesAreEqual(dt, messages[0].DateTime);
			Assert.AreEqual("Body", messages[0].Body, "Wrong body");

			messages = messages[0].Replies;
			Assert.AreEqual(1, messages.Length, "Wrong reply count");
			Assert.AreEqual(0, messages[0].Replies.Length, "Wrong reply count");

			Assert.AreEqual("NUnit1", messages[0].Username, "Wrong username");
			Assert.AreEqual("Subject1", messages[0].Subject, "Wrong subject");
			Tools.AssertDateTimesAreEqual(dt.AddDays(1), messages[0].DateTime);
			Assert.AreEqual("Body1", messages[0].Body, "Wrong body");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void GetMessages_NullPage() {
			IPagesStorageProviderV30 prov = GetProvider();

			prov.GetMessages(null);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void GetMessageCount_NullPage() {
			IPagesStorageProviderV30 prov = GetProvider();

			prov.GetMessageCount(null);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void AddMessage_NullPage() {
			IPagesStorageProviderV30 prov = GetProvider();

			prov.AddMessage(null, "NUnit", "Subject", DateTime.Now, "Body", -1);
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void AddMessage_InvalidUsername(string u) {
			IPagesStorageProviderV30 prov = GetProvider();

			PageInfo page = prov.AddPage(null, "Page", DateTime.Now);

			prov.AddMessage(page, u, "Subject", DateTime.Now, "Body", -1);
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void AddMessage_InvalidSubject(string s) {
			IPagesStorageProviderV30 prov = GetProvider();

			PageInfo page = prov.AddPage(null, "Page", DateTime.Now);

			prov.AddMessage(page, "NUnit", s, DateTime.Now, "Body", -1);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void AddMessage_NullBody() {
			IPagesStorageProviderV30 prov = GetProvider();

			PageInfo page = prov.AddPage(null, "Page", DateTime.Now);

			prov.AddMessage(page, "NUnit", "Subject", DateTime.Now, null, -1);
		}

		[Test]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void AddMessage_InvalidParent() {
			IPagesStorageProviderV30 prov = GetProvider();

			PageInfo page = prov.AddPage(null, "Page", DateTime.Now);

			prov.AddMessage(page, "NUnit", "Subject", DateTime.Now, "Body", -2);
		}

		[Test]
		public void AddMessage_InexistentParent() {
			IPagesStorageProviderV30 prov = GetProvider();

			PageInfo page = prov.AddPage(null, "Page", DateTime.Now);

			Assert.IsFalse(prov.AddMessage(page, "NUnit", "Subject", DateTime.Now, "Body", 5), "AddMessage should return false");
		}

		[Test]
		public void AddMessage_PerformSearch() {
			IPagesStorageProviderV30 prov = GetProvider();

			PageInfo page = prov.AddPage(null, "Page", DateTime.Now);

			prov.AddMessage(page, "NUnit", "Message", DateTime.Now, "Blah, Test.", -1);
			prov.AddMessage(page, "NUnit", "Re: Message2", DateTime.Now, "Dummy.", prov.GetMessages(page)[0].ID);

			SearchResultCollection result = prov.PerformSearch(new SearchParameters("dummy message"));
			Assert.AreEqual(2, result.Count, "Wrong result count");

			bool found1 = false, found2 = false;
			foreach(SearchResult res in result) {
				Assert.AreEqual(MessageDocument.StandardTypeTag, res.Document.TypeTag, "Wrong type tag");
				if(res.Matches[0].Text == "dummy") found1 = true;
				if(res.Matches[0].Text == "message") found2 = true;
			}

			Assert.IsTrue(found1, "First word not found");
			Assert.IsTrue(found2, "Second word not found");
		}

		[Test]
		public void RemoveMessage_Root() {
			IPagesStorageProviderV30 prov = GetProvider();

			PageInfo page = prov.AddPage(null, "Page", DateTime.Now);

			// Subject0
			//    Subject00
			// Subject1
			//    Subject11

			prov.AddMessage(page, "NUnit0", "Subject0", DateTime.Now, "Body0", -1);
			prov.AddMessage(page, "NUnit00", "Subject00", DateTime.Now.AddHours(1), "Body00", prov.GetMessages(page)[0].ID);
			prov.AddMessage(page, "NUnit1", "Subject1", DateTime.Now.AddHours(2), "Body1", -1);
			prov.AddMessage(page, "NUnit11", "Subject11", DateTime.Now.AddHours(3), "Body11", prov.GetMessages(page)[1].ID);

			Message[] messages = prov.GetMessages(page);

			Assert.IsFalse(prov.RemoveMessage(page, 5, true), "RemoveMessage should return false");

			Assert.IsFalse(prov.RemoveMessage(new PageInfo("Inexistent", prov, DateTime.Now), 1, true),
				"RemoveMessage should return false");

			Assert.IsTrue(prov.RemoveMessage(page, messages[0].ID, false), "RemoveMessage should return true");

			Assert.AreEqual(3, prov.GetMessageCount(page), "Wrong message count");
			Assert.AreEqual("Subject00", prov.GetMessages(page)[0].Subject, "Wrong message");
			Assert.AreEqual("Subject1", prov.GetMessages(page)[1].Subject, "Wrong message");

			Assert.IsTrue(prov.RemoveMessage(page, messages[1].ID, true), "RemoveMessages should return true");
			Assert.AreEqual(1, prov.GetMessageCount(page), "Wrong message count");
			Assert.AreEqual("Subject00", prov.GetMessages(page)[0].Subject, "Wrong message");
		}

		[Test]
		public void RemoveMessage_Sub() {
			IPagesStorageProviderV30 prov = GetProvider();

			NamespaceInfo ns = prov.AddNamespace("NS");

			PageInfo page = prov.AddPage(ns.Name, "Page", DateTime.Now);

			// Subject0
			//    Subject00
			// Subject1
			//    Subject11

			prov.AddMessage(page, "NUnit0", "Subject0", DateTime.Now, "Body0", -1);
			prov.AddMessage(page, "NUnit00", "Subject00", DateTime.Now.AddHours(1), "Body00", prov.GetMessages(page)[0].ID);
			prov.AddMessage(page, "NUnit1", "Subject1", DateTime.Now.AddHours(2), "Body1", -1);
			prov.AddMessage(page, "NUnit11", "Subject11", DateTime.Now.AddHours(3), "Body11", prov.GetMessages(page)[1].ID);

			Message[] messages = prov.GetMessages(page);

			Assert.IsFalse(prov.RemoveMessage(page, 5, true), "RemoveMessage should return false");
			Assert.IsFalse(prov.RemoveMessage(new PageInfo(NameTools.GetFullName(ns.Name, "Inexistent"),
				prov, DateTime.Now), 1, true), "RemoveMessage should return false");

			Assert.IsTrue(prov.RemoveMessage(page, messages[0].ID, false), "RemoveMessage should return true");

			Assert.AreEqual(3, prov.GetMessageCount(page), "Wrong message count");
			Assert.AreEqual("Subject00", prov.GetMessages(page)[0].Subject, "Wrong message");
			Assert.AreEqual("Subject1", prov.GetMessages(page)[1].Subject, "Wrong message");

			Assert.IsTrue(prov.RemoveMessage(page, messages[1].ID, true), "RemoveMessages should return true");
			Assert.AreEqual(1, prov.GetMessageCount(page), "Wrong message count");
			Assert.AreEqual("Subject00", prov.GetMessages(page)[0].Subject, "Wrong message");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RemoveMessage_NullPage() {
			IPagesStorageProviderV30 prov = GetProvider();

			prov.RemoveMessage(null, 1, true);
		}

		[Test]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void RemoveMessage_InvalidId() {
			IPagesStorageProviderV30 prov = GetProvider();
			
			PageInfo page = prov.AddPage(null, "Page", DateTime.Now);

			prov.RemoveMessage(page, -1, true);
		}

		[Test]
		public void RemoveMessage_KeepReplies_PerformSearch() {
			IPagesStorageProviderV30 prov = GetProvider();

			PageInfo page = prov.AddPage(null, "Page", DateTime.Now);
			prov.AddMessage(page, "NUnit", "Test", DateTime.Now, "Blah", -1);
			prov.AddMessage(page, "NUnit", "RE: Test2", DateTime.Now, "Blah2", prov.GetMessages(page)[0].ID);

			prov.RemoveMessage(page, prov.GetMessages(page)[0].ID, false);

			SearchResultCollection result = prov.PerformSearch(new SearchParameters("test blah test2 blah2"));
			Assert.AreEqual(1, result.Count, "Wrong result count");
			Assert.AreEqual(2, result[0].Matches.Count, "Wrong match count");

			bool found1 = false, found2 = false;
			foreach(WordInfo info in result[0].Matches) {
				if(info.Text == "test2") found1 = true;
				if(info.Text == "blah2") found2 = true;
			}

			Assert.IsTrue(found1, "First word not found");
			Assert.IsTrue(found2, "Second word not found");
		}

		[Test]
		public void RemoveMessage_RemoveReplies_PerformSearch() {
			IPagesStorageProviderV30 prov = GetProvider();

			PageInfo page = prov.AddPage(null, "Page", DateTime.Now);
			prov.AddMessage(page, "NUnit", "Test", DateTime.Now, "Blah", -1);
			prov.AddMessage(page, "NUnit", "RE: Test2", DateTime.Now, "Blah2", prov.GetMessages(page)[0].ID);

			prov.RemoveMessage(page, prov.GetMessages(page)[0].ID, true);

			SearchResultCollection result = prov.PerformSearch(new SearchParameters("test blah test2 blah2"));
			Assert.AreEqual(0, result.Count, "Wrong result count");
		}

		[Test]
		public void ModifyMessage_Root() {
			IPagesStorageProviderV30 prov = GetProvider();

			PageInfo page = prov.AddPage(null, "Page", DateTime.Now);

			DateTime dt = DateTime.Now;

			prov.AddMessage(page, "NUnit", "Subject", dt, "Body", -1);
			prov.AddMessage(page, "NUnit", "Subject", dt, "Body", -1);

			Assert.IsFalse(prov.ModifyMessage(new PageInfo("Inexistent", prov, DateTime.Now), 1,
				"NUnit", "Subject", DateTime.Now, "Body"), "ModifyMessage should return false");

			Assert.IsFalse(prov.ModifyMessage(page, 5, "NUnit", "Subject", DateTime.Now, "Body"), "ModifyMessage should return false");

			Assert.IsTrue(prov.ModifyMessage(page, prov.GetMessages(page)[0].ID, "NUnit1", "Subject1", dt.AddDays(1), "Body1"), "ModifyMessage should return true");

			Assert.AreEqual(2, prov.GetMessageCount(page), "Wrong message count");

			Message[] messages = prov.GetMessages(page);

			Assert.AreEqual("NUnit", messages[0].Username, "Wrong username");
			Assert.AreEqual("Subject", messages[0].Subject, "Wrong subject");
			Tools.AssertDateTimesAreEqual(dt, messages[0].DateTime);
			Assert.AreEqual("Body", messages[0].Body, "Wrong body");

			Assert.AreEqual("NUnit1", messages[1].Username, "Wrong username");
			Assert.AreEqual("Subject1", messages[1].Subject, "Wrong subject");
			Tools.AssertDateTimesAreEqual(dt.AddDays(1), messages[1].DateTime);
			Assert.AreEqual("Body1", messages[1].Body, "Wrong body");
		}

		[Test]
		public void ModifyMessage_Sub() {
			IPagesStorageProviderV30 prov = GetProvider();

			NamespaceInfo ns = prov.AddNamespace("NS");

			PageInfo page = prov.AddPage(ns.Name, "Page", DateTime.Now);

			DateTime dt = DateTime.Now;

			prov.AddMessage(page, "NUnit", "Subject", dt, "Body", -1);
			prov.AddMessage(page, "NUnit", "Subject", dt, "Body", -1);

			Assert.IsFalse(prov.ModifyMessage(new PageInfo(NameTools.GetFullName(ns.Name, "Inexistent"),
				prov, DateTime.Now), 1, "NUnit", "Subject", DateTime.Now, "Body"), "ModifyMessage should return false");

			Assert.IsFalse(prov.ModifyMessage(page, 5, "NUnit", "Subject", DateTime.Now, "Body"), "ModifyMessage should return false");

			Assert.IsTrue(prov.ModifyMessage(page, prov.GetMessages(page)[0].ID, "NUnit1", "Subject1", dt.AddDays(1), "Body1"), "ModifyMessage should return true");

			Assert.AreEqual(2, prov.GetMessageCount(page), "Wrong message count");

			Message[] messages = prov.GetMessages(page);

			Assert.AreEqual("NUnit", messages[0].Username, "Wrong username");
			Assert.AreEqual("Subject", messages[0].Subject, "Wrong subject");
			Tools.AssertDateTimesAreEqual(dt, messages[0].DateTime);
			Assert.AreEqual("Body", messages[0].Body, "Wrong body");

			Assert.AreEqual("NUnit1", messages[1].Username, "Wrong username");
			Assert.AreEqual("Subject1", messages[1].Subject, "Wrong subject");
			Tools.AssertDateTimesAreEqual(dt.AddDays(1), messages[1].DateTime);
			Assert.AreEqual("Body1", messages[1].Body, "Wrong body");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void ModifyMessage_NullPage() {
			IPagesStorageProviderV30 prov = GetProvider();

			Assert.IsFalse(prov.ModifyMessage(null, 1, "NUnit", "Subject", DateTime.Now, "Body"), "ModifyMessage should return false");
		}

		[Test]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void ModifyMessage_InvalidId() {
			IPagesStorageProviderV30 prov = GetProvider();

			PageInfo page = prov.AddPage(null, "Page", DateTime.Now);

			Assert.IsFalse(prov.ModifyMessage(page, -1, "NUnit", "Subject", DateTime.Now, "Body"), "ModifyMessage should return false");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void ModifyMessage_InvalidUsername(string u) {
			IPagesStorageProviderV30 prov = GetProvider();

			PageInfo page = prov.AddPage(null, "Page", DateTime.Now);
			prov.AddMessage(page, "NUnit", "Subject", DateTime.Now, "Body", -1);

			Assert.IsFalse(prov.ModifyMessage(page, prov.GetMessages(page)[0].ID, u, "Subject", DateTime.Now, "Body"), "ModifyMessage should return false");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void ModifyMessage_InvalidSubject(string s) {
			IPagesStorageProviderV30 prov = GetProvider();

			PageInfo page = prov.AddPage(null, "Page", DateTime.Now);
			prov.AddMessage(page, "NUnit", "Subject", DateTime.Now, "Body", -1);

			Assert.IsFalse(prov.ModifyMessage(page, prov.GetMessages(page)[0].ID, "NUnit", s, DateTime.Now, "Body"), "ModifyMessage should return false");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void ModifyMessage_NullBody() {
			IPagesStorageProviderV30 prov = GetProvider();

			PageInfo page = prov.AddPage(null, "Page", DateTime.Now);
			prov.AddMessage(page, "NUnit", "Subject", DateTime.Now, "Body", -1);

			Assert.IsFalse(prov.ModifyMessage(page, prov.GetMessages(page)[0].ID, "NUnit", "Subject", DateTime.Now, null), "ModifyMessage should return false");
		}

		[Test]
		public void ModifyMessage_PerformSearch() {
			IPagesStorageProviderV30 prov = GetProvider();

			PageInfo page = prov.AddPage(null, "Page", DateTime.Now);

			prov.AddMessage(page, "NUnit", "Message", DateTime.Now, "Blah, Test.", -1);
			prov.ModifyMessage(page, prov.GetMessages(page)[0].ID, "NUnit", "MessageMod", DateTime.Now, "Modified");

			SearchResultCollection result = prov.PerformSearch(new SearchParameters("message modified"));
			Assert.AreEqual(1, result.Count, "Wrong result count");

			Assert.AreEqual(1, result[0].Matches.Count, "Wrong match count");
			Assert.AreEqual("modified", result[0].Matches[0].Text, "Wrong match");
		}

		[Test]
		public void AddNavigationPath_GetNavigationPaths_Root() {
			IPagesStorageProviderV30 prov = GetProvider();

			PageInfo page1 = prov.AddPage(null, "Page1", DateTime.Now);
			PageInfo page2 = prov.AddPage(null, "Page2", DateTime.Now);

			Assert.AreEqual(0, prov.GetNavigationPaths(null).Length, "Wrong initial navigation path count");

			NavigationPath path1 = prov.AddNavigationPath(null, "Path1", new PageInfo[] { page1, page2 });
			Assert.IsNotNull(path1, "AddNavigationPath should return something");
			Assert.AreEqual("Path1", path1.FullName, "Wrong name");
			Assert.AreEqual(2, path1.Pages.Length, "Wrong page count");
			Assert.AreEqual(page1.FullName, path1.Pages[0], "Wrong page at position 0");
			Assert.AreEqual(page2.FullName, path1.Pages[1], "Wrong page at position 1");

			NavigationPath path2 = prov.AddNavigationPath(null, "Path2", new PageInfo[] { page1 });
			Assert.IsNotNull(path2, "AddNavigationPath should return something");
			Assert.AreEqual("Path2", path2.FullName, "Wrong name");
			Assert.AreEqual(1, path2.Pages.Length, "Wrong page count");
			Assert.AreEqual(page1.FullName, path2.Pages[0], "Wrong page at position 0");

			Assert.IsNull(prov.AddNavigationPath(null, "Path1", new PageInfo[] { page2, page1 }), "AddNavigationPath should return null");

			NavigationPath[] paths = prov.GetNavigationPaths(null);
			Assert.AreEqual(2, paths.Length, "Wrong navigation path count");
			Assert.AreEqual("Path1", paths[0].FullName, "Wrong name");
			Assert.AreEqual(2, paths[0].Pages.Length, "Wrong page count");
			Assert.AreEqual(page1.FullName, paths[0].Pages[0], "Wrong page at position 0");
			Assert.AreEqual(page2.FullName, paths[0].Pages[1], "Wrong page at position 1");
			Assert.AreEqual("Path2", paths[1].FullName, "Wrong name");
			Assert.AreEqual(1, paths[1].Pages.Length, "Wrong page count");
			Assert.AreEqual(page1.FullName, paths[1].Pages[0], "Wrong page at position 0");
		}

		[Test]
		public void AddNavigationPath_GetNavigationPaths_Sub() {
			IPagesStorageProviderV30 prov = GetProvider();

			NamespaceInfo ns = prov.AddNamespace("NS");

			PageInfo page1 = prov.AddPage(ns.Name, "Page1", DateTime.Now);
			PageInfo page2 = prov.AddPage(ns.Name, "Page2", DateTime.Now);

			Assert.AreEqual(0, prov.GetNavigationPaths(ns).Length, "Wrong initial navigation path count");

			NavigationPath path1 = prov.AddNavigationPath(ns.Name, "Path1", new PageInfo[] { page1, page2 });
			Assert.IsNotNull(path1, "AddNavigationPath should return something");
			Assert.AreEqual(NameTools.GetFullName(ns.Name, "Path1"), path1.FullName, "Wrong name");
			Assert.AreEqual(2, path1.Pages.Length, "Wrong page count");
			Assert.AreEqual(page1.FullName, path1.Pages[0], "Wrong page at position 0");
			Assert.AreEqual(page2.FullName, path1.Pages[1], "Wrong page at position 1");

			NavigationPath path2 = prov.AddNavigationPath(ns.Name, "Path2", new PageInfo[] { page1 });
			Assert.IsNotNull(path2, "AddNavigationPath should return something");
			Assert.AreEqual(NameTools.GetFullName(ns.Name, "Path2"), path2.FullName, "Wrong name");
			Assert.AreEqual(1, path2.Pages.Length, "Wrong page count");
			Assert.AreEqual(page1.FullName, path2.Pages[0], "Wrong page at position 0");

			Assert.IsNull(prov.AddNavigationPath(ns.Name, "Path1", new PageInfo[] { page2, page1 }), "AddNavigationPath should return null");

			NavigationPath[] paths = prov.GetNavigationPaths(ns);
			Assert.AreEqual(2, paths.Length, "Wrong navigation path count");
			Assert.AreEqual(NameTools.GetFullName(ns.Name, "Path1"), paths[0].FullName, "Wrong name");
			Assert.AreEqual(2, paths[0].Pages.Length, "Wrong page count");
			Assert.AreEqual(page1.FullName, paths[0].Pages[0], "Wrong page at position 0");
			Assert.AreEqual(page2.FullName, paths[0].Pages[1], "Wrong page at position 1");
			Assert.AreEqual(NameTools.GetFullName(ns.Name, "Path2"), paths[1].FullName, "Wrong name");
			Assert.AreEqual(1, paths[1].Pages.Length, "Wrong page count");
			Assert.AreEqual(page1.FullName, paths[1].Pages[0], "Wrong page at position 0");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void AddNavigationPath_InvalidName(string n) {
			IPagesStorageProviderV30 prov = GetProvider();

			PageInfo page1 = prov.AddPage(null, "Page1", DateTime.Now);
			PageInfo page2 = prov.AddPage(null, "Page2", DateTime.Now);

			prov.AddNavigationPath(null, n, new PageInfo[] { page1, page2 });
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void AddNavigationPath_NullPages() {
			IPagesStorageProviderV30 prov = GetProvider();

			prov.AddNavigationPath(null, "Path", null);
		}

		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void AddNavigationPath_EmptyPages() {
			IPagesStorageProviderV30 prov = GetProvider();

			prov.AddNavigationPath(null, "Path", new PageInfo[0]);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void AddNavigationPath_NullPageElement() {
			IPagesStorageProviderV30 prov = GetProvider();

			PageInfo page1 = prov.AddPage(null, "Page1", DateTime.Now);

			prov.AddNavigationPath(null, "Path", new PageInfo[] { page1, null });
		}

		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void AddNavigationPath_InexistentPageElement() {
			IPagesStorageProviderV30 prov = GetProvider();

			PageInfo page1 = prov.AddPage(null, "Page1", DateTime.Now);

			prov.AddNavigationPath(null, "Path", new PageInfo[] { page1, new PageInfo("Inexistent", prov, DateTime.Now) });
		}

		[Test]
		public void ModifyNavigationPath_Root() {
			IPagesStorageProviderV30 prov = GetProvider();

			PageInfo page1 = prov.AddPage(null, "Page1", DateTime.Now);
			PageInfo page2 = prov.AddPage(null, "Page2", DateTime.Now);
			PageInfo page3 = prov.AddPage(null, "Page3", DateTime.Now);

			NavigationPath path = prov.AddNavigationPath(null, "Path", new PageInfo[] { page1, page2 });

			Assert.IsNull(prov.ModifyNavigationPath(new NavigationPath("Inexistent", prov), new PageInfo[] { page1, page3 }), "ModifyNavigationPath should return null");

			NavigationPath output = prov.ModifyNavigationPath(path, new PageInfo[] { page1, page3 });

			Assert.IsNotNull(output, "ModifyNavigationPath should return something");
			Assert.AreEqual("Path", path.FullName, "Wrong name");
			Assert.AreEqual(page1.FullName, output.Pages[0], "Wrong page at position 0");
			Assert.AreEqual(page3.FullName, output.Pages[1], "Wrong page at position 1");

			NavigationPath[] paths = prov.GetNavigationPaths(null);
			Assert.AreEqual(1, paths.Length, "Wrong navigation path count");
			Assert.AreEqual("Path", paths[0].FullName, "Wrong name");
			Assert.AreEqual(page1.FullName, paths[0].Pages[0], "Wrong page at position 0");
			Assert.AreEqual(page3.FullName, paths[0].Pages[1], "Wrong page at position 1");
		}

		[Test]
		public void ModifyNavigationPath_Sub() {
			IPagesStorageProviderV30 prov = GetProvider();

			NamespaceInfo ns = prov.AddNamespace("NS");

			PageInfo page1 = prov.AddPage(ns.Name, "Page1", DateTime.Now);
			PageInfo page2 = prov.AddPage(ns.Name, "Page2", DateTime.Now);
			PageInfo page3 = prov.AddPage(ns.Name, "Page3", DateTime.Now);

			NavigationPath path = prov.AddNavigationPath(ns.Name, "Path", new PageInfo[] { page1, page2 });

			Assert.IsNull(prov.ModifyNavigationPath(new NavigationPath(NameTools.GetFullName(ns.Name, "Inexistent"), prov), new PageInfo[] { page1, page3 }), "ModifyNavigationPath should return null");

			NavigationPath output = prov.ModifyNavigationPath(path, new PageInfo[] { page1, page3 });

			Assert.IsNotNull(output, "ModifyNavigationPath should return something");
			Assert.AreEqual(NameTools.GetFullName(ns.Name, "Path"), path.FullName, "Wrong name");
			Assert.AreEqual(page1.FullName, output.Pages[0], "Wrong page at position 0");
			Assert.AreEqual(page3.FullName, output.Pages[1], "Wrong page at position 1");

			NavigationPath[] paths = prov.GetNavigationPaths(ns);
			Assert.AreEqual(1, paths.Length, "Wrong navigation path count");
			Assert.AreEqual(NameTools.GetFullName(ns.Name, "Path"), paths[0].FullName, "Wrong name");
			Assert.AreEqual(page1.FullName, paths[0].Pages[0], "Wrong page at position 0");
			Assert.AreEqual(page3.FullName, paths[0].Pages[1], "Wrong page at position 1");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void ModifyNavigationPath_NullPath() {
			IPagesStorageProviderV30 prov = GetProvider();

			PageInfo page1 = prov.AddPage(null, "Page1", DateTime.Now);
			PageInfo page2 = prov.AddPage(null, "Page2", DateTime.Now);

			prov.ModifyNavigationPath(null, new PageInfo[] { page1, page2 });
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void ModifyNavigationPath_NullPages() {
			IPagesStorageProviderV30 prov = GetProvider();

			PageInfo page1 = prov.AddPage(null, "Page1", DateTime.Now);
			PageInfo page2 = prov.AddPage(null, "Page2", DateTime.Now);

			NavigationPath path = prov.AddNavigationPath(null, "Path", new PageInfo[] { page1, page2 });

			NavigationPath output = prov.ModifyNavigationPath(path, null);
		}

		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void ModifyNavigationPath_EmptyPages() {
			IPagesStorageProviderV30 prov = GetProvider();

			PageInfo page1 = prov.AddPage(null, "Page1", DateTime.Now);
			PageInfo page2 = prov.AddPage(null, "Page2", DateTime.Now);

			NavigationPath path = prov.AddNavigationPath(null, "Path", new PageInfo[] { page1, page2 });

			NavigationPath output = prov.ModifyNavigationPath(path, new PageInfo[0]);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void ModifyNavigationPath_NullPageElement() {
			IPagesStorageProviderV30 prov = GetProvider();

			PageInfo page1 = prov.AddPage(null, "Page1", DateTime.Now);
			PageInfo page2 = prov.AddPage(null, "Page2", DateTime.Now);

			NavigationPath path = prov.AddNavigationPath(null, "Path", new PageInfo[] { page1, page2 });

			NavigationPath output = prov.ModifyNavigationPath(path, new PageInfo[] { page1, null });
		}

		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void ModifyNavigationPath_InexistentPageElement() {
			IPagesStorageProviderV30 prov = GetProvider();

			PageInfo page1 = prov.AddPage(null, "Page1", DateTime.Now);
			PageInfo page2 = prov.AddPage(null, "Page2", DateTime.Now);

			NavigationPath path = prov.AddNavigationPath(null, "Path", new PageInfo[] { page1, page2 });

			NavigationPath output = prov.ModifyNavigationPath(path,
				new PageInfo[] { page1, new PageInfo("Inexistent", prov, DateTime.Now) });
		}

		[Test]
		public void RemoveNavigationPath_Root() {
			IPagesStorageProviderV30 prov = GetProvider();

			PageInfo page1 = prov.AddPage(null, "Page1", DateTime.Now);
			PageInfo page2 = prov.AddPage(null, "Page2", DateTime.Now);

			NavigationPath path1 = prov.AddNavigationPath(null, "Path1", new PageInfo[] { page1, page2 });
			NavigationPath path2 = prov.AddNavigationPath(null, "Path2", new PageInfo[] { page2, page1 });

			Assert.IsFalse(prov.RemoveNavigationPath(new NavigationPath("Inexistent", prov)), "RemoveNavigationPath should return false");

			Assert.IsTrue(prov.RemoveNavigationPath(path2), "RemoveNavigationPath should return true");

			NavigationPath[] paths = prov.GetNavigationPaths(null);
			Assert.AreEqual(1, paths.Length, "Wrong navigation path count");
			Assert.AreEqual("Path1", paths[0].FullName, "Wrong name");
			Assert.AreEqual(page1.FullName, paths[0].Pages[0], "Wrong page at position 0");
			Assert.AreEqual(page2.FullName, paths[0].Pages[1], "Wrong page at position 1");
		}

		[Test]
		public void RemoveNavigationPath_Sub() {
			IPagesStorageProviderV30 prov = GetProvider();

			NamespaceInfo ns = prov.AddNamespace("NS");

			PageInfo page1 = prov.AddPage(ns.Name, "Page1", DateTime.Now);
			PageInfo page2 = prov.AddPage(ns.Name, "Page2", DateTime.Now);

			NavigationPath path1 = prov.AddNavigationPath(ns.Name, "Path1", new PageInfo[] { page1, page2 });
			NavigationPath path2 = prov.AddNavigationPath(ns.Name, "Path2", new PageInfo[] { page2, page1 });

			Assert.IsFalse(prov.RemoveNavigationPath(new NavigationPath(NameTools.GetFullName(ns.Name, "Inexistent"), prov)), "RemoveNavigationPath should return false");

			Assert.IsTrue(prov.RemoveNavigationPath(path2), "RemoveNavigationPath should return true");

			NavigationPath[] paths = prov.GetNavigationPaths(ns);
			Assert.AreEqual(1, paths.Length, "Wrong navigation path count");
			Assert.AreEqual(NameTools.GetFullName(ns.Name, "Path1"), paths[0].FullName, "Wrong name");
			Assert.AreEqual(page1.FullName, paths[0].Pages[0], "Wrong page at position 0");
			Assert.AreEqual(page2.FullName, paths[0].Pages[1], "Wrong page at position 1");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RemoveNavigationPath_NullPath() {
			IPagesStorageProviderV30 prov = GetProvider();

			prov.RemoveNavigationPath(null);
		}

		[Test]
		public void AddSnippet_GetSnippets() {
			IPagesStorageProviderV30 prov = GetProvider();

			Assert.AreEqual(0, prov.GetSnippets().Length, "Wrong snippet count");

			Snippet snippet1 = prov.AddSnippet("Snippet1", "Content1");
			Snippet snippet2 = prov.AddSnippet("Snippet2", "Content2");

			Assert.IsNull(prov.AddSnippet("Snippet1", "Content"), "AddSnippet should return null");

			Assert.AreEqual("Snippet1", snippet1.Name, "Wrong name");
			Assert.AreEqual("Content1", snippet1.Content, "Wrong content");
			Assert.AreEqual("Snippet2", snippet2.Name, "Wrong name");
			Assert.AreEqual("Content2", snippet2.Content, "Wrong content");

			Snippet[] snippets = prov.GetSnippets();
			Assert.AreEqual(2, snippets.Length, "Wrong snippet count");

			Array.Sort(snippets, delegate(Snippet x, Snippet y) { return x.Name.CompareTo(y.Name); });
			Assert.AreEqual("Snippet1", snippets[0].Name, "Wrong name");
			Assert.AreEqual("Content1", snippets[0].Content, "Wrong content");
			Assert.AreEqual("Snippet2", snippets[1].Name, "Wrong name");
			Assert.AreEqual("Content2", snippets[1].Content, "Wrong content");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void AddSnippet_InvalidName(string n) {
			IPagesStorageProviderV30 prov = GetProvider();

			prov.AddSnippet(n, "Content");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void AddSnippet_NullContent() {
			IPagesStorageProviderV30 prov = GetProvider();

			prov.AddSnippet("Snippet", null);
		}

		[Test]
		public void ModifySnippet() {
			IPagesStorageProviderV30 prov = GetProvider();

			Assert.IsNull(prov.ModifySnippet("Inexistent", "Content"), "ModifySnippet should return null");

			prov.AddSnippet("Snippet1", "Content");
			prov.AddSnippet("Snippet2", "Content2");

			Snippet output = prov.ModifySnippet("Snippet1", "Content1");
			Assert.IsNotNull(output, "ModifySnippet should return something");

			Assert.AreEqual("Snippet1", output.Name, "Wrong name");
			Assert.AreEqual("Content1", output.Content, "Wrong content");

			Snippet[] snippets = prov.GetSnippets();
			Assert.AreEqual(2, snippets.Length, "Wrong snippet count");

			Array.Sort(snippets, delegate(Snippet x, Snippet y) { return x.Name.CompareTo(y.Name); });
			Assert.AreEqual("Snippet1", snippets[0].Name, "Wrong name");
			Assert.AreEqual("Content1", snippets[0].Content, "Wrong content");
			Assert.AreEqual("Snippet2", snippets[1].Name, "Wrong name");
			Assert.AreEqual("Content2", snippets[1].Content, "Wrong content");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void ModifySnippet_InvalidName(string n) {
			IPagesStorageProviderV30 prov = GetProvider();

			prov.ModifySnippet(n, "Content");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void ModifySnippet_NullContent() {
			IPagesStorageProviderV30 prov = GetProvider();

			prov.AddSnippet("Snippet", "Blah");

			prov.AddSnippet("Snippet", null);
		}

		[Test]
		public void RemoveSnippet() {
			IPagesStorageProviderV30 prov = GetProvider();

			Assert.IsFalse(prov.RemoveSnippet("Inexistent"), "RemoveSnippet should return false");

			prov.AddSnippet("Snippet1", "Content1");
			prov.AddSnippet("Snippet2", "Content2");

			Assert.IsTrue(prov.RemoveSnippet("Snippet2"), "RemoveSnippet should return true");

			Snippet[] snippets = prov.GetSnippets();
			Assert.AreEqual(1, snippets.Length, "Wrong snippet count");

			Assert.AreEqual("Snippet1", snippets[0].Name, "Wrong name");
			Assert.AreEqual("Content1", snippets[0].Content, "Wrong content");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void RemoveSnippet_InvalidName(string n) {
			IPagesStorageProviderV30 prov = GetProvider();

			prov.RemoveSnippet(n);
		}

		[Test]
		public void AddContentTemplate_GetContentTemplates() {
			IPagesStorageProviderV30 prov = GetProvider();

			ContentTemplate temp1 = prov.AddContentTemplate("T1", "Template1");
			Assert.AreEqual("T1", temp1.Name, "Wrong name");
			Assert.AreEqual("Template1", temp1.Content, "Wrong content");

			Assert.IsNull(prov.AddContentTemplate("T1", "Blah"), "AddContentTemplate should return null");

			ContentTemplate temp2 = prov.AddContentTemplate("T2", "Template2");
			Assert.AreEqual("T2", temp2.Name, "Wrong name");
			Assert.AreEqual("Template2", temp2.Content, "Wrong content");

			ContentTemplate[] templates = prov.GetContentTemplates();
			Assert.AreEqual(2, templates.Length, "Wrong template count");

			Array.Sort(templates, (x, y) => { return x.Name.CompareTo(y.Name); });

			Assert.AreEqual("T1", templates[0].Name, "Wrong name");
			Assert.AreEqual("Template1", templates[0].Content, "Wrong content");
			Assert.AreEqual("T2", templates[1].Name, "Wrong name");
			Assert.AreEqual("Template2", templates[1].Content, "Wrong content");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void AddContentTemplate_InvalidName(string n) {
			IPagesStorageProviderV30 prov = GetProvider();

			prov.AddContentTemplate(n, "Content");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void AddContentTemplate_NullContent() {
			IPagesStorageProviderV30 prov = GetProvider();

			prov.AddContentTemplate("T", null);
		}

		[Test]
		public void ModifyContentTemplate() {
			IPagesStorageProviderV30 prov = GetProvider();

			Assert.IsNull(prov.ModifyContentTemplate("T", "Content"), "ModifyContentTemplate should return null");

			prov.AddContentTemplate("T", "Content");
			prov.AddContentTemplate("T2", "Blah");

			ContentTemplate temp = prov.ModifyContentTemplate("T", "Mod");
			Assert.AreEqual("T", temp.Name, "Wrong name");
			Assert.AreEqual("Mod", temp.Content, "Wrong content");

			ContentTemplate[] templates = prov.GetContentTemplates();
			Assert.AreEqual(2, templates.Length, "Wrong template count");

			Array.Sort(templates, (x, y) => { return x.Name.CompareTo(y.Name); });

			Assert.AreEqual("T", templates[0].Name, "Wrong name");
			Assert.AreEqual("Mod", templates[0].Content, "Wrong content");
			Assert.AreEqual("T2", templates[1].Name, "Wrong name");
			Assert.AreEqual("Blah", templates[1].Content, "Wrong content");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void ModifyContentTemplate_InvalidName(string n) {
			IPagesStorageProviderV30 prov = GetProvider();

			prov.ModifyContentTemplate(n, "Content");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void ModifyContentTemplate_NullContent() {
			IPagesStorageProviderV30 prov = GetProvider();

			prov.ModifyContentTemplate("T", null);
		}

		[Test]
		public void RemoveContentTemplate() {
			IPagesStorageProviderV30 prov = GetProvider();

			Assert.IsFalse(prov.RemoveContentTemplate("T"), "RemoveContentTemplate should return false");

			prov.AddContentTemplate("T", "Content");
			prov.AddContentTemplate("T2", "Blah");

			Assert.IsTrue(prov.RemoveContentTemplate("T"), "RemoveContentTemplate should return true");

			ContentTemplate[] templates = prov.GetContentTemplates();
			Assert.AreEqual(1, templates.Length, "Wrong template count");

			Assert.AreEqual("T2", templates[0].Name, "Wrong name");
			Assert.AreEqual("Blah", templates[0].Content, "Wrong content");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void RemoveContentTemplate_InvalidName(string n) {
			IPagesStorageProviderV30 prov = GetProvider();

			prov.RemoveContentTemplate(n);
		}

		[Test]
		public void RebuildIndex_ManyPages() {
			IPagesStorageProviderV30 prov = GetProvider();

			for(int i = 0; i < PagesContent.Length; i++) {
				PageInfo page = prov.AddPage(null, "The Longest Page Name Ever Seen In The Whole Universe (Maybe) - " + i.ToString(), DateTime.Now);
				Assert.IsNotNull(page, "AddPage should return something");

				bool done = prov.ModifyPage(page, "Page " + i.ToString(), "NUnit", DateTime.Now, "Comment " + i.ToString(),
					PagesContent[i], null, "Test Page " + i.ToString(), SaveMode.Normal);
				Assert.IsTrue(done, "ModifyPage should return true");
			}

			DoChecksFor_RebuildIndex_ManyPages(prov);

			prov.RebuildIndex();

			DoChecksFor_RebuildIndex_ManyPages(prov);
		}

		private void DoChecksFor_RebuildIndex_ManyPages(IPagesStorageProviderV30 prov) {
			int docCount, wordCount, matchCount;
			long size;
			prov.GetIndexStats(out docCount, out wordCount, out matchCount, out size);
			Assert.AreEqual(PagesContent.Length, docCount, "Wrong document count");
			Assert.IsTrue(wordCount > 0, "Wrong word count");
			Assert.IsTrue(matchCount > 0, "Wrong match count");
			Assert.IsTrue(size > 0, "Wrong size");

			SearchResultCollection results = prov.PerformSearch(new SearchParameters("lorem"));
			Assert.IsTrue(results.Count > 0, "No results returned");
		}

		private static readonly string[] PagesContent =
		{
			"Lorem ipsum dolor sit amet, consectetur adipiscing elit. Duis non massa eu erat imperdiet porta nec eu ipsum. Nulla ullamcorper massa et dui tincidunt eget volutpat velit pellentesque.",
			"Class aptent taciti sociosqu ad litora torquent per conubia nostra, per inceptos himenaeos. Nunc venenatis vestibulum velit, at molestie dui blandit eget.",
			"Praesent bibendum accumsan nulla quis convallis. Quisque eget est metus. Praesent cursus mauris at diam aliquam luctus sit amet sed odio.",
			"Cras dignissim eros quis risus vehicula quis ultrices ipsum mattis. Praesent hendrerit sodales volutpat.",
			"Sed laoreet, quam at tempus rhoncus, ipsum ipsum tempus libero, in sodales justo nisl a tortor. Mauris non enim libero, ac rutrum tortor. Quisque at lacus mauris. Aenean non tortor a eros fermentum fringilla. Mauris tincidunt scelerisque mattis.",
			"Ut ut augue ut sapien dapibus ullamcorper a imperdiet lorem. Maecenas rhoncus nibh nec purus ullamcorper dapibus. Sed blandit, dui eget aliquet adipiscing, nunc orci semper leo, eu fringilla ipsum neque ut augue.",
			"Vestibulum cursus lectus dolor, eget lobortis libero. Sed nulla lacus, vulputate at vestibulum sit amet, faucibus id sapien. Nunc egestas semper laoreet.",
			"Nunc tempus molestie velit, eu imperdiet ante luctus ut. Praesent diam sapien, mattis nec feugiat a, gravida sit amet quam.",
			"Sed eu erat sed nulla vulputate molestie vel ut justo. Cras vestibulum ultrices mauris in consectetur. In bibendum enim neque, id tempus erat.",
			"Aenean blandit, justo et tempus dignissim, arcu odio vestibulum erat, sed venenatis odio turpis sed nulla. Aenean venenatis rhoncus sem, sed tincidunt est cursus id. Nam ut sem id dui varius porta.",

			"Suspendisse potenti. Duis non dui ac nulla cursus varius. Morbi auctor diam quis urna lobortis sit amet laoreet leo egestas. Integer velit ante, dictum id faucibus quis, pulvinar vitae ipsum. Ut sed lorem lacus. Morbi a enim purus, quis tincidunt risus.",
			"Maecenas ac odio quis magna vehicula faucibus. Ut arcu est, volutpat fringilla gravida non, mattis a diam. Nullam dapibus, arcu eget sagittis mattis, tortor leo consectetur tortor, eget mollis libero elit vel metus.",
			"Vivamus faucibus ante at urna adipiscing pulvinar. Pellentesque ligula ante, sollicitudin a iaculis sed, dictum quis leo. Quisque augue ipsum, ultrices vitae pretium vel, vulputate vel arcu.",
			"Nullam semper luctus dui. Morbi gravida tortor odio, et condimentum velit. Integer semper dapibus turpis, ac suscipit mauris eleifend et. Cras a quam tortor. Mauris lorem mauris, ultricies sed tristique ac, sollicitudin sit amet nibh.",
			"Praesent scelerisque convallis risus, a tincidunt turpis porta nec. Aenean tristique malesuada diam, ut fringilla tortor congue vel. Duis id feugiat sapien. In hendrerit, nisl id porttitor convallis, sem est pretium sem, a tincidunt lorem ligula eget massa.",
			"Aliquam neque quam, cursus eu iaculis non, laoreet et eros. Maecenas a lacus arcu. Mauris et placerat erat. Pellentesque ut felis est, sit amet sollicitudin turpis. Etiam non odio orci.",
			"Nulla purus orci, elementum nec convallis in, feugiat sit amet lectus. Aenean eu elit sem. Quisque sit amet ante nibh, sed elementum magna. Quisque non est odio.",
			"Morbi porta metus at mi vehicula sit amet scelerisque nulla vehicula. Sed eleifend venenatis velit. Nulla augue mauris, dignissim sed rhoncus non, luctus nec nulla. In hac habitasse platea dictumst.",
			"Pellentesque habitant morbi tristique senectus et netus et malesuada fames ac turpis egestas. Lorem ipsum dolor sit amet, consectetur adipiscing elit. In nunc est, euismod et ullamcorper vitae, aliquam quis nulla.",
			"Morbi porttitor vehicula placerat. Nunc et lorem mauris. Morbi in nunc lorem. Integer aliquet sem vel magna scelerisque lobortis. Integer nec suscipit libero. Vestibulum ante ipsum primis in faucibus orci luctus et ultrices posuere cubilia Curae; Nam posuere cursus enim. Sed auctor semper vehicula. Phasellus volutpat est et sem ultricies ornare."
		};

	}

}
