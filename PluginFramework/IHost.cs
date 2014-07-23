
using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Mail;
using ScrewTurn.Wiki.SearchEngine;

namespace ScrewTurn.Wiki.PluginFramework {

	/// <summary>
	/// It is the interface that the ScrewTurn Wiki's Host object implements.
	/// </summary>
	public interface IHostV30 {

		/// <summary>
		/// Gets the values of the Wiki Settings.
		/// </summary>
		/// <param name="name">The Setting's Name.</param>
		/// <returns>The Setting's value.</returns>
		string GetSettingValue(SettingName name);

		/// <summary>
		/// Gets the list of the Users.
		/// </summary>
		/// <returns>The users.</returns>
		UserInfo[] GetUsers();

		/// <summary>
		/// Finds a user by username.
		/// </summary>
		/// <param name="username">The username.</param>
		/// <returns>The <see cref="T:UserInfo" />, or <c>null</c> if no users are found.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="username"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="username"/> is empty.</exception>
		UserInfo FindUser(string username);

		/// <summary>
		/// Gets the authenticated user in the current session, if any.
		/// </summary>
		/// <returns>The authenticated user, or <c>null</c> if no user is authenticated.</returns>
		/// <remarks>If the built-it <i>admin</i> user is authenticated, the returned user 
		/// has <i>admin</i> as Username.</remarks>
		UserInfo GetCurrentUser();

		/// <summary>
		/// Gets the list of the user groups.
		/// </summary>
		/// <returns>The groups.</returns>
		UserGroup[] GetUserGroups();

		/// <summary>
		/// Finds a user group by name.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <returns>The <see cref="T:UserGroup "/>, or <c>null</c> if no groups are found.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="name"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="name"/> is empty.</exception>
		UserGroup FindUserGroup(string name);

		/// <summary>
		/// Checks whether an action is allowed for a global resource.
		/// </summary>
		/// <param name="action">The action (see <see cref="Actions.ForGlobals" /> class)</param>
		/// <param name="user">The user.</param>
		/// <returns><c>true</c> if the action is allowed, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="action"/> or <paramref name="user"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="action"/> is empty.</exception>
		bool CheckActionForGlobals(string action, UserInfo user);

		/// <summary>
		/// Checks whether an action is allowed for a namespace.
		/// </summary>
		/// <param name="nspace">The namespace (<c>null</c> for the root).</param>
		/// <param name="action">The action (see <see cref="Actions.ForNamespaces" /> class)</param>
		/// <param name="user">The user.</param>
		/// <returns><c>true</c> if the action is allowed, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="action"/> or <paramref name="user"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="action"/> is empty.</exception>
		bool CheckActionForNamespace(NamespaceInfo nspace, string action, UserInfo user);

		/// <summary>
		/// Checks whether an action is allowed for a page.
		/// </summary>
		/// <param name="page">The page.</param>
		/// <param name="action">The action (see <see cref="Actions.ForPages" /> class)</param>
		/// <param name="user">The user.</param>
		/// <returns><c>true</c> if the action is allowed, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="page"/>, <paramref name="action"/> or <paramref name="user"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="action"/> is empty.</exception>
		bool CheckActionForPage(PageInfo page, string action, UserInfo user);

		/// <summary>
		/// Checks whether an action is allowed for a directory.
		/// </summary>
		/// <param name="directory">The directory.</param>
		/// <param name="action">The action (see <see cref="Actions.ForDirectories" />).</param>
		/// <param name="user">The user.</param>
		/// <returns><c>true</c> if the action is allowed, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="directory"/>, <paramref name="action"/> or <paramref name="user"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="action"/> is empty.</exception>
		bool CheckActionForDirectory(StDirectoryInfo directory, string action, UserInfo user);

		/// <summary>
		/// Gets the theme in use for a namespace.
		/// </summary>
		/// <param name="nspace">The namespace (<c>null</c> for the root).</param>
		/// <returns>The theme.</returns>
		string GetTheme(NamespaceInfo nspace);

		/// <summary>
		/// Gets the list of the namespaces.
		/// </summary>
		/// <returns>The namespaces.</returns>
		NamespaceInfo[] GetNamespaces();

		/// <summary>
		/// Finds a namespace by name.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <returns>The <see cref="T:NamespaceInfo" />, or <c>null</c> if no namespaces are found.</returns>
		NamespaceInfo FindNamespace(string name);

		/// <summary>
		/// Gets the list of the Pages in a namespace.
		/// </summary>
		/// <param name="nspace">The namespace (<c>null</c> for the root).</param>
		/// <returns>The pages.</returns>
		PageInfo[] GetPages(NamespaceInfo nspace);

		/// <summary>
		/// Gets the list of the Categories in a namespace.
		/// </summary>
		/// <param name="nspace">The namespace (<c>null</c> for the root).</param>
		/// <returns>The categories.</returns>
		CategoryInfo[] GetCategories(NamespaceInfo nspace);

		/// <summary>
		/// Gets the list of Snippets.
		/// </summary>
		/// <returns>The snippets.</returns>
		Snippet[] GetSnippets();

		/// <summary>
		/// Gets the list of Navigation Paths in a namespace.
		/// </summary>
		/// <param name="nspace">The namespace (<c>null</c> for the root).</param>
		/// <returns>The navigation paths.</returns>
		NavigationPath[] GetNavigationPaths(NamespaceInfo nspace);

		/// <summary>
		/// Gets the Categories of a Page.
		/// </summary>
		/// <param name="page">The Page.</param>
		/// <returns>The Categories.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="page"/> is <c>null</c>.</exception>
		CategoryInfo[] GetCategoriesPerPage(PageInfo page);

		/// <summary>
		/// Gets the WikiPage with the specified full Name.
		/// </summary>
		/// <param name="fullName">The full Name of the Page.</param>
		/// <returns>The Wiki Page, or <c>null</c> if no pages are found.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="fullName"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="fullName"/> is empty.</exception>
		PageInfo FindPage(string fullName);

		/// <summary>
		/// Gets the Content of a Page.
		/// </summary>
		/// <param name="page">The Page.</param>
		/// <returns>The Page Content.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="page"/> is <c>null</c>.</exception>
		PageContent GetPageContent(PageInfo page);

		/// <summary>
		/// Gets the Backup/Revision numbers of a Page.
		/// </summary>
		/// <param name="page">The Page.</param>
		/// <returns>The Backup/Revision numbers.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="page"/> is <c>null</c>.</exception>
		int[] GetBackups(PageInfo page);

		/// <summary>
		/// Gets the Content of a Page Backup.
		/// </summary>
		/// <param name="page">The Page.</param>
		/// <param name="revision">The revision.</param>
		/// <returns>The Backup Content.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="page"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="revision"/> is less than zero.</exception>
		PageContent GetBackupContent(PageInfo page, int revision);

		/// <summary>
		/// Gets the formatted content of a Page, retrieving it from the cache (if available).
		/// </summary>
		/// <param name="page">The Page.</param>
		/// <returns>The formatted content of the Page.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="page"/> is <c>null</c>.</exception>
		string GetFormattedContent(PageInfo page);

		/// <summary>
		/// Formats a block of WikiMarkup, using the built-in formatter only.
		/// </summary>
		/// <param name="raw">The block of WikiMarkup.</param>
		/// <returns>The formatted content.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="raw"/> is <c>null</c>.</exception>
		string Format(string raw);

		/// <summary>
		/// Prepares content for indexing in the search engine, performing bare-bones formatting and removing all WikiMarkup and XML-like characters.
		/// </summary>
		/// <param name="page">The page being indexed, if any, <c>null</c> otherwise.</param>
		/// <param name="content">The string to prepare.</param>
		/// <returns>The sanitized string.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="content"/> is <c>null</c>.</exception>
		string PrepareContentForIndexing(PageInfo page, string content);

		/// <summary>
		/// Prepares a title for indexing in the search engine, removing all WikiMarkup and XML-like characters.
		/// </summary>
		/// <param name="page">The page being indexed, if any, <c>null</c> otherwise.</param>
		/// <param name="title">The title to prepare.</param>
		/// <returns>The sanitized string.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="title"/> is <c>null</c>.</exception>
		string PrepareTitleForIndexing(PageInfo page, string title);

		/// <summary>
		/// Performs a search.
		/// </summary>
		/// <param name="query">The search query.</param>
		/// <param name="fullText">A value indicating whether to perform a full-text search.</param>
		/// <param name="filesAndAttachments">A value indicating whether to search the names of files and attachments.</param>
		/// <param name="options">The search options.</param>
		/// <returns>The search results.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="query"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="query"/> is empty.</exception>
		SearchResultCollection PerformSearch(string query, bool fullText, bool filesAndAttachments, SearchOptions options);

		/// <summary>
		/// Lists directories in a directory.
		/// </summary>
		/// <param name="directory">The directory (<c>null</c> for the root, first invocation).</param>
		/// <returns>The directories.</returns>
		StDirectoryInfo[] ListDirectories(StDirectoryInfo directory);

		/// <summary>
		/// Lists files in a directory.
		/// </summary>
		/// <param name="directory">The directory (<c>null</c> for the root, first invocation).</param>
		/// <returns>The files.</returns>
		StFileInfo[] ListFiles(StDirectoryInfo directory);

		/// <summary>
		/// Lists page attachments.
		/// </summary>
		/// <param name="page">The page.</param>
		/// <returns>The attachments.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="page"/> is <c>null</c>.</exception>
		StFileInfo[] ListPageAttachments(PageInfo page);

		/// <summary>
		/// Sends an Email.
		/// </summary>
		/// <param name="recipient">The Recipient Email address.</param>
		/// <param name="sender">The Sender's Email address.</param>
		/// <param name="subject">The Subject.</param>
		/// <param name="body">The Body.</param>
		/// <param name="html">True if the message is HTML.</param>
		/// <returns>True if the message has been sent successfully.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="recipient"/>, <paramref name="sender"/>, <paramref name="subject"/> or <paramref name="body"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="recipient"/>, <paramref name="sender"/>, <paramref name="subject"/> or <paramref name="body"/> are empty.</exception>
		bool SendEmail(string recipient, string sender, string subject, string body, bool html);

		/// <summary>
		/// Logs a new message.
		/// </summary>
		/// <param name="message">The Message.</param>
		/// <param name="entryType">The Entry Type.</param>
		/// <param name="user">The user, or <c>null</c>. If <c>null</c>, the system will log "PluginName+System".</param>
		/// <param name="caller">The Component that calls the method. The caller cannot be null.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="message"/> or <paramref name="caller"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="message"/> is empty.</exception>
		void LogEntry(string message, LogEntryType entryType, string user, object caller);

		/// <summary>
		/// Changes the language of the current user.
		/// </summary>
		/// <param name="language">The language code.</param>
		void ChangeCurrentUserLanguage(string language);

		/// <summary>
		/// Aligns a Date and Time object to the User's Time Zone preferences.
		/// </summary>
		/// <param name="dt">The Date/Time to align.</param>
		/// <returns>The aligned Date/Time.</returns>
		/// <remarks>The method takes care of daylight saving settings.</remarks>
		DateTime AlignDateTimeWithPreferences(DateTime dt);

		/// <summary>
		/// Clears the cache.
		/// </summary>
		/// <param name="data">The part of the cache to clear.</param>
		void ClearCache(CacheData data);

		/// <summary>
		/// Adds an item in the Editing Toolbar.
		/// </summary>
		/// <param name="item">The item to add.</param>
		/// <param name="text">The text of the item showed in the toolbar.</param>
		/// <param name="value">The value of the item, placed in the content: if <b>item</b> is <b>ToolbarItem.SpecialTagWrap</b>, separate start and end tag with a pipe.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="text"/> or <paramref name="value"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="text"/> or <paramref name="value"/> are empty, or if they contain single or double quotes, 
		/// or if <paramref name="value"/> does not contain a pipe when <paramref name="item"/> is <b>SpecialTagWrap</b>.</exception>
		void AddToolbarItem(ToolbarItem item, string text, string value);

		/// <summary>
		/// Gets the default provider of the specified type.
		/// </summary>
		/// <param name="providerType">The type of the provider (
		/// <see cref="T:IPagesStorageProviderV30" />, 
		/// <see cref="T:IUsersStorageProviderV30" />, 
		/// <see cref="T:IFilesStorageProviderV30" />, 
		/// <see cref="T:ICacheStorageProviderV30" />,
		/// <see cref="T:IFormatterProviderV30" />).</param>
		/// <returns>The Full type name of the default provider of the specified type or <c>null</c>.</returns>
		string GetDefaultProvider(Type providerType);

		/// <summary>
		/// Gets the pages storage providers, either enabled or disabled.
		/// </summary>
		/// <param name="enabled"><c>true</c> to get enabled providers, <c>false</c> to get disabled providers.</param>
		/// <returns>The providers.</returns>
		IPagesStorageProviderV30[] GetPagesStorageProviders(bool enabled);

		/// <summary>
		/// Gets the users storage providers, either enabled or disabled.
		/// </summary>
		/// <param name="enabled"><c>true</c> to get enabled providers, <c>false</c> to get disabled providers.</param>
		/// <returns>The providers.</returns>
		IUsersStorageProviderV30[] GetUsersStorageProviders(bool enabled);

		/// <summary>
		/// Gets the files storage providers, either enabled or disabled.
		/// </summary>
		/// <param name="enabled"><c>true</c> to get enabled providers, <c>false</c> to get disabled providers.</param>
		/// <returns>The providers.</returns>
		IFilesStorageProviderV30[] GetFilesStorageProviders(bool enabled);

		/// <summary>
		/// Gets the cache providers, either enabled or disabled.
		/// </summary>
		/// <param name="enabled"><c>true</c> to get enabled providers, <c>false</c> to get disabled providers.</param>
		/// <returns>The providers.</returns>
		ICacheProviderV30[] GetCacheProviders(bool enabled);

		/// <summary>
		/// Gets the formatter providers, either enabled or disabled.
		/// </summary>
		/// <param name="enabled"><c>true</c> to get enabled providers, <c>false</c> to get disabled providers.</param>
		/// <returns>The providers.</returns>
		IFormatterProviderV30[] GetFormatterProviders(bool enabled);

		/// <summary>
		/// Gets the current settings storage provider.
		/// </summary>
		/// <returns>The settings storage provider.</returns>
		ISettingsStorageProviderV30 GetSettingsStorageProvider();

		/// <summary>
		/// Gets the configuration of a provider.
		/// </summary>
		/// <param name="providerTypeName">The type name of the provider, such as 'Vendor.Namespace.Provider'.</param>
		/// <returns>The configuration (can be empty or <c>null</c>).</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="providerTypeName"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="providerTypeName"/> is empty.</exception>
		string GetProviderConfiguration(string providerTypeName);

		/// <summary>
		/// Sets the configuration of a provider.
		/// </summary>
		/// <param name="provider">The provider of which to set the configuration.</param>
		/// <param name="configuration">The configuration to set.</param>
		/// <returns><c>true</c> if the configuration is set, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="provider"/> is <c>null</c>.</exception>
		bool SetProviderConfiguration(IProviderV30 provider, string configuration);

		/// <summary>
		/// Upgrades the old Page Status to use the new ACL facilities.
		/// </summary>
		/// <param name="page">The page of which to upgrade the status.</param>
		/// <param name="oldStatus">The old status ('L' = Locked, 'P' = Public).</param>
		/// <returns><c>true</c> if the operation succeeded, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="page"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="oldStatus"/> is invalid.</exception>
		bool UpgradePageStatusToAcl(PageInfo page, char oldStatus);

		/// <summary>
		/// Upgrades the old security flags to use the new ACL facilities and user groups support.
		/// </summary>
		/// <param name="administrators">The administrators group.</param>
		/// <param name="users">The users group.</param>
		/// <returns><c>true</c> if the operation succeeded, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="administrators"/> or <paramref name="users"/> are <c>null</c>.</exception>
		bool UpgradeSecurityFlagsToGroupsAcl(UserGroup administrators, UserGroup users);

		/// <summary>
		/// Overrides the public directory.
		/// </summary>
		/// <param name="fullPath">The new full path of the public directory.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="fullPath"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="fullPath"/> is empty.</exception>
		/// <exception cref="InvalidOperationException">If it's too late to override the public directory.</exception>
		void OverridePublicDirectory(string fullPath);

		/// <summary>
		/// Event fired whenever an activity is performed on a User Account.
		/// </summary>
		event EventHandler<UserAccountActivityEventArgs> UserAccountActivity;

		/// <summary>
		/// Event fired whenever an activity is performed on a user group.
		/// </summary>
		event EventHandler<UserGroupActivityEventArgs> UserGroupActivity;

		/// <summary>
		/// Event fired whenever an activity is performed on a namespace.
		/// </summary>
		event EventHandler<NamespaceActivityEventArgs> NamespaceActivity;

		/// <summary>
		/// Even fired whenever an activity is performed on a Page.
		/// </summary>
		event EventHandler<PageActivityEventArgs> PageActivity;

		/// <summary>
		/// Event fired whenever an activity is performed on a file, directory or attachment.
		/// </summary>
		event EventHandler<FileActivityEventArgs> FileActivity;

	}

	/// <summary>
	/// Enumerates the Types of Log Entries.
	/// </summary>
	public enum LogEntryType {
		/// <summary>
		/// Represents a simple Message.
		/// </summary>
		General,
		/// <summary>
		/// Represents a Warning.
		/// </summary>
		Warning,
		/// <summary>
		/// Represents an Error.
		/// </summary>
		Error
	}

	/// <summary>
	/// Enumerates the Setting values' names.
	/// </summary>
	public enum SettingName {
		/// <summary>
		/// The Title of the Wiki.
		/// </summary>
		WikiTitle,
		/// <summary>
		/// The Main URL of the Wiki.
		/// </summary>
		MainUrl,
		/// <summary>
		/// The default page of the root namespace.
		/// </summary>
		RootNamespaceDefaultPage,
		/// <summary>
		/// The Contact Email.
		/// </summary>
		ContactEmail,
		/// <summary>
		/// The Sender Email.
		/// </summary>
		SenderEmail,
		/// <summary>
		/// The Date/Time format.
		/// </summary>
		DateTimeFormat,
		/// <summary>
		/// The default Language (for example <b>en-US</b>).
		/// </summary>
		DefaultLanguage,
		/// <summary>
		/// The default Time Zone (a string representing the shift in <b>minutes</b> respect to the Greenwich time, for example <b>-120</b>).
		/// </summary>
		DefaultTimeZone,
		/// <summary>
		/// The Themes directory.
		/// </summary>
		ThemesDirectory,
		/// <summary>
		/// The Public directory.
		/// </summary>
		PublicDirectory,
		/// <summary>
		/// A value (true/false) indicating whether users can create new accounts.
		/// </summary>
		UsersCanRegister,
		/// <summary>
		/// The regex used to validate usernames.
		/// </summary>
		UsernameRegex,
		/// <summary>
		/// The regex used to validate passwords.
		/// </summary>
		PasswordRegex,
		/// <summary>
		/// The regex used to validate email addresses.
		/// </summary>
		EmailRegex,
		/// <summary>
		/// The regex used to validate the main Wiki URL.
		/// </summary>
		MainUrlRegex,
		/// <summary>
		/// A value (true/false) indicating whether to activate page editing with a double click.
		/// </summary>
		EnableDoubleClickEditing,
		/// <summary>
		/// A value (true/false) indicating whether to process single line breaks in WikiMarkup.
		/// </summary>
		ProcessSingleLineBreaks,
		/// <summary>
		/// The account activation mode.
		/// </summary>
		AccountActivationMode,
		/// <summary>
		/// The file types allowed for upload.
		/// </summary>
		AllowedFileTypes,
		/// <summary>
		/// A value (true/false) indicating whether to disable the automatic version check.
		/// </summary>
		DisableAutomaticVersionCheck,
		/// <summary>
		/// A value (true/false) indicating whether to disable the breadcrumbs trail.
		/// </summary>
		DisableBreadcrumbsTrail,
		/// <summary>
		/// A value (true/false) indicating whether to disable the page cache.
		/// </summary>
		DisableCache,
		/// <summary>
		/// A value (true/false) indicating whether to disable the captcha control in public functionalities.
		/// </summary>
		DisableCaptchaControl,
		/// <summary>
		/// A value (true/false) indicating whether to disable concurrent page editing.
		/// </summary>
		DisableConcurrentEditing,
		/// <summary>
		/// A value (true/false) indicating whether to enable HTTP compression.
		/// </summary>
		EnableHttpCompression,
		/// <summary>
		/// A value (true/false) indicating whether to enable View State compression.
		/// </summary>
		EnableViewStateCompression,
		/// <summary>
		/// The logging level.
		/// </summary>
		LoggingLevel,
		/// <summary>
		/// The max size for uploaded files (bytes).
		/// </summary>
		MaxFileSize,
		/// <summary>
		/// The extension used for pages.
		/// </summary>
		PageExtension,
		/// <summary>
		/// A value (true/false) indicating whether to SCRIPT tags are allowed in WikiMarkup.
		/// </summary>
		ScriptTagsAllowed,
		/// <summary>
		/// The version of the Wiki Engine.
		/// </summary>
		WikiVersion,
		/// <summary>
		/// The max size, in KB, of the log.
		/// </summary>
		MaxLogSize,
		/// <summary>
		/// The max number of recent changes to log.
		/// </summary>
		MaxRecentChanges,
		/// <summary>
		/// The size of the cache (# of pages).
		/// </summary>
		CacheSize,
		/// <summary>
		/// The # of pages to remove fromm the cache when it is full.
		/// </summary>
		CacheCutSize,
		/// <summary>
		/// The timeout, in seconds, after which a page editing session is considered to be dead.
		/// </summary>
		EditingSessionTimeout,
		/// <summary>
		/// The default administrators group.
		/// </summary>
		AdministratorsGroup,
		/// <summary>
		/// The default users group.
		/// </summary>
		UsersGroup,
		/// <summary>
		/// The default anonymous users group.
		/// </summary>
		AnonymousGroup,
		/// <summary>
		/// The page change moderation mode.
		/// </summary>
		ChangeModerationMode,
		/// <summary>
		/// The default pages provider.
		/// </summary>
		DefaultPagesStorageProvider,
		/// <summary>
		/// The default users provider.
		/// </summary>
		DefaultUsersStorageProvider,
		/// <summary>
		/// The default files provider.
		/// </summary>
		DefaultFilesStorageProvider,
		/// <summary>
		/// The default cache provider.
		/// </summary>
		DefaultCacheProvider,
		/// <summary>
		/// The name of the theme of the root namespace.
		/// </summary>
		RootNamespaceTheme,
		/// <summary>
		/// The number of items to display in lists of items before enabling paging.
		/// </summary>
		ListSize
	}

	/// <summary>
	/// Enumerates the parts of the cache that can be cleared.
	/// </summary>
	public enum CacheData {
		/// <summary>
		/// All the pages data.
		/// </summary>
		Pages,
		/// <summary>
		/// All the meta-files (Header, Footer, Sidebar, etc.).
		/// </summary>
		MetaFiles
	}

	/// <summary>
	/// Enumerates the toolbar items that can be added.
	/// </summary>
	public enum ToolbarItem {
		/// <summary>
		/// A Special Tag that is inserted in the text.
		/// </summary>
		SpecialTag,
		/// <summary>
		/// A special tag that wraps the selected text.
		/// </summary>
		SpecialTagWrap
	}

}
