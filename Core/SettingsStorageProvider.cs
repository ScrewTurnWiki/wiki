
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ScrewTurn.Wiki.PluginFramework;
using ScrewTurn.Wiki.AclEngine;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Implements a Settings Storage Provider against local text pluginAssemblies.
	/// </summary>
	public class SettingsStorageProvider : ProviderBase, ISettingsStorageProviderV30 {

		// Filenames: Settings, Log, RecentChanges, MetaData
		private const string ConfigFile = "Config.cs";
		private const string LogFile = "Log.cs";
		private const string RecentChangesFile = "RecentChanges3.cs"; // Old v2 format no more supported
		private const string AccountActivationMessageFile = "AccountActivationMessage.cs";
		private const string EditNoticeFile = "EditNotice.cs";
		private const string FooterFile = "Footer.cs";
		private const string HeaderFile = "Header.cs";
		private const string HtmlHeadFile = "HtmlHead.cs";
		private const string LoginNoticeFile = "LoginNotice.cs";
		private const string AccessDeniedNoticeFile = "AccessDeniedNotice.cs";
		private const string RegisterNoticeFile = "RegisterNotice.cs";
		private const string PageFooterFile = "PageFooter.cs";
		private const string PageHeaderFile = "PageHeader.cs";
		private const string PasswordResetProcedureMessageFile = "PasswordResetProcedureMessage.cs";
		private const string SidebarFile = "Sidebar.cs";
		private const string PageChangeMessageFile = "PageChangeMessage.cs";
		private const string DiscussionChangeMessageFile = "DiscussionChangeMessage.cs";
		private const string ApproveDraftMessageFile = "ApproveDraftMessage.cs";

		private const string PluginsDirectory = "Plugins";
		private const string PluginsStatusFile = "Status.cs";
		private const string PluginsConfigDirectory = "Config";

		private const string AclFile = "ACL.cs";

		private const string LinksFile = "Links.cs";

		private const int EstimatedLogEntrySize = 60; // bytes

		/// <summary>
		/// The name of the provider.
		/// </summary>
		public static readonly string ProviderName = "Local Settings Provider";

		private readonly ComponentInformation info =
			new ComponentInformation(ProviderName, "Threeplicate Srl", Settings.WikiVersion, "http://www.screwturn.eu", null);

		private IHostV30 host;

		private IAclManager aclManager;
		private AclStorerBase aclStorer;

		private bool bulkUpdating = false;
		private Dictionary<string, string> configData = null;

		private bool isFirstStart = false;

		private string GetFullPath(string name) {
			return Path.Combine(GetDataDirectory(host), name);
		}

		private string GetFullPathForPlugin(string name) {
			return Path.Combine(Path.Combine(GetDataDirectory(host), PluginsDirectory), name);
		}

		private string GetFullPathForPluginConfig(string name) {
			return Path.Combine(Path.Combine(Path.Combine(GetDataDirectory(host), PluginsDirectory), PluginsConfigDirectory), name);
		}

		/// <summary>
		/// Initializes the Storage Provider.
		/// </summary>
		/// <param name="host">The Host of the Component.</param>
		/// <param name="config">The Configuration data, if any.</param>
		/// <exception cref="ArgumentNullException">If <b>host</b> or <b>config</b> are <c>null</c>.</exception>
		/// <exception cref="InvalidConfigurationException">If <b>config</b> is not valid or is incorrect.</exception>
		public void Init(IHostV30 host, string config) {
			if(host == null) throw new ArgumentNullException("host");
			if(config == null) throw new ArgumentNullException("config");

			this.host = host;

			if(!LocalProvidersTools.CheckWritePermissions(GetDataDirectory(host))) {
				throw new InvalidConfigurationException("Cannot write into the public directory - check permissions");
			}

			// Create all needed pluginAssemblies
			if(!File.Exists(GetFullPath(LogFile))) {
				File.Create(GetFullPath(LogFile)).Close();
			}

			if(!File.Exists(GetFullPath(ConfigFile))) {
				File.Create(GetFullPath(ConfigFile)).Close();
				isFirstStart = true;
			}

			if(!File.Exists(GetFullPath(RecentChangesFile))) {
				File.Create(GetFullPath(RecentChangesFile)).Close();
			}

			if(!File.Exists(GetFullPath(HtmlHeadFile))) {
				File.Create(GetFullPath(HtmlHeadFile)).Close();
			}

			if(!File.Exists(GetFullPath(HeaderFile))) {
				File.Create(GetFullPath(HeaderFile)).Close();
			}

			if(!File.Exists(GetFullPath(SidebarFile))) {
				File.Create(GetFullPath(SidebarFile)).Close();
			}

			if(!File.Exists(GetFullPath(FooterFile))) {
				File.Create(GetFullPath(FooterFile)).Close();
			}

			if(!File.Exists(GetFullPath(PageHeaderFile))) {
				File.Create(GetFullPath(PageHeaderFile)).Close();
			}

			if(!File.Exists(GetFullPath(PageFooterFile))) {
				File.Create(GetFullPath(PageFooterFile)).Close();
			}

			if(!File.Exists(GetFullPath(AccountActivationMessageFile))) {
				File.Create(GetFullPath(AccountActivationMessageFile)).Close();
			}

			if(!File.Exists(GetFullPath(PasswordResetProcedureMessageFile))) {
				File.Create(GetFullPath(PasswordResetProcedureMessageFile)).Close();
			}

			if(!File.Exists(GetFullPath(EditNoticeFile))) {
				File.Create(GetFullPath(EditNoticeFile)).Close();
			}

			if(!File.Exists(GetFullPath(LoginNoticeFile))) {
				File.Create(GetFullPath(LoginNoticeFile)).Close();
			}

			if(!File.Exists(GetFullPath(AccessDeniedNoticeFile))) {
				File.Create(GetFullPath(AccessDeniedNoticeFile)).Close();
			}

			if(!File.Exists(GetFullPath(RegisterNoticeFile))) {
				File.Create(GetFullPath(RegisterNoticeFile)).Close();
			}

			if(!File.Exists(GetFullPath(PageChangeMessageFile))) {
				File.Create(GetFullPath(PageChangeMessageFile)).Close();
			}

			if(!File.Exists(GetFullPath(DiscussionChangeMessageFile))) {
				File.Create(GetFullPath(DiscussionChangeMessageFile)).Close();
			}

			if(!File.Exists(GetFullPath(ApproveDraftMessageFile))) {
				File.Create(GetFullPath(ApproveDraftMessageFile)).Close();
			}

			if(!Directory.Exists(GetFullPath(PluginsDirectory))) {
				Directory.CreateDirectory(GetFullPath(PluginsDirectory));
			}

			if(!Directory.Exists(GetFullPathForPlugin(PluginsConfigDirectory))) {
				Directory.CreateDirectory(GetFullPathForPlugin(PluginsConfigDirectory));
			}

			if(!File.Exists(GetFullPathForPlugin(PluginsStatusFile))) {
				File.Create(GetFullPathForPlugin(PluginsStatusFile)).Close();
			}

			if(!File.Exists(GetFullPath(LinksFile))) {
				File.Create(GetFullPath(LinksFile)).Close();
			}

			LoadConfig();

			// Initialize ACL Manager and Storer
			aclManager = new StandardAclManager();
			aclStorer = new AclStorer(aclManager, GetFullPath(AclFile));
			aclStorer.LoadData();
		}

		/// <summary>
		/// Method invoked on shutdown.
		/// </summary>
		/// <remarks>This method might not be invoked in some cases.</remarks>
		public void Shutdown() {
			lock(this) {
				aclStorer.Dispose();
			}
		}

		/// <summary>
		/// Gets the Information about the Provider.
		/// </summary>
		public ComponentInformation Information {
			get { return info; }
		}

		/// <summary>
		/// Gets a brief summary of the configuration string format, in HTML. Returns <c>null</c> if no configuration is needed.
		/// </summary>
		public string ConfigHelpHtml {
			get { return null; }
		}

		/// <summary>
		/// Retrieves the value of a Setting.
		/// </summary>
		/// <param name="name">The name of the Setting.</param>
		/// <returns>The value of the Setting, or null.</returns>
		/// <exception cref="ArgumentNullException">If <b>name</b> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>name</b> is empty.</exception>
		public string GetSetting(string name) {
			if(name == null) throw new ArgumentNullException("name");
			if(name.Length == 0) throw new ArgumentException("Name cannot be empty", "name");

			lock(this) {
				string val = null;
				if(configData.TryGetValue(name, out val)) return val;
				else return null;
			}
		}

		/// <summary>
		/// Stores the value of a Setting.
		/// </summary>
		/// <param name="name">The name of the Setting.</param>
		/// <param name="value">The value of the Setting. Value cannot contain CR and LF characters, which will be removed.</param>
		/// <returns>True if the Setting is stored, false otherwise.</returns>
		/// <remarks>This method stores the Value immediately.</remarks>
		/// <exception cref="ArgumentNullException">If <b>name</b> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>name</b> is empty.</exception>
		public bool SetSetting(string name, string value) {
			if(name == null) throw new ArgumentNullException("name");
			if(name.Length == 0) throw new ArgumentException("Name cannot be empty", "name");

			// Nulls are converted to empty strings
			if(value == null) value = "";

			// Store, then if not bulkUpdating, dump
			lock(this) {
				configData[name] = value;
				if(!bulkUpdating) DumpConfig();

				return true;
			}
		}

		/// <summary>
		/// Gets the all the setting values.
		/// </summary>
		/// <returns>All the settings.</returns>
		public IDictionary<string, string> GetAllSettings() {
			lock(this) {
				Dictionary<string, string> result = new Dictionary<string, string>(configData.Count);
				foreach(KeyValuePair<string, string> pair in configData) {
					result.Add(pair.Key, pair.Value);
				}
				return result;
			}
		}

		/// <summary>
		/// Loads configuration settings from disk.
		/// </summary>
		private void LoadConfig() {
			// This method should not call Log.*(...)
			lock(this) {
				configData = new Dictionary<string, string>(30);
				string data = File.ReadAllText(GetFullPath(ConfigFile), System.Text.UTF8Encoding.UTF8);

				data = data.Replace("\r", "");

				string[] lines = data.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

				string[] fields;
				for(int i = 0; i < lines.Length; i++) {
					lines[i] = lines[i].Trim();
					
					// Skip comments
					if(lines[i].StartsWith("#")) continue;

					fields = new string[2];
					int idx = lines[i].IndexOf("=");
					if(idx < 0) continue;

					try {
						// Extract key
						fields[0] = lines[i].Substring(0, idx).Trim();
					}
					catch {
						// Unexpected string format
						continue;
					}

					try {
						// Extract value
						fields[1] = lines[i].Substring(idx + 1).Trim();
					}
					catch {
						// Blank/invalid value?
						fields[1] = "";
					}

					configData.Add(fields[0], fields[1]);
				}
			}
		}

		/// <summary>
		/// Dumps settings on disk.
		/// </summary>
		private void DumpConfig() {
			lock(this) {
				StringBuilder buffer = new StringBuilder(4096);

				string[] keys = new string[configData.Keys.Count];
				configData.Keys.CopyTo(keys, 0);
				for(int i = 0; i < keys.Length; i++) {
					buffer.AppendFormat("{0} = {1}\r\n", keys[i], configData[keys[i]]);
				}

				File.WriteAllText(GetFullPath(ConfigFile), buffer.ToString());
			}
		}

		/// <summary>
		/// Starts a Bulk update of the Settings so that a bulk of settings can be set before storing them.
		/// </summary>
		public void BeginBulkUpdate() {
			lock(this) {
				bulkUpdating = true;
			}
		}

		/// <summary>
		/// Ends a Bulk update of the Settings and stores the settings.
		/// </summary>
		public void EndBulkUpdate() {
			lock(this) {
				bulkUpdating = false;
				DumpConfig();
			}
		}

		/// <summary>
		/// Sanitizes a stiring from all unfriendly characters.
		/// </summary>
		/// <param name="input">The input string.</param>
		/// <returns>The sanitized result.</returns>
		private static string Sanitize(string input) {
			StringBuilder sb = new StringBuilder(input);
			sb.Replace("|", "{PIPE}");
			sb.Replace("\r", "");
			sb.Replace("\n", "{BR}");
			sb.Replace("<", "&lt;");
			sb.Replace(">", "&gt;");
			return sb.ToString();
		}

		/// <summary>
		/// Re-sanitizes a string from all unfriendly characters.
		/// </summary>
		/// <param name="input">The input string.</param>
		/// <returns>The sanitized result.</returns>
		private static string Resanitize(string input) {
			StringBuilder sb = new StringBuilder(input);
			sb.Replace("<", "&lt;");
			sb.Replace(">", "&gt;");
			sb.Replace("{BR}", "\n");
			sb.Replace("{PIPE}", "|");
			return sb.ToString();
		}

		/// <summary>
		/// Converts an <see cref="T:EntryType" /> to a string.
		/// </summary>
		/// <param name="type">The entry type.</param>
		/// <returns>The corresponding string.</returns>
		private static string EntryTypeToString(EntryType type) {
			switch(type) {
				case EntryType.General:
					return "G";
				case EntryType.Warning:
					return "W";
				case EntryType.Error:
					return "E";
				default:
					return "G";
			}
		}

		/// <summary>
		/// Converts an entry type string to an <see cref="T:EntryType" />.
		/// </summary>
		/// <param name="value">The string.</param>
		/// <returns>The <see cref="T:EntryType" />.</returns>
		private static EntryType EntryTypeParse(string value) {
			switch(value) {
				case "G":
					return EntryType.General;
				case "W":
					return EntryType.Warning;
				case "E":
					return EntryType.Error;
				default:
					return EntryType.General;
			}
		}

		/// <summary>
		/// Records a message to the System Log.
		/// </summary>
		/// <param name="message">The Log Message.</param>
		/// <param name="entryType">The Type of the Entry.</param>
		/// <param name="user">The User.</param>
		/// <remarks>This method <b>should not</b> write messages to the Log using the method IHost.LogEntry.
		/// This method should also never throw exceptions (except for parameter validation).</remarks>
		/// <exception cref="ArgumentNullException">If <b>message</b> or <b>user</b> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>message</b> or <b>user</b> are empty.</exception>
		public void LogEntry(string message, EntryType entryType, string user) {
			if(message == null) throw new ArgumentNullException("message");
			if(message.Length == 0) throw new ArgumentException("Message cannot be empty", "message");
			if(user == null) throw new ArgumentNullException("user");
			if(user.Length == 0) throw new ArgumentException("User cannot be empty", "user");

			lock(this) {
				message = Sanitize(message);
				user = Sanitize(user);
				LoggingLevel level = (LoggingLevel)Enum.Parse(typeof(LoggingLevel), host.GetSettingValue(SettingName.LoggingLevel));
				switch(level) {
					case LoggingLevel.AllMessages:
						break;
					case LoggingLevel.WarningsAndErrors:
						if(entryType != EntryType.Error && entryType != EntryType.Warning) return;
						break;
					case LoggingLevel.ErrorsOnly:
						if(entryType != EntryType.Error) return;
						break;
					case LoggingLevel.DisableLog:
						return;
					default:
						break;
				}

				FileStream fs = null;
				try {
					fs = new FileStream(GetFullPath(LogFile), FileMode.Append, FileAccess.Write, FileShare.None);
				}
				catch {
					return;
				}

				StreamWriter sw = new StreamWriter(fs, System.Text.UTF8Encoding.UTF8);
				// Type | DateTime | Message | User
				try {
					sw.Write(EntryTypeToString(entryType) + "|" + string.Format("{0:yyyy'/'MM'/'dd' 'HH':'mm':'ss}", DateTime.Now) + "|" + message + "|" + user + "\r\n");
				}
				catch { }
				finally {
					try {
						sw.Close();
					}
					catch { }
				}

				try {
					FileInfo fi = new FileInfo(GetFullPath(LogFile));
					if(fi.Length > (long)(int.Parse(host.GetSettingValue(SettingName.MaxLogSize)) * 1024)) {
						CutLog((int)(fi.Length * 0.75));
					}
				}
				catch { }
			}
		}

		/// <summary>
		/// Reduces the size of the Log to the specified size (or less).
		/// </summary>
		/// <param name="size">The size to shrink the log to (in bytes).</param>
		private void CutLog(int size) {
			lock(this) {
				// Contains the log messages from oldest to newest, and reverse the list
				List<LogEntry> entries = new List<LogEntry>(GetLogEntries());
				entries.Reverse();

				FileInfo fi = new FileInfo(GetFullPath(LogFile));
				int difference = (int)(fi.Length - size);
				int removeEntries = difference / EstimatedLogEntrySize * 2; // Double the number of removed entries in order to reduce the # of times Cut is needed
				int preserve = entries.Count - removeEntries; // The number of entries to be preserved

				// Copy the entries to preserve in a temp list
				List<LogEntry> toStore = new List<LogEntry>();
				for(int i = 0; i < preserve; i++) {
					toStore.Add(entries[i]);
				}

				toStore.Sort((a, b) => a.DateTime.CompareTo(b.DateTime));

				StringBuilder sb = new StringBuilder();
				// Type | DateTime | Message | User
				foreach(LogEntry e in toStore) {
					sb.Append(EntryTypeToString(e.EntryType));
					sb.Append("|");
					sb.Append(e.DateTime.ToString("yyyy'/'MM'/'dd' 'HH':'mm':'ss"));
					sb.Append("|");
					sb.Append(e.Message);
					sb.Append("|");
					sb.Append(e.User);
					sb.Append("\r\n");
				}

				FileStream fs = null;
				try {
					fs = new FileStream(GetFullPath(LogFile), FileMode.Create, FileAccess.Write, FileShare.None);
				}
				catch(Exception ex) {
					throw new IOException("Unable to open the file: " + LogFile, ex);
				}

				StreamWriter sw = new StreamWriter(fs, System.Text.UTF8Encoding.UTF8);
				// Type | DateTime | Message | User
				try {
					sw.Write(sb.ToString());
				}
				catch { }
				sw.Close();
			}
		}

		/// <summary>
		/// Gets all the Log Entries, sorted by date/time (oldest to newest).
		/// </summary>
		/// <remarks>The Log Entries.</remarks>
		public LogEntry[] GetLogEntries() {
			lock(this) {
				string content = File.ReadAllText(GetFullPath(LogFile)).Replace("\r", "");
				List<LogEntry> result = new List<LogEntry>(50);
				string[] lines = content.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
				string[] fields;
				for(int i = 0; i < lines.Length; i++) {
					fields = lines[i].Split('|');
					try {
						// Try/catch to avoid problems with corrupted file (raw method)
						result.Add(new LogEntry(EntryTypeParse(fields[0]), DateTime.Parse(fields[1]), Resanitize(fields[2]), Resanitize(fields[3])));
					}
					catch { }
				}

				result.Sort((a, b) => a.DateTime.CompareTo(b.DateTime));

				return result.ToArray();
			}
		}

		/// <summary>
		/// Clear the Log.
		/// </summary>
		public void ClearLog() {
			lock(this) {
				FileStream fs = null;
				try {
					fs = new FileStream(GetFullPath(LogFile), FileMode.Create, FileAccess.Write, FileShare.None);
				}
				catch(Exception ex) {
					throw new IOException("Unable to access the file: " + LogFile, ex);
				}
				fs.Close();
			}
		}

		/// <summary>
		/// Gets the current size of the Log, in KB.
		/// </summary>
		public int LogSize {
			get {
				lock(this) {
					FileInfo fi = new FileInfo(GetFullPath(LogFile));
					return (int)(fi.Length / 1024);
				}
			}
		}

		/// <summary>
		/// Builds the full path for a meta-data item file.
		/// </summary>
		/// <param name="tag">The tag that specifies the context.</param>
		/// <param name="file">The file.</param>
		/// <returns>The full path.</returns>
		private string GetFullPathForMetaDataItem(string tag, string file) {
			string targetFile = (!string.IsNullOrEmpty(tag) ? tag + "." : "") + file;
			return GetFullPath(targetFile);
		}

		private static readonly Dictionary<MetaDataItem, string> MetaDataItemFiles = new Dictionary<MetaDataItem, string>() {
			{ MetaDataItem.AccountActivationMessage, AccountActivationMessageFile },
			{ MetaDataItem.EditNotice, EditNoticeFile },
			{ MetaDataItem.Footer, FooterFile },
			{ MetaDataItem.Header, HeaderFile },
			{ MetaDataItem.HtmlHead, HtmlHeadFile },
			{ MetaDataItem.LoginNotice, LoginNoticeFile },
			{ MetaDataItem.AccessDeniedNotice, AccessDeniedNoticeFile },
			{ MetaDataItem.RegisterNotice, RegisterNoticeFile },
			{ MetaDataItem.PageFooter, PageFooterFile },
			{ MetaDataItem.PageHeader, PageHeaderFile },
			{ MetaDataItem.PasswordResetProcedureMessage, PasswordResetProcedureMessageFile },
			{ MetaDataItem.Sidebar, SidebarFile },
			{ MetaDataItem.PageChangeMessage, PageChangeMessageFile },
			{ MetaDataItem.DiscussionChangeMessage, DiscussionChangeMessageFile },
			{ MetaDataItem.ApproveDraftMessage, ApproveDraftMessageFile }
		};

		/// <summary>
		/// Gets a meta-data item's content.
		/// </summary>
		/// <param name="item">The item.</param>
		/// <param name="tag">The tag that specifies the context (usually the namespace).</param>
		/// <returns>The content.</returns>
		public string GetMetaDataItem(MetaDataItem item, string tag) {
			lock(this) {
				string fullFile = GetFullPathForMetaDataItem(tag, MetaDataItemFiles[item]);
				if(!File.Exists(fullFile)) return "";
				else return File.ReadAllText(fullFile);
			}
		}

		/// <summary>
		/// Sets a meta-data items' content.
		/// </summary>
		/// <param name="item">The item.</param>
		/// <param name="tag">The tag that specifies the context (usually the namespace).</param>
		/// <param name="content">The content.</param>
		/// <returns><c>true</c> if the content is set, <c>false</c> otherwise.</returns>
		public bool SetMetaDataItem(MetaDataItem item, string tag, string content) {
			if(content == null) content = "";

			lock(this) {
				File.WriteAllText(GetFullPathForMetaDataItem(tag, MetaDataItemFiles[item]), content);
				return true;
			}
		}

		/// <summary>
		/// Gets the change from a string.
		/// </summary>
		/// <param name="change">The string.</param>
		/// <returns>The change.</returns>
		private static ScrewTurn.Wiki.PluginFramework.Change GetChange(string change) {
			switch(change.ToUpperInvariant()) {
				case "U":
					return ScrewTurn.Wiki.PluginFramework.Change.PageUpdated;
				case "D":
					return ScrewTurn.Wiki.PluginFramework.Change.PageDeleted;
				case "R":
					return ScrewTurn.Wiki.PluginFramework.Change.PageRolledBack;
				case "N":
					return ScrewTurn.Wiki.PluginFramework.Change.PageRenamed;
				case "MP":
					return ScrewTurn.Wiki.PluginFramework.Change.MessagePosted;
				case "ME":
					return ScrewTurn.Wiki.PluginFramework.Change.MessageEdited;
				case "MD":
					return ScrewTurn.Wiki.PluginFramework.Change.MessageDeleted;
				default:
					throw new NotSupportedException();
			}
		}

		/// <summary>
		/// Gets the change string for a change.
		/// </summary>
		/// <param name="change">The change.</param>
		/// <returns>The change string.</returns>
		private static string GetChangeString(ScrewTurn.Wiki.PluginFramework.Change change) {
			switch(change) {
				case ScrewTurn.Wiki.PluginFramework.Change.PageUpdated:
					return "U";
				case ScrewTurn.Wiki.PluginFramework.Change.PageDeleted:
					return "D";
				case ScrewTurn.Wiki.PluginFramework.Change.PageRolledBack:
					return "R";
				case ScrewTurn.Wiki.PluginFramework.Change.PageRenamed:
					return "N";
				case ScrewTurn.Wiki.PluginFramework.Change.MessagePosted:
					return "MP";
				case ScrewTurn.Wiki.PluginFramework.Change.MessageEdited:
					return "ME";
				case ScrewTurn.Wiki.PluginFramework.Change.MessageDeleted:
					return "MD";
				default:
					throw new NotSupportedException();
			}
		}

		/// <summary>
		/// Gets the recent changes of the Wiki.
		/// </summary>
		/// <returns>The recent Changes (oldest to newest).</returns>
		public RecentChange[] GetRecentChanges() {
			lock(this) {
				// Load from file
				string data = File.ReadAllText(GetFullPath(RecentChangesFile)).Replace("\r", "");
				string[] lines = data.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

				List<RecentChange> changes = new List<RecentChange>(lines.Length);

				string[] fields;
				for(int i = 0; i < lines.Length; i++) {
					try {
						// Try/catch to avoid problems for corrupted file (raw method)
						fields = lines[i].Split('|');
						ScrewTurn.Wiki.PluginFramework.Change c = GetChange(fields[5]);
						changes.Add(new RecentChange(Tools.UnescapeString(fields[0]), Tools.UnescapeString(fields[1]), Tools.UnescapeString(fields[2]),
							DateTime.Parse(fields[3]), Tools.UnescapeString(fields[4]), c, Tools.UnescapeString(fields[6])));
					}
					catch { }
				}

				changes.Sort((x, y) => { return x.DateTime.CompareTo(y.DateTime); });

				return changes.ToArray();
			}
		}

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
		/// <exception cref="ArgumentNullException">If <b>page</b>, <b>title</b> or <b>user</b> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>page</b>, <b>title</b> or <b>user</b> are empty.</exception>
		public bool AddRecentChange(string page, string title, string messageSubject, DateTime dateTime, string user,
			ScrewTurn.Wiki.PluginFramework.Change change, string descr) {

			if(page == null) throw new ArgumentNullException("page");
			if(page.Length == 0) throw new ArgumentException("Page cannot be empty", "page");
			if(title == null) throw new ArgumentNullException("title");
			if(title.Length == 0) throw new ArgumentException("Title cannot be empty", "title");
			if(user == null) throw new ArgumentNullException("user");
			if(user.Length == 0) throw new ArgumentException("User cannot be empty", "user");

			if(messageSubject == null) messageSubject = "";
			if(descr == null) descr = "";

			lock(this) {
				StringBuilder sb = new StringBuilder(100);
				sb.Append(Tools.EscapeString(page));
				sb.Append("|");
				sb.Append(Tools.EscapeString(title));
				sb.Append("|");
				sb.Append(Tools.EscapeString(messageSubject));
				sb.Append("|");
				sb.Append(dateTime.ToString("yyyy'/'MM'/'dd' 'HH':'mm':'ss"));
				sb.Append("|");
				sb.Append(Tools.EscapeString(user));
				sb.Append("|");
				sb.Append(GetChangeString(change));
				sb.Append("|");
				sb.Append(Tools.EscapeString(descr));
				sb.Append("\r\n");
				File.AppendAllText(GetFullPath(RecentChangesFile), sb.ToString());

				// Delete old changes, if needed
				int max = int.Parse(host.GetSettingValue(SettingName.MaxRecentChanges));
				if(GetRecentChanges().Length > max) CutRecentChanges((int)(max * 0.90));

				return true;
			}
		}

		/// <summary>
		/// Reduces the size of the recent changes file to the specified size, deleting old entries.
		/// </summary>
		/// <param name="size">The new Size.</param>
		private void CutRecentChanges(int size) {
			lock(this) {
				List<RecentChange> changes = new List<RecentChange>(GetRecentChanges());
				if(size >= changes.Count) return;

				int idx = changes.Count - size + 1;

				StringBuilder sb = new StringBuilder();

				for(int i = idx; i < changes.Count; i++) {
					sb.Append(Tools.EscapeString(changes[i].Page));
					sb.Append("|");
					sb.Append(Tools.EscapeString(changes[i].Title));
					sb.Append("|");
					sb.Append(Tools.EscapeString(changes[i].MessageSubject));
					sb.Append("|");
					sb.Append(changes[i].DateTime.ToString("yyyy'/'MM'/'dd' 'HH':'mm':'ss"));
					sb.Append("|");
					sb.Append(Tools.EscapeString(changes[i].User));
					sb.Append("|");
					sb.Append(GetChangeString(changes[i].Change));
					sb.Append("|");
					sb.Append(Tools.EscapeString(changes[i].Description));
					sb.Append("\r\n");
				}
				File.WriteAllText(GetFullPath(RecentChangesFile), sb.ToString());
			}
		}

		/// <summary>
		/// Lists the stored plugin assemblies.
		/// </summary>
		/// <returns></returns>
		public string[] ListPluginAssemblies() {
			lock(this) {
				string[] files = Directory.GetFiles(GetFullPath(PluginsDirectory), "*.dll");
				string[] result = new string[files.Length];
				for(int i = 0; i < files.Length; i++) result[i] = Path.GetFileName(files[i]);
				return result;
			}
		}

		/// <summary>
		/// Stores a plugin's assembly, overwriting existing ones if present.
		/// </summary>
		/// <param name="filename">The file name of the assembly, such as "Assembly.dll".</param>
		/// <param name="assembly">The assembly content.</param>
		/// <returns><c>true</c> if the assembly is stored, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <b>filename</b> or <b>assembly</b> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>filename</b> or <b>assembly</b> are empty.</exception>
		public bool StorePluginAssembly(string filename, byte[] assembly) {
			if(filename == null) throw new ArgumentNullException("filename");
			if(filename.Length == 0) throw new ArgumentException("Filename cannot be empty", "filename");
			if(assembly == null) throw new ArgumentNullException("assembly");
			if(assembly.Length == 0) throw new ArgumentException("Assembly cannot be empty", "assembly");

			lock(this) {
				try {
					File.WriteAllBytes(GetFullPathForPlugin(filename), assembly);
				}
				catch(IOException) {
					return false;
				}
				return true;
			}
		}

		/// <summary>
		/// Retrieves a plugin's assembly.
		/// </summary>
		/// <param name="filename">The file name of the assembly.</param>
		/// <returns>The assembly content, or <c>null</c>.</returns>
		/// <exception cref="ArgumentNullException">If <b>filename</b> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>filename</b> is empty.</exception>
		public byte[] RetrievePluginAssembly(string filename) {
			if(filename == null) throw new ArgumentNullException("filename");
			if(filename.Length == 0) throw new ArgumentException("Filename cannot be empty", "filename");

			if(!File.Exists(GetFullPathForPlugin(filename))) return null;

			lock(this) {
				try {
					return File.ReadAllBytes(GetFullPathForPlugin(filename));
				}
				catch(IOException) {
					return null;
				}
			}
		}

		/// <summary>
		/// Removes a plugin's assembly.
		/// </summary>
		/// <param name="filename">The file name of the assembly to remove, such as "Assembly.dll".</param>
		/// <returns><c>true</c> if the assembly is removed, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <b>filename</b> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>filename</b> is empty.</exception>
		public bool DeletePluginAssembly(string filename) {
			if(filename == null) throw new ArgumentNullException(filename);
			if(filename.Length == 0) throw new ArgumentException("Filename cannot be empty", "filename");

			lock(this) {
				string fullName = GetFullPathForPlugin(filename);
				if(!File.Exists(fullName)) return false;
				try {
					File.Delete(fullName);
					return true;
				}
				catch(IOException) {
					return false;
				}
			}
		}

		/// <summary>
		/// Sets the status of a plugin.
		/// </summary>
		/// <param name="typeName">The Type name of the plugin.</param>
		/// <param name="enabled">The plugin status.</param>
		/// <returns><c>true</c> if the status is stored, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <b>typeName</b> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>typeName</b> is empty.</exception>
		public bool SetPluginStatus(string typeName, bool enabled) {
			if(typeName == null) throw new ArgumentNullException("typeName");
			if(typeName.Length == 0) throw new ArgumentException("Type Name cannot be empty", "typeName");

			lock(this) {
				string data = File.ReadAllText(GetFullPathForPlugin(PluginsStatusFile)).Replace("\r", "");
				string[] lines = data.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
				int idx = -1;
				for(int i = 0; i < lines.Length; i++) {
					if(lines[i].Equals(typeName)) {
						idx = i;
						break;
					}
				}
				if(enabled) {
					if(idx >= 0) {
						StringBuilder sb = new StringBuilder(200);
						for(int i = 0; i < lines.Length; i++) {
							if(i != idx) sb.Append(lines[i] + "\r\n");
						}
						File.WriteAllText(GetFullPathForPlugin(PluginsStatusFile), sb.ToString());
					}
					// Else nothing to do
				}
				else {
					if(idx == -1) {
						StringBuilder sb = new StringBuilder(200);
						for(int i = 0; i < lines.Length; i++) {
							if(i != idx) sb.Append(lines[i] + "\r\n");
						}
						sb.Append(typeName + "\r\n");
						File.WriteAllText(GetFullPathForPlugin(PluginsStatusFile), sb.ToString());
					}
					// Else nothing to do
				}
			}
			return true;
		}

		/// <summary>
		/// Gets the status of a plugin.
		/// </summary>
		/// <param name="typeName">The Type name of the plugin.</param>
		/// <returns>The status (<c>true</c> for enabled, <c>false</c> for disabled), or <c>true</c> if no status is found.</returns>
		/// <exception cref="ArgumentNullException">If <b>typeName</b> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>typeName</b> is empty.</exception>
		public bool GetPluginStatus(string typeName) {
			if(typeName == null) throw new ArgumentNullException("typeName");
			if(typeName.Length == 0) throw new ArgumentException("Type Name cannot be empty", "typeName");

			lock(this) {
				string data = File.ReadAllText(GetFullPathForPlugin(PluginsStatusFile)).Replace("\r", "");
				string[] lines = data.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
				for(int i = 0; i < lines.Length; i++) {
					if(lines[i].Equals(typeName)) return false;
				}
				return true;
			}
		}

		/// <summary>
		/// Sets the configuration of a plugin.
		/// </summary>
		/// <param name="typeName">The Type name of the plugin.</param>
		/// <param name="config">The configuration.</param>
		/// <returns><c>true</c> if the configuration is stored, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <b>typeName</b> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>typeName</b> is empty.</exception>
		public bool SetPluginConfiguration(string typeName, string config) {
			if(typeName == null) throw new ArgumentNullException("typeName");
			if(typeName.Length == 0) throw new ArgumentException("Type Name cannot be empty", "typeName");

			lock(this) {
				try {
					File.WriteAllText(GetFullPathForPluginConfig(typeName + ".cs"), config != null ? config : "");
					return true;
				}
				catch(IOException) {
					return false;
				}
			}
		}

		/// <summary>
		/// Gets the configuration of a plugin.
		/// </summary>
		/// <param name="typeName">The Type name of the plugin.</param>
		/// <returns>The plugin configuration, or <b>String.Empty</b>.</returns>
		/// <exception cref="ArgumentNullException">If <b>typeName</b> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>typeName</b> is empty.</exception>
		public string GetPluginConfiguration(string typeName) {
			if(typeName == null) throw new ArgumentNullException("typeName");
			if(typeName.Length == 0) throw new ArgumentException("Type Name cannot be empty", "typeName");

			lock(this) {
				string file = GetFullPathForPluginConfig(typeName + ".cs");
				if(!File.Exists(file)) return "";
				else {
					try {
						return File.ReadAllText(file);
					}
					catch(IOException) {
						return "";
					}
				}
			}
		}

		/// <summary>
		/// Gets the ACL Manager instance.
		/// </summary>
		public IAclManager AclManager {
			get {
				lock(this) {
					return aclManager;
				}
			}
		}

		/// <summary>
		/// Stores the outgoing links of a page, overwriting existing data.
		/// </summary>
		/// <param name="page">The full name of the page.</param>
		/// <param name="outgoingLinks">The full names of the pages that <b>page</b> links to.</param>
		/// <returns><c>true</c> if the outgoing links are stored, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <b>page</b> or <b>outgoingLinks</b> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>page</b> or <b>outgoingLinks</b> are empty.</exception>
		public bool StoreOutgoingLinks(string page, string[] outgoingLinks) {
			if(page == null) throw new ArgumentNullException("page");
			if(page.Length == 0) throw new ArgumentException("Page cannot be empty", "page");
			if(outgoingLinks == null) throw new ArgumentNullException("outgoingLinks");

			lock(this) {
				// Step 1: remove old values
				string[] lines = File.ReadAllLines(GetFullPath(LinksFile));

				StringBuilder sb = new StringBuilder(lines.Length * 100);

				string testString = page + "|";

				foreach(string line in lines) {
					if(!line.StartsWith(testString)) {
						sb.Append(line);
						sb.Append("\r\n");
					}
				}

				lines = null;

				// Step 2: add new values
				sb.Append(page);
				sb.Append("|");

				for(int i = 0; i < outgoingLinks.Length; i++) {
					if(outgoingLinks[i] == null) throw new ArgumentNullException("outgoingLinks", "Null element in outgoing links array");
					if(outgoingLinks[i].Length == 0) throw new ArgumentException("Elements in outgoing links cannot be empty", "outgoingLinks");

					sb.Append(outgoingLinks[i]);
					if(i != outgoingLinks.Length - 1) sb.Append("|");
				}
				sb.Append("\r\n");

				File.WriteAllText(GetFullPath(LinksFile), sb.ToString());
			}

			return true;
		}

		/// <summary>
		/// Gets the outgoing links of a page.
		/// </summary>
		/// <param name="page">The full name of the page.</param>
		/// <returns>The outgoing links.</returns>
		/// <exception cref="ArgumentNullException">If <b>page</b> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>page</b> is empty.</exception>
		public string[] GetOutgoingLinks(string page) {
			if(page == null) throw new ArgumentNullException("page");
			if(page.Length == 0) throw new ArgumentException("Page cannot be empty", "page");

			lock(this) {
				string[] lines = File.ReadAllLines(GetFullPath(LinksFile));

				string testString = page + "|";

				foreach(string line in lines) {
					if(line.StartsWith(testString)) {
						string[] fields = line.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

						string[] result = new string[fields.Length - 1];
						Array.Copy(fields, 1, result, 0, result.Length);

						return result;
					}
				}
			}

			// Nothing found, return empty array
			return new string[0];
		}

		/// <summary>
		/// Gets all the outgoing links stored.
		/// </summary>
		/// <returns>The outgoing links, in a dictionary in the form page->outgoing_links.</returns>
		public IDictionary<string, string[]> GetAllOutgoingLinks() {
			lock(this) {
				string[] lines = File.ReadAllLines(GetFullPath(LinksFile));

				Dictionary<string, string[]> result = new Dictionary<string, string[]>(lines.Length);

				string[] fields;
				string[] links;
				foreach(string line in lines) {
					fields = line.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

					links = new string[fields.Length - 1];
					Array.Copy(fields, 1, links, 0, links.Length);

					result.Add(fields[0], links);
				}

				return result;
			}
		}

		/// <summary>
		/// Deletes the outgoing links of a page and all the target links that include the page.
		/// </summary>
		/// <param name="page">The full name of the page.</param>
		/// <returns><c>true</c> if the links are deleted, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <b>page</b> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>page</b> is empty.</exception>
		public bool DeleteOutgoingLinks(string page) {
			if(page == null) throw new ArgumentNullException("page");
			if(page.Length == 0) throw new ArgumentException("Page cannot be empty", "page");

			lock(this) {
				bool removedSomething = false;

				IDictionary<string, string[]> links = GetAllOutgoingLinks();

				// Step 1: remove source page, if any
				removedSomething = links.Remove(page);

				// Step 2: remove all target pages, for all source pages
				foreach(string key in links.Keys.ToList()) {
					List<string> currentLinks = new List<string>(links[key]);
					removedSomething |= currentLinks.Remove(page);
					links[key] = currentLinks.ToArray();
				}

				// Step 3: save on disk, if data changed
				if(removedSomething) {
					StringBuilder sb = new StringBuilder(links.Count * 100);
					foreach(string key in links.Keys) {
						sb.Append(key);
						sb.Append("|");
						for(int i = 0; i < links[key].Length; i++) {
							sb.Append(links[key][i]);
							if(i != links[key][i].Length - 1) sb.Append("|");
						}
						sb.Append("\r\n");
					}

					File.WriteAllText(GetFullPath(LinksFile), sb.ToString());
				}

				return removedSomething;
			}
		}

		/// <summary>
		/// Updates all outgoing links data for a page rename.
		/// </summary>
		/// <param name="oldName">The old page name.</param>
		/// <param name="newName">The new page name.</param>
		/// <returns><c>true</c> if the data is updated, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <b>oldName</b> or <b>newName</b> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>oldName</b> or <b>newName</b> are empty.</exception>
		public bool UpdateOutgoingLinksForRename(string oldName, string newName) {
			if(oldName == null) throw new ArgumentNullException("oldName");
			if(oldName.Length == 0) throw new ArgumentException("Old Name cannot be empty", "oldName");
			if(newName == null) throw new ArgumentNullException("newName");
			if(newName.Length == 0) throw new ArgumentException("New Name cannot be empty", "newName");

			lock(this) {
				bool replacedSomething = false;

				IDictionary<string, string[]> links = GetAllOutgoingLinks();

				// Step 1: rename source page, if any
				string[] tempLinks = null;
				if(links.TryGetValue(oldName, out tempLinks)) {
					links.Remove(oldName);
					links.Add(newName, tempLinks);
					replacedSomething = true;
				}

				// Step 2: rename all target pages, for all source pages
				foreach(string key in links.Keys) {
					for(int i = 0; i < links[key].Length; i++) {
						if(links[key][i] == oldName) {
							links[key][i] = newName;
							replacedSomething = true;
						}
					}
				}

				// Step 3: save on disk, if data changed
				if(replacedSomething) {
					StringBuilder sb = new StringBuilder(links.Count * 100);
					foreach(string key in links.Keys) {
						sb.Append(key);
						sb.Append("|");
						for(int i = 0; i < links[key].Length; i++) {
							sb.Append(links[key][i]);
							if(i != links[key][i].Length - 1) sb.Append("|");
						}
						sb.Append("\r\n");
					}

					File.WriteAllText(GetFullPath(LinksFile), sb.ToString());
				}

				return replacedSomething;
			}
		}

		/// <summary>
		/// Determines whether the application was started for the first time.
		/// </summary>
		/// <returns><c>true</c> if the application was started for the first time, <c>false</c> otherwise.</returns>
		public bool IsFirstApplicationStart() {
			return isFirstStart;
		}

	}

}
