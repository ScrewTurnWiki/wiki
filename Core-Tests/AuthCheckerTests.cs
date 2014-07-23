
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using Rhino.Mocks;
using ScrewTurn.Wiki.PluginFramework;
using ScrewTurn.Wiki.AclEngine;

namespace ScrewTurn.Wiki.Tests {

	[TestFixture]
	public class AuthCheckerTests {

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

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		[TestCase("Blah", ExpectedException = typeof(ArgumentException))]
		[TestCase("*", ExpectedException = typeof(ArgumentException))]
		public void CheckActionForGlobals_InvalidAction(string a) {
			Collectors.SettingsProvider = MockProvider();
			AuthChecker.CheckActionForGlobals(a, "User", new string[0]);
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void CheckActionForGlobals_InvalidUser(string u) {
			Collectors.SettingsProvider = MockProvider();
			AuthChecker.CheckActionForGlobals(Actions.ForGlobals.ManageAccounts, u, new string[0]);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void CheckActionForGlobals_NullGroups() {
			Collectors.SettingsProvider = MockProvider();
			AuthChecker.CheckActionForGlobals(Actions.ForGlobals.ManageAccounts, "User", null);
		}

		[Test]
		public void CheckActionForGlobals_AdminBypass() {
			Collectors.SettingsProvider = MockProvider();
			Assert.IsTrue(AuthChecker.CheckActionForGlobals(Actions.ForGlobals.ManageAccounts, "admin", new string[0]), "Admin account should bypass security");
		}

		[Test]
		public void CheckActionForGlobals_GrantUserExplicit() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.ForGlobals.ManageFiles, "U.User", Value.Grant));
			entries.Add(new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.ForGlobals.ManageFiles, "U.User10", Value.Deny));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsFalse(AuthChecker.CheckActionForGlobals(Actions.ForGlobals.ManageGroups, "User", new string[] { "Group" }), "Permission should be denied");
			Assert.IsTrue(AuthChecker.CheckActionForGlobals(Actions.ForGlobals.ManageFiles, "User", new string[] { "Group" }), "Permission should be granted");
			Assert.IsFalse(AuthChecker.CheckActionForGlobals(Actions.ForGlobals.ManageFiles, "User2", new string[] { "Group" }), "Permission should be denied");
		}

		[Test]
		public void CheckActionForGlobals_GrantUserFullControl() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.FullControl, "U.User", Value.Grant));
			entries.Add(new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.FullControl, "U.User10", Value.Deny));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsTrue(AuthChecker.CheckActionForGlobals(Actions.ForGlobals.ManageGroups, "User", new string[] { "Group" }), "Permission should be granted");
			Assert.IsTrue(AuthChecker.CheckActionForGlobals(Actions.ForGlobals.ManageFiles, "User", new string[] { "Group" }), "Permission should be granted");
			Assert.IsFalse(AuthChecker.CheckActionForGlobals(Actions.ForGlobals.ManageGroups, "User2", new string[] { "Group" }), "Permission should be denied");
		}

		[Test]
		public void CheckActionForGlobals_GrantGroupExplicit() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.ForGlobals.ManageFiles, "G.Group", Value.Grant));
			entries.Add(new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.ForGlobals.ManageFiles, "G.Group10", Value.Deny));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsFalse(AuthChecker.CheckActionForGlobals(Actions.ForGlobals.ManageGroups, "User", new string[] { "Group" }), "Permission should be denied");
			Assert.IsTrue(AuthChecker.CheckActionForGlobals(Actions.ForGlobals.ManageFiles, "User", new string[] { "Group" }), "Permission should be granted");
			Assert.IsFalse(AuthChecker.CheckActionForGlobals(Actions.ForGlobals.ManageFiles, "User", new string[] { "Group2" }), "Permission should be denied");
		}

		[Test]
		public void CheckActionForGlobals_GrantGroupFullControl() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.FullControl, "G.Group", Value.Grant));
			entries.Add(new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.FullControl, "G.Group10", Value.Deny));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsTrue(AuthChecker.CheckActionForGlobals(Actions.ForGlobals.ManageGroups, "User", new string[] { "Group" }), "Permission should be granted");
			Assert.IsTrue(AuthChecker.CheckActionForGlobals(Actions.ForGlobals.ManageFiles, "User", new string[] { "Group" }), "Permission should be granted");
			Assert.IsFalse(AuthChecker.CheckActionForGlobals(Actions.ForGlobals.ManageGroups, "User", new string[] { "Group2" }), "Permission should be denied");
		}

		[Test]
		public void CheckActionForGlobals_DenyUserExplicit() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.ForGlobals.ManageFiles, "U.User", Value.Deny));
			entries.Add(new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.ForGlobals.ManageFiles, "U.User10", Value.Grant));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsFalse(AuthChecker.CheckActionForGlobals(Actions.ForGlobals.ManageGroups, "User", new string[] { "Group" }), "Permission should be denied");
			Assert.IsFalse(AuthChecker.CheckActionForGlobals(Actions.ForGlobals.ManageFiles, "User", new string[] { "Group" }), "Permission should be denied");
			Assert.IsFalse(AuthChecker.CheckActionForGlobals(Actions.ForGlobals.ManageFiles, "User2", new string[] { "Group" }), "Permission should be denied");
		}

		[Test]
		public void CheckActionForGlobals_DenyUserFullControl() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.FullControl, "U.User", Value.Deny));
			entries.Add(new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.FullControl, "U.User10", Value.Grant));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsFalse(AuthChecker.CheckActionForGlobals(Actions.ForGlobals.ManageGroups, "User", new string[] { "Group" }), "Permission should be denied");
			Assert.IsFalse(AuthChecker.CheckActionForGlobals(Actions.ForGlobals.ManageFiles, "User", new string[] { "Group" }), "Permission should be denied");
			Assert.IsFalse(AuthChecker.CheckActionForGlobals(Actions.ForGlobals.ManageFiles, "User2", new string[] { "Group" }), "Permission should be denied");
		}

		[Test]
		public void CheckActionForGlobals_DenyGroupExplicit() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.ForGlobals.ManageFiles, "G.Group", Value.Deny));
			entries.Add(new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.ForGlobals.ManageFiles, "G.Group10", Value.Grant));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsFalse(AuthChecker.CheckActionForGlobals(Actions.ForGlobals.ManageGroups, "User", new string[] { "Group" }), "Permission should be denied");
			Assert.IsFalse(AuthChecker.CheckActionForGlobals(Actions.ForGlobals.ManageFiles, "User", new string[] { "Group" }), "Permission should be denied");
			Assert.IsFalse(AuthChecker.CheckActionForGlobals(Actions.ForGlobals.ManageFiles, "User2", new string[] { "Group" }), "Permission should be denied");
		}

		[Test]
		public void CheckActionForGlobals_DenyGroupFullControl() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.FullControl, "G.Group", Value.Deny));
			entries.Add(new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.FullControl, "G.Group10", Value.Grant));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsFalse(AuthChecker.CheckActionForGlobals(Actions.ForGlobals.ManageGroups, "User", new string[] { "Group" }), "Permission should be denied");
			Assert.IsFalse(AuthChecker.CheckActionForGlobals(Actions.ForGlobals.ManageFiles, "User", new string[] { "Group" }), "Permission should be denied");
			Assert.IsFalse(AuthChecker.CheckActionForGlobals(Actions.ForGlobals.ManageFiles, "User2", new string[] { "Group" }), "Permission should be denied");
		}

		[Test]
		public void CheckActionForNamespace_NullNamespace() {
			// No exceptions should be thrown
			Collectors.SettingsProvider = MockProvider();
			AuthChecker.CheckActionForNamespace(null, Actions.ForNamespaces.CreatePages, "User", new string[0]);
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		[TestCase("Blah", ExpectedException = typeof(ArgumentException))]
		[TestCase("*", ExpectedException = typeof(ArgumentException))]
		public void CheckActionForNamespace_InvalidAction(string a) {
			Collectors.SettingsProvider = MockProvider();
			AuthChecker.CheckActionForNamespace(new NamespaceInfo("NS", null, null), a, "User", new string[0]);
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void CheckActionForNamespace_InvalidUser(string u) {
			Collectors.SettingsProvider = MockProvider();
			AuthChecker.CheckActionForNamespace(new NamespaceInfo("NS", null, null), Actions.ForNamespaces.ManagePages, u, new string[0]);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void CheckActionForNamespace_NullGroups() {
			Collectors.SettingsProvider = MockProvider();
			AuthChecker.CheckActionForNamespace(new NamespaceInfo("NS", null, null), Actions.ForNamespaces.ManagePages, "User", null);
		}

		[Test]
		public void CheckActionForNamespace_AdminBypass() {
			Collectors.SettingsProvider = MockProvider();
			Assert.IsTrue(AuthChecker.CheckActionForNamespace(null, Actions.ForNamespaces.CreatePages, "admin", new string[0]), "Admin account should bypass security");
		}

		[Test]
		public void CheckActionForNamespace_GrantUserExplicit() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix + "Namespace", Actions.ForNamespaces.ManagePages, "U.User", Value.Grant));
			entries.Add(new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix + "Namespace", Actions.ForNamespaces.ManagePages, "U.User100", Value.Deny));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsFalse(AuthChecker.CheckActionForNamespace(new NamespaceInfo("Namespace", null, null), Actions.ForNamespaces.ManageCategories, "User", new string[] { "Group" }), "Permission should be denied");
			Assert.IsTrue(AuthChecker.CheckActionForNamespace(new NamespaceInfo("Namespace", null, null), Actions.ForNamespaces.ManagePages, "User", new string[] { "Group" }), "Permission should be granted");
			Assert.IsFalse(AuthChecker.CheckActionForNamespace(new NamespaceInfo("Namespace", null, null), Actions.ForNamespaces.ManagePages, "User2", new string[] { "Group" }), "Permission should be denied");
			Assert.IsFalse(AuthChecker.CheckActionForNamespace(new NamespaceInfo("Namespace2", null, null), Actions.ForNamespaces.ManagePages, "User", new string[] { "Group" }), "Permission should be denied");
		}

		[Test]
		public void CheckActionForNamespace_DenyUserExplicit() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix + "Namespace", Actions.ForNamespaces.ManagePages, "U.User", Value.Deny));
			entries.Add(new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix + "Namespace", Actions.ForNamespaces.ManagePages, "U.User100", Value.Grant));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsFalse(AuthChecker.CheckActionForNamespace(new NamespaceInfo("Namespace", null, null), Actions.ForNamespaces.ManageCategories, "User", new string[] { "Group" }), "Permission should be denied");
			Assert.IsFalse(AuthChecker.CheckActionForNamespace(new NamespaceInfo("Namespace", null, null), Actions.ForNamespaces.ManagePages, "User", new string[] { "Group" }), "Permission should be denied");
			Assert.IsFalse(AuthChecker.CheckActionForNamespace(new NamespaceInfo("Namespace", null, null), Actions.ForNamespaces.ManagePages, "User2", new string[] { "Group" }), "Permission should be denied");
			Assert.IsFalse(AuthChecker.CheckActionForNamespace(new NamespaceInfo("Namespace2", null, null), Actions.ForNamespaces.ManagePages, "User", new string[] { "Group" }), "Permission should be denied");
		}

		[Test]
		public void CheckActionForNamespace_GrantUserFullControl() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix + "Namespace", Actions.FullControl, "U.User", Value.Grant));
			entries.Add(new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix + "Namespace", Actions.FullControl, "U.User100", Value.Deny));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsTrue(AuthChecker.CheckActionForNamespace(new NamespaceInfo("Namespace", null, null), Actions.ForNamespaces.ManageCategories, "User", new string[] { "Group" }), "Permission should be granted");
			Assert.IsTrue(AuthChecker.CheckActionForNamespace(new NamespaceInfo("Namespace", null, null), Actions.ForNamespaces.ManagePages, "User", new string[] { "Group" }), "Permission should be granted");
			Assert.IsFalse(AuthChecker.CheckActionForNamespace(new NamespaceInfo("Namespace", null, null), Actions.ForNamespaces.ManagePages, "User2", new string[] { "Group" }), "Permission should be denied");
			Assert.IsFalse(AuthChecker.CheckActionForNamespace(new NamespaceInfo("Namespace2", null, null), Actions.ForNamespaces.ManagePages, "User", new string[] { "Group" }), "Permission should be denied");
		}

		[Test]
		public void CheckActionForNamespace_DenyUserFullControl() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix + "Namespace", Actions.FullControl, "U.User", Value.Deny));
			entries.Add(new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix + "Namespace", Actions.FullControl, "U.User100", Value.Grant));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsFalse(AuthChecker.CheckActionForNamespace(new NamespaceInfo("Namespace", null, null), Actions.ForNamespaces.ManageCategories, "User", new string[] { "Group" }), "Permission should be denied");
			Assert.IsFalse(AuthChecker.CheckActionForNamespace(new NamespaceInfo("Namespace", null, null), Actions.ForNamespaces.ManagePages, "User", new string[] { "Group" }), "Permission should be denied");
			Assert.IsFalse(AuthChecker.CheckActionForNamespace(new NamespaceInfo("Namespace", null, null), Actions.ForNamespaces.ManagePages, "User2", new string[] { "Group" }), "Permission should be denied");
			Assert.IsFalse(AuthChecker.CheckActionForNamespace(new NamespaceInfo("Namespace2", null, null), Actions.ForNamespaces.ManagePages, "User", new string[] { "Group" }), "Permission should be denied");
		}

		[Test]
		public void CheckActionForNamespace_GrantGroupExplicit() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix + "Namespace", Actions.ForNamespaces.ManagePages, "G.Group", Value.Grant));
			entries.Add(new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix + "Namespace", Actions.ForNamespaces.ManagePages, "G.Group100", Value.Deny));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsFalse(AuthChecker.CheckActionForNamespace(new NamespaceInfo("Namespace", null, null), Actions.ForNamespaces.ManageCategories, "User", new string[] { "Group" }), "Permission should be denied");
			Assert.IsTrue(AuthChecker.CheckActionForNamespace(new NamespaceInfo("Namespace", null, null), Actions.ForNamespaces.ManagePages, "User", new string[] { "Group" }), "Permission should be granted");
			Assert.IsFalse(AuthChecker.CheckActionForNamespace(new NamespaceInfo("Namespace", null, null), Actions.ForNamespaces.ManagePages, "User", new string[] { "Group2" }), "Permission should be denied");
			Assert.IsFalse(AuthChecker.CheckActionForNamespace(new NamespaceInfo("Namespace2", null, null), Actions.ForNamespaces.ManagePages, "User", new string[] { "Group" }), "Permission should be denied");
		}

		[Test]
		public void CheckActionForNamespace_DenyGroupExplicit() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix + "Namespace", Actions.ForNamespaces.ManagePages, "G.Group", Value.Deny));
			entries.Add(new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix + "Namespace", Actions.ForNamespaces.ManagePages, "G.Group100", Value.Grant));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsFalse(AuthChecker.CheckActionForNamespace(new NamespaceInfo("Namespace", null, null), Actions.ForNamespaces.ManageCategories, "User", new string[] { "Group" }), "Permission should be denied");
			Assert.IsFalse(AuthChecker.CheckActionForNamespace(new NamespaceInfo("Namespace", null, null), Actions.ForNamespaces.ManagePages, "User", new string[] { "Group" }), "Permission should be denied");
			Assert.IsFalse(AuthChecker.CheckActionForNamespace(new NamespaceInfo("Namespace", null, null), Actions.ForNamespaces.ManagePages, "User", new string[] { "Group2" }), "Permission should be denied");
			Assert.IsFalse(AuthChecker.CheckActionForNamespace(new NamespaceInfo("Namespace2", null, null), Actions.ForNamespaces.ManagePages, "User", new string[] { "Group" }), "Permission should be denied");
		}

		[Test]
		public void CheckActionForNamespace_GrantGroupFullControl() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix + "Namespace", Actions.FullControl, "G.Group", Value.Grant));
			entries.Add(new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix + "Namespace", Actions.FullControl, "G.Group100", Value.Deny));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsTrue(AuthChecker.CheckActionForNamespace(new NamespaceInfo("Namespace", null, null), Actions.ForNamespaces.ManageCategories, "User", new string[] { "Group" }), "Permission should be granted");
			Assert.IsTrue(AuthChecker.CheckActionForNamespace(new NamespaceInfo("Namespace", null, null), Actions.ForNamespaces.ManagePages, "User", new string[] { "Group" }), "Permission should be granted");
			Assert.IsFalse(AuthChecker.CheckActionForNamespace(new NamespaceInfo("Namespace", null, null), Actions.ForNamespaces.ManagePages, "User", new string[] { "Group2" }), "Permission should be denied");
			Assert.IsFalse(AuthChecker.CheckActionForNamespace(new NamespaceInfo("Namespace2", null, null), Actions.ForNamespaces.ManagePages, "User", new string[] { "Group" }), "Permission should be denied");
		}

		[Test]
		public void CheckActionForNamespace_DenyGroupFullControl() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix + "Namespace", Actions.FullControl, "G.Group", Value.Deny));
			entries.Add(new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix + "Namespace", Actions.FullControl, "G.Group100", Value.Grant));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsFalse(AuthChecker.CheckActionForNamespace(new NamespaceInfo("Namespace", null, null), Actions.ForNamespaces.ManageCategories, "User", new string[] { "Group" }), "Permission should be denied");
			Assert.IsFalse(AuthChecker.CheckActionForNamespace(new NamespaceInfo("Namespace", null, null), Actions.ForNamespaces.ManagePages, "User", new string[] { "Group" }), "Permission should be denied");
			Assert.IsFalse(AuthChecker.CheckActionForNamespace(new NamespaceInfo("Namespace", null, null), Actions.ForNamespaces.ManagePages, "User", new string[] { "Group2" }), "Permission should be denied");
			Assert.IsFalse(AuthChecker.CheckActionForNamespace(new NamespaceInfo("Namespace2", null, null), Actions.ForNamespaces.ManagePages, "User", new string[] { "Group" }), "Permission should be denied");
		}

		[Test]
		public void CheckActionForNamespace_GrantUserGlobalEscalator() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.ForGlobals.ManagePagesAndCategories, "U.User", Value.Grant));
			entries.Add(new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.ForGlobals.ManagePagesAndCategories, "U.User100", Value.Deny));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsTrue(AuthChecker.CheckActionForNamespace(new NamespaceInfo("Namespace", null, null), Actions.ForNamespaces.ManageCategories, "User", new string[] { "Group" }), "Permission should be granted");
			Assert.IsTrue(AuthChecker.CheckActionForNamespace(new NamespaceInfo("Namespace", null, null), Actions.ForNamespaces.ManagePages, "User", new string[] { "Group" }), "Permission should be granted");
			Assert.IsFalse(AuthChecker.CheckActionForNamespace(new NamespaceInfo("Namespace", null, null), Actions.ForNamespaces.ManagePages, "User2", new string[] { "Group" }), "Permission should be denied");
			Assert.IsTrue(AuthChecker.CheckActionForNamespace(new NamespaceInfo("Namespace2", null, null), Actions.ForNamespaces.ManagePages, "User", new string[] { "Group" }), "Permission should be granted");
		}

		[Test]
		public void CheckActionForNamespace_GrantGroupGlobalEscalator() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.ForGlobals.ManagePagesAndCategories, "G.Group", Value.Grant));
			entries.Add(new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.ForGlobals.ManagePagesAndCategories, "G.Group100", Value.Deny));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsTrue(AuthChecker.CheckActionForNamespace(new NamespaceInfo("Namespace", null, null), Actions.ForNamespaces.ManageCategories, "User", new string[] { "Group" }), "Permission should be granted");
			Assert.IsTrue(AuthChecker.CheckActionForNamespace(new NamespaceInfo("Namespace", null, null), Actions.ForNamespaces.ManagePages, "User", new string[] { "Group" }), "Permission should be granted");
			Assert.IsFalse(AuthChecker.CheckActionForNamespace(new NamespaceInfo("Namespace", null, null), Actions.ForNamespaces.ManagePages, "User", new string[] { "Group2" }), "Permission should be denied");
			Assert.IsTrue(AuthChecker.CheckActionForNamespace(new NamespaceInfo("Namespace2", null, null), Actions.ForNamespaces.ManagePages, "User", new string[] { "Group" }), "Permission should be granted");
		}

		[Test]
		public void CheckActionForNamespace_GrantUserGlobalFullControl() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.FullControl, "U.User", Value.Grant));
			entries.Add(new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.FullControl, "U.User100", Value.Deny));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsTrue(AuthChecker.CheckActionForNamespace(new NamespaceInfo("Namespace", null, null), Actions.ForNamespaces.ManageCategories, "User", new string[] { "Group" }), "Permission should be granted");
			Assert.IsTrue(AuthChecker.CheckActionForNamespace(new NamespaceInfo("Namespace", null, null), Actions.ForNamespaces.ManagePages, "User", new string[] { "Group" }), "Permission should be granted");
			Assert.IsFalse(AuthChecker.CheckActionForNamespace(new NamespaceInfo("Namespace", null, null), Actions.ForNamespaces.ManagePages, "User2", new string[] { "Group" }), "Permission should be denied");
			Assert.IsTrue(AuthChecker.CheckActionForNamespace(new NamespaceInfo("Namespace2", null, null), Actions.ForNamespaces.ManagePages, "User", new string[] { "Group" }), "Permission should be granted");
		}

		[Test]
		public void CheckActionForNamespace_GrantGroupGlobalFullControl() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.FullControl, "G.Group", Value.Grant));
			entries.Add(new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.FullControl, "G.Group100", Value.Deny));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsTrue(AuthChecker.CheckActionForNamespace(new NamespaceInfo("Namespace", null, null), Actions.ForNamespaces.ManageCategories, "User", new string[] { "Group" }), "Permission should be granted");
			Assert.IsTrue(AuthChecker.CheckActionForNamespace(new NamespaceInfo("Namespace", null, null), Actions.ForNamespaces.ManagePages, "User", new string[] { "Group" }), "Permission should be granted");
			Assert.IsFalse(AuthChecker.CheckActionForNamespace(new NamespaceInfo("Namespace", null, null), Actions.ForNamespaces.ManagePages, "User", new string[] { "Group2" }), "Permission should be denied");
			Assert.IsTrue(AuthChecker.CheckActionForNamespace(new NamespaceInfo("Namespace2", null, null), Actions.ForNamespaces.ManagePages, "User", new string[] { "Group" }), "Permission should be granted");
		}

		[Test]
		public void CheckActionForNamespace_GrantUserLocalEscalator() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix + "Namespace", Actions.ForNamespaces.ManagePages, "U.User", Value.Grant));
			entries.Add(new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix + "Namespace", Actions.ForNamespaces.ManagePages, "U.User100", Value.Deny));
			
			Collectors.SettingsProvider = MockProvider(entries);
			
			Assert.IsTrue(AuthChecker.CheckActionForNamespace(new NamespaceInfo("Namespace", null, null), Actions.ForNamespaces.ReadPages, "User", new string[] { "Group" }), "Permission should be granted");
			Assert.IsFalse(AuthChecker.CheckActionForNamespace(new NamespaceInfo("Namespace", null, null), Actions.ForNamespaces.ReadPages, "User2", new string[] { "Group" }), "Permission should be denied");
			Assert.IsFalse(AuthChecker.CheckActionForNamespace(new NamespaceInfo("Namespace2", null, null), Actions.ForNamespaces.ReadPages, "User", new string[] { "Group" }), "Permission should be denied");
		}

		[Test]
		public void CheckActionForNamespace_GrantGroupLocalEscalator() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix + "Namespace", Actions.ForNamespaces.ManagePages, "G.Group", Value.Grant));
			entries.Add(new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix + "Namespace", Actions.ForNamespaces.ManagePages, "G.Group100", Value.Deny));
			
			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsTrue(AuthChecker.CheckActionForNamespace(new NamespaceInfo("Namespace", null, null), Actions.ForNamespaces.ReadPages, "User", new string[] { "Group" }), "Permission should be granted");
			Assert.IsFalse(AuthChecker.CheckActionForNamespace(new NamespaceInfo("Namespace", null, null), Actions.ForNamespaces.ReadPages, "User", new string[] { "Group2" }), "Permission should be denied");
			Assert.IsFalse(AuthChecker.CheckActionForNamespace(new NamespaceInfo("Namespace2", null, null), Actions.ForNamespaces.ReadPages, "User", new string[] { "Group" }), "Permission should be denied");
		}

		[Test]
		public void CheckActionForNamespace_GrantUserGlobalEscalator_DenyUserExplicit() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.ForGlobals.ManageNamespaces, "U.User", Value.Grant));
			entries.Add(new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix + "NS", Actions.ForNamespaces.ModifyPages, "U.User", Value.Deny));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsFalse(AuthChecker.CheckActionForNamespace(new NamespaceInfo("NS", null, null), Actions.ForNamespaces.ModifyPages, "User", new string[] { "Group" }), "Permission should be denied");
		}

		[Test]
		public void CheckActionForNamespace_GrantGroupGlobalEscalator_DenyUserExplicit() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.ForGlobals.ManageNamespaces, "G.Group", Value.Grant));
			entries.Add(new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix + "NS", Actions.ForNamespaces.ModifyPages, "U.User", Value.Deny));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsFalse(AuthChecker.CheckActionForNamespace(new NamespaceInfo("NS", null, null), Actions.ForNamespaces.ModifyPages, "User", new string[] { "Group" }), "Permission should be denied");
		}

		[Test]
		public void CheckActionForNamespace_GrantUserGlobalFullControl_DenyUserExplicit() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.FullControl, "U.User", Value.Grant));
			entries.Add(new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix + "NS", Actions.ForNamespaces.ModifyPages, "U.User", Value.Deny));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsFalse(AuthChecker.CheckActionForNamespace(new NamespaceInfo("NS", null, null), Actions.ForNamespaces.ModifyPages, "User", new string[] { "Group" }), "Permission should be denied");
		}

		[Test]
		public void CheckActionForNamespace_GrantGroupGlobalFullControl_DenyUserExplicit() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.FullControl, "G.Group", Value.Grant));
			entries.Add(new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix + "NS", Actions.ForNamespaces.ModifyPages, "U.User", Value.Deny));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsFalse(AuthChecker.CheckActionForNamespace(new NamespaceInfo("NS", null, null), Actions.ForNamespaces.ModifyPages, "User", new string[] { "Group" }), "Permission should be denied");
		}

		[Test]
		public void CheckActionForNamespace_GrantUserLocalEscalator_DenyUserExplicit() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix + "NS", Actions.ForNamespaces.ManagePages, "U.User", Value.Grant));
			entries.Add(new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix + "NS", Actions.ForNamespaces.ReadPages, "U.User", Value.Deny));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsFalse(AuthChecker.CheckActionForNamespace(new NamespaceInfo("NS", null, null), Actions.ForNamespaces.ReadPages, "User", new string[] { "Group" }), "Permission should be denied");
		}

		[Test]
		public void CheckActionForNamespace_GrantGroupLocalEscalator_DenyUserExplicit() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix + "NS", Actions.ForNamespaces.ManagePages, "G.Group", Value.Grant));
			entries.Add(new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix + "NS", Actions.ForNamespaces.ReadPages, "U.User", Value.Deny));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsFalse(AuthChecker.CheckActionForNamespace(new NamespaceInfo("NS", null, null), Actions.ForNamespaces.ReadPages, "User", new string[] { "Group" }), "Permission should be denied");
		}

		[Test]
		public void CheckActionForNamespace_GrantUserRootEscalator() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix,
				Actions.ForNamespaces.ManagePages, "U.User", Value.Grant));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsTrue(AuthChecker.CheckActionForNamespace(new NamespaceInfo("Sub", null, null), Actions.ForNamespaces.ManagePages,
				"User", new string[] { "Group" }), "Permission should be granted");
		}

		[Test]
		public void CheckActionForNamespace_GrantUserRootEscalator_DenyUserExplicitSub() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix,
				Actions.ForNamespaces.ManagePages, "U.User", Value.Grant));
			entries.Add(new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix + "Sub",
				Actions.ForNamespaces.ManagePages, "U.User", Value.Deny));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsFalse(AuthChecker.CheckActionForNamespace(new NamespaceInfo("Sub", null, null), Actions.ForNamespaces.ManagePages,
				"User", new string[] { "Group" }), "Permission should be denied");
		}

		[Test]
		public void CheckActionForNamespace_GrantGroupRootEscalator_DenyUserExplicitSub() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix,
				Actions.ForNamespaces.ManagePages, "G.Group", Value.Grant));
			entries.Add(new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix + "Sub",
				Actions.ForNamespaces.ManagePages, "U.User", Value.Deny));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsFalse(AuthChecker.CheckActionForNamespace(new NamespaceInfo("Sub", null, null), Actions.ForNamespaces.ManagePages,
				"User", new string[] { "Group" }), "Permission should be denied");
		}

		[Test]
		public void CheckActionForNamespace_GrantUserRootEscalator_DenyGroupExplicitSub() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix,
				Actions.ForNamespaces.ManagePages, "G.Group", Value.Deny));
			entries.Add(new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix + "Sub",
				Actions.ForNamespaces.ManagePages, "U.User", Value.Grant));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsTrue(AuthChecker.CheckActionForNamespace(new NamespaceInfo("Sub", null, null), Actions.ForNamespaces.ManagePages,
				"User", new string[] { "Group" }), "Permission should be granted");
		}

		[Test]
		public void CheckActionForNamespace_RandomTestForRootNamespace() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix,
				Actions.ForNamespaces.ReadPages, "U.User", Value.Grant));
			entries.Add(new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix,
				Actions.ForNamespaces.ReadPages, "U.User100", Value.Deny));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsFalse(AuthChecker.CheckActionForNamespace(null, Actions.ForNamespaces.ManageCategories,
				"User", new string[] { "Group" }), "Permission should be denied");

			Assert.IsTrue(AuthChecker.CheckActionForNamespace(null, Actions.ForNamespaces.ReadPages,
				"User", new string[] { "Group" }), "Permission should be granted");

			Assert.IsFalse(AuthChecker.CheckActionForNamespace(null, Actions.ForNamespaces.ReadPages,
				"User2", new string[] { "Group" }), "Permission should be denied");

			Assert.IsTrue(AuthChecker.CheckActionForNamespace(new NamespaceInfo("NS", null, null), Actions.ForNamespaces.ReadPages,
				"User", new string[] { "Group" }), "Permission should be granted");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void CheckActionForPage_NullPage() {
			Collectors.SettingsProvider = MockProvider();
			AuthChecker.CheckActionForPage(null, Actions.ForPages.DeleteAttachments, "User", new string[0]);
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		[TestCase("Blah", ExpectedException = typeof(ArgumentException))]
		[TestCase("*", ExpectedException = typeof(ArgumentException))]
		public void CheckActionForPage_InvalidAction(string a) {
			Collectors.SettingsProvider = MockProvider();
			AuthChecker.CheckActionForPage(new PageInfo("Page", null, DateTime.Now), a, "User", new string[0]);
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void CheckActionForPage_InvalidUser(string u) {
			Collectors.SettingsProvider = MockProvider();
			AuthChecker.CheckActionForPage(new PageInfo("Page", null, DateTime.Now), Actions.ForPages.DeleteAttachments, u, new string[0]);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void CheckActionForPage_NullGroups() {
			Collectors.SettingsProvider = MockProvider();
			AuthChecker.CheckActionForPage(new PageInfo("Page", null, DateTime.Now), Actions.ForPages.DeleteAttachments, "User", null);
		}

		[Test]
		public void CheckActionForPage_AdminBypass() {
			Collectors.SettingsProvider = MockProvider();
			Assert.IsTrue(AuthChecker.CheckActionForPage(new PageInfo("Page", null, DateTime.Now), Actions.ForPages.ManagePage, "admin", new string[0]), "Admin account should bypass security");
		}

		[Test]
		public void CheckActionForPage_GrantUserExplicit() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForPages.ResourceMasterPrefix + "Page", Actions.ForPages.ModifyPage, "U.User", Value.Grant));
			entries.Add(new AclEntry(Actions.ForPages.ResourceMasterPrefix + "Page", Actions.ForPages.ModifyPage, "U.User100", Value.Deny));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsFalse(AuthChecker.CheckActionForPage(new PageInfo("Page", null, DateTime.Now), Actions.ForPages.DeleteAttachments,
				"User", new string[] { "Group" }), "Permission should be denied");

			Assert.IsTrue(AuthChecker.CheckActionForPage(new PageInfo("Page", null, DateTime.Now), Actions.ForPages.ModifyPage,
				"User", new string[] { "Group" }), "Permission should be granted");

			Assert.IsFalse(AuthChecker.CheckActionForPage(new PageInfo("Page", null, DateTime.Now), Actions.ForPages.DeleteAttachments,
				"User2", new string[] { "Group" }), "Permission should be denied");

			Assert.IsFalse(AuthChecker.CheckActionForPage(new PageInfo("Inexistent", null, DateTime.Now), Actions.ForPages.ModifyPage,
				"User", new string[] { "Group" }), "Permission should be denied");
		}

		[Test]
		public void CheckActionForPage_DenyUserExplicit() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForPages.ResourceMasterPrefix + "Page", Actions.ForPages.ModifyPage, "U.User", Value.Deny));
			entries.Add(new AclEntry(Actions.ForPages.ResourceMasterPrefix + "Page", Actions.ForPages.ModifyPage, "U.User100", Value.Grant));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsFalse(AuthChecker.CheckActionForPage(new PageInfo("Page", null, DateTime.Now), Actions.ForPages.DeleteAttachments,
				"User", new string[] { "Group" }), "Permission should be denied");

			Assert.IsFalse(AuthChecker.CheckActionForPage(new PageInfo("Page", null, DateTime.Now), Actions.ForPages.ModifyPage,
				"User", new string[] { "Group" }), "Permission should be denied");

			Assert.IsFalse(AuthChecker.CheckActionForPage(new PageInfo("Page", null, DateTime.Now), Actions.ForPages.DeleteAttachments,
				"User2", new string[] { "Group" }), "Permission should be denied");

			Assert.IsFalse(AuthChecker.CheckActionForPage(new PageInfo("Inexistent", null, DateTime.Now), Actions.ForPages.ModifyPage,
				"User", new string[] { "Group" }), "Permission should be denied");
		}

		[Test]
		public void CheckActionForPage_GrantUserFullControl() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForPages.ResourceMasterPrefix + "Page", Actions.FullControl, "U.User", Value.Grant));
			entries.Add(new AclEntry(Actions.ForPages.ResourceMasterPrefix + "Page", Actions.FullControl, "U.User100", Value.Deny));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsTrue(AuthChecker.CheckActionForPage(new PageInfo("Page", null, DateTime.Now), Actions.ForPages.DeleteAttachments,
				"User", new string[] { "Group" }), "Permission should be granted");

			Assert.IsTrue(AuthChecker.CheckActionForPage(new PageInfo("Page", null, DateTime.Now), Actions.ForPages.ModifyPage,
				"User", new string[] { "Group" }), "Permission should be granted");

			Assert.IsFalse(AuthChecker.CheckActionForPage(new PageInfo("Page", null, DateTime.Now), Actions.ForPages.DeleteAttachments,
				"User2", new string[] { "Group" }), "Permission should be denied");

			Assert.IsFalse(AuthChecker.CheckActionForPage(new PageInfo("Inexistent", null, DateTime.Now), Actions.ForPages.ModifyPage,
				"User", new string[] { "Group" }), "Permission should be denied");
		}

		[Test]
		public void CheckActionForPage_DenyUserFullControl() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForPages.ResourceMasterPrefix + "Page", Actions.FullControl, "U.User", Value.Deny));
			entries.Add(new AclEntry(Actions.ForPages.ResourceMasterPrefix + "Page", Actions.FullControl, "U.User100", Value.Grant));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsFalse(AuthChecker.CheckActionForPage(new PageInfo("Page", null, DateTime.Now), Actions.ForPages.DeleteAttachments,
				"User", new string[] { "Group" }), "Permission should be denied");

			Assert.IsFalse(AuthChecker.CheckActionForPage(new PageInfo("Page", null, DateTime.Now), Actions.ForPages.ModifyPage,
				"User", new string[] { "Group" }), "Permission should be denied");

			Assert.IsFalse(AuthChecker.CheckActionForPage(new PageInfo("Page", null, DateTime.Now), Actions.ForPages.DeleteAttachments,
				"User2", new string[] { "Group" }), "Permission should be denied");

			Assert.IsFalse(AuthChecker.CheckActionForPage(new PageInfo("Inexistent", null, DateTime.Now), Actions.ForPages.ModifyPage,
				"User", new string[] { "Group" }), "Permission should be denied");
		}

		[Test]
		public void CheckActionForPage_GrantGroupExplicit() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForPages.ResourceMasterPrefix + "Page", Actions.ForPages.ModifyPage, "G.Group", Value.Grant));
			entries.Add(new AclEntry(Actions.ForPages.ResourceMasterPrefix + "Page", Actions.ForPages.ModifyPage, "G.Group100", Value.Deny));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsFalse(AuthChecker.CheckActionForPage(new PageInfo("Page", null, DateTime.Now), Actions.ForPages.DeleteAttachments,
				"User", new string[] { "Group" }), "Permission should be denied");

			Assert.IsTrue(AuthChecker.CheckActionForPage(new PageInfo("Page", null, DateTime.Now), Actions.ForPages.ModifyPage,
				"User", new string[] { "Group" }), "Permission should be granted");

			Assert.IsFalse(AuthChecker.CheckActionForPage(new PageInfo("Page", null, DateTime.Now), Actions.ForPages.DeleteAttachments,
				"User", new string[] { "Group2" }), "Permission should be denied");

			Assert.IsFalse(AuthChecker.CheckActionForPage(new PageInfo("Inexistent", null, DateTime.Now), Actions.ForPages.ModifyPage,
				"User", new string[] { "Group" }), "Permission should be denied");
		}

		[Test]
		public void CheckActionForPage_DenyGroupExplicit() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForPages.ResourceMasterPrefix + "Page", Actions.ForPages.ModifyPage, "G.Group", Value.Deny));
			entries.Add(new AclEntry(Actions.ForPages.ResourceMasterPrefix + "Page", Actions.ForPages.ModifyPage, "G.Group100", Value.Grant));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsFalse(AuthChecker.CheckActionForPage(new PageInfo("Page", null, DateTime.Now), Actions.ForPages.DeleteAttachments,
				"User", new string[] { "Group" }), "Permission should be denied");

			Assert.IsFalse(AuthChecker.CheckActionForPage(new PageInfo("Page", null, DateTime.Now), Actions.ForPages.ModifyPage,
				"User", new string[] { "Group" }), "Permission should be denied");

			Assert.IsFalse(AuthChecker.CheckActionForPage(new PageInfo("Page", null, DateTime.Now), Actions.ForPages.DeleteAttachments,
				"User", new string[] { "Group2" }), "Permission should be denied");

			Assert.IsFalse(AuthChecker.CheckActionForPage(new PageInfo("Inexistent", null, DateTime.Now), Actions.ForPages.ModifyPage,
				"User", new string[] { "Group" }), "Permission should be denied");
		}

		[Test]
		public void CheckActionForPage_GrantGroupFullControl() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForPages.ResourceMasterPrefix + "Page", Actions.FullControl, "G.Group", Value.Grant));
			entries.Add(new AclEntry(Actions.ForPages.ResourceMasterPrefix + "Page", Actions.FullControl, "G.Group100", Value.Deny));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsTrue(AuthChecker.CheckActionForPage(new PageInfo("Page", null, DateTime.Now), Actions.ForPages.DeleteAttachments,
				"User", new string[] { "Group" }), "Permission should be granted");

			Assert.IsTrue(AuthChecker.CheckActionForPage(new PageInfo("Page", null, DateTime.Now), Actions.ForPages.ModifyPage,
				"User", new string[] { "Group" }), "Permission should be granted");

			Assert.IsFalse(AuthChecker.CheckActionForPage(new PageInfo("Page", null, DateTime.Now), Actions.ForPages.DeleteAttachments,
				"User", new string[] { "Group2" }), "Permission should be denied");

			Assert.IsFalse(AuthChecker.CheckActionForPage(new PageInfo("Inexistent", null, DateTime.Now), Actions.ForPages.ModifyPage,
				"User", new string[] { "Group" }), "Permission should be denied");
		}

		[Test]
		public void CheckActionForPage_DenyGroupFullControl() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForPages.ResourceMasterPrefix + "Page", Actions.FullControl, "G.Group", Value.Deny));
			entries.Add(new AclEntry(Actions.ForPages.ResourceMasterPrefix + "Page", Actions.FullControl, "G.Group100", Value.Grant));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsFalse(AuthChecker.CheckActionForPage(new PageInfo("Page", null, DateTime.Now), Actions.ForPages.DeleteAttachments,
				"User", new string[] { "Group" }), "Permission should be denied");

			Assert.IsFalse(AuthChecker.CheckActionForPage(new PageInfo("Page", null, DateTime.Now), Actions.ForPages.ModifyPage,
				"User", new string[] { "Group" }), "Permission should be denied");

			Assert.IsFalse(AuthChecker.CheckActionForPage(new PageInfo("Page", null, DateTime.Now), Actions.ForPages.DeleteAttachments,
				"User", new string[] { "Group2" }), "Permission should be denied");

			Assert.IsFalse(AuthChecker.CheckActionForPage(new PageInfo("Inexistent", null, DateTime.Now), Actions.ForPages.ModifyPage,
				"User", new string[] { "Group" }), "Permission should be denied");
		}

		[Test]
		public void CheckActionForPage_GrantUserGlobalEscalator() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.ForGlobals.ManagePagesAndCategories, "U.User", Value.Grant));
			entries.Add(new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.ForGlobals.ManagePagesAndCategories, "U.User100", Value.Deny));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsTrue(AuthChecker.CheckActionForPage(new PageInfo("Page", null, DateTime.Now), Actions.ForPages.ModifyPage,
				"User", new string[] { "Group" }), "Permission should be granted");

			Assert.IsTrue(AuthChecker.CheckActionForPage(new PageInfo("Page", null, DateTime.Now), Actions.ForPages.ReadPage,
				"User", new string[] { "Group" }), "Permission should be granted");

			Assert.IsFalse(AuthChecker.CheckActionForPage(new PageInfo("Page", null, DateTime.Now), Actions.ForPages.ReadPage,
				"User2", new string[] { "Group" }), "Permission should be denied");

			Assert.IsTrue(AuthChecker.CheckActionForPage(new PageInfo("Page2", null, DateTime.Now), Actions.ForPages.ReadPage,
				"User", new string[] { "Group" }), "Permission should be granted");
		}

		[Test]
		public void CheckActionForPage_GrantGroupGlobalEscalator() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.ForGlobals.ManagePagesAndCategories, "G.Group", Value.Grant));
			entries.Add(new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.ForGlobals.ManagePagesAndCategories, "G.Group100", Value.Deny));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsTrue(AuthChecker.CheckActionForPage(new PageInfo("Page", null, DateTime.Now), Actions.ForPages.ModifyPage,
				"User", new string[] { "Group" }), "Permission should be granted");

			Assert.IsTrue(AuthChecker.CheckActionForPage(new PageInfo("Page", null, DateTime.Now), Actions.ForPages.ReadPage,
				"User", new string[] { "Group" }), "Permission should be granted");

			Assert.IsFalse(AuthChecker.CheckActionForPage(new PageInfo("Page", null, DateTime.Now), Actions.ForPages.ReadPage,
				"User", new string[] { "Group2" }), "Permission should be denied");

			Assert.IsTrue(AuthChecker.CheckActionForPage(new PageInfo("Page2", null, DateTime.Now), Actions.ForPages.ReadPage,
				"User", new string[] { "Group" }), "Permission should be granted");
		}

		[Test]
		public void CheckActionForPage_GrantUserGlobalFullControl() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.FullControl, "U.User", Value.Grant));
			entries.Add(new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.FullControl, "U.User100", Value.Deny));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsTrue(AuthChecker.CheckActionForPage(new PageInfo("Page", null, DateTime.Now), Actions.ForPages.ModifyPage,
				"User", new string[] { "Group" }), "Permission should be granted");

			Assert.IsTrue(AuthChecker.CheckActionForPage(new PageInfo("Page", null, DateTime.Now), Actions.ForPages.ReadPage,
				"User", new string[] { "Group" }), "Permission should be granted");

			Assert.IsFalse(AuthChecker.CheckActionForPage(new PageInfo("Page", null, DateTime.Now), Actions.ForPages.ReadPage,
				"User2", new string[] { "Group" }), "Permission should be denied");

			Assert.IsTrue(AuthChecker.CheckActionForPage(new PageInfo("Page2", null, DateTime.Now), Actions.ForPages.ReadPage,
				"User", new string[] { "Group" }), "Permission should be granted");
		}

		[Test]
		public void CheckActionForPage_GrantGroupGlobalFullControl() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.FullControl, "G.Group", Value.Grant));
			entries.Add(new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.FullControl, "G.Group100", Value.Deny));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsTrue(AuthChecker.CheckActionForPage(new PageInfo("Page", null, DateTime.Now), Actions.ForPages.ModifyPage,
				"User", new string[] { "Group" }), "Permission should be granted");

			Assert.IsTrue(AuthChecker.CheckActionForPage(new PageInfo("Page", null, DateTime.Now), Actions.ForPages.ReadPage,
				"User", new string[] { "Group" }), "Permission should be granted");

			Assert.IsFalse(AuthChecker.CheckActionForPage(new PageInfo("Page", null, DateTime.Now), Actions.ForPages.ReadPage,
				"User", new string[] { "Group2" }), "Permission should be denied");
			Assert.IsTrue(AuthChecker.CheckActionForPage(new PageInfo("Page2", null, DateTime.Now), Actions.ForPages.ReadPage,
				"User", new string[] { "Group" }), "Permission should be granted");
		}

		[Test]
		public void CheckActionForPage_GrantUserLocalEscalator() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForPages.ResourceMasterPrefix + "Page", Actions.ForPages.ModifyPage, "U.User", Value.Grant));
			entries.Add(new AclEntry(Actions.ForPages.ResourceMasterPrefix + "Page", Actions.ForPages.ModifyPage, "U.User100", Value.Deny));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsTrue(AuthChecker.CheckActionForPage(new PageInfo("Page", null, DateTime.Now), Actions.ForPages.ModifyPage,
				"User", new string[] { "Group" }), "Permission should be granted");

			Assert.IsTrue(AuthChecker.CheckActionForPage(new PageInfo("Page", null, DateTime.Now), Actions.ForPages.ReadPage,
				"User", new string[] { "Group" }), "Permission should be granted");

			Assert.IsFalse(AuthChecker.CheckActionForPage(new PageInfo("Page", null, DateTime.Now), Actions.ForPages.ReadPage,
				"User2", new string[] { "Group" }), "Permission should be denied");

			Assert.IsFalse(AuthChecker.CheckActionForPage(new PageInfo("Page2", null, DateTime.Now), Actions.ForPages.ReadPage,
				"User", new string[] { "Group" }), "Permission should be granted");
		}

		[Test]
		public void CheckActionForPage_GrantGroupLocalEscalator() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForPages.ResourceMasterPrefix + "Page", Actions.ForPages.ModifyPage, "G.Group", Value.Grant));
			entries.Add(new AclEntry(Actions.ForPages.ResourceMasterPrefix + "Page", Actions.ForPages.ModifyPage, "G.Group100", Value.Deny));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsTrue(AuthChecker.CheckActionForPage(new PageInfo("Page", null, DateTime.Now), Actions.ForPages.ModifyPage,
				"User", new string[] { "Group" }), "Permission should be granted");

			Assert.IsTrue(AuthChecker.CheckActionForPage(new PageInfo("Page", null, DateTime.Now), Actions.ForPages.ReadPage,
				"User", new string[] { "Group" }), "Permission should be granted");

			Assert.IsFalse(AuthChecker.CheckActionForPage(new PageInfo("Page", null, DateTime.Now), Actions.ForPages.ReadPage,
				"User", new string[] { "Group2" }), "Permission should be denied");

			Assert.IsFalse(AuthChecker.CheckActionForPage(new PageInfo("Page2", null, DateTime.Now), Actions.ForPages.ReadPage,
				"User", new string[] { "Group" }), "Permission should be granted");
		}

		[Test]
		public void CheckActionForPage_GrantUserNamespaceEscalator() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix, Actions.ForNamespaces.ModifyPages, "U.User", Value.Grant));
			entries.Add(new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix, Actions.ForNamespaces.ModifyPages, "U.User100", Value.Deny));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsTrue(AuthChecker.CheckActionForPage(new PageInfo("Page", null, DateTime.Now), Actions.ForPages.ModifyPage,
				"User", new string[] { "Group" }), "Permission should be granted");

			Assert.IsTrue(AuthChecker.CheckActionForPage(new PageInfo("Page", null, DateTime.Now), Actions.ForPages.ReadPage,
				"User", new string[] { "Group" }), "Permission should be granted");

			Assert.IsFalse(AuthChecker.CheckActionForPage(new PageInfo("Page", null, DateTime.Now), Actions.ForPages.ReadPage,
				"User2", new string[] { "Group" }), "Permission should be denied");
		}

		[Test]
		public void CheckActionForPage_GrantGroupNamespaceEscalator() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix, Actions.ForNamespaces.ModifyPages, "G.Group", Value.Grant));
			entries.Add(new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix, Actions.ForNamespaces.ModifyPages, "G.Group100", Value.Deny));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsTrue(AuthChecker.CheckActionForPage(new PageInfo("Page", null, DateTime.Now), Actions.ForPages.ModifyPage,
				"User", new string[] { "Group" }), "Permission should be granted");

			Assert.IsTrue(AuthChecker.CheckActionForPage(new PageInfo("Page", null, DateTime.Now), Actions.ForPages.ReadPage,
				"User", new string[] { "Group" }), "Permission should be granted");

			Assert.IsFalse(AuthChecker.CheckActionForPage(new PageInfo("Page", null, DateTime.Now), Actions.ForPages.ReadPage,
				"User", new string[] { "Group2" }), "Permission should be denied");
		}

		[Test]
		public void CheckActionForPage_GrantUserNamespaceEscalator_DenyUserExplicit() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix, Actions.ForNamespaces.ModifyPages, "U.User", Value.Grant));
			entries.Add(new AclEntry(Actions.ForPages.ResourceMasterPrefix + "Page", Actions.ForPages.ModifyPage, "U.User", Value.Deny));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsFalse(AuthChecker.CheckActionForPage(new PageInfo("Page", null, DateTime.Now), Actions.ForPages.ModifyPage,
				"User", new string[] { "Group" }), "Permission should be denied");
		}

		[Test]
		public void CheckActionForPage_GrantGroupNamespaceEscalator_DenyUserExplicit() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix, Actions.ForNamespaces.ModifyPages, "G.Group", Value.Grant));
			entries.Add(new AclEntry(Actions.ForPages.ResourceMasterPrefix + "Page", Actions.ForPages.ModifyPage, "U.User", Value.Deny));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsFalse(AuthChecker.CheckActionForPage(new PageInfo("Page", null, DateTime.Now), Actions.ForPages.ModifyPage,
				"User", new string[] { "Group" }), "Permission should be denied");
		}

		[Test]
		public void CheckActionForPage_GrantUserGlobalEscalator_DenyUserExplicit() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.ForGlobals.ManagePagesAndCategories, "U.User", Value.Grant));
			entries.Add(new AclEntry(Actions.ForPages.ResourceMasterPrefix + "Page", Actions.ForPages.ModifyPage, "U.User", Value.Deny));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsFalse(AuthChecker.CheckActionForPage(new PageInfo("Page", null, DateTime.Now), Actions.ForPages.ModifyPage,
				"User", new string[] { "Group" }), "Permission should be denied");
		}

		[Test]
		public void CheckActionForPage_GrantGroupGlobalEscalator_DenyUserExplicit() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.ForGlobals.ManagePagesAndCategories, "G.Group", Value.Grant));
			entries.Add(new AclEntry(Actions.ForPages.ResourceMasterPrefix + "Page", Actions.ForPages.ModifyPage, "U.User", Value.Deny));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsFalse(AuthChecker.CheckActionForPage(new PageInfo("Page", null, DateTime.Now), Actions.ForPages.ModifyPage,
				"User", new string[] { "Group" }), "Permission should be denied");
		}

		[Test]
		public void CheckActionForPage_GrantUserGlobalFullControl_DenyUserExplicit() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.FullControl, "U.User", Value.Grant));
			entries.Add(new AclEntry(Actions.ForPages.ResourceMasterPrefix + "Page", Actions.ForPages.ModifyPage, "U.User", Value.Deny));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsFalse(AuthChecker.CheckActionForPage(new PageInfo("Page", null, DateTime.Now), Actions.ForPages.ModifyPage,
				"User", new string[] { "Group" }), "Permission should be denied");
		}

		[Test]
		public void CheckActionForPage_GrantGroupGlobalFullControl_DenyUserExplicit() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.FullControl, "G.Group", Value.Grant));
			entries.Add(new AclEntry(Actions.ForPages.ResourceMasterPrefix + "Page", Actions.ForPages.ModifyPage, "U.User", Value.Deny));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsFalse(AuthChecker.CheckActionForPage(new PageInfo("Page", null, DateTime.Now), Actions.ForPages.ModifyPage,
				"User", new string[] { "Group" }), "Permission should be denied");
		}

		[Test]
		public void CheckActionForPage_GrantUserLocalEscalator_DenyUserExplicit() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForPages.ResourceMasterPrefix + "Page", Actions.ForPages.ModifyPage, "U.User", Value.Grant));
			entries.Add(new AclEntry(Actions.ForPages.ResourceMasterPrefix + "Page", Actions.ForPages.ReadPage, "U.User", Value.Deny));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsFalse(AuthChecker.CheckActionForPage(new PageInfo("Page", null, DateTime.Now), Actions.ForPages.ReadPage,
				"User", new string[] { "Group" }), "Permission should be denied");
		}

		[Test]
		public void CheckActionForPage_GrantGroupLocalEscalator_DenyUserExplicit() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForPages.ResourceMasterPrefix + "Page", Actions.ForPages.ModifyPage, "G.Group", Value.Grant));
			entries.Add(new AclEntry(Actions.ForPages.ResourceMasterPrefix + "Page", Actions.ForPages.ReadPage, "U.User", Value.Deny));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsFalse(AuthChecker.CheckActionForPage(new PageInfo("Page", null, DateTime.Now), Actions.ForPages.ReadPage,
				"User", new string[] { "Group" }), "Permission should be denied");
		}

		[Test]
		public void CheckActionForPage_GrantUserRootEscalator() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix,
				Actions.ForNamespaces.ModifyPages, "U.User", Value.Grant));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsTrue(AuthChecker.CheckActionForPage(new PageInfo(NameTools.GetFullName("Sub", "Page"), null, DateTime.Now),
				Actions.ForPages.ModifyPage, "User", new string[] { "Group" }), "Permission should be granted");
		}

		[Test]
		public void CheckActionForPage_GrantUserRootEscalator_DenyUserExplicitSub() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix,
				Actions.ForNamespaces.ModifyPages, "U.User", Value.Grant));
			entries.Add(new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix + "Sub",
				Actions.ForNamespaces.ModifyPages, "U.User", Value.Deny));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsFalse(AuthChecker.CheckActionForPage(new PageInfo(NameTools.GetFullName("Sub", "Page"), null, DateTime.Now),
				Actions.ForPages.ModifyPage, "User", new string[] { "Group" }), "Permission should be denied");
		}

		[Test]
		public void CheckActionForPage_GrantGroupRootEscalator_DenyUserExplicitSub() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix,
				Actions.ForNamespaces.ModifyPages, "G.Group", Value.Grant));
			entries.Add(new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix + "Sub",
				Actions.ForNamespaces.ModifyPages, "U.User", Value.Deny));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsFalse(AuthChecker.CheckActionForPage(new PageInfo(NameTools.GetFullName("Sub", "Page"), null, DateTime.Now),
				Actions.ForPages.ModifyPage, "User", new string[] { "Group" }), "Permission should be denied");
		}

		[Test]
		public void CheckActionForPage_GrantUserRootEscalator_DenyGroupExplicitSub() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix,
				Actions.ForNamespaces.ModifyPages, "G.Group", Value.Deny));
			entries.Add(new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix + "Sub",
				Actions.ForNamespaces.ModifyPages, "U.User", Value.Grant));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsTrue(AuthChecker.CheckActionForPage(new PageInfo(NameTools.GetFullName("Sub", "Page"), null, DateTime.Now),
				Actions.ForPages.ModifyPage, "User", new string[] { "Group" }), "Permission should be granted");
		}

		[Test]
		public void CheckActionForPage_GrantUserRootEscalator_DenyUserExplicitPage() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix,
				Actions.ForNamespaces.ModifyPages, "U.User", Value.Grant));
			entries.Add(new AclEntry(Actions.ForPages.ResourceMasterPrefix + NameTools.GetFullName("Sub", "Page"),
				Actions.ForPages.ModifyPage, "U.User", Value.Deny));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsFalse(AuthChecker.CheckActionForPage(new PageInfo(NameTools.GetFullName("Sub", "Page"), null, DateTime.Now),
				Actions.ForPages.ModifyPage, "User", new string[] { "Group" }), "Permission should be denied");
		}

		[Test]
		public void CheckActionForPage_GrantGroupRootEscalator_DenyUserExplicitPage() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix,
				Actions.ForNamespaces.ModifyPages, "G.Group", Value.Grant));
			entries.Add(new AclEntry(Actions.ForPages.ResourceMasterPrefix + NameTools.GetFullName("Sub", "Page"),
				Actions.ForPages.ModifyPage, "U.User", Value.Deny));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsFalse(AuthChecker.CheckActionForPage(new PageInfo(NameTools.GetFullName("Sub", "Page"), null, DateTime.Now),
				Actions.ForPages.ModifyPage, "User", new string[] { "Group" }), "Permission should be denied");
		}

		[Test]
		public void CheckActionForPage_GrantGroupFullControl_DenyGroupExplicitNamespace_ExceptReadPages() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.FullControl, "G.Group", Value.Grant));
			entries.Add(new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix + "NS1", Actions.ForNamespaces.ReadPages, "G.Group", Value.Grant));
			entries.Add(new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix + "NS1", Actions.FullControl, "G.Group", Value.Deny));

			Collectors.SettingsProvider = MockProvider(entries);
			Assert.IsFalse(AuthChecker.CheckActionForPage(new PageInfo(NameTools.GetFullName("NS1", "Page"), null, DateTime.Now), Actions.ForPages.ModifyPage, "User", new string[] { "Group" }), "Permission should be denied");
			Assert.IsTrue(AuthChecker.CheckActionForPage(new PageInfo(NameTools.GetFullName("NS1", "Page"), null, DateTime.Now), Actions.ForPages.ReadPage, "User", new string[] { "Group" }), "Permission should be granted");
		}

		[Test]
		public void CheckActionForPage_GrantGroupFullControl_DenyGroupNamespaceEscalator_ExceptReadPages() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.FullControl, "G.Group", Value.Grant));
			entries.Add(new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix, Actions.ForNamespaces.ReadPages, "G.Group", Value.Grant));
			entries.Add(new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix, Actions.FullControl, "G.Group", Value.Deny));

			Collectors.SettingsProvider = MockProvider(entries);
			Assert.IsFalse(AuthChecker.CheckActionForPage(new PageInfo(NameTools.GetFullName("NS1", "Page"), null, DateTime.Now), Actions.ForPages.ModifyPage, "User", new string[] { "Group" }), "Permission should be denied");
			Assert.IsTrue(AuthChecker.CheckActionForPage(new PageInfo(NameTools.GetFullName("NS1", "Page"), null, DateTime.Now), Actions.ForPages.ReadPage, "User", new string[] { "Group" }), "Permission should be granted");
		}

		[Test]
		public void CheckActionForPage_DenyGroupFullControl_GrantGroupExplicitNamespace() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.FullControl, "G.Group", Value.Deny));
			entries.Add(new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix + "NS1", Actions.FullControl, "G.Group", Value.Grant));

			Collectors.SettingsProvider = MockProvider(entries);
			Assert.IsTrue(AuthChecker.CheckActionForPage(new PageInfo(NameTools.GetFullName("NS1", "Page"), null, DateTime.Now), Actions.ForPages.ModifyPage, "User", new string[] { "Group" }), "Permission should be granted");
			Assert.IsTrue(AuthChecker.CheckActionForPage(new PageInfo(NameTools.GetFullName("NS1", "Page"), null, DateTime.Now), Actions.ForPages.ReadPage, "User", new string[] { "Group" }), "Permission should be granted");
		}

		[Test]
		public void CheckActionForPage_DenyGroupFullControl_GrantGroupNamespaceEscalator() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.FullControl, "G.Group", Value.Deny));
			entries.Add(new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix, Actions.FullControl, "G.Group", Value.Grant));

			Collectors.SettingsProvider = MockProvider(entries);
			Assert.IsTrue(AuthChecker.CheckActionForPage(new PageInfo(NameTools.GetFullName("NS1", "Page"), null, DateTime.Now), Actions.ForPages.ModifyPage, "User", new string[] { "Group" }), "Permission should be granted");
			Assert.IsTrue(AuthChecker.CheckActionForPage(new PageInfo(NameTools.GetFullName("NS1", "Page"), null, DateTime.Now), Actions.ForPages.ReadPage, "User", new string[] { "Group" }), "Permission should be granted");
		}

		[Test]
		public void CheckActionForPage_DenyGroupFullControl_GrantGroupReadPagesExplicitNamespace() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.FullControl, "G.Group", Value.Deny));
			entries.Add(new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix + "NS1", Actions.ForNamespaces.ReadPages, "G.Group", Value.Grant));

			Collectors.SettingsProvider = MockProvider(entries);
			Assert.IsFalse(AuthChecker.CheckActionForPage(new PageInfo(NameTools.GetFullName("NS1", "Page"), null, DateTime.Now), Actions.ForPages.ModifyPage, "User", new string[] { "Group" }), "Permission should be denied");
			Assert.IsTrue(AuthChecker.CheckActionForPage(new PageInfo(NameTools.GetFullName("NS1", "Page"), null, DateTime.Now), Actions.ForPages.ReadPage, "User", new string[] { "Group" }), "Permission should be granted");
		}

		[Test]
		public void CheckActionForPage_DenyGroupFullControl_GrantGroupReadPagesNamespaceEscalator() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.FullControl, "G.Group", Value.Deny));
			entries.Add(new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix, Actions.ForNamespaces.ReadPages, "G.Group", Value.Grant));

			Collectors.SettingsProvider = MockProvider(entries);
			Assert.IsFalse(AuthChecker.CheckActionForPage(new PageInfo(NameTools.GetFullName("NS1", "Page"), null, DateTime.Now), Actions.ForPages.ModifyPage, "User", new string[] { "Group" }), "Permission should be denied");
			Assert.IsTrue(AuthChecker.CheckActionForPage(new PageInfo(NameTools.GetFullName("NS1", "Page"), null, DateTime.Now), Actions.ForPages.ReadPage, "User", new string[] { "Group" }), "Permission should be granted");
		}

		[Test]
		public void CheckActionForPage_DenyGroupFullControl_GrantGroupReadPagesExplicitNamespaceLocalEscalator() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.FullControl, "G.Group", Value.Deny));
			entries.Add(new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix + "NS1", Actions.ForNamespaces.ManagePages, "G.Group", Value.Grant));

			Collectors.SettingsProvider = MockProvider(entries);
			Assert.IsTrue(AuthChecker.CheckActionForPage(new PageInfo(NameTools.GetFullName("NS1", "Page"), null, DateTime.Now), Actions.ForPages.ModifyPage, "User", new string[] { "Group" }), "Permission should be granted");
			Assert.IsTrue(AuthChecker.CheckActionForPage(new PageInfo(NameTools.GetFullName("NS1", "Page"), null, DateTime.Now), Actions.ForPages.ReadPage, "User", new string[] { "Group" }), "Permission should be granted");
		}

		[Test]
		public void CheckActionForPage_DenyGroupFullControl_GrantGroupReadPagesNamespaceEscalatorLocalEscalator() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.FullControl, "G.Group", Value.Deny));
			entries.Add(new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix, Actions.ForNamespaces.ManagePages, "G.Group", Value.Grant));

			Collectors.SettingsProvider = MockProvider(entries);
			Assert.IsTrue(AuthChecker.CheckActionForPage(new PageInfo(NameTools.GetFullName("NS1", "Page"), null, DateTime.Now), Actions.ForPages.ModifyPage, "User", new string[] { "Group" }), "Permission should be granted");
			Assert.IsTrue(AuthChecker.CheckActionForPage(new PageInfo(NameTools.GetFullName("NS1", "Page"), null, DateTime.Now), Actions.ForPages.ReadPage, "User", new string[] { "Group" }), "Permission should be granted");
		}

		[Test]
		public void CheckActionForPage_GrantUserRootEscalator_DenyGroupExplicitPage() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix,
				Actions.ForNamespaces.ModifyPages, "G.Group", Value.Deny));
			entries.Add(new AclEntry(Actions.ForPages.ResourceMasterPrefix + NameTools.GetFullName("Sub", "Page"),
				Actions.ForPages.ModifyPage, "U.User", Value.Grant));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsTrue(AuthChecker.CheckActionForPage(new PageInfo(NameTools.GetFullName("Sub", "Page"), null, DateTime.Now),
				Actions.ForPages.ModifyPage, "User", new string[] { "Group" }), "Permission should be granted");
		}

		[Test]
		public void CheckActionForPage_RandomTestForSubNamespace() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForPages.ResourceMasterPrefix + "P.Page", Actions.ForPages.ModifyPage, "U.User", Value.Grant));
			entries.Add(new AclEntry(Actions.ForPages.ResourceMasterPrefix + "Page", Actions.ForPages.ModifyPage, "U.User", Value.Deny));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsTrue(AuthChecker.CheckActionForPage(new PageInfo("P.Page", null, DateTime.Now), Actions.ForPages.ModifyPage, "User", new string[0]), "Permission should be granted");
			Assert.IsFalse(AuthChecker.CheckActionForPage(new PageInfo("Page", null, DateTime.Now), Actions.ForPages.ModifyPage, "User", new string[0]), "Permission should be denied");
		}

		private IFilesStorageProviderV30 MockFilesProvider() {
			IFilesStorageProviderV30 prov = mocks.DynamicMock<IFilesStorageProviderV30>();
			mocks.Replay(prov);
			return prov;
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void CheckActionForDirectory_NullProvider() {
			Collectors.SettingsProvider = MockProvider();
			AuthChecker.CheckActionForDirectory(null, "/Dir", Actions.ForDirectories.CreateDirectories, "User", new string[0]);
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void CheckActionForDirectory_InvalidDirectory(string d) {
			Collectors.SettingsProvider = MockProvider();
			AuthChecker.CheckActionForDirectory(MockFilesProvider(), d, Actions.ForDirectories.CreateDirectories, "User", new string[0]);
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		[TestCase("Blah", ExpectedException = typeof(ArgumentException))]
		[TestCase("*", ExpectedException = typeof(ArgumentException))]
		public void CheckActionForDirectory_InvalidAction(string a) {
			Collectors.SettingsProvider = MockProvider();
			AuthChecker.CheckActionForDirectory(MockFilesProvider(), "/Dir", a, "User", new string[0]);
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void CheckActionForDirectory_InvalidUser(string u) {
			Collectors.SettingsProvider = MockProvider();
			AuthChecker.CheckActionForDirectory(MockFilesProvider(), "/Dir", Actions.ForDirectories.CreateDirectories, u, new string[0]);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void CheckActionForDirectory_NullGroups() {
			Collectors.SettingsProvider = MockProvider();
			AuthChecker.CheckActionForDirectory(MockFilesProvider(), "/Dir", Actions.ForDirectories.CreateDirectories, "User", null);
		}

		[Test]
		public void CheckActionForDirectory_AdminBypass() {
			Collectors.SettingsProvider = MockProvider();
			Assert.IsTrue(AuthChecker.CheckActionForDirectory(MockFilesProvider(), "/", Actions.ForDirectories.DeleteFiles, "admin", new string[0]), "Admin account should bypass security");
		}

		[Test]
		public void CheckActionForDirectory_GrantUserExplicit() {

			IFilesStorageProviderV30 filesProv = MockFilesProvider();

			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForDirectories.ResourceMasterPrefix +
				AuthTools.GetDirectoryName(filesProv, "/Dir"),
				Actions.ForDirectories.UploadFiles, "U.User", Value.Grant));
			entries.Add(new AclEntry(Actions.ForDirectories.ResourceMasterPrefix +
				AuthTools.GetDirectoryName(filesProv, "/Dir"),
				Actions.ForDirectories.UploadFiles, "U.User100", Value.Deny));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsFalse(AuthChecker.CheckActionForDirectory(filesProv, "/Dir",
				Actions.ForDirectories.DeleteFiles, "User", new string[] { "Group" }), "Permission should be denied");

			Assert.IsTrue(AuthChecker.CheckActionForDirectory(filesProv, "/Dir",
				Actions.ForDirectories.UploadFiles, "User", new string[] { "Group" }), "Permission should be granted");

			Assert.IsFalse(AuthChecker.CheckActionForDirectory(filesProv, "/Dir",
				Actions.ForDirectories.UploadFiles, "User2", new string[] { "Group" }), "Permission should be denied");

			Assert.IsFalse(AuthChecker.CheckActionForDirectory(filesProv, "/Dir2",
				Actions.ForDirectories.UploadFiles, "User", new string[] { "Group" }), "Permission should be denied");
		}

		[Test]
		public void CheckActionForDirectory_DenyUserExplicit() {
			IFilesStorageProviderV30 filesProv = MockFilesProvider();

			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForDirectories.ResourceMasterPrefix +
				AuthTools.GetDirectoryName(filesProv, "/Dir"),
				Actions.ForDirectories.UploadFiles, "U.User", Value.Deny));
			entries.Add(new AclEntry(Actions.ForDirectories.ResourceMasterPrefix +
				AuthTools.GetDirectoryName(filesProv, "/Dir"),
				Actions.ForDirectories.UploadFiles, "U.User100", Value.Grant));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsFalse(AuthChecker.CheckActionForDirectory(filesProv, "/Dir",
				Actions.ForDirectories.DeleteFiles, "User", new string[] { "Group" }), "Permission should be denied");

			Assert.IsFalse(AuthChecker.CheckActionForDirectory(filesProv, "/Dir",
				Actions.ForDirectories.UploadFiles, "User", new string[] { "Group" }), "Permission should be denied");

			Assert.IsFalse(AuthChecker.CheckActionForDirectory(filesProv, "/Dir",
				Actions.ForDirectories.UploadFiles, "User2", new string[] { "Group" }), "Permission should be denied");

			Assert.IsFalse(AuthChecker.CheckActionForDirectory(filesProv, "/Dir2",
				Actions.ForDirectories.UploadFiles, "User", new string[] { "Group" }), "Permission should be denied");
		}

		[Test]
		public void CheckActionForDirectory_GrantUserFullControl() {
			IFilesStorageProviderV30 filesProv = MockFilesProvider();

			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForDirectories.ResourceMasterPrefix +
				AuthTools.GetDirectoryName(filesProv, "/Dir"),
				Actions.FullControl, "U.User", Value.Grant));
			entries.Add(new AclEntry(Actions.ForDirectories.ResourceMasterPrefix +
				AuthTools.GetDirectoryName(filesProv, "/Dir"),
				Actions.FullControl, "U.User100", Value.Deny));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsTrue(AuthChecker.CheckActionForDirectory(filesProv, "/Dir",
				Actions.ForDirectories.DeleteFiles, "User", new string[] { "Group" }), "Permission should be granted");

			Assert.IsTrue(AuthChecker.CheckActionForDirectory(filesProv, "/Dir",
				Actions.ForDirectories.UploadFiles, "User", new string[] { "Group" }), "Permission should be granted");

			Assert.IsFalse(AuthChecker.CheckActionForDirectory(filesProv, "/Dir",
				Actions.ForDirectories.UploadFiles, "User2", new string[] { "Group" }), "Permission should be denied");

			Assert.IsFalse(AuthChecker.CheckActionForDirectory(filesProv, "/Dir2",
				Actions.ForDirectories.UploadFiles, "User", new string[] { "Group" }), "Permission should be denied");
		}

		[Test]
		public void CheckActionForDirectory_DenyUserFullControl() {
			IFilesStorageProviderV30 filesProv = MockFilesProvider();

			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForDirectories.ResourceMasterPrefix +
				AuthTools.GetDirectoryName(filesProv, "/Dir"),
				Actions.FullControl, "U.User", Value.Deny));
			entries.Add(new AclEntry(Actions.ForDirectories.ResourceMasterPrefix +
				AuthTools.GetDirectoryName(filesProv, "/Dir"),
				Actions.FullControl, "U.User100", Value.Grant));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsFalse(AuthChecker.CheckActionForDirectory(filesProv, "/Dir",
				Actions.ForDirectories.DeleteFiles, "User", new string[] { "Group" }), "Permission should be denied");

			Assert.IsFalse(AuthChecker.CheckActionForDirectory(filesProv, "/Dir",
				Actions.ForDirectories.UploadFiles, "User", new string[] { "Group" }), "Permission should be denied");

			Assert.IsFalse(AuthChecker.CheckActionForDirectory(filesProv, "/Dir",
				Actions.ForDirectories.UploadFiles, "User2", new string[] { "Group" }), "Permission should be denied");

			Assert.IsFalse(AuthChecker.CheckActionForDirectory(filesProv, "/Dir2",
				Actions.ForDirectories.UploadFiles, "User", new string[] { "Group" }), "Permission should be denied");
		}

		[Test]
		public void CheckActionForDirectory_GrantGroupExplicit() {
			IFilesStorageProviderV30 filesProv = MockFilesProvider();

			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForDirectories.ResourceMasterPrefix +
				AuthTools.GetDirectoryName(filesProv, "/Dir"),
				Actions.ForDirectories.UploadFiles, "G.Group", Value.Grant));
			entries.Add(new AclEntry(Actions.ForDirectories.ResourceMasterPrefix +
				AuthTools.GetDirectoryName(filesProv, "/Dir"),
				Actions.ForDirectories.UploadFiles, "G.Group100", Value.Deny));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsFalse(AuthChecker.CheckActionForDirectory(filesProv, "/Dir",
				Actions.ForDirectories.DeleteFiles, "User", new string[] { "Group" }), "Permission should be denied");

			Assert.IsTrue(AuthChecker.CheckActionForDirectory(filesProv, "/Dir",
				Actions.ForDirectories.UploadFiles, "User", new string[] { "Group" }), "Permission should be granted");

			Assert.IsFalse(AuthChecker.CheckActionForDirectory(filesProv, "/Dir",
				Actions.ForDirectories.UploadFiles, "User", new string[] { "Group2" }), "Permission should be denied");

			Assert.IsFalse(AuthChecker.CheckActionForDirectory(filesProv, "/Dir2",
				Actions.ForDirectories.UploadFiles, "User", new string[] { "Group" }), "Permission should be denied");
		}

		[Test]
		public void CheckActionForDirectory_DenyGroupExplicit() {

			IFilesStorageProviderV30 filesProv = MockFilesProvider();

			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForDirectories.ResourceMasterPrefix +
				AuthTools.GetDirectoryName(filesProv, "/Dir"),
				Actions.ForDirectories.UploadFiles, "G.Group", Value.Deny));
			entries.Add(new AclEntry(Actions.ForDirectories.ResourceMasterPrefix +
				AuthTools.GetDirectoryName(filesProv, "/Dir"),
				Actions.ForDirectories.UploadFiles, "G.Group100", Value.Grant));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsFalse(AuthChecker.CheckActionForDirectory(filesProv, "/Dir",
				Actions.ForDirectories.DeleteFiles, "User", new string[] { "Group" }), "Permission should be denied");

			Assert.IsFalse(AuthChecker.CheckActionForDirectory(filesProv, "/Dir",
				Actions.ForDirectories.UploadFiles, "User", new string[] { "Group" }), "Permission should be denied");

			Assert.IsFalse(AuthChecker.CheckActionForDirectory(filesProv, "/Dir",
				Actions.ForDirectories.UploadFiles, "User", new string[] { "Group2" }), "Permission should be denied");

			Assert.IsFalse(AuthChecker.CheckActionForDirectory(filesProv, "/Dir2",
				Actions.ForDirectories.UploadFiles, "User", new string[] { "Group" }), "Permission should be denied");
		}

		[Test]
		public void CheckActionForDirectory_GrantGroupFullControl() {

			IFilesStorageProviderV30 filesProv = MockFilesProvider();

			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForDirectories.ResourceMasterPrefix +
				AuthTools.GetDirectoryName(filesProv, "/Dir"),
				Actions.FullControl, "G.Group", Value.Grant));
			entries.Add(new AclEntry(Actions.ForDirectories.ResourceMasterPrefix +
				AuthTools.GetDirectoryName(filesProv, "/Dir"),
				Actions.FullControl, "G.Group100", Value.Deny));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsTrue(AuthChecker.CheckActionForDirectory(filesProv, "/Dir",
				Actions.ForDirectories.DeleteFiles, "User", new string[] { "Group" }), "Permission should be granted");

			Assert.IsTrue(AuthChecker.CheckActionForDirectory(filesProv, "/Dir",
				Actions.ForDirectories.UploadFiles, "User", new string[] { "Group" }), "Permission should be granted");

			Assert.IsFalse(AuthChecker.CheckActionForDirectory(filesProv, "/Dir",
				Actions.ForDirectories.UploadFiles, "User", new string[] { "Group2" }), "Permission should be denied");

			Assert.IsFalse(AuthChecker.CheckActionForDirectory(filesProv, "/Dir2",
				Actions.ForDirectories.UploadFiles, "User", new string[] { "Group" }), "Permission should be denied");
		}

		[Test]
		public void CheckActionForDirectory_DenyGroupFullControl() {

			IFilesStorageProviderV30 filesProv = MockFilesProvider();

			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForDirectories.ResourceMasterPrefix +
				AuthTools.GetDirectoryName(filesProv, "/Dir"),
				Actions.FullControl, "G.Group", Value.Deny));
			entries.Add(new AclEntry(Actions.ForDirectories.ResourceMasterPrefix +
				AuthTools.GetDirectoryName(filesProv, "/Dir"),
				Actions.FullControl, "G.Group100", Value.Grant));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsFalse(AuthChecker.CheckActionForDirectory(filesProv, "/Dir",
				Actions.ForDirectories.DeleteFiles, "User", new string[] { "Group" }), "Permission should be denied");

			Assert.IsFalse(AuthChecker.CheckActionForDirectory(filesProv, "/Dir",
				Actions.ForDirectories.UploadFiles, "User", new string[] { "Group" }), "Permission should be denied");

			Assert.IsFalse(AuthChecker.CheckActionForDirectory(filesProv, "/Dir",
				Actions.ForDirectories.UploadFiles, "User", new string[] { "Group2" }), "Permission should be denied");

			Assert.IsFalse(AuthChecker.CheckActionForDirectory(filesProv, "/Dir2",
				Actions.ForDirectories.UploadFiles, "User", new string[] { "Group" }), "Permission should be denied");
		}

		[Test]
		public void CheckActionForDirectory_GrantUserGlobalEscalator() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForGlobals.ResourceMasterPrefix,
				Actions.ForGlobals.ManageFiles, "U.User", Value.Grant));
			entries.Add(new AclEntry(Actions.ForGlobals.ResourceMasterPrefix,
				Actions.ForGlobals.ManageFiles, "U.User100", Value.Deny));

			IFilesStorageProviderV30 filesProv = MockFilesProvider();

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsTrue(AuthChecker.CheckActionForDirectory(filesProv, "/Dir",
				Actions.ForDirectories.UploadFiles, "User", new string[] { "Group" }), "Permission should be granted");
		}

		[Test]
		public void CheckActionForDirectory_GrantGroupGlobalEscalator() {
			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForGlobals.ResourceMasterPrefix,
				Actions.ForGlobals.ManageFiles, "G.Group", Value.Grant));
			entries.Add(new AclEntry(Actions.ForGlobals.ResourceMasterPrefix,
				Actions.ForGlobals.ManageFiles, "G.Group100", Value.Deny));

			IFilesStorageProviderV30 filesProv = MockFilesProvider();

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsTrue(AuthChecker.CheckActionForDirectory(filesProv, "/Dir",
				Actions.ForDirectories.UploadFiles, "User", new string[] { "Group" }), "Permission should be granted");
		}

		[Test]
		public void CheckActionForDirectory_GrantUserLocalEscalator() {
			IFilesStorageProviderV30 filesProv = MockFilesProvider();

			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForDirectories.ResourceMasterPrefix +
				AuthTools.GetDirectoryName(filesProv, "/Dir"),
				Actions.ForDirectories.DownloadFiles, "U.User", Value.Grant));
			entries.Add(new AclEntry(Actions.ForDirectories.ResourceMasterPrefix +
				AuthTools.GetDirectoryName(filesProv, "/Dir"),
				Actions.ForDirectories.DownloadFiles, "U.User100", Value.Deny));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsTrue(AuthChecker.CheckActionForDirectory(filesProv, "/Dir",
				Actions.ForDirectories.List, "User", new string[] { "Group" }), "Permission should be granted");
		}

		[Test]
		public void CheckActionForDirectory_GrantGroupLocalEscalator() {
			IFilesStorageProviderV30 filesProv = MockFilesProvider();

			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForDirectories.ResourceMasterPrefix +
				AuthTools.GetDirectoryName(filesProv, "/Dir"),
				Actions.ForDirectories.DownloadFiles, "G.Group", Value.Grant));
			entries.Add(new AclEntry(Actions.ForDirectories.ResourceMasterPrefix +
				AuthTools.GetDirectoryName(filesProv, "/Dir"),
				Actions.ForDirectories.DownloadFiles, "G.Group100", Value.Deny));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsTrue(AuthChecker.CheckActionForDirectory(filesProv, "/Dir",
				Actions.ForDirectories.List, "User", new string[] { "Group" }), "Permission should be granted");
		}

		[Test]
		public void CheckActionForDirectory_GrantUserGlobalFullControl() {
			IFilesStorageProviderV30 filesProv = MockFilesProvider();

			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForGlobals.ResourceMasterPrefix,
				Actions.FullControl, "U.User", Value.Grant));
			entries.Add(new AclEntry(Actions.ForGlobals.ResourceMasterPrefix,
				Actions.FullControl, "U.User100", Value.Deny));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsTrue(AuthChecker.CheckActionForDirectory(filesProv, "/Dir",
				Actions.ForDirectories.List, "User", new string[] { "Group" }), "Permission should be granted");
		}

		[Test]
		public void CheckActionForDirectory_GrantGroupGlobalFullControl() {
			IFilesStorageProviderV30 filesProv = MockFilesProvider();

			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForGlobals.ResourceMasterPrefix,
				Actions.FullControl, "G.Group", Value.Grant));
			entries.Add(new AclEntry(Actions.ForGlobals.ResourceMasterPrefix,
				Actions.FullControl, "G.Group100", Value.Deny));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsTrue(AuthChecker.CheckActionForDirectory(filesProv, "/Dir",
				Actions.ForDirectories.List, "User", new string[] { "Group" }), "Permission should be granted");
		}

		[Test]
		public void CheckActionForDirectory_GrantUserDirectoryEscalator() {
			IFilesStorageProviderV30 filesProv = MockFilesProvider();

			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForDirectories.ResourceMasterPrefix +
				AuthTools.GetDirectoryName(filesProv, "/Dir"),
				Actions.ForDirectories.DownloadFiles, "U.User", Value.Grant));
			entries.Add(new AclEntry(Actions.ForDirectories.ResourceMasterPrefix +
				AuthTools.GetDirectoryName(filesProv, "/Dir"),
				Actions.ForDirectories.DownloadFiles, "U.User100", Value.Deny));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsTrue(AuthChecker.CheckActionForDirectory(filesProv, "/Dir/Sub",
				Actions.ForDirectories.DownloadFiles, "User", new string[] { "Group" }), "Permission should be granted");
		}

		[Test]
		public void CheckActionForDirectory_GrantGroupDirectoryEscalator() {
			IFilesStorageProviderV30 filesProv = MockFilesProvider();

			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForDirectories.ResourceMasterPrefix +
				AuthTools.GetDirectoryName(filesProv, "/Dir"),
				Actions.ForDirectories.DownloadFiles, "G.Group", Value.Grant));
			entries.Add(new AclEntry(Actions.ForDirectories.ResourceMasterPrefix +
				AuthTools.GetDirectoryName(filesProv, "/Dir"),
				Actions.ForDirectories.DownloadFiles, "G.Group100", Value.Deny));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsTrue(AuthChecker.CheckActionForDirectory(filesProv, "/Dir/Sub",
				Actions.ForDirectories.DownloadFiles, "User", new string[] { "Group" }), "Permission should be granted");
		}

		[Test]
		public void CheckActionForDirectory_GrantUserGlobalEscalator_DenyUserExplicit() {
			IFilesStorageProviderV30 filesProv = MockFilesProvider();

			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForGlobals.ResourceMasterPrefix,
				Actions.ForGlobals.ManageFiles, "U.User", Value.Grant));
			entries.Add(new AclEntry(Actions.ForDirectories.ResourceMasterPrefix +
				AuthTools.GetDirectoryName(filesProv, "/Dir"), Actions.ForDirectories.DownloadFiles,
				"U.User", Value.Deny));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsFalse(AuthChecker.CheckActionForDirectory(filesProv, "/Dir",
				Actions.ForDirectories.DownloadFiles, "User", new string[] { "Group" }), "Permission should be denied");
		}

		[Test]
		public void CheckActionForDirectory_GrantGroupGlobalEscalator_DenyUserExplicit() {
			IFilesStorageProviderV30 filesProv = MockFilesProvider();

			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForGlobals.ResourceMasterPrefix,
				Actions.ForGlobals.ManageFiles, "G.Group", Value.Grant));
			entries.Add(new AclEntry(Actions.ForDirectories.ResourceMasterPrefix +
				AuthTools.GetDirectoryName(filesProv, "/Dir"), Actions.ForDirectories.DownloadFiles,
				"U.User", Value.Deny));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsFalse(AuthChecker.CheckActionForDirectory(filesProv, "/Dir",
				Actions.ForDirectories.DownloadFiles, "User", new string[] { "Group" }), "Permission should be denied");
		}

		[Test]
		public void CheckActionForDirectory_GrantUserLocalEscalator_DenyUserExplicit() {
			IFilesStorageProviderV30 filesProv = MockFilesProvider();

			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForDirectories.ResourceMasterPrefix +
				AuthTools.GetDirectoryName(filesProv, "/Dir"), Actions.ForDirectories.DownloadFiles,
				"U.User", Value.Grant));
			entries.Add(new AclEntry(Actions.ForDirectories.ResourceMasterPrefix +
				AuthTools.GetDirectoryName(filesProv, "/Dir"), Actions.ForDirectories.List,
				"U.User", Value.Deny));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsFalse(AuthChecker.CheckActionForDirectory(filesProv, "/Dir",
				Actions.ForDirectories.List, "User", new string[] { "Group" }), "Permission should be denied");
		}

		[Test]
		public void CheckActionForDirectory_GrantGroupLocalEscalator_DenyUserExplicit() {
			IFilesStorageProviderV30 filesProv = MockFilesProvider();

			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForDirectories.ResourceMasterPrefix +
				AuthTools.GetDirectoryName(filesProv, "/Dir"), Actions.ForDirectories.DownloadFiles,
				"G.Group", Value.Grant));
			entries.Add(new AclEntry(Actions.ForDirectories.ResourceMasterPrefix +
				AuthTools.GetDirectoryName(filesProv, "/Dir"), Actions.ForDirectories.List,
				"U.User", Value.Deny));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsFalse(AuthChecker.CheckActionForDirectory(filesProv, "/Dir",
				Actions.ForDirectories.List, "User", new string[] { "Group" }), "Permission should be denied");
		}

		[Test]
		public void CheckActionForDirectory_GrantUserGlobalFullControl_DenyUserExplicit() {
			IFilesStorageProviderV30 filesProv = MockFilesProvider();

			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForGlobals.ResourceMasterPrefix,
				Actions.FullControl, "U.User", Value.Grant));
			entries.Add(new AclEntry(Actions.ForDirectories.ResourceMasterPrefix +
				AuthTools.GetDirectoryName(filesProv, "/Dir"), Actions.ForDirectories.DownloadFiles,
				"U.User", Value.Deny));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsFalse(AuthChecker.CheckActionForDirectory(filesProv, "/Dir",
				Actions.ForDirectories.DownloadFiles, "User", new string[] { "Group" }), "Permission should be denied");
		}

		[Test]
		public void CheckActionForDirectory_GrantGroupGlobalFullControl_DenyUserExplicit() {
			IFilesStorageProviderV30 filesProv = MockFilesProvider();

			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForGlobals.ResourceMasterPrefix,
				Actions.FullControl, "G.Group", Value.Grant));
			entries.Add(new AclEntry(Actions.ForDirectories.ResourceMasterPrefix +
				AuthTools.GetDirectoryName(filesProv, "/Dir"), Actions.ForDirectories.DownloadFiles,
				"U.User", Value.Deny));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsFalse(AuthChecker.CheckActionForDirectory(filesProv, "/Dir",
				Actions.ForDirectories.DownloadFiles, "User", new string[] { "Group" }), "Permission should be denied");
		}

		[Test]
		public void CheckActionForDirectory_GrantUserDirectoryEscalator_DenyUserExplicit() {
			IFilesStorageProviderV30 filesProv = MockFilesProvider();

			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForDirectories.ResourceMasterPrefix +
				AuthTools.GetDirectoryName(filesProv, "/Dir"),
				Actions.ForDirectories.DownloadFiles, "U.User", Value.Grant));
			entries.Add(new AclEntry(Actions.ForDirectories.ResourceMasterPrefix +
				AuthTools.GetDirectoryName(filesProv, "/Dir/Sub"),
				Actions.ForDirectories.DownloadFiles, "U.User", Value.Deny));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsFalse(AuthChecker.CheckActionForDirectory(filesProv, "/Dir/Sub",
				Actions.ForDirectories.DownloadFiles, "User", new string[] { "Group" }), "Permission should be denied");
		}

		[Test]
		public void CheckActionForDirectory_GrantGroupDirectoryEscalator_DenyUserExplicit() {
			IFilesStorageProviderV30 filesProv = MockFilesProvider();

			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForDirectories.ResourceMasterPrefix +
				AuthTools.GetDirectoryName(filesProv, "/Dir"),
				Actions.ForDirectories.DownloadFiles, "G.Group", Value.Grant));
			entries.Add(new AclEntry(Actions.ForDirectories.ResourceMasterPrefix +
				AuthTools.GetDirectoryName(filesProv, "/Dir/Sub"),
				Actions.ForDirectories.DownloadFiles, "U.User", Value.Deny));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsFalse(AuthChecker.CheckActionForDirectory(filesProv, "/Dir/Sub",
				Actions.ForDirectories.DownloadFiles, "User", new string[] { "Group" }), "Permission should be denied");
		}

		[Test]
		public void CheckActionForDirectory_RandomTestForRootDirectory() {
			IFilesStorageProviderV30 filesProv = MockFilesProvider();

			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForDirectories.ResourceMasterPrefix +
				AuthTools.GetDirectoryName(filesProv, "/"),
				Actions.ForDirectories.List, "U.User", Value.Grant));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsTrue(AuthChecker.CheckActionForDirectory(filesProv, "/", Actions.ForDirectories.List,
				"User", new string[0]), "Permission should be granted");

			Assert.IsFalse(AuthChecker.CheckActionForDirectory(filesProv, "/", Actions.ForDirectories.List,
				"User2", new string[0]), "Permission should be denied");
		}

		[Test]
		public void CheckActionForDirectory_GrantUserDirectoryEscalator_RecursiveName() {
			IFilesStorageProviderV30 filesProv = MockFilesProvider();

			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry(Actions.ForDirectories.ResourceMasterPrefix +
				AuthTools.GetDirectoryName(filesProv, "/"),
				Actions.ForDirectories.List, "U.User", Value.Grant));
			entries.Add(new AclEntry(Actions.ForDirectories.ResourceMasterPrefix +
				AuthTools.GetDirectoryName(filesProv, "/Sub/Sub/"),
				Actions.ForDirectories.List, "U.User2", Value.Grant));

			Collectors.SettingsProvider = MockProvider(entries);

			Assert.IsTrue(AuthChecker.CheckActionForDirectory(filesProv, "/Sub/Sub/Sub", Actions.ForDirectories.List,
				"User", new string[0]), "Permission should be granted");

			Assert.IsTrue(AuthChecker.CheckActionForDirectory(filesProv, "/Sub/Sub/Sub/", Actions.ForDirectories.List,
				"User", new string[0]), "Permission should be granted");

			Assert.IsTrue(AuthChecker.CheckActionForDirectory(filesProv, "/Sub/Sub/Sub/", Actions.ForDirectories.List,
				"User2", new string[0]), "Permission should be granted");

			Assert.IsFalse(AuthChecker.CheckActionForDirectory(filesProv, "/Sub/Sub/Sub", Actions.ForDirectories.List,
				"User2", new string[0]), "Permission should be granted");
		}

	}

}
