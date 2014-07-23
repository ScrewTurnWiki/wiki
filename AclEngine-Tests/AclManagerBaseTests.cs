
using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Rhino.Mocks;

namespace ScrewTurn.Wiki.AclEngine.Tests {

	[TestFixture]
	public class AclManagerBaseTests {

		private MockRepository mocks = new MockRepository();

		private AclManagerBase MockAclManager() {
			AclManagerBase manager = mocks.DynamicMock<AclManagerBase>();

			return manager;
		}

		private void AssertAclEntriesAreEqual(AclEntry expected, AclEntry actual) {
			Assert.AreEqual(expected.Resource, actual.Resource, "Wrong resource");
			Assert.AreEqual(expected.Action, actual.Action, "Wrong action");
			Assert.AreEqual(expected.Subject, actual.Subject, "Wrong subject");
			Assert.AreEqual(expected.Value, actual.Value, "Wrong value");
		}

		[Test]
		public void StoreEntry_RetrieveAllEntries() {
			AclManagerBase manager = MockAclManager();

			Assert.AreEqual(0, manager.TotalEntries, "Wrong initial entry count");
			Assert.AreEqual(0, manager.RetrieveAllEntries().Length, "Wrong initial entry count");

			Assert.IsTrue(manager.StoreEntry("Res", "Action", "U.User", Value.Grant), "StoreEntry should return true");
			Assert.IsTrue(manager.StoreEntry("Res", "Action", "G.Group", Value.Deny), "StoreEntry should return true");

			Assert.AreEqual(2, manager.TotalEntries, "Wrong entry count");

			AclEntry[] allEntries = manager.RetrieveAllEntries();
			Assert.AreEqual(2, allEntries.Length, "Wrong entry count");

			Array.Sort(allEntries, delegate(AclEntry x, AclEntry y) { return x.Subject.CompareTo(y.Subject); });

			AssertAclEntriesAreEqual(new AclEntry("Res", "Action", "G.Group", Value.Deny), allEntries[0]);
			AssertAclEntriesAreEqual(new AclEntry("Res", "Action", "U.User", Value.Grant), allEntries[1]);
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void StoreEntry_InvalidResource(string s) {
			AclManagerBase manager = MockAclManager();
			manager.StoreEntry(s, "Action", "U.User", Value.Grant);
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void StoreEntry_InvalidAction(string a) {
			AclManagerBase manager = MockAclManager();
			manager.StoreEntry("Res", a, "U.User", Value.Grant);
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void StoreEntry_InvalidSubject(string s) {
			AclManagerBase manager = MockAclManager();
			manager.StoreEntry("Res", "Action", s, Value.Grant);
		}

		[Test]
		public void StoreEntry_Overwrite() {
			AclManagerBase manager = MockAclManager();

			Assert.IsTrue(manager.StoreEntry("Res", "Action", "U.User", Value.Grant), "StoreEntry should return true");
			Assert.IsTrue(manager.StoreEntry("Res", "Action", "G.Group", Value.Grant), "StoreEntry should return true");

			// Overwrite with a deny
			Assert.IsTrue(manager.StoreEntry("Res", "Action", "U.User", Value.Deny), "StoreEntry should return true");

			Assert.AreEqual(2, manager.TotalEntries, "Wrong entry count");

			AclEntry[] allEntries = manager.RetrieveAllEntries();
			Assert.AreEqual(2, allEntries.Length, "Wrong entry count");

			Array.Sort(allEntries, delegate(AclEntry x, AclEntry y) { return x.Subject.CompareTo(y.Subject); });

			AssertAclEntriesAreEqual(new AclEntry("Res", "Action", "G.Group", Value.Grant), allEntries[0]);
			AssertAclEntriesAreEqual(new AclEntry("Res", "Action", "U.User", Value.Deny), allEntries[1]);
		}

		[Test]
		public void DeleteEntry_RetrieveAllEntries() {
			AclManagerBase manager = MockAclManager();

			Assert.IsTrue(manager.StoreEntry("Res", "Action", "U.User", Value.Grant), "StoreEntry should return true");
			Assert.IsTrue(manager.StoreEntry("Res", "Action", "G.Group", Value.Deny), "StoreEntry should return true");

			Assert.AreEqual(2, manager.TotalEntries, "Wrong entry count");

			Assert.IsFalse(manager.DeleteEntry("Res1", "Action", "G.Group"), "DeleteEntry should return false");
			Assert.IsFalse(manager.DeleteEntry("Res", "Action1", "G.Group"), "DeleteEntry should return false");
			Assert.IsFalse(manager.DeleteEntry("Res", "Action", "G.Group1"), "DeleteEntry should return false");

			Assert.AreEqual(2, manager.TotalEntries, "Wrong entry count");

			Assert.IsTrue(manager.DeleteEntry("Res", "Action", "G.Group"), "DeleteEntry should return true");

			Assert.AreEqual(1, manager.TotalEntries, "Wrong entry count");

			AclEntry[] allEntries = manager.RetrieveAllEntries();
			Assert.AreEqual(1, allEntries.Length, "Wrong entry count");

			AssertAclEntriesAreEqual(new AclEntry("Res", "Action", "U.User", Value.Grant), allEntries[0]);
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void DeleteEntry_InvalidResource(string r) {
			AclManagerBase manager = MockAclManager();
			manager.DeleteEntry(r, "Action", "U.User");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void DeleteEntry_InvalidAction(string a) {
			AclManagerBase manager = MockAclManager();
			manager.DeleteEntry("Res", a, "U.User");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void DeleteEntry_InvalidSubject(string s) {
			AclManagerBase manager = MockAclManager();
			manager.DeleteEntry("Res", "Action", s);
		}

		[Test]
		public void DeleteEntriesForResource_RetrieveAllEntries() {
			AclManagerBase manager = MockAclManager();

			Assert.IsTrue(manager.StoreEntry("Res", "Action", "U.User", Value.Grant), "StoreEntry should return true");
			Assert.IsTrue(manager.StoreEntry("Res", "Action", "G.Group", Value.Deny), "StoreEntry should return true");
			Assert.IsTrue(manager.StoreEntry("Res2", "Action2", "G.Group2", Value.Grant), "StoreEntry should return true");
			Assert.IsTrue(manager.StoreEntry("Res2", "Action3", "U.User", Value.Deny), "StoreEntry should return true");

			Assert.AreEqual(4, manager.TotalEntries, "Wrong entry count");

			Assert.IsFalse(manager.DeleteEntriesForResource("Inexistent"), "DeleteEntriesForResource should return false");

			Assert.AreEqual(4, manager.TotalEntries, "Wrong entry count");

			Assert.IsTrue(manager.DeleteEntriesForResource("Res2"), "DeleteEntriesForResource should return true");

			Assert.AreEqual(2, manager.TotalEntries, "Wrong entry count");

			AclEntry[] allEntries = manager.RetrieveAllEntries();
			Assert.AreEqual(2, allEntries.Length, "Wrong entry count");

			Array.Sort(allEntries, delegate(AclEntry x, AclEntry y) { return x.Subject.CompareTo(y.Subject); });

			AssertAclEntriesAreEqual(new AclEntry("Res", "Action", "G.Group", Value.Deny), allEntries[0]);
			AssertAclEntriesAreEqual(new AclEntry("Res", "Action", "U.User", Value.Grant), allEntries[1]);
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void DeleteEntriesForResource_InvalidResource(string r) {
			AclManagerBase manager = MockAclManager();
			manager.DeleteEntriesForResource(r);
		}

		[Test]
		public void DeleteEntriesForSubject_RetrieveAllEntries() {
			AclManagerBase manager = MockAclManager();

			Assert.IsTrue(manager.StoreEntry("Res", "Action", "U.User", Value.Grant), "StoreEntry should return true");
			Assert.IsTrue(manager.StoreEntry("Res", "Action", "G.Group", Value.Deny), "StoreEntry should return true");
			Assert.IsTrue(manager.StoreEntry("Res2", "Action2", "G.Group2", Value.Grant), "StoreEntry should return true");
			Assert.IsTrue(manager.StoreEntry("Res2", "Action3", "G.Group2", Value.Deny), "StoreEntry should return true");

			Assert.AreEqual(4, manager.TotalEntries, "Wrong entry count");

			Assert.IsFalse(manager.DeleteEntriesForSubject("I.Inexistent"), "DeleteEntriesForSubject should return false");

			Assert.AreEqual(4, manager.TotalEntries, "Wrong entry count");

			Assert.IsTrue(manager.DeleteEntriesForSubject("G.Group2"), "DeleteEntriesForSubject should return true");

			Assert.AreEqual(2, manager.TotalEntries, "Wrong entry count");

			AclEntry[] allEntries = manager.RetrieveAllEntries();
			Assert.AreEqual(2, allEntries.Length, "Wrong entry count");

			Array.Sort(allEntries, delegate(AclEntry x, AclEntry y) { return x.Subject.CompareTo(y.Subject); });

			AssertAclEntriesAreEqual(new AclEntry("Res", "Action", "G.Group", Value.Deny), allEntries[0]);
			AssertAclEntriesAreEqual(new AclEntry("Res", "Action", "U.User", Value.Grant), allEntries[1]);
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void DeleteEntriesForSubject_InvalidSubject(string s) {
			AclManagerBase manager = MockAclManager();
			manager.DeleteEntriesForSubject(s);
		}

		[Test]
		public void RetrieveEntriesForResource() {
			AclManagerBase manager = MockAclManager();

			Assert.IsTrue(manager.StoreEntry("Res", "Action", "U.User", Value.Grant), "StoreEntry should return true");
			Assert.IsTrue(manager.StoreEntry("Res", "Action", "G.Group", Value.Deny), "StoreEntry should return true");
			Assert.IsTrue(manager.StoreEntry("Res2", "Action2", "G.Group2", Value.Grant), "StoreEntry should return true");
			Assert.IsTrue(manager.StoreEntry("Res2", "Action3", "G.Group2", Value.Deny), "StoreEntry should return true");

			Assert.AreEqual(0, manager.RetrieveEntriesForResource("Inexistent").Length, "Wrong result count");

			AclEntry[] allEntries = manager.RetrieveEntriesForResource("Res");
			Assert.AreEqual(2, allEntries.Length, "Wrong entry count");

			Array.Sort(allEntries, delegate(AclEntry x, AclEntry y) { return x.Subject.CompareTo(y.Subject); });

			AssertAclEntriesAreEqual(new AclEntry("Res", "Action", "G.Group", Value.Deny), allEntries[0]);
			AssertAclEntriesAreEqual(new AclEntry("Res", "Action", "U.User", Value.Grant), allEntries[1]);
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void RetrieveEntriesForResource_InvalidResource(string r) {
			AclManagerBase manager = MockAclManager();
			manager.RetrieveEntriesForResource(r);
		}

		[Test]
		public void RetrieveEntriesForSubject() {
			AclManagerBase manager = MockAclManager();

			Assert.IsTrue(manager.StoreEntry("Res", "Action", "U.User", Value.Grant), "StoreEntry should return true");
			Assert.IsTrue(manager.StoreEntry("Res", "Action", "G.Group", Value.Deny), "StoreEntry should return true");
			Assert.IsTrue(manager.StoreEntry("Res2", "Action2", "G.Group2", Value.Grant), "StoreEntry should return true");
			Assert.IsTrue(manager.StoreEntry("Res2", "Action3", "G.Group2", Value.Deny), "StoreEntry should return true");

			Assert.AreEqual(0, manager.RetrieveEntriesForSubject("I.Inexistent").Length, "Wrong result count");

			AclEntry[] allEntries = manager.RetrieveEntriesForSubject("G.Group2");
			Assert.AreEqual(2, allEntries.Length, "Wrong entry count");

			Array.Sort(allEntries, delegate(AclEntry x, AclEntry y) { return x.Action.CompareTo(y.Action); });

			AssertAclEntriesAreEqual(new AclEntry("Res2", "Action2", "G.Group2", Value.Grant), allEntries[0]);
			AssertAclEntriesAreEqual(new AclEntry("Res2", "Action3", "G.Group2", Value.Deny), allEntries[1]);
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void RetrieveEntriesForSubject_InvalidSubject(string s) {
			AclManagerBase manager = MockAclManager();
			manager.RetrieveEntriesForSubject(s);
		}

		[Test]
		public void InitializeData() {
			AclManagerBase manager = MockAclManager();

			List<AclEntry> entries = new List<AclEntry>();
			entries.Add(new AclEntry("Res", "Action", "U.User", Value.Grant));
			entries.Add(new AclEntry("Res", "Action", "G.Group", Value.Deny));

			manager.InitializeData(entries.ToArray());

			Assert.AreEqual(2, manager.TotalEntries, "Wrong entry count");

			AclEntry[] allEntries = manager.RetrieveAllEntries();
			Assert.AreEqual(2, allEntries.Length, "Wrong entry count");

			Array.Sort(allEntries, delegate(AclEntry x, AclEntry y) { return x.Subject.CompareTo(y.Subject); });

			AssertAclEntriesAreEqual(new AclEntry("Res", "Action", "G.Group", Value.Deny), allEntries[0]);
			AssertAclEntriesAreEqual(new AclEntry("Res", "Action", "U.User", Value.Grant), allEntries[1]);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void InitializeData_NullData() {
			AclManagerBase manager = MockAclManager();
			manager.InitializeData(null);
		}

		[Test]
		public void Event_AclChanged_StoreEntry() {
			AclManagerBase manager = MockAclManager();

			AclEntry entry = new AclEntry("Res", "Action", "U.User", Value.Grant);

			bool invoked = false;
			manager.AclChanged += delegate(object sender, AclChangedEventArgs e) {
				invoked = true;
				Assert.AreEqual(1, e.Entries.Length, "Wrong entry count");
				AssertAclEntriesAreEqual(entry, e.Entries[0]);
				Assert.AreEqual(Change.EntryStored, e.Change);
			};

			manager.StoreEntry(entry.Resource, entry.Action, entry.Subject, entry.Value);

			Assert.IsTrue(invoked, "Store event not invoked");
		}

		[Test]
		public void Event_AclChanged_OverwriteEntry() {
			AclManagerBase manager = MockAclManager();

			AclEntry entryOld = new AclEntry("Res", "Action", "U.User", Value.Deny);

			AclEntry entryNew = new AclEntry("Res", "Action", "U.User", Value.Grant);

			manager.StoreEntry(entryOld.Resource, entryOld.Action, entryOld.Subject, entryOld.Value);

			bool invokedStore = false;
			bool invokedDelete = false;
			manager.AclChanged += delegate(object sender, AclChangedEventArgs e) {
				if(e.Change == Change.EntryStored) {
					invokedStore = true;
					Assert.AreEqual(1, e.Entries.Length, "Wrong entry count");
					AssertAclEntriesAreEqual(entryNew, e.Entries[0]);
					Assert.AreEqual(Change.EntryStored, e.Change);
				}
				else {
					invokedDelete = true;
					Assert.AreEqual(1, e.Entries.Length, "Wrong entry count");
					AssertAclEntriesAreEqual(entryOld, e.Entries[0]);
					Assert.AreEqual(Change.EntryDeleted, e.Change);
				}
			};

			manager.StoreEntry(entryNew.Resource, entryNew.Action, entryNew.Subject, entryNew.Value);

			Assert.IsTrue(invokedStore, "Store event not invoked");
			Assert.IsTrue(invokedDelete, "Delete event not invoked");
		}

		[Test]
		public void Event_AclChanged_DeleteEntry() {
			AclManagerBase manager = MockAclManager();

			AclEntry entry = new AclEntry("Res", "Action", "U.User", Value.Grant);

			manager.StoreEntry(entry.Resource, entry.Action, entry.Subject, entry.Value);

			bool invokedDelete = false;
			manager.AclChanged += delegate(object sender, AclChangedEventArgs e) {
				invokedDelete = true;
				Assert.AreEqual(1, e.Entries.Length, "Wrong entry count");
				AssertAclEntriesAreEqual(entry, e.Entries[0]);
				Assert.AreEqual(Change.EntryDeleted, e.Change, "Wrong change");
			};

			manager.DeleteEntry(entry.Resource, entry.Action, entry.Subject);

			Assert.IsTrue(invokedDelete, "Delete event not invoked");
		}

		[Test]
		public void Event_AclChanged_DeleteEntriesForResource() {
			AclManagerBase manager = MockAclManager();

			AclEntry entry = new AclEntry("Res", "Action", "U.User", Value.Grant);

			manager.StoreEntry(entry.Resource, entry.Action, entry.Subject, entry.Value);
			manager.StoreEntry("Res2", "Action", "G.Group", Value.Deny);

			bool invokedDelete = false;
			manager.AclChanged += delegate(object sender, AclChangedEventArgs e) {
				invokedDelete = true;
				Assert.AreEqual(1, e.Entries.Length, "Wrong entry count");
				AssertAclEntriesAreEqual(entry, e.Entries[0]);
				Assert.AreEqual(Change.EntryDeleted, e.Change, "Wrong change");
			};

			manager.DeleteEntriesForResource(entry.Resource);

			Assert.IsTrue(invokedDelete, "Delete event not invoked");
		}

		[Test]
		public void Event_AclChanged_DeleteEntriesForSubject() {
			AclManagerBase manager = MockAclManager();

			AclEntry entry = new AclEntry("Res", "Action", "U.User", Value.Grant);

			manager.StoreEntry(entry.Resource, entry.Action, entry.Subject, entry.Value);
			manager.StoreEntry("Res2", "Action", "G.Group", Value.Deny);

			bool invokedDelete = false;
			manager.AclChanged += delegate(object sender, AclChangedEventArgs e) {
				invokedDelete = true;
				Assert.AreEqual(1, e.Entries.Length, "Wrong entry count");
				AssertAclEntriesAreEqual(entry, e.Entries[0]);
				Assert.AreEqual(Change.EntryDeleted, e.Change, "Wrong change");
			};

			manager.DeleteEntriesForSubject(entry.Subject);

			Assert.IsTrue(invokedDelete, "Delete event not invoked");
		}

		[Test]
		public void RenameResource() {
			AclManagerBase manager = MockAclManager();

			Assert.IsFalse(manager.RenameResource("Res", "Res_Renamed"), "RenameResource should return false");

			AclEntry entry1 = new AclEntry("Res", "Action", "U.User", Value.Grant);
			AclEntry newEntry1 = new AclEntry("Res_Renamed", "Action", "U.User", Value.Grant);
			AclEntry entry2 = new AclEntry("Res", "Action2", "U.User2", Value.Deny);
			AclEntry newEntry2 = new AclEntry("Res_Renamed", "Action2", "U.User", Value.Deny);

			manager.StoreEntry(entry1.Resource, entry1.Action, entry1.Subject, entry1.Value);
			manager.StoreEntry(entry2.Resource, entry2.Action, entry2.Subject, entry2.Value);
			manager.StoreEntry("Res2", "Action", "G.Group", Value.Deny);

			bool invokedDelete1 = false;
			bool invokedStore1 = false;
			bool invokedDelete2 = false;
			bool invokedStore2 = false;
			manager.AclChanged += delegate(object sender, AclChangedEventArgs e) {
				if(e.Change == Change.EntryDeleted) {
					Assert.AreEqual(1, e.Entries.Length, "Wrong entry count");
					Assert.AreEqual("Res", e.Entries[0].Resource, "Wrong resource");

					if(e.Entries[0].Action == entry1.Action) invokedDelete1 = true;
					if(e.Entries[0].Action == entry2.Action) invokedDelete2 = true;
				}
				else {
					Assert.AreEqual(1, e.Entries.Length, "Wrong entry count");
					Assert.AreEqual("Res_Renamed", e.Entries[0].Resource, "Wrong resource");

					if(e.Entries[0].Action == entry1.Action) invokedStore1 = true;
					if(e.Entries[0].Action == entry2.Action) invokedStore2 = true;
				}
			};

			Assert.IsTrue(manager.RenameResource("Res", "Res_Renamed"), "RenameResource should return true");

			Assert.IsTrue(invokedDelete1, "Delete event 1 not invoked");
			Assert.IsTrue(invokedStore1, "Store event 1 not invoked");
			Assert.IsTrue(invokedDelete2, "Delete event 2 not invoked");
			Assert.IsTrue(invokedStore2, "Store event 2 not invoked");

			AclEntry[] entries = manager.RetrieveAllEntries();

			Assert.AreEqual(3, entries.Length, "Wrong entry count");
			Array.Sort(entries, delegate(AclEntry x, AclEntry y) { return x.Resource.CompareTo(y.Resource); });

			Assert.AreEqual("Res_Renamed", entries[0].Resource, "Wrong resource");
			if(entries[0].Value == Value.Grant) {
				Assert.AreEqual("Action", entries[0].Action, "Wrong action");
				Assert.AreEqual("U.User", entries[0].Subject, "Wrong subject");
			}
			else {
				Assert.AreEqual("Action2", entries[0].Action, "Wrong action");
				Assert.AreEqual("U.User2", entries[0].Subject, "Wrong subject");
			}

			Assert.AreEqual("Res_Renamed", entries[1].Resource, "Wrong resource");
			if(entries[1].Value == Value.Grant) {
				Assert.AreEqual("Action", entries[1].Action, "Wrong action");
				Assert.AreEqual("U.User", entries[1].Subject, "Wrong subject");
			}
			else {
				Assert.AreEqual("Action2", entries[1].Action, "Wrong action");
				Assert.AreEqual("U.User2", entries[1].Subject, "Wrong subject");
			}

			Assert.AreEqual("Res2", entries[2].Resource, "Wrong resource");
			Assert.AreEqual("Action", entries[2].Action, "Wrong action");
			Assert.AreEqual("G.Group", entries[2].Subject, "Wrong subject");
			Assert.AreEqual(Value.Deny, entries[2].Value, "Wrong value");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void RenameResource_InvalidResource(string r) {
			AclManagerBase manager = MockAclManager();

			manager.RenameResource(r, "new_name");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void RenameResource_InvalidNewName(string n) {
			AclManagerBase manager = MockAclManager();

			manager.RenameResource("res", n);
		}

	}

}
