
using System;
using System.Collections.Generic;
using System.Text;
using ScrewTurn.Wiki.PluginFramework;
using ScrewTurn.Wiki.AclEngine;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Utility class for writing permissions and authorizations.
	/// </summary>
	public static class AuthWriter {

		private const string Delete = "DELETE";
		private const string Set = "SET-";

		private const string MessageDeleteSuccess = "Deleted ACL Entry: ";
		private const string MessageDeleteFailure = "Deletion failed for ACL Entry: ";
		private const string MessageSetSuccess = "Set ACL Entry: ";
		private const string MessageSetFailure = "Setting failed for ACL Entry: ";

		/// <summary>
		/// Gets the settings storage provider.
		/// </summary>
		private static ISettingsStorageProviderV30 SettingsProvider {
			get { return Collectors.SettingsProvider; }
		}

		/// <summary>
		/// Sets a permission for a global resource.
		/// </summary>
		/// <param name="status">The authorization status.</param>
		/// <param name="action">The action of which to modify the authorization status.</param>
		/// <param name="group">The group subject of the authorization change.</param>
		/// <returns><c>true</c> if the authorization status is changed, <c>false</c> otherwise.</returns>
		public static bool SetPermissionForGlobals(AuthStatus status, string action, UserGroup group) {
			if(group == null) throw new ArgumentNullException("group");

			return SetPermissionForGlobals(status, action, AuthTools.PrepareGroup(group.Name));
		}

		/// <summary>
		/// Sets a permission for a global resource.
		/// </summary>
		/// <param name="status">The authorization status.</param>
		/// <param name="action">The action of which to modify the authorization status.</param>
		/// <param name="user">The user subject of the authorization change.</param>
		/// <returns><c>true</c> if the authorization status is changed, <c>false</c> otherwise.</returns>
		public static bool SetPermissionForGlobals(AuthStatus status, string action, UserInfo user) {
			if(user == null) throw new ArgumentNullException("user");

			return SetPermissionForGlobals(status, action, AuthTools.PrepareUsername(user.Username));
		}

		/// <summary>
		/// Sets a permission for a global resource.
		/// </summary>
		/// <param name="status">The authorization status.</param>
		/// <param name="action">The action of which to modify the authorization status.</param>
		/// <param name="subject">The subject of the authorization change.</param>
		/// <returns><c>true</c> if the authorization status is changed, <c>false</c> otherwise.</returns>
		private static bool SetPermissionForGlobals(AuthStatus status, string action, string subject) {
			if(action == null) throw new ArgumentNullException("action");
			if(action.Length == 0) throw new ArgumentException("Action cannot be empty", "action");
			if(action != Actions.FullControl && !AuthTools.IsValidAction(action, Actions.ForGlobals.All)) {
				throw new ArgumentException("Invalid action", "action");
			}

			if(status == AuthStatus.Delete) {
				bool done = SettingsProvider.AclManager.DeleteEntry(Actions.ForGlobals.ResourceMasterPrefix, action, subject);

				if(done) {
					Log.LogEntry(MessageDeleteSuccess + GetLogMessage(Actions.ForGlobals.ResourceMasterPrefix, "",
					action, subject, Delete), EntryType.General, Log.SystemUsername);
				}
				else {
					Log.LogEntry(MessageDeleteFailure + GetLogMessage(Actions.ForGlobals.ResourceMasterPrefix, "",
					action, subject, Delete), EntryType.Error, Log.SystemUsername);
				}

				return done;
			}
			else {
				bool done = SettingsProvider.AclManager.StoreEntry(Actions.ForGlobals.ResourceMasterPrefix,
					action, subject, status == AuthStatus.Grant ? Value.Grant : Value.Deny);

				if(done) {
					Log.LogEntry(MessageSetSuccess + GetLogMessage(Actions.ForGlobals.ResourceMasterPrefix, "",
						action, subject, Set + status.ToString()), EntryType.General, Log.SystemUsername);
				}
				else {
					Log.LogEntry(MessageSetFailure + GetLogMessage(Actions.ForGlobals.ResourceMasterPrefix, "",
					action, subject, Set + status.ToString()), EntryType.Error, Log.SystemUsername);
				}

				return done;
			}
		}

		/// <summary>
		/// Sets a permission for a namespace.
		/// </summary>
		/// <param name="status">The authorization status.</param>
		/// <param name="nspace">The namespace (<c>null</c> for the root).</param>
		/// <param name="action">The action of which to modify the authorization status.</param>
		/// <param name="group">The group subject of the authorization change.</param>
		/// <returns><c>true</c> if the authorization status is changed, <c>false</c> otherwise.</returns>
		public static bool SetPermissionForNamespace(AuthStatus status, NamespaceInfo nspace, string action, UserGroup group) {
			if(group == null) throw new ArgumentNullException("group");

			return SetPermissionForNamespace(status, nspace, action, AuthTools.PrepareGroup(group.Name));
		}

		/// <summary>
		/// Sets a permission for a namespace.
		/// </summary>
		/// <param name="status">The authorization status.</param>
		/// <param name="nspace">The namespace (<c>null</c> for the root).</param>
		/// <param name="action">The action of which to modify the authorization status.</param>
		/// <param name="user">The user subject of the authorization change.</param>
		/// <returns><c>true</c> if the authorization status is changed, <c>false</c> otherwise.</returns>
		public static bool SetPermissionForNamespace(AuthStatus status, NamespaceInfo nspace, string action, UserInfo user) {
			if(user == null) throw new ArgumentNullException("user");

			return SetPermissionForNamespace(status, nspace, action, AuthTools.PrepareUsername(user.Username));
		}

		/// <summary>
		/// Sets a permission for a namespace.
		/// </summary>
		/// <param name="status">The authorization status.</param>
		/// <param name="nspace">The namespace (<c>null</c> for the root).</param>
		/// <param name="action">The action of which to modify the authorization status.</param>
		/// <param name="subject">The subject of the authorization change.</param>
		/// <returns><c>true</c> if the authorization status is changed, <c>false</c> otherwise.</returns>
		private static bool SetPermissionForNamespace(AuthStatus status, NamespaceInfo nspace, string action, string subject) {
			if(action == null) throw new ArgumentNullException("action");
			if(action.Length == 0) throw new ArgumentException("Action cannot be empty", "action");
			if(action != Actions.FullControl && !AuthTools.IsValidAction(action, Actions.ForNamespaces.All)) {
				throw new ArgumentException("Invalid action", "action");
			}

			string namespaceName = nspace != null ? nspace.Name : "";

			if(status == AuthStatus.Delete) {
				bool done = SettingsProvider.AclManager.DeleteEntry(Actions.ForNamespaces.ResourceMasterPrefix + namespaceName,
					action, subject);

				if(done) {
					Log.LogEntry(MessageDeleteSuccess + GetLogMessage(Actions.ForNamespaces.ResourceMasterPrefix, namespaceName,
						action, subject, Delete), EntryType.General, Log.SystemUsername);
				}
				else {
					Log.LogEntry(MessageDeleteFailure + GetLogMessage(Actions.ForNamespaces.ResourceMasterPrefix, namespaceName,
						action, subject, Delete), EntryType.Error, Log.SystemUsername);
				}

				return done;
			}
			else {
				bool done = SettingsProvider.AclManager.StoreEntry(Actions.ForNamespaces.ResourceMasterPrefix + namespaceName,
					action, subject, status == AuthStatus.Grant ? Value.Grant : Value.Deny);

				if(done) {
					Log.LogEntry(MessageSetSuccess + GetLogMessage(Actions.ForNamespaces.ResourceMasterPrefix, namespaceName,
						action, subject, Set + status.ToString()), EntryType.General, Log.SystemUsername);
				}
				else {
					Log.LogEntry(MessageSetFailure + GetLogMessage(Actions.ForNamespaces.ResourceMasterPrefix, namespaceName,
						action, subject, Set + status.ToString()), EntryType.Error, Log.SystemUsername);
				}

				return done;
			}
		}

		/// <summary>
		/// Sets a permission for a directory.
		/// </summary>
		/// <param name="status">The authorization status.</param>
		/// <param name="provider">The provider that handles the directory.</param>
		/// <param name="directory">The directory.</param>
		/// <param name="action">The action of which to modify the authorization status.</param>
		/// <param name="group">The group subject of the authorization change.</param>
		/// <returns><c>true</c> if the authorization status is changed, <c>false</c> otherwise.</returns>
		public static bool SetPermissionForDirectory(AuthStatus status, IFilesStorageProviderV30 provider, string directory, string action, UserGroup group) {
			if(group == null) throw new ArgumentNullException("group");

			return SetPermissionForDirectory(status, provider, directory, action, AuthTools.PrepareGroup(group.Name));
		}

		/// <summary>
		/// Sets a permission for a directory.
		/// </summary>
		/// <param name="status">The authorization status.</param>
		/// <param name="provider">The provider that handles the directory.</param>
		/// <param name="directory">The directory.</param>
		/// <param name="action">The action of which to modify the authorization status.</param>
		/// <param name="user">The user subject of the authorization change.</param>
		/// <returns><c>true</c> if the authorization status is changed, <c>false</c> otherwise.</returns>
		public static bool SetPermissionForDirectory(AuthStatus status, IFilesStorageProviderV30 provider, string directory, string action, UserInfo user) {
			if(user == null) throw new ArgumentNullException("user");

			return SetPermissionForDirectory(status, provider, directory, action, AuthTools.PrepareUsername(user.Username));
		}

		/// <summary>
		/// Sets a permission for a directory.
		/// </summary>
		/// <param name="status">The authorization status.</param>
		/// <param name="provider">The provider that handles the directory.</param>
		/// <param name="directory">The directory.</param>
		/// <param name="action">The action of which to modify the authorization status.</param>
		/// <param name="subject">The subject of the authorization change.</param>
		/// <returns><c>true</c> if the authorization status is changed, <c>false</c> otherwise.</returns>
		private static bool SetPermissionForDirectory(AuthStatus status, IFilesStorageProviderV30 provider, string directory, string action, string subject) {
			if(provider == null) throw new ArgumentNullException("provider");
			if(directory == null) throw new ArgumentNullException("directory");
			if(directory.Length == 0) throw new ArgumentException("Directory cannot be empty", "directory");
			if(action == null) throw new ArgumentNullException("action");
			if(action.Length == 0) throw new ArgumentException("Action cannot be empty", "action");
			if(action != Actions.FullControl && !AuthTools.IsValidAction(action, Actions.ForDirectories.All)) {
				throw new ArgumentException("Invalid action", "action");
			}

			string directoryName = AuthTools.GetDirectoryName(provider, directory);

			if(status == AuthStatus.Delete) {
				bool done = SettingsProvider.AclManager.DeleteEntry(Actions.ForDirectories.ResourceMasterPrefix + directoryName,
					action, subject);

				if(done) {
					Log.LogEntry(MessageDeleteSuccess + GetLogMessage(Actions.ForDirectories.ResourceMasterPrefix, directoryName,
						action, subject, Delete), EntryType.General, Log.SystemUsername);
				}
				else {
					Log.LogEntry(MessageDeleteFailure + GetLogMessage(Actions.ForDirectories.ResourceMasterPrefix, directoryName,
						action, subject, Delete), EntryType.Error, Log.SystemUsername);
				}

				return done;
			}
			else {
				bool done = SettingsProvider.AclManager.StoreEntry(Actions.ForDirectories.ResourceMasterPrefix + directoryName,
					action, subject, status == AuthStatus.Grant ? Value.Grant : Value.Deny);

				if(done) {
					Log.LogEntry(MessageSetSuccess + GetLogMessage(Actions.ForDirectories.ResourceMasterPrefix, directoryName,
						action, subject, Set + status.ToString()), EntryType.General, Log.SystemUsername);
				}
				else {
					Log.LogEntry(MessageSetFailure + GetLogMessage(Actions.ForDirectories.ResourceMasterPrefix, directoryName,
						action, subject, Set + status.ToString()), EntryType.Error, Log.SystemUsername);
				}

				return done;
			}
		}

		/// <summary>
		/// Sets a permission for a page.
		/// </summary>
		/// <param name="status">The authorization status.</param>
		/// <param name="page">The page.</param>
		/// <param name="action">The action of which to modify the authorization status.</param>
		/// <param name="group">The group subject of the authorization change.</param>
		/// <returns><c>true</c> if the authorization status is changed, <c>false</c> otherwise.</returns>
		public static bool SetPermissionForPage(AuthStatus status, PageInfo page, string action, UserGroup group) {
			if(group == null) throw new ArgumentNullException("group");

			return SetPermissionForPage(status, page, action, AuthTools.PrepareGroup(group.Name));
		}

		/// <summary>
		/// Sets a permission for a page.
		/// </summary>
		/// <param name="status">The authorization status.</param>
		/// <param name="page">The page.</param>
		/// <param name="action">The action of which to modify the authorization status.</param>
		/// <param name="user">The user subject of the authorization change.</param>
		/// <returns><c>true</c> if the authorization status is changed, <c>false</c> otherwise.</returns>
		public static bool SetPermissionForPage(AuthStatus status, PageInfo page, string action, UserInfo user) {
			if(user == null) throw new ArgumentNullException("user");

			return SetPermissionForPage(status, page, action, AuthTools.PrepareUsername(user.Username));
		}

		/// <summary>
		/// Sets a permission for a page.
		/// </summary>
		/// <param name="status">The authorization status.</param>
		/// <param name="page">The page.</param>
		/// <param name="action">The action of which to modify the authorization status.</param>
		/// <param name="subject">The subject of the authorization change.</param>
		/// <returns><c>true</c> if the authorization status is changed, <c>false</c> otherwise.</returns>
		private static bool SetPermissionForPage(AuthStatus status, PageInfo page, string action, string subject) {
			if(page == null) throw new ArgumentNullException("page");
			if(action == null) throw new ArgumentNullException("action");
			if(action.Length == 0) throw new ArgumentException("Action cannot be empty", "action");
			if(action != Actions.FullControl && !AuthTools.IsValidAction(action, Actions.ForPages.All)) {
				throw new ArgumentException("Invalid action", "action");
			}

			if(status == AuthStatus.Delete) {
				bool done = SettingsProvider.AclManager.DeleteEntry(Actions.ForPages.ResourceMasterPrefix + page.FullName,
					action, subject);

				if(done) {
					Log.LogEntry(MessageDeleteSuccess + GetLogMessage(Actions.ForPages.ResourceMasterPrefix, page.FullName,
						action, subject, Delete), EntryType.General, Log.SystemUsername);
				}
				else {
					Log.LogEntry(MessageDeleteFailure + GetLogMessage(Actions.ForPages.ResourceMasterPrefix, page.FullName,
						action, subject, Delete), EntryType.Error, Log.SystemUsername);
				}

				return done;
			}
			else {
				bool done = SettingsProvider.AclManager.StoreEntry(Actions.ForPages.ResourceMasterPrefix + page.FullName,
					action, subject, status == AuthStatus.Grant ? Value.Grant : Value.Deny);

				if(done) {
					Log.LogEntry(MessageSetSuccess + GetLogMessage(Actions.ForPages.ResourceMasterPrefix, page.FullName,
						action, subject, Set + status.ToString()), EntryType.General, Log.SystemUsername);
				}
				else {
					Log.LogEntry(MessageSetFailure + GetLogMessage(Actions.ForPages.ResourceMasterPrefix, page.FullName,
						action, subject, Set + status.ToString()), EntryType.Error, Log.SystemUsername);
				}

				return done;
			}
		}

		/// <summary>
		/// Removes all the ACL Entries for global resources that are bound to a user group.
		/// </summary>
		/// <param name="group">The user group.</param>
		/// <returns><c>true</c> if the operation succeeded, <c>false</c> otherwise.</returns>
		public static bool RemoveEntriesForGlobals(UserGroup group) {
			if(group == null) throw new ArgumentNullException("group");

			return RemoveEntriesForGlobals(AuthTools.PrepareGroup(group.Name));
		}

		/// <summary>
		/// Removes all the ACL Entries for global resources that are bound to a user.
		/// </summary>
		/// <param name="user">The user.</param>
		/// <returns><c>true</c> if the operation succeeded, <c>false</c> otherwise.</returns>
		public static bool RemoveEntriesForGlobals(UserInfo user) {
			if(user == null) throw new ArgumentNullException("user");

			return RemoveEntriesForGlobals(AuthTools.PrepareUsername(user.Username));
		}

		/// <summary>
		/// Removes all the ACL Entries for global resources that are bound to a subject.
		/// </summary>
		/// <param name="subject">The subject.</param>
		/// <returns><c>true</c> if the operation succeeded, <c>false</c> otherwise.</returns>
		private static bool RemoveEntriesForGlobals(string subject) {
			AclEntry[] entries = SettingsProvider.AclManager.RetrieveEntriesForSubject(subject);

			foreach(AclEntry entry in entries) {
				if(entry.Resource == Actions.ForGlobals.ResourceMasterPrefix) {
					// This call automatically logs the operation result
					bool done = SetPermissionForGlobals(AuthStatus.Delete, entry.Action, subject);
					if(!done) return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Removes all the ACL Entries for a namespace that are bound to a user group.
		/// </summary>
		/// <param name="group">The user group.</param>
		/// <param name="nspace">The namespace (<c>null</c> for the root).</param>
		/// <returns><c>true</c> if the operation succeeded, <c>false</c> otherwise.</returns>
		public static bool RemoveEntriesForNamespace(UserGroup group, NamespaceInfo nspace) {
			if(group == null) throw new ArgumentNullException("group");

			return RemoveEntriesForNamespace(AuthTools.PrepareGroup(group.Name), nspace);
		}

		/// <summary>
		/// Removes all the ACL Entries for a namespace that are bound to a user.
		/// </summary>
		/// <param name="user">The user.</param>
		/// <param name="nspace">The namespace (<c>null</c> for the root).</param>
		/// <returns><c>true</c> if the operation succeeded, <c>false</c> otherwise.</returns>
		public static bool RemoveEntriesForNamespace(UserInfo user, NamespaceInfo nspace) {
			if(user == null) throw new ArgumentNullException("user");

			return RemoveEntriesForNamespace(AuthTools.PrepareUsername(user.Username), nspace);
		}

		/// <summary>
		/// Removes all the ACL Entries for a namespace that are bound to a subject.
		/// </summary>
		/// <param name="subject">The subject.</param>
		/// <param name="nspace">The namespace (<c>null</c> for the root).</param>
		/// <returns><c>true</c> if the operation succeeded, <c>false</c> otherwise.</returns>
		private static bool RemoveEntriesForNamespace(string subject, NamespaceInfo nspace) {
			string resourceName = Actions.ForNamespaces.ResourceMasterPrefix;
			if(nspace != null) resourceName += nspace.Name;

			AclEntry[] entries = SettingsProvider.AclManager.RetrieveEntriesForSubject(subject);

			foreach(AclEntry entry in entries) {
				if(entry.Resource == resourceName) {
					// This call automatically logs the operation result
					bool done = SetPermissionForNamespace(AuthStatus.Delete, nspace, entry.Action, subject);
					if(!done) return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Removes all the ACL Entries for a page that are bound to a user group.
		/// </summary>
		/// <param name="group">The user group.</param>
		/// <param name="page">The page.</param>
		/// <returns><c>true</c> if the operation succeeded, <c>false</c> otherwise.</returns>
		public static bool RemoveEntriesForPage(UserGroup group, PageInfo page) {
			if(group == null) throw new ArgumentNullException("group");

			return RemoveEntriesForPage(AuthTools.PrepareGroup(group.Name), page);
		}

		/// <summary>
		/// Removes all the ACL Entries for a page that are bound to a user.
		/// </summary>
		/// <param name="user">The user.</param>
		/// <param name="page">The page.</param>
		/// <returns><c>true</c> if the operation succeeded, <c>false</c> otherwise.</returns>
		public static bool RemoveEntriesForPage(UserInfo user, PageInfo page) {
			if(user == null) throw new ArgumentNullException("user");

			return RemoveEntriesForPage(AuthTools.PrepareUsername(user.Username), page);
		}

		/// <summary>
		/// Removes all the ACL Entries for a page that are bound to a subject.
		/// </summary>
		/// <param name="subject">The subject.</param>
		/// <param name="page">The page.</param>
		/// <returns><c>true</c> if the operation succeeded, <c>false</c> otherwise.</returns>
		private static bool RemoveEntriesForPage(string subject, PageInfo page) {
			if(page == null) throw new ArgumentNullException("page");

			string resourceName = Actions.ForPages.ResourceMasterPrefix + page.FullName;

			AclEntry[] entries = SettingsProvider.AclManager.RetrieveEntriesForSubject(subject);

			foreach(AclEntry entry in entries) {
				if(entry.Resource == resourceName) {
					// This call automatically logs the operation result
					bool done = SetPermissionForPage(AuthStatus.Delete, page, entry.Action, subject);
					if(!done) return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Removes all the ACL Entries for a directory that are bound to a user group.
		/// </summary>
		/// <param name="group">The group.</param>
		/// <param name="provider">The provider.</param>
		/// <param name="directory">The directory.</param>
		/// <returns><c>true</c> if the operation succeeded, <c>false</c> otherwise.</returns>
		public static bool RemoveEntriesForDirectory(UserGroup group, IFilesStorageProviderV30 provider, string directory) {
			if(group == null) throw new ArgumentNullException("group");

			return RemoveEntriesForDirectory(AuthTools.PrepareGroup(group.Name), provider, directory);
		}

		/// <summary>
		/// Removes all the ACL Entries for a directory that are bound to a user.
		/// </summary>
		/// <param name="user">The user.</param>
		/// <param name="provider">The provider.</param>
		/// <param name="directory">The directory.</param>
		/// <returns><c>true</c> if the operation succeeded, <c>false</c> otherwise.</returns>
		public static bool RemoveEntriesForDirectory(UserInfo user, IFilesStorageProviderV30 provider, string directory) {
			if(user == null) throw new ArgumentNullException("user");

			return RemoveEntriesForDirectory(AuthTools.PrepareUsername(user.Username), provider, directory);
		}

		/// <summary>
		/// Removes all the ACL Entries for a directory that are bound to a subject.
		/// </summary>
		/// <param name="subject">The subject.</param>
		/// <param name="provider">The provider.</param>
		/// <param name="directory">The directory.</param>
		/// <returns><c>true</c> if the operation succeeded, <c>false</c> otherwise.</returns>
		private static bool RemoveEntriesForDirectory(string subject, IFilesStorageProviderV30 provider, string directory) {
			if(provider == null) throw new ArgumentNullException("provider");
			if(directory == null) throw new ArgumentNullException("directory");
			if(directory.Length == 0) throw new ArgumentException("Directory cannot be empty", "directory");

			string resourceName = Actions.ForDirectories.ResourceMasterPrefix + AuthTools.GetDirectoryName(provider, directory);

			AclEntry[] entries = SettingsProvider.AclManager.RetrieveEntriesForSubject(subject);

			foreach(AclEntry entry in entries) {
				if(entry.Resource == resourceName) {
					// This call automatically logs the operation result
					bool done = SetPermissionForDirectory(AuthStatus.Delete, provider, directory, entry.Action, subject);
					if(!done) return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Clears all the ACL entries for a directory.
		/// </summary>
		/// <param name="provider">The provider.</param>
		/// <param name="directory">The directory.</param>
		public static void ClearEntriesForDirectory(IFilesStorageProviderV30 provider, string directory) {
			if(provider == null) throw new ArgumentNullException("provider");

			if(directory == null) throw new ArgumentNullException("directory");
			if(directory.Length == 0) throw new ArgumentException("Directory cannot be empty", "directory");

			string resourceName = Actions.ForDirectories.ResourceMasterPrefix + AuthTools.GetDirectoryName(provider, directory);

			SettingsProvider.AclManager.DeleteEntriesForResource(resourceName);
		}

		/// <summary>
		/// Clears all the ACL entries for a namespace.
		/// </summary>
		/// <param name="nspace">The namespace.</param>
		/// <param name="pages">The local names of the pages in the namespace.</param>
		public static void ClearEntriesForNamespace(string nspace, List<string> pages) {
			if(nspace == null) throw new ArgumentNullException("nspac");
			if(nspace.Length == 0) throw new ArgumentException("Namespace cannot be empty", "nspace");

			if(pages == null) throw new ArgumentNullException("pages");

			foreach(string p in pages) {
				if(p == null) throw new ArgumentNullException("pages");
				if(p.Length == 0) throw new ArgumentException("Page Element cannot be empty", "pages");
			}

			string resourceName;

			foreach(string p in pages) {
				resourceName = Actions.ForPages.ResourceMasterPrefix + NameTools.GetFullName(nspace, p);
				SettingsProvider.AclManager.DeleteEntriesForResource(resourceName);
			}

			resourceName = Actions.ForNamespaces.ResourceMasterPrefix + nspace;
			SettingsProvider.AclManager.DeleteEntriesForResource(resourceName);
		}

		/// <summary>
		/// Clears all the ACL entries for a page.
		/// </summary>
		/// <param name="page">The page full name.</param>
		public static void ClearEntriesForPage(string page) {
			if(page == null) throw new ArgumentNullException("page");
			if(page.Length == 0) throw new ArgumentException("Page cannot be empty", "page");

			string resourceName = Actions.ForPages.ResourceMasterPrefix + page;

			SettingsProvider.AclManager.DeleteEntriesForResource(resourceName);
		}

		/// <summary>
		/// Processes the renaming of a directory.
		/// </summary>
		/// <param name="provider">The provider.</param>
		/// <param name="oldName">The old directory name (full path).</param>
		/// <param name="newName">The new directory name (full path).</param>
		/// <returns><c>true</c> if the operation completed successfully, <c>false</c> otherwise.</returns>
		/// <remarks>The method <b>does not</b> recurse in sub-directories.</remarks>
		public static bool ProcessDirectoryRenaming(IFilesStorageProviderV30 provider, string oldName, string newName) {
			if(provider == null) throw new ArgumentNullException("provider");

			if(oldName == null) throw new ArgumentNullException("oldName");
			if(oldName.Length == 0) throw new ArgumentException("Old Name cannot be empty", "oldName");

			if(newName == null) throw new ArgumentNullException("newName");
			if(newName.Length == 0) throw new ArgumentException("New Name cannot be empty", "newName");

			return SettingsProvider.AclManager.RenameResource(
				Actions.ForDirectories.ResourceMasterPrefix + AuthTools.GetDirectoryName(provider, oldName),
				Actions.ForDirectories.ResourceMasterPrefix + AuthTools.GetDirectoryName(provider, newName));
		}

		/// <summary>
		/// Processes the renaming of a namespace.
		/// </summary>
		/// <param name="oldName">The old name of the namespace.</param>
		/// <param name="oldPages">The list of local names of the pages in the renamed namespace.</param>
		/// <param name="newName">The new name of the namespace.</param>
		/// <returns><c>true</c> if the operation completed successfully, <c>false</c> otherwise.</returns>
		public static bool ProcessNamespaceRenaming(string oldName, List<string> oldPages, string newName) {
			if(oldName == null) throw new ArgumentNullException("oldName");
			if(oldName.Length == 0) throw new ArgumentException("Old Name cannot be empty", "oldName");

			if(oldPages == null) throw new ArgumentNullException("oldPages");
			foreach(string p in oldPages) {
				if(p == null) throw new ArgumentNullException("oldPages");
				if(p.Length == 0) throw new ArgumentException("Page cannot be empty", "oldPages");
			}

			if(newName == null) throw new ArgumentNullException("newName");
			if(newName.Length == 0) throw new ArgumentException("New Name cannot be empty", "newName");

			foreach(string p in oldPages) {
				SettingsProvider.AclManager.RenameResource(
					Actions.ForPages.ResourceMasterPrefix + NameTools.GetFullName(oldName, p),
					Actions.ForPages.ResourceMasterPrefix + NameTools.GetFullName(newName, p));
			}

			return SettingsProvider.AclManager.RenameResource(
				Actions.ForNamespaces.ResourceMasterPrefix + oldName,
				Actions.ForNamespaces.ResourceMasterPrefix + newName);
		}

		/// <summary>
		/// Processes the renaming of a page.
		/// </summary>
		/// <param name="oldName">The old full page name.</param>
		/// <param name="newName">The new full page name.</param>
		/// <returns><c>true</c> if the operation completed successfully, <c>false</c> otherwise.</returns>
		public static bool ProcessPageRenaming(string oldName, string newName) {
			if(oldName == null) throw new ArgumentNullException("oldName");
			if(oldName.Length == 0) throw new ArgumentException("Old Name cannot be empty", "oldName");

			if(newName == null) throw new ArgumentNullException("newName");
			if(newName.Length == 0) throw new ArgumentException("New Name cannot be empty", "newName");

			return SettingsProvider.AclManager.RenameResource(
				Actions.ForPages.ResourceMasterPrefix + oldName,
				Actions.ForPages.ResourceMasterPrefix + newName);
		}

		/// <summary>
		/// Gets the log message for an ACL entry change.
		/// </summary>
		/// <param name="resourcePrefix">The resource prefix.</param>
		/// <param name="resource">The resource name.</param>
		/// <param name="action">The action.</param>
		/// <param name="subject">The subject.</param>
		/// <param name="status">The status.</param>
		/// <returns>The message.</returns>
		private static string GetLogMessage(string resourcePrefix, string resource, string action, string subject, string status) {
			return resourcePrefix + resource + ":" + action + ":" + subject + "->" + status;
		}

	}

}
