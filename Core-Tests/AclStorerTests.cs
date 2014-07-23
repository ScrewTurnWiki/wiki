
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using Rhino.Mocks;
using ScrewTurn.Wiki.AclEngine;

namespace ScrewTurn.Wiki.Tests {

	[TestFixture]
	public class AclStorerTests {

		private string testFile = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), "__ACL_File.dat");

		private MockRepository mocks = new MockRepository();

		private IAclManager MockAclManager() {
			IAclManager manager = mocks.DynamicMock<AclManagerBase>();

			return manager;
		}

		[TearDown]
		public void TearDown() {
			try {
				File.Delete(testFile);
			}
			catch { }
		}

		[Test]
		public void Constructor() {
			IAclManager manager = MockAclManager();

			AclStorer storer = new AclStorer(manager, testFile);
			Assert.AreSame(manager, storer.AclManager, "Wrong ACL Manager instance");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void Constructor_NullAclManager() {
			AclStorer storer = new AclStorer(null, testFile);
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void Constructor_InvalidFile(string f) {
			AclStorer storer = new AclStorer(MockAclManager(), f);
		}

		private void AssertAclEntriesAreEqual(AclEntry expected, AclEntry actual) {
			Assert.AreEqual(expected.Resource, actual.Resource, "Wrong resource");
			Assert.AreEqual(expected.Action, actual.Action, "Wrong action");
			Assert.AreEqual(expected.Subject, actual.Subject, "Wrong subject");
			Assert.AreEqual(expected.Value, actual.Value, "Wrong value");
		}

		[Test]
		public void Store_LoadData() {
			IAclManager manager = MockAclManager();

			AclStorer storer = new AclStorer(manager, testFile);

			manager.StoreEntry("Res1", "Action1", "U.User", Value.Grant);
			manager.StoreEntry("Res2", "Action2", "G.Group", Value.Deny);

			storer.Dispose();
			storer = null;

			manager = MockAclManager();

			storer = new AclStorer(manager, testFile);
			storer.LoadData();

			Assert.AreEqual(2, manager.TotalEntries, "Wrong entry count");

			AclEntry[] allEntries = manager.RetrieveAllEntries();
			Assert.AreEqual(2, allEntries.Length, "Wrong entry count");

			Array.Sort(allEntries, delegate(AclEntry x, AclEntry y) { return x.Subject.CompareTo(y.Subject); });
			AssertAclEntriesAreEqual(new AclEntry("Res2", "Action2", "G.Group", Value.Deny), allEntries[0]);
			AssertAclEntriesAreEqual(new AclEntry("Res1", "Action1", "U.User", Value.Grant), allEntries[1]);
		}

		[Test]
		public void Delete_LoadData() {
			IAclManager manager = MockAclManager();

			AclStorer storer = new AclStorer(manager, testFile);

			manager.StoreEntry("Res1", "Action1", "U.User", Value.Grant);
			manager.StoreEntry("Res2", "Action2", "G.Group", Value.Deny);
			manager.StoreEntry("Res3", "Action1", "U.User", Value.Grant);
			manager.StoreEntry("Res3", "Action2", "G.Group", Value.Deny);

			manager.DeleteEntriesForResource("Res3");

			storer.Dispose();
			storer = null;

			manager = MockAclManager();

			storer = new AclStorer(manager, testFile);
			storer.LoadData();

			Assert.AreEqual(2, manager.TotalEntries, "Wrong entry count");

			AclEntry[] allEntries = manager.RetrieveAllEntries();
			Assert.AreEqual(2, allEntries.Length, "Wrong entry count");

			Array.Sort(allEntries, delegate(AclEntry x, AclEntry y) { return x.Subject.CompareTo(y.Subject); });
			AssertAclEntriesAreEqual(new AclEntry("Res2", "Action2", "G.Group", Value.Deny), allEntries[0]);
			AssertAclEntriesAreEqual(new AclEntry("Res1", "Action1", "U.User", Value.Grant), allEntries[1]);
		}

		[Test]
		public void Overwrite_LoadData() {
			IAclManager manager = MockAclManager();

			AclStorer storer = new AclStorer(manager, testFile);

			manager.StoreEntry("Res1", "Action1", "U.User", Value.Grant);
			manager.StoreEntry("Res2", "Action2", "G.Group", Value.Grant);
			manager.StoreEntry("Res2", "Action2", "G.Group", Value.Deny); // Overwrite

			storer.Dispose();
			storer = null;

			manager = MockAclManager();

			storer = new AclStorer(manager, testFile);
			storer.LoadData();

			Assert.AreEqual(2, manager.TotalEntries, "Wrong entry count");

			AclEntry[] allEntries = manager.RetrieveAllEntries();
			Assert.AreEqual(2, allEntries.Length, "Wrong entry count");

			Array.Sort(allEntries, delegate(AclEntry x, AclEntry y) { return x.Subject.CompareTo(y.Subject); });
			AssertAclEntriesAreEqual(new AclEntry("Res2", "Action2", "G.Group", Value.Deny), allEntries[0]);
			AssertAclEntriesAreEqual(new AclEntry("Res1", "Action1", "U.User", Value.Grant), allEntries[1]);
		}

	}

}
