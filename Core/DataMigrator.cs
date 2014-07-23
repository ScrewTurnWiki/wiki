
using System;
using System.Collections.Generic;
using System.IO;
using ScrewTurn.Wiki.AclEngine;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Provides methods for migrating data from a Provider to another.
	/// </summary>
	public static class DataMigrator {

		/// <summary>
		/// Migrates <b>all</b> the data from a Pages Provider to another one.
		/// </summary>
		/// <param name="source">The source Provider.</param>
		/// <param name="destination">The destination Provider.</param>
		public static void MigratePagesStorageProviderData(IPagesStorageProviderV30 source, IPagesStorageProviderV30 destination) {
			// Move Snippets
			Snippet[] snippets = source.GetSnippets();
			for(int i = 0; i < snippets.Length; i++) {
				destination.AddSnippet(snippets[i].Name, snippets[i].Content);
				source.RemoveSnippet(snippets[i].Name);
			}

			// Move Content Templates
			ContentTemplate[] templates = source.GetContentTemplates();
			for(int i = 0; i < templates.Length; i++) {
				destination.AddContentTemplate(templates[i].Name, templates[i].Content);
				source.RemoveContentTemplate(templates[i].Name);
			}

			// Create namespaces
			NamespaceInfo[] namespaces = source.GetNamespaces();
			NamespaceInfo[] createdNamespaces = new NamespaceInfo[namespaces.Length];
			for(int i = 0; i < namespaces.Length; i++) {
				createdNamespaces[i] = destination.AddNamespace(namespaces[i].Name);
			}

			List<NamespaceInfo> sourceNamespaces = new List<NamespaceInfo>();
			sourceNamespaces.Add(null);
			sourceNamespaces.AddRange(namespaces);

			int currentNamespaceIndex = 0;
			foreach(NamespaceInfo currentNamespace in sourceNamespaces) {

				// Load all nav paths now to avoid problems with missing pages from source provider
				// after the pages have been moved already
				NavigationPath[] sourceNavPaths = source.GetNavigationPaths(currentNamespace);

				// Copy categories (removed from source later)
				CategoryInfo[] sourceCats = source.GetCategories(currentNamespace);
				for(int i = 0; i < sourceCats.Length; i++) {
					destination.AddCategory(NameTools.GetNamespace(sourceCats[i].FullName), NameTools.GetLocalName(sourceCats[i].FullName));
				}

				// Move Pages
				PageInfo[] pages = source.GetPages(currentNamespace);
				for(int i = 0; i < pages.Length; i++) {

					// Create Page
					PageInfo newPage = destination.AddPage(NameTools.GetNamespace(pages[i].FullName),
						NameTools.GetLocalName(pages[i].FullName), pages[i].CreationDateTime);
					if(newPage == null) {
						Log.LogEntry("Unable to move Page " + pages[i].FullName + " - Skipping", EntryType.Error, Log.SystemUsername);
						continue;
					}

					// Get content and store it, without backup
					PageContent c = source.GetContent(pages[i]);
					destination.ModifyPage(newPage, c.Title, c.User, c.LastModified, c.Comment, c.Content, c.Keywords, c.Description, SaveMode.Normal);

					// Move all the backups
					int[] revs = source.GetBackups(pages[i]);
					for(int k = 0; k < revs.Length; k++) {
						c = source.GetBackupContent(pages[i], revs[k]);
						destination.SetBackupContent(c, revs[k]);
					}

					// Move all messages
					Message[] messages = source.GetMessages(pages[i]);
					destination.BulkStoreMessages(newPage, messages);

					// Bind current page (find all proper categories, and use them to bind the page)
					List<string> pageCats = new List<string>();
					for(int k = 0; k < sourceCats.Length; k++) {
						for(int z = 0; z < sourceCats[k].Pages.Length; z++) {
							if(sourceCats[k].Pages[z].Equals(newPage.FullName)) {
								pageCats.Add(sourceCats[k].FullName);
								break;
							}
						}
					}
					destination.RebindPage(newPage, pageCats.ToArray());

					// Copy draft
					PageContent draft = source.GetDraft(pages[i]);
					if(draft != null) {
						destination.ModifyPage(newPage, draft.Title, draft.User, draft.LastModified,
							draft.Comment, draft.Content, draft.Keywords, draft.Description, SaveMode.Draft);
					}

					// Remove Page from source
					source.RemovePage(pages[i]); // Also deletes the Messages
				}

				// Remove Categories from source
				for(int i = 0; i < sourceCats.Length; i++) {
					source.RemoveCategory(sourceCats[i]);
				}

				// Move navigation paths
				List<PageInfo> newPages = new List<PageInfo>(destination.GetPages(currentNamespace == null ? null : createdNamespaces[currentNamespaceIndex]));
				for(int i = 0; i < sourceNavPaths.Length; i++) {
					PageInfo[] tmp = new PageInfo[sourceNavPaths[i].Pages.Length];
					for(int k = 0; k < tmp.Length; k++) {
						tmp[k] = newPages.Find(delegate(PageInfo p) { return p.FullName == sourceNavPaths[i].Pages[k]; });
					}
					destination.AddNavigationPath(NameTools.GetNamespace(sourceNavPaths[i].FullName),
						NameTools.GetLocalName(sourceNavPaths[i].FullName), tmp);
					source.RemoveNavigationPath(sourceNavPaths[i]);
				}

				if(currentNamespace != null) {
					// Set default page
					PageInfo defaultPage = currentNamespace.DefaultPage == null ? null :
						newPages.Find(delegate(PageInfo p) { return p.FullName == currentNamespace.DefaultPage.FullName; });
					destination.SetNamespaceDefaultPage(createdNamespaces[currentNamespaceIndex], defaultPage);

					// Remove namespace from source
					source.RemoveNamespace(currentNamespace);

					currentNamespaceIndex++;
				}
			}
		}

		/// <summary>
		/// Migrates all the User Accounts from a Provider to another.
		/// </summary>
		/// <param name="source">The source Provider.</param>
		/// <param name="destination">The destination Provider.</param>
		/// <param name="sendEmailNotification">A value indicating whether or not to send a notification email message to the moved users.</param>
		public static void MigrateUsersStorageProviderData(IUsersStorageProviderV30 source, IUsersStorageProviderV30 destination, bool sendEmailNotification) {
			// User groups
			UserGroup[] groups = source.GetUserGroups();
			foreach(UserGroup group in groups) {
				destination.AddUserGroup(group.Name, group.Description);
			}

			// Users
			UserInfo[] users = source.GetUsers();
			MovedUser[] movedUsers = new MovedUser[users.Length];
			
			for(int i = 0; i < users.Length; i++) {
				// Generate new Password, create MovedUser object, add new User, delete old User
				string password = Tools.GenerateRandomPassword();
				movedUsers[i] = new MovedUser(users[i].Username, users[i].Email, users[i].DateTime);
				UserInfo newUser = destination.AddUser(users[i].Username, users[i].DisplayName, password, users[i].Email, users[i].Active, users[i].DateTime);
				
				// Membership
				destination.SetUserMembership(newUser, users[i].Groups);

				// User data
				IDictionary<string, string> uData = source.RetrieveAllUserData(users[i]);
				foreach(KeyValuePair<string, string> pair in uData) {
					destination.StoreUserData(newUser, pair.Key, pair.Value);
				}

				source.RemoveUser(users[i]);
			}

			// Remove old groups
			foreach(UserGroup group in groups) {
				source.RemoveUserGroup(group);
			}

			if(sendEmailNotification) {
				// Send Emails
				for(int i = 0; i < movedUsers.Length; i++) {
					Users.SendPasswordResetMessage(movedUsers[i].Username, movedUsers[i].Email, movedUsers[i].DateTime);
				}
			}
		}

		/// <summary>
		/// Migrates all the stored files and page attachments from a Provider to another.
		/// </summary>
		/// <param name="source">The source Provider.</param>
		/// <param name="destination">The destination Provider.</param>
		/// <param name="settingsProvider">The settings storage provider that handles permissions.</param>
		public static void MigrateFilesStorageProviderData(IFilesStorageProviderV30 source, IFilesStorageProviderV30 destination, ISettingsStorageProviderV30 settingsProvider) {
			// Directories
			MigrateDirectories(source, destination, "/", settingsProvider);

			// Attachments
			foreach(string page in source.GetPagesWithAttachments()) {
				PageInfo pageInfo = new PageInfo(page, null, DateTime.Now);

				string[] attachments = source.ListPageAttachments(pageInfo);

				foreach(string attachment in attachments) {
					// Copy file content
					using(MemoryStream ms = new MemoryStream(1048576)) {
						source.RetrievePageAttachment(pageInfo, attachment, ms, false);
						ms.Seek(0, SeekOrigin.Begin);
						destination.StorePageAttachment(pageInfo, attachment, ms, false);
					}

					// Copy download count
					FileDetails fileDetails = source.GetPageAttachmentDetails(pageInfo, attachment);
					destination.SetPageAttachmentRetrievalCount(pageInfo, attachment, fileDetails.RetrievalCount);

					// Delete attachment
					source.DeletePageAttachment(pageInfo, attachment);
				}
			}
		}

		private static void MigrateDirectories(IFilesStorageProviderV30 source, IFilesStorageProviderV30 destination, string current, ISettingsStorageProviderV30 settingsProvider) {
			// Copy files
			string[] files = source.ListFiles(current);
			foreach(string file in files) {
				// Copy file content
				using(MemoryStream ms = new MemoryStream(1048576)) {
					source.RetrieveFile(file, ms, false);
					ms.Seek(0, SeekOrigin.Begin);
					destination.StoreFile(file, ms, false);
				}

				// Copy download count
				FileDetails fileDetails = source.GetFileDetails(file);
				destination.SetFileRetrievalCount(file, fileDetails.RetrievalCount);

				// Delete source file, if root
				if(current == "/") {
					source.DeleteFile(file);
				}
			}

			settingsProvider.AclManager.RenameResource(
				Actions.ForDirectories.ResourceMasterPrefix + AuthTools.GetDirectoryName(source, current),
				Actions.ForDirectories.ResourceMasterPrefix + AuthTools.GetDirectoryName(destination, current));

			// Copy directories
			string[] directories = source.ListDirectories(current);
			foreach(string dir in directories) {
				destination.CreateDirectory(current, dir.Substring(dir.TrimEnd('/').LastIndexOf("/") + 1).Trim('/'));
				MigrateDirectories(source, destination, dir, settingsProvider);

				// Delete directory, if root
				if(current == "/") {
					source.DeleteDirectory(dir);
				}
			}
		}

		/// <summary>
		/// Copies the settings from a Provider to another.
		/// </summary>
		/// <param name="source">The source Provider.</param>
		/// <param name="destination">The destination Provider.</param>
		/// <param name="knownNamespaces">The currently known page namespaces.</param>
		/// <param name="knownPlugins">The currently known plugins.</param>
		public static void CopySettingsStorageProviderData(ISettingsStorageProviderV30 source, ISettingsStorageProviderV30 destination,
			string[] knownNamespaces, string[] knownPlugins) {

			// Settings
			destination.BeginBulkUpdate();
			foreach(KeyValuePair<string, string> pair in source.GetAllSettings()) {
				destination.SetSetting(pair.Key, pair.Value);
			}
			destination.EndBulkUpdate();

			// Meta-data (global)
			destination.SetMetaDataItem(MetaDataItem.AccountActivationMessage, null,
				source.GetMetaDataItem(MetaDataItem.AccountActivationMessage, null));
			destination.SetMetaDataItem(MetaDataItem.PasswordResetProcedureMessage, null,
				source.GetMetaDataItem(MetaDataItem.PasswordResetProcedureMessage, null));
			destination.SetMetaDataItem(MetaDataItem.LoginNotice, null,
				source.GetMetaDataItem(MetaDataItem.LoginNotice, null));
			destination.SetMetaDataItem(MetaDataItem.PageChangeMessage, null,
				source.GetMetaDataItem(MetaDataItem.PageChangeMessage, null));
			destination.SetMetaDataItem(MetaDataItem.DiscussionChangeMessage, null,
				source.GetMetaDataItem(MetaDataItem.DiscussionChangeMessage, null));

			// Meta-data (ns-specific)
			List<string> namespacesToProcess = new List<string>();
			namespacesToProcess.Add(null);
			namespacesToProcess.AddRange(knownNamespaces);
			foreach(string nspace in namespacesToProcess) {
				destination.SetMetaDataItem(MetaDataItem.EditNotice, nspace,
					source.GetMetaDataItem(MetaDataItem.EditNotice, nspace));
				destination.SetMetaDataItem(MetaDataItem.Footer, nspace,
					source.GetMetaDataItem(MetaDataItem.Footer, nspace));
				destination.SetMetaDataItem(MetaDataItem.Header, nspace,
					source.GetMetaDataItem(MetaDataItem.Header, nspace));
				destination.SetMetaDataItem(MetaDataItem.HtmlHead, nspace,
					source.GetMetaDataItem(MetaDataItem.HtmlHead, nspace));
				destination.SetMetaDataItem(MetaDataItem.PageFooter, nspace,
					source.GetMetaDataItem(MetaDataItem.PageFooter, nspace));
				destination.SetMetaDataItem(MetaDataItem.PageHeader, nspace,
					source.GetMetaDataItem(MetaDataItem.PageHeader, nspace));
				destination.SetMetaDataItem(MetaDataItem.Sidebar, nspace,
					source.GetMetaDataItem(MetaDataItem.Sidebar, nspace));
			}

			// Plugin assemblies
			string[] assemblies = source.ListPluginAssemblies();
			foreach(string asm in assemblies) {
				destination.StorePluginAssembly(asm,
					source.RetrievePluginAssembly(asm));
			}

			// Plugin status and config
			foreach(string plug in knownPlugins) {
				destination.SetPluginStatus(plug,
					source.GetPluginStatus(plug));

				destination.SetPluginConfiguration(plug,
					source.GetPluginConfiguration(plug));
			}

			// Outgoing links
			IDictionary<string, string[]> allLinks = source.GetAllOutgoingLinks();
			foreach(KeyValuePair<string, string[]> link in allLinks) {
				destination.StoreOutgoingLinks(link.Key, link.Value);
			}

			// ACLs
			AclEntry[] allEntries = source.AclManager.RetrieveAllEntries();
			foreach(AclEntry entry in allEntries) {
				destination.AclManager.StoreEntry(entry.Resource, entry.Action, entry.Subject, entry.Value);
			}
		}

	}

	/// <summary>
	/// Contains username, email and registration date/time of a moved User account.
	/// </summary>
	public class MovedUser {

		private string username, email;
		private DateTime dateTime;

		/// <summary>
		/// Initializes a new instance of the <b>MovedUser</b> class.
		/// </summary>
		/// <param name="username">The esername.</param>
		/// <param name="email">The email address.</param>
		/// <param name="dateTime">The registration date/time.</param>
		public MovedUser(string username, string email, DateTime dateTime) {
			this.username = username;
			this.email = email;
			this.dateTime = dateTime;
		}

		/// <summary>
		/// Gets the Username.
		/// </summary>
		public string Username {
			get { return username; }
		}

		/// <summary>
		/// Gets the Email.
		/// </summary>
		public string Email {
			get { return email; }
		}

		/// <summary>
		/// Gets the registration date/time.
		/// </summary>
		public DateTime DateTime {
			get { return dateTime; }
		}

	}

}