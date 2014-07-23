
using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Rhino.Mocks;
using RMC = Rhino.Mocks.Constraints;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki.Plugins.PluginPack.Tests {

	[TestFixture]
	public class DownloadCounterTests {

		[Test]
		public void Format() {
			MockRepository mocks = new MockRepository();

			IFilesStorageProviderV30 prov = mocks.StrictMock<IFilesStorageProviderV30>();

			string content = string.Format(
@"This is a test page.
<countDownloads pattern=""#CoUnT#-#DaIlY#-#WeEkLy#-#MoNtHlY#"" startDate=""{0:yyyy'/'MM'/'dd}"">
	<file name=""/my/file.zip"" provider=""{1}"" />
	<file name=""/my/other/file.zip"" />
	<file name=""/my/inexistent-file.zip"" provider=""{1}"" />
	<file name=""/my/other/inexistent-file.zip"" />

	<attachment name=""attn.zip"" page=""page"" provider=""{1}"" />
	<attachment name=""attn.zip"" page=""page"" />
	<attachment name=""inexistent-attn.zip"" page=""page"" provider=""{1}"" />
	<attachment name=""inexistent-attn.zip"" page=""page"" />
	<attachment name=""attn.zip"" page=""inexistent-page"" provider=""{1}"" />
	<attachment name=""attn.zip"" page=""inexistent-page"" />
</countDownloads>", DateTime.Today.AddDays(-60), prov.GetType().FullName);

			// */file.zip was downloaded 13 times
			// attn.zip was downloaded 56 times
			// Expected output: 138-2-16-68

			IHostV30 host = mocks.StrictMock<IHostV30>();
			host.LogEntry(null, LogEntryType.Warning, null, null);
			LastCall.On(host).IgnoreArguments().Repeat.Any();
			Expect.Call(host.GetSettingValue(SettingName.DefaultFilesStorageProvider)).Return(
				prov.GetType().FullName).Repeat.Times(4);
			Expect.Call(host.GetFilesStorageProviders(true)).Return(
				new IFilesStorageProviderV30[] { prov }).Repeat.Times(8);

			StFileInfo[] myFiles = new StFileInfo[] {
				new StFileInfo(1000, DateTime.Now, 13, "/my/File.zip", prov),
				new StFileInfo(10000, DateTime.Now, 1000, "/my/other-file.zip", prov)
			};
			StFileInfo[] myOtherFiles = new StFileInfo[] {
				new StFileInfo(1000, DateTime.Now, 13, "/my/OTHER/file.zip", prov),
				new StFileInfo(10000, DateTime.Now, 2000, "/my/OTHER/other-file.zip", prov)
			};

			StFileInfo[] attachments = new StFileInfo[] {
				new StFileInfo(2000, DateTime.Now, 56, "aTTn.zip", prov),
				new StFileInfo(20000, DateTime.Now, 1000, "other-attn.zip", prov)
			};

			// /my/*
			Expect.Call(host.ListFiles(null)).IgnoreArguments().Constraints(
				RMC.Is.Matching(
					delegate(StDirectoryInfo dir) {
						return dir.FullPath == "/my/";
					})).Return(myFiles).Repeat.Times(2);

			// /my/other/*
			Expect.Call(host.ListFiles(null)).IgnoreArguments().Constraints(
				RMC.Is.Matching(
					delegate(StDirectoryInfo dir) {
						return dir.FullPath == "/my/other/";
					})).Return(myOtherFiles).Repeat.Times(2);

			PageInfo page = new PageInfo("page", null, DateTime.Now);

			Expect.Call(host.FindPage("page")).Return(page).Repeat.Times(4);
			Expect.Call(host.FindPage("inexistent-page")).Return(null).Repeat.Twice();

			Expect.Call(host.ListPageAttachments(page)).Return(attachments).Repeat.Times(4);

			mocks.ReplayAll();

			DownloadCounter counter = new DownloadCounter();
			counter.Init(host, "");

			string output = counter.Format(content, null, FormattingPhase.Phase3);

			Assert.IsTrue(output == @"This is a test page.
138-2-16-68" || output == @"This is a test page.
138-2-16-69", "Wrong output");

			mocks.VerifyAll();
		}

	}

}
