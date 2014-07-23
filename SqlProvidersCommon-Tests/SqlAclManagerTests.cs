
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Rhino.Mocks;
using ScrewTurn.Wiki.AclEngine;

namespace ScrewTurn.Wiki.Plugins.SqlCommon.Tests {

	[TestFixture]
	public class SqlAclManagerTests {

		// Note: AclEntry overrides Equals

		private MockRepository _mocks;

		private StoreEntry _storeEntry;
		private DeleteEntries _deleteEntries;
		private RenameResource _renameResource;
		private RetrieveAllEntries _retrieveAllEntries;
		private RetrieveEntriesForResource _retrieveEntriesForResource;
		private RetrieveEntriesForSubject _retrieveEntriesForSubject;

		[SetUp]
		public void SetUp() {
			_mocks = new MockRepository();

			_storeEntry = _mocks.StrictMock<StoreEntry>();
			_deleteEntries = _mocks.StrictMock<DeleteEntries>();
			_renameResource = _mocks.StrictMock<RenameResource>();
			_retrieveAllEntries = _mocks.StrictMock<RetrieveAllEntries>();
			_retrieveEntriesForResource = _mocks.StrictMock<RetrieveEntriesForResource>();
			_retrieveEntriesForSubject = _mocks.StrictMock<RetrieveEntriesForSubject>();
		}

		[Test]
		public void StoreEntry() {
			_storeEntry.Stub(x => x(new AclEntry("res", "action", "subject", Value.Grant))).Return(true);

			_mocks.ReplayAll();

			SqlAclManager manager = new SqlAclManager(_storeEntry, _deleteEntries, _renameResource, _retrieveAllEntries, _retrieveEntriesForResource, _retrieveEntriesForSubject);

			Assert.IsTrue(manager.StoreEntry("res", "action", "subject", Value.Grant), "StoreEntry should return true");

			_mocks.VerifyAll();
		}

		[Test]
		public void DeleteEntry() {
			_deleteEntries.Stub(x => x(new[] { new AclEntry("res", "action", "subject", Value.Deny) })).Return(true);

			_mocks.ReplayAll();

			SqlAclManager manager = new SqlAclManager(_storeEntry, _deleteEntries, _renameResource, _retrieveAllEntries, _retrieveEntriesForResource, _retrieveEntriesForSubject);

			Assert.IsTrue(manager.DeleteEntry("res", "action", "subject"), "DeleteEntry should return true");

			_mocks.VerifyAll();
		}

		[Test]
		public void DeleteEntriesForResource() {
			AclEntry[] entries = new[] {
				new AclEntry("res", "action1", "subject1", Value.Grant),
				new AclEntry("res", "action1", "subject2", Value.Deny),
				new AclEntry("res", "action2", "subject1", Value.Grant)
			};

			_retrieveEntriesForResource.Stub(x => x("res")).Return(entries);
			_deleteEntries.Stub(x => x(entries)).Return(true);

			_mocks.ReplayAll();

			SqlAclManager manager = new SqlAclManager(_storeEntry, _deleteEntries, _renameResource, _retrieveAllEntries, _retrieveEntriesForResource, _retrieveEntriesForSubject);

			Assert.IsTrue(manager.DeleteEntriesForResource("res"), "DeleteEntriesForResource should return true");

			_mocks.VerifyAll();
		}

		[Test]
		public void DeleteEntriesForSubject() {
			AclEntry[] entries = new[] {
				new AclEntry("res1", "action1", "subject", Value.Grant),
				new AclEntry("res1", "action1", "subject", Value.Deny),
				new AclEntry("res2", "action2", "subject", Value.Grant)
			};

			_retrieveEntriesForSubject.Stub(x => x("subject")).Return(entries);
			_deleteEntries.Stub(x => x(entries)).Return(true);

			_mocks.ReplayAll();

			SqlAclManager manager = new SqlAclManager(_storeEntry, _deleteEntries, _renameResource, _retrieveAllEntries, _retrieveEntriesForResource, _retrieveEntriesForSubject);

			Assert.IsTrue(manager.DeleteEntriesForSubject("subject"), "DeleteEntriesForSubject should return true");

			_mocks.VerifyAll();
		}

		[Test]
		public void RenameResource() {
			_renameResource.Stub(x => x("res", "newName")).Return(true);

			_mocks.ReplayAll();

			SqlAclManager manager = new SqlAclManager(_storeEntry, _deleteEntries, _renameResource, _retrieveAllEntries, _retrieveEntriesForResource, _retrieveEntriesForSubject);

			Assert.IsTrue(manager.RenameResource("res", "newName"), "RenameResource should return true");

			_mocks.VerifyAll();
		}

		[Test]
		public void RetrieveAllEntries() {
			AclEntry[] entries = new[] {
				new AclEntry("res1", "action1", "subject", Value.Grant),
				new AclEntry("res1", "action1", "subject", Value.Deny),
				new AclEntry("res2", "action2", "subject", Value.Grant)
			};

			_retrieveAllEntries.Stub(x => { x(); }).Return(entries);

			_mocks.ReplayAll();

			SqlAclManager manager = new SqlAclManager(_storeEntry, _deleteEntries, _renameResource, _retrieveAllEntries, _retrieveEntriesForResource, _retrieveEntriesForSubject);

			// Returned array reference-equals entries
			Assert.AreEqual(entries, manager.RetrieveAllEntries(), "Wrong array returned");

			_mocks.VerifyAll();
		}

		[Test]
		public void RetrieveEntriesForResource() {
			AclEntry[] entries = new[] {
				new AclEntry("res", "action1", "subject1", Value.Grant),
				new AclEntry("res", "action1", "subject2", Value.Deny),
				new AclEntry("res", "action2", "subject1", Value.Grant)
			};

			_retrieveEntriesForResource.Stub(x => x("res")).Return(entries);

			_mocks.ReplayAll();

			SqlAclManager manager = new SqlAclManager(_storeEntry, _deleteEntries, _renameResource, _retrieveAllEntries, _retrieveEntriesForResource, _retrieveEntriesForSubject);

			// Returned array reference-equals entries
			Assert.AreEqual(entries, manager.RetrieveEntriesForResource("res"), "Wrong array returned");

			_mocks.VerifyAll();
		}

		[Test]
		public void RetrieveEntriesForSubject() {
			AclEntry[] entries = new[] {
				new AclEntry("res1", "action1", "subject", Value.Grant),
				new AclEntry("res1", "action1", "subject", Value.Deny),
				new AclEntry("res2", "action2", "subject", Value.Grant)
			};

			_retrieveEntriesForSubject.Stub(x => x("subject")).Return(entries);

			_mocks.ReplayAll();

			SqlAclManager manager = new SqlAclManager(_storeEntry, _deleteEntries, _renameResource, _retrieveAllEntries, _retrieveEntriesForResource, _retrieveEntriesForSubject);

			// Returned array reference-equals entries
			Assert.AreEqual(entries, manager.RetrieveEntriesForSubject("subject"), "Wrong array returned");

			_mocks.VerifyAll();
		}

		[Test]
		public void TotalEntries() {
			AclEntry[] entries = new[] {
				new AclEntry("res1", "action1", "subject", Value.Grant),
				new AclEntry("res1", "action1", "subject", Value.Deny),
				new AclEntry("res2", "action2", "subject", Value.Grant)
			};

			_retrieveAllEntries.Stub(x => x()).Return(entries);

			_mocks.ReplayAll();

			SqlAclManager manager = new SqlAclManager(_storeEntry, _deleteEntries, _renameResource, _retrieveAllEntries, _retrieveEntriesForResource, _retrieveEntriesForSubject);

			Assert.AreEqual(entries.Length, manager.TotalEntries, "Wrong entry count");

			_mocks.VerifyAll();
		}

	}

}
