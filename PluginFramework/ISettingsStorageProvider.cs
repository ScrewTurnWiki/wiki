
using System;
using System.Collections.Generic;
using System.Text;
using ScrewTurn.Wiki.AclEngine;

namespace ScrewTurn.Wiki.PluginFramework {

	/// <summary>
	/// The interface that must be implemented in order to create a custom Settings and Log Storage Provider for ScrewTurn Wiki.
	/// </summary>
	/// <remarks>A class that implements this interface <b>should</b> have some kind of data caching.
	/// The Provider <b>should not</b> use the method <see cref="M:IHost.GetSettingValue" /> during the execution of 
	/// the <see cref="M:ISettingsStorageProvider.Init" /> method, nor access the wiki log.</remarks>
	public interface ISettingsStorageProviderV30 : IProviderV30 {

		/// <summary>
		/// Retrieves the value of a Setting.
		/// </summary>
		/// <param name="name">The name of the Setting.</param>
		/// <returns>The value of the Setting, or <c>null</c>.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="name"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="name"/> is empty.</exception>
		string GetSetting(string name);

		/// <summary>
		/// Stores the value of a Setting.
		/// </summary>
		/// <param name="name">The name of the Setting.</param>
		/// <param name="value">The value of the Setting. Value cannot contain CR and LF characters, which will be removed.</param>
		/// <returns>True if the Setting is stored, false otherwise.</returns>
		/// <remarks>This method stores the Value immediately.</remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="name"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="name"/> is empty.</exception>
		bool SetSetting(string name, string value);

		/// <summary>
		/// Gets the all the setting values.
		/// </summary>
		/// <returns>All the settings.</returns>
		IDictionary<string, string> GetAllSettings();

		/// <summary>
		/// Starts a Bulk update of the Settings so that a bulk of settings can be set before storing them.
		/// </summary>
		void BeginBulkUpdate();

		/// <summary>
		/// Ends a Bulk update of the Settings and stores the settings.
		/// </summary>
		void EndBulkUpdate();

		/// <summary>
		/// Records a message to the System Log.
		/// </summary>
		/// <param name="message">The Log Message.</param>
		/// <param name="entryType">The Type of the Entry.</param>
		/// <param name="user">The User.</param>
		/// <remarks>This method <b>should not</b> write messages to the Log using the method IHost.LogEntry.
		/// This method should also never throw exceptions (except for parameter validation).</remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="message"/> or <paramref name="user"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="message"/> or <paramref name="user"/> are empty.</exception>
		void LogEntry(string message, EntryType entryType, string user);

		/// <summary>
		/// Gets all the Log Entries, sorted by date/time (oldest to newest).
		/// </summary>
		/// <returns>The Log Entries.</returns>
		LogEntry[] GetLogEntries();

		/// <summary>
		/// Clear the Log.
		/// </summary>
		void ClearLog();

		/// <summary>
		/// Gets the current size of the Log, in KB.
		/// </summary>
		int LogSize { get; }

		/// <summary>
		/// Gets a meta-data item's content.
		/// </summary>
		/// <param name="item">The item.</param>
		/// <param name="tag">The tag that specifies the context (usually the namespace).</param>
		/// <returns>The content.</returns>
		string GetMetaDataItem(MetaDataItem item, string tag);

		/// <summary>
		/// Sets a meta-data items' content.
		/// </summary>
		/// <param name="item">The item.</param>
		/// <param name="tag">The tag that specifies the context (usually the namespace).</param>
		/// <param name="content">The content.</param>
		/// <returns><c>true</c> if the content is set, <c>false</c> otherwise.</returns>
		bool SetMetaDataItem(MetaDataItem item, string tag, string content);

		/// <summary>
		/// Gets the recent changes of the Wiki.
		/// </summary>
		/// <returns>The recent Changes, oldest to newest.</returns>
		RecentChange[] GetRecentChanges();

		/// <summary>
		/// Adds a new change.
		/// </summary>
		/// <param name="page">The page name.</param>
		/// <param name="title">The page title.</param>
		/// <param name="messageSubject">The message subject (or <c>null</c>).</param>
		/// <param name="dateTime">The date/time.</param>
		/// <param name="user">The user.</param>
		/// <param name="change">The change.</param>
		/// <param name="descr">The description (optional).</param>
		/// <returns><c>true</c> if the change is saved, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="page"/>, <paramref name="title"/> or <paramref name="user"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="page"/>, <paramref name="title"/> or <paramref name="user"/> are empty.</exception>
		bool AddRecentChange(string page, string title, string messageSubject, DateTime dateTime, string user, Change change, string descr);

		/// <summary>
		/// Lists the stored plugin assemblies.
		/// </summary>
		/// <returns></returns>
		string[] ListPluginAssemblies();

		/// <summary>
		/// Stores a plugin's assembly, overwriting existing ones if present.
		/// </summary>
		/// <param name="filename">The file name of the assembly, such as "Assembly.dll".</param>
		/// <param name="assembly">The assembly content.</param>
		/// <returns><c>true</c> if the assembly is stored, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="filename"/> or <paramref name="assembly"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="filename"/> or <paramref name="assembly"/> are empty.</exception>
		bool StorePluginAssembly(string filename, byte[] assembly);

		/// <summary>
		/// Retrieves a plugin's assembly.
		/// </summary>
		/// <param name="filename">The file name of the assembly.</param>
		/// <returns>The assembly content, or <c>null</c>.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="filename"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="filename"/> is empty.</exception>
		byte[] RetrievePluginAssembly(string filename);

		/// <summary>
		/// Removes a plugin's assembly.
		/// </summary>
		/// <param name="filename">The file name of the assembly to remove, such as "Assembly.dll".</param>
		/// <returns><c>true</c> if the assembly is removed, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="filename"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="filename"/> is empty.</exception>
		bool DeletePluginAssembly(string filename);

		/// <summary>
		/// Sets the status of a plugin.
		/// </summary>
		/// <param name="typeName">The Type name of the plugin.</param>
		/// <param name="enabled">The plugin status.</param>
		/// <returns><c>true</c> if the status is stored, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="typeName"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="typeName"/> is empty.</exception>
		bool SetPluginStatus(string typeName, bool enabled);

		/// <summary>
		/// Gets the status of a plugin.
		/// </summary>
		/// <param name="typeName">The Type name of the plugin.</param>
		/// <returns>The status (<c>false</c> for disabled, <c>true</c> for enabled), or <c>true</c> if no status is found.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="typeName"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="typeName"/> is empty.</exception>
		bool GetPluginStatus(string typeName);

		/// <summary>
		/// Sets the configuration of a plugin.
		/// </summary>
		/// <param name="typeName">The Type name of the plugin.</param>
		/// <param name="config">The configuration.</param>
		/// <returns><c>true</c> if the configuration is stored, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="typeName"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="typeName"/> is empty.</exception>
		bool SetPluginConfiguration(string typeName, string config);

		/// <summary>
		/// Gets the configuration of a plugin.
		/// </summary>
		/// <param name="typeName">The Type name of the plugin.</param>
		/// <returns>The plugin configuration, or <b>String.Empty</b>.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="typeName"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="typeName"/> is empty.</exception>
		string GetPluginConfiguration(string typeName);

		/// <summary>
		/// Gets the ACL Manager instance.
		/// </summary>
		IAclManager AclManager { get; }

		/// <summary>
		/// Stores the outgoing links of a page, overwriting existing data.
		/// </summary>
		/// <param name="page">The full name of the page.</param>
		/// <param name="outgoingLinks">The full names of the pages that <b>page</b> links to.</param>
		/// <returns><c>true</c> if the outgoing links are stored, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="page"/> or <paramref name="outgoingLinks"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="page"/> or <paramref name="outgoingLinks"/> are empty.</exception>
		bool StoreOutgoingLinks(string page, string[] outgoingLinks);

		/// <summary>
		/// Gets the outgoing links of a page.
		/// </summary>
		/// <param name="page">The full name of the page.</param>
		/// <returns>The outgoing links.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="page"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="page"/> is empty.</exception>
		string[] GetOutgoingLinks(string page);

		/// <summary>
		/// Gets all the outgoing links stored.
		/// </summary>
		/// <returns>The outgoing links, in a dictionary in the form page->outgoing_links.</returns>
		IDictionary<string, string[]> GetAllOutgoingLinks();

		/// <summary>
		/// Deletes the outgoing links of a page and all the target links that include the page.
		/// </summary>
		/// <param name="page">The full name of the page.</param>
		/// <returns><c>true</c> if the links are deleted, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="page"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="page"/> is empty.</exception>
		bool DeleteOutgoingLinks(string page);

		/// <summary>
		/// Updates all outgoing links data for a page rename.
		/// </summary>
		/// <param name="oldName">The old page name.</param>
		/// <param name="newName">The new page name.</param>
		/// <returns><c>true</c> if the data is updated, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="oldName"/> or <paramref name="newName"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="oldName"/> or <paramref name="newName"/> are empty.</exception>
		bool UpdateOutgoingLinksForRename(string oldName, string newName);

		/// <summary>
		/// Determines whether the application was started for the first time.
		/// </summary>
		/// <returns><c>true</c> if the application was started for the first time, <c>false</c> otherwise.</returns>
		bool IsFirstApplicationStart();

	}

	/// <summary>
	/// Lists legal meta-data items (global items have a integer value greater than or equal to 100.
	/// </summary>
	public enum MetaDataItem {
		// Numbers < 100 -> global items
		// Numbers >= 100 -> namespace-specific items

		/// <summary>
		/// The account activation message which is sent to the newly registered users.
		/// </summary>
		AccountActivationMessage = 0,
		/// <summary>
		/// The password reset message which is sent to the users who reset their password.
		/// </summary>
		PasswordResetProcedureMessage = 7,
		/// <summary>
		/// The notice that appears and replaces the text in the Login page.
		/// </summary>
		LoginNotice = 2,
		/// <summary>
		/// The notice that appears and replaces the text in the Access Denied page.
		/// </summary>
		AccessDeniedNotice = 8,
		/// <summary>
		/// The message that is sent when a page is modified.
		/// </summary>
		PageChangeMessage = 3,
		/// <summary>
		/// The message that is sent when a new message is posted on a discussion.
		/// </summary>
		DiscussionChangeMessage = 4,
		/// <summary>
		/// The message sent when a page draft requires approval.
		/// </summary>
		ApproveDraftMessage = 5,
		/// <summary>
		/// The notice that appears and replates the text in the Register page.
		/// </summary>
		RegisterNotice = 6,

		/// <summary>
		/// The notice that appears in the editing page.
		/// </summary>
		EditNotice = 100,
		/// <summary>
		/// The wiki footer.
		/// </summary>
		Footer = 101,
		/// <summary>
		/// The wiki header.
		/// </summary>
		Header = 102,
		/// <summary>
		/// The custom content of the HTML Head tag.
		/// </summary>
		HtmlHead = 103,
		/// <summary>
		/// The pages footer.
		/// </summary>
		PageFooter = 104,
		/// <summary>
		/// The pages header.
		/// </summary>
		PageHeader = 105,
		/// <summary>
		/// The content of the sidebar.
		/// </summary>
		Sidebar = 106
	}

	/// <summary>
	/// Represents a Log Entry.
	/// </summary>
	public class LogEntry {

		private EntryType type;
		private DateTime dateTime;
		private string message;
		private string user;

		/// <summary>
		/// Initializes a new instance of the <b>LogEntry</b> class.
		/// </summary>
		/// <param name="type">The type of the Entry</param>
		/// <param name="dateTime">The DateTime.</param>
		/// <param name="message">The Message.</param>
		/// <param name="user">The User.</param>
		public LogEntry(EntryType type, DateTime dateTime, string message, string user) {
			this.type = type;
			this.dateTime = dateTime;
			this.message = message;
			this.user = user;
		}

		/// <summary>
		/// Gets the EntryType.
		/// </summary>
		public EntryType EntryType {
			get { return type; }
		}

		/// <summary>
		/// Gets the DateTime.
		/// </summary>
		public DateTime DateTime {
			get { return dateTime; }
		}

		/// <summary>
		/// Gets the Message.
		/// </summary>
		public string Message {
			get { return message; }
		}

		/// <summary>
		/// Gets the User.
		/// </summary>
		public string User {
			get { return user; }
		}

	}

	/// <summary>
	/// Enumerates the Types of Log Entries.
	/// </summary>
	public enum EntryType {
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
	/// Lists legal logging level values.
	/// </summary>
	public enum LoggingLevel {
		/// <summary>
		/// All messages are logged.
		/// </summary>
		AllMessages = 3,
		/// <summary>
		/// Warnings and errors are logged.
		/// </summary>
		WarningsAndErrors = 2,
		/// <summary>
		/// Errors only are logged.
		/// </summary>
		ErrorsOnly = 1,
		/// <summary>
		/// Logging is completely disabled.
		/// </summary>
		DisableLog = 0
	}

}
