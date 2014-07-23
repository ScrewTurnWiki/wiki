
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ScrewTurn.Wiki.AclEngine;

namespace ScrewTurn.Wiki.Plugins.SqlCommon {

	/// <summary>
	/// Implements a SQL ACL Manager.
	/// </summary>
	public class SqlAclManager : IAclManager {

		// This class is similar to AclManagerBase in AclEngine
		// but it does not work with in-memory data.
		// All operations are actually handled by a backend database, via delegates
		// The AclChanged event is never fired

		private StoreEntry _storeEntry;
		private DeleteEntries _deleteEntries;
		private RenameResource _renameResource;
		private RetrieveAllEntries _retrieveAllEntries;
		private RetrieveEntriesForResource _retrieveEntriesForResource;
		private RetrieveEntriesForSubject _retrieveEntriesForSubject;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:SqlAclManager" /> class.
		/// </summary>
		/// <param name="storeEntry">The <see cref="StoreEntry"/> delegate.</param>
		/// <param name="deleteEntries">The <see cref="DeleteEntries"/> delegate.</param>
		/// <param name="renameResource">The <see cref="RenameResource"/> delegate.</param>
		/// <param name="retrieveAllEntries">The <see cref="RetrieveAllEntries"/> delegate.</param>
		/// <param name="retrieveEntriesForResource">The <see cref="RetrieveEntriesForResource"/> delegate.</param>
		/// <param name="retrieveEntriesForSubject">The <see cref="RetrieveEntriesForSubject"/> delegate.</param>
		public SqlAclManager(StoreEntry storeEntry, DeleteEntries deleteEntries, RenameResource renameResource,
			RetrieveAllEntries retrieveAllEntries, RetrieveEntriesForResource retrieveEntriesForResource, RetrieveEntriesForSubject retrieveEntriesForSubject) {

			if(storeEntry == null) throw new ArgumentNullException("storeEntry");
			if(deleteEntries == null) throw new ArgumentNullException("deleteEntries");
			if(renameResource == null) throw new ArgumentNullException("renameResource");
			if(retrieveAllEntries == null) throw new ArgumentNullException("retrieveAllEntries");
			if(retrieveEntriesForResource == null) throw new ArgumentNullException("retrieveEntriesForResource");
			if(retrieveEntriesForSubject == null) throw new ArgumentNullException("retrieveEntriesForSubject");

			_storeEntry = storeEntry;
			_deleteEntries = deleteEntries;
			_renameResource = renameResource;
			_retrieveAllEntries = retrieveAllEntries;
			_retrieveEntriesForResource = retrieveEntriesForResource;
			_retrieveEntriesForSubject = retrieveEntriesForSubject;
		}

		/// <summary>
		/// Handles the invokation of <see cref="IAclManager.AclChanged" /> event.
		/// </summary>
		/// <param name="entries">The changed entries.</param>
		/// <param name="change">The change.</param>
		[Obsolete]
		private void OnAclChanged(AclEntry[] entries, Change change) {
			if(AclChanged != null) {
				AclChanged(this, new AclChangedEventArgs(entries, change));
			}
		}

		/// <summary>
		/// Stores a new ACL entry.
		/// </summary>
		/// <param name="resource">The controlled resource.</param>
		/// <param name="action">The action on the controlled resource.</param>
		/// <param name="subject">The subject whose access to the resource/action is controlled.</param>
		/// <param name="value">The value of the entry.</param>
		/// <returns><c>true</c> if the entry is stored, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="resource"/>, <paramref name="action"/> or <paramref name="subject"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="resource"/>, <paramref name="action"/> or <paramref name="subject"/> are empty.</exception>
		public bool StoreEntry(string resource, string action, string subject, Value value) {
			if(resource == null) throw new ArgumentNullException("resource");
			if(resource.Length == 0) throw new ArgumentException("Resource cannot be empty", "resource");
			if(action == null) throw new ArgumentNullException("action");
			if(action.Length == 0) throw new ArgumentException("Action cannot be empty", "action");
			if(subject == null) throw new ArgumentNullException("subject");
			if(subject.Length == 0) throw new ArgumentException("Subject cannot be empty", "subject");

			AclEntry entry = new AclEntry(resource, action, subject, value);

			return _storeEntry(entry);
		}

		/// <summary>
		/// Deletes an ACL entry.
		/// </summary>
		/// <param name="resource">The controlled resource.</param>
		/// <param name="action">The action on the controlled resource.</param>
		/// <param name="subject">The subject whose access to the resource/action is controlled.</param>
		/// <returns><c>true</c> if the entry is deleted, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="resource"/>, <paramref name="action"/> or <paramref name="subject"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="resource"/>, <paramref name="action"/> or <paramref name="subject"/> are empty.</exception>
		public bool DeleteEntry(string resource, string action, string subject) {
			if(resource == null) throw new ArgumentNullException("resource");
			if(resource.Length == 0) throw new ArgumentException("Resource cannot be empty", "resource");
			if(action == null) throw new ArgumentNullException("action");
			if(action.Length == 0) throw new ArgumentException("Action cannot be empty", "action");
			if(subject == null) throw new ArgumentNullException("subject");
			if(subject.Length == 0) throw new ArgumentException("Subject cannot be empty", "subject");

			AclEntry entry = new AclEntry(resource, action, subject, Value.Deny);

			return _deleteEntries(new[] { entry });
		}

		/// <summary>
		/// Deletes all the ACL entries for a resource.
		/// </summary>
		/// <param name="resource">The controlled resource.</param>
		/// <returns><c>true</c> if the entries are deleted, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="resource"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="resource"/> is empty.</exception>
		public bool DeleteEntriesForResource(string resource) {
			if(resource == null) throw new ArgumentNullException("resource");
			if(resource.Length == 0) throw new ArgumentException("Resource cannot be empty", "resource");

			AclEntry[] entries = _retrieveEntriesForResource(resource);
			return _deleteEntries(entries);
		}

		/// <summary>
		/// Deletes all the ACL entries for a subject.
		/// </summary>
		/// <param name="subject">The subject.</param>
		/// <returns><c>true</c> if the entries are deleted, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="subject"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="subject"/> is empty.</exception>
		public bool DeleteEntriesForSubject(string subject) {
			if(subject == null) throw new ArgumentNullException("subject");
			if(subject.Length == 0) throw new ArgumentException("Subject cannot be empty", "subject");

			AclEntry[] entries = _retrieveEntriesForSubject(subject);
			return _deleteEntries(entries);
		}

		/// <summary>
		/// Renames a resource.
		/// </summary>
		/// <param name="resource">The resource.</param>
		/// <param name="newName">The new name of the resource.</param>
		/// <returns><c>true</c> if the resource is renamed, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="resource"/> or <paramref name="newName"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="resource"/> or <paramref name="newName"/> are empty.</exception>
		public bool RenameResource(string resource, string newName) {
			if(resource == null) throw new ArgumentNullException("resource");
			if(resource.Length == 0) throw new ArgumentException("Resource cannot be empty", "resource");

			if(newName == null) throw new ArgumentNullException("newName");
			if(newName.Length == 0) throw new ArgumentException("New Name cannot be empty", "newName");

			return _renameResource(resource, newName);
		}

		/// <summary>
		/// Retrieves all the ACL entries for a resource.
		/// </summary>
		/// <returns>The entries.</returns>
		public AclEntry[] RetrieveAllEntries() {
			return _retrieveAllEntries();
		}

		/// <summary>
		/// Retrieves all the ACL entries for a resource.
		/// </summary>
		/// <param name="resource">The resource.</param>
		/// <returns>The entries.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="resource"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="resource"/> is empty.</exception>
		public AclEntry[] RetrieveEntriesForResource(string resource) {
			if(resource == null) throw new ArgumentNullException("resource");
			if(resource.Length == 0) throw new ArgumentException("Resource cannot be empty", "resource");

			return _retrieveEntriesForResource(resource);
		}

		/// <summary>
		/// Retrieves all the ACL entries for a subject.
		/// </summary>
		/// <param name="subject">The subject.</param>
		/// <returns>The entries.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="subject"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="subject"/> is empty.</exception>
		public AclEntry[] RetrieveEntriesForSubject(string subject) {
			if(subject == null) throw new ArgumentNullException("subject");
			if(subject.Length == 0) throw new ArgumentException("Subject cannot be empty", "subject");

			return _retrieveEntriesForSubject(subject);
		}

		/// <summary>
		/// Initializes the manager data.
		/// </summary>
		/// <param name="entries">The ACL entries.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="entries"/> is <c>null</c>.</exception>
		public void InitializeData(AclEntry[] entries) {
			if(entries == null) throw new ArgumentNullException("entries");
		}

		/// <summary>
		/// Gets the total number of ACL entries.
		/// </summary>
		public int TotalEntries {
			get {
				return RetrieveAllEntries().Length;
			}
		}

		/// <summary>
		/// Event fired when an ACL entry is stored or deleted.
		/// </summary>
		public event EventHandler<AclChangedEventArgs> AclChanged;

	}

	/// <summary>
	/// Defines a delegate for a method that stores a ACL entry in the storage.
	/// </summary>
	/// <param name="entry">The entry to store.</param>
	/// <returns><c>true</c> if the entry was stored, <c>false</c> otherwise.</returns>
	public delegate bool StoreEntry(AclEntry entry);

	/// <summary>
	/// Defines a delegate for a method that deletes a ACL entry in the storage.
	/// </summary>
	/// <param name="entry">The entry to delete.</param>
	/// <remarks><c>true</c> if the entry was deleted, <c>false</c> otherwise.</remarks>
	public delegate bool DeleteEntry(AclEntry entry);

	/// <summary>
	/// Defines a delegate for a method that deletes ACL entries in the storage.
	/// </summary>
	/// <param name="entries">The entries to delete.</param>
	/// <remarks><c>true</c> if one or more enties were deleted, <c>false</c> otherwise.</remarks>
	public delegate bool DeleteEntries(AclEntry[] entries);

	/// <summary>
	/// Defines a delegate for a method that renames a resource.
	/// </summary>
	/// <param name="resource">The resource to rename.</param>
	/// <param name="newName">The new name of the resource.</param>
	/// <returns><c>true</c> if the resource was renamed, <c>false</c> otherwise.</returns>
	public delegate bool RenameResource(string resource, string newName);

	/// <summary>
	/// Defines a delegate for a method that retrieves all entries.
	/// </summary>
	/// <returns>The entries.</returns>
	public delegate AclEntry[] RetrieveAllEntries();

	/// <summary>
	/// Defines a delegate for a method that retrieves all entries for a resource.
	/// </summary>
	/// <param name="resource">The resource.</param>
	/// <returns>The entries of the resource.</returns>
	public delegate AclEntry[] RetrieveEntriesForResource(string resource);

	/// <summary>
	/// Defines a delegate for a method that retrieves all entries for a subject.
	/// </summary>
	/// <param name="subject">The subject.</param>
	/// <returns>The entries of the subject.</returns>
	public delegate AclEntry[] RetrieveEntriesForSubject(string subject);

}
