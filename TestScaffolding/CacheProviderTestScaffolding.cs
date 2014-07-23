
using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Rhino.Mocks;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki.Tests {
	
	[TestFixture]
	public abstract class CacheProviderTestScaffolding {

		private MockRepository mocks = new MockRepository();

		protected IHostV30 MockHost() {
			IHostV30 host = mocks.DynamicMock<IHostV30>();

			Expect.Call(host.GetSettingValue(SettingName.CacheSize)).Return("20").Repeat.Any();
			Expect.Call(host.GetSettingValue(SettingName.EditingSessionTimeout)).Return("1").Repeat.Any();

			mocks.Replay(host);

			return host;
		}

		protected IPagesStorageProviderV30 MockPagesProvider() {
			IPagesStorageProviderV30 prov = mocks.DynamicMock<IPagesStorageProviderV30>();

			mocks.Replay(prov);

			return prov;
		}

		public abstract ICacheProviderV30 GetProvider();

		[Test]
		public void Init() {
			ICacheProviderV30 prov = GetProvider();
			prov.Init(MockHost(), "");

			Assert.IsNotNull(prov.Information, "Information should not be null");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void Init_NullHost() {
			ICacheProviderV30 prov = GetProvider();

			prov.Init(null, "");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void Init_NullConfig() {
			ICacheProviderV30 prov = GetProvider();
			prov.Init(MockHost(), null);
		}

		[Test]
		public void SetOnlineUsers_GetOnlineUsers() {
			ICacheProviderV30 prov = GetProvider();

			prov.OnlineUsers = 100;
			Assert.AreEqual(100, prov.OnlineUsers, "Wrong online users count");

			prov.OnlineUsers++;
			Assert.AreEqual(101, prov.OnlineUsers, "Wrong online users count");

			prov.OnlineUsers--;
			Assert.AreEqual(100, prov.OnlineUsers, "Wrong online users count");
		}

		[Test]
		public void SetPseudoCacheValue_GetPseudoCacheValue() {
			ICacheProviderV30 prov = GetProvider();

			prov.SetPseudoCacheValue("Name", "Value");
			prov.SetPseudoCacheValue("Test", "Blah");

			Assert.AreEqual("Value", prov.GetPseudoCacheValue("Name"), "Wrong pseudo-cache value");
			Assert.AreEqual("Blah", prov.GetPseudoCacheValue("Test"), "Wrong pseudo-cache value");
			Assert.IsNull(prov.GetPseudoCacheValue("Inexistent"), "Pseudo-cache value should be null");

			prov.SetPseudoCacheValue("Name", null);
			prov.SetPseudoCacheValue("Test", "");

			Assert.IsNull(prov.GetPseudoCacheValue("Name"), "Pseudo-cache value should be null");
			Assert.AreEqual("", prov.GetPseudoCacheValue("Test"), "Wrong pseudo-cache value");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void GetPseudoCacheValue_InvalidName(string n) {
			ICacheProviderV30 prov = GetProvider();
			prov.GetPseudoCacheValue(n);
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void SetPseudoCacheValue_InvalidName(string n) {
			ICacheProviderV30 prov = GetProvider();
			prov.SetPseudoCacheValue(n, "Value");
		}

		[Test]
		public void SetPageContent_GetPageContent() {
			ICacheProviderV30 prov = GetProvider();

			PageInfo p1 = new PageInfo("Page1", MockPagesProvider(), DateTime.Now);
			PageInfo p2 = new PageInfo("Page2", MockPagesProvider(), DateTime.Now);
			PageContent c1 = new PageContent(p1, "Page 1", "admin", DateTime.Now, "Comment", "Content", new string[] { "test", "page" }, null);
			PageContent c2 = new PageContent(p2, "Page 2", "user", DateTime.Now, "", "Blah", null, null);
			PageContent c3 = new PageContent(p2, "Page 5", "john", DateTime.Now, "Comm.", "Blah 222", null, "Description");

			Assert.AreEqual(0, prov.PageCacheUsage, "Wrong cache usage");

			prov.SetPageContent(p1, c1);
			prov.SetPageContent(p2, c2);
			prov.SetPageContent(p2, c3);

			Assert.AreEqual(2, prov.PageCacheUsage, "Wrong cache usage");

			PageContent res = prov.GetPageContent(p1);
			Assert.AreEqual(c1.PageInfo, res.PageInfo, "Wrong page info");
			Assert.AreEqual(c1.Title, res.Title, "Wrong title");
			Assert.AreEqual(c1.User, res.User, "Wrong user");
			Assert.AreEqual(c1.LastModified, res.LastModified, "Wrong date/time");
			Assert.AreEqual(c1.Comment, res.Comment, "Wrong comment");
			Assert.AreEqual(c1.Content, res.Content, "Wrong content");
			Assert.AreEqual(2, c1.Keywords.Length, "Wrong keyword count");
			Assert.AreEqual("test", c1.Keywords[0], "Wrong keyword");
			Assert.AreEqual("page", c1.Keywords[1], "Wrong keyword");
			Assert.IsNull(c1.Description, "Description should be null");

			res = prov.GetPageContent(p2);
			Assert.AreEqual(c3.PageInfo, res.PageInfo, "Wrong page info");
			Assert.AreEqual(c3.Title, res.Title, "Wrong title");
			Assert.AreEqual(c3.User, res.User, "Wrong user");
			Assert.AreEqual(c3.LastModified, res.LastModified, "Wrong date/time");
			Assert.AreEqual(c3.Comment, res.Comment, "Wrong comment");
			Assert.AreEqual(c3.Content, res.Content, "Wrong content");
			Assert.AreEqual(0, c3.Keywords.Length, "Keywords should be empty");
			Assert.AreEqual("Description", c3.Description, "Wrong description");

			Assert.IsNull(prov.GetPageContent(new PageInfo("Blah", MockPagesProvider(), DateTime.Now)), "GetPageContent should return null");
		}

		[ExpectedException(typeof(ArgumentNullException))]
		public void GetPageContent_NullPage() {
			ICacheProviderV30 prov = GetProvider();
			prov.GetPageContent(null);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void SetPageContent_NullPage() {
			ICacheProviderV30 prov = GetProvider();

			PageInfo p1 = new PageInfo("Page1", MockPagesProvider(), DateTime.Now);
			PageContent c1 = new PageContent(p1, "Page 1", "admin", DateTime.Now, "Comment", "Content", null, null);

			prov.SetPageContent(null, c1);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void SetPageContent_NullContent() {
			ICacheProviderV30 prov = GetProvider();

			PageInfo p1 = new PageInfo("Page1", MockPagesProvider(), DateTime.Now);
			PageContent c1 = new PageContent(p1, "Page 1", "admin", DateTime.Now, "Comment", "Content", null, null);

			prov.SetPageContent(p1, null);
		}

		[Test]
		public void SetFormattedPageContent_GetFormattedPageContent() {
			ICacheProviderV30 prov = GetProvider();

			PageInfo p1 = new PageInfo("Page1", MockPagesProvider(), DateTime.Now);
			PageInfo p2 = new PageInfo("Page2", MockPagesProvider(), DateTime.Now);

			Assert.AreEqual(0, prov.PageCacheUsage, "Wrong cache usage");

			prov.SetFormattedPageContent(p1, "Content 1");
			prov.SetFormattedPageContent(p2, "Content 2");
			prov.SetFormattedPageContent(p1, "Content 1 mod");

			Assert.AreEqual(0, prov.PageCacheUsage, "Wrong cache usage");

			Assert.AreEqual("Content 1 mod", prov.GetFormattedPageContent(p1), "Wrong content");
			Assert.AreEqual("Content 2", prov.GetFormattedPageContent(p2), "Wrong content");

			Assert.IsNull(prov.GetFormattedPageContent(new PageInfo("Blah", MockPagesProvider(), DateTime.Now)), "GetFormattedPageContent should return null");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void GetFormattedPageContent_NullPage() {
			ICacheProviderV30 prov = GetProvider();
			prov.GetFormattedPageContent(null);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void SetFormattedPageContent_NullPage() {
			ICacheProviderV30 prov = GetProvider();
			prov.SetFormattedPageContent(null, "Content");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void SetFormattedPageContent_NullContent() {
			ICacheProviderV30 prov = GetProvider();

			PageInfo p1 = new PageInfo("Page1", MockPagesProvider(), DateTime.Now);

			prov.SetFormattedPageContent(p1, null);
		}

		[Test]
		public void RemovePageContent() {
			ICacheProviderV30 prov = GetProvider();

			PageInfo p1 = new PageInfo("Page1", MockPagesProvider(), DateTime.Now);
			PageInfo p2 = new PageInfo("Page2", MockPagesProvider(), DateTime.Now);
			PageContent c1 = new PageContent(p1, "Page 1", "admin", DateTime.Now, "Comment", "Content", null, null);
			PageContent c2 = new PageContent(p2, "Page 2", "user", DateTime.Now, "", "Blah", null, null);

			Assert.AreEqual(0, prov.PageCacheUsage, "Wrong cache usage");

			prov.SetPageContent(p1, c1);
			prov.SetPageContent(p2, c2);
			prov.SetFormattedPageContent(p1, "Content 1");
			prov.SetFormattedPageContent(p2, "Content 2");

			Assert.AreEqual(2, prov.PageCacheUsage, "Wrong cache usage");

			prov.RemovePage(p2);

			Assert.IsNotNull(prov.GetFormattedPageContent(p1), "GetFormattedPageContent should not return null");
			Assert.IsNotNull(prov.GetPageContent(p1), "GetPageContent should not return null");

			Assert.IsNull(prov.GetFormattedPageContent(p2), "GetFormattedPageContent should return null");
			Assert.IsNull(prov.GetPageContent(p2), "GetPageContent should return null");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RemovePageContent_NullPage() {
			ICacheProviderV30 prov = GetProvider();

			prov.RemovePage(null);
		}

		[Test]
		public void ClearPageContentCache() {
			ICacheProviderV30 prov = GetProvider();

			PageInfo p1 = new PageInfo("Page1", MockPagesProvider(), DateTime.Now);
			PageInfo p2 = new PageInfo("Page2", MockPagesProvider(), DateTime.Now);
			PageContent c1 = new PageContent(p1, "Page 1", "admin", DateTime.Now, "Comment", "Content", null, null);
			PageContent c2 = new PageContent(p2, "Page 2", "user", DateTime.Now, "", "Blah", null, null);

			Assert.AreEqual(0, prov.PageCacheUsage, "Wrong cache usage");

			prov.SetPageContent(p1, c1);
			prov.SetPageContent(p2, c2);
			prov.SetFormattedPageContent(p1, "Content 1");
			prov.SetFormattedPageContent(p2, "Content 2");

			Assert.AreEqual(2, prov.PageCacheUsage, "Wrong cache usage");

			prov.ClearPageContentCache();

			Assert.AreEqual(0, prov.PageCacheUsage, "Wrong cache usage");

			Assert.IsNull(prov.GetPageContent(p1), "GetPageContent should return null");
			Assert.IsNull(prov.GetPageContent(p2), "GetPageContent should return null");
			Assert.IsNull(prov.GetFormattedPageContent(p1), "GetFormattedPageContent should return null");
			Assert.IsNull(prov.GetFormattedPageContent(p2), "GetFormattedPageContent should return null");
		}

		[Test]
		public void ClearPseudoCache() {
			ICacheProviderV30 prov = GetProvider();

			prov.SetPseudoCacheValue("Test", "Value");
			prov.SetPseudoCacheValue("222", "VVV");

			prov.ClearPseudoCache();

			Assert.IsNull(prov.GetPseudoCacheValue("Test"), "GetPseudoCacheValue should return null");
			Assert.IsNull(prov.GetPseudoCacheValue("222"), "GetPseudoCacheValue should return null");
		}

		[Test]
		public void CutCache() {
			ICacheProviderV30 prov = GetProvider();

			PageInfo p1 = new PageInfo("Page1", MockPagesProvider(), DateTime.Now);
			PageInfo p2 = new PageInfo("Page2", MockPagesProvider(), DateTime.Now);
			PageInfo p3 = new PageInfo("Page3", MockPagesProvider(), DateTime.Now);
			PageContent c1 = new PageContent(p1, "Page 1", "admin", DateTime.Now, "Comment", "Content", null, null);
			PageContent c2 = new PageContent(p2, "Page 2", "user", DateTime.Now, "", "Blah", null, null);
			PageContent c3 = new PageContent(p3, "Page 3", "admin", DateTime.Now, "", "Content", null, null);

			Assert.AreEqual(0, prov.PageCacheUsage, "Wrong cache usage");

			prov.SetPageContent(p1, c1);
			prov.SetPageContent(p2, c2);
			prov.SetPageContent(p3, c3);
			prov.SetFormattedPageContent(p1, "Content 1");
			prov.SetFormattedPageContent(p3, "Content 2");

			prov.GetPageContent(p3);

			Assert.AreEqual(3, prov.PageCacheUsage, "Wrong cache usage");

			prov.CutCache(2);

			Assert.AreEqual(1, prov.PageCacheUsage, "Wrong cache usage");

			Assert.IsNotNull(prov.GetPageContent(p3), "GetPageContent should not return null");
			Assert.IsNull(prov.GetPageContent(p2), "GetPageContent should not null");
			Assert.IsNull(prov.GetPageContent(p1), "GetPageContent should not null");

			Assert.IsNotNull(prov.GetFormattedPageContent(p3), "GetFormattedPageContent should not return null");
			Assert.IsNull(prov.GetFormattedPageContent(p2), "GetFormattedPageContent should not null");
			Assert.IsNull(prov.GetFormattedPageContent(p1), "GetFormattedPageContent should not null");
		}

		[TestCase(-1, ExpectedException = typeof(ArgumentOutOfRangeException))]
		[TestCase(0, ExpectedException = typeof(ArgumentOutOfRangeException))]
		public void CutCache_InvalidSize(int s) {
			ICacheProviderV30 prov = GetProvider();
			prov.CutCache(s);
		}

		[Test]
		public void RenewEditingSession_IsPageBeingEdited() {
			ICacheProviderV30 prov = GetProvider();

			prov.RenewEditingSession("Page", "User");

			Assert.IsFalse(prov.IsPageBeingEdited("Page", "User"), "IsPageBeingEditing should return false");
			Assert.IsTrue(prov.IsPageBeingEdited("Page", "User2"), "IsPageBeingEditing should return true");
			Assert.IsFalse(prov.IsPageBeingEdited("Page2", "User"), "IsPageBeingEditing should return false");
			Assert.IsFalse(prov.IsPageBeingEdited("Page2", "User2"), "IsPageBeingEditing should return false");

			// Wait for timeout to expire
			System.Threading.Thread.Sleep(6500);
			Assert.IsFalse(prov.IsPageBeingEdited("Page", "User2"), "IsPageBeingEdited should return false");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void RenewEditingSession_InvalidPage(string p) {
			ICacheProviderV30 prov = GetProvider();
			prov.RenewEditingSession(p, "User");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void RenewEditingSession_InvalidUser(string u) {
			ICacheProviderV30 prov = GetProvider();
			prov.RenewEditingSession("Page", u);
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void IsPageBeingEdited_InvalidPage(string p) {
			ICacheProviderV30 prov = GetProvider();
			prov.IsPageBeingEdited(p, "User");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void IsPageBeingEdited_InvalidUser(string u) {
			ICacheProviderV30 prov = GetProvider();
			prov.IsPageBeingEdited("Page", u);
		}

		[Test]
		public void CancelEditingSession_IsPageBeingEdited() {
			ICacheProviderV30 prov = GetProvider();

			prov.RenewEditingSession("Page", "User");

			Assert.IsFalse(prov.IsPageBeingEdited("Page", "User"), "IsPageBeingEditing should return false");
			Assert.IsTrue(prov.IsPageBeingEdited("Page", "User2"), "IsPageBeingEditing should return true");

			prov.CancelEditingSession("Page", "User");

			Assert.IsFalse(prov.IsPageBeingEdited("Page", "User"), "IsPageBeingEditing should return false");
			Assert.IsFalse(prov.IsPageBeingEdited("Page", "User2"), "IsPageBeingEditing should return false");

			prov.RenewEditingSession("Page", "User1");
			prov.RenewEditingSession("Page", "User2");

			prov.CancelEditingSession("Page", "User1");

			Assert.IsTrue(prov.IsPageBeingEdited("Page", "User1"), "IsPageBeingEditing should return true");
			Assert.IsFalse(prov.IsPageBeingEdited("Page", "User2"), "IsPageBeingEditing should return false");

			prov.CancelEditingSession("Page", "User2");

			Assert.IsFalse(prov.IsPageBeingEdited("Page", "User2"), "IsPageBeingEditing should return false");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void CancelEditingSession_InvalidPage(string p) {
			ICacheProviderV30 prov = GetProvider();
			prov.CancelEditingSession(p, "User");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void CancelEditingSession_InvalidUser(string u) {
			ICacheProviderV30 prov = GetProvider();
			prov.CancelEditingSession("Page", u);
		}

		[Test]
		public void WhosEditing() {
			ICacheProviderV30 prov = GetProvider();

			prov.RenewEditingSession("Page", "User1");
			prov.RenewEditingSession("Page", "User2");

			Assert.AreEqual("", prov.WhosEditing("Inexistent"), "Wrong result (should be empty)");

			Assert.AreEqual("User1", prov.WhosEditing("Page"), "Wrong user");

			prov.CancelEditingSession("Page", "User1");

			Assert.AreEqual("User2", prov.WhosEditing("Page"), "Wrong user");

			prov.CancelEditingSession("Page", "User2");

			Assert.AreEqual("", prov.WhosEditing("Page"), "Wrong user");

		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void WhosEditing_InvalidPage(string p) {
			ICacheProviderV30 prov = GetProvider();
			prov.WhosEditing(p);
		}

		[Test]
		public void AddRedirection_GetDestination_RemovePageFromRedirections_Clear() {
			ICacheProviderV30 prov = GetProvider();

			Assert.IsNull(prov.GetRedirectionDestination("Page"), "No redirection should be in cache");

			prov.AddRedirection("Page", "NS.OtherPage");
			prov.AddRedirection("NS.OtherPage", "Page3");
			prov.AddRedirection("ThirdPage", "Page");

			Assert.AreEqual("NS.OtherPage", prov.GetRedirectionDestination("Page"), "Wrong destination");
			Assert.AreEqual("Page3", prov.GetRedirectionDestination("NS.OtherPage"), "Wrong destination");
			Assert.AreEqual("Page", prov.GetRedirectionDestination("ThirdPage"), "Wrong destination");

			prov.RemovePageFromRedirections("Page");

			Assert.IsNull(prov.GetRedirectionDestination("Page"), "No redirection should be in cache for Page");
			Assert.AreEqual("Page3", prov.GetRedirectionDestination("NS.OtherPage"), "Wrong destination");
			Assert.IsNull(prov.GetRedirectionDestination("Page"), "No redirection should be in cache for ThirdPage");

			prov.ClearRedirections();

			Assert.IsNull(prov.GetRedirectionDestination("Page"), "No redirection should be in cache");
			Assert.IsNull(prov.GetRedirectionDestination("NS.OtherPage"), "No redirection should be in cache");
			Assert.IsNull(prov.GetRedirectionDestination("Page"), "No redirection should be in cache");
		}

		[TestCase(null, "destination", ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", "destination", ExpectedException = typeof(ArgumentException))]
		[TestCase("source", null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("source", "", ExpectedException = typeof(ArgumentException))]
		public void AddRedirection_InvalidParameters(string src, string dest) {
			ICacheProviderV30 prov = GetProvider();
			prov.AddRedirection(src, dest);
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void GetRedirectionDestination_InvalidSource(string src) {
			ICacheProviderV30 prov = GetProvider();
			prov.GetRedirectionDestination(src);
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void RemovePageFromRedirections_InvalidName(string name) {
			ICacheProviderV30 prov = GetProvider();
			prov.RemovePageFromRedirections(name);
		}

	}

}
