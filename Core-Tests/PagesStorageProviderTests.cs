
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ScrewTurn.Wiki.PluginFramework;
using NUnit.Framework;
using Rhino.Mocks;

namespace ScrewTurn.Wiki.Tests {

	public class PagesStorageProviderTests : PagesStorageProviderTestScaffolding {

		public override IPagesStorageProviderV30 GetProvider() {
			PagesStorageProvider prov = new PagesStorageProvider();
			prov.Init(MockHost(), "");
			return prov;
		}

		[Test]
		public void Init() {
			IPagesStorageProviderV30 prov = GetProvider();
			prov.Init(MockHost(), "");

			Assert.IsNotNull(prov.Information, "Information should not be null");
		}

		[Test]
		public void Init_Upgrade() {
			string testDir = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), Guid.NewGuid().ToString());
			Directory.CreateDirectory(testDir);

			MockRepository mocks = new MockRepository();
			IHostV30 host = mocks.DynamicMock<IHostV30>();
			Expect.Call(host.GetSettingValue(SettingName.PublicDirectory)).Return(testDir).Repeat.AtLeastOnce();

			Expect.Call(host.UpgradePageStatusToAcl(null, 'L')).IgnoreArguments().Repeat.Twice().Return(true);

			mocks.Replay(host);

			string file = Path.Combine(host.GetSettingValue(SettingName.PublicDirectory), "Pages.cs");
			string categoriesFile = Path.Combine(host.GetSettingValue(SettingName.PublicDirectory), "Categories.cs");
			string navPathsFile = Path.Combine(host.GetSettingValue(SettingName.PublicDirectory), "NavigationPaths.cs");
			string directory = Path.Combine(host.GetSettingValue(SettingName.PublicDirectory), "Pages");
			string messagesDirectory = Path.Combine(host.GetSettingValue(SettingName.PublicDirectory), "Messages");
			Directory.CreateDirectory(directory);
			Directory.CreateDirectory(messagesDirectory);

			// Structure (Keywords and Description are new in v3)
			// Page Title
			// Username|DateTime[|Comment] --- Comment is optional
			// ##PAGE##
			// Content...

			File.WriteAllText(Path.Combine(directory, "Page1.cs"), "Title1\r\nSYSTEM|2008/10/30 20:20:20|Comment\r\n##PAGE##\r\nContent...");
			File.WriteAllText(Path.Combine(directory, "Page2.cs"), "Title2\r\nSYSTEM|2008/10/30 20:20:20\r\n##PAGE\r\nContent. [[Page.3]] [Page.3|Link to update].");
			File.WriteAllText(Path.Combine(directory, "Page.3.cs"), "Title3\r\nSYSTEM|2008/10/30 20:20:20|Comment\r\n##PAGE\r\nContent...");

			// ID|Username|Subject|DateTime|ParentID|Body
			File.WriteAllText(Path.Combine(messagesDirectory, "Page.3.cs"), "0|User|Hello|2008/10/30 21:21:21|-1|Blah\r\n");

			// Structure
			// [Namespace.]PageName|PageFile|Status|DateTime
			File.WriteAllText(file, "Page1|Page1.cs|NORMAL|2008/10/30 20:20:20\r\nPage2|Page2.cs|PUBLIC\r\nPage.3|Page.3.cs|LOCKED");

			File.WriteAllText(categoriesFile, "Cat1|Page.3\r\nCat.2|Page1|Page2\r\n");

			File.WriteAllText(navPathsFile, "Path1|Page1|Page.3\r\nPath2|Page2\r\n");

			PagesStorageProvider prov = new PagesStorageProvider();
			prov.Init(host, "");

			PageInfo[] pages = prov.GetPages(null);

			Assert.AreEqual(3, pages.Length, "Wrong page count");
			Assert.AreEqual("Page1", pages[0].FullName, "Wrong name");
			Assert.AreEqual("Page2", pages[1].FullName, "Wrong name");
			Assert.AreEqual("Page_3", pages[2].FullName, "Wrong name");
			//Assert.IsFalse(prov.GetContent(pages[1]).Content.Contains("Page.3"), "Content should not contain 'Page.3'");
			//Assert.IsTrue(prov.GetContent(pages[1]).Content.Contains("Page_3"), "Content should contain 'Page_3'");

			Message[] messages = prov.GetMessages(pages[2]);
			Assert.AreEqual(1, messages.Length, "Wrong message count");
			Assert.AreEqual("Hello", messages[0].Subject, "Wrong subject");

			CategoryInfo[] categories = prov.GetCategories(null);

			Assert.AreEqual(2, categories.Length, "Wrong category count");
			Assert.AreEqual("Cat1", categories[0].FullName, "Wrong name");
			Assert.AreEqual(1, categories[0].Pages.Length, "Wrong page count");
			Assert.AreEqual("Page_3", categories[0].Pages[0], "Wrong page");
			Assert.AreEqual("Cat_2", categories[1].FullName, "Wrong name");
			Assert.AreEqual(2, categories[1].Pages.Length, "Wrong page count");
			Assert.AreEqual("Page1", categories[1].Pages[0], "Wrong page");
			Assert.AreEqual("Page2", categories[1].Pages[1], "Wrong page");

			NavigationPath[] navPaths = prov.GetNavigationPaths(null);

			Assert.AreEqual(2, navPaths.Length, "Wrong nav path count");
			Assert.AreEqual("Path1", navPaths[0].FullName, "Wrong name");
			Assert.AreEqual(2, navPaths[0].Pages.Length, "Wrong page count");
			Assert.AreEqual("Page1", navPaths[0].Pages[0], "Wrong page");
			Assert.AreEqual("Page_3", navPaths[0].Pages[1], "Wrong page");
			Assert.AreEqual(1, navPaths[1].Pages.Length, "Wrong page count");
			Assert.AreEqual("Page2", navPaths[1].Pages[0], "Wrong page");

			mocks.Verify(host);

			// Simulate another startup - upgrade not needed anymore

			mocks.BackToRecord(host);
			Expect.Call(host.GetSettingValue(SettingName.PublicDirectory)).Return(testDir).Repeat.AtLeastOnce();
			Expect.Call(host.UpgradePageStatusToAcl(null, 'L')).IgnoreArguments().Repeat.Times(0).Return(false);

			mocks.Replay(host);

			prov = new PagesStorageProvider();
			prov.Init(host, "");

			mocks.Verify(host);

			Directory.Delete(testDir, true);
		}

		/*[Test]
		public void Init_UpgradeCategories() {
			string testDir = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), Guid.NewGuid().ToString());
			Directory.CreateDirectory(testDir);

			MockRepository mocks = new MockRepository();
			IHost host = mocks.DynamicMock<IHost>();
			Expect.Call(host.GetSettingValue(SettingName.PublicDirectory)).Return(testDir).Repeat.AtLeastOnce();

			mocks.Replay(host);

			string file = Path.Combine(host.GetSettingValue(SettingName.PublicDirectory), "Categories.cs");
			string filePages = Path.Combine(host.GetSettingValue(SettingName.PublicDirectory), "Pages.cs");
			string directory = Path.Combine(host.GetSettingValue(SettingName.PublicDirectory), "Pages");
			Directory.CreateDirectory(directory);

			File.WriteAllText(file, "Cat1|Page1\r\nCat.2|Page2|Page3\r\n");

			File.WriteAllText(Path.Combine(directory, "Page1.cs"), "Title1\r\nSYSTEM|2008/10/30 20:20:20|Comment\r\n##PAGE##\r\nContent...");
			File.WriteAllText(Path.Combine(directory, "Page2.cs"), "Title2\r\nSYSTEM|2008/10/30 20:20:20\r\n##PAGE\r\nContent...");
			File.WriteAllText(Path.Combine(directory, "Page3.cs"), "Title3\r\nSYSTEM|2008/10/30 20:20:20|Comment\r\n##PAGE\r\nContent...");

			File.WriteAllText(filePages, "Page1|Page1.cs|NORMAL|2008/10/30 20:20:20\r\nPage2|Page2.cs\r\nPage3|Page3.cs|LOCKED");

			PagesStorageProvider prov = new PagesStorageProvider();
			prov.Init(host, "");

			CategoryInfo[] categories = prov.GetCategories(null);
			Assert.AreEqual("Cat1", categories[0].FullName, "Wrong name");
			Assert.AreEqual("Cat_2", categories[1].FullName, "Wrong name");

			Assert.AreEqual(2, categories.Length, "Wrong page count");

			mocks.Verify(host);
		}*/

	}

}
