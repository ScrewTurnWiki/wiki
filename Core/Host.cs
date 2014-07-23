
using System;
using System.Collections.Generic;
using ScrewTurn.Wiki.PluginFramework;
using System.Text;
using ScrewTurn.Wiki.SearchEngine;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Implements the <b>IHost</b> interface.
	/// </summary>
	public class Host : IHostV30 {

		private static Host instance;

		/// <summary>
		/// Gets or sets the singleton instance of the <b>Host</b> object.
		/// </summary>
		public static Host Instance {
			get {
				if(instance == null) throw new InvalidOperationException("Host.Instance is null");
				return instance;
			}
			set { instance = value; }
		}

		private Dictionary<string, CustomToolbarItem> customSpecialTags;

		/// <summary>
		/// Initializes a new instance of the <b>PluginHost</b> class.
		/// </summary>
		public Host() {
			customSpecialTags = new Dictionary<string, CustomToolbarItem>(5);
		}

		/// <summary>
		/// Gets the Special Tags added by providers.
		/// </summary>
		public Dictionary<string, CustomToolbarItem> CustomSpecialTags {
			get {
				lock(customSpecialTags) {
					return customSpecialTags;
				}
			}
		}

		/// <summary>
		/// Gets the values of the Wiki Settings.
		/// </summary>
		/// <param name="name">The Setting's Name.</param>
		/// <returns>The Setting's value.</returns>
		public string GetSettingValue(SettingName name) {
			switch(name) {
				case SettingName.ContactEmail:
					return Settings.ContactEmail;
				case SettingName.DateTimeFormat:
					return Settings.DateTimeFormat;
				case SettingName.DefaultLanguage:
					return Settings.DefaultLanguage;
				case SettingName.RootNamespaceDefaultPage:
					return Settings.DefaultPage;
				case SettingName.DefaultTimeZone:
					return Settings.DefaultTimezone.ToString();
				case SettingName.MainUrl:
					return Settings.MainUrl;
				case SettingName.SenderEmail:
					return Settings.SenderEmail;
				case SettingName.WikiTitle:
					return Settings.WikiTitle;
				case SettingName.ThemesDirectory:
					return Settings.ThemesDirectory;
				case SettingName.PublicDirectory:
					return Settings.PublicDirectory;
				case SettingName.UsersCanRegister:
					return Settings.UsersCanRegister.ToString();
				case SettingName.UsernameRegex:
					return Settings.UsernameRegex;
				case SettingName.PasswordRegex:
					return Settings.PasswordRegex;
				case SettingName.EmailRegex:
					return Settings.EmailRegex;
				case SettingName.MainUrlRegex:
					return Settings.MainUrlRegex;
				case SettingName.EnableDoubleClickEditing:
					return Settings.EnableDoubleClickEditing.ToString();
				case SettingName.ProcessSingleLineBreaks:
					return Settings.ProcessSingleLineBreaks.ToString();
				case SettingName.AccountActivationMode:
					return Settings.AccountActivationMode.ToString();
				case SettingName.AllowedFileTypes:
					StringBuilder sb = new StringBuilder(50);
					foreach(string s in Settings.AllowedFileTypes) sb.Append(s + ",");
					return sb.ToString().TrimEnd(',');
				case SettingName.DisableAutomaticVersionCheck:
					return Settings.DisableAutomaticVersionCheck.ToString();
				case SettingName.DisableBreadcrumbsTrail:
					return Settings.DisableBreadcrumbsTrail.ToString();
				case SettingName.DisableCache:
					return Settings.DisableCache.ToString();
				case SettingName.DisableCaptchaControl:
					return Settings.DisableCaptchaControl.ToString();
				case SettingName.DisableConcurrentEditing:
					return Settings.DisableConcurrentEditing.ToString();
				case SettingName.EnableHttpCompression:
					return Settings.EnableHttpCompression.ToString();
				case SettingName.EnableViewStateCompression:
					return Settings.EnableViewStateCompression.ToString();
				case SettingName.LoggingLevel:
					return Settings.LoggingLevel.ToString();
				case SettingName.MaxFileSize:
					return Settings.MaxFileSize.ToString();
				case SettingName.PageExtension:
					return Settings.PageExtension;
				case SettingName.ScriptTagsAllowed:
					return Settings.ScriptTagsAllowed.ToString();
				case SettingName.WikiVersion:
					return Settings.WikiVersion;
				case SettingName.MaxLogSize:
					return Settings.MaxLogSize.ToString();
				case SettingName.MaxRecentChanges:
					return Settings.MaxRecentChanges.ToString();
				case SettingName.CacheSize:
					return Settings.CacheSize.ToString();
				case SettingName.CacheCutSize:
					return Settings.CacheCutSize.ToString();
				case SettingName.EditingSessionTimeout:
					return Collisions.EditingSessionTimeout.ToString();
				case SettingName.AdministratorsGroup:
					return Settings.AdministratorsGroup;
				case SettingName.UsersGroup:
					return Settings.UsersGroup;
				case SettingName.AnonymousGroup:
					return Settings.AnonymousGroup;
				case SettingName.ChangeModerationMode:
					return Settings.ChangeModerationMode.ToString();
				case SettingName.DefaultPagesStorageProvider:
					return Settings.DefaultPagesProvider;
				case SettingName.DefaultUsersStorageProvider:
					return Settings.DefaultUsersProvider;
				case SettingName.DefaultFilesStorageProvider:
					return Settings.DefaultFilesProvider;
				case SettingName.DefaultCacheProvider:
					return Settings.DefaultCacheProvider;
				case SettingName.RootNamespaceTheme:
					return Settings.GetTheme(null);
				case SettingName.ListSize:
					return Settings.ListSize.ToString();
			}
			return "";
		}

		/// <summary>
		/// Gets the list of the Users.
		/// </summary>
		/// <returns>The users.</returns>
		public UserInfo[] GetUsers() {
			return Users.GetUsers().ToArray();
		}

		/// <summary>
		/// Finds a user by username, properly handling Users Storage Providers.
		/// </summary>
		/// <param name="username">The username.</param>
		/// <returns>The <see cref="T:UserInfo" />, or <c>null</c> if no users are found.</returns>
		/// <exception cref="ArgumentNullException">If <b>username</b> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>username</b> is empty.</exception>
		public UserInfo FindUser(string username) {
			if(username == null) throw new ArgumentNullException("username");
			if(username.Length == 0) throw new ArgumentException("Username cannot be empty", "username");

			return Users.FindUser(username);
		}

		/// <summary>
		/// Gets the authenticated user in the current session, if any.
		/// </summary>
		/// <returns>The authenticated user, or <c>null</c> if no user is authenticated.</returns>
		/// <remarks>If the built-it <i>admin</i> user is authenticated, the returned user 
		/// has <i>admin</i> as Username.</remarks>
		public UserInfo GetCurrentUser() {
			return SessionFacade.GetCurrentUser();
		}

		/// <summary>
		/// Gets the list of the user groups.
		/// </summary>
		/// <returns>The groups.</returns>
		public UserGroup[] GetUserGroups() {
			return Users.GetUserGroups().ToArray();
		}

		/// <summary>
		/// Finds a user group by name.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <returns>The <see cref="T:UserGroup "/>, or <c>null</c> if no groups are found.</returns>
		/// <exception cref="ArgumentNullException">If <b>name</b> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>name</b> is empty.</exception>
		public UserGroup FindUserGroup(string name) {
			if(name == null) throw new ArgumentNullException("name");
			if(name.Length == 0) throw new ArgumentException("Name cannot be empty", "name");

			return Users.FindUserGroup(name);
		}

		/// <summary>
		/// Checks whether an action is allowed for a global resource.
		/// </summary>
		/// <param name="action">The action (see <see cref="Actions.ForGlobals" /> class)</param>
		/// <param name="user">The user.</param>
		/// <returns><c>true</c> if the action is allowed, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <b>action</b> or <b>user</b> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>action</b> is empty.</exception>
		public bool CheckActionForGlobals(string action, UserInfo user) {
			if(action == null) throw new ArgumentNullException("action");
			if(action.Length == 0) throw new ArgumentException("Action cannot be empty", "action");

			var temp = user != null ? user : Users.GetAnonymousAccount();

			return AuthChecker.CheckActionForGlobals(action, temp.Username, temp.Groups);
		}

		/// <summary>
		/// Checks whether an action is allowed for a namespace.
		/// </summary>
		/// <param name="nspace">The namespace (<c>null</c> for the root).</param>
		/// <param name="action">The action (see <see cref="Actions.ForNamespaces" /> class)</param>
		/// <param name="user">The user.</param>
		/// <returns><c>true</c> if the action is allowed, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <b>action</b> or <b>user</b> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>action</b> is empty.</exception>
		public bool CheckActionForNamespace(NamespaceInfo nspace, string action, UserInfo user) {
			if(action == null) throw new ArgumentNullException("action");
			if(action.Length == 0) throw new ArgumentException("Action cannot be empty", "action");

			var temp = user != null ? user : Users.GetAnonymousAccount();

			return AuthChecker.CheckActionForNamespace(nspace, action, temp.Username, temp.Groups);
		}

		/// <summary>
		/// Checks whether an action is allowed for a page.
		/// </summary>
		/// <param name="page">The page.</param>
		/// <param name="action">The action (see <see cref="Actions.ForPages" /> class)</param>
		/// <param name="user">The user.</param>
		/// <returns><c>true</c> if the action is allowed, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <b>page</b>, <b>action</b> or <b>user</b> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>action</b> is empty.</exception>
		public bool CheckActionForPage(PageInfo page, string action, UserInfo user) {
			if(page == null) throw new ArgumentNullException("page");
			if(action == null) throw new ArgumentNullException("action");
			if(action.Length == 0) throw new ArgumentException("Action cannot be empty", "action");

			var temp = user != null ? user : Users.GetAnonymousAccount();

			return AuthChecker.CheckActionForPage(page, action, temp.Username, temp.Groups);
		}

		/// <summary>
		/// Checks whether an action is allowed for a directory.
		/// </summary>
		/// <param name="directory">The directory.</param>
		/// <param name="action">The action (see <see cref="Actions.ForDirectories" />).</param>
		/// <param name="user">The user.</param>
		/// <returns><c>true</c> if the action is allowed, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <b>directory</b>, <b>action</b> or <b>user</b> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>action</b> is empty.</exception>
		public bool CheckActionForDirectory(StDirectoryInfo directory, string action, UserInfo user) {
			if(directory == null) throw new ArgumentNullException("directory");
			if(action == null) throw new ArgumentNullException("action");
			if(action.Length == 0) throw new ArgumentException("Action cannot be empty", "action");

			var temp = user != null ? user : Users.GetAnonymousAccount();

			return AuthChecker.CheckActionForDirectory(directory.Provider, directory.FullPath, action, temp.Username, temp.Groups);
		}

		/// <summary>
		/// Gets the theme in use for a namespace.
		/// </summary>
		/// <param name="nspace">The namespace (<c>null</c> for the root).</param>
		/// <returns>The theme.</returns>
		public string GetTheme(NamespaceInfo nspace) {
			return Settings.GetTheme(nspace != null ? nspace.Name : null);
		}

		/// <summary>
		/// Gets the list of the namespaces.
		/// </summary>
		/// <returns>The namespaces.</returns>
		public NamespaceInfo[] GetNamespaces() {
			return Pages.GetNamespaces().ToArray();
		}

		/// <summary>
		/// Finds a namespace by name.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <returns>The <see cref="T:NamespaceInfo" />, or <c>null</c> if no namespaces are found.</returns>
		public NamespaceInfo FindNamespace(string name) {
			return Pages.FindNamespace(name);
		}

		/// <summary>
		/// Gets the list of the Wiki Pages in a namespace.
		/// </summary>
		/// <param name="nspace">The namespace (<c>null</c> for the root).</param>
		/// <returns>The pages.</returns>
		public PageInfo[] GetPages(NamespaceInfo nspace) {
			return Pages.GetPages(nspace).ToArray();
		}

		/// <summary>
		/// Gets the List of Categories in a namespace.
		/// </summary>
		/// <param name="nspace">The namespace (<c>null</c> for the root).</param>
		/// <returns>The categories.</returns>
		public CategoryInfo[] GetCategories(NamespaceInfo nspace) {
			return Pages.GetCategories(nspace).ToArray();
		}

		/// <summary>
		/// Gets the list of Snippets.
		/// </summary>
		/// <returns>The snippets.</returns>
		public Snippet[] GetSnippets() {
			return Snippets.GetSnippets().ToArray();
		}

		/// <summary>
		/// Gets the list of Navigation Paths in a namespace.
		/// </summary>
		/// <param name="nspace">The namespace (<c>null</c> for the root).</param>
		/// <returns>The navigation paths.</returns>
		public NavigationPath[] GetNavigationPaths(NamespaceInfo nspace) {
			return NavigationPaths.GetNavigationPaths(nspace).ToArray();
		}

		/// <summary>
		/// Gets the Categories of a Page.
		/// </summary>
		/// <param name="page">The Page.</param>
		/// <returns>The Categories.</returns>
		/// <exception cref="ArgumentNullException">If <b>page</b> is <c>null</c>.</exception>
		public CategoryInfo[] GetCategoriesPerPage(PageInfo page) {
			if(page == null) throw new ArgumentNullException("page");

			return Pages.GetCategoriesForPage(page);
		}

		/// <summary>
		/// Gets a Wiki Page.
		/// </summary>
		/// <param name="fullName">The full Name of the Page.</param>
		/// <returns>The Wiki Page or <c>null</c>.</returns>
		/// <exception cref="ArgumentNullException">If <b>fullName</b> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>fullName</b> is empty.</exception>
		public PageInfo FindPage(string fullName) {
			if(fullName == null) throw new ArgumentNullException("fullName");
			if(fullName.Length == 0) throw new ArgumentException("Full Name cannot be empty");

			return Pages.FindPage(fullName);
		}

		/// <summary>
		/// Gets the Content of a Page.
		/// </summary>
		/// <param name="page">The Page.</param>
		/// <returns>The Page Content.</returns>
		/// <exception cref="ArgumentNullException">If <b>page</b> is <c>null</c>.</exception>
		public PageContent GetPageContent(PageInfo page) {
			if(page == null) throw new ArgumentNullException("page");

			return Content.GetPageContent(page, true);
		}

		/// <summary>
		/// Gets the Backup/Revision numbers of a Page.
		/// </summary>
		/// <param name="page">The Page.</param>
		/// <returns>The Backup/Revision numbers.</returns>
		/// <exception cref="ArgumentNullException">If <b>page</b> is <c>null</c>.</exception>
		public int[] GetBackups(PageInfo page) {
			if(page == null) throw new ArgumentNullException("page");

			return Pages.GetBackups(page).ToArray();
		}

		/// <summary>
		/// Gets the Content of a Page Backup.
		/// </summary>
		/// <param name="page">The Page.</param>
		/// <param name="revision">The revision.</param>
		/// <returns>The Backup Content.</returns>
		/// <exception cref="ArgumentNullException">If <b>page</b> is <c>null</c>.</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <b>revision</b> is less than zero.</exception>
		public PageContent GetBackupContent(PageInfo page, int revision) {
			if(page == null) throw new ArgumentNullException("page");
			if(revision < 0) throw new ArgumentOutOfRangeException("revision", "Revision must be greater than or equal to zero");

			return Pages.GetBackupContent(page, revision);
		}

		/// <summary>
		/// Gets the formatted content of a Wiki Page.
		/// </summary>
		/// <param name="page">The Page.</param>
		/// <returns>The formatted content.</returns>
		/// <exception cref="ArgumentNullException">If <b>page</b> is <c>null</c>.</exception>
		public string GetFormattedContent(PageInfo page) {
			if(page == null) throw new ArgumentNullException("page");

			PageInfo pageInfo = Pages.FindPage(page.FullName);
			if(pageInfo == null) return null;
			PageContent content = Content.GetPageContent(pageInfo, true);
			return Formatter.Format(content.Content, false, FormattingContext.PageContent, page);
		}

		/// <summary>
		/// Formats a block of WikiMarkup, using the built-in formatter only.
		/// </summary>
		/// <param name="raw">The block of WikiMarkup.</param>
		/// <returns>The formatted content.</returns>
		/// <exception cref="ArgumentNullException">If <b>raw</b> is <c>null</c>.</exception>
		public string Format(string raw) {
			if(raw == null) throw new ArgumentNullException("raw");

			return Formatter.Format(raw, false, FormattingContext.Unknown, null);
		}

		/// <summary>
		/// Prepares content for indexing in the search engine, performing bare-bones formatting and removing all WikiMarkup and XML-like characters.
		/// </summary>
		/// <param name="page">The page being indexed, if any, <c>null</c> otherwise.</param>
		/// <param name="content">The string to prepare.</param>
		/// <returns>The sanitized string.</returns>
		/// <exception cref="ArgumentNullException">If <b>content</b> is <c>null</c>.</exception>
		public string PrepareContentForIndexing(PageInfo page, string content) {
			if(content == null) throw new ArgumentNullException("content");

			// TODO: Improve this method - HTML formatting should not be needed
			return Tools.RemoveHtmlMarkup(FormattingPipeline.FormatWithPhase1And2(content, true,
				page != null ? FormattingContext.PageContent : FormattingContext.Unknown, page));
		}

		/// <summary>
		/// Prepares a title for indexing in the search engine, removing all WikiMarkup and XML-like characters.
		/// </summary>
		/// <param name="page">The page being indexed, if any, <c>null</c> otherwise.</param>
		/// <param name="title">The title to prepare.</param>
		/// <returns>The sanitized string.</returns>
		/// <exception cref="ArgumentNullException">If <b>title</b> is <c>null</c>.</exception>
		public string PrepareTitleForIndexing(PageInfo page, string title) {
			if(title == null) throw new ArgumentNullException("title");

			return FormattingPipeline.PrepareTitle(title, true,
				page != null ? FormattingContext.PageContent : FormattingContext.Unknown, page);
		}

		/// <summary>
		/// Performs a search.
		/// </summary>
		/// <param name="query">The search query.</param>
		/// <param name="fullText">A value indicating whether to perform a full-text search.</param>
		/// <param name="filesAndAttachments">A value indicating whether to search the names of files and attachments.</param>
		/// <param name="options">The search options.</param>
		/// <returns>The search results.</returns>
		/// <exception cref="ArgumentNullException">If <b>query</b> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>query</b> is empty.</exception>
		public SearchResultCollection PerformSearch(string query, bool fullText, bool filesAndAttachments, SearchOptions options) {
			if(query == null) throw new ArgumentNullException("query");
			if(query.Length == 0) throw new ArgumentException("Query cannot be empty", "query");

			return SearchTools.Search(query, fullText, filesAndAttachments, options);
		}

		/// <summary>
		/// Lists directories in a directory.
		/// </summary>
		/// <param name="directory">The directory (<c>null</c> for the root, first invocation).</param>
		/// <returns>The directories.</returns>
		public StDirectoryInfo[] ListDirectories(StDirectoryInfo directory) {
			List<StDirectoryInfo> result = new List<StDirectoryInfo>(20);

			if(directory == null) {
				foreach(IFilesStorageProviderV30 prov in Collectors.FilesProviderCollector.AllProviders) {
					string[] dirs = prov.ListDirectories(null);

					foreach(string dir in dirs) {
						result.Add(new StDirectoryInfo(dir, prov));
					}
				}
			}
			else {
				string[] dirs = directory.Provider.ListDirectories(directory.FullPath);

				foreach(string dir in dirs) {
					result.Add(new StDirectoryInfo(dir, directory.Provider));
				}
			}

			return result.ToArray();
		}

		/// <summary>
		/// Lists files in a directory.
		/// </summary>
		/// <param name="directory">The directory (<c>null</c> for the root, first invocation).</param>
		/// <returns>The files.</returns>
		public StFileInfo[] ListFiles(StDirectoryInfo directory) {
			List<StFileInfo> result = new List<StFileInfo>(20);

			if(directory == null) {
				foreach(IFilesStorageProviderV30 prov in Collectors.FilesProviderCollector.AllProviders) {
					string[] files = prov.ListFiles(null);

					foreach(string file in files) {
						FileDetails details = prov.GetFileDetails(file);
						result.Add(new StFileInfo(details.Size, details.LastModified, details.RetrievalCount, file, prov));
					}
				}
			}
			else {
				string[] files = directory.Provider.ListFiles(directory.FullPath);

				foreach(string file in files) {
					FileDetails details = directory.Provider.GetFileDetails(file);
					result.Add(new StFileInfo(details.Size, details.LastModified, details.RetrievalCount, file, directory.Provider));
				}
			}

			return result.ToArray();
		}

		/// <summary>
		/// Lists page attachments.
		/// </summary>
		/// <param name="page">The page.</param>
		/// <returns>The attachments.</returns>
		/// <exception cref="ArgumentNullException">If <b>page</b> is <c>null</c>.</exception>
		public StFileInfo[] ListPageAttachments(PageInfo page) {
			if(page == null) throw new ArgumentNullException("page");

			List<StFileInfo> result = new List<StFileInfo>(10);
			foreach(IFilesStorageProviderV30 prov in Collectors.FilesProviderCollector.AllProviders) {
				string[] attachments = prov.ListPageAttachments(page);

				foreach(string attn in attachments) {
					FileDetails details = prov.GetPageAttachmentDetails(page, attn);
					result.Add(new StFileInfo(details.Size, details.LastModified, details.RetrievalCount, attn, prov));
				}
			}

			return result.ToArray();
		}

		/// <summary>
		/// Sends an Email.
		/// </summary>
		/// <param name="recipient">The Recipient Email address.</param>
		/// <param name="sender">The Sender's Email address.</param>
		/// <param name="subject">The Subject.</param>
		/// <param name="body">The Body.</param>
		/// <param name="html">True if the message is HTML.</param>
		/// <returns>True if the message has been sent successfully.</returns>
		/// <exception cref="ArgumentNullException">If <b>recipient</b>, <b>sender</b>, <b>subject</b> or <b>body</b> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>recipient</b>, <b>sender</b>, <b>subject</b> or <b>body</b> are empty.</exception>
		public bool SendEmail(string recipient, string sender, string subject, string body, bool html) {
			if(recipient == null) throw new ArgumentNullException("recipient");
			if(recipient.Length == 0) throw new ArgumentException("Recipient cannot be empty");
			if(sender == null) throw new ArgumentNullException("sender");
			if(sender.Length == 0) throw new ArgumentException("Sender cannot be empty");
			if(subject == null) throw new ArgumentNullException("subject");
			if(subject.Length == 0) throw new ArgumentException("Subject cannot be empty", "subject");
			if(body == null) throw new ArgumentNullException("body");
			if(body.Length == 0) throw new ArgumentException("Body cannot be empty", "body");

			try {
				EmailTools.AsyncSendEmail(recipient, sender, subject, body, html);
				return true;
			}
			catch {
				return false;
			}
		}

		/// <summary>
		/// Logs a new message.
		/// </summary>
		/// <param name="message">The Message.</param>
		/// <param name="entryType">The Entry Type.</param>
		/// <param name="user">The user, or <c>null</c>. If <c>null</c>, the system will log "PluginName+System".</param>
		/// <param name="caller">The Component that calls the method. The caller cannot be null.</param>
		/// <exception cref="ArgumentNullException">If <b>message</b> or <b>caller</b> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>message is empty.</b></exception>
		public void LogEntry(string message, LogEntryType entryType, string user, object caller) {
			if(message == null) throw new ArgumentNullException("message");
			if(message.Length == 0) throw new ArgumentException("Message cannot be empty");
			if(caller == null) throw new ArgumentNullException("caller");

			EntryType t = EntryType.General;
			switch(entryType) {
				case LogEntryType.General:
					t = EntryType.General;
					break;
				case LogEntryType.Warning:
					t = EntryType.Warning;
					break;
				case LogEntryType.Error:
					t = EntryType.Error;
					break;
			}
			string name = "?";
			if(user == null) {
				if(caller is IUsersStorageProviderV30) name = ((IUsersStorageProviderV30)caller).Information.Name;
				else if(caller is IPagesStorageProviderV30) name = ((IPagesStorageProviderV30)caller).Information.Name;
				else if(caller is IFormatterProviderV30) name = ((IFormatterProviderV30)caller).Information.Name;
				else if(caller is ISettingsStorageProviderV30) name = ((ISettingsStorageProviderV30)caller).Information.Name;
				else if(caller is IFilesStorageProviderV30) name = ((IFilesStorageProviderV30)caller).Information.Name;
				else if(caller is ICacheProviderV30) name = ((ICacheProviderV30)caller).Information.Name;
				name += "+" + Log.SystemUsername;
			}
			else name = user;
			Log.LogEntry(message, t, name);
		}

		/// <summary>
		/// Changes the language of the current user.
		/// </summary>
		/// <param name="language">The language code.</param>
		public void ChangeCurrentUserLanguage(string language) {
			int timezone = Preferences.LoadTimezoneFromCookie() ?? Settings.DefaultTimezone;
			if(SessionFacade.LoginKey == null || SessionFacade.CurrentUsername == "admin") Preferences.SavePreferencesInCookie(language, timezone);
			else Preferences.SavePreferencesInUserData(language, timezone);
		}

		/// <summary>
		/// Aligns a Date and Time object to the User's Time Zone preferences.
		/// </summary>
		/// <param name="dt">The Date/Time to align.</param>
		/// <returns>The aligned Date/Time.</returns>
		/// <remarks>The method takes care of daylight saving settings.</remarks>
		public DateTime AlignDateTimeWithPreferences(DateTime dt) {
			return Preferences.AlignWithTimezone(dt);
		}

		/// <summary>
		/// Clears the cache.
		/// </summary>
		/// <param name="data">The part of the cache to clear.</param>
		public void ClearCache(CacheData data) {
			switch(data) {
				case CacheData.Pages:
					Content.InvalidateAllPages();
					break;
				case CacheData.MetaFiles:
					Content.ClearPseudoCache();
					break;
				default:
					throw new ArgumentException("Invalid CacheData");
			}
		}

		/// <summary>
		/// Adds an item in the Editing Toolbar.
		/// </summary>
		/// <param name="item">The item to add.</param>
		/// <param name="text">The text of the item.</param>
		/// <param name="value">The value of the item.</param>
		/// <exception cref="ArgumentNullException">If <b>text</b> or <b>value</b> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>text</b> or <b>value</b> are empty, or if they contain single or double quotes, 
		/// or if <b>value</b> does not contain a pipe when <b>item</b> is <b>SpecialTagWrap</b>.</exception>
		public void AddToolbarItem(ToolbarItem item, string text, string value) {
			if(text == null) throw new ArgumentNullException("text");
			if(text.Length == 0) throw new ArgumentException("Text cannot be empty", "text");
			if(text.Contains("\"") || text.Contains("'")) throw new ArgumentException("Text cannot contain single or double quotes", "text");
			if(value == null) throw new ArgumentNullException("value");
			if(value.Length == 0) throw new ArgumentException("Value cannot be empty", "value");
			if(value.Contains("\"") || value.Contains("'")) throw new ArgumentException("Value cannot contain single or double quotes", "value");

			if(item == ToolbarItem.SpecialTagWrap && !value.Contains("|")) throw new ArgumentException("Invalid value for a SpecialTagWrap (pipe not found)", "value");

			lock(customSpecialTags) {
				if(customSpecialTags.ContainsKey(text)) {
					customSpecialTags[text].Value = value;
					customSpecialTags[text].Item = item;
				}
				else customSpecialTags.Add(text, new CustomToolbarItem(item, text, value));
			}
		}

		/// <summary>
		/// Gets the default provider of the specified type.
		/// </summary>
		/// <param name="providerType">The type of the provider (
		/// <see cref="T:IPagesStorageProviderV30" />, 
		/// <see cref="T:IUsersStorageProviderV30" />, 
		/// <see cref="T:IFilesStorageProviderV30" />, 
		/// <see cref="T:ICacheStorageProviderV30" />).</param>
		/// <returns>The Full type name of the default provider of the specified type or <c>null</c>.</returns>
		public string GetDefaultProvider(Type providerType) {
			switch(providerType.FullName) {
				case ProviderLoader.UsersProviderInterfaceName:
					return Settings.DefaultUsersProvider;
				case ProviderLoader.PagesProviderInterfaceName:
					return Settings.DefaultPagesProvider;
				case ProviderLoader.FilesProviderInterfaceName:
					return Settings.DefaultFilesProvider;
				case ProviderLoader.CacheProviderInterfaceName:
					return Settings.DefaultCacheProvider;
				default:
					return null;
			}
		}

		/// <summary>
		/// Gets the pages storage providers, either enabled or disabled.
		/// </summary>
		/// <param name="enabled"><c>true</c> to get enabled providers, <c>false</c> to get disabled providers.</param>
		/// <returns>The providers.</returns>
		public IPagesStorageProviderV30[] GetPagesStorageProviders(bool enabled) {
			if(enabled) return Collectors.PagesProviderCollector.AllProviders;
			else return Collectors.DisabledPagesProviderCollector.AllProviders;
		}

		/// <summary>
		/// Gets the users storage providers, either enabled or disabled.
		/// </summary>
		/// <param name="enabled"><c>true</c> to get enabled providers, <c>false</c> to get disabled providers.</param>
		/// <returns>The providers.</returns>
		public IUsersStorageProviderV30[] GetUsersStorageProviders(bool enabled) {
			if(enabled) return Collectors.UsersProviderCollector.AllProviders;
			else return Collectors.DisabledUsersProviderCollector.AllProviders;
		}

		/// <summary>
		/// Gets the files storage providers, either enabled or disabled.
		/// </summary>
		/// <param name="enabled"><c>true</c> to get enabled providers, <c>false</c> to get disabled providers.</param>
		/// <returns>The providers.</returns>
		public IFilesStorageProviderV30[] GetFilesStorageProviders(bool enabled) {
			if(enabled) return Collectors.FilesProviderCollector.AllProviders;
			else return Collectors.DisabledFilesProviderCollector.AllProviders;
		}

		/// <summary>
		/// Gets the cache providers, either enabled or disabled.
		/// </summary>
		/// <param name="enabled"><c>true</c> to get enabled providers, <c>false</c> to get disabled providers.</param>
		/// <returns>The providers.</returns>
		public ICacheProviderV30[] GetCacheProviders(bool enabled) {
			if(enabled) return Collectors.CacheProviderCollector.AllProviders;
			else return Collectors.DisabledCacheProviderCollector.AllProviders;
		}

		/// <summary>
		/// Gets the formatter providers, either enabled or disabled.
		/// </summary>
		/// <param name="enabled"><c>true</c> to get enabled providers, <c>false</c> to get disabled providers.</param>
		/// <returns>The providers.</returns>
		public IFormatterProviderV30[] GetFormatterProviders(bool enabled) {
			if(enabled) return Collectors.FormatterProviderCollector.AllProviders;
			else return Collectors.DisabledFormatterProviderCollector.AllProviders;
		}

		/// <summary>
		/// Gets the current settings storage provider.
		/// </summary>
		/// <returns>The settings storage provider.</returns>
		public ISettingsStorageProviderV30 GetSettingsStorageProvider() {
			return Settings.Provider;
		}

		/// <summary>
		/// Gets the configuration of a provider.
		/// </summary>
		/// <param name="providerTypeName">The type name of the provider, such as 'Vendor.Namespace.Provider'.</param>
		/// <returns>The configuration (can be empty or <c>null</c>).</returns>
		/// <exception cref="ArgumentNullException">If <b>providerTypeName</b> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>providerTypeName</b> is empty.</exception>
		public string GetProviderConfiguration(string providerTypeName) {
			if(providerTypeName == null) throw new ArgumentNullException("providerTypeName");
			if(providerTypeName.Length == 0) throw new ArgumentException("Provider Type Name cannot be empty", "providerTypeName");

			if(providerTypeName == Settings.Provider.GetType().FullName) {
				return StartupTools.GetSettingsStorageProviderConfiguration();
			}
			else return ProviderLoader.LoadConfiguration(providerTypeName);
		}

		/// <summary>
		/// Sets the configuration of a provider.
		/// </summary>
		/// <param name="provider">The provider of which to set the configuration.</param>
		/// <param name="configuration">The configuration to set.</param>
		/// <returns><c>true</c> if the configuration is set, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <b>provider</b> is <c>null</c>.</exception>
		public bool SetProviderConfiguration(IProviderV30 provider, string configuration) {
			if(provider == null) throw new ArgumentNullException("provider");

			if(configuration == null) configuration = "";

			ProviderLoader.SaveConfiguration(provider.GetType().FullName, configuration);

			return true;
		}

		/// <summary>
		/// Upgrades the old Page Status to use the new ACL facilities.
		/// </summary>
		/// <param name="page">The page of which to upgrade the status.</param>
		/// <param name="oldStatus">The old status ('L' = Locked, 'P' = Public).</param>
		/// <returns><c>true</c> if the operation succeeded, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <b>page</b> is <c>null</c>.</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <b>oldStatus</b> is invalid.</exception>
		public bool UpgradePageStatusToAcl(PageInfo page, char oldStatus) {
			if(page == null) throw new ArgumentNullException("page");

			switch(oldStatus) {
				case 'L':
					// Locked: only administrators can edit this page
					return AuthWriter.SetPermissionForPage(AuthStatus.Deny, page, Actions.ForPages.ModifyPage,
						Users.FindUserGroup(Settings.UsersGroup));
				case 'P':
					// Public: anonymous users can edit this page
					return AuthWriter.SetPermissionForPage(AuthStatus.Grant, page, Actions.ForPages.ModifyPage,
						Users.FindUserGroup(Settings.AnonymousGroup));
				default:
					throw new ArgumentOutOfRangeException("oldStatus", "Invalid old status code");
			}
		}

		/// <summary>
		/// Upgrades the old security flags to use the new ACL facilities and user groups support.
		/// </summary>
		/// <param name="administrators">The administrators group.</param>
		/// <param name="users">The users group.</param>
		/// <returns><c>true</c> if the operation succeeded, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <b>administrators</b> or <b>users</b> are <c>null</c>.</exception>
		public bool UpgradeSecurityFlagsToGroupsAcl(UserGroup administrators, UserGroup users) {
			if(administrators == null) throw new ArgumentNullException("administrators");
			if(users == null) throw new ArgumentNullException("users");

			bool done = true;
			done &= StartupTools.SetAdministratorsGroupDefaultPermissions(administrators);
			done &= StartupTools.SetUsersGroupDefaultPermissions(users);

			return done;
		}

		/// <summary>
		/// Overrides the public directory.
		/// </summary>
		/// <param name="fullPath">The new full path of the public directory.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="fullPath"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="fullPath"/> is empty.</exception>
		/// <exception cref="InvalidOperationException">If it's too late to override the public directory.</exception>
		public void OverridePublicDirectory(string fullPath) {
			if(fullPath == null) throw new ArgumentNullException("fullPath");
			if(fullPath == "") throw new ArgumentException("Full Path cannot be empty", "fullPath");

			Settings.OverridePublicDirectory(fullPath);
		}

		/// <summary>
		/// Event fired whenever an activity is performed on a User Account.
		/// </summary>
		public event EventHandler<UserAccountActivityEventArgs> UserAccountActivity;

		/// <summary>
		/// Fires the UserAccountActivity event.
		/// </summary>
		/// <param name="user">The user the activity refers to.</param>
		/// <param name="activity">The activity.</param>
		public void OnUserAccountActivity(UserInfo user, UserAccountActivity activity) {
			if(UserAccountActivity != null) {
				UserAccountActivity(this, new UserAccountActivityEventArgs(user, activity));
			}
		}

		/// <summary>
		/// Event fired whenever an activity is performed on a user group.
		/// </summary>
		public event EventHandler<UserGroupActivityEventArgs> UserGroupActivity;

		/// <summary>
		/// Fires the UserGroupActivity event.
		/// </summary>
		/// <param name="group">The group the activity refers to.</param>
		/// <param name="activity">The activity.</param>
		public void OnUserGroupActivity(UserGroup group, UserGroupActivity activity) {
			if(UserGroupActivity != null) {
				UserGroupActivity(this, new UserGroupActivityEventArgs(group, activity));
			}
		}

		/// <summary>
		/// Event fired whenever an activity is performed on a namespace.
		/// </summary>
		public event EventHandler<NamespaceActivityEventArgs> NamespaceActivity;

		/// <summary>
		/// Fires the NamespaceActivity event.
		/// </summary>
		/// <param name="nspace">The namespace the activity refers to.</param>
		/// <param name="nspaceOldName">The old name of the renamed namespace, or <c>null</c>.</param>
		/// <param name="activity">The activity.</param>
		public void OnNamespaceActivity(NamespaceInfo nspace, string nspaceOldName, NamespaceActivity activity) {
			if(NamespaceActivity != null) {
				NamespaceActivity(this, new NamespaceActivityEventArgs(nspace, nspaceOldName, activity));
			}
		}

		/// <summary>
		/// Even fired whenever an activity is performed on a Page.
		/// </summary>
		public event EventHandler<PageActivityEventArgs> PageActivity;

		/// <summary>
		/// Fires the PageActivity event.
		/// </summary>
		/// <param name="page">The page the activity refers to.</param>
		/// <param name="pageOldName">The old name of the renamed page, or <c>null</c>.</param>
		/// <param name="author">The author of the activity.</param>
		/// <param name="activity">The activity.</param>
		public void OnPageActivity(PageInfo page, string pageOldName, string author, PageActivity activity) {
			if(PageActivity != null) {
				PageActivity(this, new PageActivityEventArgs(page, pageOldName, author, activity));
			}
		}

		/// <summary>
		/// Event fired whenever an activity is performed on a file, directory or attachment.
		/// </summary>
		public event EventHandler<FileActivityEventArgs> FileActivity;

		/// <summary>
		/// Fires the FileActivity event.
		/// </summary>
		/// <param name="provider">The provider that handles the file.</param>
		/// <param name="file">The name of the file that changed.</param>
		/// <param name="oldFileName">The old name of the renamed file, if any.</param>
		/// <param name="activity">The activity.</param>
		public void OnFileActivity(string provider, string file, string oldFileName, FileActivity activity) {
			if(FileActivity != null) {
				IFilesStorageProviderV30 prov = Collectors.FilesProviderCollector.GetProvider(provider);

				FileActivity(this, new FileActivityEventArgs(
					new StFileInfo(prov.GetFileDetails(file), file, prov), oldFileName, null, null, null, activity));
			}
		}

		/// <summary>
		/// Fires the FileActivity event.
		/// </summary>
		/// <param name="provider">The provider that handles the attachment.</param>
		/// <param name="attachment">The old name of the renamed attachment, if any.</param>
		/// <param name="page">The page that owns the attachment.</param>
		/// <param name="oldAttachmentName">The old name of the renamed attachment, if any.</param>
		/// <param name="activity">The activity.</param>
		public void OnAttachmentActivity(string provider, string attachment, string page, string oldAttachmentName, FileActivity activity) {
			if(FileActivity != null) {
				IFilesStorageProviderV30 prov = Collectors.FilesProviderCollector.GetProvider(provider);
				PageInfo pageInfo = Pages.FindPage(page);

				FileActivity(this, new FileActivityEventArgs(
					new StFileInfo(prov.GetPageAttachmentDetails(pageInfo, attachment), attachment, prov), oldAttachmentName, null, null, pageInfo, activity));
			}
		}

		/// <summary>
		/// Fires the FileActivity event.
		/// </summary>
		/// <param name="provider">The provider that handles the directory.</param>
		/// <param name="directory">The directory that changed.</param>
		/// <param name="oldDirectoryName">The old name of the renamed directory, if any.</param>
		/// <param name="activity">The activity.</param>
		public void OnDirectoryActivity(string provider, string directory, string oldDirectoryName, FileActivity activity) {
			if(FileActivity != null) {
				IFilesStorageProviderV30 prov = Collectors.FilesProviderCollector.GetProvider(provider);

				FileActivity(this, new FileActivityEventArgs(
					null, null, new StDirectoryInfo(directory, prov), oldDirectoryName, null, activity));
			}
		}

	}

	/// <summary>
	/// Represents a custom toolbar item.
	/// </summary>
	public class CustomToolbarItem {

		private ToolbarItem item;
		private string text, value;

		/// <summary>
		/// Initializes a new instance of the <b>ToolbarItem</b> class.
		/// </summary>
		/// <param name="item">The item.</param>
		/// <param name="text">The text.</param>
		/// <param name="value">The value.</param>
		public CustomToolbarItem(ToolbarItem item, string text, string value) {
			this.item = item;
			this.text = text;
			this.value = value;
		}

		/// <summary>
		/// Gets or sets the item.
		/// </summary>
		public ToolbarItem Item {
			get { return item; }
			set { item = value; }
		}

		/// <summary>
		/// Gets the text.
		/// </summary>
		public string Text {
			get { return text; }
		}

		/// <summary>
		/// Gets or sets the value.
		/// </summary>
		public string Value {
			get { return value; }
			set { this.value = value; }
		}

	}

}
