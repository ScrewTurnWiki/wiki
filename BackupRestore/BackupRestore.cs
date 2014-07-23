
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ScrewTurn.Wiki.PluginFramework;
using System.Web.Script.Serialization;
using System.Collections;
using ScrewTurn.Wiki.AclEngine;
using Ionic.Zip;
using System.IO;

namespace ScrewTurn.Wiki.BackupRestore {

	/// <summary>
	/// Implements a Backup and Restore procedure for settings storage providers.
	/// </summary>
	public static class BackupRestore {

		private const string BACKUP_RESTORE_UTILITY_VERSION = "1.0";

		private static VersionFile generateVersionFile(string backupName) {
			return new VersionFile() {
				BackupRestoreVersion = BACKUP_RESTORE_UTILITY_VERSION,
				WikiVersion = typeof(BackupRestore).Assembly.GetName().Version.ToString(),
				BackupName = backupName
			};
		}

		/// <summary>
		/// Backups all the providers (excluded global settings storage provider).
		/// </summary>
		/// <param name="backupZipFileName">The name of the zip file where to store the backup file.</param>
		/// <param name="plugins">The available plugins.</param>
		/// <param name="settingsStorageProvider">The settings storage provider.</param>
		/// <param name="pagesStorageProviders">The pages storage providers.</param>
		/// <param name="usersStorageProviders">The users storage providers.</param>
		/// <param name="filesStorageProviders">The files storage providers.</param>
		/// <returns><c>true</c> if the backup has been succesfull.</returns>
		public static bool BackupAll(string backupZipFileName, string[] plugins, ISettingsStorageProviderV30 settingsStorageProvider, IPagesStorageProviderV30[] pagesStorageProviders, IUsersStorageProviderV30[] usersStorageProviders, IFilesStorageProviderV30[] filesStorageProviders) {
			string tempPath = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), Guid.NewGuid().ToString());
			Directory.CreateDirectory(tempPath);

			using(ZipFile backupZipFile = new ZipFile(backupZipFileName)) {

				// Find all namespaces
				List<string> namespaces = new List<string>();
				foreach(IPagesStorageProviderV30 pagesStorageProvider in pagesStorageProviders) {
					foreach(NamespaceInfo ns in pagesStorageProvider.GetNamespaces()) {
						namespaces.Add(ns.Name);
					}
				}

				// Backup settings storage provider
				string zipSettingsBackup = Path.Combine(tempPath, "SettingsBackup-" + settingsStorageProvider.GetType().FullName + ".zip");
				BackupSettingsStorageProvider(zipSettingsBackup, settingsStorageProvider, namespaces.ToArray(), plugins);
				backupZipFile.AddFile(zipSettingsBackup, ""); 

				// Backup pages storage providers
				foreach(IPagesStorageProviderV30 pagesStorageProvider in pagesStorageProviders) {
					string zipPagesBackup = Path.Combine(tempPath, "PagesBackup-" + pagesStorageProvider.GetType().FullName + ".zip");
					BackupPagesStorageProvider(zipPagesBackup, pagesStorageProvider);
					backupZipFile.AddFile(zipPagesBackup, "");
				}

				// Backup users storage providers
				foreach(IUsersStorageProviderV30 usersStorageProvider in usersStorageProviders) {
					string zipUsersProvidersBackup = Path.Combine(tempPath, "UsersBackup-" + usersStorageProvider.GetType().FullName + ".zip");
					BackupUsersStorageProvider(zipUsersProvidersBackup, usersStorageProvider);
					backupZipFile.AddFile(zipUsersProvidersBackup, "");
				}

				// Backup files storage providers
				foreach(IFilesStorageProviderV30 filesStorageProvider in filesStorageProviders) {
					string zipFilesProviderBackup = Path.Combine(tempPath, "FilesBackup-" + filesStorageProvider.GetType().FullName + ".zip");
					BackupFilesStorageProvider(zipFilesProviderBackup, filesStorageProvider, pagesStorageProviders);
					backupZipFile.AddFile(zipFilesProviderBackup, "");
				}
				backupZipFile.Save();
			}

			Directory.Delete(tempPath, true);
			return true;
		}

		/// <summary>
		/// Backups the specified settings provider.
		/// </summary>
		/// <param name="zipFileName">The zip file name where to store the backup.</param>
		/// <param name="settingsStorageProvider">The source settings provider.</param>
		/// <param name="knownNamespaces">The currently known page namespaces.</param>
		/// <param name="knownPlugins">The currently known plugins.</param>
		/// <returns><c>true</c> if the backup file has been succesfully created.</returns>
		public static bool BackupSettingsStorageProvider(string zipFileName, ISettingsStorageProviderV30 settingsStorageProvider, string[] knownNamespaces, string[] knownPlugins) {
			SettingsBackup settingsBackup = new SettingsBackup();

			// Settings
			settingsBackup.Settings = (Dictionary<string, string>)settingsStorageProvider.GetAllSettings();

			// Plugins Status and Configuration
			settingsBackup.PluginsFileNames = knownPlugins.ToList();
			Dictionary<string, bool> pluginsStatus = new Dictionary<string, bool>();
			Dictionary<string, string> pluginsConfiguration = new Dictionary<string, string>();
			foreach(string plugin in knownPlugins) {
				pluginsStatus[plugin] = settingsStorageProvider.GetPluginStatus(plugin);
				pluginsConfiguration[plugin] = settingsStorageProvider.GetPluginConfiguration(plugin);
			}
			settingsBackup.PluginsStatus = pluginsStatus;
			settingsBackup.PluginsConfiguration = pluginsConfiguration;

			// Metadata
			List<MetaData> metadataList = new List<MetaData>();
			// Meta-data (global)
			metadataList.Add(new MetaData() {
				Item = MetaDataItem.AccountActivationMessage,
				Tag = null,
				Content = settingsStorageProvider.GetMetaDataItem(MetaDataItem.AccountActivationMessage, null)
			});
			metadataList.Add(new MetaData() { Item = MetaDataItem.PasswordResetProcedureMessage, Tag = null, Content = settingsStorageProvider.GetMetaDataItem(MetaDataItem.PasswordResetProcedureMessage, null) });
			metadataList.Add(new MetaData() { Item = MetaDataItem.LoginNotice, Tag = null, Content = settingsStorageProvider.GetMetaDataItem(MetaDataItem.LoginNotice, null) });
			metadataList.Add(new MetaData() { Item = MetaDataItem.PageChangeMessage, Tag = null, Content = settingsStorageProvider.GetMetaDataItem(MetaDataItem.PageChangeMessage, null) });
			metadataList.Add(new MetaData() { Item = MetaDataItem.DiscussionChangeMessage, Tag = null, Content = settingsStorageProvider.GetMetaDataItem(MetaDataItem.DiscussionChangeMessage, null) });
			// Meta-data (ns-specific)
			List<string> namespacesToProcess = new List<string>();
			namespacesToProcess.Add("");
			namespacesToProcess.AddRange(knownNamespaces);
			foreach(string nspace in namespacesToProcess) {
				metadataList.Add(new MetaData() { Item = MetaDataItem.EditNotice, Tag = nspace, Content = settingsStorageProvider.GetMetaDataItem(MetaDataItem.EditNotice, nspace) });
				metadataList.Add(new MetaData() { Item = MetaDataItem.Footer, Tag = nspace, Content = settingsStorageProvider.GetMetaDataItem(MetaDataItem.Footer, nspace) });
				metadataList.Add(new MetaData() { Item = MetaDataItem.Header, Tag = nspace, Content = settingsStorageProvider.GetMetaDataItem(MetaDataItem.Header, nspace) });
				metadataList.Add(new MetaData() { Item = MetaDataItem.HtmlHead, Tag = nspace, Content = settingsStorageProvider.GetMetaDataItem(MetaDataItem.HtmlHead, nspace) });
				metadataList.Add(new MetaData() { Item = MetaDataItem.PageFooter, Tag = nspace, Content = settingsStorageProvider.GetMetaDataItem(MetaDataItem.PageFooter, nspace) });
				metadataList.Add(new MetaData() { Item = MetaDataItem.PageHeader, Tag = nspace, Content = settingsStorageProvider.GetMetaDataItem(MetaDataItem.PageHeader, nspace) });
				metadataList.Add(new MetaData() { Item = MetaDataItem.Sidebar, Tag = nspace, Content = settingsStorageProvider.GetMetaDataItem(MetaDataItem.Sidebar, nspace) });
			}
			settingsBackup.Metadata = metadataList;

			// RecentChanges
			settingsBackup.RecentChanges = settingsStorageProvider.GetRecentChanges().ToList();

			// OutgoingLinks
			settingsBackup.OutgoingLinks = (Dictionary<string, string[]>)settingsStorageProvider.GetAllOutgoingLinks();

			// ACLEntries
			AclEntry[] aclEntries = settingsStorageProvider.AclManager.RetrieveAllEntries();
			settingsBackup.AclEntries = new List<AclEntryBackup>(aclEntries.Length);
			foreach(AclEntry aclEntry in aclEntries) {
				settingsBackup.AclEntries.Add(new AclEntryBackup() {
					Action = aclEntry.Action,
					Resource = aclEntry.Resource,
					Subject = aclEntry.Subject,
					Value = aclEntry.Value
				});
			}

			JavaScriptSerializer javascriptSerializer = new JavaScriptSerializer();
			javascriptSerializer.MaxJsonLength = javascriptSerializer.MaxJsonLength * 10;

			string tempDir = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), Guid.NewGuid().ToString());
			Directory.CreateDirectory(tempDir);

			FileStream tempFile = File.Create(Path.Combine(tempDir, "Settings.json"));
			byte[] buffer = Encoding.Unicode.GetBytes(javascriptSerializer.Serialize(settingsBackup));
			tempFile.Write(buffer, 0, buffer.Length);
			tempFile.Close();

			tempFile = File.Create(Path.Combine(tempDir, "Version.json"));
			buffer = Encoding.Unicode.GetBytes(javascriptSerializer.Serialize(generateVersionFile("Settings")));
			tempFile.Write(buffer, 0, buffer.Length);
			tempFile.Close();

			using(ZipFile zipFile = new ZipFile()) {
				zipFile.AddDirectory(tempDir, "");
				zipFile.Save(zipFileName);
			}
			Directory.Delete(tempDir, true);

			return true;
		}

		/// <summary>
		/// Backups the pages storage provider.
		/// </summary>
		/// <param name="zipFileName">The zip file name where to store the backup.</param>
		/// <param name="pagesStorageProvider">The pages storage provider.</param>
		/// <returns><c>true</c> if the backup file has been succesfully created.</returns>
		public static bool BackupPagesStorageProvider(string zipFileName, IPagesStorageProviderV30 pagesStorageProvider) {
			JavaScriptSerializer javascriptSerializer = new JavaScriptSerializer();
			javascriptSerializer.MaxJsonLength = javascriptSerializer.MaxJsonLength * 10;

			string tempDir = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), Guid.NewGuid().ToString());
			Directory.CreateDirectory(tempDir);

			List<NamespaceInfo> nspaces = new List<NamespaceInfo>(pagesStorageProvider.GetNamespaces());
			nspaces.Add(null);
			List<NamespaceBackup> namespaceBackupList = new List<NamespaceBackup>(nspaces.Count);
			foreach(NamespaceInfo nspace in nspaces) {

				// Backup categories
				CategoryInfo[] categories = pagesStorageProvider.GetCategories(nspace);
				List<CategoryBackup> categoriesBackup = new List<CategoryBackup>(categories.Length);
				foreach(CategoryInfo category in categories) {
					// Add this category to the categoriesBackup list
					categoriesBackup.Add(new CategoryBackup() {
						FullName = category.FullName,
						Pages = category.Pages
					});
				}

				// Backup NavigationPaths
				NavigationPath[] navigationPaths = pagesStorageProvider.GetNavigationPaths(nspace);
				List<NavigationPathBackup> navigationPathsBackup = new List<NavigationPathBackup>(navigationPaths.Length);
				foreach(NavigationPath navigationPath in navigationPaths) {
					navigationPathsBackup.Add(new NavigationPathBackup() {
						FullName = navigationPath.FullName,
						Pages = navigationPath.Pages
					});
				}

				// Add this namespace to the namespaceBackup list
				namespaceBackupList.Add(new NamespaceBackup() {
					Name = nspace == null ? "" : nspace.Name,
					DefaultPageFullName = nspace == null ? "" : nspace.DefaultPage.FullName,
					Categories = categoriesBackup,
					NavigationPaths = navigationPathsBackup
				});

				// Backup pages (one json file for each page containing a maximum of 100 revisions)
				PageInfo[] pages = pagesStorageProvider.GetPages(nspace);
				foreach(PageInfo page in pages) {
					PageContent pageContent = pagesStorageProvider.GetContent(page);
					PageBackup pageBackup = new PageBackup();
					pageBackup.FullName = page.FullName;
					pageBackup.CreationDateTime = page.CreationDateTime;
					pageBackup.LastModified = pageContent.LastModified;
					pageBackup.Content = pageContent.Content;
					pageBackup.Comment = pageContent.Comment;
					pageBackup.Description = pageContent.Description;
					pageBackup.Keywords = pageContent.Keywords;
					pageBackup.Title = pageContent.Title;
					pageBackup.User = pageContent.User;
					pageBackup.LinkedPages = pageContent.LinkedPages;
					pageBackup.Categories = (from c in pagesStorageProvider.GetCategoriesForPage(page)
											 select c.FullName).ToArray();

					// Backup the 100 most recent versions of the page
					List<PageRevisionBackup> pageContentBackupList = new List<PageRevisionBackup>();
					int[] revisions = pagesStorageProvider.GetBackups(page);
					for(int i = revisions.Length - 1; i > revisions.Length - 100 && i >= 0; i--) {
						PageContent pageRevision = pagesStorageProvider.GetBackupContent(page, revisions[i]);
						PageRevisionBackup pageContentBackup = new PageRevisionBackup() {
							Revision = revisions[i],
							Content = pageRevision.Content,
							Comment = pageRevision.Comment,
							Description = pageRevision.Description,
							Keywords = pageRevision.Keywords,
							Title = pageRevision.Title,
							User = pageRevision.User,
							LastModified = pageRevision.LastModified
						};
						pageContentBackupList.Add(pageContentBackup);
					}
					pageBackup.Revisions = pageContentBackupList;

					// Backup draft of the page
					PageContent draft = pagesStorageProvider.GetDraft(page);
					if(draft != null) {
						pageBackup.Draft = new PageRevisionBackup() {
							Content = draft.Content,
							Comment = draft.Comment,
							Description = draft.Description,
							Keywords = draft.Keywords,
							Title = draft.Title,
							User = draft.User,
							LastModified = draft.LastModified
						};
					}

					// Backup all messages of the page
					List<MessageBackup> messageBackupList = new List<MessageBackup>();
					foreach(Message message in pagesStorageProvider.GetMessages(page)) {
						messageBackupList.Add(BackupMessage(message));
					}
					pageBackup.Messages = messageBackupList;

					FileStream tempFile = File.Create(Path.Combine(tempDir, page.FullName + ".json"));
					byte[] buffer = Encoding.Unicode.GetBytes(javascriptSerializer.Serialize(pageBackup));
					tempFile.Write(buffer, 0, buffer.Length);
					tempFile.Close();
				}
			}
			FileStream tempNamespacesFile = File.Create(Path.Combine(tempDir, "Namespaces.json"));
			byte[] namespacesBuffer = Encoding.Unicode.GetBytes(javascriptSerializer.Serialize(namespaceBackupList));
			tempNamespacesFile.Write(namespacesBuffer, 0, namespacesBuffer.Length);
			tempNamespacesFile.Close();

			// Backup content templates
			ContentTemplate[] contentTemplates = pagesStorageProvider.GetContentTemplates();
			List<ContentTemplateBackup> contentTemplatesBackup = new List<ContentTemplateBackup>(contentTemplates.Length);
			foreach(ContentTemplate contentTemplate in contentTemplates) {
				contentTemplatesBackup.Add(new ContentTemplateBackup() {
					Name = contentTemplate.Name,
					Content = contentTemplate.Content
				});
			}
			FileStream tempContentTemplatesFile = File.Create(Path.Combine(tempDir, "ContentTemplates.json"));
			byte[] contentTemplateBuffer = Encoding.Unicode.GetBytes(javascriptSerializer.Serialize(contentTemplatesBackup));
			tempContentTemplatesFile.Write(contentTemplateBuffer, 0, contentTemplateBuffer.Length);
			tempContentTemplatesFile.Close();

			// Backup Snippets
			Snippet[] snippets = pagesStorageProvider.GetSnippets();
			List<SnippetBackup> snippetsBackup = new List<SnippetBackup>(snippets.Length);
			foreach(Snippet snippet in snippets) {
				snippetsBackup.Add(new SnippetBackup() {
					Name = snippet.Name,
					Content = snippet.Content
				});
			}
			FileStream tempSnippetsFile = File.Create(Path.Combine(tempDir, "Snippets.json"));
			byte[] snippetBuffer = Encoding.Unicode.GetBytes(javascriptSerializer.Serialize(snippetsBackup));
			tempSnippetsFile.Write(snippetBuffer, 0, snippetBuffer.Length);
			tempSnippetsFile.Close();

			FileStream tempVersionFile = File.Create(Path.Combine(tempDir, "Version.json"));
			byte[] versionBuffer = Encoding.Unicode.GetBytes(javascriptSerializer.Serialize(generateVersionFile("Pages")));
			tempVersionFile.Write(versionBuffer, 0, versionBuffer.Length);
			tempVersionFile.Close();

			using(ZipFile zipFile = new ZipFile()) {
				zipFile.AddDirectory(tempDir, "");
				zipFile.Save(zipFileName);
			}
			Directory.Delete(tempDir, true);

			return true;
		}

		// Backup a message with a recursive function to backup all its replies.
		private static MessageBackup BackupMessage(Message message) {
			MessageBackup messageBackup = new MessageBackup() {
				Id = message.ID,
				Subject = message.Subject,
				Body = message.Body,
				DateTime = message.DateTime,
				Username = message.Username
			};
			List<MessageBackup> repliesBackup = new List<MessageBackup>(message.Replies.Length);
			foreach(Message reply in message.Replies) {
				repliesBackup.Add(BackupMessage(reply));
			}
			messageBackup.Replies = repliesBackup;
			return messageBackup;
		}

		/// <summary>
		/// Backups the users storage provider.
		/// </summary>
		/// <param name="zipFileName">The zip file name where to store the backup.</param>
		/// <param name="usersStorageProvider">The users storage provider.</param>
		/// <returns><c>true</c> if the backup file has been succesfully created.</returns>
		public static bool BackupUsersStorageProvider(string zipFileName, IUsersStorageProviderV30 usersStorageProvider) {
			JavaScriptSerializer javascriptSerializer = new JavaScriptSerializer();
			javascriptSerializer.MaxJsonLength = javascriptSerializer.MaxJsonLength * 10;

			string tempDir = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), Guid.NewGuid().ToString());
			Directory.CreateDirectory(tempDir);

			// Backup users
			UserInfo[] users = usersStorageProvider.GetUsers();
			List<UserBackup> usersBackup = new List<UserBackup>(users.Length);
			foreach(UserInfo user in users) {
				usersBackup.Add(new UserBackup() {
					Username = user.Username,
					Active = user.Active,
					DateTime = user.DateTime,
					DisplayName = user.DisplayName,
					Email = user.Email,
					Groups = user.Groups,
					UserData = usersStorageProvider.RetrieveAllUserData(user)
				});
			}
			FileStream tempFile = File.Create(Path.Combine(tempDir, "Users.json"));
			byte[] buffer = Encoding.Unicode.GetBytes(javascriptSerializer.Serialize(usersBackup));
			tempFile.Write(buffer, 0, buffer.Length);
			tempFile.Close();

			// Backup UserGroups
			UserGroup[] userGroups = usersStorageProvider.GetUserGroups();
			List<UserGroupBackup> userGroupsBackup = new List<UserGroupBackup>(userGroups.Length);
			foreach(UserGroup userGroup in userGroups) {
				userGroupsBackup.Add(new UserGroupBackup() {
					Name = userGroup.Name,
					Description = userGroup.Description
				});
			}
			
			tempFile = File.Create(Path.Combine(tempDir, "Groups.json"));
			buffer = Encoding.Unicode.GetBytes(javascriptSerializer.Serialize(userGroupsBackup));
			tempFile.Write(buffer, 0, buffer.Length);
			tempFile.Close();

			tempFile = File.Create(Path.Combine(tempDir, "Version.json"));
			buffer = Encoding.Unicode.GetBytes(javascriptSerializer.Serialize(generateVersionFile("Users")));
			tempFile.Write(buffer, 0, buffer.Length);
			tempFile.Close();


			using(ZipFile zipFile = new ZipFile()) {
				zipFile.AddDirectory(tempDir, "");
				zipFile.Save(zipFileName);
			}
			Directory.Delete(tempDir, true);

			return true;
		}

		/// <summary>
		/// Backups the files storage provider.
		/// </summary>
		/// <param name="zipFileName">The zip file name where to store the backup.</param>
		/// <param name="filesStorageProvider">The files storage provider.</param>
		/// <param name="pagesStorageProviders">The pages storage providers.</param>
		/// <returns><c>true</c> if the backup file has been succesfully created.</returns>
		public static bool BackupFilesStorageProvider(string zipFileName, IFilesStorageProviderV30 filesStorageProvider, IPagesStorageProviderV30[] pagesStorageProviders) {
			JavaScriptSerializer javascriptSerializer = new JavaScriptSerializer();
			javascriptSerializer.MaxJsonLength = javascriptSerializer.MaxJsonLength * 10;

			string tempDir = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), Guid.NewGuid().ToString());
			Directory.CreateDirectory(tempDir);

			DirectoryBackup directoriesBackup = BackupDirectory(filesStorageProvider, tempDir, null);
			FileStream tempFile = File.Create(Path.Combine(tempDir, "Files.json"));
			byte[] buffer = Encoding.Unicode.GetBytes(javascriptSerializer.Serialize(directoriesBackup));
			tempFile.Write(buffer, 0, buffer.Length);
			tempFile.Close();


			// Backup Pages Attachments
			string[] pagesWithAttachment = filesStorageProvider.GetPagesWithAttachments();
			foreach(string pageWithAttachment in pagesWithAttachment) {
				PageInfo pageInfo = FindPageInfo(pageWithAttachment, pagesStorageProviders);
				if(pageInfo != null) {
					string[] attachments = filesStorageProvider.ListPageAttachments(pageInfo);
					List<AttachmentBackup> attachmentsBackup = new List<AttachmentBackup>(attachments.Length);
					foreach(string attachment in attachments) {
						FileDetails attachmentDetails = filesStorageProvider.GetPageAttachmentDetails(pageInfo, attachment);
						attachmentsBackup.Add(new AttachmentBackup() {
							Name = attachment,
							PageFullName = pageWithAttachment,
							LastModified = attachmentDetails.LastModified,
							Size = attachmentDetails.Size
						});
						using(MemoryStream stream = new MemoryStream()) {
							filesStorageProvider.RetrievePageAttachment(pageInfo, attachment, stream, false);
							stream.Seek(0, SeekOrigin.Begin);
							byte[] tempBuffer = new byte[stream.Length];
							stream.Read(tempBuffer, 0, (int)stream.Length);

							DirectoryInfo dir = Directory.CreateDirectory(Path.Combine(tempDir, Path.Combine("__attachments", pageInfo.FullName)));
							tempFile = File.Create(Path.Combine(dir.FullName, attachment));
							tempFile.Write(tempBuffer, 0, tempBuffer.Length);
							tempFile.Close();
						}
					}
					tempFile = File.Create(Path.Combine(tempDir, Path.Combine("__attachments", Path.Combine(pageInfo.FullName, "Attachments.json"))));
					buffer = Encoding.Unicode.GetBytes(javascriptSerializer.Serialize(attachmentsBackup));
					tempFile.Write(buffer, 0, buffer.Length);
					tempFile.Close();
				}
			}

			tempFile = File.Create(Path.Combine(tempDir, "Version.json"));
			buffer = Encoding.Unicode.GetBytes(javascriptSerializer.Serialize(generateVersionFile("Files")));
			tempFile.Write(buffer, 0, buffer.Length);
			tempFile.Close();

			using(ZipFile zipFile = new ZipFile()) {
				zipFile.AddDirectory(tempDir, "");
				zipFile.Save(zipFileName);
			}
			Directory.Delete(tempDir, true);

			return true;
		}

		private static PageInfo FindPageInfo(string pageWithAttachment, IPagesStorageProviderV30[] pagesStorageProviders) {
			foreach(IPagesStorageProviderV30 pagesStorageProvider in pagesStorageProviders) {
				PageInfo pageInfo = pagesStorageProvider.GetPage(pageWithAttachment);
				if(pageInfo != null) return pageInfo;
			}
			return null;
		}

		private static DirectoryBackup BackupDirectory(IFilesStorageProviderV30 filesStorageProvider, string zipFileName, string directory) {
			DirectoryBackup directoryBackup = new DirectoryBackup();

			string[] files = filesStorageProvider.ListFiles(directory);
			List<FileBackup> filesBackup = new List<FileBackup>(files.Length);
			foreach(string file in files) {
				FileDetails fileDetails = filesStorageProvider.GetFileDetails(file);
				filesBackup.Add(new FileBackup() {
					Name = file,
					Size = fileDetails.Size,
					LastModified = fileDetails.LastModified
				});

				FileStream tempFile = File.Create(Path.Combine(zipFileName.Trim('/').Trim('\\'), file.Trim('/').Trim('\\')));
				using(MemoryStream stream = new MemoryStream()) {
					filesStorageProvider.RetrieveFile(file, stream, false);
					stream.Seek(0, SeekOrigin.Begin);
					byte[] buffer = new byte[stream.Length];
					stream.Read(buffer, 0, buffer.Length);
					tempFile.Write(buffer, 0, buffer.Length);
					tempFile.Close();
				}
			}
			directoryBackup.Name = directory;
			directoryBackup.Files = filesBackup;

			string[] directories = filesStorageProvider.ListDirectories(directory);
			List<DirectoryBackup> subdirectoriesBackup = new List<DirectoryBackup>(directories.Length);
			foreach(string d in directories) {
				subdirectoriesBackup.Add(BackupDirectory(filesStorageProvider, zipFileName, d));
			}
			directoryBackup.SubDirectories = subdirectoriesBackup;

			return directoryBackup;
		}

	}

	internal class SettingsBackup {
		public Dictionary<string, string> Settings { get; set; }
		public List<string> PluginsFileNames { get; set; }
		public Dictionary<string, bool> PluginsStatus { get; set; }
		public Dictionary<string, string> PluginsConfiguration { get; set; }
		public List<MetaData> Metadata { get; set; }
		public List<RecentChange> RecentChanges { get; set; }
		public Dictionary<string, string[]> OutgoingLinks { get; set; }
		public List<AclEntryBackup> AclEntries { get; set; }
	}

	internal class AclEntryBackup {
		public Value Value { get; set; }
		public string Subject { get; set; }
		public string Resource { get; set; }
		public string Action { get; set; }
	}

	internal class MetaData {
		public MetaDataItem Item {get; set;}
		public string Tag {get; set;}
		public string Content {get; set;}
	}

	internal class GlobalSettingsBackup {
		public Dictionary<string, string> Settings { get; set; }
		public List<string> pluginsFileNames { get; set; }
	}

	internal class PageBackup {
		public String FullName { get; set; }
		public DateTime CreationDateTime { get; set; }
		public DateTime LastModified { get; set; }
		public string Content { get; set; }
		public string Comment { get; set; }
		public string Description { get; set; }
		public string[] Keywords { get; set; }
		public string Title { get; set; }
		public string User { get; set; }
		public string[] LinkedPages { get; set; }
		public List<PageRevisionBackup> Revisions { get; set; }
		public PageRevisionBackup Draft { get; set; }
		public List<MessageBackup> Messages { get; set; }
		public string[] Categories { get; set; }
	}

	internal class PageRevisionBackup {
		public string Content { get; set; }
		public string Comment { get; set; }
		public string Description { get; set; }
		public string[] Keywords { get; set; }
		public string Title { get; set; }
		public string User { get; set; }
		public DateTime LastModified { get; set; }
		public int Revision { get; set; }
	}

	internal class NamespaceBackup {
		public string Name { get; set; }
		public string DefaultPageFullName { get; set; }
		public List<CategoryBackup> Categories { get; set; }
		public List<NavigationPathBackup> NavigationPaths { get; set; }
	}

	internal class CategoryBackup {
		public string FullName { get; set; }
		public string[] Pages { get; set; }
	}

	internal class ContentTemplateBackup {
		public string Name { get; set; }
		public string Content { get; set; }
	}

	internal class MessageBackup {
		public List<MessageBackup> Replies { get; set; }
		public int Id { get; set; }
		public string Subject { get; set; }
		public string Body { get; set; }
		public DateTime DateTime { get; set; }
		public string Username { get; set; }
	}

	internal class NavigationPathBackup {
		public string FullName { get; set; }
		public string[] Pages { get; set; }
	}

	internal class SnippetBackup {
		public string Name { get; set; }
		public string Content { get; set; }
	}

	internal class UserBackup {
		public string Username { get; set; }
		public bool Active { get; set; }
		public DateTime DateTime { get; set; }
		public string DisplayName { get; set; }
		public string Email { get; set; }
		public string[] Groups { get; set; }
		public IDictionary<string, string> UserData { get; set; }
	}

	internal class UserGroupBackup {
		public string Name { get; set; }
		public string Description { get; set; }
	}

	internal class DirectoryBackup {
		public List<FileBackup> Files { get; set; }
		public List<DirectoryBackup> SubDirectories { get; set; }
		public string Name { get; set; }
	}

	internal class FileBackup {
		public string Name { get; set; }
		public long Size { get; set; }
		public DateTime LastModified { get; set; }
		public string DirectoryName { get; set; }
	}

	internal class VersionFile {
		public string BackupRestoreVersion { get; set; }
		public string WikiVersion { get; set; }
		public string BackupName { get; set; }
	}

	internal class AttachmentBackup {
		public string Name { get; set; }
		public string PageFullName { get; set; }
		public DateTime LastModified { get; set; }
		public long Size { get; set; }
	}

}
