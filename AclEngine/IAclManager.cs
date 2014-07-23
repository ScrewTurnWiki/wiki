
using System;
using System.Collections.Generic;
using System.Text;

namespace ScrewTurn.Wiki.AclEngine {

	/// <summary>
	/// Defines an interface for an ACL manager.
	/// </summary>
	public interface IAclManager {

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
		bool StoreEntry(string resource, string action, string subject, Value value);

		/// <summary>
		/// Deletes an ACL entry.
		/// </summary>
		/// <param name="resource">The controlled resource.</param>
		/// <param name="action">The action on the controlled resource.</param>
		/// <param name="subject">The subject whose access to the resource/action is controlled.</param>
		/// <returns><c>true</c> if the entry is deleted, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="resource"/>, <paramref name="action"/> or <paramref name="subject"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="resource"/>, <paramref name="action"/> or <paramref name="subject"/> are empty.</exception>
		bool DeleteEntry(string resource, string action, string subject);

		/// <summary>
		/// Deletes all the ACL entries for a resource.
		/// </summary>
		/// <param name="resource">The controlled resource.</param>
		/// <returns><c>true</c> if the entries are deleted, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="resource"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="resource"/> is empty.</exception>
		bool DeleteEntriesForResource(string resource);

		/// <summary>
		/// Deletes all the ACL entries for a subject.
		/// </summary>
		/// <param name="subject">The subject.</param>
		/// <returns><c>true</c> if the entries are deleted, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="subject"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="subject"/> is empty.</exception>
		bool DeleteEntriesForSubject(string subject);

		/// <summary>
		/// Renames a resource.
		/// </summary>
		/// <param name="resource">The resource.</param>
		/// <param name="newName">The new name of the resource.</param>
		/// <returns><c>true</c> if the resource is renamed, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="resource"/> or <paramref name="newName"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="resource"/> or <paramref name="newName"/> are empty.</exception>
		bool RenameResource(string resource, string newName);

		/// <summary>
		/// Retrieves all the ACL entries for a resource.
		/// </summary>
		/// <returns>The entries.</returns>
		AclEntry[] RetrieveAllEntries();

		/// <summary>
		/// Retrieves all the ACL entries for a resource.
		/// </summary>
		/// <param name="resource">The resource.</param>
		/// <returns>The entries.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="resource"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="resource"/> is empty.</exception>
		AclEntry[] RetrieveEntriesForResource(string resource);

		/// <summary>
		/// Retrieves all the ACL entries for a subject.
		/// </summary>
		/// <param name="subject">The subject.</param>
		/// <returns>The entries.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="subject"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="subject"/> is empty.</exception>
		AclEntry[] RetrieveEntriesForSubject(string subject);

		/// <summary>
		/// Initializes the manager data.
		/// </summary>
		/// <param name="entries">The ACL entries.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="entries"/> is <c>null</c>.</exception>
		void InitializeData(AclEntry[] entries);

		/// <summary>
		/// Gets the total number of ACL entries.
		/// </summary>
		int TotalEntries { get; }

		/// <summary>
		/// Event fired when an ACL entry is stored or deleted.
		/// </summary>
		event EventHandler<AclChangedEventArgs> AclChanged;

	}

}
