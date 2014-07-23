
using System;
using System.Configuration;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Web;
using System.Web.Security;
using System.Web.Configuration;
using System.Text;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Allows access to all the ScrewTurn Wiki settings and configuration options.
	/// </summary>
	[System.Security.Permissions.FileIOPermission(System.Security.Permissions.SecurityAction.Assert,
		AllLocalFiles = System.Security.Permissions.FileIOPermissionAccess.PathDiscovery)]
	public static class Settings {

		private static string version = null;

		/// <summary>
		/// A value indicating whether the public directory can still be overridden.
		/// </summary>
		internal static bool CanOverridePublicDirectory = true;
		private static string _overriddenPublicDirectory = null;

		/// <summary>
		/// Gets the settings storage provider.
		/// </summary>
		public static ISettingsStorageProviderV30 Provider {
			get { return Collectors.SettingsProvider; }
		}

		/// <summary>
		/// Gets the version of the Wiki.
		/// </summary>
		public static string WikiVersion {
			get {
				if(version == null) {
					version = typeof(Settings).Assembly.GetName().Version.ToString();
				}

				return version;
			}
		}

		/// <summary>
		/// Gets the name of the Login Cookie.
		/// </summary>
		public static string LoginCookieName {
			get { return "ScrewTurnWikiLogin3"; }
		}

		/// <summary>
		/// Gets the name of the Culture Cookie.
		/// </summary>
		public static string CultureCookieName {
			get { return "ScrewTurnWikiCulture3"; }
		}

		/// <summary>
		/// Gets the Master Password, used to encrypt the Users data.
		/// </summary>
		public static string MasterPassword {
			get {
				string pass = WebConfigurationManager.AppSettings["MasterPassword"];
				if(pass == null || pass.Length == 0) throw new Exception("Configuration: MasterPassword cannot be null.");
				return pass;
			}
		}

		/// <summary>
		/// Gets direction of the application
		/// </summary>
		public static string Direction {
			get {
				if(Tools.IsRightToLeftCulture()) return "rtl";
				else return "ltr";
			}
		}

		/// <summary>
		/// Gets the bytes of the MasterPassword.
		/// </summary>
		public static byte[] MasterPasswordBytes {
			get {
				MD5 md5 = MD5CryptoServiceProvider.Create();
				return md5.ComputeHash(System.Text.UTF8Encoding.UTF8.GetBytes(MasterPassword));
			}
		}

		/// <summary>
		/// Gets the extension used for Pages, including the dot.
		/// </summary>
		public static string PageExtension {
			get { return ".ashx"; }
		}

		/// <summary>
		/// Gets the display name validation regex.
		/// </summary>
		public static string DisplayNameRegex {
			get { return "^[^\\|\\r\\n]*$"; }
		}

		/// <summary>
		/// Gets the Email validation Regex.
		/// </summary>
		public static string EmailRegex {
			get { return @"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\.)+))([a-zA-Z]{2,5}|[0-9]{1,3})$"; }
		}

		/// <summary>
		/// Gets the WikiTitle validation Regex.
		/// </summary>
		public static string WikiTitleRegex {
			get { return ".+"; }
		}

		/// <summary>
		/// Gets the MainUrl validation Regex.
		/// </summary>
		public static string MainUrlRegex {
			get { return @"^https?\://{1}\S+/$"; }
		}

		/// <summary>
		/// Gets the SMTP Server validation Regex.
		/// </summary>
		public static string SmtpServerRegex {
			get { return @"^[A-Za-z0-9\.\-_]+$"; }
		}

		/// <summary>
		/// Begins a bulk update session.
		/// </summary>
		public static void BeginBulkUpdate() {
			Provider.BeginBulkUpdate();
		}

		/// <summary>
		/// Ends a bulk update session.
		/// </summary>
		public static void EndBulkUpdate() {
			Provider.EndBulkUpdate();
		}

		#region Directories and Files

		/// <summary>
		/// Gets the Root Directory of the Wiki.
		/// </summary>
		public static string RootDirectory {
			get { return System.Web.HttpRuntime.AppDomainAppPath; }
		}

		/// <summary>
		/// Overrides the public directory, unless it's too late to do that.
		/// </summary>
		/// <param name="fullPath">The full path.</param>
		internal static void OverridePublicDirectory(string fullPath) {
			if(!CanOverridePublicDirectory) throw new InvalidOperationException("Cannot override public directory - that can only be done during Settings Storage Provider initialization");

			_overriddenPublicDirectory = fullPath;
		}

		/// <summary>
		/// Gets the Public Directory of the Wiki.
		/// </summary>
		public static string PublicDirectory {
			get {
				if(!string.IsNullOrEmpty(_overriddenPublicDirectory)) return _overriddenPublicDirectory;

				string pubDirName = PublicDirectoryName;
				if(Path.IsPathRooted(pubDirName)) return pubDirName;
				else {
					string path = Path.Combine(RootDirectory, pubDirName);
					if(!path.EndsWith(Path.DirectorySeparatorChar.ToString())) path += Path.DirectorySeparatorChar;
					return path;
				}
			}
		}

		/// <summary>
		/// Gets the Public Directory Name (without the full Path) of the Wiki.
		/// </summary>
		private static string PublicDirectoryName {
			get {
				string dir = WebConfigurationManager.AppSettings["PublicDirectory"];
				if(string.IsNullOrEmpty(dir)) throw new InvalidConfigurationException("PublicDirectory cannot be empty or null");
				dir = dir.Trim('\\', '/'); // Remove '/' and '\' from head and tail
				if(string.IsNullOrEmpty(dir)) throw new InvalidConfigurationException("PublicDirectory cannot be empty or null");
				else return dir;
			}
		}

		/// <summary>
		/// Gets the Name of the Themes directory.
		/// </summary>
		public static string ThemesDirectoryName {
			get { return "Themes"; }
		}

		/// <summary>
		/// Gets the Themes directory.
		/// </summary>
		public static string ThemesDirectory {
			get { return RootDirectory + ThemesDirectoryName + Path.DirectorySeparatorChar; }
		}

		/// <summary>
		/// Gets the Name of the JavaScript Directory.
		/// </summary>
		public static string JsDirectoryName {
			get { return "JS"; }
		}

		/// <summary>
		/// Gets the JavaScript Directory.
		/// </summary>
		public static string JsDirectory {
			get { return RootDirectory + JsDirectoryName + Path.DirectorySeparatorChar; }
		}

		#endregion

		#region Basic Settings and Associated Data

		/// <summary>
		/// Gets an integer.
		/// </summary>
		/// <param name="value">The string value.</param>
		/// <param name="def">The default value, returned when string parsing fails.</param>
		/// <returns>The result.</returns>
		private static int GetInt(string value, int def) {
			if(value == null) return def;
			int i = def;
			int.TryParse(value, out i);
			return i;
		}

		/// <summary>
		/// Gets a boolean.
		/// </summary>
		/// <param name="value">The string value.</param>
		/// <param name="def">The default value, returned when parsing fails.</param>
		/// <returns>The result.</returns>
		private static bool GetBool(string value, bool def) {
			if(value == null) return def;
			else {
				if(value.ToLowerInvariant() == "yes") return true;
				bool b = def;
				bool.TryParse(value, out b);
				return b;
			}
		}

		/// <summary>
		/// Prints a boolean.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns>The string result.</returns>
		private static string PrintBool(bool value) {
			return value ? "yes" : "no";
		}

		/// <summary>
		/// Gets a string.
		/// </summary>
		/// <param name="value">The raw string.</param>
		/// <param name="def">The default value, returned when the raw string is <c>null</c>.</param>
		/// <returns>The result.</returns>
		private static string GetString(string value, string def) {
			if(string.IsNullOrEmpty(value)) return def;
			else return value;
		}

		/// <summary>
		/// Gets or sets the Title of the Wiki.
		/// </summary>
		public static string WikiTitle {
			get {
				return GetString(Provider.GetSetting("WikiTitle"), "ScrewTurn Wiki");
			}
			set {
				Provider.SetSetting("WikiTitle", value);
			}
		}

		/// <summary>
		/// Gets or sets the SMTP Server.
		/// </summary>
		public static string SmtpServer {
			get {
				return GetString(Provider.GetSetting("SmtpServer"), "smtp.server.com");
			}
			set {
				Provider.SetSetting("SmtpServer", value);
			}
		}

		/// <summary>
		/// Gets or sets the SMTP Server Username.
		/// </summary>
		public static string SmtpUsername {
			get {
				return GetString(Provider.GetSetting("SmtpUsername"), "");
			}
			set {
				Provider.SetSetting("SmtpUsername", value);
			}
		}

		/// <summary>
		/// Gets or sets the SMTP Server Password.
		/// </summary>
		public static string SmtpPassword {
			get {
				return GetString(Provider.GetSetting("SmtpPassword"), "");
			}
			set {
				Provider.SetSetting("SmtpPassword", value);
			}
		}

		/// <summary>
		/// Gets or sets the SMTP Server Port.
		/// </summary>
		public static int SmtpPort {
			get {
				return GetInt(Provider.GetSetting("SmtpPort"), -1);
			}
			set {
				Provider.SetSetting("SmtpPort", value.ToString());
			}
		}

		/// <summary>
		/// Gets or sets a value specifying whether to enable SSL in SMTP.
		/// </summary>
		public static bool SmtpSsl {
			get {
				return GetBool(Provider.GetSetting("SmtpSsl"), false);
			}
			set {
				Provider.SetSetting("SmtpSsl", PrintBool(value));
			}
		}

		/// <summary>
		/// Gets or sets a value specifying whether the access to the Wiki is public or not (in this case users won't need to login in order to edit pagesCache).
		/// </summary>
		/// <remarks>Deprecated in version 3.0.</remarks>
		public static bool PublicAccess {
			get {
				return GetBool(Provider.GetSetting("PublicAccess"), false);
			}
			set {
				Provider.SetSetting("PublicAccess", PrintBool(value));
			}
		}

		/// <summary>
		/// Gets or sets a value specifying whether the access to the Wiki is private or not (in this case users won't be able to view pagesCache unless they are logged in).
		/// </summary>
		/// <remarks>Deprecated in version 3.0.</remarks>
		public static bool PrivateAccess {
			get {
				return GetBool(Provider.GetSetting("PrivateAccess"), false);
			}
			set {
				Provider.SetSetting("PrivateAccess", PrintBool(value));
			}
		}

		/// <summary>
		/// Gets or sets a value specifying whether, in Public Access mode, anonymous file management is allowed.
		/// </summary>
		/// <remarks>Deprecated in version 3.0.</remarks>
		public static bool FileManagementInPublicAccessAllowed {
			get {
				return GetBool(Provider.GetSetting("FileManagementInPublicAccessAllowed"), false);
			}
			set {
				Provider.SetSetting("FileManagementInPublicAccessAllowed", PrintBool(value));
			}
		}

		/// <summary>
		/// Gets or sets a value specifying whether Users can create new accounts or not (in this case Register.aspx won't be available).
		/// </summary>
		public static bool UsersCanRegister {
			get {
				return GetBool(Provider.GetSetting("UsersCanRegister"), true);
			}
			set {
				Provider.SetSetting("UsersCanRegister", PrintBool(value));
			}
		}

		/// <summary>
		/// Gets or sets a value specifying whether to disable the Captcha control in the Registration Page.
		/// </summary>
		public static bool DisableCaptchaControl {
			get {
				return GetBool(Provider.GetSetting("DisableCaptchaControl"), false);
			}
			set {
				Provider.SetSetting("DisableCaptchaControl", PrintBool(value));
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether to enable the "View Page Code" feature.
		/// </summary>
		public static bool EnableViewPageCodeFeature {
			get {
				return GetBool(Provider.GetSetting("EnableViewPageCode"), true);
			}
			set {
				Provider.SetSetting("EnableViewPageCode", PrintBool(value));
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether to enable the Page Information DIV.
		/// </summary>
		public static bool EnablePageInfoDiv {
			get {
				return GetBool(Provider.GetSetting("EnablePageInfoDiv"), true);
			}
			set {
				Provider.SetSetting("EnablePageInfoDiv", PrintBool(value));
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether to enable the Page Toolbar.
		/// </summary>
		public static bool EnablePageToolbar {
			get {
				return GetBool(Provider.GetSetting("EnablePageToolbar"), true);
			}
			set {
				Provider.SetSetting("EnablePageToolbar", PrintBool(value));
			}
		}

		/// <summary>
		/// Gets or sets the number of items displayed in pages/users lists.
		/// </summary>
		public static int ListSize {
			get {
				return GetInt(Provider.GetSetting("ListSize"), 50);
			}
			set {
				Provider.SetSetting("ListSize", value.ToString());
			}
		}

		/// <summary>
		/// Gets or sets the Account Activation Mode.
		/// </summary>
		public static AccountActivationMode AccountActivationMode {
			get {
				string value = GetString(Provider.GetSetting("AccountActivationMode"), "EMAIL");
				switch(value.ToLowerInvariant()) {
					case "email":
						return AccountActivationMode.Email;
					case "admin":
						return AccountActivationMode.Administrator;
					case "auto":
						return AccountActivationMode.Auto;
					default:
						return AccountActivationMode.Email;
				}
			}
			set {
				string aa = "";
				switch(value) {
					case AccountActivationMode.Email:
						aa = "EMAIL";
						break;
					case AccountActivationMode.Administrator:
						aa = "ADMIN";
						break;
					case AccountActivationMode.Auto:
						aa = "AUTO";
						break;
					default:
						throw new ArgumentException("Invalid Account Activation Mode.");
				}

				Provider.SetSetting("AccountActivationMode", aa);
			}
		}

		/// <summary>
		/// Gets or sets the page change moderation mode.
		/// </summary>
		public static ChangeModerationMode ChangeModerationMode {
			get {
				string value = GetString(Provider.GetSetting("ChangeModerationMode"),
					ChangeModerationMode.None.ToString());

				return (ChangeModerationMode)Enum.Parse(typeof(ChangeModerationMode), value, true);
			}
			set {
				Provider.SetSetting("ChangeModerationMode", value.ToString());
			}
		}

		/// <summary>
		/// Gets or sets a value specifying whether or not Users can create new Page.
		/// </summary>
		/// <remarks>Deprecated in version 3.0.</remarks>
		public static bool UsersCanCreateNewPages {
			get {
				return GetBool(Provider.GetSetting("UsersCanCreateNewPages"), true);
			}
			set {
				Provider.SetSetting("UsersCanCreateNewPages", PrintBool(value));
			}
		}

		/// <summary>
		/// Gets or sets a value specifying whether or not users can view uploaded Files.
		/// </summary>
		/// <remarks>Deprecated in version 3.0.</remarks>
		public static bool UsersCanViewFiles {
			get {
				return GetBool(Provider.GetSetting("UsersCanViewFiles"), true);
			}
			set {
				Provider.SetSetting("UsersCanViewFiles", PrintBool(value));
			}
		}

		/// <summary>
		/// Gets or sets a value specifying whether or not users can upload Files.
		/// </summary>
		/// <remarks>Deprecated in version 3.0.</remarks>
		public static bool UsersCanUploadFiles {
			get {
				return GetBool(Provider.GetSetting("UsersCanUploadFiles"), false);
			}
			set {
				Provider.SetSetting("UsersCanUploadFiles", PrintBool(value));
			}
		}

		/// <summary>
		/// Gets or sets a value specifying whether or not users can delete Files.
		/// </summary>
		/// <remarks>Deprecated in version 3.0.</remarks>
		public static bool UsersCanDeleteFiles {
			get {
				return GetBool(Provider.GetSetting("UsersCanDeleteFiles"), false);
			}
			set {
				Provider.SetSetting("UsersCanDeleteFiles", PrintBool(value));
			}
		}

		/// <summary>
		/// Gets or sets a value specifying whether or not users can create new Categories.
		/// </summary>
		/// <remarks>Deprecated in version 3.0.</remarks>
		public static bool UsersCanCreateNewCategories {
			get {
				return GetBool(Provider.GetSetting("UsersCanCreateNewCategories"), false);
			}
			set {
				Provider.SetSetting("UsersCanCreateNewCategories", PrintBool(value));
			}
		}

		/// <summary>
		/// Gets or sets a value specifying whether or not users can manage Page Categories.
		/// </summary>
		/// <remarks>Deprecated in version 3.0.</remarks>
		public static bool UsersCanManagePageCategories {
			get {
				return GetBool(Provider.GetSetting("UsersCanManagePageCategories"), false);
			}
			set {
				Provider.SetSetting("UsersCanManagePageCategories", PrintBool(value));
			}
		}

		/// <summary>
		/// Gets or sets the Default Language.
		/// </summary>
		public static string DefaultLanguage {
			get {
				return GetString(Provider.GetSetting("DefaultLanguage"), "en-US");
			}
			set {
				Provider.SetSetting("DefaultLanguage", value);
			}
		}

		/// <summary>
		/// Gets or sets the Default Timezone (time delta in minutes).
		/// </summary>
		public static int DefaultTimezone {
			get {
				string value = GetString(Provider.GetSetting("DefaultTimezone"), "0");
				return int.Parse(value);
			}
			set {
				Provider.SetSetting("DefaultTimezone", value.ToString());
			}
		}

		/// <summary>
		/// Gets or sets the DateTime format.
		/// </summary>
		public static string DateTimeFormat {
			get {
				return GetString(Provider.GetSetting("DateTimeFormat"), "yyyy'/'MM'/'dd' 'HH':'mm");
			}
			set {
				Provider.SetSetting("DateTimeFormat", value);
			}
		}

		/// <summary>
		/// Gets or sets the main URL of the Wiki.
		/// </summary>
		public static string MainUrl {
			get {
				string s = GetString(Provider.GetSetting("MainUrl"), "http://www.server.com/");
				if(!s.EndsWith("/")) s += "/";
				return s;
			}
			set {
				Provider.SetSetting("MainUrl", value);
			}
		}

		/// <summary>
		/// Gets the main URL of the wiki, defaulting to the current request URL if none is configured manually.
		/// </summary>
		/// <returns>The URL of the wiki.</returns>
		public static Uri GetMainUrl() {
			Uri mainUrl = new Uri(MainUrl);
			if(mainUrl.Host == "www.server.com") {
				try {
					// STW never uses internal URLs with slashes, so trimming to the last slash should work
					// Example: http://server/wiki/namespace.page.ashx
					string temp = HttpContext.Current.Request.Url.ToString();
					mainUrl = new Uri(temp.Substring(0, temp.LastIndexOf("/") + 1));
				}
				catch { }
			}
			return mainUrl;
		}

		/// <summary>
		/// Gets the correct path to use with Cookies.
		/// </summary>
		public static string CookiePath {
			get {
				string requestUrl = HttpContext.Current.Request.RawUrl;
				string virtualDirectory = HttpContext.Current.Request.ApplicationPath;
				// We need to convert the case of the virtual directory to that used in the url
				// Return the virtual directory as is if we can't find it in the URL
				if(requestUrl.ToLowerInvariant().Contains(virtualDirectory.ToLowerInvariant())) {
					return requestUrl.Substring(requestUrl.ToLowerInvariant().IndexOf(virtualDirectory.ToLowerInvariant()), virtualDirectory.Length);
				}
				return virtualDirectory;
			}
		}

		/// <summary>
		/// Gets or sets the Contact Email.
		/// </summary>
		public static string ContactEmail {
			get {
				return GetString(Provider.GetSetting("ContactEmail"), "info@server.com");
			}
			set {
				Provider.SetSetting("ContactEmail", value);
			}
		}

		/// <summary>
		/// Gets or sets the Sender Email.
		/// </summary>
		public static string SenderEmail {
			get {
				return GetString(Provider.GetSetting("SenderEmail"), "no-reply@server.com");
			}
			set {
				Provider.SetSetting("SenderEmail", value);
			}
		}

		/// <summary>
		/// Gets or sets the email addresses to send a message to when an error occurs.
		/// </summary>
		public static string[] ErrorsEmails {
			get {
				return GetString(Provider.GetSetting("ErrorsEmails"), "").Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
			}
			set {
				Provider.SetSetting("ErrorsEmails", string.Join("|", value));
			}
		}

		/// <summary>
		/// Gets or sets the Defaul Page of the Wiki.
		/// </summary>
		public static string DefaultPage {
			get {
				return GetString(Provider.GetSetting("DefaultPage"), "MainPage");
			}
			set {
				Provider.SetSetting("DefaultPage", value);
			}
		}

		/// <summary>
		/// Gets or sets the maximum number of recent changes to display with {RecentChanges} special tag.
		/// </summary>
		public static int MaxRecentChangesToDisplay {
			get {
				return GetInt(Provider.GetSetting("MaxRecentChangesToDisplay"), 10);
			}
			set {
				Provider.SetSetting("MaxRecentChangesToDisplay", value.ToString());
			}
		}

		/// <summary>
		/// Gets or sets a value specifying whether to enable double-click Page editing.
		/// </summary>
		public static bool EnableDoubleClickEditing {
			get {
				return GetBool(Provider.GetSetting("EnableDoubleClickEditing"), false);
			}
			set {
				Provider.SetSetting("EnableDoubleClickEditing", PrintBool(value));
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether to enable section editing.
		/// </summary>
		public static bool EnableSectionEditing {
			get {
				return GetBool(Provider.GetSetting("EnableSectionEditing"), true);
			}
			set {
				Provider.SetSetting("EnableSectionEditing", PrintBool(value));
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether to display section anchors.
		/// </summary>
		public static bool EnableSectionAnchors {
			get {
				return GetBool(Provider.GetSetting("EnableSectionAnchors"), true);
			}
			set {
				Provider.SetSetting("EnableSectionAnchors", PrintBool(value));
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether to disable the Breadcrumbs Trail.
		/// </summary>
		public static bool DisableBreadcrumbsTrail {
			get {
				return GetBool(Provider.GetSetting("DisableBreadcrumbsTrail"), false);
			}
			set {
				Provider.SetSetting("DisableBreadcrumbsTrail", PrintBool(value));
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether the editor should auto-generate page names from the title.
		/// </summary>
		public static bool AutoGeneratePageNames {
			get {
				return GetBool(Provider.GetSetting("AutoGeneratePageNames"), true);
			}
			set {
				Provider.SetSetting("AutoGeneratePageNames", PrintBool(value));
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether or not to process single line breaks in WikiMarkup.
		/// </summary>
		public static bool ProcessSingleLineBreaks {
			get {
				return GetBool(Provider.GetSetting("ProcessSingleLineBreaks"), false);
			}
			set {
				Provider.SetSetting("ProcessSingleLineBreaks", PrintBool(value));
			}
		}

		/// <summary>
		/// Gets or sets the # of Backups that are kept. Older backups are deleted after Page editing.
		/// </summary>
		/// <remarks>-1 indicates that no backups are deleted.</remarks>
		public static int KeptBackupNumber {
			get {
				return GetInt(Provider.GetSetting("KeptBackupNumber"), -1);
			}
			set {
				Provider.SetSetting("KeptBackupNumber", value.ToString());
			}
		}

		/// <summary>
		/// Gets the theme name for a namespace.
		/// </summary>
		/// <param name="nspace">The namespace (<c>null</c> for the root).</param>
		/// <returns>The theme name.</returns>
		public static string GetTheme(string nspace) {
			if(!string.IsNullOrEmpty(nspace)) nspace = Pages.FindNamespace(nspace).Name;
			string propertyName = "Theme" + (!string.IsNullOrEmpty(nspace) ? "-" + nspace : "");
			return GetString(Provider.GetSetting(propertyName), "Default");
		}

		/// <summary>
		/// Sets the theme for a namespace.
		/// </summary>
		/// <param name="nspace">The namespace (<c>null</c> for the root).</param>
		/// <param name="theme">The theme name.</param>
		public static void SetTheme(string nspace, string theme) {
			if(!string.IsNullOrEmpty(nspace)) nspace = Pages.FindNamespace(nspace).Name;
			string propertyName = "Theme" + (!string.IsNullOrEmpty(nspace) ? "-" + nspace : "");
			Provider.SetSetting(propertyName, theme);
		}

		/// <summary>
		/// Gets the Theme Path for a namespace.
		/// </summary>
		/// <param name="nspace">The namespace (<c>null</c> for the root).</param>
		/// <returns>The path of the theme.</returns>
		public static string GetThemePath(string nspace) {
			string theme = GetTheme(nspace);
			if(!Directory.Exists(ThemesDirectory + theme)) return ThemesDirectoryName + "/Default/";
			else return ThemesDirectoryName + "/" + theme + "/";
		}

		/// <summary>
		/// Gets or sets the list of allowed file types.
		/// </summary>
		public static string[] AllowedFileTypes {
			get {
				string raw = GetString(Provider.GetSetting("AllowedFileTypes"), "jpg|jpeg|gif|png|tif|tiff|bmp|svg|htm|html|zip|rar|pdf|txt|doc|xls|ppt|docx|xlsx|pptx");
				return raw.ToLowerInvariant().Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
			}
			set {
				string res = string.Join("|", value);
				Provider.SetSetting("AllowedFileTypes", res);
			}
		}

		/// <summary>
		/// Gets or sets the file download count filter mode.
		/// </summary>
		public static FileDownloadCountFilterMode FileDownloadCountFilterMode {
			get {
				string raw = GetString(Provider.GetSetting("FileDownloadCountFilterMode"), FileDownloadCountFilterMode.CountAll.ToString());
				return (FileDownloadCountFilterMode)Enum.Parse(typeof(FileDownloadCountFilterMode), raw);
			}
			set {
				Provider.SetSetting("FileDownloadCountFilterMode", value.ToString());
			}
		}

		/// <summary>
		/// Gets or sets the file download count extension filter.
		/// </summary>
		public static string[] FileDownloadCountFilter {
			get {
				string raw = GetString(Provider.GetSetting("FileDownloadCountFilter"), "");
				return raw.ToLowerInvariant().Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
			}
			set {
				string res = string.Join("|", value);
				Provider.SetSetting("FileDownloadCountFilter", res);
			}
		}

		/// <summary>
		/// Gets or sets a value specifying whether script tags are allowed.
		/// </summary>
		public static bool ScriptTagsAllowed {
			get {
				return GetBool(Provider.GetSetting("ScriptTagsAllowed"), false);
			}
			set {
				Provider.SetSetting("ScriptTagsAllowed", PrintBool(value));
			}
		}

		/// <summary>
		/// Gets or sets the Logging Level.
		/// </summary>
		public static LoggingLevel LoggingLevel {
			get {
				int value = GetInt(Provider.GetSetting("LoggingLevel"), 3);
				return (LoggingLevel)value;
			}
			set {
				Provider.SetSetting("LoggingLevel", ((int)value).ToString());
			}
		}

		/// <summary>
		/// Gets or sets the Max size of the Log file (KB).
		/// </summary>
		public static int MaxLogSize {
			get {
				return GetInt(Provider.GetSetting("MaxLogSize"), 256);
			}
			set {
				Provider.SetSetting("MaxLogSize", value.ToString());
			}
		}

		/// <summary>
		/// Gets or sets the IP/Host filter for page editing.
		/// </summary>
		public static string IpHostFilter {
			get {
				return GetString(Provider.GetSetting("IpHostFilter"), "");
			}
			set {
				Provider.SetSetting("IpHostFilter", value);
			}
		}

		/// <summary>
		/// Gets or sets the max number of recent changes to log.
		/// </summary>
		public static int MaxRecentChanges {
			get {
				return GetInt(Provider.GetSetting("MaxRecentChanges"), 100);
			}
			set {
				Provider.SetSetting("MaxRecentChanges", value.ToString());
			}
		}

		/// <summary>
		/// Gets or sets a valus specifying whether or not Administrators cannot access the Configuration section in the Administration page.
		/// </summary>
		/// <remarks>Deprecated in version 3.0.</remarks>
		[Obsolete]
		public static bool ConfigVisibleToAdmins {
			get {
				return GetBool(Provider.GetSetting("ConfigVisibleToAdmins"), false);
			}
			set {
				Provider.SetSetting("ConfigVisibleToAdmins", PrintBool(value));
			}
		}

		/// <summary>
		/// Gets or sets the Type name of the Default Users Provider.
		/// </summary>
		public static string DefaultUsersProvider {
			get {
				return GetString(Provider.GetSetting("DefaultUsersProvider"), typeof(UsersStorageProvider).ToString());
			}
			set {
				Provider.SetSetting("DefaultUsersProvider", value);
			}
		}

		/// <summary>
		/// Gets or sets the Type name of the Default Pages Provider.
		/// </summary>
		public static string DefaultPagesProvider {
			get {
				return GetString(Provider.GetSetting("DefaultPagesProvider"), typeof(PagesStorageProvider).ToString());
			}
			set {
				Provider.SetSetting("DefaultPagesProvider", value);
			}
		}

		/// <summary>
		/// Gets or sets the Type name of the Default Files Provider.
		/// </summary>
		public static string DefaultFilesProvider {
			get {
				return GetString(Provider.GetSetting("DefaultFilesProvider"), typeof(FilesStorageProvider).ToString());
			}
			set {
				Provider.SetSetting("DefaultFilesProvider", value);
			}
		}

		/// <summary>
		/// Gets or sets the Default Cache Provider.
		/// </summary>
		public static string DefaultCacheProvider {
			get {
				return GetString(Provider.GetSetting("DefaultCacheProvider"), typeof(CacheProvider).ToString());
			}
			set {
				Provider.SetSetting("DefaultCacheProvider", value);
			}
		}

		/// <summary>
		/// Gets or sets the Discussion Permissions.
		/// </summary>
		public static string DiscussionPermissions {
			get {
				return GetString(Provider.GetSetting("DiscussionPermissions"), "PAGE");
			}
			set {
				Provider.SetSetting("DiscussionPermissions", value);
			}
		}

		/// <summary>
		/// Gets or sets a value specifying whether or not to disable concurrent editing of Pages.
		/// </summary>
		public static bool DisableConcurrentEditing {
			get {
				return GetBool(Provider.GetSetting("DisableConcurrentEditing"), false);
			}
			set {
				Provider.SetSetting("DisableConcurrentEditing", PrintBool(value));
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether to use the Visual editor as default.
		/// </summary>
		public static bool UseVisualEditorAsDefault {
			get {
				return GetBool(Provider.GetSetting("UseVisualEditorAsDefault"), false);
			}
			set {
				Provider.SetSetting("UseVisualEditorAsDefault", PrintBool(value));
			}
		}

		/// <summary>
		/// Gets or sets the name of the default Administrators group.
		/// </summary>
		public static string AdministratorsGroup {
			get {
				return GetString(Provider.GetSetting("AdministratorsGroup"), "Administrators");
			}
			set {
				Provider.SetSetting("AdministratorsGroup", value);
			}
		}

		/// <summary>
		/// Gets or sets the name of the default Users group.
		/// </summary>
		public static string UsersGroup {
			get {
				return GetString(Provider.GetSetting("UsersGroup"), "Users");
			}
			set {
				Provider.SetSetting("UsersGroup", value);
			}
		}

		/// <summary>
		/// Gets or sets the name of the default Anonymous users group.
		/// </summary>
		public static string AnonymousGroup {
			get {
				return GetString(Provider.GetSetting("AnonymousGroup"), "Anonymous");
			}
			set {
				Provider.SetSetting("AnonymousGroup", value);
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether to display gravatars.
		/// </summary>
		public static bool DisplayGravatars {
			get {
				return GetBool(Provider.GetSetting("DisplayGravatars"), true);
			}
			set {
				Provider.SetSetting("DisplayGravatars", PrintBool(value));
			}
		}

		/// <summary>
		/// Gets or sets the functioning mode of RSS feeds.
		/// </summary>
		public static RssFeedsMode RssFeedsMode {
			get {
				string value = GetString(Provider.GetSetting("RssFeedsMode"), RssFeedsMode.Summary.ToString());
				return (RssFeedsMode)Enum.Parse(typeof(RssFeedsMode), value);
			}
			set {
				Provider.SetSetting("RssFeedsMode", value.ToString());
			}
		}

		#endregion

		#region Advanced Settings and Associated Data

		/// <summary>
		/// Gets or sets a value indicating whether to disable the Automatic Version Check.
		/// </summary>
		public static bool DisableAutomaticVersionCheck {
			get {
				return GetBool(Provider.GetSetting("DisableAutomaticVersionCheck"), false);
			}
			set {
				Provider.SetSetting("DisableAutomaticVersionCheck", PrintBool(value));
			}
		}

		/// <summary>
		/// Gets or sets the Max file size for upload (in KB).
		/// </summary>
		public static int MaxFileSize {
			get {
				return GetInt(Provider.GetSetting("MaxFileSize"), 10240);
			}
			set {
				Provider.SetSetting("MaxFileSize", value.ToString());
			}
		}

		/// <summary>
		/// Gets or sets a value specifying whether to disable the cache.
		/// </summary>
		public static bool DisableCache {
			get {
				return GetBool(Provider.GetSetting("DisableCache"), false);
			}
			set {
				Provider.SetSetting("DisableCache", PrintBool(value));
			}
		}

		/// <summary>
		/// Gets or sets the Cache Size.
		/// </summary>
		public static int CacheSize {
			get {
				return GetInt(Provider.GetSetting("CacheSize"), 100);
			}
			set {
				Provider.SetSetting("CacheSize", value.ToString());
			}
		}

		/// <summary>
		/// Gets or sets the Cache Cut Size.
		/// </summary>
		public static int CacheCutSize {
			get {
				return GetInt(Provider.GetSetting("CacheCutSize"), 20);
			}
			set {
				Provider.SetSetting("CacheCutSize", value.ToString());
			}
		}

		/// <summary>
		/// Gets or sets a value specifying whether ViewState compression is enabled or not.
		/// </summary>
		public static bool EnableViewStateCompression {
			get {
				return GetBool(Provider.GetSetting("EnableViewStateCompression"), false);
			}
			set {
				Provider.SetSetting("EnableViewStateCompression", PrintBool(value));
			}
		}

		/// <summary>
		/// Gets or sets a value specifying whether HTTP compression is enabled or not.
		/// </summary>
		public static bool EnableHttpCompression {
			get {
				return GetBool(Provider.GetSetting("EnableHttpCompression"), false);
			}
			set {
				Provider.SetSetting("EnableHttpCompression", PrintBool(value));
			}
		}

		/// <summary>
		/// Gets or sets the Username validation Regex.
		/// </summary>
		public static string UsernameRegex {
			get {
				return GetString(Provider.GetSetting("UsernameRegex"), @"^\w[\w\ !$@%^\.\(\)\-_]{3,25}$");
			}
			set {
				Provider.SetSetting("UsernameRegex", value);
			}
		}

		/// <summary>
		/// Gets or sets the Password validation Regex.
		/// </summary>
		public static string PasswordRegex {
			get {
					return GetString(Provider.GetSetting("PasswordRegex"), @"^\w[\w~!@#$%^\(\)\[\]\{\}\.,=\-_\ ]{5,25}$");
			}
			set {
				Provider.SetSetting("PasswordRegex", value);
			}
		}

		/// <summary>
		/// Gets or sets the last page indexing.
		/// </summary>
		/// <value>The last page indexing DateTime.</value>
		public static DateTime LastPageIndexing {
			get {
				return DateTime.ParseExact(GetString(Provider.GetSetting("LastPageIndexing"), DateTime.Now.AddYears(-10).ToString("yyyyMMddHHmmss")),
					"yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture);
			}
			set {
				Provider.SetSetting("LastPageIndexing", value.ToString("yyyyMMddHHmmss"));
			}
		}

		#endregion

		/// <summary>
		/// Determines whether a meta-data item is global or namespace-specific.
		/// </summary>
		/// <param name="item">The item to test.</param>
		/// <returns><c>true</c> if the meta-data item is global, <c>false</c> otherwise.</returns>
		public static bool IsMetaDataItemGlobal(MetaDataItem item) {
			int value = (int)item;
			return value < 100; // See MetaDataItem
		}

	}

	/// <summary>
	/// Lists legal RSS feeds function modes.
	/// </summary>
	public enum RssFeedsMode {
		/// <summary>
		/// RSS feeds serve full-text content.
		/// </summary>
		FullText,
		/// <summary>
		/// RSS feeds serve summary content.
		/// </summary>
		Summary,
		/// <summary>
		/// RSS feeds are disabled.
		/// </summary>
		Disabled
	}

	/// <summary>
	/// Lists legal file download filter modes.
	/// </summary>
	public enum FileDownloadCountFilterMode {
		/// <summary>
		/// Counts all downloads.
		/// </summary>
		CountAll,
		/// <summary>
		/// Counts only the specified extensions.
		/// </summary>
		CountSpecifiedExtensions,
		/// <summary>
		/// Excludes the specified extensions.
		/// </summary>
		ExcludeSpecifiedExtensions
	}

	/// <summary>
	/// Lists legal page change moderation mode values.
	/// </summary>
	public enum ChangeModerationMode {
		/// <summary>
		/// Page change moderation is disabled.
		/// </summary>
		None,
		/// <summary>
		/// Anyone who has page editing permissoins but not page management permissions 
		/// can edit pages, but the changes are held in moderation.
		/// </summary>
		RequirePageEditingPermissions,
		/// <summary>
		/// Anyone who has page viewing permissions can edit pages, but the changes are 
		/// held in moderation.
		/// </summary>
		RequirePageViewingPermissions
	}

	/// <summary>
	/// Lists legal account activation mode values.
	/// </summary>
	public enum AccountActivationMode {
		/// <summary>
		/// Users must activate their account via email.
		/// </summary>
		Email,
		/// <summary>
		/// Accounts must be activated by administrators.
		/// </summary>
		Administrator,
		/// <summary>
		/// Accounts are active by default.
		/// </summary>
		Auto
	}

}
