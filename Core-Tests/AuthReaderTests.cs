
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using Rhino.Mocks;
using ScrewTurn.Wiki.AclEngine;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki.Tests {

	[TestFixture]
	public class AuthReaderTests {

		private MockRepository mocks = null;
		private string testDir = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), Guid.NewGuid().ToString());

		[SetUp]
		public void SetUp() {
			mocks = new MockRepository();
			// TODO: Verify if this is really needed
			Collectors.SettingsProvider = MockProvider();
		}

		[TearDown]
		public void TearDown() {
			try {
				Directory.Delete(testDir, true);
			}
			catch {
				//Console.WriteLine("Test: could not delete temp directory");
			}
			mocks.VerifyAll();
		}

		protected IHostV30 MockHost() {
			if(!Directory.Exists(testDir)) Directory.CreateDirectory(testDir);

			IHostV30 host = mocks.DynamicMock<IHostV30>();
			Expect.Call(host.GetSettingValue(SettingName.PublicDirectory)).Return(testDir).Repeat.Any();

			mocks.Replay(host);

			return host;
		}

		private ISettingsStorageProviderV30 MockProvider(List<AclEntry> entries) {
			ISettingsStorageProviderV30 provider = mocks.DynamicMock<ISettingsStorageProviderV30>();
			provider.Init(MockHost(), "");
			LastCall.On(provider).Repeat.Any();

			AclManagerBase aclManager = new StandardAclManager();
			Expect.Call(provider.AclManager).Return(aclManager).Repeat.Any();

			mocks.Replay(provider);

			foreach(AclEntry entry in entries) {
				aclManager.StoreEntry(entry.Resource, entry.Action, entry.Subject, entry.Value);
			}

			return provider;
		}

		private ISettingsStorageProviderV30 MockProvider() {
			return MockProvider(new List<AclEntry>());
		}

		[Test]
		public void RetrieveGrantsForGlobals_Group() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV30 prov = mocks.DynamicMock<ISettingsStorageProviderV30>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			Expect.Call(aclManager.RetrieveEntriesForSubject("G.Group")).Return(
				new AclEntry[] {
					new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.ForGlobals.ManageFiles, "G.Group", Value.Deny),
					new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.ForGlobals.ManageAccounts, "G.Group", Value.Grant),
					new AclEntry(Actions.ForDirectories.ResourceMasterPrefix + "/", Actions.ForDirectories.UploadFiles, "G.Group", Value.Grant) });

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			Collectors.SettingsProvider = prov;
			string[] grants = AuthReader.RetrieveGrantsForGlobals(new UserGroup("Group", "Group", null));
			Assert.AreEqual(1, grants.Length, "Wrong grant count");
			Assert.AreEqual(Actions.ForGlobals.ManageAccounts, grants[0], "Wrong grant");
			
			mocks.Verify(prov);
			mocks.Verify(aclManager);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RetrieveGrantsForGlobals_Group_NullGroup() {
			Collectors.SettingsProvider = MockProvider();

			AuthReader.RetrieveGrantsForGlobals(null as UserGroup);
		}

		[Test]
		public void RetrieveGrantsForGlobals_User() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV30 prov = mocks.DynamicMock<ISettingsStorageProviderV30>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			Expect.Call(aclManager.RetrieveEntriesForSubject("U.User")).Return(
				new AclEntry[] {
					new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.ForGlobals.ManageFiles, "U.User", Value.Deny),
					new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.ForGlobals.ManageAccounts, "U.User", Value.Grant),
					new AclEntry(Actions.ForDirectories.ResourceMasterPrefix + "/", Actions.ForDirectories.UploadFiles, "U.User", Value.Grant) });

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			Collectors.SettingsProvider = prov;
			string[] grants = AuthReader.RetrieveGrantsForGlobals(new UserInfo("User", "User", "user@users.com", true, DateTime.Now, null));
			Assert.AreEqual(1, grants.Length, "Wrong grant count");
			Assert.AreEqual(Actions.ForGlobals.ManageAccounts, grants[0], "Wrong grant");

			mocks.Verify(prov);
			mocks.Verify(aclManager);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RetrieveGrantsForGlobals_User_NullUser() {
			Collectors.SettingsProvider = MockProvider();

			AuthReader.RetrieveGrantsForGlobals(null as UserInfo);
		}

		[Test]
		public void RetrieveDenialsForGlobals_Group() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV30 prov = mocks.DynamicMock<ISettingsStorageProviderV30>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			Expect.Call(aclManager.RetrieveEntriesForSubject("G.Group")).Return(
				new AclEntry[] {
					new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.ForGlobals.ManageFiles, "G.Group", Value.Deny),
					new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.ForGlobals.ManageConfiguration, "G.Group", Value.Grant),
					new AclEntry(Actions.ForDirectories.ResourceMasterPrefix + "/", Actions.ForDirectories.UploadFiles, "G.Group", Value.Deny) });

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			Collectors.SettingsProvider = prov;
			string[] grants = AuthReader.RetrieveDenialsForGlobals(new UserGroup("Group", "Group", null));
			Assert.AreEqual(1, grants.Length, "Wrong denial count");
			Assert.AreEqual(Actions.ForGlobals.ManageFiles, grants[0], "Wrong denial");

			mocks.Verify(prov);
			mocks.Verify(aclManager);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RetrieveDenialsForGlobals_Group_NullGroup() {
			Collectors.SettingsProvider = MockProvider();

			AuthReader.RetrieveDenialsForGlobals(null as UserGroup);
		}

		[Test]
		public void RetrieveDenialsForGlobals_User() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV30 prov = mocks.DynamicMock<ISettingsStorageProviderV30>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			Expect.Call(aclManager.RetrieveEntriesForSubject("U.User")).Return(
				new AclEntry[] {
					new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.ForGlobals.ManageFiles, "U.User", Value.Deny),
					new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.ForGlobals.ManageAccounts, "U.User", Value.Grant),
					new AclEntry(Actions.ForDirectories.ResourceMasterPrefix + "/", Actions.ForDirectories.UploadFiles, "U.User", Value.Deny) });

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			Collectors.SettingsProvider = prov;
			string[] grants = AuthReader.RetrieveDenialsForGlobals(new UserInfo("User", "User", "user@users.com", true, DateTime.Now, null));
			Assert.AreEqual(1, grants.Length, "Wrong denial count");
			Assert.AreEqual(Actions.ForGlobals.ManageFiles, grants[0], "Wrong denial");

			mocks.Verify(prov);
			mocks.Verify(aclManager);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RetrieveDenialsForGlobals_User_NullUser() {
			Collectors.SettingsProvider = MockProvider();

			AuthReader.RetrieveDenialsForGlobals(null as UserInfo);
		}

		[Test]
		public void RetrieveSubjectsForNamespace_Root() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV30 prov = mocks.DynamicMock<ISettingsStorageProviderV30>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			Expect.Call(aclManager.RetrieveEntriesForResource("N.")).Return(
				new AclEntry[] {
					new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix, Actions.ForNamespaces.DeletePages, "U.User1", Value.Grant),
					new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix, Actions.ForNamespaces.ReadPages, "U.User", Value.Grant),
					new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix, Actions.ForNamespaces.ManagePages, "U.User", Value.Grant),
					new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix, Actions.ForNamespaces.ManagePages, "G.Group", Value.Deny) });

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			Collectors.SettingsProvider = prov;

			SubjectInfo[] infos = AuthReader.RetrieveSubjectsForNamespace(null);

			Assert.AreEqual(3, infos.Length, "Wrong info count");

			Array.Sort(infos, delegate(SubjectInfo x, SubjectInfo y) { return x.Name.CompareTo(y.Name); });
			Assert.AreEqual("Group", infos[0].Name, "Wrong subject name");
			Assert.AreEqual(SubjectType.Group, infos[0].Type, "Wrong subject type");
			Assert.AreEqual("User", infos[1].Name, "Wrong subject name");
			Assert.AreEqual(SubjectType.User, infos[1].Type, "Wrong subject type");
			Assert.AreEqual("User1", infos[2].Name, "Wrong subject name");
			Assert.AreEqual(SubjectType.User, infos[2].Type, "Wrong subject type");

			mocks.Verify(prov);
			mocks.Verify(aclManager);
		}

		[Test]
		public void RetrieveSubjectsForNamespace_Sub() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV30 prov = mocks.DynamicMock<ISettingsStorageProviderV30>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			Expect.Call(aclManager.RetrieveEntriesForResource("N.Sub")).Return(
				new AclEntry[] {
					new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix, Actions.ForNamespaces.DeletePages, "U.User1", Value.Grant),
					new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix, Actions.ForNamespaces.ReadPages, "U.User", Value.Grant),
					new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix, Actions.ForNamespaces.ManagePages, "U.User", Value.Grant),
					new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix, Actions.ForNamespaces.ManagePages, "G.Group", Value.Deny) });

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			Collectors.SettingsProvider = prov;

			SubjectInfo[] infos = AuthReader.RetrieveSubjectsForNamespace(new NamespaceInfo("Sub", null, null));

			Assert.AreEqual(3, infos.Length, "Wrong info count");

			Array.Sort(infos, delegate(SubjectInfo x, SubjectInfo y) { return x.Name.CompareTo(y.Name); });
			Assert.AreEqual("Group", infos[0].Name, "Wrong subject name");
			Assert.AreEqual(SubjectType.Group, infos[0].Type, "Wrong subject type");
			Assert.AreEqual("User", infos[1].Name, "Wrong subject name");
			Assert.AreEqual(SubjectType.User, infos[1].Type, "Wrong subject type");
			Assert.AreEqual("User1", infos[2].Name, "Wrong subject name");
			Assert.AreEqual(SubjectType.User, infos[2].Type, "Wrong subject type");

			mocks.Verify(prov);
			mocks.Verify(aclManager);
		}

		[Test]
		public void RetrieveGrantsForNamespace_Group_Root() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV30 prov = mocks.DynamicMock<ISettingsStorageProviderV30>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			Expect.Call(aclManager.RetrieveEntriesForSubject("G.Group")).Return(
				new AclEntry[] {
					new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix, Actions.ForNamespaces.ManagePages, "G.Group", Value.Grant),
					new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix, Actions.ForNamespaces.ManageCategories, "G.Group", Value.Deny),
					new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix + "Sub", Actions.ForNamespaces.ManagePages, "G.Group", Value.Grant) });

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			Collectors.SettingsProvider = prov;

			string[] grants = AuthReader.RetrieveGrantsForNamespace(new UserGroup("Group", "Group", null), null);

			Assert.AreEqual(1, grants.Length, "Wrong grant count");
			Assert.AreEqual(Actions.ForNamespaces.ManagePages, grants[0], "Wrong grant");
		}

		[Test]
		public void RetrieveGrantsForNamespace_Group_Sub() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV30 prov = mocks.DynamicMock<ISettingsStorageProviderV30>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			Expect.Call(aclManager.RetrieveEntriesForSubject("G.Group")).Return(
				new AclEntry[] {
					new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix + "Sub", Actions.ForNamespaces.ManagePages, "G.Group", Value.Grant),
					new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix + "Sub", Actions.ForNamespaces.ManageCategories, "G.Group", Value.Deny),
					new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix, Actions.ForNamespaces.ManagePages, "G.Group", Value.Grant) });

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			Collectors.SettingsProvider = prov;

			string[] grants = AuthReader.RetrieveGrantsForNamespace(new UserGroup("Group", "Group", null), new NamespaceInfo("Sub", null, null));

			Assert.AreEqual(1, grants.Length, "Wrong grant count");
			Assert.AreEqual(Actions.ForNamespaces.ManagePages, grants[0], "Wrong grant");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RetrieveGrantsForNamespace_Group_NullGroup() {
			Collectors.SettingsProvider = MockProvider();

			AuthReader.RetrieveGrantsForNamespace(null as UserGroup, new NamespaceInfo("Sub", null, null));
		}

		[Test]
		public void RetrieveGrantsForNamespace_User_Root() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV30 prov = mocks.DynamicMock<ISettingsStorageProviderV30>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			Expect.Call(aclManager.RetrieveEntriesForSubject("U.User")).Return(
				new AclEntry[] {
					new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix, Actions.ForNamespaces.ManagePages, "U.User", Value.Grant),
					new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix, Actions.ForNamespaces.ManageCategories, "U.User", Value.Deny),
					new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix + "Sub", Actions.ForNamespaces.ManagePages, "U.User", Value.Grant) });

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			Collectors.SettingsProvider = prov;

			string[] grants = AuthReader.RetrieveGrantsForNamespace(new UserInfo("User", "User", "user@users.com", true, DateTime.Now, null), null);

			Assert.AreEqual(1, grants.Length, "Wrong grant count");
			Assert.AreEqual(Actions.ForNamespaces.ManagePages, grants[0], "Wrong grant");
		}

		[Test]
		public void RetrieveGrantsForNamespace_User_Sub() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV30 prov = mocks.DynamicMock<ISettingsStorageProviderV30>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			Expect.Call(aclManager.RetrieveEntriesForSubject("U.User")).Return(
				new AclEntry[] {
					new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix + "Sub", Actions.ForNamespaces.ManagePages, "U.User", Value.Grant),
					new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix + "Sub", Actions.ForNamespaces.ManageCategories, "U.User", Value.Deny),
					new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix, Actions.ForNamespaces.ManagePages, "U.User", Value.Grant) });

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			Collectors.SettingsProvider = prov;

			string[] grants = AuthReader.RetrieveGrantsForNamespace(new UserInfo("User", "User", "user@users.com", true, DateTime.Now, null),
				new NamespaceInfo("Sub", null, null));

			Assert.AreEqual(1, grants.Length, "Wrong grant count");
			Assert.AreEqual(Actions.ForNamespaces.ManagePages, grants[0], "Wrong grant");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RetrieveGrantsForNamespace_User_NullUser() {
			Collectors.SettingsProvider = MockProvider();

			AuthReader.RetrieveGrantsForNamespace(null as UserInfo, new NamespaceInfo("Sub", null, null));
		}

		[Test]
		public void RetrieveDenialsForNamespace_Group_Root() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV30 prov = mocks.DynamicMock<ISettingsStorageProviderV30>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			Expect.Call(aclManager.RetrieveEntriesForSubject("G.Group")).Return(
				new AclEntry[] {
					new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix, Actions.ForNamespaces.ManagePages, "G.Group", Value.Deny),
					new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix, Actions.ForNamespaces.ManageCategories, "G.Group", Value.Grant),
					new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix + "Sub", Actions.ForNamespaces.ManagePages, "G.Group", Value.Deny) });

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			Collectors.SettingsProvider = prov;

			string[] grants = AuthReader.RetrieveDenialsForNamespace(new UserGroup("Group", "Group", null), null);

			Assert.AreEqual(1, grants.Length, "Wrong grant count");
			Assert.AreEqual(Actions.ForNamespaces.ManagePages, grants[0], "Wrong grant");
		}

		[Test]
		public void RetrieveDenialsForNamespace_Group_Sub() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV30 prov = mocks.DynamicMock<ISettingsStorageProviderV30>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			Expect.Call(aclManager.RetrieveEntriesForSubject("G.Group")).Return(
				new AclEntry[] {
					new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix + "Sub", Actions.ForNamespaces.ManagePages, "G.Group", Value.Deny),
					new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix + "Sub", Actions.ForNamespaces.ManageCategories, "G.Group", Value.Grant),
					new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix, Actions.ForNamespaces.ManagePages, "G.Group", Value.Deny) });

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			Collectors.SettingsProvider = prov;

			string[] grants = AuthReader.RetrieveDenialsForNamespace(new UserGroup("Group", "Group", null), new NamespaceInfo("Sub", null, null));

			Assert.AreEqual(1, grants.Length, "Wrong grant count");
			Assert.AreEqual(Actions.ForNamespaces.ManagePages, grants[0], "Wrong grant");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RetrieveDenialsForNamespace_Group_NullGroup() {
			Collectors.SettingsProvider = MockProvider();

			AuthReader.RetrieveDenialsForNamespace(null as UserGroup, new NamespaceInfo("Sub", null, null));
		}

		[Test]
		public void RetrieveDenialsForNamespace_User_Root() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV30 prov = mocks.DynamicMock<ISettingsStorageProviderV30>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			Expect.Call(aclManager.RetrieveEntriesForSubject("U.User")).Return(
				new AclEntry[] {
					new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix, Actions.ForNamespaces.ManagePages, "U.User", Value.Deny),
					new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix, Actions.ForNamespaces.ManageCategories, "U.User", Value.Grant),
					new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix + "Sub", Actions.ForNamespaces.ManagePages, "U.User", Value.Deny) });

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			Collectors.SettingsProvider = prov;

			string[] grants = AuthReader.RetrieveDenialsForNamespace(new UserInfo("User", "User", "user@users.com", true, DateTime.Now, null), null);

			Assert.AreEqual(1, grants.Length, "Wrong grant count");
			Assert.AreEqual(Actions.ForNamespaces.ManagePages, grants[0], "Wrong grant");
		}

		[Test]
		public void RetrieveDenialsForNamespace_User_Sub() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV30 prov = mocks.DynamicMock<ISettingsStorageProviderV30>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			Expect.Call(aclManager.RetrieveEntriesForSubject("U.User")).Return(
				new AclEntry[] {
					new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix + "Sub", Actions.ForNamespaces.ManagePages, "U.User", Value.Deny),
					new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix + "Sub", Actions.ForNamespaces.ManageCategories, "U.User", Value.Grant),
					new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix, Actions.ForNamespaces.ManagePages, "U.User", Value.Deny) });

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			Collectors.SettingsProvider = prov;

			string[] grants = AuthReader.RetrieveDenialsForNamespace(new UserInfo("User", "User", "user@users.com", true, DateTime.Now, null),
				new NamespaceInfo("Sub", null, null));

			Assert.AreEqual(1, grants.Length, "Wrong grant count");
			Assert.AreEqual(Actions.ForNamespaces.ManagePages, grants[0], "Wrong grant");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RetrieveDenialsForNamespace_User_NullUser() {
			Collectors.SettingsProvider = MockProvider();

			AuthReader.RetrieveDenialsForNamespace(null as UserInfo, new NamespaceInfo("Sub", null, null));
		}

		[Test]
		public void RetrieveSubjectsForPage() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV30 prov = mocks.DynamicMock<ISettingsStorageProviderV30>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			Expect.Call(aclManager.RetrieveEntriesForResource("P.NS.Page")).Return(
				new AclEntry[] {
					new AclEntry(Actions.ForPages.ResourceMasterPrefix + "Page", Actions.ForPages.ModifyPage, "U.User1", Value.Grant),
					new AclEntry(Actions.ForPages.ResourceMasterPrefix + "Page", Actions.ForPages.ManageCategories, "U.User", Value.Grant),
					new AclEntry(Actions.ForPages.ResourceMasterPrefix + "Page", Actions.ForPages.DeleteAttachments, "G.Group", Value.Grant),
					new AclEntry(Actions.ForPages.ResourceMasterPrefix + "Page", Actions.ForPages.DeleteAttachments, "U.User", Value.Deny) });

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			Collectors.SettingsProvider = prov;

			SubjectInfo[] infos = AuthReader.RetrieveSubjectsForPage(new PageInfo("NS.Page", null, DateTime.Now));

			Assert.AreEqual(3, infos.Length, "Wrong info count");

			Array.Sort(infos, delegate(SubjectInfo x, SubjectInfo y) { return x.Name.CompareTo(y.Name); });
			Assert.AreEqual("Group", infos[0].Name, "Wrong subject name");
			Assert.AreEqual(SubjectType.Group, infos[0].Type, "Wrong subject type");
			Assert.AreEqual("User", infos[1].Name, "Wrong subject name");
			Assert.AreEqual(SubjectType.User, infos[1].Type, "Wrong subject type");
			Assert.AreEqual("User1", infos[2].Name, "Wrong subject name");
			Assert.AreEqual(SubjectType.User, infos[2].Type, "Wrong subject type");

			mocks.Verify(prov);
			mocks.Verify(aclManager);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RetrieveSubjectsForPage_NullPage() {
			Collectors.SettingsProvider = MockProvider();

			AuthReader.RetrieveSubjectsForPage(null);
		}

		[Test]
		public void RetrieveGrantsForPage_Group() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV30 prov = mocks.DynamicMock<ISettingsStorageProviderV30>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			Expect.Call(aclManager.RetrieveEntriesForSubject("G.Group")).Return(
				new AclEntry[] {
					new AclEntry(Actions.ForPages.ResourceMasterPrefix + "Page", Actions.ForPages.ModifyPage, "G.Group", Value.Grant),
					new AclEntry(Actions.ForPages.ResourceMasterPrefix + "Page", Actions.FullControl, "G.Group", Value.Deny),
					new AclEntry(Actions.ForPages.ResourceMasterPrefix + "NS.Blah", Actions.ForPages.ManagePage, "G.Group", Value.Grant)
				});

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			Collectors.SettingsProvider = prov;

			string[] grants = AuthReader.RetrieveGrantsForPage(new UserGroup("Group", "Group", null),
				new PageInfo("Page", null, DateTime.Now));

			Assert.AreEqual(1, grants.Length, "Wrong grant count");
			Assert.AreEqual(Actions.ForPages.ModifyPage, grants[0], "Wrong grant");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RetrieveGrantsForPage_Group_NullGroup() {
			Collectors.SettingsProvider = MockProvider();

			AuthReader.RetrieveGrantsForPage(null as UserGroup, new PageInfo("Page", null, DateTime.Now));
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RetrieveGrantsForPage_Group_NullPage() {
			Collectors.SettingsProvider = MockProvider();

			AuthReader.RetrieveGrantsForPage(new UserGroup("Group", "Group", null), null);
		}

		[Test]
		public void RetrieveGrantsForPage_User() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV30 prov = mocks.DynamicMock<ISettingsStorageProviderV30>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			Expect.Call(aclManager.RetrieveEntriesForSubject("U.User")).Return(
				new AclEntry[] {
					new AclEntry(Actions.ForPages.ResourceMasterPrefix + "Page", Actions.ForPages.ModifyPage, "U.User", Value.Grant),
					new AclEntry(Actions.ForPages.ResourceMasterPrefix + "Page", Actions.FullControl, "U.User", Value.Deny),
					new AclEntry(Actions.ForPages.ResourceMasterPrefix + "NS.Blah", Actions.ForPages.ManagePage, "U.User", Value.Grant)
				});

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			Collectors.SettingsProvider = prov;

			string[] grants = AuthReader.RetrieveGrantsForPage(new UserInfo("User", "User", "user@users.com", true, DateTime.Now, null),
				new PageInfo("Page", null, DateTime.Now));

			Assert.AreEqual(1, grants.Length, "Wrong grant count");
			Assert.AreEqual(Actions.ForPages.ModifyPage, grants[0], "Wrong grant");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RetrieveGrantsForPage_User_NullUser() {
			Collectors.SettingsProvider = MockProvider();

			AuthReader.RetrieveGrantsForPage(null as UserInfo, new PageInfo("Page", null, DateTime.Now));
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RetrieveGrantsForPage_User_NullPage() {
			Collectors.SettingsProvider = MockProvider();

			AuthReader.RetrieveGrantsForPage(new UserInfo("User", "User", "user@users.com", true, DateTime.Now, null), null);
		}

		[Test]
		public void RetrieveDenialsForPage_Group() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV30 prov = mocks.DynamicMock<ISettingsStorageProviderV30>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			Expect.Call(aclManager.RetrieveEntriesForSubject("G.Group")).Return(
				new AclEntry[] {
					new AclEntry(Actions.ForPages.ResourceMasterPrefix + "Page", Actions.ForPages.ModifyPage, "G.Group", Value.Deny),
					new AclEntry(Actions.ForPages.ResourceMasterPrefix + "Page", Actions.FullControl, "G.Group", Value.Grant),
					new AclEntry(Actions.ForPages.ResourceMasterPrefix + "NS.Blah", Actions.ForPages.ManagePage, "G.Group", Value.Deny)
				});

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			Collectors.SettingsProvider = prov;

			string[] grants = AuthReader.RetrieveDenialsForPage(new UserGroup("Group", "Group", null),
				new PageInfo("Page", null, DateTime.Now));

			Assert.AreEqual(1, grants.Length, "Wrong denial count");
			Assert.AreEqual(Actions.ForPages.ModifyPage, grants[0], "Wrong denial");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RetrieveDenialsForPage_Group_NullGroup() {
			Collectors.SettingsProvider = MockProvider();

			AuthReader.RetrieveDenialsForPage(null as UserGroup, new PageInfo("Page", null, DateTime.Now));
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RetrieveDenialsForPage_Group_NullPage() {
			Collectors.SettingsProvider = MockProvider();

			AuthReader.RetrieveDenialsForPage(new UserGroup("Group", "Group", null), null);
		}

		[Test]
		public void RetrieveDenialsForPage_User() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV30 prov = mocks.DynamicMock<ISettingsStorageProviderV30>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			Expect.Call(aclManager.RetrieveEntriesForSubject("U.User")).Return(
				new AclEntry[] {
					new AclEntry(Actions.ForPages.ResourceMasterPrefix + "Page", Actions.ForPages.ModifyPage, "U.User", Value.Deny),
					new AclEntry(Actions.ForPages.ResourceMasterPrefix + "Page", Actions.FullControl, "U.User", Value.Grant),
					new AclEntry(Actions.ForPages.ResourceMasterPrefix + "NS.Blah", Actions.ForPages.ManagePage, "U.User", Value.Deny)
				});

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			Collectors.SettingsProvider = prov;

			string[] grants = AuthReader.RetrieveDenialsForPage(new UserInfo("User", "User", "user@users.com", true, DateTime.Now, null),
				new PageInfo("Page", null, DateTime.Now));

			Assert.AreEqual(1, grants.Length, "Wrong denial count");
			Assert.AreEqual(Actions.ForPages.ModifyPage, grants[0], "Wrong denial");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RetrieveDenialsForPage_User_NullUser() {
			Collectors.SettingsProvider = MockProvider();

			AuthReader.RetrieveDenialsForPage(null as UserInfo, new PageInfo("Page", null, DateTime.Now));
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RetrieveDenialsForPage_User_NullPage() {
			Collectors.SettingsProvider = MockProvider();

			AuthReader.RetrieveDenialsForPage(new UserInfo("User", "User", "user@users.com", true, DateTime.Now, null), null);
		}

		[Test]
		public void RetrieveSubjectsForDirectory_Root() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV30 prov = mocks.DynamicMock<ISettingsStorageProviderV30>();
			IFilesStorageProviderV30 filesProv = mocks.DynamicMock<IFilesStorageProviderV30>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			string dirName = Actions.ForDirectories.ResourceMasterPrefix + AuthTools.GetDirectoryName(filesProv, "/");
			Expect.Call(aclManager.RetrieveEntriesForResource(dirName)).Return(
				new AclEntry[] {
					new AclEntry(dirName, Actions.ForDirectories.List, "U.User1", Value.Grant),
					new AclEntry(dirName, Actions.ForDirectories.UploadFiles, "U.User", Value.Grant),
					new AclEntry(dirName, Actions.ForDirectories.DownloadFiles, "G.Group", Value.Grant),
					new AclEntry(dirName, Actions.ForDirectories.CreateDirectories, "U.User", Value.Deny) });

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			Collectors.SettingsProvider = prov;

			SubjectInfo[] infos = AuthReader.RetrieveSubjectsForDirectory(filesProv, "/");

			Assert.AreEqual(3, infos.Length, "Wrong info count");

			Array.Sort(infos, delegate(SubjectInfo x, SubjectInfo y) { return x.Name.CompareTo(y.Name); });
			Assert.AreEqual("Group", infos[0].Name, "Wrong subject name");
			Assert.AreEqual(SubjectType.Group, infos[0].Type, "Wrong subject type");
			Assert.AreEqual("User", infos[1].Name, "Wrong subject name");
			Assert.AreEqual(SubjectType.User, infos[1].Type, "Wrong subject type");
			Assert.AreEqual("User1", infos[2].Name, "Wrong subject name");
			Assert.AreEqual(SubjectType.User, infos[2].Type, "Wrong subject type");

			mocks.Verify(prov);
			mocks.Verify(aclManager);
		}

		[Test]
		public void RetrieveSubjectsForDirectory_Sub() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV30 prov = mocks.DynamicMock<ISettingsStorageProviderV30>();
			IFilesStorageProviderV30 filesProv = mocks.DynamicMock<IFilesStorageProviderV30>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			string dirName = Actions.ForDirectories.ResourceMasterPrefix + AuthTools.GetDirectoryName(filesProv, "/Dir/Sub/");
			Expect.Call(aclManager.RetrieveEntriesForResource(dirName)).Return(
				new AclEntry[] {
					new AclEntry(dirName, Actions.ForDirectories.List, "U.User1", Value.Grant),
					new AclEntry(dirName, Actions.ForDirectories.UploadFiles, "U.User", Value.Grant),
					new AclEntry(dirName, Actions.ForDirectories.DownloadFiles, "G.Group", Value.Grant),
					new AclEntry(dirName, Actions.ForDirectories.CreateDirectories, "U.User", Value.Deny) });

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			Collectors.SettingsProvider = prov;

			SubjectInfo[] infos = AuthReader.RetrieveSubjectsForDirectory(filesProv, "/Dir/Sub/");

			Assert.AreEqual(3, infos.Length, "Wrong info count");

			Array.Sort(infos, delegate(SubjectInfo x, SubjectInfo y) { return x.Name.CompareTo(y.Name); });
			Assert.AreEqual("Group", infos[0].Name, "Wrong subject name");
			Assert.AreEqual(SubjectType.Group, infos[0].Type, "Wrong subject type");
			Assert.AreEqual("User", infos[1].Name, "Wrong subject name");
			Assert.AreEqual(SubjectType.User, infos[1].Type, "Wrong subject type");
			Assert.AreEqual("User1", infos[2].Name, "Wrong subject name");
			Assert.AreEqual(SubjectType.User, infos[2].Type, "Wrong subject type");

			mocks.Verify(prov);
			mocks.Verify(aclManager);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RetrieveSubjectsForDirectory_NullProvider() {
			Collectors.SettingsProvider = MockProvider();

			AuthReader.RetrieveSubjectsForDirectory(null, "/");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void RetrieveSubjectsForDirectory_InvalidDirectory(string d) {
			Collectors.SettingsProvider = MockProvider();

			IFilesStorageProviderV30 fProv = mocks.DynamicMock<IFilesStorageProviderV30>();
			mocks.Replay(fProv);
			AuthReader.RetrieveSubjectsForDirectory(fProv, d);
		}

		[Test]
		public void RetrieveGrantsForDirectory_Root_Group() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV30 prov = mocks.DynamicMock<ISettingsStorageProviderV30>();
			IFilesStorageProviderV30 filesProv = mocks.DynamicMock<IFilesStorageProviderV30>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			string dirName = Actions.ForDirectories.ResourceMasterPrefix + AuthTools.GetDirectoryName(filesProv, "/");
			Expect.Call(aclManager.RetrieveEntriesForSubject("G.Group")).Return(
				new AclEntry[] {
					new AclEntry(dirName, Actions.ForDirectories.List, "G.Group", Value.Grant),
					new AclEntry(dirName, Actions.FullControl, "G.Group", Value.Deny),
					new AclEntry("D." + AuthTools.GetDirectoryName(filesProv, "/Other/"), Actions.ForDirectories.UploadFiles, "G.Group", Value.Grant)
				});

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			Collectors.SettingsProvider = prov;

			string[] grants = AuthReader.RetrieveGrantsForDirectory(new UserGroup("Group", "Group", null),
				filesProv, "/");

			Assert.AreEqual(1, grants.Length, "Wrong grant count");
			Assert.AreEqual(Actions.ForDirectories.List, grants[0], "Wrong grant");
		}

		[Test]
		public void RetrieveGrantsForDirectory_Sub_Group() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV30 prov = mocks.DynamicMock<ISettingsStorageProviderV30>();
			IFilesStorageProviderV30 filesProv = mocks.DynamicMock<IFilesStorageProviderV30>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			string dirName = Actions.ForDirectories.ResourceMasterPrefix + AuthTools.GetDirectoryName(filesProv, "/Dir/Sub/");
			Expect.Call(aclManager.RetrieveEntriesForSubject("G.Group")).Return(
				new AclEntry[] {
					new AclEntry(dirName, Actions.ForDirectories.List, "G.Group", Value.Grant),
					new AclEntry(dirName, Actions.FullControl, "G.Group", Value.Deny),
					new AclEntry("D." + AuthTools.GetDirectoryName(filesProv, "/"), Actions.ForDirectories.UploadFiles, "G.Group", Value.Grant)
				});

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			Collectors.SettingsProvider = prov;

			string[] grants = AuthReader.RetrieveGrantsForDirectory(new UserGroup("Group", "Group", null),
				filesProv, "/Dir/Sub/");

			Assert.AreEqual(1, grants.Length, "Wrong grant count");
			Assert.AreEqual(Actions.ForDirectories.List, grants[0], "Wrong grant");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RetrieveGrantsForDirectory_Group_NullGroup() {
			Collectors.SettingsProvider = MockProvider();

			IFilesStorageProviderV30 fProv = mocks.DynamicMock<IFilesStorageProviderV30>();
			mocks.Replay(fProv);
			AuthReader.RetrieveGrantsForDirectory(null as UserGroup, fProv, "/");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RetrieveGrantsForDirectory_Group_NullProvider() {
			Collectors.SettingsProvider = MockProvider();

			AuthReader.RetrieveGrantsForDirectory(new UserGroup("Group", "Group", null), null, "/");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void RetrieveGrantsForDirectory_Group_InvalidDirectory(string d) {
			Collectors.SettingsProvider = MockProvider();

			IFilesStorageProviderV30 fProv = mocks.DynamicMock<IFilesStorageProviderV30>();
			mocks.Replay(fProv);
			AuthReader.RetrieveGrantsForDirectory(new UserGroup("Group", "Group", null), fProv, d);	
		}

		[Test]
		public void RetrieveGrantsForDirectory_Root_User() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV30 prov = mocks.DynamicMock<ISettingsStorageProviderV30>();
			IFilesStorageProviderV30 filesProv = mocks.DynamicMock<IFilesStorageProviderV30>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			string dirName = Actions.ForDirectories.ResourceMasterPrefix + AuthTools.GetDirectoryName(filesProv, "/");
			Expect.Call(aclManager.RetrieveEntriesForSubject("U.User")).Return(
				new AclEntry[] {
					new AclEntry(dirName, Actions.ForDirectories.UploadFiles, "U.User", Value.Grant),
					new AclEntry(dirName, Actions.FullControl, "U.User", Value.Deny),
					new AclEntry("D." + AuthTools.GetDirectoryName(filesProv, "/Other/"), Actions.ForDirectories.List, "U.User", Value.Grant)
				});

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			Collectors.SettingsProvider = prov;

			string[] grants = AuthReader.RetrieveGrantsForDirectory(new UserInfo("User", "User", "user@users.com", true, DateTime.Now, null),
				filesProv, "/");

			Assert.AreEqual(1, grants.Length, "Wrong grant count");
			Assert.AreEqual(Actions.ForDirectories.UploadFiles, grants[0], "Wrong grant");
		}

		[Test]
		public void RetrieveGrantsForDirectory_Sub_User() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV30 prov = mocks.DynamicMock<ISettingsStorageProviderV30>();
			IFilesStorageProviderV30 filesProv = mocks.DynamicMock<IFilesStorageProviderV30>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			string dirName = Actions.ForDirectories.ResourceMasterPrefix + AuthTools.GetDirectoryName(filesProv, "/Dir/Sub/");
			Expect.Call(aclManager.RetrieveEntriesForSubject("U.User")).Return(
				new AclEntry[] {
					new AclEntry(dirName, Actions.ForDirectories.UploadFiles, "U.User", Value.Grant),
					new AclEntry(dirName, Actions.FullControl, "U.User", Value.Deny),
					new AclEntry("D." + AuthTools.GetDirectoryName(filesProv, "/"), Actions.ForDirectories.List, "U.User", Value.Grant)
				});

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			Collectors.SettingsProvider = prov;

			string[] grants = AuthReader.RetrieveGrantsForDirectory(new UserInfo("User", "User", "user@users.com", true, DateTime.Now, null),
				filesProv, "/Dir/Sub/");

			Assert.AreEqual(1, grants.Length, "Wrong grant count");
			Assert.AreEqual(Actions.ForDirectories.UploadFiles, grants[0], "Wrong grant");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RetrieveGrantsForDirectory_User_NullUser() {
			Collectors.SettingsProvider = MockProvider();

			IFilesStorageProviderV30 fProv = mocks.DynamicMock<IFilesStorageProviderV30>();
			mocks.Replay(fProv);
			AuthReader.RetrieveGrantsForDirectory(null as UserInfo, fProv, "/");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RetrieveGrantsForDirectory_User_NullProvider() {
			Collectors.SettingsProvider = MockProvider();

			AuthReader.RetrieveGrantsForDirectory(new UserInfo("User", "User", "user@users.com", true, DateTime.Now, null),
				null, "/");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void RetrieveGrantsForDirectory_User_InvalidDirectory(string d) {
			Collectors.SettingsProvider = MockProvider();

			IFilesStorageProviderV30 fProv = mocks.DynamicMock<IFilesStorageProviderV30>();
			mocks.Replay(fProv);
			AuthReader.RetrieveGrantsForDirectory(new UserInfo("User", "User", "user@users.com", true, DateTime.Now, null), fProv, d);
		}

		[Test]
		public void RetrieveDenialsForDirectory_Root_Group() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV30 prov = mocks.DynamicMock<ISettingsStorageProviderV30>();
			IFilesStorageProviderV30 filesProv = mocks.DynamicMock<IFilesStorageProviderV30>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			string dirName = Actions.ForDirectories.ResourceMasterPrefix + AuthTools.GetDirectoryName(filesProv, "/");
			Expect.Call(aclManager.RetrieveEntriesForSubject("G.Group")).Return(
				new AclEntry[] {
					new AclEntry(dirName, Actions.ForDirectories.List, "G.Group", Value.Deny),
					new AclEntry(dirName, Actions.FullControl, "G.Group", Value.Grant),
					new AclEntry("D." + AuthTools.GetDirectoryName(filesProv, "/Other/"), Actions.ForDirectories.UploadFiles, "G.Group", Value.Deny)
				});

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			Collectors.SettingsProvider = prov;

			string[] grants = AuthReader.RetrieveDenialsForDirectory(new UserGroup("Group", "Group", null),
				filesProv, "/");

			Assert.AreEqual(1, grants.Length, "Wrong denial count");
			Assert.AreEqual(Actions.ForDirectories.List, grants[0], "Wrong denial");
		}

		[Test]
		public void RetrieveDenialsForDirectory_Sub_Group() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV30 prov = mocks.DynamicMock<ISettingsStorageProviderV30>();
			IFilesStorageProviderV30 filesProv = mocks.DynamicMock<IFilesStorageProviderV30>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			string dirName = Actions.ForDirectories.ResourceMasterPrefix + AuthTools.GetDirectoryName(filesProv, "/Dir/Sub/");
			Expect.Call(aclManager.RetrieveEntriesForSubject("G.Group")).Return(
				new AclEntry[] {
					new AclEntry(dirName, Actions.ForDirectories.List, "G.Group", Value.Deny),
					new AclEntry(dirName, Actions.FullControl, "G.Group", Value.Grant),
					new AclEntry("D." + AuthTools.GetDirectoryName(filesProv, "/"), Actions.ForDirectories.UploadFiles, "G.Group", Value.Deny)
				});

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			Collectors.SettingsProvider = prov;

			string[] grants = AuthReader.RetrieveDenialsForDirectory(new UserGroup("Group", "Group", null),
				filesProv, "/Dir/Sub/");

			Assert.AreEqual(1, grants.Length, "Wrong denial count");
			Assert.AreEqual(Actions.ForDirectories.List, grants[0], "Wrong denial");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RetrieveDenialsForDirectory_Group_NullGroup() {
			Collectors.SettingsProvider = MockProvider();

			IFilesStorageProviderV30 fProv = mocks.DynamicMock<IFilesStorageProviderV30>();
			mocks.Replay(fProv);
			AuthReader.RetrieveDenialsForDirectory(null as UserGroup, fProv, "/");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RetrieveDenialsForDirectory_Group_NullProvider() {
			Collectors.SettingsProvider = MockProvider();

			AuthReader.RetrieveDenialsForDirectory(new UserGroup("Group", "Group", null), null, "/");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void RetrieveDenialsForDirectory_Group_InvalidDirectory(string d) {
			Collectors.SettingsProvider = MockProvider();

			IFilesStorageProviderV30 fProv = mocks.DynamicMock<IFilesStorageProviderV30>();
			mocks.Replay(fProv);
			AuthReader.RetrieveDenialsForDirectory(new UserGroup("Group", "Group", null), fProv, d);
		}

		[Test]
		public void RetrieveDenialsForDirectory_Root_User() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV30 prov = mocks.DynamicMock<ISettingsStorageProviderV30>();
			IFilesStorageProviderV30 filesProv = mocks.DynamicMock<IFilesStorageProviderV30>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			string dirName = Actions.ForDirectories.ResourceMasterPrefix + AuthTools.GetDirectoryName(filesProv, "/");
			Expect.Call(aclManager.RetrieveEntriesForSubject("U.User")).Return(
				new AclEntry[] {
					new AclEntry(dirName, Actions.ForDirectories.UploadFiles, "U.User", Value.Deny),
					new AclEntry(dirName, Actions.FullControl, "U.User", Value.Grant),
					new AclEntry("D." + AuthTools.GetDirectoryName(filesProv, "/Other/"), Actions.ForDirectories.List, "U.User", Value.Deny)
				});

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			Collectors.SettingsProvider = prov;

			string[] grants = AuthReader.RetrieveDenialsForDirectory(new UserInfo("User", "User", "user@users.com", true, DateTime.Now, null),
				filesProv, "/");

			Assert.AreEqual(1, grants.Length, "Wrong denial count");
			Assert.AreEqual(Actions.ForDirectories.UploadFiles, grants[0], "Wrong denial");
		}

		[Test]
		public void RetrieveDenialsForDirectory_Sub_User() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV30 prov = mocks.DynamicMock<ISettingsStorageProviderV30>();
			IFilesStorageProviderV30 filesProv = mocks.DynamicMock<IFilesStorageProviderV30>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			string dirName = Actions.ForDirectories.ResourceMasterPrefix + AuthTools.GetDirectoryName(filesProv, "/Dir/Sub/");
			Expect.Call(aclManager.RetrieveEntriesForSubject("U.User")).Return(
				new AclEntry[] {
					new AclEntry(dirName, Actions.ForDirectories.UploadFiles, "U.User", Value.Deny),
					new AclEntry(dirName, Actions.FullControl, "U.User", Value.Grant),
					new AclEntry("D." + AuthTools.GetDirectoryName(filesProv, "/"), Actions.ForDirectories.List, "U.User", Value.Deny)
				});

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			Collectors.SettingsProvider = prov;

			string[] grants = AuthReader.RetrieveDenialsForDirectory(new UserInfo("User", "User", "user@users.com", true, DateTime.Now, null),
				filesProv, "/Dir/Sub/");

			Assert.AreEqual(1, grants.Length, "Wrong denial count");
			Assert.AreEqual(Actions.ForDirectories.UploadFiles, grants[0], "Wrong denial");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RetrieveDenialsForDirectory_User_NullUser() {
			Collectors.SettingsProvider = MockProvider();

			IFilesStorageProviderV30 fProv = mocks.DynamicMock<IFilesStorageProviderV30>();
			mocks.Replay(fProv);
			AuthReader.RetrieveDenialsForDirectory(null as UserInfo, fProv, "/");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RetrieveDenialsForDirectory_User_NullProvider() {
			Collectors.SettingsProvider = MockProvider();

			AuthReader.RetrieveDenialsForDirectory(new UserInfo("User", "User", "user@users.com", true, DateTime.Now, null),
				null, "/");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void RetrieveDenialsForDirectory_User_InvalidDirectory(string d) {
			Collectors.SettingsProvider = MockProvider();

			IFilesStorageProviderV30 fProv = mocks.DynamicMock<IFilesStorageProviderV30>();
			mocks.Replay(fProv);
			AuthReader.RetrieveDenialsForDirectory(new UserInfo("User", "User", "user@users.com", true, DateTime.Now, null), fProv, d);
		}

	}

}
